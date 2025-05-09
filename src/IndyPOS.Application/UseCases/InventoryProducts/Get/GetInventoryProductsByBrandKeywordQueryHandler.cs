using IndyPOS.Application.Abstractions.Pos.Repositories;
using Nokpirab;

namespace IndyPOS.Application.UseCases.InventoryProducts.Get;

public class GetInventoryProductsByBrandKeywordQueryHandler : IQueryHandler<GetInventoryProductsByBrandKeywordQuery, IEnumerable<InventoryProductDto>>
{
	private readonly IInventoryProductRepository _productRepository;

	public GetInventoryProductsByBrandKeywordQueryHandler(IInventoryProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

	public Task<IEnumerable<InventoryProductDto>> HandleAsync(GetInventoryProductsByBrandKeywordQuery query, CancellationToken cancellationToken = default)
	{
		var keyword = query.Keyword;
		var results = _productRepository.GetProductsByBrandKeyword(keyword);

		return Task.FromResult(results.Select(x => x.ToDto()));
	}
}