using System.Net;
using FluentAssertions;
using Xunit;

namespace IndyPOS.CloudApi.IntegrationTests;

/// <summary>
/// A batch without its events list, or with a null event in it, answered 500: JSON binding leaves
/// them null despite the non-nullable types and the handler dereferenced them. It is the store's
/// malformed request, so it is a 400. Each test gets a fresh database.
/// </summary>
public class SyncEventsEndpointTests(CloudPostgresFixture postgres) : IClassFixture<CloudPostgresFixture>
{
    private const string ThisStore = "store-a";

    [Fact]
    public async Task SyncEvents_WithoutAnEventsList_ReturnsBadRequest()
    {
        await using var host = await SyncTestHost.StartAsync(await postgres.CreateDatabaseAsync());

        var response = await host.PostEventsAsStoreAsync(ThisStore, "{}");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SyncEvents_WithANullEvent_ReturnsBadRequest()
    {
        await using var host = await SyncTestHost.StartAsync(await postgres.CreateDatabaseAsync());

        var response = await host.PostEventsAsStoreAsync(ThisStore, """{"events":[null]}""");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SyncEvents_WithAnEmptyEventsList_ReturnsOk()
    {
        await using var host = await SyncTestHost.StartAsync(await postgres.CreateDatabaseAsync());

        var response = await host.PostEventsAsStoreAsync(ThisStore, """{"events":[]}""");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.OK);
    }
}
