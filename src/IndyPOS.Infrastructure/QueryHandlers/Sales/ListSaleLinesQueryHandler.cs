using IndyPOS.Application.Common.Authorization;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using IndyPOS.Infrastructure.QueryHandlers.Reports;
using Microsoft.EntityFrameworkCore;
using Nokpirab;
using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Infrastructure.QueryHandlers.Sales;

/// <summary>
/// Every sold line in the range, oldest bill first, under the same date rules as the bill list. The
/// order is total (bill time, bill number, line priority, line id), so paging never repeats or skips a line.
/// </summary>
public sealed class ListSaleLinesQueryHandler(StoreHubDbContext db, IStoreIdentityService storeIdentity, ICashDrawerClock clock)
    : IQueryHandler<ListSaleLinesQuery, SaleLinesPage>
{
    public async Task<SaleLinesPage> HandleAsync(ListSaleLinesQuery query, CancellationToken cancellationToken = default)
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
        var storeId = storeIdentity.StoreId;

        var rows = await db.Set<InvoiceLine>()
            .AsNoTracking()
            .Where(l => l.Invoice.StoreId == storeId
                     && l.Invoice.CreatedUtc >= range.StartUtc
                     && l.Invoice.CreatedUtc < range.EndExclusiveUtc)
            .OrderBy(l => l.Invoice.CreatedUtc)
            .ThenBy(l => l.Invoice.InvoiceNumber)
            .ThenBy(l => l.Priority)
            .ThenBy(l => l.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize + 1)
            .Select(l => new SaleLineRowDto(
                l.InvoiceId,
                l.Invoice.InvoiceNumber,
                l.Product.Barcode,
                l.ProductName,
                l.Quantity,
                l.UnitPrice,
                l.UnitPrice * l.Quantity,
                l.Product.Category,
                l.Invoice.CreatedUtc,
                l.Note))
            .ToListAsync(cancellationToken);

        return new SaleLinesPage(rows.Take(query.PageSize).ToList(), query.Page, query.PageSize, HasMore: rows.Count > query.PageSize);
    }
}
