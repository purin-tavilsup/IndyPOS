using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.Reports;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetSalesSummary;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.ValueObjects;
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
    private readonly IStoreIdentityService _storeIdentity;
    private readonly ILogger<GetSalesSummaryQueryHandler> _logger;

    public GetSalesSummaryQueryHandler(
        StoreHubDbContext dbContext,
        IStoreIdentityService storeIdentity,
        ILogger<GetSalesSummaryQueryHandler> logger)
    {
        _dbContext = dbContext;
        _storeIdentity = storeIdentity;
        _logger = logger;
    }

    public async Task<SalesSummaryDto> HandleAsync(GetSalesSummaryQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Generating sales summary: FromDate={FromDate}, ToDate={ToDate}, TopProducts={TopCount}, TimeZone={TimeZone}",
            query.FromDate, query.ToDate, query.TopProductsCount, _storeIdentity.TimeZone.Id);

        var dateRange = ReportDateRange.ToUtcRange(query.FromDate, query.ToDate, _storeIdentity.TimeZone);

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

        var catalogue = await _dbContext.PaymentMethods.AsNoTracking().ToListAsync(cancellationToken);
        var serviceSales = _storeIdentity.Features.ServiceProductsEnabled
            ? await ServiceSalesAsync(invoices, cancellationToken)
            : [];

        return new SalesSummaryDto(
            FromDate: query.FromDate,
            ToDate: query.ToDate,
            InvoiceCount: invoiceCount,
            TotalRevenue: totalRevenue,
            PaymentBreakdown: paymentBreakdown,
            TopProducts: topProductsWithCategory)
        {
            PaymentsByMethod = PaymentMethodTotals.Build(catalogue, payments),
            ServiceSales = serviceSales
        };
    }

    // One row per service product, in ServiceProductBarcodes order, even at zero: the cashier reads a
    // fixed pair of figures. A product missing from the catalogue still shows, under its barcode.
    private async Task<IReadOnlyList<ServiceSaleDto>> ServiceSalesAsync(
        IReadOnlyList<Invoice> invoices, CancellationToken cancellationToken)
    {
        var products = await _dbContext.Products
            .AsNoTracking()
            .Where(p => ServiceProductBarcodes.All.Contains(p.Barcode))
            .Select(p => new { p.Id, p.Barcode, p.Name })
            .ToListAsync(cancellationToken);

        var lines = invoices.SelectMany(i => i.Lines).ToList();

        return ServiceProductBarcodes.All
            .Select(barcode =>
            {
                var product = products.FirstOrDefault(p => p.Barcode == barcode);
                var total = product is null ? 0m : lines.Where(l => l.ProductId == product.Id).Sum(l => l.LineTotal);
                return new ServiceSaleDto(barcode, product?.Name ?? barcode, total);
            })
            .ToList();
    }
}
