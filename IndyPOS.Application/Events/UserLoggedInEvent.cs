using IndyPOS.Application.Interfaces;
using Prism.Events;

namespace IndyPOS.Application.Events;

/// <summary>
/// Event for notifying a user has logged in.
/// </summary>
public class UserLoggedInEvent : PubSubEvent<IUserAccount>
{
}