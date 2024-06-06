using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.InventoryProducts.Queries.GetByCategoryId;

public record GetInventoryProductsByCategoryIdQuery(int CategoryId) : IQuery<IEnumerable<InventoryProductDto>>;