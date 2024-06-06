using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Abstractions.Pos.Repositories;

namespace IndyPOS.Application.InventoryProducts.Queries.GetByCategoryId;

public class GetInventoryProductsByCategoryIdQueryHandler : IQueryHandler<GetInventoryProductsByCategoryIdQuery, IEnumerable<InventoryProductDto>>
{
	private readonly IInventoryProductRepository _productRepository;

	public GetInventoryProductsByCategoryIdQueryHandler(IInventoryProductRepository inventoryProductRepository)
	{
		_productRepository = inventoryProductRepository;
	}

    public Task<IEnumerable<InventoryProductDto>> Handle(GetInventoryProductsByCategoryIdQuery query, CancellationToken cancellationToken)
	{
		var categoryId = query.CategoryId;
		var results = _productRepository.GetProductsByCategoryId(categoryId);

		return Task.FromResult(results.Select(x => x.ToDto()));
	}
}