using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.InventoryProducts.Commands.UpdateInventoryProduct;

public record UpdateInventoryProductCommand : ICommand
{
	public int Id { get; set; }

	public string Description { get; set; } = string.Empty;

	public string Manufacturer { get; set; } = string.Empty;

	public string Brand { get; set; } = string.Empty;

	public int Category { get; set; } = 0;

	public decimal UnitPrice { get; set; } = 0m;

	public int QuantityInStock { get; set; } = 0;

	public int? GroupPriceQuantity { get; set; }

	public decimal GroupPrice { get; set; }
}