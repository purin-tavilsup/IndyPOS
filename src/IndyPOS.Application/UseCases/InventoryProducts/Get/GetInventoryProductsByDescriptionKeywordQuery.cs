using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UseCases.InventoryProducts.Get;

public record GetInventoryProductsByDescriptionKeywordQuery(string Keyword) : IQuery<IEnumerable<InventoryProductDto>>;