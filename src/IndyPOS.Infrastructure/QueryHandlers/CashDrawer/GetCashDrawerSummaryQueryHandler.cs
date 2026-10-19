using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Constants;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Summary;
using IndyPOS.Application.UseCases.StoreHub.Reports;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetLegacySalesSummary;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Domain.Enums;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using IndyPOS.Infrastructure.QueryHandlers.Reports;
using Microsoft.EntityFrameworkCore;
using Nokpirab;
using Payment = IndyPOS.Domain.Entities.Core.Payment;

namespace IndyPOS.Infrastructure.QueryHandlers.CashDrawer;

/// <summary>
/// Gathers the day's sales, payments, hand-typed inputs and latest count, then applies the cash
/// formula. The drawer holds only Cash-method takings (spec 2026-10-07 §4.3), so every other method
/// is listed, never added. Soft-deleted entries drop out through the global "SoftDelete" filter.
/// </summary>
public class GetCashDrawerSummaryQueryHandler(
    IQueryHandler<GetLegacySalesSummaryQuery, SalesSummary> salesSummary,
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
        var payments = await PaymentsAsync(day, cancellationToken);
        var payouts = await db.CashPayouts.Where(p => p.StoreId == storeId && p.BusinessDate == day)
                              .Select(p => new { p.Category, p.Amount }).ToListAsync(cancellationToken);
        var floatsTotal = await db.CashFloats.Where(f => f.StoreId == storeId && f.BusinessDate == day)
                                  .SumAsync(f => f.Amount, cancellationToken);
        var repaymentsTotal = await db.DebtRepayments.Where(r => r.StoreId == storeId && r.BusinessDate == day)
                                      .SumAsync(r => r.Amount, cancellationToken);
        var latest = await counts.GetLatestAsync(storeId, day, cancellationToken);

        var payoutsTotal = payouts.Sum(p => p.Amount);
        var expected = CashDrawerCalculator.ExpectedCash(new CashDrawerTotals(
            payments.Cash, repaymentsTotal, floatsTotal, payoutsTotal));
        var counted = latest?.CountedTotal;

        return new CashDrawerSummaryDto
        {
            BusinessDate = day,
            IsEditable = day == today,
            GeneralProductsTotal = sales.GeneralProductsTotal,
            HardwareProductsTotal = sales.HardwareProductsTotal,
            CashPaymentsTotal = payments.Cash,
            NonCashPayments = payments.NonCash,
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

    // PayLater is credit, not a tender the cashier holds: it keeps its own general/hardware totals.
    private async Task<(decimal Cash, IReadOnlyList<PaymentMethodTotalDto> NonCash)> PaymentsAsync(
        DateOnly day, CancellationToken cancellationToken)
    {
        var range = ReportDateRange.ToUtcRange(day, day, storeIdentity.TimeZone);
        var payments = await db.Payments.AsNoTracking()
                               .Include(p => p.Invoice)
                               .Where(p => p.CreatedUtc >= range.StartUtc && p.CreatedUtc < range.EndExclusiveUtc)
                               .ToListAsync(cancellationToken);
        var catalogue = await db.PaymentMethods.AsNoTracking().ToListAsync(cancellationToken);

        var cash = payments.GroupBy(p => p.InvoiceId).Sum(CashKept);
        var nonCash = PaymentMethodTotals.Build(catalogue, payments)
                                         .Where(t => !IsCode(t.Code, PaymentMethodCodes.Cash) && !IsCode(t.Code, PaymentMethodCodes.PayLater))
                                         .ToList();
        return (cash, nonCash);
    }

    // A payment stores what the customer handed over, and the change always leaves the drawer, even
    // when the bill was mostly paid another way. SaleFigures is the till's own change rule.
    private static decimal CashKept(IGrouping<Guid, Payment> invoicePayments)
    {
        var invoice = invoicePayments.First().Invoice;
        var figures = SaleFigures.From(invoice.TotalAmount, invoicePayments.Select(p => new SalePayment(p.Method, p.Amount)));

        // Case-insensitive, as a sale accepts its codes: it stores the caller's spelling, so "cash" is Cash.
        var cashTendered = invoicePayments.Where(p => IsCode(p.Method, PaymentMethodCodes.Cash)).Sum(p => p.Amount);
        return cashTendered - figures.ChangeGiven;
    }

    private static bool IsCode(string method, string code) =>
        string.Equals(method, code, StringComparison.OrdinalIgnoreCase);
}
