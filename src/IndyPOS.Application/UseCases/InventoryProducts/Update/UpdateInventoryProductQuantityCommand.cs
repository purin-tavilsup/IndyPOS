using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UseCases.InventoryProducts.Update;

public class UpdateInventoryProductQuantityCommand : ICommand
{
	public int Id { get; set; }

	public int Quantity { get; set; }
}