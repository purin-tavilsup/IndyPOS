using AutoMapper;
using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;

namespace IndyPOS.Application.InventoryProducts.Queries.GetInventoryProductById;

public class GetInventoryProductByIdQueryHandler : IQueryHandler<GetInventoryProductByIdQuery, InventoryProductDto>
{
	private readonly IInventoryProductRepository _productRepository;
	private readonly IMapper _mapper;

	public GetInventoryProductByIdQueryHandler(IInventoryProductRepository inventoryProductRepository, IMapper mapper)
	{
		_productRepository = inventoryProductRepository;
		_mapper = mapper;
	}

	public Task<InventoryProductDto> Handle(GetInventoryProductByIdQuery request, CancellationToken cancellationToken)
	{
		var id = request.Id;
		var result = _productRepository.GetById(id);
		var product = _mapper.Map<InventoryProductDto>(result);

		return Task.FromResult(product);
	}
}