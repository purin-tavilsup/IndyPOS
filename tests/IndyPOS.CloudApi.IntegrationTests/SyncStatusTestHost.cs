using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using IndyPOS.CloudApi.Endpoints;
using IndyPOS.CloudApi.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IndyPOS.CloudApi.IntegrationTests;

/// <summary>
/// The real /sync/status route on a TestServer. A stub scheme stands in for OpenIddict: it turns
/// "Bearer {storeId}" into the store_id claim a store token carries. Booting the real Program would need
/// an OpenIddict certificate, a registered client and an HTTPS token exchange to prove nothing more
/// about this route.
/// </summary>
internal sealed class SyncStatusTestHost : IAsyncDisposable
{
    /// <summary>A bearer value the stub authenticates without a store_id claim.</summary>
    public const string WithoutAStore = "no-store-claim";

    private readonly WebApplication _app;

    private SyncStatusTestHost(WebApplication app)
    {
        _app = app;
        Client = app.GetTestClient();
    }

    public HttpClient Client { get; }

    public static async Task<SyncStatusTestHost> StartAsync(string connectionString)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddDbContext<CloudDbContext>(options => CloudPostgresFixture.Configure(options, connectionString));
        builder.Services.AddAuthentication(StoreTokenStubHandler.SchemeName)
                        .AddScheme<AuthenticationSchemeOptions, StoreTokenStubHandler>(StoreTokenStubHandler.SchemeName, configureOptions: null);
        builder.Services.AddAuthorization();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapSyncStatus();
        await app.StartAsync();

        return new SyncStatusTestHost(app);
    }

    public async Task<HttpResponseMessage> GetAsStoreAsync(string storeId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/sync/status");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", storeId);

        return await Client.SendAsync(request);
    }

    public async Task<SyncStatusBody> GetStatusAsync(string storeId)
    {
        var response = await GetAsStoreAsync(storeId);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<SyncStatusBody>())!;
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.DisposeAsync();
    }
}

internal sealed record SyncStatusBody(int TotalEvents, int UnprocessedEvents, int ProcessedEvents, int TotalInvoices);

internal sealed class StoreTokenStubHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "StoreTokenStub";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var header) || header.Parameter is null)
            return Task.FromResult(AuthenticateResult.NoResult());

        Claim[] claims = header.Parameter == SyncStatusTestHost.WithoutAStore
            ? []
            : [new Claim("store_id", header.Parameter)];
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
