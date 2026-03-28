using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Reports.GetPayLaterReport;

/// <summary>
/// Query to get PayLater (accounts receivable) report.
/// Returns outstanding balances grouped by customer.
/// </summary>
public record GetPayLaterReportQuery(
    bool IncludeCompleted = false,
    int Page = 1,
    int PageSize = 50) : IQuery<PayLaterReportDto>;

/// <summary>
/// PayLater report result with summary and details.
/// </summary>
public record PayLaterReportDto(
    decimal TotalOutstanding,
    decimal TotalPaid,
    int ActiveCustomers,
    int CompletedCount,
    PagedResult<PayLaterSummaryDto> Customers);
