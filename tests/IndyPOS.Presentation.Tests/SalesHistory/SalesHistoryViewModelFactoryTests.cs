using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Receipts;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Presentation.SalesHistory;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;

namespace IndyPOS.Presentation.Tests.SalesHistory;

public class SalesHistoryViewModelFactoryTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 10, 15, 3, 0, 0, TimeSpan.Zero);
    private static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    private readonly Mock<ICurrentUser> _currentUser = new();

    [Fact]
    public void CreateDetail_WithNobodyLoggedIn_ThrowsNotLoggedIn()
    {
        var act = () => Factory().CreateDetail();

        act.Should()
           .Throw<InvalidOperationException>()
           .WithMessage(SalesHistoryViewModelFactory.NotLoggedIn);
    }

    [Fact]
    public void CreateHistory_WithNobodyLoggedIn_ThrowsNotLoggedIn()
    {
        var act = () => Factory().CreateHistory();

        act.Should()
           .Throw<InvalidOperationException>()
           .WithMessage(SalesHistoryViewModelFactory.NotLoggedIn);
    }

    [Fact]
    public void CreateTodayOnly_ForAStoreManager_HoldsReprintToToday()
    {
        _currentUser.SetupGet(c => c.User).Returns(User((int)UserRole.StoreManager));
        var history = Factory().CreateTodayOnly();

        history.Detail.Show(Bill(new DateTime(2026, 10, 14, 2, 0, 0, DateTimeKind.Utc)));

        history.Detail.ReprintCommand.CanExecute(null).Should()
                                                      .BeFalse();
    }

    [Fact]
    public void CreateTodayOnly_ForAStoreManager_IsTodayOnly()
    {
        _currentUser.SetupGet(c => c.User).Returns(User((int)UserRole.StoreManager));

        var history = Factory().CreateTodayOnly();

        history.IsTodayOnly.Should()
                           .BeTrue();
    }

    [Fact]
    public void CreateHistory_ForAStoreManager_CanPickAPeriod()
    {
        _currentUser.SetupGet(c => c.User).Returns(User((int)UserRole.StoreManager));

        var history = Factory().CreateHistory();

        history.ShowThisMonthCommand.CanExecute(null).Should()
                                                     .BeTrue();
    }

    [Fact]
    public void CreateDetail_ForACashier_HoldsReprintToToday()
    {
        _currentUser.SetupGet(c => c.User).Returns(User((int)UserRole.Cashier));
        var detail = Factory().CreateDetail();

        detail.Show(Bill(new DateTime(2026, 10, 14, 2, 0, 0, DateTimeKind.Utc)));

        detail.ReprintCommand.CanExecute(null).Should()
                                              .BeFalse();
    }

    [Fact]
    public void CreateDetail_ForAStoreManager_ReprintsAnyDay()
    {
        _currentUser.SetupGet(c => c.User).Returns(User((int)UserRole.StoreManager));
        var detail = Factory().CreateDetail();

        detail.Show(Bill(new DateTime(2026, 10, 14, 2, 0, 0, DateTimeKind.Utc)));

        detail.ReprintCommand.CanExecute(null).Should()
                                              .BeTrue();
    }

    private SalesHistoryViewModelFactory Factory() =>
        new(Mock.Of<IStoreHubClient>(), Mock.Of<IReceiptPrinterService>(), new ReceiptDocumentFactory(Bangkok),
            new StoreClock(new FakeTimeProvider(NowUtc), Bangkok), _currentUser.Object, Mock.Of<IPaymentMethodNames>());

    private static ILoggedInUser User(int roleId)
    {
        var user = new Mock<ILoggedInUser>();
        user.SetupGet(u => u.FirstName).Returns("Malee");
        user.SetupGet(u => u.LastName).Returns("Cashier");
        user.SetupGet(u => u.RoleId).Returns(roleId);

        return user.Object;
    }

    private static InvoiceDetailDto Bill(DateTime createdUtc) => new(
        Guid.NewGuid(), 1042, "store-a", Guid.NewGuid(), "Malee Cashier", 100m, 100m, 0m, false, false, 0m,
        createdUtc, [], []);
}
