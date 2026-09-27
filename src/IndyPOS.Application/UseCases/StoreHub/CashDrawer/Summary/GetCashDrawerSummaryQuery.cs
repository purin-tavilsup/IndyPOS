using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Summary;

/// <param name="BusinessDate">Null means today (store timezone).</param>
public record GetCashDrawerSummaryQuery(DateOnly? BusinessDate) : IQuery<CashDrawerSummaryDto>;
