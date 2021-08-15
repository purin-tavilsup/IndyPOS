namespace IndyPOS.SaleInvoice
{
	public class SaleInvoiceProduct : ISaleInvoiceProduct
    {
        public int InvoiceProductId { get; set; }

        public int InvoiceId { get; set; }

        public int InventoryProductId { get; set; }

        public string SKU { get; set; }

        public string Barcode { get; set; }

        public string Description { get; set; }

        public string Manufacturer { get; set; }

        public string Brand { get; set; }

        public int Category { get; set; }

        public decimal? UnitCost { get; set; }

        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }
    }
}
