using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;

namespace IndyPOS.Application.InventoryProducts.Queries.GetInventoryProductsByBrandKeyword;

public class GetInventoryProductsByBrandKeywordQueryHandler : IQueryHandler<GetInventoryProductsByBrandKeywordQuery, IEnumerable<InventoryProductDto>>
{
	private readonly IInventoryProductRepository _productRepository;

	public GetInventoryProductsByBrandKeywordQueryHandler(IInventoryProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public Task<IEnumerable<InventoryProductDto>> Handle(GetInventoryProductsByBrandKeywordQuery query, CancellationToken cancellationToken)
    {
		var keyword = query.Keyword;
		var results = _productRepository.GetProductsByBrandKeyword(keyword);

		return Task.FromResult(results.Select(x => x.ToDto()));
    }
}