using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;
using IndyPOS.Presentation.CashDrawer;
using IndyPOS.Presentation.SalesHistory;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;

namespace IndyPOS.Presentation.Tests.CashDrawer;

public class CashCountViewModelTests
{
    // Now is 10:00 on 2026-10-18 in Bangkok.
    private static readonly DateTimeOffset NowUtc = new(2026, 10, 18, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 18);
    private static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
    private const string Refusal = "จำนวนต้องไม่ติดลบ";
    private const decimal OneOfEach = 1688m;

    private readonly Mock<IStoreHubClient> _client = new();
    private readonly StoreClock _clock = new(new FakeTimeProvider(NowUtc), Bangkok);
    private int _reloads;

    [Fact]
    public async Task SaveCommand_WhenStoreHubRefuses_KeepsTheCounts()
    {
        var vm = Count();
        vm.BankNote1000Count = 3;
        RefuseSave();

        await vm.SaveCommand.ExecuteAsync(null);

        vm.BankNote1000Count.Should()
                            .Be(3);
    }

    [Fact]
    public async Task SaveCommand_WhenStoreHubRefuses_ShowsItsReason()
    {
        var vm = Count();
        RefuseSave();

        await vm.SaveCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should()
                       .Be(string.Format(CashCountViewModel.SaveFailed, Refusal));
    }

    [Fact]
    public async Task SaveCommand_WhenStoreHubRefuses_DoesNotReloadTheScreen()
    {
        var vm = Count();
        RefuseSave();

        await vm.SaveCommand.ExecuteAsync(null);

        _reloads.Should()
                .Be(0);
    }

    [Fact]
    public void SaveCommand_WhenNotEditable_CannotExecute()
    {
        var vm = Count();
        vm.IsEditable = false;

        vm.SaveCommand.CanExecute(null).Should()
                                       .BeFalse();
    }

    [Fact]
    public void ResetCommand_WithCountsTyped_DoesNotCallStoreHub()
    {
        var vm = Count();
        vm.Coin5Count = 4;

        vm.ResetCommand.Execute(null);

        _client.VerifyNoOtherCalls();
    }

    [Fact]
    public void ResetCommand_WithCountsTyped_ZeroesThem()
    {
        var vm = Count();
        vm.BankNote500Count = 2;
        vm.Coin1Count = 7;

        vm.ResetCommand.Execute(null);

        vm.TypedTotal.Should()
                     .Be(0m);
    }

    [Fact]
    public void TypedTotal_WithOneOfEach_ReturnsTheSumOfTheirValues()
    {
        var vm = Count();

        TypeOneOfEach(vm);

        vm.TypedTotal.Should()
                     .Be(OneOfEach);
    }

    [Fact]
    public async Task SaveCommand_WhenSaved_SendsTheNineCounts()
    {
        var vm = Count();
        TypeOneOfEach(vm);

        await vm.SaveCommand.ExecuteAsync(null);

        _client.Verify(c => c.AddCashCountAsync(new AddCashCountRequest(1, 1, 1, 1, 1, 1, 1, 1, 1), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task SaveCommand_WhenSaved_KeepsTheCountsOnScreen()
    {
        var vm = Count();
        TypeOneOfEach(vm);

        await vm.SaveCommand.ExecuteAsync(null);

        vm.TypedTotal.Should()
                     .Be(OneOfEach);
    }

    [Fact]
    public async Task SaveCommand_WhenSaved_ReloadsTheScreen()
    {
        var vm = Count();

        await vm.SaveCommand.ExecuteAsync(null);

        _reloads.Should()
                .Be(1);
    }

    [Fact]
    public async Task LoadAsync_WithTwoCounts_ListsTheNewestFirst()
    {
        var vm = Count();
        _client.Setup(c => c.GetCashCountsAsync(Today, It.IsAny<CancellationToken>()))
               .ReturnsAsync([CountAt(NowUtc.UtcDateTime.AddHours(-4), 500m), CountAt(NowUtc.UtcDateTime, 900m)]);

        await vm.LoadAsync(Today, CancellationToken.None);

        vm.History.Select(h => h.CountedTotal).Should()
                                              .Equal(900m, 500m);
    }

    private CashCountViewModel Count() => new(_client.Object, _clock, Reload) { IsEditable = true };

    private static void TypeOneOfEach(CashCountViewModel vm)
    {
        vm.BankNote1000Count = 1;
        vm.BankNote500Count = 1;
        vm.BankNote100Count = 1;
        vm.BankNote50Count = 1;
        vm.BankNote20Count = 1;
        vm.Coin10Count = 1;
        vm.Coin5Count = 1;
        vm.Coin2Count = 1;
        vm.Coin1Count = 1;
    }

    private void RefuseSave() =>
        _client.Setup(c => c.AddCashCountAsync(It.IsAny<AddCashCountRequest>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new StoreHubException(Refusal));

    private static CashCountDto CountAt(DateTime createdUtc, decimal total) =>
        new(Guid.NewGuid(), Today, 0, 0, 0, 0, 0, 0, 0, 0, 0, total, createdUtc, Guid.NewGuid());

    private Task Reload()
    {
        _reloads++;
        return Task.CompletedTask;
    }
}
