using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.InventoryProducts.Queries.GetById;

public record GetInventoryProductByIdQuery(int Id) : IQuery<InventoryProductDto>;