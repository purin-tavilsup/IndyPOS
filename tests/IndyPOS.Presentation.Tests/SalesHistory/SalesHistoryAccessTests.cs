using FluentAssertions;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Presentation.SalesHistory;
using Moq;
using Xunit;

namespace IndyPOS.Presentation.Tests.SalesHistory;

public class SalesHistoryAccessTests
{
    private const int UnknownRole = 0;

    [Fact]
    public void For_WithAnUnknownRole_GrantsNothing()
    {
        var access = SalesHistoryAccess.For(User(UnknownRole));

        access.Should()
              .BeEquivalentTo(new { CanViewAnyDay = false, CanReprint = false });
    }

    [Fact]
    public void For_WithNoLastName_TrimsTheDisplayName()
    {
        var access = SalesHistoryAccess.For(User((int)UserRole.Cashier, lastName: ""));

        access.UserDisplayName.Should()
                              .Be("Malee");
    }

    [Fact]
    public void For_WithACashier_IsHeldToToday()
    {
        var access = SalesHistoryAccess.For(User((int)UserRole.Cashier));

        access.IsTodayOnly.Should()
                          .BeTrue();
    }

    [Fact]
    public void For_WithACashier_MayReprint()
    {
        var access = SalesHistoryAccess.For(User((int)UserRole.Cashier));

        access.CanReprint.Should()
                         .BeTrue();
    }

    [Fact]
    public void For_WithAStoreManager_MayViewAnyDay()
    {
        var access = SalesHistoryAccess.For(User((int)UserRole.StoreManager));

        access.CanViewAnyDay.Should()
                            .BeTrue();
    }

    private static ILoggedInUser User(int roleId, string lastName = "Cashier")
    {
        var user = new Mock<ILoggedInUser>();
        user.SetupGet(u => u.FirstName).Returns("Malee");
        user.SetupGet(u => u.LastName).Returns(lastName);
        user.SetupGet(u => u.RoleId).Returns(roleId);

        return user.Object;
    }
}
