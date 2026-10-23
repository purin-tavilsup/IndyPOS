using System.Net;
using System.Text.Json;
using FluentAssertions;
using IndyPOS.Application.Common.Constants;
using IndyPOS.Cloud.Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using OpenIddict.Abstractions;
using Xunit;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace IndyPOS.Cloud.Auth.IntegrationTests;

/// <summary>
/// Auth issues the stores' access tokens (client credentials). Only an enabled store gets one, and every store
/// token is for CloudApi's audience, so a token meant for something else is refused there.
/// </summary>
public class StoreTokenTests(AuthPostgresFixture postgres) : IClassFixture<AuthPostgresFixture>
{
    private const string Issuer = "https://auth.test/";
    private const string StoreId = "STORE-A";
    private const string ClientId = "store_STORE-A";
    private const string Secret = "store-a-secret";

    private async Task<(AuthTestHost Host, string ConnectionString)> StartAsync(bool enabled = true)
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        var host = TokenHosts.Create(connectionString, Issuer);
        await TokenHosts.AddStoreClientAsync(host, StoreId, ClientId, Secret, enabled);
        return (host, connectionString);
    }

    private static async Task<StoreClient> StoreClientAsync(string connectionString)
    {
        await using var db = AuthPostgresFixture.CreateContext(connectionString);
        return await db.StoreClients.AsNoTracking().SingleAsync();
    }

    [Fact]
    public async Task Token_ForAnEnabledStore_HasItsStoreIdAndTheApiAudience()
    {
        var (host, _) = await StartAsync();
        await using var _ = host;

        var token = new JsonWebToken(await TokenHosts.AccessTokenAsync(host, ClientId, Secret));

        token.GetClaim("store_id").Value.Should().Be(StoreId);
        token.Audiences.Should().Equal(AuthResources.CloudApi);
        token.Issuer.Should().Be(Issuer);
    }

    [Fact]
    public async Task Token_ForAnEnabledStore_RecordsWhenItAuthenticated()
    {
        var (host, connectionString) = await StartAsync();
        await using var _ = host;

        await TokenHosts.AccessTokenAsync(host, ClientId, Secret);

        (await StoreClientAsync(connectionString)).LastAuthenticatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Token_ForADisabledStore_IsRefusedAsInvalidClient()
    {
        var (host, connectionString) = await StartAsync(enabled: false);
        await using var _ = host;

        var response = await TokenHosts.RequestTokenAsync(host, ClientId, Secret);

        (await TokenHosts.ErrorOfAsync(response)).Should().Be(Errors.InvalidClient);
        (await StoreClientAsync(connectionString)).LastAuthenticatedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task Token_ForAnUnknownClient_IsRefusedAsInvalidClient()
    {
        var (host, _) = await StartAsync();
        await using var _ = host;

        var response = await TokenHosts.RequestTokenAsync(host, "store_NOPE", Secret);

        (await TokenHosts.ErrorOfAsync(response)).Should().Be(Errors.InvalidClient);
    }

    [Fact]
    public async Task Token_WithAWrongSecret_IsRefused()
    {
        var (host, _) = await StartAsync();
        await using var _ = host;

        var response = await TokenHosts.RequestTokenAsync(host, ClientId, "wrong");

        response.IsSuccessStatusCode.Should().BeFalse();
    }

    [Fact]
    public async Task Token_WithAnotherGrantType_IsRefused()
    {
        var (host, _) = await StartAsync();
        await using var _ = host;

        var response = await host.CreateClient().PostAsync("/oauth/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password", ["username"] = "x", ["password"] = "y", ["client_id"] = ClientId, ["client_secret"] = Secret
        }));

        response.IsSuccessStatusCode.Should().BeFalse();
    }

    // Behind Caddy the app sees plain http; without forwarded headers the advertised URLs would say http.
    [Fact]
    public async Task Discovery_BehindATlsTerminator_AdvertisesHttpsUrls()
    {
        await using var host = TokenHosts.Create(await postgres.CreateDatabaseAsync(), issuer: null);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/.well-known/openid-configuration");
        request.Headers.Add("X-Forwarded-Proto", "https");

        var response = await host.CreateClient().SendAsync(request);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("token_endpoint").GetString().Should().StartWith("https://");
    }

    [Fact]
    public async Task Startup_OutsideDevelopmentWithoutASigningKey_Fails()
    {
        await using var host = new ProductionAuthHost(await postgres.CreateDatabaseAsync());

        var start = () => host.CreateClient();

        start.Should().Throw<InvalidOperationException>().WithMessage("*signing key*");
    }

    // Without a configured issuer the issuer would come from each request's Host; CloudApi pins one, so it would refuse
    // every token with no clear reason. And over http, CloudApi would fetch the signing keys in the clear.
    [Theory]
    [InlineData(null)]
    [InlineData("http://auth.test/")]
    public async Task Startup_OutsideDevelopmentWithoutAnHttpsIssuer_Fails(string? issuer)
    {
        await using var host = new ProductionAuthHost(await postgres.CreateDatabaseAsync(), new Dictionary<string, string?>
        {
            ["OpenIddict:RsaSigningKey"] = TestKeys.Rsa2048Base64Pem(),
            ["OpenIddict:Issuer"] = issuer
        });

        var start = () => host.CreateClient();

        start.Should().Throw<InvalidOperationException>().WithMessage("*Issuer*");
    }

    private sealed class ProductionAuthHost(string connectionString, IReadOnlyDictionary<string, string?>? settings = null)
        : AuthTestHost(connectionString, settings)
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            Microsoft.AspNetCore.Hosting.HostingAbstractionsWebHostBuilderExtensions.UseEnvironment(builder, "Production");
        }
    }
}

