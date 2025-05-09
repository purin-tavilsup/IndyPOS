using Nokpirab;

namespace IndyPOS.Application.UseCases.InventoryProducts.Get;

public record GetInventoryProductsByCategoryIdQuery(int CategoryId) : IQuery<IEnumerable<InventoryProductDto>>;