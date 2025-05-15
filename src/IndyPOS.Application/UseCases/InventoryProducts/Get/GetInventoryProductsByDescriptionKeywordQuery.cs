using Nokpirab;

namespace IndyPOS.Application.UseCases.InventoryProducts.Get;

public record GetInventoryProductsByDescriptionKeywordQuery(string Keyword) : IQuery<IEnumerable<InventoryProductDto>>;