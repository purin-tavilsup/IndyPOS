namespace IndyPOS.Application.UseCases.InventoryProducts;

public record InventoryProductDto
{
	/// <summary>
	/// Primary product ID (StoreHub UUID).
	/// </summary>
	public required Guid Id { get; init; }

	/// <summary>
	/// Legacy SQLite product ID. Zero for StoreHub-only products.
	/// </summary>
	[Obsolete("Use Id (Guid) instead. Kept for legacy SQLite compatibility.")]
	public int InventoryProductId { get; init; }

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