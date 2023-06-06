using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;

namespace IndyPOS.Application.InventoryProducts.Commands.UpdateInventoryProductBarcodeCounter;

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