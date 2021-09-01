using Prism.Events;

namespace IndyPOS.Events
{
	/// <summary>
	/// Event for notifying an inventory product has been updated.
	/// InventoryProductId will be passed along with the event.
	/// </summary>
	public class InventoryProductUpdatedEvent : PubSubEvent<int>
	{
	}
}