using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using IndyPOS.Application.Common.Constants;
using Xunit;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace IndyPOS.Cloud.Auth.IntegrationTests;

/// <summary>
/// CloudApi registers and deactivates stores through Auth's store-client API, with its own service client. Nothing
/// else may call it: not a store, not an anonymous caller. Deactivation is final.
/// </summary>
public class StoreClientApiTests(AuthPostgresFixture postgres) : IClassFixture<AuthPostgresFixture>
{
    private const string StoreId = "STORE-A";

    private sealed record Credentials(string ClientId, string ClientSecret);

    private async Task<AuthTestHost> StartAsync() =>
        TokenHosts.Create(await postgres.CreateDatabaseAsync(), "https://auth.test/");

    private static async Task<HttpClient> ServiceClientAsync(AuthTestHost host)
    {
        var token = await TokenHosts.AccessTokenAsync(host, "cloud-api", AuthTestHost.ServiceClientSecret, AuthScopes.StoreClients);
        var client = host.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<Credentials> PutAsync(HttpClient client, string storeId = StoreId)
    {
        var response = await client.PutAsJsonAsync($"/store-clients/{storeId}", new { displayName = "Store A" });
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<Credentials>())!;
    }

    [Fact]
    public async Task PutStoreClient_WithTheServiceToken_ReturnsCredentialsThatWork()
    {
        await using var host = await StartAsync();
        var credentials = await PutAsync(await ServiceClientAsync(host));

        var response = await TokenHosts.RequestTokenAsync(host, credentials.ClientId, credentials.ClientSecret);

        credentials.ClientId.Should().Be("store_STORE-A");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PutStoreClient_ForAnEnabledStore_ReplacesTheSecret()
    {
        await using var host = await StartAsync();
        var client = await ServiceClientAsync(host);
        var first = await PutAsync(client);

        var second = await PutAsync(client);

        second.ClientSecret.Should().NotBe(first.ClientSecret);
        (await TokenHosts.RequestTokenAsync(host, first.ClientId, first.ClientSecret)).IsSuccessStatusCode.Should().BeFalse();
    }

    [Fact]
    public async Task PutStoreClient_ForADisabledStore_ReturnsConflict()
    {
        await using var host = await StartAsync();
        var client = await ServiceClientAsync(host);
        await PutAsync(client);
        (await client.PostAsync($"/store-clients/{StoreId}/disable", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var response = await client.PutAsJsonAsync($"/store-clients/{StoreId}", new { displayName = "Store A" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DisableStoreClient_ThenToken_IsRefused()
    {
        await using var host = await StartAsync();
        var client = await ServiceClientAsync(host);
        var credentials = await PutAsync(client);

        await client.PostAsync($"/store-clients/{StoreId}/disable", null);

        (await TokenHosts.ErrorOfAsync(await TokenHosts.RequestTokenAsync(host, credentials.ClientId, credentials.ClientSecret)))
            .Should().Be(Errors.InvalidClient);
    }

    [Fact]
    public async Task DeleteStoreClient_RemovesItsCredentials()
    {
        await using var host = await StartAsync();
        var client = await ServiceClientAsync(host);
        var credentials = await PutAsync(client);

        (await client.DeleteAsync($"/store-clients/{StoreId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await TokenHosts.RequestTokenAsync(host, credentials.ClientId, credentials.ClientSecret)).IsSuccessStatusCode.Should().BeFalse();
    }

    // "a/b" escaped arrives as "a%2Fb", which would share a client with a store really named "a%2Fb".
    [Theory]
    [InlineData("a%2Fb")]
    [InlineData("a%20b")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public async Task PutStoreClient_WithAnInvalidStoreId_ReturnsBadRequest(string escapedStoreId)
    {
        await using var host = await StartAsync();

        var response = await (await ServiceClientAsync(host)).PutAsJsonAsync($"/store-clients/{escapedStoreId}", new { displayName = "x" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task StoreClientApi_WithoutAToken_ReturnsUnauthorized()
    {
        await using var host = await StartAsync();

        var response = await host.CreateClient().PutAsJsonAsync($"/store-clients/{StoreId}", new { displayName = "Store A" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task StoreClientApi_WithAStoreToken_ReturnsForbidden()
    {
        await using var host = await StartAsync();
        var credentials = await PutAsync(await ServiceClientAsync(host), "STORE-B");
        var storeClient = host.CreateClient();
        storeClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await TokenHosts.AccessTokenAsync(host, credentials.ClientId, credentials.ClientSecret));

        var response = await storeClient.PutAsJsonAsync($"/store-clients/{StoreId}", new { displayName = "Store A" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ServiceToken_IsForAuthNotTheCloudApi()
    {
        await using var host = await StartAsync();

        var token = new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(
            await TokenHosts.AccessTokenAsync(host, "cloud-api", AuthTestHost.ServiceClientSecret, AuthScopes.StoreClients));

        token.Audiences.Should().Equal(AuthResources.Auth);
    }

    [Fact]
    public async Task Startup_WithoutTheServiceClientSecret_Fails()
    {
        await using var host = TokenHostsWithout.ServiceSecret(await postgres.CreateDatabaseAsync());

        var start = () => host.CreateClient();

        start.Should().Throw<InvalidOperationException>().WithMessage("*ServiceClients:CloudApi:ClientSecret*");
    }
}

internal static class TokenHostsWithout
{
    public static AuthTestHost ServiceSecret(string connectionString) =>
        new(connectionString, new Dictionary<string, string?>
        {
            ["OpenIddict:TlsTerminatedUpstream"] = "true",
            ["ServiceClients:CloudApi:ClientSecret"] = ""
        });
}
