using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.DebtRepayments;

/// <param name="BusinessDate">Null means today (store timezone). Past days are viewable, not editable.</param>
public record GetDebtRepaymentsQuery(DateOnly? BusinessDate) : IQuery<IReadOnlyList<DebtRepaymentDto>>;