/// <summary>Builds token-issuing Auth hosts for tests: http behind a pretend TLS terminator, a fixed issuer.</summary>
internal static class TokenHosts
{
    public static AuthTestHost Create(string connectionString, string? issuer) =>
        new(connectionString, new Dictionary<string, string?>
        {
            ["OpenIddict:TlsTerminatedUpstream"] = "true",
            ["OpenIddict:Issuer"] = issuer
        });

    public static async Task AddStoreClientAsync(AuthTestHost host, string storeId, string clientId, string secret, bool enabled)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var applications = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        await applications.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = clientId, ClientSecret = secret, ClientType = ClientTypes.Confidential, DisplayName = storeId,
            Permissions =
            {
                Permissions.Endpoints.Token, Permissions.GrantTypes.ClientCredentials,
                Permissions.Prefixes.Scope + AuthScopes.SyncWrite, Permissions.Prefixes.Scope + AuthScopes.MasterRead
            }
        });
        var db = scope.ServiceProvider.GetRequiredService<Data.AuthDbContext>();
        db.StoreClients.Add(new StoreClient { StoreId = storeId, ClientId = clientId, Enabled = enabled, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();
    }

    public static Task<HttpResponseMessage> RequestTokenAsync(AuthTestHost host, string clientId, string secret, string scope = "sync.write master.read") =>
        host.CreateClient().PostAsync("/oauth/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials", ["client_id"] = clientId, ["client_secret"] = secret, ["scope"] = scope
        }));

    public static async Task<string> AccessTokenAsync(AuthTestHost host, string clientId, string secret, string scope = "sync.write master.read")
    {
        var response = await RequestTokenAsync(host, clientId, secret, scope);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("access_token").GetString()!;
    }

    public static async Task<string?> ErrorOfAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.TryGetProperty("error", out var error) ? error.GetString() : null;
    }
}

internal static class TestKeys
{
    public static string Rsa2048Base64Pem()
    {
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(rsa.ExportPkcs8PrivateKeyPem()));
    }
}
