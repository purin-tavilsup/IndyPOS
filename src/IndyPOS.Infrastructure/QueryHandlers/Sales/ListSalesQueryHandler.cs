using IndyPOS.Application.Common.Authorization;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using IndyPOS.Infrastructure.QueryHandlers.Reports;
using Microsoft.EntityFrameworkCore;
using Nokpirab;

namespace IndyPOS.Infrastructure.QueryHandlers.Sales;

/// <summary>
/// This store's bills, newest first, one page at a time. Reads one row more than the page to answer
/// "is there more?" without a COUNT.
/// </summary>
public class ListSalesQueryHandler(
    StoreHubDbContext db,
    IStoreIdentityService storeIdentity,
    ICashDrawerClock clock) : IQueryHandler<ListSalesQuery, SalesPage>
{
    public async Task<SalesPage> HandleAsync(ListSalesQuery query, CancellationToken cancellationToken = default)
    {
        var today = clock.Now().BusinessDate;
        var from = query.From ?? today;
        var to = query.To ?? today;

        // Malformed input is a 400 before the today-only rule is a 403.
        SalesQueryRules.EnsureValidRange(from, to);
        SalesQueryRules.EnsureValidPage(query.Page, query.PageSize);
        TodayOnlyRule.EnsureAllowed(from, today, query.CanViewAnyDay);
        TodayOnlyRule.EnsureAllowed(to, today, query.CanViewAnyDay);

        var range = ReportDateRange.ToUtcRange(from, to, storeIdentity.TimeZone);
        var rows = await FetchPageAndOneMoreAsync(query, range, cancellationToken);

        return new SalesPage(rows.Take(query.PageSize).ToList(), query.Page, query.PageSize, HasMore: rows.Count > query.PageSize);
    }

    private Task<List<InvoiceSummaryDto>> FetchPageAndOneMoreAsync(
        ListSalesQuery query, UtcDateTimeRange range, CancellationToken cancellationToken) =>
        db.Invoices
          .AsNoTracking()
          .Where(i => i.StoreId == storeIdentity.StoreId
                      && i.CreatedUtc >= range.StartUtc
                      && i.CreatedUtc < range.EndExclusiveUtc)
          .OrderByDescending(i => i.CreatedUtc)
          .ThenByDescending(i => i.InvoiceNumber)   // stable paging for bills in the same instant
          .Skip((query.Page - 1) * query.PageSize)
          .Take(query.PageSize + 1)
          .Select(i => new InvoiceSummaryDto(
              i.Id,
              i.InvoiceNumber,
              i.CreatedUtc,
              i.TotalAmount,
              i.Payments.OrderByDescending(p => p.Amount).Select(p => p.Method).FirstOrDefault() ?? "Unknown",
              i.Lines.Count))
          .ToListAsync(cancellationToken);
}
