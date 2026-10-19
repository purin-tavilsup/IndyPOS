using IndyPOS.Domain.Enums;

namespace IndyPOS.Presentation.CashDrawer;

/// <summary>One row of a cash-entry list, in store time. Text is the description or the customer's name.</summary>
public sealed record CashEntryRow(Guid Id, DateTime StoreTime, string? Text, PayoutCategory? Category, decimal Amount);

/// <summary>What the cashier typed. Category is used by payouts only.</summary>
public sealed record CashEntryInput(decimal Amount, string? Text, PayoutCategory Category);

/// <summary>
/// One cash-entry list's StoreHub calls (payouts, floats or debt repayments), so a single
/// <see cref="CashEntriesViewModel"/> serves all three.
/// </summary>
public interface ICashEntryApi
{
    bool HasCategory { get; }

    Task<IReadOnlyList<CashEntryRow>> ListAsync(DateOnly day, CancellationToken cancellationToken);

    Task AddAsync(CashEntryInput input, CancellationToken cancellationToken);

    Task EditAsync(Guid id, CashEntryInput input, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
