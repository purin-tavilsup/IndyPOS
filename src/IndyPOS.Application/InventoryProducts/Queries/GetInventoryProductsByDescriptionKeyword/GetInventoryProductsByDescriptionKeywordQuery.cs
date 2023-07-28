using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.InventoryProducts.Queries.GetInventoryProductsByDescriptionKeyword;

public record GetInventoryProductsByDescriptionKeywordQuery(string Keyword) : IQuery<IEnumerable<InventoryProductDto>>;