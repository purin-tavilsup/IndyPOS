namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Summary;

public readonly record struct CashDrawerTotals(
    decimal CashPaymentsTotal,
    decimal DebtRepaymentsTotal,
    decimal CashFloatsTotal,
    decimal PayoutsTotal);

/// <summary>
/// The cash formula. Only Cash-method takings go into the drawer, so it starts from them. v3 instead
/// took every non-PayLater sale and subtracted transfer and welfare card, so each other campaign
/// (คนละครึ่ง, ม.33, เราชนะ) inflated expected cash.
/// </summary>
public static class CashDrawerCalculator
{
    public static decimal ExpectedCash(CashDrawerTotals totals) =>
        totals.CashPaymentsTotal
        + totals.DebtRepaymentsTotal
        + totals.CashFloatsTotal
        - totals.PayoutsTotal;

    public static decimal? Difference(decimal? countedCash, decimal expectedCash) =>
        countedCash - expectedCash;
}
