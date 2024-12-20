﻿using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Domain.Entities;

[ExcludeFromCodeCoverage]
public class InventoryProduct
{
	public int InventoryProductId { get; set; }

	public string Barcode { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public string Manufacturer { get; set; } = string.Empty;

	public string Brand { get; set; } = string.Empty;

	public int Category { get; set; }

	public decimal UnitPrice { get; set; }

	public int QuantityInStock { get; set; }

	public decimal GroupPrice { get; set; }

	public int? GroupPriceQuantity { get; set; }

	public bool IsTrackable { get; set; }

	public string DateCreated { get; set; } = string.Empty;

	public string DateUpdated { get; set; } = string.Empty;
}