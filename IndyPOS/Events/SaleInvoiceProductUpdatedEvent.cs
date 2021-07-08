using Prism.Events;

namespace IndyPOS.Events
{
	/// <summary>
	/// Event for notifying a product on sale invoice has been updated.
	/// Product barcode will be passed along with the event.
	/// </summary>
	public class SaleInvoiceProductUpdatedEvent : PubSubEvent<string>
	{
	}
}