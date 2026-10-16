using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using IndyPOS.Presentation.SalesHistory;
using IndyPOS.Windows.Forms.UI.Report;
using Moq;
using Xunit;
using static IndyPOS.Windows.Forms.Tests.UI.Report.SalesHistoryFixture;

namespace IndyPOS.Windows.Forms.Tests.UI.Report;

public class SalesHistoryReportPanelTests
{
    private static readonly StoreFeaturesDto MimyMart = new(PayLaterEnabled: false, MultipleProductTypesEnabled: false);

    private readonly Mock<IStoreHubClient> _client = new();

    public SalesHistoryReportPanelTests() =>
        _client.Setup(c => c.ListSalesAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<int>(), It.IsAny<int>(),
                                            It.IsAny<CancellationToken>()))
               .ReturnsAsync(Page(hasMore: false));

    [Fact]
    public async Task OpenAsync_WithNobodyLoggedIn_DoesNotThrow()
    {
        using var panel = new SalesHistoryReportPanel(Factory(_client.Object, NobodyLoggedIn()), Features(MimyMart));

        var open = () => panel.OpenAsync();

        await open.Should()
                  .NotThrowAsync();
    }

    [Fact]
    public async Task OpenAsync_AfterAnotherUserLogsIn_FollowsTheNewUser()
    {
        var current = new SwitchableUser(LoggedIn(UserRole.Cashier));
        using var panel = new SalesHistoryReportPanel(Factory(_client.Object, current), Features(MimyMart));
        await panel.OpenAsync();
        current.Now = LoggedIn(UserRole.SystemAdmin);

        await panel.OpenAsync();

        History(panel).IsTodayOnly.Should()
                                  .BeFalse();
    }

    [Fact]
    public async Task OpenAsync_AtAStoreWithoutTheSplit_HidesIt()
    {
        using var panel = new SalesHistoryReportPanel(Factory(_client.Object, LoggedIn(UserRole.StoreManager)), Features(MimyMart));

        await panel.OpenAsync();

        History(panel).ShowProductTypeSplit.Should()
                                           .BeFalse();
    }

    [Fact]
    public async Task OpenAsync_WhenOpened_ListsToday()
    {
        using var panel = new SalesHistoryReportPanel(Factory(_client.Object, LoggedIn(UserRole.StoreManager)), Features(MimyMart));

        await panel.OpenAsync();

        _client.Verify(c => c.ListSalesAsync(Today, Today, 1, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()),
                       Times.Once);
    }

    private static InvoiceHistoryView History(SalesHistoryReportPanel panel) =>
        (InvoiceHistoryView)Find(panel, "InvoiceHistoryView");

    // Another user logs in between two visits: the factory reads whoever is current now.
    private sealed class SwitchableUser(ICurrentUser now) : ICurrentUser
    {
        public ICurrentUser Now { get; set; } = now;

        public ILoggedInUser? User => Now.User;
    }
}
