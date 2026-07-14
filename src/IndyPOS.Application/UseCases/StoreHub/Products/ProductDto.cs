namespace IndyPOS.Application.UseCases.StoreHub.Products;

/// <summary>
/// DTO for StoreHub product data.
/// </summary>
public record ProductDto(
    Guid Id,
    string Barcode,
    string Name,
    string? Description,
    string? Category,
    string? Brand,
    string? Manufacturer,
    decimal UnitPrice,
    decimal? GroupPrice,
    int? GroupPriceQuantity,
    bool IsActive);
