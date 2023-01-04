using Prism.Events;

namespace IndyPOS.Application.Events;

/// <summary>
/// Event for notifying all payments have been removed from sale invoice.
/// </summary>
public class AllPaymentsRemovedEvent : PubSubEvent
{
}