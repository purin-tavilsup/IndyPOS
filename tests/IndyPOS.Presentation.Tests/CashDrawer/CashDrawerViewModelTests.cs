using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.DebtRepayments;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Floats;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Payouts;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Summary;
using IndyPOS.Application.UseCases.StoreHub.PayLater;
using IndyPOS.Presentation.CashDrawer;
using IndyPOS.Presentation.SalesHistory;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;

namespace IndyPOS.Presentation.Tests.CashDrawer;

public class CashDrawerViewModelTests
{
    // Now is 10:00 on 2026-10-18 in Bangkok.
    private static readonly DateTimeOffset NowUtc = new(2026, 10, 18, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 18);
    private static readonly DateOnly Yesterday = Today.AddDays(-1);
    private static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
    private static readonly CashDrawerAccess Cashier = new(CanOpen: true, CanViewAnyDay: false);
    private static readonly CashDrawerAccess Manager = new(CanOpen: true, CanViewAnyDay: true);
    private static readonly StoreFeaturesDto GeneralHardware = new(PayLaterEnabled: true, MultipleProductTypesEnabled: true);
    private static readonly StoreFeaturesDto Minimart = new(PayLaterEnabled: false, MultipleProductTypesEnabled: false);
    private const string Unreachable = "Cannot connect to StoreHub";
    private const decimal ExpectedCash = 1_234m;

    private readonly Mock<IStoreHubClient> _client = new();
    private readonly FakeTimeProvider _time = new(NowUtc);
    private readonly StoreClock _clock;

    public CashDrawerViewModelTests()
    {
        _clock = new StoreClock(_time, Bangkok);
        AnswerDay(Today, Summary(Today, isEditable: true));
        AnswerDay(Yesterday, Summary(Yesterday, isEditable: false));
        AnswerPayLater();
    }

    [Fact]
    public async Task Refresh_WhenStoreHubFails_ShowsItsReason()
    {
        _client.Setup(c => c.GetCashSummaryAsync(Today, It.IsAny<CancellationToken>()))
               .ThrowsAsync(new StoreHubException(Unreachable));
        var vm = Drawer(Manager);

        await vm.RefreshCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should()
                       .Be(string.Format(CashDrawerViewModel.LoadFailed, Unreachable));
    }

    [Fact]
    public async Task Refresh_WhenTheDayIsNoLongerEditable_DisablesAdding()
    {
        AnswerDay(Today, Summary(Today, isEditable: false));
        var vm = Drawer(Manager);

        await vm.RefreshCommand.ExecuteAsync(null);

        vm.Payouts.AddCommand.CanExecute(null).Should()
                                              .BeFalse();
    }

