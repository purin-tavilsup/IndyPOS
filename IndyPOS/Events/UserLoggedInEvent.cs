using IndyPOS.Interfaces;
using Prism.Events;

namespace IndyPOS.Events
{
    /// <summary>
    /// Event for notifying a user has logged in.
    /// </summary>
    internal class UserLoggedInEvent : PubSubEvent<IUserAccount>
    {
    }
}
