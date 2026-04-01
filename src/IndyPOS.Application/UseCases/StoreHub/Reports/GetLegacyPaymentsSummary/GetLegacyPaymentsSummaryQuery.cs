using IndyPOS.Application.Common.Models;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Reports.GetLegacyPaymentsSummary;

/// <summary>
/// Query to get payments summary in legacy format (for WinForms compatibility).
/// </summary>
public record GetLegacyPaymentsSummaryQuery(
    DateOnly FromDate,
    DateOnly ToDate) : IQuery<PaymentsSummary>;
