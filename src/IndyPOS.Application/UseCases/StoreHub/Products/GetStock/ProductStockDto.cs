namespace IndyPOS.Application.UseCases.StoreHub.Products.GetStock;

/// <summary>
/// Current stock for one product, as SUM(InventoryMovement.QuantityDelta).
/// Deliberately not part of ProductDto: ProductDto is cached for the whole session
/// by the POS, and a cached quantity would be stale after the first sale.
/// </summary>
public record ProductStockDto(Guid ProductId, int Quantity);
