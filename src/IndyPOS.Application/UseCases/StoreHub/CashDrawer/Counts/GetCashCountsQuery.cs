using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;

/// <summary>The day's counts, newest first — the history list. The first row is the one used for calculations.</summary>
/// <param name="BusinessDate">Null means today (store timezone).</param>
public record GetCashCountsQuery(DateOnly? BusinessDate) : IQuery<IReadOnlyList<CashCountDto>>;
