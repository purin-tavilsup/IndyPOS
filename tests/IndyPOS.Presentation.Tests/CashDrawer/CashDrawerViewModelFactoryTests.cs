using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Presentation.CashDrawer;
using IndyPOS.Presentation.SalesHistory;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;

namespace IndyPOS.Presentation.Tests.CashDrawer;

public class CashDrawerViewModelFactoryTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 10, 18, 3, 0, 0, TimeSpan.Zero);
    private static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    private readonly Mock<ICurrentUser> _currentUser = new();

    [Fact]
    public void Create_WithNobodyLoggedIn_ThrowsNotLoggedIn()
    {
        var act = () => Factory().Create(features: null);

        act.Should()
           .Throw<InvalidOperationException>()
           .WithMessage(CashDrawerViewModelFactory.NotLoggedIn);
    }

    [Fact]
    public void Create_ForACashier_CannotPickAnotherDay()
    {
        _currentUser.SetupGet(c => c.User).Returns(User(UserRole.Cashier));

        var vm = Factory().Create(features: null);

        vm.CanPickDay.Should()
                     .BeFalse();
    }

    [Fact]
    public void Create_ForAStoreManager_CanPickAnotherDay()
    {
        _currentUser.SetupGet(c => c.User).Returns(User(UserRole.StoreManager));

        var vm = Factory().Create(features: null);

        vm.CanPickDay.Should()
                     .BeTrue();
    }

    [Fact]
    public void For_ACashier_CanOpenTheDrawer()
    {
        var access = CashDrawerAccess.For(User(UserRole.Cashier));

        access.CanOpen.Should()
                      .BeTrue();
    }

    private CashDrawerViewModelFactory Factory() =>
        new(Mock.Of<IStoreHubClient>(), new StoreClock(new FakeTimeProvider(NowUtc), Bangkok), _currentUser.Object);

    private static ILoggedInUser User(UserRole role)
    {
        var user = new Mock<ILoggedInUser>();
        user.SetupGet(u => u.FirstName).Returns("Malee");
        user.SetupGet(u => u.LastName).Returns("Cashier");
        user.SetupGet(u => u.RoleId).Returns((int)role);

        return user.Object;
    }
}
