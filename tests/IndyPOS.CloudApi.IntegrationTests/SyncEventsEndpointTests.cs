using System.Net;
using System.Text.Json;
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

    [Fact]
    public async Task SyncEvents_WithoutAnEventsList_ReturnsAMarkedRefusal()
    {
        await using var host = await SyncTestHost.StartAsync(await postgres.CreateDatabaseAsync());

        var response = await host.PostEventsAsStoreAsync(ThisStore, "{}");

        (await RefusalOf(response)).Should()
                                   .Be("malformed-batch");
    }

    [Fact]
    public async Task SyncEvents_ForAnotherStore_ReturnsAMarkedRefusal()
    {
        await using var host = await SyncTestHost.StartAsync(await postgres.CreateDatabaseAsync());

        var response = await host.PostEventsAsStoreAsync(ThisStore, BatchWithPayload("""{"StoreId":"store-b"}"""));

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Forbidden);
        (await RefusalOf(response)).Should()
                                   .Be("store-mismatch");
    }

    [Fact]
    public async Task SyncEvents_WithAPayloadWithoutAStoreId_ReturnsAMarkedRefusal()
    {
        await using var host = await SyncTestHost.StartAsync(await postgres.CreateDatabaseAsync());

        var response = await host.PostEventsAsStoreAsync(ThisStore, BatchWithPayload("{}"));

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Forbidden);
        (await RefusalOf(response)).Should()
                                   .Be("store-mismatch");
    }

    private static string BatchWithPayload(string payload) =>
        JsonSerializer.Serialize(new
        {
            events = new[]
            {
                new { eventId = Guid.NewGuid(), storeId = 0, eventType = "InvoiceCompleted", payload, createdAtUtc = DateTime.UtcNow }
            }
        });

    /// <summary>The problem's syncRefusal, or null when the answer is not a problem or carries none.</summary>
    private static async Task<string?> RefusalOf(HttpResponseMessage response)
    {
        if (response.Content.Headers.ContentType?.MediaType != "application/problem+json")
            return null;

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return body.RootElement.TryGetProperty("syncRefusal", out var refusal) ? refusal.GetString() : null;
    }
}
