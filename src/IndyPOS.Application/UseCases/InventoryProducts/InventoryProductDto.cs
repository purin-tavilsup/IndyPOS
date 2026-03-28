namespace IndyPOS.Application.UseCases.InventoryProducts;

public record InventoryProductDto
{
	/// <summary>
	/// Legacy SQLite product ID.
	/// </summary>
	public int InventoryProductId { get; init; }

	/// <summary>
	/// StoreHub product ID (UUID). Null for legacy SQLite-only products.
	/// </summary>
	public Guid? StoreHubProductId { get; init; }

	public string Barcode { get; init; } = string.Empty;
	public string Description { get; init; } = string.Empty;
	public string Manufacturer { get; init; } = string.Empty;
	public string Brand { get; init; } = string.Empty;
	public int Category { get; init; }
	public decimal UnitPrice { get; init; }
	public int QuantityInStock { get; init; }
	public decimal GroupPrice { get; init; }
	public int? GroupPriceQuantity { get; init; }
	public bool IsTrackable { get; init; }
	public string DateCreated { get; init; } = string.Empty;
	public string DateUpdated { get; init; } = string.Empty;
}