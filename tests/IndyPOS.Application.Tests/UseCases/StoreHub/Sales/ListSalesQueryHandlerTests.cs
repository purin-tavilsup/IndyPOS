using FluentAssertions;
using IndyPOS.Application.Common.Constants;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using Xunit;
using static IndyPOS.Application.Tests.UseCases.StoreHub.Sales.SalesHistoryTestContext;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.Sales;

public class ListSalesQueryHandlerTests
{
    private const int PageSizeOfOne = 1;

    private static ListSalesQuery TodayOnly(DateOnly? from = null, DateOnly? to = null, int pageSize = SalesQueryRules.DefaultPageSize) =>
        new(from, to, SalesQueryRules.FirstPage, pageSize, CanViewAnyDay: false);

    [Fact]
    public async Task Handle_WithToBeforeFrom_ThrowsValidation()
    {
        await using var c = new SalesHistoryTestContext();

        var act = () => c.ListHandler().HandleAsync(TodayOnly(from: Today, to: Yesterday) with { CanViewAnyDay = true });

        await act.Should()
                 .ThrowAsync<SalesQueryValidationException>();
    }

    [Fact]
    public async Task Handle_WithPageSizeAboveMaximum_ThrowsValidation()
    {
        await using var c = new SalesHistoryTestContext();

        var act = () => c.ListHandler().HandleAsync(TodayOnly(pageSize: SalesQueryRules.MaxPageSize + 1));

        await act.Should()
                 .ThrowAsync<SalesQueryValidationException>();
    }

    [Fact]
    public async Task Handle_AsTodayOnlyCallerForYesterday_ThrowsForbidden()
    {
        await using var c = new SalesHistoryTestContext();

        var act = () => c.ListHandler().HandleAsync(TodayOnly(from: Yesterday, to: Yesterday));

        await act.Should()
                 .ThrowAsync<OtherDayForbiddenException>();
    }

    [Fact]
    public async Task Handle_AsTodayOnlyCallerWithARangeReachingBackToYesterday_ThrowsForbidden()
    {
        await using var c = new SalesHistoryTestContext();

        var act = () => c.ListHandler().HandleAsync(TodayOnly(from: Yesterday, to: Today));

        await act.Should()
                 .ThrowAsync<OtherDayForbiddenException>();
    }

    [Fact]
    public async Task Handle_WithoutDates_ExcludesYesterdaysBill()
    {
        await using var c = new SalesHistoryTestContext();
        await c.SeedInvoiceAsync(new InvoiceSeed(1001, NineAmYesterdayUtc));

        var page = await c.ListHandler().HandleAsync(TodayOnly());

        page.Items.Should()
                  .BeEmpty();
    }

    [Fact]
    public async Task Handle_WithABillJustAfterBangkokMidnight_IncludesItInToday()
    {
        await using var c = new SalesHistoryTestContext();
        await c.SeedInvoiceAsync(new InvoiceSeed(1001, JustAfterMidnightTodayUtc));

        var page = await c.ListHandler().HandleAsync(TodayOnly());

        page.Items.Should()
                  .ContainSingle(i => i.InvoiceNumber == 1001);
    }

    [Fact]
    public async Task Handle_WithAnotherStoresBillToday_ExcludesIt()
    {
        await using var c = new SalesHistoryTestContext();
        await c.SeedInvoiceAsync(new InvoiceSeed(1001, NineAmTodayUtc) { StoreId = OtherStoreId });

        var page = await c.ListHandler().HandleAsync(TodayOnly());

        page.Items.Should()
                  .BeEmpty();
    }

    [Fact]
    public async Task Handle_WithExactlyOnePageOfBills_ReportsNoMore()
    {
        await using var c = new SalesHistoryTestContext();
        await c.SeedInvoiceAsync(new InvoiceSeed(1001, NineAmTodayUtc));

        var page = await c.ListHandler().HandleAsync(TodayOnly(pageSize: PageSizeOfOne));

        page.HasMore.Should()
                    .BeFalse();
    }

    [Fact]
    public async Task Handle_WithMoreBillsThanThePage_ReportsHasMore()
    {
        await using var c = new SalesHistoryTestContext();
        await c.SeedInvoiceAsync(new InvoiceSeed(1001, NineAmTodayUtc));
        await c.SeedInvoiceAsync(new InvoiceSeed(1002, HalfPastNineTodayUtc));

        var page = await c.ListHandler().HandleAsync(TodayOnly(pageSize: PageSizeOfOne));

        page.HasMore.Should()
                    .BeTrue();
    }

    [Fact]
    public async Task Handle_WithTwoBills_ReturnsTheNewestFirst()
    {
        await using var c = new SalesHistoryTestContext();
        await c.SeedInvoiceAsync(new InvoiceSeed(1001, NineAmTodayUtc));
        await c.SeedInvoiceAsync(new InvoiceSeed(1002, HalfPastNineTodayUtc));

        var page = await c.ListHandler().HandleAsync(TodayOnly());

        page.Items.Select(i => i.InvoiceNumber).Should()
                                               .Equal(1002, 1001);
    }

    [Fact]
    public async Task Handle_AsAnyDayCallerForYesterday_ReturnsYesterdaysBill()
    {
        await using var c = new SalesHistoryTestContext();
        await c.SeedInvoiceAsync(new InvoiceSeed(1001, NineAmYesterdayUtc));

        var page = await c.ListHandler().HandleAsync(TodayOnly(from: Yesterday, to: Yesterday) with { CanViewAnyDay = true });

        page.Items.Should()
                  .ContainSingle(i => i.InvoiceNumber == 1001);
    }

    [Fact]
    public async Task Handle_OnTheSecondPage_ReturnsTheOlderBill()
    {
        await using var c = new SalesHistoryTestContext();
        await c.SeedInvoiceAsync(new InvoiceSeed(1001, NineAmTodayUtc));
        await c.SeedInvoiceAsync(new InvoiceSeed(1002, HalfPastNineTodayUtc));
        var secondPage = TodayOnly(pageSize: PageSizeOfOne) with { Page = 2 };

        var page = await c.ListHandler().HandleAsync(secondPage);

        page.Items.Select(i => i.InvoiceNumber).Should()
                                               .Equal(1001);
    }

    [Fact]
    public async Task Handle_WithTwoEqualPayments_PicksTheMethodThatSortsFirst()
    {
        await using var c = new SalesHistoryTestContext();
        await c.SeedInvoiceAsync(new InvoiceSeed(1001, NineAmTodayUtc)
        {
            Payments = [new(PaymentMethodCodes.MoneyTransfer, 175m), new(PaymentMethodCodes.Cash, 175m)]
        });

        var page = await c.ListHandler().HandleAsync(TodayOnly());

        page.Items.Should()
                  .ContainSingle()
                  .Which.PrimaryPaymentMethod.Should()
                                             .Be(PaymentMethodCodes.Cash);
    }
}
