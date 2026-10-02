using IndyPOS.Application.Abstractions.Cloud.Repositories;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.CloudApi.Infrastructure;

/// <summary>
/// Database-backed implementation of ISyncedEventRepository.
/// Uses CloudDbContext with SyncedEvent table.
/// </summary>
public class DbSyncedEventRepository : ISyncedEventRepository
{
    private readonly CloudDbContext _dbContext;

    public DbSyncedEventRepository(CloudDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> ExistsAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SyncedEvents.AnyAsync(e => e.EventId == eventId, cancellationToken);
    }

    public async Task AddAsync(SyncedEventEntity entity, CancellationToken cancellationToken = default)
    {
        _dbContext.SyncedEvents.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SyncedEventEntity>> GetUnprocessedAsync(
        IReadOnlyCollection<string> eventTypes,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _dbContext.SyncedEvents
            .Where(e => e.ProcessedAtUtc == null
                        && eventTypes.Contains(e.EventType)
                        && (e.NextAttemptAtUtc == null || e.NextAttemptAtUtc <= now))
            .OrderBy(e => e.ReceivedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <remarks>
    /// Matched on <see cref="SyncedEventEntity.EventId"/>, not the key: the inbox key is a long
    /// <c>Id</c>, and a FindAsync by the Guid threw on every call, so no event was ever marked.
    /// An unknown id updates nothing.
    /// </remarks>
    public async Task MarkProcessedAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var processedAtUtc = DateTime.UtcNow;

        await _dbContext.SyncedEvents
                        .Where(e => e.EventId == eventId)
                        .ExecuteUpdateAsync(s => s.SetProperty(e => e.ProcessedAtUtc, processedAtUtc), cancellationToken);
    }

    public async Task MarkFailedAsync(Guid eventId, DateTime nextAttemptAtUtc, CancellationToken cancellationToken = default)
    {
        await _dbContext.SyncedEvents
                        .Where(e => e.EventId == eventId)
                        .ExecuteUpdateAsync(s => s.SetProperty(e => e.Attempts, e => e.Attempts + 1)
                                                  .SetProperty(e => e.NextAttemptAtUtc, nextAttemptAtUtc), cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SyncedEvents.CountAsync(cancellationToken);
    }

    public async Task<int> GetUnprocessedCountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SyncedEvents.CountAsync(e => e.ProcessedAtUtc == null, cancellationToken);
    }

    public async Task<int> GetProcessedCountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SyncedEvents.CountAsync(e => e.ProcessedAtUtc != null, cancellationToken);
    }
}
