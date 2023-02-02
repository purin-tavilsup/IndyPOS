using AutoMapper;
using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;

namespace IndyPOS.Application.InventoryProducts.Queries.GetInventoryProductByBarcode;

public class GetInventoryProductByBarcodeQueryHandler : IQueryHandler<GetInventoryProductByBarcodeQuery, InventoryProductDto>
{
	private readonly IInventoryProductRepository _productRepository;
	private readonly IMapper _mapper;

	public GetInventoryProductByBarcodeQueryHandler(IInventoryProductRepository productRepository, IMapper mapper)
	{
		_productRepository = productRepository;
		_mapper = mapper;
	}

	public Task<InventoryProductDto> Handle(GetInventoryProductByBarcodeQuery request, CancellationToken cancellationToken)
	{
		var barcode = request.Barcode;
		var result = _productRepository.GetByBarcode(barcode);
		var product = _mapper.Map<InventoryProductDto>(result);

		return Task.FromResult(product);
	}
}