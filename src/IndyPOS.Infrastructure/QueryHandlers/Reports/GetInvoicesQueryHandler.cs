using IndyPOS.Application.UseCases.StoreHub.Reports;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetInvoices;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Nokpirab;

namespace IndyPOS.Infrastructure.QueryHandlers.Reports;

/// <summary>
/// Handler for GetInvoicesQuery.
/// Returns paginated invoice summaries.
/// </summary>
public class GetInvoicesQueryHandler : IQueryHandler<GetInvoicesQuery, PagedResult<InvoiceSummaryDto>>
{
    private readonly StoreHubDbContext _dbContext;

    public GetInvoicesQueryHandler(StoreHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<InvoiceSummaryDto>> HandleAsync(GetInvoicesQuery query, CancellationToken cancellationToken = default)
    {
        var fromDateUtc = query.FromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toDateUtc = query.ToDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var baseQuery = _dbContext.Invoices
            .Where(i => i.CreatedUtc >= fromDateUtc && i.CreatedUtc <= toDateUtc)
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

        return new PagedResult<InvoiceSummaryDto>(
            Items: invoices,
            TotalCount: totalCount,
            Page: query.Page,
            PageSize: query.PageSize);
    }
}
