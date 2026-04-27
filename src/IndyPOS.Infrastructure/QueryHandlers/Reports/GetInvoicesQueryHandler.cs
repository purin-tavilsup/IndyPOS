using IndyPOS.Application.UseCases.StoreHub.Reports;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetInvoices;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nokpirab;

namespace IndyPOS.Infrastructure.QueryHandlers.Reports;

/// <summary>
/// Handler for GetInvoicesQuery.
/// Returns paginated invoice summaries.
/// </summary>
public class GetInvoicesQueryHandler : IQueryHandler<GetInvoicesQuery, PagedResult<InvoiceSummaryDto>>
{
    private readonly StoreHubDbContext _dbContext;
    private readonly ILogger<GetInvoicesQueryHandler> _logger;

    public GetInvoicesQueryHandler(StoreHubDbContext dbContext, ILogger<GetInvoicesQueryHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<PagedResult<InvoiceSummaryDto>> HandleAsync(GetInvoicesQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Fetching invoices: FromDate={FromDate}, ToDate={ToDate}, Page={Page}, PageSize={PageSize}",
            query.FromDate, query.ToDate, query.Page, query.PageSize);

        var dateRange = ReportDateRange.ToUtcRange(query.FromDate, query.ToDate);

        var baseQuery = _dbContext.Invoices
            .Where(i => i.CreatedUtc >= dateRange.StartUtc && i.CreatedUtc < dateRange.EndExclusiveUtc)
            .AsNoTracking();

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var invoices = await baseQuery
            .OrderByDescending(i => i.CreatedUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(i => new InvoiceSummaryDto(
                Id: i.Id,
                TotalAmount: i.TotalAmount,
                PrimaryPaymentMethod: i.Payments.OrderByDescending(p => p.Amount).Select(p => p.Method).FirstOrDefault() ?? "Unknown",
                LineCount: i.Lines.Count,
                CreatedUtc: i.CreatedUtc))
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Invoices fetched: FromDate={FromDate}, ToDate={ToDate}, Page={Page}, Returned={Count}, Total={TotalCount}",
            query.FromDate, query.ToDate, query.Page, invoices.Count, totalCount);

        return new PagedResult<InvoiceSummaryDto>(
            Items: invoices,
            TotalCount: totalCount,
            Page: query.Page,
            PageSize: query.PageSize);
    }
}
