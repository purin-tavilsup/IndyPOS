using Prism.Events;

namespace IndyPOS.Events
{
	/// <summary>
	/// Event for notifying a product has been added to InventoryProducts.
	/// InventoryProductId will be passed along with the event.
	/// </summary>
	public class InventoryProductAddedEvent : PubSubEvent<int>
	{
	}
}