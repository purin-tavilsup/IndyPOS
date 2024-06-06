using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.InventoryProducts.Commands.UpdateQuantity;

public class UpdateInventoryProductQuantityCommand : ICommand
{
	public int Id { get; set; }

	public int Quantity { get; set; }
}