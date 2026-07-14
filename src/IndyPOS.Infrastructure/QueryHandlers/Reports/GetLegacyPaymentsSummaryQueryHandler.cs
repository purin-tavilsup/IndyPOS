using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetLegacyPaymentsSummary;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nokpirab;

namespace IndyPOS.Infrastructure.QueryHandlers.Reports;

/// <summary>
/// Handler for GetLegacyPaymentsSummaryQuery.
/// Returns payments summary in the legacy format expected by WinForms UI.
/// </summary>
public class GetLegacyPaymentsSummaryQueryHandler : IQueryHandler<GetLegacyPaymentsSummaryQuery, PaymentsSummary>
{
    private readonly StoreHubDbContext _dbContext;
    private readonly IStoreIdentityService _storeIdentity;
    private readonly ILogger<GetLegacyPaymentsSummaryQueryHandler> _logger;

    public GetLegacyPaymentsSummaryQueryHandler(
        StoreHubDbContext dbContext,
        IStoreIdentityService storeIdentity,
        ILogger<GetLegacyPaymentsSummaryQueryHandler> logger)
    {
        _dbContext = dbContext;
        _storeIdentity = storeIdentity;
        _logger = logger;
    }

    public async Task<PaymentsSummary> HandleAsync(GetLegacyPaymentsSummaryQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Generating legacy payments summary: FromDate={FromDate}, ToDate={ToDate}, TimeZone={TimeZone}",
            query.FromDate, query.ToDate, _storeIdentity.TimeZone.Id);

        var dateRange = ReportDateRange.ToUtcRange(query.FromDate, query.ToDate, _storeIdentity.TimeZone);

        // Get all payments in the date range
        var payments = await _dbContext.Payments
            .Where(p => p.CreatedUtc >= dateRange.StartUtc && p.CreatedUtc < dateRange.EndExclusiveUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var totalPayments = payments.Sum(p => p.Amount);
        _logger.LogInformation(
            "Legacy payments summary generated: FromDate={FromDate}, ToDate={ToDate}, PaymentCount={Count}, Total={Total:C}",
            query.FromDate, query.ToDate, payments.Count, totalPayments);

        // Group by payment method
        // Legacy payment type names map to method strings
        return new PaymentsSummary
        {
            MoneyTransferTotal = payments
                .Where(p => p.Method.Equals("MoneyTransfer", StringComparison.OrdinalIgnoreCase) ||
                           p.Method.Equals("Transfer", StringComparison.OrdinalIgnoreCase))
                .Sum(p => p.Amount),

            FiftyFiftyTotal = payments
                .Where(p => p.Method.Equals("FiftyFifty", StringComparison.OrdinalIgnoreCase) ||
                           p.Method.Equals("5050", StringComparison.OrdinalIgnoreCase))
                .Sum(p => p.Amount),

            M33WeLoveTotal = payments
                .Where(p => p.Method.Equals("M33WeLove", StringComparison.OrdinalIgnoreCase) ||
                           p.Method.Equals("M33", StringComparison.OrdinalIgnoreCase))
                .Sum(p => p.Amount),

            WeWinTotal = payments
                .Where(p => p.Method.Equals("WeWin", StringComparison.OrdinalIgnoreCase))
                .Sum(p => p.Amount),

            WelfareCardTotal = payments
                .Where(p => p.Method.Equals("WelfareCard", StringComparison.OrdinalIgnoreCase))
                .Sum(p => p.Amount),

            PayLaterTotal = payments
                .Where(p => p.Method.Equals("PayLater", StringComparison.OrdinalIgnoreCase))
                .Sum(p => p.Amount)
        };
    }
}
