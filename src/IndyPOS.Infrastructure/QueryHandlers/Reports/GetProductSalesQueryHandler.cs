using IndyPOS.Application.UseCases.StoreHub.Reports;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetProductSales;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Nokpirab;

namespace IndyPOS.Infrastructure.QueryHandlers.Reports;

/// <summary>
/// Handler for GetProductSalesQuery.
/// Aggregates invoice lines by product for the date range.
/// </summary>
public class GetProductSalesQueryHandler : IQueryHandler<GetProductSalesQuery, PagedResult<ProductSalesDto>>
{
    private readonly StoreHubDbContext _dbContext;

    public GetProductSalesQueryHandler(StoreHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<ProductSalesDto>> HandleAsync(GetProductSalesQuery query, CancellationToken cancellationToken = default)
    {
        var fromDateUtc = query.FromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toDateUtc = query.ToDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        // Get invoice lines in date range with product info
        var invoiceLines = await _dbContext.InvoiceLines
            .Include(l => l.Invoice)
            .Where(l => l.Invoice.CreatedUtc >= fromDateUtc && l.Invoice.CreatedUtc <= toDateUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Get products for barcodes and categories
        var productIds = invoiceLines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _dbContext.Products
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Barcode, p.Category })
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        // Group by product
        var productSales = invoiceLines
            .GroupBy(l => new { l.ProductId, l.ProductName })
            .Select(g =>
            {
                var product = products.GetValueOrDefault(g.Key.ProductId);
                var totalQty = g.Sum(l => l.Quantity);
                var totalRevenue = g.Sum(l => l.LineTotal);

                return new ProductSalesDto(
                    ProductId: g.Key.ProductId,
                    Barcode: product?.Barcode ?? string.Empty,
                    ProductName: g.Key.ProductName,
                    Category: product?.Category,
                    QuantitySold: totalQty,
                    TotalRevenue: totalRevenue,
                    AverageUnitPrice: totalQty > 0 ? totalRevenue / totalQty : 0);
            })
            .ToList();

        // Apply category filter if specified
        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            productSales = productSales
                .Where(p => string.Equals(p.Category, query.Category, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // Order by revenue descending
        productSales = productSales.OrderByDescending(p => p.TotalRevenue).ToList();

        var totalCount = productSales.Count;

        var pagedProducts = productSales
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return new PagedResult<ProductSalesDto>(
            Items: pagedProducts,
            TotalCount: totalCount,
            Page: query.Page,
            PageSize: query.PageSize);
    }
}
