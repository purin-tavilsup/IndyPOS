using Prism.Events;

namespace IndyPOS.Events
{
    /// <summary>
    /// Event for notifying a product has been removed from sale invoice.
    /// Product barcode will be passed along with the event
    /// </summary>
    public class SaleInvoiceProductRemovedEvent : PubSubEvent<string>
    {
    }
}
