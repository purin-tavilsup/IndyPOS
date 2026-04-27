using IndyPOS.Application.UseCases.InventoryProducts;

namespace IndyPOS.Application.Common.Interfaces;

/// <summary>
/// Service for managing inventory products.
/// Abstracts the underlying data source (SQLite, StoreHub API, etc.)
/// </summary>
public interface IInventoryProductService
{
    /// <summary>
    /// Create a new inventory product.
    /// </summary>
    Task<InventoryProductDto> CreateAsync(CreateInventoryProductRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing inventory product.
    /// </summary>
    Task<InventoryProductDto> UpdateAsync(UpdateInventoryProductRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft delete an inventory product (sets IsActive = false).
    /// </summary>
    Task DeleteAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adjust product quantity via inventory movement.
    /// </summary>
    Task<InventoryProductDto> AdjustQuantityAsync(Guid productId, int targetQuantity, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate the next available barcode for the store.
    /// </summary>
    Task<string> GenerateBarcodeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get an inventory product by barcode.
    /// </summary>
    Task<InventoryProductDto> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all cached inventory products.
    /// </summary>
    Task<IReadOnlyList<InventoryProductDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get inventory products by category ID.
    /// </summary>
    Task<IReadOnlyList<InventoryProductDto>> GetByCategoryIdAsync(int categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search inventory products by description keyword.
    /// </summary>
    Task<IReadOnlyList<InventoryProductDto>> SearchByDescriptionAsync(string keyword, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search inventory products by brand keyword.
    /// </summary>
    Task<IReadOnlyList<InventoryProductDto>> SearchByBrandAsync(string keyword, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request to create a new inventory product.
/// </summary>
public record CreateInventoryProductRequest
{
    public required string Barcode { get; init; }
    public required string Description { get; init; }
    public string? Manufacturer { get; init; }
    public string? Brand { get; init; }
    public required int Category { get; init; }
    public required decimal UnitPrice { get; init; }
    public int QuantityInStock { get; init; }
    public int? GroupPriceQuantity { get; init; }
    public decimal? GroupPrice { get; init; }
    public bool IsTrackable { get; init; } = true;
}

/// <summary>
/// Request to update an existing inventory product.
/// </summary>
public record UpdateInventoryProductRequest
{
    public required Guid Id { get; init; }
    public required string Description { get; init; }
    public string? Manufacturer { get; init; }
    public string? Brand { get; init; }
    public required int Category { get; init; }
    public required decimal UnitPrice { get; init; }
    public int QuantityInStock { get; init; }
    public int? GroupPriceQuantity { get; init; }
    public decimal? GroupPrice { get; init; }
}
