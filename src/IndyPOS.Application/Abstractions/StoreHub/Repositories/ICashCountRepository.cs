using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Application.Abstractions.StoreHub.Repositories;

/// <summary>Append-only persistence for cash counts: add and read, never update or delete.</summary>
public interface ICashCountRepository
{
    Task AddAsync(CashCount count, OutboxEvent outboxEvent, CancellationToken cancellationToken = default);

    /// <summary>The day's counts, newest first; same-instant ties broken by Id so the order is stable.</summary>
    Task<IReadOnlyList<CashCount>> ListNewestFirstAsync(string storeId, DateOnly businessDate, CancellationToken cancellationToken = default);

    /// <summary>The count used for calculations: the first row of <see cref="ListNewestFirstAsync"/>.</summary>
    Task<CashCount?> GetLatestAsync(string storeId, DateOnly businessDate, CancellationToken cancellationToken = default);
}
