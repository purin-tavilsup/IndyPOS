using IndyPOS.Application.Abstractions.Pos.Repositories;
using Nokpirab;

namespace IndyPOS.Application.UseCases.InventoryProducts.Get;

public class GetInventoryProductsByDescriptionKeywordQueryHandler : IQueryHandler<GetInventoryProductsByDescriptionKeywordQuery, IEnumerable<InventoryProductDto>>
{
	private readonly IInventoryProductRepository _productRepository;

	public GetInventoryProductsByDescriptionKeywordQueryHandler(IInventoryProductRepository productRepository)
	{
		_productRepository = productRepository;
	}

	public Task<IEnumerable<InventoryProductDto>> HandleAsync(GetInventoryProductsByDescriptionKeywordQuery query, CancellationToken cancellationToken = default)
	{
		var keyword = query.Keyword;
		var results = _productRepository.GetProductsByDescriptionKeyword(keyword);

		return Task.FromResult(results.Select(x => x.ToDto()));
	}
}