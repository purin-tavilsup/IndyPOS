namespace IndyPOS.Application.UseCases.InvoiceProducts;

public record InvoiceProductDto(
	int InvoiceProductId,
	int Priority,
	int InvoiceId,
	int InventoryProductId,
	string Barcode,
	string Description,
	string Manufacturer,
	string Brand,
	int Category,
	decimal UnitPrice,
	int Quantity,
	string DateCreated,
	string Note,
	decimal GroupPrice,
	bool IsGroupProduct)
{
	/// <summary>
	/// Calculates the total price for this product line.
	/// Uses GroupPrice if IsGroupProduct, otherwise UnitPrice * Quantity.
	/// </summary>
	public decimal GetTotal() => IsGroupProduct ? GroupPrice : UnitPrice * Quantity;
}