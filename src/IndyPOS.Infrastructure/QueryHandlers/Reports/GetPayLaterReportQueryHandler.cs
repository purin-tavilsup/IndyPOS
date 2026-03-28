using IndyPOS.Application.UseCases.StoreHub.Reports;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetPayLaterReport;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Nokpirab;

namespace IndyPOS.Infrastructure.QueryHandlers.Reports;

/// <summary>
/// Handler for GetPayLaterReportQuery.
/// Aggregates PayLater records by customer (Description field).
/// </summary>
public class GetPayLaterReportQueryHandler : IQueryHandler<GetPayLaterReportQuery, PayLaterReportDto>
{
    private readonly StoreHubDbContext _dbContext;

    public GetPayLaterReportQueryHandler(StoreHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PayLaterReportDto> HandleAsync(GetPayLaterReportQuery query, CancellationToken cancellationToken = default)
    {
        var baseQuery = _dbContext.PayLaters.AsNoTracking();

        if (!query.IncludeCompleted)
        {
            baseQuery = baseQuery.Where(p => !p.IsCompleted);
        }

        // Get all matching records for aggregation
        var payLaters = await baseQuery.ToListAsync(cancellationToken);

        // Summary stats
        var totalOutstanding = payLaters.Sum(p => p.PayLaterAmount - p.PaidAmount);
        var totalPaid = payLaters.Sum(p => p.PaidAmount);
        var activeCustomers = payLaters.Where(p => !p.IsCompleted).Select(p => p.Description).Distinct().Count();
        var completedCount = payLaters.Count(p => p.IsCompleted);

        // Group by customer (Description)
        var customerGroups = payLaters
            .GroupBy(p => p.Description)
            .Select(g => new PayLaterSummaryDto(
                CustomerName: g.Key,
                TotalOwed: g.Sum(p => p.PayLaterAmount),
                TotalPaid: g.Sum(p => p.PaidAmount),
                RemainingBalance: g.Sum(p => p.PayLaterAmount - p.PaidAmount),
                InvoiceCount: g.Count(),
                OldestInvoiceDate: g.Min(p => p.CreatedUtc)))
            .OrderByDescending(c => c.RemainingBalance)
            .ToList();

        var totalCount = customerGroups.Count;

        var pagedCustomers = customerGroups
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return new PayLaterReportDto(
            TotalOutstanding: totalOutstanding,
            TotalPaid: totalPaid,
            ActiveCustomers: activeCustomers,
            CompletedCount: completedCount,
            Customers: new PagedResult<PayLaterSummaryDto>(
                Items: pagedCustomers,
                TotalCount: totalCount,
                Page: query.Page,
                PageSize: query.PageSize));
    }
}
