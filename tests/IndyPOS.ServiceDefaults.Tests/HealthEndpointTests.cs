using System.Diagnostics;
using System.Net;
using System.Text.Json;
using FluentAssertions;
using IndyPOS.Testing.Postgres;
using Xunit;

namespace IndyPOS.ServiceDefaults.Tests;

public class HealthEndpointTests : IAsyncLifetime
{
    private const string Ready = "/health/ready";
    private const string Live = "/health/live";
    private const string Detailed = "/health";
    private static readonly TimeSpan ProbeBudget = TimeSpan.FromSeconds(5);

    private TestPostgres _postgres = null!;

    public async Task InitializeAsync() => _postgres = await TestPostgres.StartAsync();

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task ReadyProbe_WithAnUnreachableDatabase_ReturnsServiceUnavailable()
    {
        await using var host = await HealthTestHost.StartAsync(HangingServer.ClosedPortConnectionString());

        var response = await host.Client.GetAsync(Ready);

        response.StatusCode.Should()
                           .Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task ReadyProbe_WithAHangingDatabase_AnswersWithinFiveSeconds()
    {
        using var server = new HangingServer();
        await using var host = await HealthTestHost.StartAsync(server.ConnectionString());
        var clock = Stopwatch.StartNew();

        var response = await host.Client.GetAsync(Ready);

        clock.Elapsed.Should()
                     .BeLessThan(ProbeBudget, "Npgsql's default 15 s connect timeout must not reach the caller");
        response.StatusCode.Should()
                           .Be(HttpStatusCode.ServiceUnavailable);
    }

    // An installer-written string may carry its own, longer Timeout; the check must still be bounded.
    [Fact]
    public async Task ReadyProbe_WithAConnectionStringThatSetsALongerTimeout_StillAnswersWithinFiveSeconds()
    {
        using var server = new HangingServer();
        await using var host = await HealthTestHost.StartAsync(server.ConnectionString("Timeout=30"));
        var clock = Stopwatch.StartNew();

        await host.Client.GetAsync(Ready);

        clock.Elapsed.Should()
                     .BeLessThan(ProbeBudget);
    }

    [Fact]
    public async Task ReadyProbe_WithAFailingDatabase_DoesNotLeakTheError()
    {
        await using var host = await HealthTestHost.StartAsync(HangingServer.ClosedPortConnectionString());

        var body = await (await host.Client.GetAsync(Ready)).Content.ReadAsStringAsync();

        body.Should()
            .Be("Unhealthy");
    }

    [Fact]
    public async Task LiveProbe_WithAnUnreachableDatabase_ReturnsOk()
    {
        await using var host = await HealthTestHost.StartAsync(HangingServer.ClosedPortConnectionString());

        var response = await host.Client.GetAsync(Live);

        response.StatusCode.Should()
                           .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DetailedHealth_OutsideDevelopment_ReturnsNotFound()
    {
        await using var host = await HealthTestHost.StartAsync(_postgres.ConnectionString);

        var response = await host.Client.GetAsync(Detailed);

        response.StatusCode.Should()
                           .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReadyProbe_WithTheDatabaseUp_ReturnsOk()
    {
        await using var host = await HealthTestHost.StartAsync(_postgres.ConnectionString);

        var response = await host.Client.GetAsync(Ready);

        response.StatusCode.Should()
                           .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DetailedHealth_InDevelopment_ReportsTheDatabaseCheck()
    {
        await using var host = await HealthTestHost.StartAsync(_postgres.ConnectionString, "Development");

        using var json = JsonDocument.Parse(await host.Client.GetStringAsync(Detailed));

        json.RootElement.GetProperty("entries")
                        .TryGetProperty(DatabaseReadinessCheck.Name, out _)
                        .Should()
                        .BeTrue();
    }
}
