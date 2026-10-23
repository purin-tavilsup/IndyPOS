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

    private static string? Truncate(string? reason) =>
        reason is { Length: > OutboxEvent.LastErrorMaxLength } ? reason[..OutboxEvent.LastErrorMaxLength] : reason;
}
