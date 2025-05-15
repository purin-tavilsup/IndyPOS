using IndyPOS.Application.Abstractions.Pos.Repositories;
using Nokpirab;

namespace IndyPOS.Application.UseCases.InventoryProducts.Update;

public class UpdateInventoryProductBarcodeCounterCommandHandler : ICommandHandler<UpdateInventoryProductBarcodeCounterCommand>
{
	private readonly IInventoryProductRepository _productRepository;

	public UpdateInventoryProductBarcodeCounterCommandHandler(IInventoryProductRepository productRepository)
	{
		_productRepository = productRepository;
	}

	public Task HandleAsync(UpdateInventoryProductBarcodeCounterCommand command, CancellationToken cancellationToken = default)
	{
		_ = _productRepository.UpdateProductBarcodeCounter(command.Counter);

		return Task.CompletedTask;
	}
}