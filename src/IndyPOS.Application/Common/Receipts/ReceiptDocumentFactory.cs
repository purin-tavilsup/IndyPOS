using IndyPOS.Application.UseCases.StoreHub.Sales;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Application.UseCases.StoreHub.Sales.Reprints;

namespace IndyPOS.Application.Common.Receipts;

/// <summary>What the till itself knows about a sale it has just completed.</summary>
public sealed record CompletedSale(
    string CashierName,
    IReadOnlyList<ReceiptItem> Items,
    IReadOnlyList<ReceiptPayment> Payments,
    decimal Change);

/// <summary>
/// Builds the printer's input in the two places a receipt is printed: right after a sale, and on a
/// reprint. Server times are UTC; the document holds them in the store's time zone.
/// </summary>
public sealed class ReceiptDocumentFactory(TimeZoneInfo storeTimeZone)
{
    /// <summary>Printed when the server no longer knows the bill's cashier.</summary>
    public const string UnknownCashier = "-";

    public ReceiptDocument ForSale(CompleteSaleResponse response, CompletedSale sale) => new(
        InvoiceNumber: response.InvoiceNumber,
        IssuedAt: ToStoreTime(response.CreatedUtc),
        CashierName: sale.CashierName,
        Items: sale.Items,
        Payments: sale.Payments,
        Total: response.TotalAmount,
        AmountReceived: sale.Payments.Sum(p => p.Amount),
        Change: sale.Change,
        IsRefund: response.TotalAmount < 0);

    public ReceiptDocument ForReprint(InvoiceReprintResultDto result, string reprintedBy)
    {
        var bill = result.Sale;

        return new ReceiptDocument(
            InvoiceNumber: bill.InvoiceNumber,
            IssuedAt: ToStoreTime(bill.CreatedUtc),
            CashierName: bill.CashierName ?? UnknownCashier,
            Items: bill.Lines.Select(ToItem).ToList(),
            Payments: bill.Payments.Select(ToPayment).ToList(),
            Total: bill.TotalAmount,
            AmountReceived: bill.AmountReceived,
            Change: bill.ChangeGiven,
            IsRefund: bill.IsRefund,
            Reprint: new ReceiptReprint(ToStoreTime(result.Reprint.CreatedUtc), reprintedBy));
    }

    private static ReceiptItem ToItem(InvoiceLineDto line) =>
        new(line.ProductName, line.Note, line.Quantity, line.UnitPrice, line.LineTotal);

    private static ReceiptPayment ToPayment(PaymentDto payment) =>
        new(payment.MethodDisplayName, payment.Amount, payment.Note);

    // JSON leaves the Kind Unspecified; the server always sends UTC.
    private DateTime ToStoreTime(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), storeTimeZone);
}
