using Prism.Events;

namespace IndyPOS.Facade.Events
{
    /// <summary>
    /// Event for notifying a product has been removed from sale invoice.
    /// </summary>
    public class SaleInvoiceProductRemovedEvent : PubSubEvent
    {
    }
}