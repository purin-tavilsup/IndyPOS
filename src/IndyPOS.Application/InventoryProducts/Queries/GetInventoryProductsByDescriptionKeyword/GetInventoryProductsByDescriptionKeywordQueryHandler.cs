using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Common.Interfaces;

namespace IndyPOS.Application.InventoryProducts.Queries.GetInventoryProductsByDescriptionKeyword;

public class GetInventoryProductsByDescriptionKeywordQueryHandler : IQueryHandler<GetInventoryProductsByDescriptionKeywordQuery, IEnumerable<InventoryProductDto>>
{
	private readonly IInventoryProductRepository _productRepository;

	public GetInventoryProductsByDescriptionKeywordQueryHandler(IInventoryProductRepository productRepository)
	{
		_productRepository = productRepository;
	}

	public Task<IEnumerable<InventoryProductDto>> Handle(GetInventoryProductsByDescriptionKeywordQuery query, CancellationToken cancellationToken)
	{
		var keyword = query.Keyword;
		var results = _productRepository.GetProductsByDescriptionKeyword(keyword);

		return Task.FromResult(results.Select(x => x.ToDto()));
	}
}