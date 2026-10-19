using System.Windows.Forms;
using FluentAssertions;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.UseCases.StoreHub.Reports;
using IndyPOS.Windows.Forms.UI.CashDrawer;
using Moq;
using Xunit;
using static IndyPOS.Windows.Forms.Tests.UI.CashDrawer.CashDrawerFixture;

namespace IndyPOS.Windows.Forms.Tests.UI.CashDrawer;

public class CashDrawerPanelTests
{
    private static readonly DateOnly Yesterday = Today.AddDays(-1);
    private const decimal ExpectedCash = 1_234m;

    [Fact]
    public async Task Open_WithNobodyLoggedIn_LeavesTheScreenEmpty()
    {
        var client = EmptyDay(Today);
        using var panel = new CashDrawerPanel(Factory(client.Object, NobodyLoggedIn()), Features(GeneralHardware));

        await panel.OpenAsync();

        client.Verify(c => c.GetCashSummaryAsync(It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Open_ForAMinimart_HidesTheDebtRepayments()
    {
        using var panel = Panel(EmptyDay(Today), UserRole.Cashier, Minimart);

        await panel.OpenAsync();

        Find(panel, "DebtRepaymentsView").Visible.Should()
                                         .BeFalse();
    }

    [Fact]
    public async Task Open_ForAMinimart_HidesTodaysPayLater()
    {
        using var panel = Panel(EmptyDay(Today), UserRole.Cashier, Minimart);

        await panel.OpenAsync();

        Find(panel, "TodaysPayLaterGrid").Visible.Should()
                                         .BeFalse();
    }

    [Fact]
    public async Task Open_ForAMinimart_HidesThePayoutCategory()
    {
        using var panel = Panel(EmptyDay(Today), UserRole.Cashier, Minimart);

        await panel.OpenAsync();

        Find(panel, "PayoutCategoryBox").Visible.Should()
                                        .BeFalse();
    }

    [Fact]
    public async Task Open_ForACashier_HidesTheDayPicker()
    {
        using var panel = Panel(EmptyDay(Today), UserRole.Cashier, GeneralHardware);

        await panel.OpenAsync();

        Find(panel, "CashDrawerDayPicker").Visible.Should()
                                          .BeFalse();
    }

    [Fact]
    public async Task ShowDay_ForYesterday_DisablesSaving()
    {
        var client = EmptyDay(Today);
        client.Setup(c => c.GetCashSummaryAsync(Yesterday, It.IsAny<CancellationToken>()))
              .ReturnsAsync(Summary(Yesterday, isEditable: false, ExpectedCash));
        foreach (var setup in new Action[]
                 {
                     () => client.Setup(c => c.GetCashPayoutsAsync(Yesterday, It.IsAny<CancellationToken>())).ReturnsAsync([]),
                     () => client.Setup(c => c.GetCashFloatsAsync(Yesterday, It.IsAny<CancellationToken>())).ReturnsAsync([]),
                     () => client.Setup(c => c.GetDebtRepaymentsAsync(Yesterday, It.IsAny<CancellationToken>())).ReturnsAsync([]),
                     () => client.Setup(c => c.GetCashCountsAsync(Yesterday, It.IsAny<CancellationToken>())).ReturnsAsync([])
                 })
            setup();
        using var panel = Panel(client, UserRole.StoreManager, GeneralHardware);
        await panel.OpenAsync();

        await panel.ViewModel!.ShowDayCommand.ExecuteAsync(Yesterday);

        Find(panel, "SaveCountButton").Enabled.Should()
                                      .BeFalse();
    }

    [Fact]
    public async Task Open_Twice_IgnoresTheFirstViewModel()
    {
        using var panel = Panel(EmptyDay(Today), UserRole.Cashier, GeneralHardware);
        await panel.OpenAsync();
        var first = panel.ViewModel!;

        await panel.OpenAsync();
        first.ErrorMessage = "stale";

        Find(panel, "CashDrawerErrorLabel").Text.Should()
                                           .BeEmpty();
    }

    [Fact]
    public async Task Open_WhenLoaded_ShowsTheExpectedCash()
    {
        using var panel = Panel(EmptyDay(Today, expectedCash: ExpectedCash), UserRole.Cashier, GeneralHardware);

        await panel.OpenAsync();

        Find(panel, "ExpectedCashLabel").Text.Should()
                                        .Be($"{ExpectedCash:N2}");
    }

    [Fact]
    public async Task Open_WithTwoNonCashMethods_ShowsBothByName()
    {
        var client = EmptyDay(Today);
        client.Setup(c => c.GetCashSummaryAsync(Today, It.IsAny<CancellationToken>()))
              .ReturnsAsync(Summary(Today, true, 0m, [new("MoneyTransfer", "โอนเงิน", 300m), new("FiftyFifty", "คนละครึ่ง", 120m)]));
        using var panel = Panel(client, UserRole.Cashier, GeneralHardware);

        await panel.OpenAsync();

        Texts(Find(panel, "CashSummaryRows")).Should()
                                             .Contain(["โอนเงิน", "คนละครึ่ง"]);
    }

    private static CashDrawerPanel Panel(Mock<IndyPOS.Application.Abstractions.StoreHub.IStoreHubClient> client, UserRole role,
                                         IndyPOS.Application.Common.Models.StoreFeaturesDto features) =>
        new(Factory(client.Object, LoggedIn(role)), Features(features));

    private static IEnumerable<string> Texts(Control root) =>
        root.Controls.Cast<Control>().SelectMany(c => Texts(c).Prepend(c.Text));
}
