using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Summary;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetLegacyPaymentsSummary;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetLegacySalesSummary;
using IndyPOS.Domain.Enums;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Nokpirab;

namespace IndyPOS.Infrastructure.QueryHandlers.CashDrawer;

/// <summary>
/// Gathers the day's sales, payments, hand-typed inputs and latest count, then applies the cash
/// formula. Soft-deleted entries drop out through the global "SoftDelete" filter.
/// </summary>
public class GetCashDrawerSummaryQueryHandler(
    IQueryHandler<GetLegacySalesSummaryQuery, SalesSummary> salesSummary,
    IQueryHandler<GetLegacyPaymentsSummaryQuery, PaymentsSummary> paymentsSummary,
    StoreHubDbContext db,
    ICashCountRepository counts,
    ICashDrawerClock clock,
    IStoreIdentityService storeIdentity) : IQueryHandler<GetCashDrawerSummaryQuery, CashDrawerSummaryDto>
{
    public async Task<CashDrawerSummaryDto> HandleAsync(GetCashDrawerSummaryQuery query, CancellationToken cancellationToken = default)
    {
        var today = clock.Now().BusinessDate;
        var day = query.BusinessDate ?? today;
        var storeId = storeIdentity.StoreId;

        var sales = await salesSummary.HandleAsync(new GetLegacySalesSummaryQuery(day, day), cancellationToken);
        var payments = await paymentsSummary.HandleAsync(new GetLegacyPaymentsSummaryQuery(day, day), cancellationToken);
        var payouts = await db.CashPayouts.Where(p => p.StoreId == storeId && p.BusinessDate == day)
                              .Select(p => new { p.Category, p.Amount }).ToListAsync(cancellationToken);
        var floatsTotal = await db.CashFloats.Where(f => f.StoreId == storeId && f.BusinessDate == day)
                                  .SumAsync(f => f.Amount, cancellationToken);
        var repaymentsTotal = await db.DebtRepayments.Where(r => r.StoreId == storeId && r.BusinessDate == day)
                                      .SumAsync(r => r.Amount, cancellationToken);
        var latest = await counts.GetLatestAsync(storeId, day, cancellationToken);

        var payoutsTotal = payouts.Sum(p => p.Amount);
        var expected = CashDrawerCalculator.ExpectedCash(new CashDrawerTotals(
            sales.InvoiceTotalWithoutPayLaterPayments, repaymentsTotal, floatsTotal,
            payments.MoneyTransferTotal, payments.WelfareCardTotal, payoutsTotal));
        var counted = latest?.CountedTotal;

        return new CashDrawerSummaryDto
        {
            BusinessDate = day,
            IsEditable = day == today,
            GeneralProductsTotal = sales.GeneralProductsTotal,
            HardwareProductsTotal = sales.HardwareProductsTotal,
            CashSalesTotal = sales.InvoiceTotalWithoutPayLaterPayments,
            MoneyTransferTotal = payments.MoneyTransferTotal,
            WelfareCardTotal = payments.WelfareCardTotal,
            PayLaterGeneralProductsTotal = sales.PayLaterPaymentsTotalForGeneralProducts,
            PayLaterHardwareProductsTotal = sales.PayLaterPaymentsTotalForHardwareProducts,
            PayoutsTotal = payoutsTotal,
            GeneralPayoutsTotal = payouts.Where(p => p.Category == PayoutCategory.General).Sum(p => p.Amount),
            HardwarePayoutsTotal = payouts.Where(p => p.Category == PayoutCategory.Hardware).Sum(p => p.Amount),
            CashFloatsTotal = floatsTotal,
            DebtRepaymentsTotal = repaymentsTotal,
            ExpectedCash = expected,
            LatestCount = latest?.ToDto(),
            CountedCash = counted,
            CashDifference = CashDrawerCalculator.Difference(counted, expected)
        };
    }
}
