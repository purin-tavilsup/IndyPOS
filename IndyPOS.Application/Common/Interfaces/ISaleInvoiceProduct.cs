namespace IndyPOS.Application.Common.Interfaces;

public interface ISaleInvoiceProduct
{
    int InvoiceProductId { get; set; }

    int Priority { get; set; }

    int InvoiceId { get; set; }

    int InventoryProductId { get; set; }

    string Barcode { get; set; }

    string Description { get; set; }

    string Manufacturer { get; set; }

    string Brand { get; set; }

    int Category { get; set; }

    decimal UnitPrice { get; set; }

    int Quantity { get; set; }

    decimal? GroupPrice { get; set; }

    int? GroupPriceQuantity { get; set; }

    bool IsTrackable { get; }

    string Note { get; set; }
}