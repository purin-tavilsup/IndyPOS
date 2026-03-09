using System.Collections.Concurrent;
using IndyPOS.Application.Abstractions.Cloud.Repositories;

namespace IndyPOS.CloudApi.Infrastructure;

/// <summary>
/// In-memory implementation of ISyncedEventRepository for development and testing.
/// Replace with a proper database implementation for production.
/// </summary>
public class InMemorySyncedEventRepository : ISyncedEventRepository
{
    private readonly ConcurrentDictionary<Guid, SyncedEventEntity> _events = new();
    private long _idCounter;

    public Task<bool> ExistsAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_events.ContainsKey(eventId));
    }

    public Task AddAsync(SyncedEventEntity entity, CancellationToken cancellationToken = default)
    {
        entity.Id = Interlocked.Increment(ref _idCounter);
        _events.TryAdd(entity.EventId, entity);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SyncedEventEntity>> GetUnprocessedAsync(
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var unprocessed = _events.Values
            .Where(e => e.ProcessedAtUtc == null)
            .OrderBy(e => e.ReceivedAtUtc)
            .Take(limit)
            .ToList();

        return Task.FromResult<IReadOnlyList<SyncedEventEntity>>(unprocessed);
    }

    public Task MarkProcessedAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        if (_events.TryGetValue(eventId, out var entity))
        {
            entity.ProcessedAtUtc = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    // Helper method for status endpoint
    public int GetTotalCount() => _events.Count;
    public int GetUnprocessedCount() => _events.Values.Count(e => e.ProcessedAtUtc == null);
}
