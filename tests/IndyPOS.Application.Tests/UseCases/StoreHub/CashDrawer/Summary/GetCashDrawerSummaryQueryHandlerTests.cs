using FluentAssertions;
using IndyPOS.Application.Common.Constants;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Summary;
using IndyPOS.Application.UseCases.StoreHub.Reports;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetLegacySalesSummary;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using IndyPOS.Infrastructure.QueryHandlers.CashDrawer;
using Moq;
using Nokpirab;
using Xunit;
using Payment = IndyPOS.Domain.Entities.Core.Payment;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.CashDrawer.Summary;

public class GetCashDrawerSummaryQueryHandlerTests
{
    /// <summary>
    /// What the legacy sales summary reports as non-PayLater takings. No test seeds a payment this
    /// big, so a handler still reading it as cash shows up in every expected-cash assertion.
    /// </summary>
    private const decimal NonPayLaterTakings = 5_000m;

    private const decimal CashPaid = 700m;
    private const decimal FiftyFiftyPaid = 250m;
    private const decimal PayLaterPaid = 400m;
    private const decimal CardPaid = 90m;
    private const string UncataloguedCode = "Card";

    /// <summary>23:59 Bangkok on <see cref="CashDrawerTestContext.Yesterday"/>.</summary>
    private static readonly DateTime JustBeforeMidnightYesterday = new(2026, 9, 25, 16, 59, 0, DateTimeKind.Utc);

    /// <summary>10:00 Bangkok on <see cref="CashDrawerTestContext.Yesterday"/>.</summary>
    private static readonly DateTime TenAmBangkokYesterday = new(2026, 9, 25, 3, 0, 0, DateTimeKind.Utc);

    private static GetCashDrawerSummaryQueryHandler HandlerFor(
        CashDrawerTestContext c,
        out Mock<IQueryHandler<GetLegacySalesSummaryQuery, SalesSummary>> sales)
    {
        sales = new Mock<IQueryHandler<GetLegacySalesSummaryQuery, SalesSummary>>();
        sales.Setup(h => h.HandleAsync(It.IsAny<GetLegacySalesSummaryQuery>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new SalesSummary { InvoiceTotalWithoutPayLaterPayments = NonPayLaterTakings });

        return new GetCashDrawerSummaryQueryHandler(
            sales.Object, c.Db, c.CountRepository(), c.Clock, c.StoreIdentity);
    }

    private static GetCashDrawerSummaryQueryHandler HandlerFor(CashDrawerTestContext c) =>
        HandlerFor(c, out _);

    private static async Task SeedPaymentAsync(CashDrawerTestContext c, string method, decimal amount, DateTime? createdUtc = null)
    {
        var at = createdUtc ?? CashDrawerTestContext.TenAmBangkok.UtcDateTime;
        var invoiceId = Guid.NewGuid();
        c.Db.Invoices.Add(new Invoice
        {
            Id = invoiceId, StoreId = c.StoreIdentity.StoreId, UserId = CashDrawerTestContext.CashierId,
            TotalAmount = amount, CreatedUtc = at, LastModifiedUtc = at,
            Payments = [new Payment { Id = Guid.NewGuid(), InvoiceId = invoiceId, Method = method, Amount = amount, CreatedUtc = at }]
        });
        await c.Db.SaveChangesAsync();
    }

    private static async Task SeedMethodAsync(CashDrawerTestContext c, string code, string displayName, bool isEnabled, int displayOrder)
    {
        c.Db.PaymentMethods.Add(new PaymentMethod
        {
            StoreId = c.StoreIdentity.StoreId, Code = code, DisplayName = displayName,
            IsEnabled = isEnabled, DisplayOrder = displayOrder
        });
        await c.Db.SaveChangesAsync();
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
    public async Task HandleAsync_WithFiftyFiftyPayment_DoesNotChangeExpectedCash()
    {
        await using var c = new CashDrawerTestContext();
        await SeedPaymentAsync(c, PaymentMethodCodes.Cash, CashPaid);
        await SeedPaymentAsync(c, PaymentMethodCodes.FiftyFifty, FiftyFiftyPaid);

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(null));

        result.ExpectedCash.Should()
                           .Be(CashPaid);
    }

    [Fact]
    public async Task HandleAsync_WithPayLaterPayment_DoesNotChangeExpectedCash()
    {
        await using var c = new CashDrawerTestContext();
        await SeedPaymentAsync(c, PaymentMethodCodes.PayLater, PayLaterPaid);

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(null));

        result.ExpectedCash.Should()
                           .Be(0m);
    }

