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

    /// <summary>
    /// Saves changes made to an entry loaded through this repository, plus its event, in one
    /// SaveChanges. The save is guarded by the deleted flag as it was loaded: if another request
    /// deleted the entry after this one loaded it, nothing is written — not the entry, not the
    /// event — and the pending changes are discarded.
    /// </summary>
    /// <returns>
    /// True when the entry and its event were saved; false when the entry was deleted concurrently.
    /// Two edits never conflict with each other: the later save wins.
    /// </returns>
    Task<bool> TrySaveChangesAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default);
}
