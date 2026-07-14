using IndyPOS.Application.UseCases.StoreHub.Products;

namespace IndyPOS.Application.Abstractions.StoreHub;

/// <summary>
/// Local cache for products fetched from StoreHub.
/// Provides fast product lookup during sales without network calls.
/// </summary>
public interface IProductCacheService
{
    /// <summary>
    /// Sync products from StoreHub API to local cache.
    /// Should be called on app startup and periodically.
    /// </summary>
    Task<ProductSyncResult> SyncProductsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a product by its barcode.
    /// Returns null if not found in cache.
    /// </summary>
    ProductDto? GetByBarcode(string barcode);

    /// <summary>
    /// Get a product by its ID.
    /// Returns null if not found in cache.
    /// </summary>
    ProductDto? GetById(Guid productId);

    /// <summary>
    /// Search products by name or barcode.
    /// </summary>
    IReadOnlyList<ProductDto> Search(string searchTerm);

    /// <summary>
    /// Get all cached products.
    /// </summary>
    IReadOnlyList<ProductDto> GetAll();

    /// <summary>
    /// Get products by category.
    /// </summary>
    IReadOnlyList<ProductDto> GetByCategory(string category);

    /// <summary>
    /// Clear the entire product cache.
    /// Next sync will fetch all products fresh.
    /// </summary>
    void ClearCache();

    /// <summary>
    /// Add or update a product in the cache.
    /// Called after creating/updating a product via API.
    /// </summary>
    void UpsertProduct(ProductDto product);

    /// <summary>
    /// Remove a product from the cache.
    /// Called after deleting a product via API.
    /// </summary>
    void RemoveProduct(Guid productId);

    /// <summary>
    /// Check if cache has been populated.
    /// </summary>
    bool IsCachePopulated { get; }

    /// <summary>
    /// Get the last sync timestamp.
    /// </summary>
    DateTime? LastSyncTime { get; }

    /// <summary>
    /// Get the count of cached products.
    /// </summary>
    int CachedProductCount { get; }
}

/// <summary>
/// Result of a product sync operation.
/// </summary>
public record ProductSyncResult(
    bool Success,
    int ProductCount,
    string? ErrorMessage = null,
    TimeSpan? Duration = null);
