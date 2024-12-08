using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UseCases.InventoryProducts.Get;

public record GetInventoryProductsByCategoryIdQuery(int CategoryId) : IQuery<IEnumerable<InventoryProductDto>>;