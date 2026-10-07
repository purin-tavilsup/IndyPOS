using System.Net;
using FluentAssertions;
using IndyPOS.ServiceDefaults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

[Collection("Integration")]
public class HealthEndpointTests : IntegrationTestBase
{
    private const string SelfCheck = "self";

    public HealthEndpointTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    // Aspire's EF integration would add a second, untagged database check; it must be switched off.
    [Fact]
    public void HealthChecks_WithTheAppBuilt_RegisterOneDatabaseCheck()
    {
        var names = Factory.Services.GetRequiredService<IOptions<HealthCheckServiceOptions>>()
                                    .Value.Registrations
                                    .Select(r => r.Name);

        names.Should()
             .BeEquivalentTo([SelfCheck, DatabaseReadinessCheck.Name]);
    }

    [Fact]
    public void RouteTable_WithTheAppBuilt_MapsTheReadyProbeOnce()
    {
        var readyRoutes = Factory.Services.GetRequiredService<EndpointDataSource>()
                                          .Endpoints
                                          .OfType<RouteEndpoint>()
                                          .Count(e => e.RoutePattern.RawText == "/health/ready");

        readyRoutes.Should()
                   .Be(1);
    }

    [Fact]
    public async Task ReadyProbe_WithTheDatabaseUp_ReturnsOk()
    {
        var response = await Client.GetAsync("/health/ready");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.OK);
    }
}