    [Fact]
    public async Task HandleAsync_WithPayLaterPayment_ExcludesItFromNonCashPayments()
    {
        await using var c = new CashDrawerTestContext();
        await SeedMethodAsync(c, PaymentMethodCodes.PayLater, "ลงบัญชี", isEnabled: true, displayOrder: 1);
        await SeedPaymentAsync(c, PaymentMethodCodes.PayLater, PayLaterPaid);

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(null));

        result.NonCashPayments.Should()
                              .NotContain(p => p.Code == PaymentMethodCodes.PayLater);
    }

    [Fact]
    public async Task HandleAsync_WithCashPaidJustBeforeMidnight_ExcludesItFromToday()
    {
        await using var c = new CashDrawerTestContext();
        await SeedPaymentAsync(c, PaymentMethodCodes.Cash, CashPaid, JustBeforeMidnightYesterday);

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(null));

        result.CashPaymentsTotal.Should()
                                .Be(0m);
    }

    [Fact]
    public async Task HandleAsync_WithLowerCaseCashCode_CountsItAsCash()
    {
        await using var c = new CashDrawerTestContext();
        await SeedPaymentAsync(c, "cash", CashPaid);

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(null));

        result.CashPaymentsTotal.Should()
                                .Be(CashPaid);
    }

    [Fact]
    public async Task HandleAsync_WithLowerCaseCashCode_ExcludesItFromNonCashPayments()
    {
        await using var c = new CashDrawerTestContext();
        await SeedPaymentAsync(c, "cash", CashPaid);

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(null));

        result.NonCashPayments.Should()
                              .BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WithUncataloguedCode_ListsItAsNonCash()
    {
        await using var c = new CashDrawerTestContext();
        await SeedPaymentAsync(c, UncataloguedCode, CardPaid);

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(null));

        result.NonCashPayments.Should()
                              .Equal(new PaymentMethodTotalDto(UncataloguedCode, UncataloguedCode, CardPaid));
    }

    [Fact]
    public async Task HandleAsync_WithUncataloguedCode_DoesNotCountItAsCash()
    {
        await using var c = new CashDrawerTestContext();
        await SeedPaymentAsync(c, UncataloguedCode, CardPaid);

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(null));

        result.CashPaymentsTotal.Should()
                                .Be(0m);
    }

    [Fact]
    public async Task HandleAsync_WithNoPaymentsToday_ReturnsEnabledNonCashMethodsAtZero()
    {
        await using var c = new CashDrawerTestContext();
        await SeedMethodAsync(c, PaymentMethodCodes.Cash, "เงินสด", isEnabled: true, displayOrder: 1);
        await SeedMethodAsync(c, PaymentMethodCodes.MoneyTransfer, "โอนเงิน", isEnabled: true, displayOrder: 2);
        await SeedMethodAsync(c, PaymentMethodCodes.WeWin, "เราชนะ", isEnabled: false, displayOrder: 3);

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(null));

        result.NonCashPayments.Should()
                              .Equal(new PaymentMethodTotalDto(PaymentMethodCodes.MoneyTransfer, "โอนเงิน", 0m));
    }

    [Fact]
    public async Task HandleAsync_WithCashPayment_AddsItToExpectedCash()
    {
        await using var c = new CashDrawerTestContext();
        await SeedPaymentAsync(c, PaymentMethodCodes.Cash, CashPaid);

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(null));

        result.ExpectedCash.Should()
                           .Be(CashPaid);
    }

    [Fact]
    public async Task HandleAsync_WithFiftyFiftyPayment_ListsItAsNonCash()
    {
        await using var c = new CashDrawerTestContext();
        await SeedMethodAsync(c, PaymentMethodCodes.FiftyFifty, "คนละครึ่ง", isEnabled: true, displayOrder: 1);
        await SeedPaymentAsync(c, PaymentMethodCodes.FiftyFifty, FiftyFiftyPaid);

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(null));

        result.NonCashPayments.Should()
                              .Equal(new PaymentMethodTotalDto(PaymentMethodCodes.FiftyFifty, "คนละครึ่ง", FiftyFiftyPaid));
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
    public async Task HandleAsync_WithTwoCounts_UsesOnlyTheLatestCount()
    {
        await using var c = new CashDrawerTestContext();
        await AddCountAsync(c, new AddCashCountRequest(9, 0, 0, 0, 0, 0, 0, 0, 0));   // 9,000 at 10:00
        c.Time.Advance(TimeSpan.FromHours(10));
        await AddCountAsync(c, new AddCashCountRequest(5, 0, 0, 0, 0, 0, 0, 0, 0));   // 5,000 at 20:00

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(null));

        result.CountedCash.Should()
                          .Be(5_000m);
    }

    [Fact]
    public async Task HandleAsync_WithTwoCounts_DiffsAgainstOnlyTheLatestCount()
    {
        await using var c = new CashDrawerTestContext();
        await AddCountAsync(c, new AddCashCountRequest(9, 0, 0, 0, 0, 0, 0, 0, 0));   // 9,000 at 10:00
        c.Time.Advance(TimeSpan.FromHours(10));
        await AddCountAsync(c, new AddCashCountRequest(5, 0, 0, 0, 0, 0, 0, 0, 0));   // 5,000 at 20:00

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(null));

        result.CashDifference.Should()
                             .Be(5_000m - 0m);
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
    public async Task HandleAsync_WithPastDate_AsksForThatDaysSales()
    {
        await using var c = new CashDrawerTestContext();
        var handler = HandlerFor(c, out var sales);

        await handler.HandleAsync(new GetCashDrawerSummaryQuery(CashDrawerTestContext.Yesterday));

        sales.Verify(h => h.HandleAsync(
            It.Is<GetLegacySalesSummaryQuery>(q => q.FromDate == CashDrawerTestContext.Yesterday && q.ToDate == CashDrawerTestContext.Yesterday),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task HandleAsync_WithPastDate_CountsThatDaysCash()
    {
        await using var c = new CashDrawerTestContext();
        await SeedPaymentAsync(c, PaymentMethodCodes.Cash, CashPaid, TenAmBangkokYesterday);

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(CashDrawerTestContext.Yesterday));

        result.CashPaymentsTotal.Should()
                                .Be(CashPaid);
    }

    [Fact]
    public async Task HandleAsync_WithPayoutsInBothCategories_SplitsTheGeneralTotal()
    {
        await using var c = new CashDrawerTestContext();
        await AddPayoutAsync(c, 100m, PayoutCategory.General);
        await AddPayoutAsync(c, 250m, PayoutCategory.Hardware);

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(null));

        result.GeneralPayoutsTotal.Should()
                                  .Be(100m);
    }

    [Fact]
    public async Task HandleAsync_WithPayoutsInBothCategories_SplitsTheHardwareTotal()
    {
        await using var c = new CashDrawerTestContext();
        await AddPayoutAsync(c, 100m, PayoutCategory.General);
        await AddPayoutAsync(c, 250m, PayoutCategory.Hardware);

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(null));

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
                           .Be(-100m);
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
                           .Be(1_000m);
    }

    [Fact]
    public async Task HandleAsync_WithDebtRepayment_AddsItToExpectedCash()
    {
        await using var c = new CashDrawerTestContext();
        await AddDebtRepaymentAsync(c, 300m);

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(null));

        result.ExpectedCash.Should()
                           .Be(300m);
    }
}
