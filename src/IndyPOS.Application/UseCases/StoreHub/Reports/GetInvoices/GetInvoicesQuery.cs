using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Reports.GetInvoices;

/// <summary>
/// Query to get paginated list of invoices for a date range.
/// </summary>
public record GetInvoicesQuery(
    DateOnly FromDate,
    DateOnly ToDate,
    int Page = 1,
    int PageSize = 50) : IQuery<PagedResult<InvoiceSummaryDto>>;
