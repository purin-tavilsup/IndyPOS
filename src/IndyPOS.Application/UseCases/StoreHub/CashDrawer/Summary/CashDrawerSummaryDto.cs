using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;
using IndyPOS.Application.UseCases.StoreHub.Reports;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Summary;

/// <summary>
/// Everything the cash-drawer screen shows, already computed. The UI renders it and does no maths,
/// so the Avalonia port inherits the rules for free.
/// </summary>
public record CashDrawerSummaryDto
{
    public required DateOnly BusinessDate { get; init; }

    /// <summary>True only for today; the ViewModel disables edit controls otherwise. The server still enforces it.</summary>
    public required bool IsEditable { get; init; }

    public required decimal GeneralProductsTotal { get; init; }
    public required decimal HardwareProductsTotal { get; init; }

    /// <summary>Takings paid with the Cash method; the only sales money in the drawer.</summary>
    public required decimal CashPaymentsTotal { get; init; }

    /// <summary>
    /// One row per non-cash method (not Cash, not PayLater) enabled or used that day, in display
    /// order. Shown, never added to expected cash.
    /// </summary>
    public required IReadOnlyList<PaymentMethodTotalDto> NonCashPayments { get; init; }

    public required decimal PayLaterGeneralProductsTotal { get; init; }
    public required decimal PayLaterHardwareProductsTotal { get; init; }

    public required decimal PayoutsTotal { get; init; }
    public required decimal GeneralPayoutsTotal { get; init; }
    public required decimal HardwarePayoutsTotal { get; init; }
    public required decimal CashFloatsTotal { get; init; }
    public required decimal DebtRepaymentsTotal { get; init; }

    public required decimal ExpectedCash { get; init; }

    /// <summary>The latest count of the day, or null if nobody has counted yet.</summary>
    public CashCountDto? LatestCount { get; init; }
    public decimal? CountedCash { get; init; }

    /// <summary>Counted − expected. Positive = over, negative = short, null = not counted yet.</summary>
    public decimal? CashDifference { get; init; }
}
