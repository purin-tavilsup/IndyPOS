using System.Text.Json;
using IndyPOS.Application.UseCases.Cloud.Sync.Heartbeat;
using IndyPOS.CloudApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.CloudApi.Infrastructure;

/// <summary>
/// Records a store's heartbeat (one row per store, overwritten), closes the resend requests it reports done, and
/// answers with the ones still open. The store comes from its token, so it can only touch its own rows.
/// </summary>
internal static class HeartbeatRecorder
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    public static async Task<HeartbeatResponse> RecordAsync(
        CloudDbContext db, string storeId, HeartbeatRequest request, DateTime nowUtc, CancellationToken ct)
    {
        await UpsertHeartbeatAsync(db, storeId, request, nowUtc, ct);
        await CompleteResendsAsync(db, storeId, request.CompletedResends, nowUtc, ct);
        await db.SaveChangesAsync(ct);

        var open = await db.SyncResendRequests.Where(r => r.StoreId == storeId && r.CompletedAtUtc == null)
                                              .OrderBy(r => r.RequestedAtUtc)
                                              .Select(r => new ResendInstruction(r.Id, r.EventId))
                                              .ToListAsync(ct);

        return new HeartbeatResponse(open);
    }

    private static async Task UpsertHeartbeatAsync(
        CloudDbContext db, string storeId, HeartbeatRequest request, DateTime nowUtc, CancellationToken ct)
    {
        var row = await db.StoreHeartbeats.FindAsync([storeId], ct);
        if (row is null)
        {
            row = new CloudStoreHeartbeat { StoreId = storeId };
            db.StoreHeartbeats.Add(row);
        }

        var failedEvents = request.FailedEvents?.Take(HeartbeatLimits.MaxFailedEventsPerHeartbeat).ToList();

        row.ReceivedAtUtc = nowUtc;
        row.StoreHubVersion = request.StoreHubVersion;
        row.PendingCount = request.PendingCount;
        row.FailedCount = request.FailedCount;
        row.OldestPendingCreatedAtUtc = request.OldestPendingCreatedAtUtc;
        row.FailedEventsJson = failedEvents is { Count: > 0 } ? JsonSerializer.Serialize(failedEvents, Web) : null;
    }

    private static async Task CompleteResendsAsync(
        CloudDbContext db, string storeId, IReadOnlyList<CompletedResend>? completed, DateTime nowUtc, CancellationToken ct)
    {
        if (completed is not { Count: > 0 })
            return;

        var ids = completed.Select(c => c.RequestId).ToList();
        var requests = await db.SyncResendRequests.Where(r => r.StoreId == storeId && r.CompletedAtUtc == null && ids.Contains(r.Id))
                                                  .ToListAsync(ct);

        foreach (var request in requests)
        {
            var report = completed.First(c => c.RequestId == request.Id);
            request.CompletedAtUtc = nowUtc;
            request.MovedEventIdsJson = JsonSerializer.Serialize(report.MovedEventIds ?? [], Web);
            request.IgnoredEventIdsJson = JsonSerializer.Serialize(report.IgnoredEventIds ?? [], Web);
        }
    }
}
