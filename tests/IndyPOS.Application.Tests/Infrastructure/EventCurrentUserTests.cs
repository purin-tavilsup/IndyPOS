using FluentAssertions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Events;
using IndyPOS.Infrastructure.Services;
using Moq;
using Prism.Events;
using Xunit;

namespace IndyPOS.Application.Tests.Infrastructure;

public class EventCurrentUserTests
{
    private readonly EventAggregator _events = new();

    [Fact]
    public void User_BeforeAnyLogin_IsNull()
    {
        var current = new EventCurrentUser(_events);

        current.User.Should()
                    .BeNull();
    }

    [Fact]
    public void User_AfterLogout_IsNull()
    {
        var current = new EventCurrentUser(_events);
        _events.GetEvent<UserLoggedInEvent>().Publish(new Mock<ILoggedInUser>().Object);

        _events.GetEvent<UserLoggedOutEvent>().Publish();

        current.User.Should()
                    .BeNull();
    }

    [Fact]
    public void User_AfterLogin_IsTheLoggedInUser()
    {
        var current = new EventCurrentUser(_events);
        var user = new Mock<ILoggedInUser>().Object;

        _events.GetEvent<UserLoggedInEvent>().Publish(user);

        current.User.Should()
                    .BeSameAs(user);
    }
}
