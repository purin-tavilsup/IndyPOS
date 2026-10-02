using System.Net;
using System.Text.Json;
using FluentAssertions;
using IndyPOS.Application.Abstractions.Cloud.Repositories;
using IndyPOS.CloudApi.Domain;
using Xunit;

namespace IndyPOS.CloudApi.IntegrationTests;

/// <summary>
/// /sync/status was open to anyone and counted every store. It now needs the store token that
/// /sync/events takes, and counts only that token's store, using the SourceStoreId stamped at ingest.
/// Each test gets a fresh database.
/// </summary>
public class SyncStatusEndpointTests(CloudPostgresFixture postgres) : IClassFixture<CloudPostgresFixture>
{
    private const string ThisStore = "store-a";
    private const string OtherStore = "store-b";

    // What HttpCloudSyncClient.ParseStoreId sends for every real (string) store id.
    private const int EnvelopeStoreIdOfEveryStore = 0;

    // An event ingested before SourceStoreId existed.
    private const string? NoSourceStore = null;

    [Fact]
    public async Task SyncStatus_WithoutAToken_ReturnsUnauthorized()
    {
        await using var host = await SyncStatusTestHost.StartAsync(await postgres.CreateDatabaseAsync());

        var response = await host.Client.GetAsync("/sync/status");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SyncStatus_WithATokenWithoutAStore_ReturnsForbidden()
    {
        await using var host = await SyncStatusTestHost.StartAsync(await postgres.CreateDatabaseAsync());

        var response = await host.GetAsStoreAsync(SyncStatusTestHost.WithoutAStore);

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SyncStatus_WithAnotherStoresInboxRow_DoesNotCountIt()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        await AddInboxRowAsync(connectionString, OtherStore, payloadStoreId: OtherStore);
        await using var host = await SyncStatusTestHost.StartAsync(connectionString);

        var status = await host.GetStatusAsync(ThisStore);

        status.TotalEvents.Should()
                          .Be(0);
    }

    // A row ingested before this release has no SourceStoreId. Even when its payload names this store,
    // it is counted for no store: the stamp, not the payload, decides.
    [Fact]
    public async Task SyncStatus_WithALegacyInboxRowWithoutASourceStore_DoesNotCountIt()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        await AddInboxRowAsync(connectionString, NoSourceStore, payloadStoreId: ThisStore);
        await using var host = await SyncStatusTestHost.StartAsync(connectionString);

        var status = await host.GetStatusAsync(ThisStore);

        status.TotalEvents.Should()
                          .Be(0);
    }

    [Fact]
    public async Task SyncStatus_WithAStoreToken_CountsOnlyThatStore()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        await SeedInboxAsync(connectionString, ThisStore, events: 2, processed: 1);
        await SeedInvoicesAsync(connectionString, ThisStore, count: 1);
        await SeedInboxAsync(connectionString, OtherStore, events: 3, processed: 1);
        await SeedInvoicesAsync(connectionString, OtherStore, count: 2);
        await using var host = await SyncStatusTestHost.StartAsync(connectionString);

        var status = await host.GetStatusAsync(ThisStore);

        status.Should()
              .BeEquivalentTo(new SyncStatusBody(TotalEvents: 2, UnprocessedEvents: 1, ProcessedEvents: 1, TotalInvoices: 1));
    }

    // The first `processed` events are marked processed in the inbox and recorded in ProcessedEvents.
    private static async Task SeedInboxAsync(string connectionString, string storeId, int events, int processed)
    {
        await using var db = CloudPostgresFixture.CreateContext(connectionString);

        for (var i = 0; i < events; i++)
        {
            var isProcessed = i < processed;
            var row = InboxRow(storeId, storeId, isProcessed ? DateTime.UtcNow : null);
            db.SyncedEvents.Add(row);

            if (isProcessed)
                db.ProcessedEvents.Add(new ProcessedEvent { EventId = row.EventId, EventType = row.EventType, StoreId = storeId, ProcessedAtUtc = DateTime.UtcNow });
        }

        await db.SaveChangesAsync();
    }

    private static async Task AddInboxRowAsync(string connectionString, string? sourceStoreId, string payloadStoreId)
    {
        await using var db = CloudPostgresFixture.CreateContext(connectionString);
        db.SyncedEvents.Add(InboxRow(sourceStoreId, payloadStoreId, processedAtUtc: null));
        await db.SaveChangesAsync();
    }

    private static async Task SeedInvoicesAsync(string connectionString, string storeId, int count)
    {
        await using var db = CloudPostgresFixture.CreateContext(connectionString);

        for (var i = 0; i < count; i++)
            db.Invoices.Add(new CloudInvoice { Id = Guid.NewGuid(), StoreId = storeId, UserId = Guid.NewGuid(), TotalAmount = 100m, CreatedAtUtc = DateTime.UtcNow, SyncedAtUtc = DateTime.UtcNow });

        await db.SaveChangesAsync();
    }

    private static SyncedEventEntity InboxRow(string? sourceStoreId, string payloadStoreId, DateTime? processedAtUtc) => new()
    {
        EventId = Guid.NewGuid(),
        StoreId = EnvelopeStoreIdOfEveryStore,
        SourceStoreId = sourceStoreId,
        EventType = "InvoiceCompleted",
        Payload = JsonSerializer.Serialize(new { StoreId = payloadStoreId }),
        CreatedAtUtc = DateTime.UtcNow,
        ReceivedAtUtc = DateTime.UtcNow,
        ProcessedAtUtc = processedAtUtc
    };
}
