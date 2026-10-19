using System.Windows.Forms;
using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Payouts;
using IndyPOS.Domain.Enums;
using IndyPOS.Presentation.CashDrawer;
using IndyPOS.Windows.Forms.UI.CashDrawer;
using Moq;
using Xunit;
using static IndyPOS.Windows.Forms.Tests.UI.CashDrawer.CashDrawerFixture;

namespace IndyPOS.Windows.Forms.Tests.UI.CashDrawer;

public class CashEntriesViewTests
{
    private const string Refusal = "จำนวนเงินต้องมากกว่า 0";
    private readonly Mock<IStoreHubClient> _client = new();

    [Fact]
    public async Task Add_WhenStoreHubRefuses_ShowsTheReasonUnderTheList()
    {
        using var view = Payouts(out var vm);
        RefuseAdd();
        Find(view, "PayoutAmountBox").Text = "0";

        await vm.AddCommand.ExecuteAsync(null);

        Find(view, "PayoutErrorLabel").Text.Should()
                                       .Be(string.Format(CashEntriesViewModel.SaveFailed, Refusal));
    }

    [Fact]
    public async Task Add_WhenStoreHubRefuses_KeepsTheAmountInTheBox()
    {
        using var view = Payouts(out var vm);
        RefuseAdd();
        Find(view, "PayoutAmountBox").Text = "150";

        await vm.AddCommand.ExecuteAsync(null);

        Find(view, "PayoutAmountBox").Text.Should()
                                      .Be("150");
    }

    [Fact]
    public void Bind_WhenNotEditable_DisablesAdd()
    {
        using var view = Payouts(out var vm);

        vm.IsEditable = false;

        Find(view, "AddPayoutButton").Enabled.Should()
                                     .BeFalse();
    }

    [Fact]
    public void Bind_WithoutCategory_HidesThePicker()
    {
        using var view = new CashEntriesView("Payout", "รายจ่าย", "รายการ");
        view.Bind(new CashEntriesViewModel(CashEntryApis.Payouts(_client.Object, Clock()), () => Task.CompletedTask), showCategory: false);

        Find(view, "PayoutCategoryBox").Visible.Should()
                                       .BeFalse();
    }

    [Fact]
    public void Delete_WithNoRowSelected_IsDisabled()
    {
        using var view = Payouts(out _);

        Find(view, "DeletePayoutButton").Enabled.Should()
                                        .BeFalse();
    }

    [Fact]
    public async Task Bind_WithTwoRows_FillsTheGrid()
    {
        using var view = Payouts(out var vm);
        AnswerPayouts(Payout(100m), Payout(200m));

        await vm.LoadAsync(Today, CancellationToken.None);

        ((DataGridView)Find(view, "PayoutsGrid")).Rows.Count.Should()
                                                 .Be(2);
    }

    [Fact]
    public async Task ClickingARow_PutsItsAmountInTheBox()
    {
        using var view = Payouts(out var vm);
        AnswerPayouts(Payout(120m));
        await vm.LoadAsync(Today, CancellationToken.None);

        vm.SelectedRow = vm.Rows[0];

        Find(view, "PayoutAmountBox").Text.Should()
                                      .Be("120.00");
    }

    [Fact]
    public void TypingAnAmount_WritesItToTheViewModel()
    {
        using var view = Payouts(out var vm);

        Find(view, "PayoutAmountBox").Text = "75";

        vm.AmountText.Should()
                     .Be("75");
    }

    private CashEntriesView Payouts(out CashEntriesViewModel vm)
    {
        var view = new CashEntriesView("Payout", "รายจ่าย", "รายการ");
        vm = new CashEntriesViewModel(CashEntryApis.Payouts(_client.Object, Clock()), () => Task.CompletedTask) { IsEditable = true };
        view.Bind(vm, showCategory: true);
        return view;
    }

    private void RefuseAdd() =>
        _client.Setup(c => c.AddCashPayoutAsync(It.IsAny<AddCashPayoutRequest>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new StoreHubException(Refusal));

    private void AnswerPayouts(params CashPayoutDto[] rows) =>
        _client.Setup(c => c.GetCashPayoutsAsync(Today, It.IsAny<CancellationToken>()))
               .ReturnsAsync(rows);

    private static CashPayoutDto Payout(decimal amount) =>
        new(Guid.NewGuid(), PayoutCategory.General, amount, "ค่าน้ำแข็ง", Today, NowUtc.UtcDateTime, NowUtc.UtcDateTime, Guid.NewGuid(), null);
}
