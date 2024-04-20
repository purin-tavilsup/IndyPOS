namespace IndyPOS.Application.InventoryProducts;

public record InventoryProductDto(
	int InventoryProductId,
	string Barcode,
	string Description,
	string Manufacturer,
	string Brand,
	int Category,
	decimal UnitPrice,
	int QuantityInStock,
	decimal GroupPrice,
	int? GroupPriceQuantity,
	bool IsTrackable,
	string DateCreated,
	string DateUpdated);