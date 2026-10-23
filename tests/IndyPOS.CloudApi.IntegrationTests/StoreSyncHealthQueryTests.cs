using FluentAssertions;
using IndyPOS.Application.UseCases.Cloud.Sync.Heartbeat;
using IndyPOS.CloudApi.Domain;
using IndyPOS.CloudApi.Infrastructure;
using Xunit;

namespace IndyPOS.CloudApi.IntegrationTests;

/// <summary>
/// What the admin page lists: every registered store, its state, and what it would need to resend. A store that was
/// registered but never seen is listed, offline, rather than missing. Each test gets a fresh database.
/// </summary>
public class StoreSyncHealthQueryTests(CloudPostgresFixture postgres) : IClassFixture<CloudPostgresFixture>
{
    private const string ThisStore = "store-a";
    private static readonly DateTime Now = new(2026, 10, 23, 12, 0, 0, DateTimeKind.Utc);

    private async Task<CloudDbContext> RegisteredStoreAsync()
    {
        var db = CloudPostgresFixture.CreateContext(await postgres.CreateDatabaseAsync());
        db.StoreConfigs.Add(new CloudStoreConfig { StoreId = ThisStore, StoreName = "Store A", StoreFullName = "Store A", LastModifiedAtUtc = Now });
        await db.SaveChangesAsync();
        return db;
    }

    private static async Task<StoreSyncHealthView> OnlyStoreAsync(CloudDbContext db) =>
        (await StoreSyncHealthQuery.ListAsync(db, new StoreSyncHealthOptions(), Now, CancellationToken.None)).Single();

    private static CloudSyncRejection Rejection(DateTime at, DateTime? resolvedAt = null) => new()
    {
        Id = Guid.NewGuid(), StoreId = ThisStore, EventId = Guid.NewGuid(), EventType = "InvoiceCompleted", StatusCode = 403,
        Reason = "store-mismatch", FirstRejectedAtUtc = at, LastRejectedAtUtc = at, Count = 1, ResolvedAtUtc = resolvedAt
    };

    [Fact]
    public async Task ListAsync_WithARegisteredStoreNeverSeen_ListsItOffline()
    {
        await using var db = await RegisteredStoreAsync();

        var store = await OnlyStoreAsync(db);

        store.State.Should().Be(StoreSyncState.Offline);
        store.LastSeenUtc.Should().BeNull();
    }

    [Fact]
    public async Task ListAsync_WithARecentHeartbeat_ReportsItsCountsAndState()
    {
        await using var db = await RegisteredStoreAsync();
        db.StoreHeartbeats.Add(new CloudStoreHeartbeat { StoreId = ThisStore, ReceivedAtUtc = Now.AddMinutes(-1), PendingCount = 2, StoreHubVersion = "4.1.0" });
        await db.SaveChangesAsync();

        var store = await OnlyStoreAsync(db);

        store.State.Should().Be(StoreSyncState.Ok);
        store.PendingCount.Should().Be(2);
        store.StoreHubVersion.Should().Be("4.1.0");
    }

    [Fact]
    public async Task ListAsync_WithAnUnresolvedRejectionNewerThanTheHeartbeat_ReportsFailedEvents()
    {
        await using var db = await RegisteredStoreAsync();
        db.StoreHeartbeats.Add(new CloudStoreHeartbeat { StoreId = ThisStore, ReceivedAtUtc = Now.AddMinutes(-3) });
        db.SyncRejections.Add(Rejection(Now.AddMinutes(-1)));
        await db.SaveChangesAsync();

        var store = await OnlyStoreAsync(db);

        store.State.Should().Be(StoreSyncState.FailedEvents);
        store.UnresolvedRejections.Should().ContainSingle();
    }

    [Fact]
    public async Task ListAsync_WithOnlyResolvedRejections_DoesNotReportThem()
    {
        await using var db = await RegisteredStoreAsync();
        db.StoreHeartbeats.Add(new CloudStoreHeartbeat { StoreId = ThisStore, ReceivedAtUtc = Now.AddMinutes(-3) });
        db.SyncRejections.Add(Rejection(Now.AddMinutes(-1), resolvedAt: Now));
        await db.SaveChangesAsync();

        var store = await OnlyStoreAsync(db);

        store.State.Should().Be(StoreSyncState.Ok);
        store.UnresolvedRejections.Should().BeEmpty();
    }

    [Fact]
    public async Task ListAsync_WithAnOpenResendRequest_ReportsItsId()
    {
        await using var db = await RegisteredStoreAsync();
        var (requestId, _) = (await ResendRequests.RequestAsync(db, ThisStore, eventId: null, Now, CancellationToken.None))!.Value;

        (await OnlyStoreAsync(db)).OpenResendRequestId.Should().Be(requestId);
    }
}
