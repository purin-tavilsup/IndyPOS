using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Presentation.SalesHistory;
using IndyPOS.Windows.Forms.Services;
using IndyPOS.Windows.Forms.UI.Report;
using Moq;
using Xunit;
using static IndyPOS.Windows.Forms.Tests.UI.Report.SalesHistoryFixture;

namespace IndyPOS.Windows.Forms.Tests.UI.Report;

/// <summary>LoadAsync only, or a load that never finishes: ShowDialog would block the test run.</summary>
public class TodaysBillsFormTests
{
    private static readonly StoreFeaturesDto GeneralHardware = new(PayLaterEnabled: true, MultipleProductTypesEnabled: true);

    private readonly Mock<IStoreHubClient> _client = new();

    // A second press while the first open is still loading must not open a second dialog. The first open
    // stalls on the store's features, before any command runs: a command left running forever would hang
    // xunit, which waits for the toolkit's async-void task monitor. No ShowDialog runs.
    [Fact]
    public void ShowTodaysBillsAsync_WhileTheFirstOpenLoads_IgnoresTheSecondRequest()
    {
        var features = new Mock<IStoreFeaturesProvider>();
        features.Setup(f => f.GetAsync()).Returns(new TaskCompletionSource<StoreFeaturesDto>().Task);
        using var form = new TodaysBillsForm(Factory(_client.Object, LoggedIn(UserRole.Cashier)), features.Object);
        _ = form.ShowTodaysBillsAsync();

        _ = form.ShowTodaysBillsAsync();

        features.Verify(f => f.GetAsync(), Times.Once);
    }

    [Fact]
    public async Task LoadAsync_ForAStoreManager_IsTodayOnly()
    {
        ListsToday();
        using var form = Form(UserRole.StoreManager);

        await form.LoadAsync();

        ((InvoiceHistoryView)Find(form, "InvoiceHistoryView")).IsTodayOnly.Should()
                                                                          .BeTrue();
    }

    [Fact]
    public async Task LoadAsync_WhenOpened_ListsToday()
    {
        ListsToday();
        using var form = Form(UserRole.Cashier);

        await form.LoadAsync();

        _client.Verify(c => c.ListSalesAsync(Today, Today, 1, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()),
                       Times.Once);
    }

    private void ListsToday() =>
        _client.Setup(c => c.ListSalesAsync(Today, Today, 1, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()))
               .ReturnsAsync(Page(hasMore: false, 1042));

    private TodaysBillsForm Form(UserRole role) =>
        new(Factory(_client.Object, LoggedIn(role)), Features(GeneralHardware));
}
