using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.InventoryProducts.Queries.GetInventoryProductById;

public record GetInventoryProductByIdQuery(int Id) : IQuery<InventoryProductDto>;