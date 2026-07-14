using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetLegacySalesSummary;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nokpirab;

namespace IndyPOS.Infrastructure.QueryHandlers.Reports;

/// <summary>
/// Handler for GetLegacySalesSummaryQuery.
/// Returns sales summary in the legacy format expected by WinForms UI.
/// </summary>
public class GetLegacySalesSummaryQueryHandler : IQueryHandler<GetLegacySalesSummaryQuery, SalesSummary>
{
    private readonly StoreHubDbContext _dbContext;
    private readonly IStoreIdentityService _storeIdentity;
    private readonly ILogger<GetLegacySalesSummaryQueryHandler> _logger;

    public GetLegacySalesSummaryQueryHandler(
        StoreHubDbContext dbContext,
        IStoreIdentityService storeIdentity,
        ILogger<GetLegacySalesSummaryQueryHandler> logger)
    {
        _dbContext = dbContext;
        _storeIdentity = storeIdentity;
        _logger = logger;
    }

    public async Task<SalesSummary> HandleAsync(GetLegacySalesSummaryQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Generating legacy sales summary: FromDate={FromDate}, ToDate={ToDate}, TimeZone={TimeZone}",
            query.FromDate, query.ToDate, _storeIdentity.TimeZone.Id);

        var dateRange = ReportDateRange.ToUtcRange(query.FromDate, query.ToDate, _storeIdentity.TimeZone);

        // Get all invoices with their lines and payments in the date range
        var invoices = await _dbContext.Invoices
            .Where(i => i.CreatedUtc >= dateRange.StartUtc && i.CreatedUtc < dateRange.EndExclusiveUtc)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Get product categories for categorization
        var productIds = invoices.SelectMany(i => i.Lines).Select(l => l.ProductId).Distinct().ToList();
        var productCategories = await _dbContext.Products
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Category })
            .ToDictionaryAsync(p => p.Id, p => p.Category, cancellationToken);

        // Get PayLater records for this period
        var payLaters = await _dbContext.PayLaters
            .Where(pl => pl.CreatedUtc >= dateRange.StartUtc && pl.CreatedUtc < dateRange.EndExclusiveUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Identify invoices with PayLater payments
        var payLaterInvoiceIds = payLaters.Select(pl => pl.InvoiceId).ToHashSet();

        // Calculate totals
        decimal generalProductsTotal = 0;
        decimal hardwareProductsTotal = 0;
        decimal payLaterTotalForGeneralProducts = 0;
        decimal payLaterTotalForHardwareProducts = 0;

        foreach (var invoice in invoices)
        {
            var hasPayLater = payLaterInvoiceIds.Contains(invoice.Id);

            foreach (var line in invoice.Lines)
            {
                var category = productCategories.GetValueOrDefault(line.ProductId);
                var isHardware = IsHardwareCategory(category);
                var lineTotal = line.LineTotal;

                if (isHardware)
                {
                    hardwareProductsTotal += lineTotal;
                    if (hasPayLater)
                    {
                        payLaterTotalForHardwareProducts += lineTotal;
                    }
                }
                else
                {
                    generalProductsTotal += lineTotal;
                    if (hasPayLater)
                    {
                        payLaterTotalForGeneralProducts += lineTotal;
                    }
                }
            }
        }

        var invoiceTotal = generalProductsTotal + hardwareProductsTotal;
        var payLaterPaymentsTotal = payLaterTotalForGeneralProducts + payLaterTotalForHardwareProducts;
        var invoiceTotalWithoutPayLater = invoiceTotal - payLaterPaymentsTotal;
        var generalWithoutPayLater = generalProductsTotal - payLaterTotalForGeneralProducts;
        var hardwareWithoutPayLater = hardwareProductsTotal - payLaterTotalForHardwareProducts;

        // Calculate completed vs incomplete PayLater
        var completedPayLaterTotal = payLaters.Where(pl => pl.IsCompleted).Sum(pl => pl.PaidAmount);
        var incompletePayLaterTotal = payLaterPaymentsTotal - completedPayLaterTotal;

        _logger.LogInformation(
            "Legacy sales summary generated: FromDate={FromDate}, ToDate={ToDate}, Invoices={InvoiceCount}, Total={InvoiceTotal:C}",
            query.FromDate, query.ToDate, invoices.Count, invoiceTotal);

        return new SalesSummary
        {
            InvoiceTotal = invoiceTotal,
            GeneralProductsTotal = generalProductsTotal,
            HardwareProductsTotal = hardwareProductsTotal,
            PayLaterPaymentsTotal = payLaterPaymentsTotal,
            PayLaterPaymentsTotalForGeneralProducts = payLaterTotalForGeneralProducts,
            PayLaterPaymentsTotalForHardwareProducts = payLaterTotalForHardwareProducts,
            InvoiceTotalWithoutPayLaterPayments = invoiceTotalWithoutPayLater,
            GeneralProductsTotalWithoutPayLaterPayments = generalWithoutPayLater,
            HardwareProductsTotalWithoutPayLaterPayments = hardwareWithoutPayLater,
            CompletedPayLaterPaymentsTotal = completedPayLaterTotal,
            IncompletePayLaterPaymentsTotal = incompletePayLaterTotal
        };
    }

    private static bool IsHardwareCategory(string? category)
    {
        // Hardware category starts at 50 in the enum
        // Categories < 50 are general goods
        if (string.IsNullOrEmpty(category)) return false;

        return category.Equals(nameof(ProductCategory.Hardware), StringComparison.OrdinalIgnoreCase);
    }
}
