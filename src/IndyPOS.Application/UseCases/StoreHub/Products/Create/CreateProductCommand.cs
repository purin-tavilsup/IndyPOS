using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Products.Create;

/// <summary>
/// Command to create a new product in StoreHub.
/// </summary>
public record CreateProductCommand : ICommand<ProductDto>
{
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
    public int? InitialQuantity { get; init; }
}
