using System.Collections.Concurrent;
using System.Diagnostics;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.UseCases.StoreHub.Products;
using Microsoft.Extensions.Logging;

namespace IndyPOS.Infrastructure.Services.StoreHub;

/// <summary>
/// In-memory product cache with thread-safe access.
/// Fetches products from StoreHub API and caches them locally.
/// </summary>
public class ProductCacheService : IProductCacheService
{
    private readonly IStoreHubClient _storeHubClient;
    private readonly ILogger<ProductCacheService> _logger;

    // Thread-safe collections for fast lookup
    private readonly ConcurrentDictionary<string, ProductDto> _productsByBarcode = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<Guid, ProductDto> _productsById = new();
    private volatile List<ProductDto> _allProducts = [];

    private readonly object _syncLock = new();
    private DateTime? _lastSyncTime;

    public ProductCacheService(IStoreHubClient storeHubClient, ILogger<ProductCacheService> logger)
    {
        _storeHubClient = storeHubClient;
        _logger = logger;
    }

    public bool IsCachePopulated => _allProducts.Count > 0;
    public DateTime? LastSyncTime => _lastSyncTime;
    public int CachedProductCount => _allProducts.Count;

    public async Task<ProductSyncResult> SyncProductsAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting product sync from StoreHub...");

            if (!_storeHubClient.IsAuthenticated)
            {
                return new ProductSyncResult(false, 0, "Not authenticated. Please login first.");
            }

            var products = await _storeHubClient.GetProductsAsync(
                activeOnly: true,
                cancellationToken: cancellationToken);

            lock (_syncLock)
            {
                // Clear existing cache
                _productsByBarcode.Clear();
                _productsById.Clear();

                // Populate cache
                foreach (var product in products)
                {
                    _productsById[product.Id] = product;

                    if (!string.IsNullOrEmpty(product.Barcode))
                    {
                        _productsByBarcode[product.Barcode] = product;
                    }
                }

                _allProducts = products.ToList();
                _lastSyncTime = DateTime.UtcNow;
            }

            stopwatch.Stop();

            _logger.LogInformation(
                "Product sync completed. Cached {Count} products in {Duration:F2}s",
                products.Count,
                stopwatch.Elapsed.TotalSeconds);

            return new ProductSyncResult(true, products.Count, Duration: stopwatch.Elapsed);
        }
        catch (StoreHubClientException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to sync products from StoreHub");
            return new ProductSyncResult(false, 0, ex.Message, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Unexpected error during product sync");
            return new ProductSyncResult(false, 0, $"Sync failed: {ex.Message}", stopwatch.Elapsed);
        }
    }

    public ProductDto? GetByBarcode(string barcode)
    {
        if (string.IsNullOrEmpty(barcode))
            return null;

        _productsByBarcode.TryGetValue(barcode, out var product);
        return product;
    }

    public ProductDto? GetById(Guid productId)
    {
        _productsById.TryGetValue(productId, out var product);
        return product;
    }

    public IReadOnlyList<ProductDto> Search(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return _allProducts;

        var term = searchTerm.Trim();

        return _allProducts
            .Where(p =>
                p.Barcode.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (p.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();
    }

    public IReadOnlyList<ProductDto> GetAll()
    {
        return _allProducts;
    }

    public IReadOnlyList<ProductDto> GetByCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return _allProducts;

        return _allProducts
            .Where(p => string.Equals(p.Category, category, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public void ClearCache()
    {
        lock (_syncLock)
        {
            _productsByBarcode.Clear();
            _productsById.Clear();
            _allProducts = [];
            _lastSyncTime = null;
        }

        _logger.LogInformation("Product cache cleared");
    }
}
