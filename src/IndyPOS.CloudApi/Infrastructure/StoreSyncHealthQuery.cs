using System.Text.Json;
using IndyPOS.Application.UseCases.Cloud.Sync.Heartbeat;
using IndyPOS.CloudApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.CloudApi.Infrastructure;

internal sealed record RejectionView(Guid? EventId, string? EventType, int StatusCode, string Reason, int Count, DateTime LastRejectedAtUtc);

internal sealed record StoreSyncHealthView(
    string StoreId,
    string? StoreName,
    StoreSyncState State,
    DateTime? LastSeenUtc,
    string? StoreHubVersion,
    int PendingCount,
    int FailedCount,
    DateTime? OldestPendingCreatedAtUtc,
    IReadOnlyList<FailedEventSummary> FailedEvents,
    IReadOnlyList<RejectionView> UnresolvedRejections,
    int CloudUnprocessedEvents,
    Guid? OpenResendRequestId);

/// <summary>
/// Every registered store's sync health for the admin page: its latest heartbeat, the refusals not yet resolved,
/// the cloud's own unprocessed count and any open resend request. A store never seen is listed too, offline.
/// </summary>
internal static class StoreSyncHealthQuery
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    public static async Task<IReadOnlyList<StoreSyncHealthView>> ListAsync(
        CloudDbContext db, StoreSyncHealthOptions options, DateTime nowUtc, CancellationToken ct)
    {
        var stores = await db.StoreConfigs.AsNoTracking().OrderBy(s => s.StoreId).ToListAsync(ct);
        var views = new List<StoreSyncHealthView>(stores.Count);

        foreach (var store in stores)
            views.Add(await ViewAsync(db, store, options, nowUtc, ct));

        return views;
    }

    private static async Task<StoreSyncHealthView> ViewAsync(
        CloudDbContext db, CloudStoreConfig store, StoreSyncHealthOptions options, DateTime nowUtc, CancellationToken ct)
    {
        var heartbeat = await db.StoreHeartbeats.AsNoTracking().SingleOrDefaultAsync(h => h.StoreId == store.StoreId, ct);
        var rejections = await db.SyncRejections.AsNoTracking()
                                                .Where(r => r.StoreId == store.StoreId && r.ResolvedAtUtc == null)
                                                .OrderByDescending(r => r.LastRejectedAtUtc)
                                                .Select(r => new RejectionView(r.EventId, r.EventType, r.StatusCode, r.Reason, r.Count, r.LastRejectedAtUtc))
                                                .ToListAsync(ct);
        var openResend = await db.SyncResendRequests.Where(r => r.StoreId == store.StoreId && r.CompletedAtUtc == null)
                                                    .OrderBy(r => r.RequestedAtUtc)
                                                    .Select(r => (Guid?)r.Id)
                                                    .FirstOrDefaultAsync(ct);
        var cloud = await StoreSyncStatusQuery.CountAsync(db, store.StoreId, ct);

        var state = StoreSyncHealth.Evaluate(heartbeat?.ReceivedAtUtc, heartbeat?.FailedCount ?? 0, heartbeat?.OldestPendingCreatedAtUtc,
            rejections.FirstOrDefault()?.LastRejectedAtUtc, nowUtc, options);

        return new StoreSyncHealthView(store.StoreId, store.StoreName, state, heartbeat?.ReceivedAtUtc, heartbeat?.StoreHubVersion,
            heartbeat?.PendingCount ?? 0, heartbeat?.FailedCount ?? 0, heartbeat?.OldestPendingCreatedAtUtc,
            FailedEventsOf(heartbeat), rejections, cloud.UnprocessedEvents, openResend);
    }

    private static IReadOnlyList<FailedEventSummary> FailedEventsOf(CloudStoreHeartbeat? heartbeat) =>
        heartbeat?.FailedEventsJson is { } json
            ? JsonSerializer.Deserialize<List<FailedEventSummary>>(json, Web) ?? []
            : [];
}

/// <summary>An admin's resend requests. A second press before the store picked the first up returns the open one.</summary>
internal static class ResendRequests
{
    /// <returns>The request and whether it is new, or null when the store is not registered.</returns>
    public static async Task<(Guid RequestId, bool Created)?> RequestAsync(
        CloudDbContext db, string storeId, Guid? eventId, DateTime nowUtc, CancellationToken ct)
    {
        if (!await db.StoreConfigs.AnyAsync(s => s.StoreId == storeId, ct))
            return null;

        var open = await db.SyncResendRequests.Where(r => r.StoreId == storeId && r.EventId == eventId && r.CompletedAtUtc == null)
                                              .Select(r => (Guid?)r.Id)
                                              .FirstOrDefaultAsync(ct);
        if (open is { } existing)
            return (existing, false);

        var request = new CloudResendRequest { Id = Guid.NewGuid(), StoreId = storeId, EventId = eventId, RequestedAtUtc = nowUtc };
        db.SyncResendRequests.Add(request);
        await db.SaveChangesAsync(ct);

        return (request.Id, true);
    }
}
