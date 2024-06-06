using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.InventoryProducts.Queries.GetByDescriptionKeyword;

public record GetInventoryProductsByDescriptionKeywordQuery(string Keyword) : IQuery<IEnumerable<InventoryProductDto>>;