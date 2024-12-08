using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UseCases.InventoryProducts.Get;

public record GetInventoryProductsByBrandKeywordQuery(string Keyword) : IQuery<IEnumerable<InventoryProductDto>>;