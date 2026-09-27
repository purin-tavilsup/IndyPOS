using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Payouts;

/// <param name="BusinessDate">Null means today (store timezone). Past days are viewable, not editable.</param>
public record GetCashPayoutsQuery(DateOnly? BusinessDate) : IQuery<IReadOnlyList<CashPayoutDto>>;
