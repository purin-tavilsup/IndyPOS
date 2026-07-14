using Prism.Events;

namespace IndyPOS.Domain.Events;

/// <summary>
/// Event for notifying a product has been added to InventoryProducts.
/// StoreHub product ID (Guid) will be passed along with the event.
/// </summary>
public class InventoryProductAddedEvent : PubSubEvent<Guid>
{
}