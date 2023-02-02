using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.InventoryProducts.Commands.CreateInventoryProduct;

public record CreateInventoryProductCommand : ICommand
{
	public string Barcode { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public string Manufacturer { get; set; } = string.Empty;

	public string Brand { get; set; } = string.Empty;

	public int Category { get; set; } = 0;

	public decimal UnitPrice { get; set; } = 0m;

	public int Quantity { get; set; } = 0;

	public bool IsTrackable { get; set; } = true;
}