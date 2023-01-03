using System.Diagnostics.CodeAnalysis;
using IndyPOS.Application.Interfaces;

namespace IndyPOS.Application.Models;

[ExcludeFromCodeCoverage]
public class InventoryProduct : IInventoryProduct
{
	public int InventoryProductId { get; }

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

	public string DateCreated { get; }

	public string DateUpdated { get; }
}