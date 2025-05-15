using Nokpirab;

namespace IndyPOS.Application.UseCases.InventoryProducts.Get;

public record GetInventoryProductsByBrandKeywordQuery(string Keyword) : IQuery<IEnumerable<InventoryProductDto>>;