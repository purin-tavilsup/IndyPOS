using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.InventoryProducts.Queries.GetByBrandKeyword;

public record GetInventoryProductsByBrandKeywordQuery(string Keyword) : IQuery<IEnumerable<InventoryProductDto>>;