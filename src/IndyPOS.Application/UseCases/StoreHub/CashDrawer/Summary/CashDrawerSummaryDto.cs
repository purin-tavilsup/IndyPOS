using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;

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
    public required decimal CashSalesTotal { get; init; }
    public required decimal MoneyTransferTotal { get; init; }
    public required decimal WelfareCardTotal { get; init; }
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
