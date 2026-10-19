using IndyPOS.Application.Common.Constants;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;

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

    /// <summary>
    /// The cash one bill leaves in the drawer: what the customer handed over in Cash, minus the change, which
    /// always comes out of the drawer even when the bill was mostly paid another way. SaleFigures is the till's
    /// own change rule. The cloud's CashDrawerDays view repeats this in SQL, pinned to it by a parity test.
    /// </summary>
    public static decimal CashKept(decimal totalAmount, IReadOnlyList<SalePayment> payments)
    {
        var figures = SaleFigures.From(totalAmount, payments);

        // Case-insensitive, as a sale accepts its codes: it stores the caller's spelling, so "cash" is Cash.
        var cashTendered = payments.Where(p => string.Equals(p.Method, PaymentMethodCodes.Cash, StringComparison.OrdinalIgnoreCase))
                                   .Sum(p => p.Amount);
        return cashTendered - figures.ChangeGiven;
    }
}
