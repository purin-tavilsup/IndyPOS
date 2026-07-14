namespace IndyPOS.Application.UseCases.StoreHub.Products.AdjustQuantity;

/// <summary>
/// Request body for adjusting product quantity.
/// </summary>
public record AdjustQuantityRequest(int TargetQuantity, string? Reason = null);
