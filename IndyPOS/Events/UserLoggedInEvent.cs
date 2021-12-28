using IndyPOS.Users;
using Prism.Events;

namespace IndyPOS.Events
{
    /// <summary>
    /// Event for notifying a user has logged in.
    /// </summary>
    internal class UserLoggedInEvent : PubSubEvent<IUser>
    {
    }
}
