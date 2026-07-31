using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Products.Update;

/// <summary>
/// Command to update an existing product in StoreHub.
/// Does NOT update quantity - use AdjustQuantity for that.
/// </summary>
public record UpdateProductCommand : ICommand<ProductDto>
{
    public required Guid Id { get; init; }
    public required string Barcode { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }

    /// <summary>
    /// Catalogue category code. Required since the categories epic: the handler resolves it
    /// against this store's catalogue, so there is no longer a meaningful "no category" product.
    /// </summary>
    public required string Category { get; init; }
    public string? Brand { get; init; }
    public string? Manufacturer { get; init; }
    public required decimal UnitPrice { get; init; }
    public decimal? GroupPrice { get; init; }
    public int? GroupPriceQuantity { get; init; }
}
