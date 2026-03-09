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
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.SyncedEvents
            .Where(e => e.ProcessedAtUtc == null)
            .OrderBy(e => e.ReceivedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkProcessedAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.SyncedEvents.FindAsync([eventId], cancellationToken);
        if (entity is not null)
        {
            entity.ProcessedAtUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
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
