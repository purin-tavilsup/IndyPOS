using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Abstractions.Pos.Repositories;

namespace IndyPOS.Application.UseCases.InventoryProducts.Update;

public class UpdateInventoryProductBarcodeCounterCommandHandler : ICommandHandler<UpdateInventoryProductBarcodeCounterCommand>
{
	private readonly IInventoryProductRepository _productRepository;

	public UpdateInventoryProductBarcodeCounterCommandHandler(IInventoryProductRepository productRepository)
	{
		_productRepository = productRepository;
	}

	public Task Handle(UpdateInventoryProductBarcodeCounterCommand command, CancellationToken cancellationToken)
	{
		_ = _productRepository.UpdateProductBarcodeCounter(command.Counter);

		return Task.CompletedTask;
	}
}