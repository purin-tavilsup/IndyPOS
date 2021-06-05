using Prism.Events;

namespace IndyPOS.EventAggregator
{
    /// <summary>
    /// Event for notifying a product has been added to sale invoice.
    /// Product barcode will be passed along with the event
    /// </summary>
    public class SaleInvoiceProductAddedEvent : PubSubEvent<string>
    {
    }
}
