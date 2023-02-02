using AutoMapper;
using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;

namespace IndyPOS.Application.InventoryProducts.Queries.GetInventoryProductsByCategoryId;

public class GetInventoryProductsByCategoryIdQueryHandler : IQueryHandler<GetInventoryProductsByCategoryIdQuery, IEnumerable<InventoryProductDto>>
{
	private readonly IInventoryProductRepository _productRepository;
	private readonly IMapper _mapper;

	public GetInventoryProductsByCategoryIdQueryHandler(IInventoryProductRepository inventoryProductRepository, IMapper mapper)
	{
		_productRepository = inventoryProductRepository;
		_mapper = mapper;
	}

    public Task<IEnumerable<InventoryProductDto>> Handle(GetInventoryProductsByCategoryIdQuery request, CancellationToken cancellationToken)
	{
		var categoryId = request.CategoryId;
		var results = _productRepository.GetProductsByCategoryId(categoryId);
		var products = _mapper.Map<IEnumerable<InventoryProductDto>>(results);

		return Task.FromResult(products);
	}
}