namespace IndyPOS.Application.Abstractions.StoreHub.Repositories;

using IndyPOS.Application.UseCases.Cloud.Sync.Heartbeat;
using IndyPOS.Domain.Entities.Core;

public interface IOutboxRepository
{
    Task<IReadOnlyList<OutboxEvent>> GetPendingEventsAsync(
        int batchSize,
        CancellationToken cancellationToken = default);

    Task MarkAsSentAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>Keeps the event Pending and due again at <paramref name="nextRetryUtc"/>.</summary>
    Task ScheduleRetryAsync(
        Guid eventId,
        DateTime nextRetryUtc,
        string? reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Parks an event the cloud refused as Failed, with the cloud's reason. It is kept, but no longer
    /// picked up, so it cannot hold back the events behind it.
    /// </summary>
    Task MarkAsRefusedAsync(Guid eventId, string reason, CancellationToken cancellationToken = default);

    Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default);

    /// <summary>The number of events the cloud refused, parked as Failed.</summary>
    Task<int> GetFailedCountAsync(CancellationToken cancellationToken = default);

    /// <summary>When the oldest Pending event was created; null when nothing is waiting. Failed events do not count.</summary>
    Task<DateTime?> GetOldestPendingCreatedUtcAsync(CancellationToken cancellationToken = default);

    /// <summary>The oldest Failed events, at most <paramref name="max"/>, with the cloud's reason.</summary>
    Task<IReadOnlyList<FailedEventSummary>> GetFailedEventsAsync(int max, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves Failed events back to Pending with their attempts and backoff reset: the one given, or every Failed event
    /// when <paramref name="eventId"/> is null. Returns the ids it moved; one that is not Failed moves nothing.
    /// </summary>
    Task<IReadOnlyList<Guid>> RequeueFailedAsync(Guid? eventId, CancellationToken cancellationToken = default);
}
