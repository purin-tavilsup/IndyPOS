namespace IndyPOS.SaleInvoice
{
	public interface ISaleInvoiceProduct
    {
        int InvoiceProductId { get; set; }

        int InvoiceId { get; set; }

        int InventoryProductId { get; set; }

        string SKU { get; set; }

        string Barcode { get; set; }

        string Description { get; set; }

        string Manufacturer { get; set; }

        string Brand { get; set; }

        int Category { get; set; }

        decimal? UnitCost { get; set; }

        decimal UnitPrice { get; set; }

        int Quantity { get; set; }
    }
}
