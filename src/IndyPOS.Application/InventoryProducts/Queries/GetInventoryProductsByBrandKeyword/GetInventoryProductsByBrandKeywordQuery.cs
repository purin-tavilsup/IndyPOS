using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.InventoryProducts.Queries.GetInventoryProductsByBrandKeyword;

public record GetInventoryProductsByBrandKeywordQuery(string Keyword) : IQuery<IEnumerable<InventoryProductDto>>;