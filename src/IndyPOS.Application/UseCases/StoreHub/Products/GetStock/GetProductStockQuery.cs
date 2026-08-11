using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Products.GetStock;

/// <summary>
/// Current stock for this store. Supply ProductId to narrow it to one product.
/// </summary>
public record GetProductStockQuery(Guid? ProductId = null)
    : IQuery<IReadOnlyList<ProductStockDto>>;
