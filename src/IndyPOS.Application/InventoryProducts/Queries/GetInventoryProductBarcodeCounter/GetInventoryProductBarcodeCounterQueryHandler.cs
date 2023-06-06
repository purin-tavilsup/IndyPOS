using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;

namespace IndyPOS.Application.InventoryProducts.Queries.GetInventoryProductBarcodeCounter;

public class GetInventoryProductBarcodeCounterQueryHandler : IQueryHandler<GetInventoryProductBarcodeCounterQuery, int>
{
	private readonly IInventoryProductRepository _productRepository;

	public GetInventoryProductBarcodeCounterQueryHandler(IInventoryProductRepository productRepository)
	{
		_productRepository = productRepository;
	}

	public Task<int> Handle(GetInventoryProductBarcodeCounterQuery query, CancellationToken cancellationToken)
	{
		var result = _productRepository.GetProductBarcodeCounter();

		return Task.FromResult(result);
	}
}