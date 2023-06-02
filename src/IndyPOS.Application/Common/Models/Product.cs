using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Application.Common.Models;

[ExcludeFromCodeCoverage]
public class Product
{
	public int InventoryProductId { get; init; }

    public string Barcode { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Manufacturer { get; init; } = string.Empty;

    public string Brand { get; init; } = string.Empty;

    public int Category { get; init; }

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal? GroupPrice { get; init; }

    public int? GroupPriceQuantity { get; init; }

    public int Priority { get; set; }

    public bool IsTrackable { get; init; }

    public string Note { get; set; } = string.Empty;
}