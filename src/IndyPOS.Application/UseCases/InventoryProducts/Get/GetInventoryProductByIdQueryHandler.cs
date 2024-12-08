using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Abstractions.Pos.Repositories;

namespace IndyPOS.Application.UseCases.InventoryProducts.Get;

public class GetInventoryProductByIdQueryHandler : IQueryHandler<GetInventoryProductByIdQuery, InventoryProductDto>
{
	private readonly IInventoryProductRepository _productRepository;

	public GetInventoryProductByIdQueryHandler(IInventoryProductRepository inventoryProductRepository)
	{
		_productRepository = inventoryProductRepository;
	}

	public Task<InventoryProductDto> Handle(GetInventoryProductByIdQuery query, CancellationToken cancellationToken)
	{
		var id = query.Id;
		var result = _productRepository.GetById(id);

		return Task.FromResult(result.ToDto());
	}
}