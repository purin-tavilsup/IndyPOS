using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Reports.GetSalesSummary;

/// <summary>
/// Query to get sales summary for a date range.
/// </summary>
public record GetSalesSummaryQuery(
    DateOnly FromDate,
    DateOnly ToDate,
    int TopProductsCount = 10) : IQuery<SalesSummaryDto>;
