namespace IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;

using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Extensions;
using IndyPOS.Application.UseCases.Cloud.Sync.Heartbeat;
using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

public class OutboxRepository(StoreHubDbContext db) : IOutboxRepository
{
    public async Task<IReadOnlyList<OutboxEvent>> GetPendingEventsAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await db.OutboxEvents
            .Where(e => e.Status == "Pending" && (e.NextRetryUtc == null || e.NextRetryUtc <= now))
            .OrderBy(e => e.CreatedUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsSentAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        await db.OutboxEvents
            .Where(e => e.Id == eventId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.Status, "Sent")
                .SetProperty(e => e.LastAttemptUtc, DateTime.UtcNow),
                cancellationToken);
    }

    public async Task ScheduleRetryAsync(
        Guid eventId,
        DateTime nextRetryUtc,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var lastError = Truncate(reason);

        await db.OutboxEvents
            .Where(e => e.Id == eventId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.Status, "Pending")
                .SetProperty(e => e.Attempts, e => e.Attempts + 1)
                .SetProperty(e => e.LastAttemptUtc, DateTime.UtcNow)
                .SetProperty(e => e.NextRetryUtc, nextRetryUtc)
                .SetProperty(e => e.LastError, lastError),
                cancellationToken);
    }

    public async Task MarkAsRefusedAsync(Guid eventId, string reason, CancellationToken cancellationToken = default)
    {
        var lastError = Truncate(reason);

        await db.OutboxEvents
            .Where(e => e.Id == eventId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.Status, "Failed")
                .SetProperty(e => e.Attempts, e => e.Attempts + 1)
                .SetProperty(e => e.LastAttemptUtc, DateTime.UtcNow)
                .SetProperty(e => e.NextRetryUtc, (DateTime?)null)
                .SetProperty(e => e.LastError, lastError),
                cancellationToken);
    }

    public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
    {
        return await db.OutboxEvents
            .CountAsync(e => e.Status == "Pending", cancellationToken);
    }

    public async Task<int> GetFailedCountAsync(CancellationToken cancellationToken = default)
    {
        return await db.OutboxEvents
            .CountAsync(e => e.Status == "Failed", cancellationToken);
    }

    public async Task<DateTime?> GetOldestPendingCreatedUtcAsync(CancellationToken cancellationToken = default)
    {
        return await db.OutboxEvents
            .Where(e => e.Status == "Pending")
            .MinAsync(e => (DateTime?)e.CreatedUtc, cancellationToken);
    }

    public async Task<IReadOnlyList<FailedEventSummary>> GetFailedEventsAsync(int max, CancellationToken cancellationToken = default)
    {
        return await db.OutboxEvents
            .Where(e => e.Status == "Failed")
            .OrderBy(e => e.CreatedUtc)
            .Take(max)
            .Select(e => new FailedEventSummary(e.Id, e.Type, e.LastAttemptUtc, e.LastError))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> RequeueFailedAsync(Guid? eventId, CancellationToken cancellationToken = default)
    {
        var failed = db.OutboxEvents.Where(e => e.Status == "Failed" && (eventId == null || e.Id == eventId));
        var ids = await failed.Select(e => e.Id).ToListAsync(cancellationToken);

        // Still filtered on Failed so a row is never moved out of another state. Nothing else writes a Failed row
        // (the sync worker only reads Pending ones), so the ids read are the ids moved.
        await db.OutboxEvents
            .Where(e => ids.Contains(e.Id) && e.Status == "Failed")
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.Status, "Pending")
                .SetProperty(e => e.Attempts, 0)
                .SetProperty(e => e.NextRetryUtc, (DateTime?)null),
                cancellationToken);

        return ids;
    }

    // Cut without splitting a surrogate pair: a lone half makes PostgreSQL's UTF-8 encoding throw.
    private static string? Truncate(string? reason) => reason.CutTo(OutboxEvent.LastErrorMaxLength);
}
