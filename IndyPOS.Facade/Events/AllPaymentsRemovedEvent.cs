using Prism.Events;

namespace IndyPOS.Facade.Events;

/// <summary>
/// Event for notifying all payments have been removed from sale invoice.
/// </summary>
public class AllPaymentsRemovedEvent : PubSubEvent
{
}