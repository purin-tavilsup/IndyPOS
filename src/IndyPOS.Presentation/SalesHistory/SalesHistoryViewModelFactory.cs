using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Receipts;

namespace IndyPOS.Presentation.SalesHistory;

/// <summary>
/// The sales-history ViewModels for whoever is logged in when a screen opens. Screens are built once,
/// before any login, so they ask here each time they open rather than holding a ViewModel.
/// </summary>
public sealed class SalesHistoryViewModelFactory(
    IStoreHubClient client,
    IReceiptPrinterService printer,
    ReceiptDocumentFactory receipts,
    StoreClock clock,
    ICurrentUser currentUser)
{
    public const string NotLoggedIn = "No user is logged in on this till.";

    public InvoiceDetailViewModel CreateDetail() => Detail(Access());

    public InvoiceHistoryViewModel CreateHistory() => History(Access());

    /// <summary>
    /// บิลวันนี้ on the sale screen: today only, whoever opens it (spec §7), so a manager's reprint from
    /// there keeps to today too.
    /// </summary>
    public InvoiceHistoryViewModel CreateTodayOnly() => History(Access() with { CanViewAnyDay = false });

    private InvoiceHistoryViewModel History(SalesHistoryAccess access) =>
        new(client, clock, access, Detail(access));

    private InvoiceDetailViewModel Detail(SalesHistoryAccess access) => new(client, printer, receipts, clock, access);

    private SalesHistoryAccess Access() =>
        SalesHistoryAccess.For(currentUser.User ?? throw new InvalidOperationException(NotLoggedIn));
}
