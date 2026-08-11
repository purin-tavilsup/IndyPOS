namespace IndyPOS.Application.UseCases.StoreHub.Products.AdjustQuantity;

/// <summary>
/// Restock or write-off by a signed amount. A delta, not a target quantity: a target
/// computed against a balance read moments earlier silently absorbs any sale that lands
/// in between.
/// </summary>
public record AdjustQuantityRequest(int Delta, string? Reason = null);
