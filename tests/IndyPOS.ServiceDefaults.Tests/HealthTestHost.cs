using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

namespace IndyPOS.ServiceDefaults.Tests;

/// <summary>
/// The smallest app that wires health the way StoreHub and CloudApi do, so the shared behaviour is
/// tested once, without either service's own startup.
/// </summary>
internal sealed class HealthTestHost : IAsyncDisposable
{
    public const string ConnectionName = "test-db";

    private readonly WebApplication _app;

    private HealthTestHost(WebApplication app)
    {
        _app = app;
        Client = app.GetTestClient();
    }

    public HttpClient Client { get; }

    public static async Task<HealthTestHost> StartAsync(string connectionString, string environment = "Production")
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.WebHost.UseTestServer();
        builder.Configuration[$"ConnectionStrings:{ConnectionName}"] = connectionString;
        builder.AddServiceDefaults();
        builder.AddDatabaseReadinessCheck(ConnectionName);

        var app = builder.Build();
        app.UseRequestTimeouts();
        app.MapDefaultEndpoints();
        await app.StartAsync();

        return new HealthTestHost(app);
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.DisposeAsync();
    }
}
