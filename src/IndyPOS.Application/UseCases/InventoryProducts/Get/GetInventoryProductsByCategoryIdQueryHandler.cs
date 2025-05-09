using IndyPOS.Application.Abstractions.Pos.Repositories;
using Nokpirab;

namespace IndyPOS.Application.UseCases.InventoryProducts.Get;

public class GetInventoryProductsByCategoryIdQueryHandler : IQueryHandler<GetInventoryProductsByCategoryIdQuery, IEnumerable<InventoryProductDto>>
{
	private readonly IInventoryProductRepository _productRepository;

	public GetInventoryProductsByCategoryIdQueryHandler(IInventoryProductRepository inventoryProductRepository)
	{
		_productRepository = inventoryProductRepository;
	}

	public Task<IEnumerable<InventoryProductDto>> HandleAsync(GetInventoryProductsByCategoryIdQuery query, CancellationToken cancellationToken = default)
	{
		var categoryId = query.CategoryId;
		var results = _productRepository.GetProductsByCategoryId(categoryId);

		return Task.FromResult(results.Select(x => x.ToDto()));
	}
}