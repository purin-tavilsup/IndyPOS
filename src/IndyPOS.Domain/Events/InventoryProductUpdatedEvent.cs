using Prism.Events;

namespace IndyPOS.Domain.Events;

/// <summary>
/// Event for notifying an inventory product has been updated.
/// StoreHub product ID (Guid) will be passed along with the event.
/// </summary>
public class InventoryProductUpdatedEvent : PubSubEvent<Guid>
{
}