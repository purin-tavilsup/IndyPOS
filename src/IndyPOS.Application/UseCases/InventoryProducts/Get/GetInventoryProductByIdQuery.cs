using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UseCases.InventoryProducts.Get;

public record GetInventoryProductByIdQuery(int Id) : IQuery<InventoryProductDto>;