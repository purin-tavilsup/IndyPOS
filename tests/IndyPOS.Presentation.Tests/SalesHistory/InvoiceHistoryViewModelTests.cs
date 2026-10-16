using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Receipts;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Presentation.SalesHistory;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;

namespace IndyPOS.Presentation.Tests.SalesHistory;

public class InvoiceHistoryViewModelTests
{
    // Now is 10:00 on 2026-10-15 in Bangkok.
    private static readonly DateTimeOffset NowUtc = new(2026, 10, 15, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 15);
    private static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    private static readonly SalesHistoryAccess Cashier = new("Malee Cashier", CanViewAnyDay: false, CanReprint: true);
    private static readonly SalesHistoryAccess Manager = new("Somchai Manager", CanViewAnyDay: true, CanReprint: true);

    private readonly Mock<IStoreHubClient> _client = new();

    [Theory]
    [InlineData("abc")]
    [InlineData("-5")]
    [InlineData("1,042")]
    [InlineData("")]
    public async Task Search_WithTextThatIsNotABillNumber_ShowsNotANumber(string text)
    {
        var vm = History(Manager);
        vm.SearchText = text;

        await vm.SearchCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should()
                       .Be(InvoiceHistoryViewModel.NotANumber);
    }

    [Fact]
    public async Task Search_WithTextThatIsNotABillNumber_DoesNotCallStoreHub()
    {
        var vm = History(Manager);
        vm.SearchText = "abc";

        await vm.SearchCommand.ExecuteAsync(null);

        _client.Verify(c => c.GetSaleByNumberAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Search_WhenStoreHubAnswersNotFound_ShowsNumberNotFound()
    {
        _client.Setup(c => c.GetSaleByNumberAsync(1042, It.IsAny<CancellationToken>()))
               .ReturnsAsync((InvoiceDetailDto?)null);
        var vm = History(Cashier);
        vm.SearchText = "1042";

        await vm.SearchCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should()
                       .Be(string.Format(InvoiceHistoryViewModel.NumberNotFound, 1042));
    }

    [Fact]
    public async Task Search_WhenStoreHubFails_ShowsItsReason()
    {
        _client.Setup(c => c.GetSaleByNumberAsync(1042, It.IsAny<CancellationToken>()))
               .ThrowsAsync(new StoreHubException("Cannot connect to StoreHub"));
        var vm = History(Manager);
        vm.SearchText = "1042";

        await vm.SearchCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should()
                       .Be(string.Format(InvoiceHistoryViewModel.SearchFailed, "Cannot connect to StoreHub"));
    }

    [Fact]
    public void ShowThisMonth_ForATodayOnlyUser_CannotExecute()
    {
        var vm = History(Cashier);

        vm.ShowThisMonthCommand.CanExecute(null).Should()
                                                .BeFalse();
    }

    [Fact]
    public void ShowRange_ForATodayOnlyUser_CannotExecute()
    {
        var vm = History(Cashier);

        vm.ShowRangeCommand.CanExecute(new DateRange(Today, Today)).Should()
                                                                   .BeFalse();
    }

    [Fact]
    public async Task ShowRange_WithTheStartAfterTheEnd_ShowsRangeReversed()
    {
        var vm = History(Manager);

        await vm.ShowRangeCommand.ExecuteAsync(new DateRange(Today, Today.AddDays(-1)));

        vm.ErrorMessage.Should()
                       .Be(InvoiceHistoryViewModel.RangeReversed);
    }

    [Fact]
    public async Task ShowRange_WithTheStartAfterTheEnd_DoesNotCallStoreHub()
    {
        var vm = History(Manager);

        await vm.ShowRangeCommand.ExecuteAsync(new DateRange(Today, Today.AddDays(-1)));

        _client.Verify(c => c.ListSalesAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<int>(),
                                             It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ShowToday_WhenStoreHubFails_ShowsItsReason()
    {
        _client.Setup(c => c.ListSalesAsync(Today, Today, 1, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()))
               .ThrowsAsync(new StoreHubException("Cannot connect to StoreHub"));
        var vm = History(Cashier);

        await vm.ShowTodayCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should()
                       .Be(string.Format(InvoiceHistoryViewModel.LoadFailed, "Cannot connect to StoreHub"));
    }

    [Fact]
    public async Task ShowToday_WhenAnEarlierLoadAnswersLast_KeepsTheLaterList()
    {
        var slowMonth = new TaskCompletionSource<SalesPage>();
        _client.Setup(c => c.ListSalesAsync(new DateOnly(2026, 10, 1), Today, 1, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()))
               .Returns(slowMonth.Task);
        _client.Setup(c => c.ListSalesAsync(Today, Today, 1, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()))
               .ReturnsAsync(Page(hasMore: false, 2));
        var vm = History(Manager);
        var monthLoad = vm.ShowThisMonthCommand.ExecuteAsync(null);
        await vm.ShowTodayCommand.ExecuteAsync(null);

        slowMonth.SetResult(Page(hasMore: false, 1));
        await monthLoad;

        vm.Bills.Select(b => b.InvoiceNumber).Should()
                                             .Equal(2);
    }

    // From/To moved to the new period before StoreHub answered, so a failure left the old list under
    // the new dates and "load more" appended the other period's bills.
    [Fact]
    public async Task ShowThisYear_WhenStoreHubFails_KeepsTheShownPeriod()
    {
        var monthStart = new DateOnly(2026, 10, 1);
        _client.Setup(c => c.ListSalesAsync(monthStart, Today, 1, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()))
               .ReturnsAsync(Page(hasMore: true, 3));
        _client.Setup(c => c.ListSalesAsync(new DateOnly(2026, 1, 1), Today, 1, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()))
               .ThrowsAsync(new StoreHubException("Cannot connect to StoreHub"));
        var vm = History(Manager);
        await vm.ShowThisMonthCommand.ExecuteAsync(null);

        await vm.ShowThisYearCommand.ExecuteAsync(null);

        vm.From.Should()
               .Be(monthStart);
    }

    [Fact]
    public async Task LoadMore_WhenANewPeriodArrivesFirst_DropsTheOldPeriodsPage()
    {
        var slowYear = new TaskCompletionSource<SalesPage>();
        var slowPageTwo = new TaskCompletionSource<SalesPage>();
        _client.Setup(c => c.ListSalesAsync(new DateOnly(2026, 10, 1), Today, 1, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()))
               .ReturnsAsync(Page(hasMore: true, 3));
        _client.Setup(c => c.ListSalesAsync(new DateOnly(2026, 1, 1), Today, 1, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()))
               .Returns(slowYear.Task);
        _client.Setup(c => c.ListSalesAsync(It.IsAny<DateOnly>(), Today, 2, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()))
               .Returns(slowPageTwo.Task);
        var vm = History(Manager);
        await vm.ShowThisMonthCommand.ExecuteAsync(null);
        var yearLoad = vm.ShowThisYearCommand.ExecuteAsync(null);
        var pageTwoLoad = vm.LoadMoreCommand.ExecuteAsync(null);

        slowYear.SetResult(Page(hasMore: false, 9));
        await yearLoad;
        slowPageTwo.SetResult(Page(hasMore: false, 2));
        await pageTwoLoad;

        vm.Bills.Select(b => b.InvoiceNumber).Should()
                                             .Equal(9);
    }

    [Fact]
    public async Task Search_WhenFound_ShowsTheBillInTheDetail()
    {
        var bill = Detail(1042);
        _client.Setup(c => c.GetSaleByNumberAsync(1042, It.IsAny<CancellationToken>()))
               .ReturnsAsync(bill);
        var vm = History(Manager);
        vm.SearchText = " 1042 ";

        await vm.SearchCommand.ExecuteAsync(null);

        vm.Detail.Bill.Should()
                      .Be(bill);
    }

    [Fact]
    public async Task ShowThisMonth_ForAManager_AsksFromTheFirstOfTheMonth()
    {
        _client.Setup(c => c.ListSalesAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(Page(hasMore: false));
        var vm = History(Manager);

        await vm.ShowThisMonthCommand.ExecuteAsync(null);

        _client.Verify(c => c.ListSalesAsync(new DateOnly(2026, 10, 1), Today, 1, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task ShowThisYear_ForAManager_AsksFromTheFirstOfTheYear()
    {
        _client.Setup(c => c.ListSalesAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(Page(hasMore: false));
        var vm = History(Manager);

        await vm.ShowThisYearCommand.ExecuteAsync(null);

        _client.Verify(c => c.ListSalesAsync(new DateOnly(2026, 1, 1), Today, 1, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task LoadMore_WithAnotherPage_AppendsIt()
    {
        _client.Setup(c => c.ListSalesAsync(Today, Today, 1, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()))
               .ReturnsAsync(Page(hasMore: true, 3, 2));
        _client.Setup(c => c.ListSalesAsync(Today, Today, 2, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()))
               .ReturnsAsync(Page(hasMore: false, 1));
        var vm = History(Cashier);
        await vm.ShowTodayCommand.ExecuteAsync(null);

        await vm.LoadMoreCommand.ExecuteAsync(null);

        vm.Bills.Select(b => b.InvoiceNumber).Should()
                                             .Equal(3, 2, 1);
    }

    [Fact]
    public async Task ShowToday_WithABill_ShowsItsTimeInStoreTime()
    {
        _client.Setup(c => c.ListSalesAsync(Today, Today, 1, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()))
               .ReturnsAsync(Page(hasMore: false, 1));
        var vm = History(Cashier);

        await vm.ShowTodayCommand.ExecuteAsync(null);

        vm.Bills.Single().CreatedAt.Should()
                                   .Be(new DateTime(2026, 10, 15, 9, 0, 0));
    }

    [Fact]
    public async Task ShowToday_WithNoNameFromStoreHub_ShowsTheCode()
    {
        _client.Setup(c => c.ListSalesAsync(Today, Today, 1, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()))
               .ReturnsAsync(PaidBy("Unknown", name: null));
        var vm = History(Manager);

        await vm.ShowTodayCommand.ExecuteAsync(null);

        vm.Bills.Single().PaymentMethodName.Should()
                                           .Be("Unknown");
    }

    // StoreHub names it from the whole catalogue, so an ended campaign method still has its name.
    [Fact]
    public async Task ShowToday_WithANameFromStoreHub_ShowsIt()
    {
        _client.Setup(c => c.ListSalesAsync(Today, Today, 1, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()))
               .ReturnsAsync(PaidBy("M33WeLove", name: "ม.33 เรารักกัน"));
        var vm = History(Manager);

        await vm.ShowTodayCommand.ExecuteAsync(null);

        vm.Bills.Single().PaymentMethodName.Should()
                                           .Be("ม.33 เรารักกัน");
    }

    // A cashier on a slow StoreHub clicks one bill, then another: the grid highlights the second, so the
    // second must load too. The detail drops the first one's late answer by itself.
    [Fact]
    public void Open_WhileAnotherBillLoads_CanOpenTheNext()
    {
        var first = new InvoiceRow(Guid.NewGuid(), 1041, new DateTime(2026, 10, 15, 9, 0, 0), 100m, "เงินสด", 1);
        var second = first with { Id = Guid.NewGuid(), InvoiceNumber = 1042 };
        var pending = new TaskCompletionSource<InvoiceDetailDto?>();
        _client.Setup(c => c.GetSaleAsync(first.Id, It.IsAny<CancellationToken>())).Returns(pending.Task);
        var vm = History(Manager);
        vm.OpenCommand.Execute(first);

        try
        {
            vm.OpenCommand.CanExecute(second).Should()
                                             .BeTrue();
        }
        finally
        {
            // xunit waits for the command's async-void monitor; a never-finished load would hang the run.
            pending.SetResult(null);
        }
    }

    [Fact]
    public void IsTodayOnly_ForACashier_IsTrue()
    {
        var vm = History(Cashier);

        vm.IsTodayOnly.Should()
                      .BeTrue();
    }

    private InvoiceHistoryViewModel History(SalesHistoryAccess access)
    {
        var clock = new StoreClock(new FakeTimeProvider(NowUtc), Bangkok);
        var detail = new InvoiceDetailViewModel(_client.Object, Mock.Of<IReceiptPrinterService>(),
                                                new ReceiptDocumentFactory(Bangkok), clock, access);

        return new InvoiceHistoryViewModel(_client.Object, clock, access, detail);
    }

    private static SalesPage PaidBy(string method, string? name) =>
        new([new InvoiceSummaryDto(Guid.NewGuid(), 1042, new DateTime(2026, 10, 15, 2, 0, 0, DateTimeKind.Utc), 100m, method, 1, name)],
            1, InvoiceHistoryViewModel.PageSize, HasMore: false);

    // Every bill at 02:00 UTC = 09:00 Bangkok today.
    private static SalesPage Page(bool hasMore, params long[] numbers) =>
        new(numbers.Select(n => new InvoiceSummaryDto(Guid.NewGuid(), n, new DateTime(2026, 10, 15, 2, 0, 0, DateTimeKind.Utc), 100m, "Cash", 1)).ToList(),
            1, InvoiceHistoryViewModel.PageSize, hasMore);

    private static InvoiceDetailDto Detail(long number) => new(
        Guid.NewGuid(), number, "store-a", Guid.NewGuid(), "Malee Cashier", 100m, 100m, 0m, false, false, 0m,
        new DateTime(2026, 10, 15, 2, 0, 0, DateTimeKind.Utc), [], []);
}
