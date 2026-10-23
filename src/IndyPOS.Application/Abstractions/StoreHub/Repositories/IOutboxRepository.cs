namespace IndyPOS.Application.Abstractions.StoreHub.Repositories;

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
}
