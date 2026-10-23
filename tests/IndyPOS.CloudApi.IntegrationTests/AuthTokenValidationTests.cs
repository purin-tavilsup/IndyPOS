using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using IndyPOS.Application.Common.Constants;
using IndyPOS.Cloud.Auth;
using IndyPOS.Cloud.Auth.Data;
using IndyPOS.Cloud.Auth.StoreClients;
using IndyPOS.CloudApi.Endpoints;
using IndyPOS.CloudApi.Infrastructure;
using IndyPOS.CloudApi.Infrastructure.Auth;
using IndyPOS.Testing.Postgres;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using OpenIddict.Validation.AspNetCore;
using Xunit;

namespace IndyPOS.CloudApi.IntegrationTests;

/// <summary>
/// CloudApi issues no token any more; it accepts only Cloud.Auth's, for its own audience. A real Auth host issues the
/// tokens, and CloudApi discovers its keys over Auth's TestServer, the way it does over https in production.
/// </summary>
public sealed class AuthTokenValidationTests : IAsyncLifetime
{
    private const string Issuer = "https://auth.test/";
    private const string StoreId = "STORE-A";
    private const string ServiceSecret = "cloud-api-test-secret";

    private TestPostgres _postgres = null!;
    private AuthHost _auth = null!;
    private WebApplication _cloudApi = null!;
    private StoreClientCredentials _store = null!;

    public async Task InitializeAsync()
    {
        _postgres = await TestPostgres.StartAsync();
        _auth = await AuthHost.StartAsync(_postgres, Issuer);
        _store = await _auth.CreateStoreClientAsync(StoreId);
        _cloudApi = await StartCloudApiAsync(await CloudDatabaseAsync(), _auth.Server.CreateHandler());
    }

    public async Task DisposeAsync()
    {
        await _cloudApi.DisposeAsync();
        await _auth.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task SyncStatus_WithAStoreTokenFromAuth_ReturnsOk()
    {
        var token = await _auth.TokenAsync(_store.ClientId, _store.ClientSecret, $"{AuthScopes.SyncWrite} {AuthScopes.MasterRead}");

        (await GetSyncStatusAsync(token)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // CloudApi's own service token is for Auth's store-client API: CloudApi must not accept it as a store.
    [Fact]
    public async Task SyncStatus_WithATokenForAnotherAudience_ReturnsUnauthorized()
    {
        var token = await _auth.TokenAsync("cloud-api", ServiceSecret, AuthScopes.StoreClients);

        (await GetSyncStatusAsync(token)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SyncStatus_WithATokenFromAnotherIssuer_ReturnsUnauthorized()
    {
        await using var other = await AuthHost.StartAsync(_postgres, "https://other.test/");
        var store = await other.CreateStoreClientAsync(StoreId);
        var token = await other.TokenAsync(store.ClientId, store.ClientSecret, AuthScopes.SyncWrite);

        (await GetSyncStatusAsync(token)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SyncStatus_WithoutAToken_ReturnsUnauthorized() =>
        (await GetSyncStatusAsync(token: null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

    [Fact]
    public async Task OAuthToken_OnCloudApi_IsGone()
    {
        await using var cloudApi = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:cloud-db", _postgres.ConnectionString);
        });

        var response = await cloudApi.CreateClient().PostAsync("/oauth/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials", ["client_id"] = _store.ClientId, ["client_secret"] = _store.ClientSecret
        }));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<HttpResponseMessage> GetSyncStatusAsync(string? token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/sync/status");
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await _cloudApi.GetTestClient().SendAsync(request);
    }

    private async Task<string> CloudDatabaseAsync()
    {
        var connectionString = await _postgres.CreateDatabaseAsync();
        await using var db = CloudPostgresFixture.CreateContext(connectionString);
        await db.Database.MigrateAsync();
        return connectionString;
    }

    /// <summary>CloudApi's real token validation on a slim host; every outgoing HTTP call goes to Auth's TestServer.</summary>
    private static async Task<WebApplication> StartCloudApiAsync(string connectionString, HttpMessageHandler toAuth)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration["Auth:Issuer"] = Issuer;
        builder.Services.AddDbContext<CloudDbContext>(options => CloudPostgresFixture.Configure(options, connectionString));
        builder.Services.AddAuthTokenValidation(builder.Configuration, builder.Environment);
        builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<IHttpMessageHandlerBuilderFilter>(new RouteEverythingTo(toAuth));

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapSyncStatus();
        await app.StartAsync();
        return app;
    }

    private sealed class RouteEverythingTo(HttpMessageHandler handler) : IHttpMessageHandlerBuilderFilter
    {
        public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next) => builder =>
        {
            next(builder);
            builder.PrimaryHandler = handler;
        };
    }

    /// <summary>A real Cloud.Auth on a TestServer with its own database and issuer.</summary>
    private sealed class AuthHost : WebApplicationFactory<AuthApp>
    {
        private string _connectionString = null!;
        private string _issuer = null!;

        public static async Task<AuthHost> StartAsync(TestPostgres postgres, string issuer)
        {
            var connectionString = await postgres.CreateDatabaseAsync();
            await using (var db = new AuthDbContext(new DbContextOptionsBuilder<AuthDbContext>().UseNpgsql(connectionString).Options))
                await db.Database.MigrateAsync();

            var host = new AuthHost { _connectionString = connectionString, _issuer = issuer };
            host.CreateClient();
            return host;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:auth-db", _connectionString);
            builder.UseSetting("ServiceClients:CloudApi:ClientSecret", ServiceSecret);
            builder.UseSetting("OpenIddict:Issuer", _issuer);
            builder.UseSetting("OpenIddict:TlsTerminatedUpstream", "true");
        }

        public async Task<StoreClientCredentials> CreateStoreClientAsync(string storeId)
        {
            await using var scope = Services.CreateAsyncScope();
            return (await scope.ServiceProvider.GetRequiredService<StoreClientService>().CreateOrReplaceAsync(storeId, storeId, CancellationToken.None))!;
        }

        public async Task<string> TokenAsync(string clientId, string secret, string scope)
        {
            var response = await CreateClient().PostAsync("/oauth/token", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials", ["client_id"] = clientId, ["client_secret"] = secret, ["scope"] = scope
            }));
            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return document.RootElement.GetProperty("access_token").GetString()!;
        }
    }
}
