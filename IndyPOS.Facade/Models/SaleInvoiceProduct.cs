using IndyPOS.Facade.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Facade.Models
{
    [ExcludeFromCodeCoverage]
	public class SaleInvoiceProduct : ISaleInvoiceProduct
    {
        public int InvoiceProductId { get; set; }

        public int InvoiceId { get; set; }

        public int InventoryProductId { get; set; }

        public string Barcode { get; set; }

        public string Description { get; set; }

        public string Manufacturer { get; set; }

        public string Brand { get; set; }

        public int Category { get; set; }

        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        public decimal? GroupPrice { get; set; }

        public int? GroupPriceQuantity { get; set; }

		public int Priority { get; set; }

        public bool IsTrackable { get; set; }

        public string Note { get; set; }
    }
}
