namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Summary;

public readonly record struct CashDrawerTotals(
    decimal CashSalesTotal,
    decimal DebtRepaymentsTotal,
    decimal CashFloatsTotal,
    decimal MoneyTransferTotal,
    decimal WelfareCardTotal,
    decimal PayoutsTotal);

/// <summary>The cash formula, unchanged from v3's CashFlowData.CalculateExpectedCash.</summary>
public static class CashDrawerCalculator
{
    public static decimal ExpectedCash(CashDrawerTotals totals) =>
        totals.CashSalesTotal
        + totals.DebtRepaymentsTotal
        + totals.CashFloatsTotal
        - totals.MoneyTransferTotal
        - totals.WelfareCardTotal
        - totals.PayoutsTotal;

    public static decimal? Difference(decimal? countedCash, decimal expectedCash) =>
        countedCash - expectedCash;
}
