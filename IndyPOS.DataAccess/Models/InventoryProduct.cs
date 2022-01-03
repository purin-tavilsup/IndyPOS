using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.DataAccess.Models
{
	[ExcludeFromCodeCoverage]
	public class InventoryProduct
    {
        public int InventoryProductId { get; set; }

        public string Barcode { get; set; }

        public string Description { get; set; }

        public string Manufacturer { get; set; }

        public string Brand { get; set; }

        public int Category { get; set; }

        public decimal UnitPrice { get; set; }

        public int QuantityInStock { get; set; }

        public decimal? GroupPrice { get; set; }

        public int? GroupPriceQuantity { get; set; }

        public bool IsTrackable { get; set; }

        public string DateCreated { get; set; }

        public string DateUpdated { get; set; }
    }
}
