using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Domain.Entities;

[ExcludeFromCodeCoverage]
public class InvoiceProduct
{
	public int InvoiceProductId { get; set; }
	public int Priority { get; set; }
	public int InvoiceId { get; set; }
	public int InventoryProductId { get; set; }
	public string Barcode { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public string Manufacturer { get; set; } = string.Empty;
	public string Brand { get; set; } = string.Empty;
	public int Category { get; set; }
	public int Quantity { get; set; }
	public string DateCreated { get; set; } = string.Empty;
	public string Note { get; set; } = string.Empty;
	public decimal UnitPrice { get; set; }
	public decimal GroupPrice { get; set; }
	public bool IsGroupProduct { get; set; }
	public decimal OriginalUnitPrice { get; set; }
}