using System.Text.Json;
using IndyPOS.Application.Common.Extensions;
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
    private const int StoreHubVersionMaxLength = 50;

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

        // The store already keeps the heartbeat light; the cloud enforces the same limits rather than trusting it.
        var failedEvents = request.FailedEvents?.Where(e => e is not null)
                                               .Take(HeartbeatLimits.MaxFailedEventsPerHeartbeat)
                                               .Select(e => e with { Reason = e.Reason.CutTo(HeartbeatLimits.MaxReasonLength) })
                                               .ToList();

        row.ReceivedAtUtc = nowUtc;
        row.StoreHubVersion = request.StoreHubVersion.CutTo(StoreHubVersionMaxLength);
        row.PendingCount = request.PendingCount;
        row.FailedCount = request.FailedCount;
        row.OldestPendingCreatedAtUtc = request.OldestPendingAgeSeconds is { } age ? nowUtc.AddSeconds(-Math.Max(0, age)) : null;
        row.FailedEventsJson = failedEvents is { Count: > 0 } ? JsonSerializer.Serialize(failedEvents, Web) : null;
    }

    private static async Task CompleteResendsAsync(
        CloudDbContext db, string storeId, IReadOnlyList<CompletedResend>? completed, DateTime nowUtc, CancellationToken ct)
    {
        var reports = completed?.Where(c => c is not null).ToList() ?? [];
        if (reports.Count == 0)
            return;

        var ids = reports.Select(c => c.RequestId).ToList();
        var requests = await db.SyncResendRequests.Where(r => r.StoreId == storeId && r.CompletedAtUtc == null && ids.Contains(r.Id))
                                                  .ToListAsync(ct);

        foreach (var request in requests)
        {
            var report = reports.First(c => c.RequestId == request.Id);
            request.CompletedAtUtc = nowUtc;
            request.MovedEventIdsJson = JsonSerializer.Serialize(report.MovedEventIds ?? [], Web);
            request.IgnoredEventIdsJson = JsonSerializer.Serialize(report.IgnoredEventIds ?? [], Web);
        }
    }
}
