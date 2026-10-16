using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Events;
using Prism.Events;

namespace IndyPOS.Infrastructure.Services;

/// <summary>The logged-in user, kept from the same login/logout events every till panel listens to.</summary>
public sealed class EventCurrentUser : ICurrentUser
{
    public EventCurrentUser(IEventAggregator events)
    {
        // Method groups, not lambdas: Prism holds subscribers weakly, and this instance is what DI keeps alive.
        events.GetEvent<UserLoggedInEvent>().Subscribe(OnLoggedIn);
        events.GetEvent<UserLoggedOutEvent>().Subscribe(OnLoggedOut);
    }

    public ILoggedInUser? User { get; private set; }

    private void OnLoggedIn(ILoggedInUser user) => User = user;

    private void OnLoggedOut() => User = null;
}
