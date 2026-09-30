namespace IndyPOS.Application.Abstractions.Cloud.Repositories;

/// <summary>
/// Repository for storing synced events from stores.
/// </summary>
public interface ISyncedEventRepository
{
    /// <summary>
    /// Check if an event with the given ID already exists.
    /// </summary>
    Task<bool> ExistsAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Store a new synced event.
    /// </summary>
    Task AddAsync(SyncedEventEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get unprocessed events of the given types, oldest first. Events of any other type are left
    /// alone, so they neither get dropped nor take a place in the batch until a handler exists.
    /// </summary>
    Task<IReadOnlyList<SyncedEventEntity>> GetUnprocessedAsync(
        IReadOnlyCollection<string> eventTypes,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark an event as processed.
    /// </summary>
    Task MarkProcessedAsync(Guid eventId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Entity representing a synced event from a store.
/// </summary>
public class SyncedEventEntity
{
    public long Id { get; set; }
    public Guid EventId { get; set; }
    public int StoreId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
}
