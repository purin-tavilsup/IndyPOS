using IndyPOS.Application.UseCases.StoreHub.Reports;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetSalesSummary;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nokpirab;

namespace IndyPOS.Infrastructure.QueryHandlers.Reports;

/// <summary>
/// Handler for GetSalesSummaryQuery.
/// Aggregates invoice and payment data for the specified date range.
/// </summary>
public class GetSalesSummaryQueryHandler : IQueryHandler<GetSalesSummaryQuery, SalesSummaryDto>
{
    private readonly StoreHubDbContext _dbContext;
    private readonly ILogger<GetSalesSummaryQueryHandler> _logger;

    public GetSalesSummaryQueryHandler(StoreHubDbContext dbContext, ILogger<GetSalesSummaryQueryHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<SalesSummaryDto> HandleAsync(GetSalesSummaryQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Generating sales summary: FromDate={FromDate}, ToDate={ToDate}, TopProducts={TopCount}",
            query.FromDate, query.ToDate, query.TopProductsCount);

        var dateRange = ReportDateRange.ToUtcRange(query.FromDate, query.ToDate);

        // Get invoices in date range
        var invoices = await _dbContext.Invoices
            .Where(i => i.CreatedUtc >= dateRange.StartUtc && i.CreatedUtc < dateRange.EndExclusiveUtc)
            .Include(i => i.Payments)
            .Include(i => i.Lines)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var invoiceCount = invoices.Count;
        var totalRevenue = invoices.Sum(i => i.TotalAmount);

        // Payment breakdown
        var payments = invoices.SelectMany(i => i.Payments).ToList();
        var paymentBreakdown = new PaymentBreakdownDto(
            Cash: payments.Where(p => p.Method == "Cash").Sum(p => p.Amount),
            Card: payments.Where(p => p.Method == "Card").Sum(p => p.Amount),
            Transfer: payments.Where(p => p.Method == "Transfer").Sum(p => p.Amount),
            PayLater: payments.Where(p => p.Method == "PayLater").Sum(p => p.Amount),
            WelfareCard: payments.Where(p => p.Method == "WelfareCard").Sum(p => p.Amount),
            Other: payments.Where(p => !new[] { "Cash", "Card", "Transfer", "PayLater", "WelfareCard" }.Contains(p.Method)).Sum(p => p.Amount));

        // Top products
        var topProducts = invoices
            .SelectMany(i => i.Lines)
            .GroupBy(l => new { l.ProductId, l.ProductName })
            .Select(g => new TopProductDto(
                ProductId: g.Key.ProductId,
                ProductName: g.Key.ProductName,
                Category: null, // Will fetch below
                QuantitySold: g.Sum(l => l.Quantity),
                Revenue: g.Sum(l => l.LineTotal)))
            .OrderByDescending(p => p.Revenue)
            .Take(query.TopProductsCount)
            .ToList();

        // Fetch categories for top products
        var productIds = topProducts.Select(p => p.ProductId).ToList();
        var productCategories = await _dbContext.Products
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Category })
            .ToDictionaryAsync(p => p.Id, p => p.Category, cancellationToken);

        var topProductsWithCategory = topProducts
            .Select(p => p with { Category = productCategories.GetValueOrDefault(p.ProductId) })
            .ToList();

        _logger.LogInformation(
            "Sales summary generated: FromDate={FromDate}, ToDate={ToDate}, Invoices={InvoiceCount}, Revenue={TotalRevenue:C}",
            query.FromDate, query.ToDate, invoiceCount, totalRevenue);

        return new SalesSummaryDto(
            FromDate: query.FromDate,
            ToDate: query.ToDate,
            InvoiceCount: invoiceCount,
            TotalRevenue: totalRevenue,
            PaymentBreakdown: paymentBreakdown,
            TopProducts: topProductsWithCategory);
    }
}
