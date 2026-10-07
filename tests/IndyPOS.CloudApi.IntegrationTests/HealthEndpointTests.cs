using FluentAssertions;
using IndyPOS.ServiceDefaults;
using IndyPOS.Testing.Postgres;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Xunit;

namespace IndyPOS.CloudApi.IntegrationTests;

/// <summary>
/// CloudApi's health wiring on its real Program, in Development: OpenIddict and the production-safety
/// check need no certificates there, and the migrations run on this fixture's own database.
/// </summary>
public class HealthEndpointTests : IAsyncLifetime
{
    private const string SelfCheck = "self";

    private TestPostgres _postgres = null!;
    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        _postgres = await TestPostgres.StartAsync();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:cloud-db", _postgres.ConnectionString);
        });
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    // I0-C: MapDefaultEndpoints and a hand-rolled MapGet both mapped this route.
    [Fact]
    public void RouteTable_WithTheAppBuilt_MapsTheReadyProbeOnce()
    {
        var readyRoutes = _factory.Services.GetRequiredService<EndpointDataSource>()
                                           .Endpoints
                                           .OfType<RouteEndpoint>()
                                           .Count(e => e.RoutePattern.RawText == "/health/ready");

        readyRoutes.Should()
                   .Be(1);
    }

    [Fact]
    public void HealthChecks_WithTheAppBuilt_RegisterOneDatabaseCheck()
    {
        var names = _factory.Services.GetRequiredService<IOptions<HealthCheckServiceOptions>>()
                                     .Value.Registrations
                                     .Select(r => r.Name);

        names.Should()
             .BeEquivalentTo([SelfCheck, DatabaseReadinessCheck.Name]);
    }
}
