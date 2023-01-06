namespace IndyPOS.Application.Common.Interfaces;

public interface IInventoryProduct
{
	int InventoryProductId { get; }

	string Barcode { get; set; }

	string Description { get; set; }

	string Manufacturer { get; set; }

	string Brand { get; set; }

	int Category { get; set; }

	decimal UnitPrice { get; set; }

	int QuantityInStock { get; set; }

	decimal? GroupPrice { get; set; }

	int? GroupPriceQuantity { get; set; }

	bool IsTrackable { get; set; }

	string DateCreated { get; }

	string DateUpdated { get; }
}