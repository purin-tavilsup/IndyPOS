using IndyPOS.Domain.Enums;

namespace IndyPOS.Application.UseCases.StoreHub.Sales.History;

/// <summary>A bill in the list. Per-bill amounts help a cashier find "the ฿350 one at 2 pm".</summary>
public record InvoiceSummaryDto(
    Guid Id,
    long InvoiceNumber,
    DateTime CreatedUtc,
    decimal TotalAmount,
    string PrimaryPaymentMethod,
    int LineCount);

/// <summary>
/// One page of bills. Deliberately no money total and no total count: the list is for finding a
/// bill, not a report. HasMore drives "load more".
/// </summary>
public record SalesPage(
    IReadOnlyList<InvoiceSummaryDto> Items,
    int Page,
    int PageSize,
    bool HasMore);

/// <summary>One bill with everything its receipt prints (spec §6, checked against ReceiptPrinterService).</summary>
public record InvoiceDetailDto(
    Guid Id,
    long InvoiceNumber,
    string StoreId,
    Guid UserId,
    string? CashierName,
    decimal TotalAmount,
    decimal AmountReceived,
    decimal ChangeGiven,
    bool IsRefund,
    bool HasPayLater,
    decimal PayLaterAmount,
    DateTime CreatedUtc,
    IReadOnlyList<InvoiceLineDto> Lines,
    IReadOnlyList<PaymentDto> Payments);

/// <param name="CategoryKind">Null when the product has no category or its code is not in the catalogue.</param>
public record InvoiceLineDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string? Barcode,
    string? Note,
    ProductCategoryKind? CategoryKind,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

/// <param name="MethodDisplayName">The catalogue's name, or the code itself for a method no longer in it.</param>
public record PaymentDto(
    Guid Id,
    string Method,
    string MethodDisplayName,
    decimal Amount,
    string? Note);
