using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.DebtRepayments;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Floats;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Payouts;
using IndyPOS.Domain.Enums;
using IndyPOS.Presentation.CashDrawer;
using IndyPOS.Presentation.SalesHistory;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;

namespace IndyPOS.Presentation.Tests.CashDrawer;

public class CashEntriesViewModelTests
{
    // Now is 10:00 on 2026-10-18 in Bangkok.
    private static readonly DateTimeOffset NowUtc = new(2026, 10, 18, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 18);
    private static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
    private static readonly Guid RowId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private const string Refusal = "จำนวนเงินต้องมากกว่า 0";
    private const string TypedAmount = "150";

    private readonly Mock<IStoreHubClient> _client = new();
    private readonly StoreClock _clock = new(new FakeTimeProvider(NowUtc), Bangkok);
    private int _reloads;

    [Fact]
    public async Task AddCommand_WhenStoreHubRefuses_KeepsTheTypedAmount()
    {
        var vm = Payouts();
        RefuseAdd();
        vm.AmountText = TypedAmount;

        await vm.AddCommand.ExecuteAsync(null);

        vm.AmountText.Should()
                     .Be(TypedAmount);
    }

    [Fact]
    public async Task AddCommand_WhenStoreHubRefuses_LeavesTheListUnchanged()
    {
        var vm = await LoadedPayoutsAsync(Payout(100m));
        RefuseAdd();
        vm.AmountText = TypedAmount;

        await vm.AddCommand.ExecuteAsync(null);

        vm.Rows.Should()
               .ContainSingle();
    }

    [Fact]
    public async Task AddCommand_WhenStoreHubRefuses_ShowsItsReason()
    {
        var vm = Payouts();
        RefuseAdd();
        vm.AmountText = TypedAmount;

        await vm.AddCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should()
                       .Be(string.Format(CashEntriesViewModel.SaveFailed, Refusal));
    }

    [Fact]
    public async Task AddCommand_WhenStoreHubRefuses_DoesNotReloadTheScreen()
    {
        var vm = Payouts();
        RefuseAdd();
        vm.AmountText = TypedAmount;

        await vm.AddCommand.ExecuteAsync(null);

        _reloads.Should()
                .Be(0);
    }

