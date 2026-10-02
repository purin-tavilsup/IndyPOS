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
    /// Events whose <see cref="SyncedEventEntity.NextAttemptAtUtc"/> is in the future are skipped
    /// too, so a failed event waits its turn instead of holding a batch slot.
    /// Ties on ReceivedAtUtc (one ingest batch) are broken by the inbox Id, which is the order the
    /// store sent them in, so an invoice is handled before a reprint of it that came in the same batch.
    /// </summary>
    Task<IReadOnlyList<SyncedEventEntity>> GetUnprocessedAsync(
        IReadOnlyCollection<string> eventTypes,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark an event as processed.
    /// </summary>
    Task MarkProcessedAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Record a failed attempt, and hold the event back until <paramref name="nextAttemptAtUtc"/>.
    /// The event is never dropped.
    /// </summary>
    Task MarkFailedAsync(Guid eventId, DateTime nextAttemptAtUtc, CancellationToken cancellationToken = default);
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

    /// <summary>How many times processing this event has failed.</summary>
    public int Attempts { get; set; }

    /// <summary>
    /// When a failed event may be tried again; null means now. A waiting event is not fetched, so
    /// it cannot hold a batch slot that the events it waits for need.
    /// </summary>
    public DateTime? NextAttemptAtUtc { get; set; }
}
