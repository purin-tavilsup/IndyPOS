using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;

namespace IndyPOS.Application.InventoryProducts.Queries.GetInventoryProductsByCategoryId;

public class GetInventoryProductsByCategoryIdQueryHandler : IQueryHandler<GetInventoryProductsByCategoryIdQuery, IEnumerable<InventoryProductDto>>
{
	private readonly IInventoryProductRepository _productRepository;

	public GetInventoryProductsByCategoryIdQueryHandler(IInventoryProductRepository inventoryProductRepository)
	{
		_productRepository = inventoryProductRepository;
	}

    public Task<IEnumerable<InventoryProductDto>> Handle(GetInventoryProductsByCategoryIdQuery request, CancellationToken cancellationToken)
	{
		var categoryId = request.CategoryId;
		var results = _productRepository.GetProductsByCategoryId(categoryId);

		return Task.FromResult(results.Select(x => x.ToDto()));
	}
}