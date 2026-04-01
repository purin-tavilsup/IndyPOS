using IndyPOS.Application.Common.Models;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Reports.GetLegacySalesSummary;

/// <summary>
/// Query to get sales summary in legacy format (for WinForms compatibility).
/// </summary>
public record GetLegacySalesSummaryQuery(
    DateOnly FromDate,
    DateOnly ToDate) : IQuery<SalesSummary>;
