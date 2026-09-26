using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Application.Abstractions.StoreHub.Repositories;

/// <summary>
/// Persistence for the soft-deletable cash-drawer entries. Every write saves the row and its
/// outbox event in one SaveChanges, so a row never exists without its sync event.
/// </summary>
public interface ICashEntryRepository<TEntry> where TEntry : CashDrawerEntry
{
    /// <summary>Returns the entry, or null if unknown OR soft-deleted.</summary>
    Task<TEntry?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns the entry even when soft-deleted. Only the delete path uses this.</summary>
    Task<TEntry?> FindIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>The day's non-deleted entries, oldest first.</summary>
    Task<IReadOnlyList<TEntry>> ListAsync(string storeId, DateOnly businessDate, CancellationToken cancellationToken = default);

    Task AddAsync(TEntry entry, OutboxEvent outboxEvent, CancellationToken cancellationToken = default);

    /// <summary>Saves changes made to an entry loaded through this repository, plus its event.</summary>
    Task SaveChangesAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default);
}
