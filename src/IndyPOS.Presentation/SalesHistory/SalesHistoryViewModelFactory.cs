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

    public InvoiceDetailViewModel CreateDetail() => new(client, printer, receipts, clock, Access());

    private SalesHistoryAccess Access() =>
        SalesHistoryAccess.For(currentUser.User ?? throw new InvalidOperationException(NotLoggedIn));
}
