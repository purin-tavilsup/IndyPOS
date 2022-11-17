using IndyPOS.Facade.Interfaces;
using Prism.Events;

namespace IndyPOS.Facade.Events;

/// <summary>
/// Event for notifying a user has logged in.
/// </summary>
public class UserLoggedInEvent : PubSubEvent<IUserAccount>
{
}