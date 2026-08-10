namespace IndyPOS.Application.UseCases.StoreHub.Products.AdjustQuantity;

/// <summary>
/// Result of an adjustment: the product and its balance after the movement was written.
/// The endpoint used to shape this anonymously while the client deserialized it as a
/// ProductDto, so every field of that DTO came back empty.
/// </summary>
public record AdjustQuantityResponse(Guid ProductId, int Quantity);
