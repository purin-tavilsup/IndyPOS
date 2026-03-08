namespace IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;

using IndyPOS.Application.Abstractions.StoreHub.Repositories;
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

    public async Task MarkAsFailedAsync(
        Guid eventId,
        DateTime nextRetryUtc,
        CancellationToken cancellationToken = default)
    {
        await db.OutboxEvents
            .Where(e => e.Id == eventId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.Status, "Pending")
                .SetProperty(e => e.Attempts, e => e.Attempts + 1)
                .SetProperty(e => e.LastAttemptUtc, DateTime.UtcNow)
                .SetProperty(e => e.NextRetryUtc, nextRetryUtc),
                cancellationToken);
    }

    public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
    {
        return await db.OutboxEvents
            .CountAsync(e => e.Status == "Pending", cancellationToken);
    }

    public async Task<int> GetFailedCountAsync(CancellationToken cancellationToken = default)
    {
        // Events that have been retried at least once and are still pending
        return await db.OutboxEvents
            .CountAsync(e => e.Status == "Pending" && e.Attempts > 0, cancellationToken);
    }
}
