using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Reports.GetProductSales;

/// <summary>
/// Query to get product sales report for a date range.
/// Returns sales data grouped by product.
/// </summary>
public record GetProductSalesQuery(
    DateOnly FromDate,
    DateOnly ToDate,
    string? Category = null,
    int Page = 1,
    int PageSize = 50) : IQuery<PagedResult<ProductSalesDto>>;
