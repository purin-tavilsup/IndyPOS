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
    IReadOnlyList<TopProductDto> TopProducts)
{
    /// <summary>One row per catalogue method that is enabled or had sales in the range, in display order.</summary>
    public IReadOnlyList<PaymentMethodTotalDto> PaymentsByMethod { get; init; } = [];

    /// <summary>The service products' sales (จัดส่ง, เอกสาร). Empty for a store without services.</summary>
    public IReadOnlyList<ServiceSaleDto> ServiceSales { get; init; } = [];
}

/// <summary>Takings by payment method, named as the store's catalogue names it.</summary>
public record PaymentMethodTotalDto(string Code, string DisplayName, decimal Total);

/// <summary>Sales of one service product.</summary>
public record ServiceSaleDto(string Barcode, string Name, decimal Total);

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