    [Fact]
    public async Task AddCommand_WithLetters_MakesNoCall()
    {
        var vm = Payouts();
        vm.AmountText = "abc";

        await vm.AddCommand.ExecuteAsync(null);

        _client.Verify(c => c.AddCashPayoutAsync(It.IsAny<AddCashPayoutRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddCommand_WithLetters_ShowsInvalidAmount()
    {
        var vm = Payouts();
        vm.AmountText = "abc";

        await vm.AddCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should()
                       .Be(CashEntriesViewModel.InvalidAmount);
    }

    [Fact]
    public void AddCommand_WhenNotEditable_CannotExecute()
    {
        var vm = Payouts();
        vm.IsEditable = false;

        vm.AddCommand.CanExecute(null).Should()
                                      .BeFalse();
    }

    [Fact]
    public void DeleteCommand_WithNothingSelected_CannotExecute()
    {
        var vm = Payouts();

        vm.DeleteCommand.CanExecute(null).Should()
                                         .BeFalse();
    }

    [Fact]
    public async Task DeleteCommand_WhenStoreHubRefuses_KeepsTheRow()
    {
        var vm = await LoadedPayoutsAsync(Payout(100m));
        vm.SelectedRow = vm.Rows[0];
        _client.Setup(c => c.DeleteCashPayoutAsync(RowId, It.IsAny<CancellationToken>()))
               .ThrowsAsync(new StoreHubException(Refusal));

        await vm.DeleteCommand.ExecuteAsync(null);

        vm.Rows.Should()
               .ContainSingle();
    }

    [Fact]
    public async Task EditCommand_WhenStoreHubRefuses_KeepsTheTypedText()
    {
        var vm = await LoadedPayoutsAsync(Payout(100m));
        vm.SelectedRow = vm.Rows[0];
        vm.Text = "ค่าน้ำแข็ง";
        _client.Setup(c => c.EditCashPayoutAsync(RowId, It.IsAny<EditCashPayoutRequest>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new StoreHubException(Refusal));

        await vm.EditCommand.ExecuteAsync(null);

        vm.Text.Should()
               .Be("ค่าน้ำแข็ง");
    }

    [Fact]
    public async Task AddCommand_WithThousandsSeparators_SendsTheAmount()
    {
        var vm = Payouts();
        vm.AmountText = "1,000.50";

        await vm.AddCommand.ExecuteAsync(null);

        _client.Verify(c => c.AddCashPayoutAsync(It.Is<AddCashPayoutRequest>(r => r.Amount == 1000.50m), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task AddCommand_WithoutChoosingACategory_SendsGeneral()
    {
        var vm = Payouts();
        vm.AmountText = TypedAmount;

        await vm.AddCommand.ExecuteAsync(null);

        _client.Verify(c => c.AddCashPayoutAsync(It.Is<AddCashPayoutRequest>(r => r.Category == PayoutCategory.General), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task AddCommand_WhenSaved_ReloadsTheScreen()
    {
        var vm = Payouts();
        vm.AmountText = TypedAmount;

        await vm.AddCommand.ExecuteAsync(null);

        _reloads.Should()
                .Be(1);
    }

    [Fact]
    public async Task AddCommand_WhenSaved_ClearsTheInputs()
    {
        var vm = Payouts();
        vm.AmountText = TypedAmount;
        vm.Text = "ค่าน้ำแข็ง";

        await vm.AddCommand.ExecuteAsync(null);

        (vm.AmountText, vm.Text).Should()
                                .Be((string.Empty, (string?)null));
    }

    [Fact]
    public async Task SelectedRow_WhenSet_LoadsItsAmountIntoTheInput()
    {
        var vm = await LoadedPayoutsAsync(Payout(100m));

        vm.SelectedRow = vm.Rows[0];

        vm.AmountText.Should()
                     .Be("100.00");
    }

    // After looking at a row, a new entry starts from empty boxes rather than a copy of that row.
    [Fact]
    public async Task SelectedRow_WhenCleared_ClearsTheInputs()
    {
        var vm = await LoadedPayoutsAsync(Payout(100m));
        vm.SelectedRow = vm.Rows[0];

        vm.SelectedRow = null;

        (vm.AmountText, vm.Text).Should()
                                .Be((string.Empty, (string?)null));
    }

    [Fact]
    public async Task EditCommand_ForAPayout_SendsItsCategory()
    {
        var vm = await LoadedPayoutsAsync(Payout(100m));
        vm.SelectedRow = vm.Rows[0];
        vm.Category = PayoutCategory.Hardware;

        await vm.EditCommand.ExecuteAsync(null);

        _client.Verify(c => c.EditCashPayoutAsync(RowId, It.Is<EditCashPayoutRequest>(r => r.Category == PayoutCategory.Hardware), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task DeleteCommand_WhenDeleted_ReloadsTheScreen()
    {
        var vm = await LoadedPayoutsAsync(Payout(100m));
        vm.SelectedRow = vm.Rows[0];

        await vm.DeleteCommand.ExecuteAsync(null);

        _reloads.Should()
                .Be(1);
    }

    [Fact]
    public async Task LoadAsync_ForFloats_AsksTheFloatsRoute()
    {
        var vm = new CashEntriesViewModel(CashEntryApis.Floats(_client.Object, _clock), Reload) { IsEditable = true };
        _client.Setup(c => c.GetCashFloatsAsync(Today, It.IsAny<CancellationToken>()))
               .ReturnsAsync([new CashFloatDto(RowId, 1_000m, null, Today, NowUtc.UtcDateTime, NowUtc.UtcDateTime, Guid.NewGuid(), null)]);

        await vm.LoadAsync(Today, CancellationToken.None);

        vm.Rows.Single().Amount.Should()
                               .Be(1_000m);
    }

    [Fact]
    public async Task LoadAsync_ForDebtRepayments_ShowsTheCustomerName()
    {
        var vm = new CashEntriesViewModel(CashEntryApis.DebtRepayments(_client.Object, _clock), Reload) { IsEditable = true };
        _client.Setup(c => c.GetDebtRepaymentsAsync(Today, It.IsAny<CancellationToken>()))
               .ReturnsAsync([new DebtRepaymentDto(RowId, "ลุงสมชาย", 300m, Today, NowUtc.UtcDateTime, NowUtc.UtcDateTime, Guid.NewGuid(), null)]);

        await vm.LoadAsync(Today, CancellationToken.None);

        vm.Rows.Single().Text.Should()
                             .Be("ลุงสมชาย");
    }

    private CashEntriesViewModel Payouts() =>
        new(CashEntryApis.Payouts(_client.Object, _clock), Reload) { IsEditable = true };

    private async Task<CashEntriesViewModel> LoadedPayoutsAsync(params CashPayoutDto[] rows)
    {
        _client.Setup(c => c.GetCashPayoutsAsync(Today, It.IsAny<CancellationToken>()))
               .ReturnsAsync(rows);
        var vm = Payouts();
        await vm.LoadAsync(Today, CancellationToken.None);
        return vm;
    }

    private void RefuseAdd() =>
        _client.Setup(c => c.AddCashPayoutAsync(It.IsAny<AddCashPayoutRequest>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new StoreHubException(Refusal));

    private static CashPayoutDto Payout(decimal amount) =>
        new(RowId, PayoutCategory.General, amount, "ค่าส่ง", Today, NowUtc.UtcDateTime, NowUtc.UtcDateTime, Guid.NewGuid(), null);

    private Task Reload()
    {
        _reloads++;
        return Task.CompletedTask;
    }
}