    [Fact]
    public async Task ShowDay_ForACashierPickingYesterday_StaysOnToday()
    {
        var vm = Drawer(Cashier);

        await vm.ShowDayCommand.ExecuteAsync(Yesterday);

        _client.Verify(c => c.GetCashSummaryAsync(Yesterday, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ShowDay_WhenAnOlderLoadFinishesLast_KeepsTheNewerDay()
    {
        var olderSummary = new TaskCompletionSource<CashDrawerSummaryDto>();
        _client.Setup(c => c.GetCashSummaryAsync(Yesterday, It.IsAny<CancellationToken>()))
               .Returns(olderSummary.Task);
        var vm = Drawer(Manager);
        var older = vm.ShowDayCommand.ExecuteAsync(Yesterday);

        await vm.ShowDayCommand.ExecuteAsync(Today);
        olderSummary.SetResult(Summary(Yesterday, isEditable: false));
        await older;

        vm.Summary!.BusinessDate.Should()
                                .Be(Today);
    }

    // A cashier may only ever read today; asking for the day the screen opened on would be refused.
    [Fact]
    public async Task Refresh_ForACashierAfterMidnight_LoadsTheNewDay()
    {
        var tomorrow = Today.AddDays(1);
        AnswerDay(tomorrow, Summary(tomorrow, isEditable: true));
        var vm = Drawer(Cashier);
        await vm.RefreshCommand.ExecuteAsync(null);
        _time.Advance(TimeSpan.FromDays(1));

        await vm.RefreshCommand.ExecuteAsync(null);

        vm.Summary!.BusinessDate.Should()
                                .Be(tomorrow);
    }

    [Fact]
    public async Task Refresh_WhenTheSummaryIsRefused_DisablesAdding()
    {
        var vm = Drawer(Manager);
        await vm.RefreshCommand.ExecuteAsync(null);
        _client.Setup(c => c.GetCashSummaryAsync(Today, It.IsAny<CancellationToken>()))
               .ThrowsAsync(new StoreHubException(Unreachable));

        await vm.RefreshCommand.ExecuteAsync(null);

        vm.Payouts.AddCommand.CanExecute(null).Should()
                                              .BeFalse();
    }

    [Fact]
    public async Task ShowDay_WhenYesterdaysFloatsFail_KeepsShowingToday()
    {
        var vm = Drawer(Manager);
        await vm.RefreshCommand.ExecuteAsync(null);
        _client.Setup(c => c.GetCashFloatsAsync(Yesterday, It.IsAny<CancellationToken>()))
               .ThrowsAsync(new StoreHubException(Unreachable));

        await vm.ShowDayCommand.ExecuteAsync(Yesterday);

        (vm.BusinessDate, vm.Summary!.BusinessDate).Should()
                                                   .Be((Today, Today));
    }

    // An error that was already on its way back must not escape the command once a newer load owns the screen.
    [Fact]
    public async Task ShowDay_WhenAnOlderLoadFailsAfterANewerOne_IgnoresTheFailure()
    {
        var olderSummary = new TaskCompletionSource<CashDrawerSummaryDto>();
        _client.Setup(c => c.GetCashSummaryAsync(Yesterday, It.IsAny<CancellationToken>()))
               .Returns(olderSummary.Task);
        var vm = Drawer(Manager);
        var older = vm.ShowDayCommand.ExecuteAsync(Yesterday);

        await vm.ShowDayCommand.ExecuteAsync(Today);
        olderSummary.SetException(new StoreHubException(Unreachable));
        var act = () => older;

        await act.Should()
                 .NotThrowAsync();
    }

    [Fact]
    public async Task Refresh_WhenTheSummaryIsRefused_TurnsOffIsEditable()
    {
        var vm = Drawer(Manager);
        await vm.RefreshCommand.ExecuteAsync(null);
        _client.Setup(c => c.GetCashSummaryAsync(Today, It.IsAny<CancellationToken>()))
               .ThrowsAsync(new StoreHubException(Unreachable));

        await vm.RefreshCommand.ExecuteAsync(null);

        vm.IsEditable.Should()
                     .BeFalse();
    }

    [Fact]
    public async Task ShowDay_WhenAnOlderLoadStallsInThePayouts_KeepsTheNewerPayouts()
    {
        var olderPayouts = new TaskCompletionSource<IReadOnlyList<CashPayoutDto>>();
        _client.Setup(c => c.GetCashPayoutsAsync(Yesterday, It.IsAny<CancellationToken>()))
               .Returns(olderPayouts.Task);
        var vm = Drawer(Manager);
        var older = vm.ShowDayCommand.ExecuteAsync(Yesterday);

        await vm.ShowDayCommand.ExecuteAsync(Today);
        olderPayouts.SetResult([Payout(Yesterday), Payout(Yesterday)]);
        await older;

        vm.Payouts.Rows.Should()
                       .ContainSingle();
    }

    [Fact]
    public async Task Refresh_ForAStoreWithoutPayLater_DoesNotAskForPayLater()
    {
        var vm = Drawer(Manager, Minimart);

        await vm.RefreshCommand.ExecuteAsync(null);

        _client.Verify(c => c.GetPayLaterAsync(It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Refresh_ForAStoreWithoutPayLater_KeepsDebtRepaymentsReadOnly()
    {
        var vm = Drawer(Manager, Minimart);

        await vm.RefreshCommand.ExecuteAsync(null);

        vm.DebtRepayments.AddCommand.CanExecute(null).Should()
                                                     .BeFalse();
    }

    [Fact]
    public async Task Refresh_WithAPayLaterFromYesterday_LeavesItOut()
    {
        AnswerPayLater(PayLater(NowUtc.UtcDateTime.AddDays(-1), "ลุงสมชาย", 200m));
        var vm = Drawer(Manager);

        await vm.RefreshCommand.ExecuteAsync(null);

        vm.TodaysPayLater.Should()
                         .BeEmpty();
    }

    // Paid off the same day, it still went on the slate today.
    [Fact]
    public async Task Refresh_WithACompletedPayLaterFromToday_ListsIt()
    {
        AnswerPayLater(PayLater(NowUtc.UtcDateTime, "ลุงสมชาย", 200m) with { IsCompleted = true });
        var vm = Drawer(Manager);

        await vm.RefreshCommand.ExecuteAsync(null);

        vm.TodaysPayLater.Should()
                         .ContainSingle();
    }

    [Fact]
    public async Task Refresh_ForPayLater_AsksForCompletedOnesToo()
    {
        var vm = Drawer(Manager);

        await vm.RefreshCommand.ExecuteAsync(null);

        _client.Verify(c => c.GetPayLaterAsync(true, null, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task Refresh_WithTwoPayLaterToday_ListsThemOldestFirst()
    {
        AnswerPayLater(PayLater(NowUtc.UtcDateTime, "ป้าแดง", 90m), PayLater(NowUtc.UtcDateTime.AddHours(-2), "ลุงสมชาย", 200m));
        var vm = Drawer(Manager);

        await vm.RefreshCommand.ExecuteAsync(null);

        vm.TodaysPayLater.Select(p => p.Description).Should()
                                                    .Equal("ลุงสมชาย", "ป้าแดง");
    }

    [Fact]
    public void ShowPayoutCategory_ForAMinimart_ReturnsFalse()
    {
        var vm = Drawer(Manager, Minimart);

        vm.ShowPayoutCategory.Should()
                             .BeFalse();
    }

    [Fact]
    public void ShowPayLater_WithoutFeatures_ReturnsTrue()
    {
        var vm = Drawer(Manager, features: null);

        vm.ShowPayLater.Should()
                       .BeTrue();
    }

    [Fact]
    public void ShowPayLater_ForAStoreWithoutPayLater_ReturnsFalse()
    {
        var vm = Drawer(Manager, Minimart);

        vm.ShowPayLater.Should()
                       .BeFalse();
    }

    [Fact]
    public void ShowProductTypeSplit_ForAMinimart_ReturnsFalse()
    {
        var vm = Drawer(Manager, Minimart);

        vm.ShowProductTypeSplit.Should()
                               .BeFalse();
    }

    [Fact]
    public async Task Refresh_ForAStoreWithoutPayLater_DoesNotAskForDebtRepayments()
    {
        var vm = Drawer(Manager, Minimart);

        await vm.RefreshCommand.ExecuteAsync(null);

        _client.Verify(c => c.GetDebtRepaymentsAsync(It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Refresh_WhenLoaded_ShowsTheSummarysExpectedCash()
    {
        var vm = Drawer(Manager);

        await vm.RefreshCommand.ExecuteAsync(null);

        vm.Summary!.ExpectedCash.Should()
                                .Be(ExpectedCash);
    }

    [Fact]
    public async Task Refresh_WhenLoaded_LoadsAllThreeLists()
    {
        var vm = Drawer(Manager);

        await vm.RefreshCommand.ExecuteAsync(null);

        (vm.Payouts.Rows.Count, vm.Floats.Rows.Count, vm.DebtRepayments.Rows.Count).Should()
                                                                                   .Be((1, 1, 1));
    }

    [Fact]
    public async Task Refresh_WhenLoaded_LoadsTheCountHistory()
    {
        var vm = Drawer(Manager);

        await vm.RefreshCommand.ExecuteAsync(null);

        vm.Count.History.Should()
                        .ContainSingle();
    }

    [Fact]
    public async Task ShowDay_ForAManagerPickingYesterday_LoadsYesterday()
    {
        var vm = Drawer(Manager);

        await vm.ShowDayCommand.ExecuteAsync(Yesterday);

        vm.Summary!.BusinessDate.Should()
                                .Be(Yesterday);
    }

    [Fact]
    public async Task PayoutsAdd_WhenSaved_ReloadsTheSummary()
    {
        var vm = Drawer(Manager);
        await vm.RefreshCommand.ExecuteAsync(null);
        vm.Payouts.AmountText = "50";

        await vm.Payouts.AddCommand.ExecuteAsync(null);

        _client.Verify(c => c.GetCashSummaryAsync(Today, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    private CashDrawerViewModel Drawer(CashDrawerAccess access, StoreFeaturesDto? features = null) =>
        new(_client.Object, _clock, access, features);

    private CashDrawerViewModel Drawer(CashDrawerAccess access) => Drawer(access, GeneralHardware);

    private void AnswerDay(DateOnly day, CashDrawerSummaryDto summary)
    {
        var at = NowUtc.UtcDateTime;
        _client.Setup(c => c.GetCashSummaryAsync(day, It.IsAny<CancellationToken>()))
               .ReturnsAsync(summary);
        _client.Setup(c => c.GetCashPayoutsAsync(day, It.IsAny<CancellationToken>()))
               .ReturnsAsync([Payout(day)]);
        _client.Setup(c => c.GetCashFloatsAsync(day, It.IsAny<CancellationToken>()))
               .ReturnsAsync([new CashFloatDto(Guid.NewGuid(), 1_000m, null, day, at, at, Guid.NewGuid(), null)]);
        _client.Setup(c => c.GetDebtRepaymentsAsync(day, It.IsAny<CancellationToken>()))
               .ReturnsAsync([new DebtRepaymentDto(Guid.NewGuid(), "ลุงสมชาย", 300m, day, at, at, Guid.NewGuid(), null)]);
        _client.Setup(c => c.GetCashCountsAsync(day, It.IsAny<CancellationToken>()))
               .ReturnsAsync([new CashCountDto(Guid.NewGuid(), day, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1_000m, at, Guid.NewGuid())]);
    }

    private void AnswerPayLater(params PayLaterDto[] items) =>
        _client.Setup(c => c.GetPayLaterAsync(It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new GetPayLaterResponse(0m, 0m, items.Length, 0, items));

    private static PayLaterDto PayLater(DateTime createdUtc, string description, decimal amount) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), description, amount, 0m, amount, false, createdUtc, createdUtc);

    private static CashPayoutDto Payout(DateOnly day) =>
        new(Guid.NewGuid(), default, 100m, null, day, NowUtc.UtcDateTime, NowUtc.UtcDateTime, Guid.NewGuid(), null);

    private static CashDrawerSummaryDto Summary(DateOnly day, bool isEditable) => new()
    {
        BusinessDate = day, IsEditable = isEditable, GeneralProductsTotal = 0m, HardwareProductsTotal = 0m,
        CashPaymentsTotal = 0m, NonCashPayments = [], PayLaterGeneralProductsTotal = 0m,
        PayLaterHardwareProductsTotal = 0m, PayoutsTotal = 0m, GeneralPayoutsTotal = 0m,
        HardwarePayoutsTotal = 0m, CashFloatsTotal = 0m, DebtRepaymentsTotal = 0m, ExpectedCash = ExpectedCash
    };
}
