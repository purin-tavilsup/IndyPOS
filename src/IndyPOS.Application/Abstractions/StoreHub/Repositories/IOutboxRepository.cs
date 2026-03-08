namespace IndyPOS.Application.Abstractions.StoreHub.Repositories;

using IndyPOS.Domain.Entities.Core;

public interface IOutboxRepository
{
    Task<IReadOnlyList<OutboxEvent>> GetPendingEventsAsync(
        int batchSize,
        CancellationToken cancellationToken = default);

    Task MarkAsSentAsync(Guid eventId, CancellationToken cancellationToken = default);

    Task MarkAsFailedAsync(
        Guid eventId,
        DateTime nextRetryUtc,
        CancellationToken cancellationToken = default);

    Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetFailedCountAsync(CancellationToken cancellationToken = default);
}
