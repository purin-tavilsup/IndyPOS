using IndyPOS.Application.Abstractions.Pos.Repositories;
using Nokpirab;

namespace IndyPOS.Application.UseCases.InventoryProducts.Get;

public class GetInventoryProductBarcodeCounterQueryHandler : IQueryHandler<GetInventoryProductBarcodeCounterQuery, int>
{
	private readonly IInventoryProductRepository _productRepository;

	public GetInventoryProductBarcodeCounterQueryHandler(IInventoryProductRepository productRepository)
	{
		_productRepository = productRepository;
	}

	public Task<int> HandleAsync(GetInventoryProductBarcodeCounterQuery query, CancellationToken cancellationToken = default)
	{
		var result = _productRepository.GetProductBarcodeCounter();

		return Task.FromResult(result);
	}
}