namespace IndyPOS.Application.UseCases.StoreHub.Reports;

/// <summary>
/// Sales summary for a date range.
/// </summary>
public record SalesSummaryDto(
    DateOnly FromDate,
    DateOnly ToDate,
    int InvoiceCount,
    decimal TotalRevenue,
    PaymentBreakdownDto PaymentBreakdown,
    IReadOnlyList<TopProductDto> TopProducts);

/// <summary>
/// Breakdown of payments by method.
/// </summary>
public record PaymentBreakdownDto(
    decimal Cash,
    decimal Card,
    decimal Transfer,
    decimal PayLater,
    decimal WelfareCard,
    decimal Other);

/// <summary>
/// Top selling product summary.
/// </summary>
public record TopProductDto(
    Guid ProductId,
    string ProductName,
    string? Category,
    int QuantitySold,
    decimal Revenue);

/// <summary>
/// Invoice summary for list views.
/// </summary>
public record InvoiceSummaryDto(
    Guid Id,
    decimal TotalAmount,
    string PrimaryPaymentMethod,
    int LineCount,
    DateTime CreatedUtc);

/// <summary>
/// Full invoice detail with lines and payments.
/// </summary>
public record InvoiceDetailDto(
    Guid Id,
    string StoreId,
    Guid UserId,
    decimal TotalAmount,
    DateTime CreatedUtc,
    IReadOnlyList<InvoiceLineDto> Lines,
    IReadOnlyList<PaymentDto> Payments);

/// <summary>
/// Invoice line item.
/// </summary>
public record InvoiceLineDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

/// <summary>
/// Payment record.
/// </summary>
public record PaymentDto(
    Guid Id,
    string Method,
    decimal Amount,
    string? Note);

/// <summary>
/// PayLater (accounts receivable) summary.
/// </summary>
public record PayLaterSummaryDto(
    string CustomerName,
    decimal TotalOwed,
    decimal TotalPaid,
    decimal RemainingBalance,
    int InvoiceCount,
    DateTime OldestInvoiceDate);

/// <summary>
/// Outstanding PayLater detail.
/// </summary>
public record PayLaterDetailDto(
    Guid Id,
    Guid InvoiceId,
    string CustomerName,
    decimal OriginalAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    bool IsCompleted,
    DateTime CreatedUtc);

/// <summary>
/// Product sales report item.
/// </summary>
public record ProductSalesDto(
    Guid ProductId,
    string Barcode,
    string ProductName,
    string? Category,
    int QuantitySold,
    decimal TotalRevenue,
    decimal AverageUnitPrice);

/// <summary>
/// Paginated result wrapper.
/// </summary>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
