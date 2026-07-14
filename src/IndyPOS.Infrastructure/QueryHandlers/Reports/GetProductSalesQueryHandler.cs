using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.Reports;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetProductSales;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nokpirab;

namespace IndyPOS.Infrastructure.QueryHandlers.Reports;

/// <summary>
/// Handler for GetProductSalesQuery.
/// Aggregates invoice lines by product for the date range.
/// </summary>
public class GetProductSalesQueryHandler : IQueryHandler<GetProductSalesQuery, PagedResult<ProductSalesDto>>
{
    private readonly StoreHubDbContext _dbContext;
    private readonly IStoreIdentityService _storeIdentity;
    private readonly ILogger<GetProductSalesQueryHandler> _logger;

    public GetProductSalesQueryHandler(
        StoreHubDbContext dbContext,
        IStoreIdentityService storeIdentity,
        ILogger<GetProductSalesQueryHandler> logger)
    {
        _dbContext = dbContext;
        _storeIdentity = storeIdentity;
        _logger = logger;
    }

    public async Task<PagedResult<ProductSalesDto>> HandleAsync(GetProductSalesQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Generating product sales report: FromDate={FromDate}, ToDate={ToDate}, Category={Category}, Page={Page}, TimeZone={TimeZone}",
            query.FromDate, query.ToDate, query.Category ?? "All", query.Page, _storeIdentity.TimeZone.Id);

        var dateRange = ReportDateRange.ToUtcRange(query.FromDate, query.ToDate, _storeIdentity.TimeZone);

        // Get invoice lines in date range with product info
        var invoiceLines = await _dbContext.InvoiceLines
            .Include(l => l.Invoice)
            .Where(l => l.Invoice.CreatedUtc >= dateRange.StartUtc && l.Invoice.CreatedUtc < dateRange.EndExclusiveUtc)
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

        _logger.LogInformation(
            "Product sales report generated: FromDate={FromDate}, ToDate={ToDate}, Products={TotalCount}, Page={Page}",
            query.FromDate, query.ToDate, totalCount, query.Page);

        return new PagedResult<ProductSalesDto>(
            Items: pagedProducts,
            TotalCount: totalCount,
            Page: query.Page,
            PageSize: query.PageSize);
    }
}
