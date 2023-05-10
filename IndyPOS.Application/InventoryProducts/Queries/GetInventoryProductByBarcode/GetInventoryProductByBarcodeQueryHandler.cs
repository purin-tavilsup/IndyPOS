using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;

namespace IndyPOS.Application.InventoryProducts.Queries.GetInventoryProductByBarcode;

public class GetInventoryProductByBarcodeQueryHandler : IQueryHandler<GetInventoryProductByBarcodeQuery, InventoryProductDto>
{
	private readonly IInventoryProductRepository _productRepository;

	public GetInventoryProductByBarcodeQueryHandler(IInventoryProductRepository productRepository)
	{
		_productRepository = productRepository;
	}

	public Task<InventoryProductDto> Handle(GetInventoryProductByBarcodeQuery request, CancellationToken cancellationToken)
	{
		var barcode = request.Barcode;
		var result = _productRepository.GetByBarcode(barcode);

		return Task.FromResult(result.ToDto());
	}
}