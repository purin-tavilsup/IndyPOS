using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetLegacyPaymentsSummary;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Nokpirab;

namespace IndyPOS.Infrastructure.QueryHandlers.Reports;

/// <summary>
/// Handler for GetLegacyPaymentsSummaryQuery.
/// Returns payments summary in the legacy format expected by WinForms UI.
/// </summary>
public class GetLegacyPaymentsSummaryQueryHandler : IQueryHandler<GetLegacyPaymentsSummaryQuery, PaymentsSummary>
{
    private readonly StoreHubDbContext _dbContext;

    public GetLegacyPaymentsSummaryQueryHandler(StoreHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaymentsSummary> HandleAsync(GetLegacyPaymentsSummaryQuery query, CancellationToken cancellationToken = default)
    {
        var fromDateUtc = query.FromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toDateUtc = query.ToDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        // Get all payments in the date range
        var payments = await _dbContext.Payments
            .Where(p => p.CreatedUtc >= fromDateUtc && p.CreatedUtc <= toDateUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

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
