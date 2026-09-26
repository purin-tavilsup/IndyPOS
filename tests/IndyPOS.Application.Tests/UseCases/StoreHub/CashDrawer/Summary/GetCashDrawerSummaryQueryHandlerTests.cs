using FluentAssertions;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Summary;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetLegacyPaymentsSummary;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetLegacySalesSummary;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using IndyPOS.Infrastructure.QueryHandlers.CashDrawer;
using Moq;
using Nokpirab;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.CashDrawer.Summary;

public class GetCashDrawerSummaryQueryHandlerTests
{
    private const decimal CashSales = 5_000m;

    private static GetCashDrawerSummaryQueryHandler HandlerFor(CashDrawerTestContext c)
    {
        var sales = new Mock<IQueryHandler<GetLegacySalesSummaryQuery, SalesSummary>>();
        sales.Setup(h => h.HandleAsync(It.IsAny<GetLegacySalesSummaryQuery>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new SalesSummary { InvoiceTotalWithoutPayLaterPayments = CashSales });
        var payments = new Mock<IQueryHandler<GetLegacyPaymentsSummaryQuery, PaymentsSummary>>();
        payments.Setup(h => h.HandleAsync(It.IsAny<GetLegacyPaymentsSummaryQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PaymentsSummary());

        return new GetCashDrawerSummaryQueryHandler(
            sales.Object, payments.Object, c.Db, c.CountRepository(), c.Clock, c.StoreIdentity);
    }

    private static Task AddCountAsync(CashDrawerTestContext c, AddCashCountRequest counts) =>
        new AddCashCountCommandHandler(c.CountRepository(), c.Clock, c.StoreIdentity)
            .HandleAsync(new AddCashCountCommand(CashDrawerTestContext.CashierId, counts));

    private static async Task AddPayoutAsync(CashDrawerTestContext c, decimal amount, PayoutCategory category, bool isDeleted = false)
    {
        c.Db.CashPayouts.Add(new CashPayout
        {
            Id = Guid.NewGuid(), StoreId = c.StoreIdentity.StoreId, Amount = amount, Category = category,
            BusinessDate = CashDrawerTestContext.Today, CreatedByUserId = CashDrawerTestContext.CashierId,
            IsDeleted = isDeleted
        });
        await c.Db.SaveChangesAsync();
    }

    private static async Task AddCashFloatAsync(CashDrawerTestContext c, decimal amount, bool isDeleted = false)
    {
        c.Db.CashFloats.Add(new CashFloat
        {
            Id = Guid.NewGuid(), StoreId = c.StoreIdentity.StoreId, Amount = amount,
            BusinessDate = CashDrawerTestContext.Today, CreatedByUserId = CashDrawerTestContext.CashierId,
            IsDeleted = isDeleted
        });
        await c.Db.SaveChangesAsync();
    }

    private static async Task AddDebtRepaymentAsync(CashDrawerTestContext c, decimal amount, bool isDeleted = false)
    {
        c.Db.DebtRepayments.Add(new DebtRepayment
        {
            Id = Guid.NewGuid(), StoreId = c.StoreIdentity.StoreId, Amount = amount,
            BusinessDate = CashDrawerTestContext.Today, CreatedByUserId = CashDrawerTestContext.CashierId,
            CustomerName = "ลุงสมชาย", IsDeleted = isDeleted
        });
        await c.Db.SaveChangesAsync();
    }

    [Fact]
    public async Task HandleAsync_WithNoCountToday_ReturnsNullDifference()
    {
        await using var c = new CashDrawerTestContext();

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(null));

        result.CashDifference.Should()
                             .BeNull();
    }

    [Fact]
    public async Task HandleAsync_WithNoCountToday_ReturnsNoLatestCount()
    {
        await using var c = new CashDrawerTestContext();

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(null));

        result.LatestCount.Should()
                          .BeNull();
    }

    [Fact]
    public async Task HandleAsync_WithSoftDeletedPayout_ExcludesItFromTotals()
    {
        await using var c = new CashDrawerTestContext();
        await AddPayoutAsync(c, 100m, PayoutCategory.General);
        await AddPayoutAsync(c, 999m, PayoutCategory.General, isDeleted: true);

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(null));

        result.PayoutsTotal.Should()
                           .Be(100m);
    }

    [Fact]
    public async Task HandleAsync_WithSoftDeletedCashFloat_ExcludesItFromTotals()
    {
        await using var c = new CashDrawerTestContext();
        await AddCashFloatAsync(c, 100m);
        await AddCashFloatAsync(c, 999m, isDeleted: true);

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(null));

        result.CashFloatsTotal.Should()
                              .Be(100m);
    }

    [Fact]
    public async Task HandleAsync_WithTwoCounts_UsesOnlyTheLatest()
    {
        await using var c = new CashDrawerTestContext();
        await AddCountAsync(c, new AddCashCountRequest(9, 0, 0, 0, 0, 0, 0, 0, 0));   // 9,000 at 10:00
        c.Time.Advance(TimeSpan.FromHours(10));
        await AddCountAsync(c, new AddCashCountRequest(5, 0, 0, 0, 0, 0, 0, 0, 0));   // 5,000 at 20:00

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(null));

        result.CountedCash.Should()
                          .Be(5_000m);
        result.CashDifference.Should()
                             .Be(5_000m - CashSales);
    }

    [Fact]
    public async Task HandleAsync_WithPastDate_ReturnsNotEditable()
    {
        await using var c = new CashDrawerTestContext();

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(CashDrawerTestContext.Yesterday));

        result.IsEditable.Should()
                         .BeFalse();
    }

    [Fact]
    public async Task HandleAsync_WithPayoutsInBothCategories_SplitsTheirTotals()
    {
        await using var c = new CashDrawerTestContext();
        await AddPayoutAsync(c, 100m, PayoutCategory.General);
        await AddPayoutAsync(c, 250m, PayoutCategory.Hardware);

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(null));

        result.GeneralPayoutsTotal.Should()
                                  .Be(100m);
        result.HardwarePayoutsTotal.Should()
                                   .Be(250m);
    }

    [Fact]
    public async Task HandleAsync_WithPayouts_SubtractsThemFromExpectedCash()
    {
        await using var c = new CashDrawerTestContext();
        await AddPayoutAsync(c, 100m, PayoutCategory.General);

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(null));

        result.ExpectedCash.Should()
                           .Be(CashSales - 100m);
    }

    [Fact]
    public async Task HandleAsync_WithoutDate_ReturnsTodaysDate()
    {
        await using var c = new CashDrawerTestContext();

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(null));

        result.BusinessDate.Should()
                           .Be(CashDrawerTestContext.Today);
    }

    [Fact]
    public async Task HandleAsync_WithoutDate_ReturnsEditable()
    {
        await using var c = new CashDrawerTestContext();

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(null));

        result.IsEditable.Should()
                         .BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WithCashFloat_AddsItToExpectedCash()
    {
        await using var c = new CashDrawerTestContext();
        await AddCashFloatAsync(c, 1_000m);

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(null));

        result.ExpectedCash.Should()
                           .Be(CashSales + 1_000m);
    }

    [Fact]
    public async Task HandleAsync_WithDebtRepayment_AddsItToExpectedCash()
    {
        await using var c = new CashDrawerTestContext();
        await AddDebtRepaymentAsync(c, 300m);

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(null));

        result.ExpectedCash.Should()
                           .Be(CashSales + 300m);
    }
}
