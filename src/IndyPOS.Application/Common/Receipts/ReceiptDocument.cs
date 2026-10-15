namespace IndyPOS.Application.Common.Receipts;

/// <summary>
/// Everything a receipt prints, and the printer's only input. Times are already in the store's time
/// zone. Built by <see cref="ReceiptDocumentFactory"/>, after a sale or for a reprint.
/// </summary>
public sealed record ReceiptDocument(
    long InvoiceNumber,
    DateTime IssuedAt,
    string CashierName,
    IReadOnlyList<ReceiptItem> Items,
    IReadOnlyList<ReceiptPayment> Payments,
    decimal Total,
    decimal AmountReceived,
    decimal Change,
    bool IsRefund,
    ReceiptReprint? Reprint = null)
{
    public bool IsCopy => Reprint is not null;
}

public sealed record ReceiptItem(string Description, string? Note, int Quantity, decimal UnitPrice, decimal LineTotal);

public sealed record ReceiptPayment(string DisplayName, decimal Amount, string? Note);

/// <summary>Who asked for the copy, and when (store-local).</summary>
public sealed record ReceiptReprint(DateTime ReprintedAt, string ReprintedBy);

/// <summary>The store lines at the top of every receipt, from the till's store configuration.</summary>
public sealed record ReceiptHeader(string StoreName, string AddressLine1, string AddressLine2, string PhoneNumber);
