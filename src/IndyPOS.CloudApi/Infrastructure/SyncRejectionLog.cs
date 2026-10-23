using IndyPOS.Application.Common.Extensions;
using IndyPOS.Application.UseCases.Cloud.Sync;
using IndyPOS.CloudApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.CloudApi.Infrastructure;

/// <summary>
/// The cloud's own record of the refusals it gave on /sync/events, one row per store and event. It shows a refused
/// event on the admin page even when the store's heartbeat cannot get through, and resolves once that event is
/// accepted. The store comes from the token, never the payload.
/// </summary>
internal static class SyncRejectionLog
{
    private const int ReasonMaxLength = 1000;

    public static async Task RecordAsync(
        CloudDbContext db, string storeId, IReadOnlyList<SyncEventRequest?>? events, int statusCode, string reason,
        DateTime nowUtc, CancellationToken ct)
    {
        // One row per event: a batch naming the same event twice must not add two rows the unique index refuses.
        var refused = events?.Where(e => e is not null).Select(e => e!).DistinctBy(e => e.EventId).ToList() ?? [];

        if (refused.Count == 0)
            db.SyncRejections.Add(NewRejection(storeId, eventId: null, eventType: null, statusCode, reason, nowUtc));

        foreach (var syncEvent in refused)
            await UpsertAsync(db, storeId, syncEvent, statusCode, reason, nowUtc, ct);

        await db.SaveChangesAsync(ct);
    }

    public static async Task ResolveAsync(CloudDbContext db, string storeId, IReadOnlyCollection<Guid> acceptedEventIds, DateTime nowUtc, CancellationToken ct)
    {
        if (acceptedEventIds.Count == 0)
            return;

        await db.SyncRejections.Where(r => r.StoreId == storeId && r.ResolvedAtUtc == null && r.EventId != null && acceptedEventIds.Contains(r.EventId.Value))
                               .ExecuteUpdateAsync(s => s.SetProperty(r => r.ResolvedAtUtc, nowUtc), ct);
    }

    private static async Task UpsertAsync(
        CloudDbContext db, string storeId, SyncEventRequest syncEvent, int statusCode, string reason, DateTime nowUtc, CancellationToken ct)
    {
        var existing = await db.SyncRejections.SingleOrDefaultAsync(r => r.StoreId == storeId && r.EventId == syncEvent.EventId, ct);

        if (existing is null)
        {
            db.SyncRejections.Add(NewRejection(storeId, syncEvent.EventId, syncEvent.EventType, statusCode, reason, nowUtc));
            return;
        }

        existing.Count++;
        existing.StatusCode = statusCode;
        existing.Reason = reason.CutTo(ReasonMaxLength)!;
        existing.LastRejectedAtUtc = nowUtc;
        existing.ResolvedAtUtc = null;
    }

    private static CloudSyncRejection NewRejection(
        string storeId, Guid? eventId, string? eventType, int statusCode, string reason, DateTime nowUtc) =>
        new()
        {
            Id = Guid.NewGuid(), StoreId = storeId, EventId = eventId, EventType = eventType.CutTo(100),
            StatusCode = statusCode, Reason = reason.CutTo(ReasonMaxLength)!,
            FirstRejectedAtUtc = nowUtc, LastRejectedAtUtc = nowUtc, Count = 1
        };
}
