using System.Windows.Forms;
using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;
using IndyPOS.Presentation.CashDrawer;
using IndyPOS.Windows.Forms.UI.CashDrawer;
using Moq;
using Xunit;
using static IndyPOS.Windows.Forms.Tests.UI.CashDrawer.CashDrawerFixture;

namespace IndyPOS.Windows.Forms.Tests.UI.CashDrawer;

public class CashCountViewTests
{
    private readonly Mock<IStoreHubClient> _client = new();

    [Fact]
    public void CountBox_WithLetters_KeepsTheTypedTotalAtZero()
    {
        using var view = Count(out _);

        Find(view, "BankNote1000CountBox").Text = "abc";

        Find(view, "TypedTotalLabel").Text.Should()
                                     .Be($"{0m:N2}");
    }

    [Fact]
    public void Reset_AfterTyping_PutsZeroBackInTheBoxes()
    {
        using var view = Count(out var vm);
        Find(view, "Coin5CountBox").Text = "4";

        vm.ResetCommand.Execute(null);

        Find(view, "Coin5CountBox").Text.Should()
                                   .Be("0");
    }

    [Fact]
    public void Save_WhenNotEditable_IsDisabled()
    {
        using var view = Count(out var vm);

        vm.IsEditable = false;

        Find(view, "SaveCountButton").Enabled.Should()
                                     .BeFalse();
    }

    [Fact]
    public void CountBox_WhenTyped_UpdatesTheTypedTotal()
    {
        using var view = Count(out _);

        Find(view, "BankNote500CountBox").Text = "3";

        Find(view, "TypedTotalLabel").Text.Should()
                                     .Be($"{1500m:N2}");
    }

    [Fact]
    public async Task Bind_WithACount_ShowsWhoCounted()
    {
        using var view = Count(out var vm);
        _client.Setup(c => c.GetCashCountsAsync(Today, It.IsAny<CancellationToken>()))
               .ReturnsAsync([new CashCountDto(Guid.NewGuid(), Today, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1000m, NowUtc.UtcDateTime, Guid.NewGuid(), "มาลี ใจดี")]);

        await vm.LoadAsync(Today, CancellationToken.None);

        ((DataGridView)Find(view, "CountHistoryGrid")).Rows[0].Cells[1].Value.Should()
                                                                   .Be("มาลี ใจดี");
    }

    private CashCountView Count(out CashCountViewModel vm)
    {
        var view = new CashCountView();
        vm = new CashCountViewModel(_client.Object, Clock(), () => Task.CompletedTask) { IsEditable = true };
        view.Bind(vm);
        return view;
    }
}
