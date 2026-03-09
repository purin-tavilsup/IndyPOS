namespace IndyPOS.Application.UseCases.Cloud.Sync;

/// <summary>
/// Request to sync events from StoreHub to Cloud.
/// </summary>
public record SyncEventRequest(
    Guid EventId,
    int StoreId,
    string EventType,
    string Payload,
    DateTime CreatedAtUtc);

/// <summary>
/// Batch request containing multiple events.
/// </summary>
public record SyncEventsRequest(IReadOnlyList<SyncEventRequest> Events);

/// <summary>
/// Response for a single event sync.
/// </summary>
public record SyncEventResult(
    Guid EventId,
    bool Accepted,
    string? Reason = null);

/// <summary>
/// Response for batch event sync.
/// </summary>
public record SyncEventsResponse(
    int AcceptedCount,
    int DuplicateCount,
    int FailedCount,
    IReadOnlyList<SyncEventResult> Results);
