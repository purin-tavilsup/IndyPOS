using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.InventoryProducts;
using IndyPOS.Application.UseCases.StoreHub.Products;
using IndyPOS.Application.UseCases.StoreHub.Products.AdjustQuantity;
using IndyPOS.Application.UseCases.StoreHub.Products.Create;
using IndyPOS.Application.UseCases.StoreHub.Products.GetStock;
using IndyPOS.Application.UseCases.StoreHub.Products.Update;
using IndyPOS.Domain.Events;
using Microsoft.Extensions.Logging;

namespace IndyPOS.Infrastructure.Services.StoreHub;

/// <summary>
/// StoreHub-based implementation of IInventoryProductService.
/// Uses IStoreHubClient to communicate with StoreHub API.
/// </summary>
public class StoreHubInventoryProductService : IInventoryProductService
{
    private readonly IStoreHubClient _storeHubClient;
    private readonly IProductCacheService _productCacheService;
    private readonly IEventAggregator _eventAggregator;
    private readonly ILogger<StoreHubInventoryProductService> _logger;

    public StoreHubInventoryProductService(
        IStoreHubClient storeHubClient,
        IProductCacheService productCacheService,
        IEventAggregator eventAggregator,
        ILogger<StoreHubInventoryProductService> logger)
    {
        _storeHubClient = storeHubClient;
        _productCacheService = productCacheService;
        _eventAggregator = eventAggregator;
        _logger = logger;
    }

    public async Task<InventoryProductDto> CreateAsync(
        CreateInventoryProductRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating product: {Barcode} - {Description}", request.Barcode, request.Description);

        var command = new CreateProductCommand
        {
            Barcode = request.Barcode,
            Name = request.Description, // StoreHub uses "Name", WinForms uses "Description"
            Description = request.Description,
            Category = request.Category,
            Brand = request.Brand,
            Manufacturer = request.Manufacturer,
            UnitPrice = request.UnitPrice,
            GroupPrice = request.GroupPrice,
            GroupPriceQuantity = request.GroupPriceQuantity,
            InitialQuantity = request.IsTrackable ? request.QuantityInStock : null
        };

        var result = await _storeHubClient.CreateProductAsync(command, cancellationToken);

        // Update cache
        _productCacheService.UpsertProduct(result);

        // Publish event for UI refresh
        _eventAggregator.GetEvent<InventoryProductAddedEvent>().Publish(result.Id);

        _logger.LogInformation("Product created: {Id} - {Name}", result.Id, result.Name);

        return MapToInventoryProductDto(result, request.Category, request.IsTrackable, request.QuantityInStock);
    }

    public async Task<InventoryProductDto> UpdateAsync(
        UpdateInventoryProductRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating product: {Id}", request.Id);

        // Get current product for barcode (needed for update command)
        var currentProduct = _productCacheService.GetById(request.Id);
        var barcode = currentProduct?.Barcode ?? string.Empty;

        var command = new UpdateProductCommand
        {
            Id = request.Id,
            Barcode = barcode,
            Name = request.Description,
            Description = request.Description,
            Category = request.Category,
            Brand = request.Brand,
            Manufacturer = request.Manufacturer,
            UnitPrice = request.UnitPrice,
            GroupPrice = request.GroupPrice,
            GroupPriceQuantity = request.GroupPriceQuantity
        };

        var result = await _storeHubClient.UpdateProductAsync(command, cancellationToken);

        // Update cache
        _productCacheService.UpsertProduct(result);

        // Publish event for UI refresh
        _eventAggregator.GetEvent<InventoryProductUpdatedEvent>().Publish(result.Id);

        _logger.LogInformation("Product updated: {Id} - {Name}", result.Id, result.Name);

        var stock = await _storeHubClient.GetProductStockAsync(result.Id, cancellationToken);

        return MapToInventoryProductDto(result, request.Category, isTrackable: true, StockFor(stock, result.Id));
    }

    public async Task DeleteAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting product: {Id}", productId);

        await _storeHubClient.DeleteProductAsync(productId, cancellationToken);

        // Remove from cache
        _productCacheService.RemoveProduct(productId);

        // Publish event for UI refresh
        _eventAggregator.GetEvent<InventoryProductDeletedEvent>().Publish();

        _logger.LogInformation("Product deleted: {Id}", productId);
    }

    public async Task<InventoryProductDto> AdjustQuantityAsync(
        Guid productId,
        int delta,
        string reason,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adjusting stock for product: {Id}, Delta: {Delta}", productId, delta);

        var request = new AdjustQuantityRequest(delta, reason);
        var response = await _storeHubClient.AdjustProductQuantityAsync(productId, request, cancellationToken);

        // Nothing on the cached ProductDto changed - stock deliberately does not live
        // there - so there is no cache entry to refresh, only a UI refresh to trigger.
        _eventAggregator.GetEvent<InventoryProductUpdatedEvent>().Publish(productId);

        _logger.LogInformation("Product stock adjusted: {Id}, Delta: {Delta}, Balance: {Balance}",
            productId, delta, response.Quantity);

        var product = _productCacheService.GetById(productId)
                      ?? throw new KeyNotFoundException($"Product not found in cache: {productId}");

        return MapToInventoryProductDto(product, product.Category, isTrackable: true, response.Quantity);
    }

    public async Task<string> GenerateBarcodeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating next barcode");

        var barcode = await _storeHubClient.GenerateBarcodeAsync(cancellationToken);

        _logger.LogInformation("Barcode generated: {Barcode}", barcode);

        return barcode;
    }

    public async Task<InventoryProductDto> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        var product = _productCacheService.GetByBarcode(barcode);

        if (product is null)
        {
            throw new KeyNotFoundException($"Product not found with barcode: {barcode}");
        }

        var stock = await _storeHubClient.GetProductStockAsync(product.Id, cancellationToken);

        return MapToInventoryProductDto(product, product.Category, isTrackable: true, StockFor(stock, product.Id));
    }

    public async Task<IReadOnlyList<InventoryProductDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var stock = await _storeHubClient.GetProductStockAsync(cancellationToken: cancellationToken);

        return _productCacheService.GetAll()
            .Select(p => MapToInventoryProductDto(p, p.Category, isTrackable: true, StockFor(stock, p.Id)))
            .ToList();
    }

    public async Task<IReadOnlyList<InventoryProductDto>> GetByCategoryAsync(
        string categoryCode,
        CancellationToken cancellationToken = default)
    {
        var stock = await _storeHubClient.GetProductStockAsync(cancellationToken: cancellationToken);

        return _productCacheService.GetAll()
            // Ordinal to match ProductCategoryRepository.GetByCodeAsync; codes come from constants.
            .Where(p => string.Equals(p.Category, categoryCode, StringComparison.Ordinal))
            .Select(p => MapToInventoryProductDto(p, categoryCode, isTrackable: true, StockFor(stock, p.Id)))
            .ToList();
    }

    public async Task<IReadOnlyList<InventoryProductDto>> SearchByDescriptionAsync(
        string keyword,
        CancellationToken cancellationToken = default)
    {
        var stock = await _storeHubClient.GetProductStockAsync(cancellationToken: cancellationToken);

        return _productCacheService.Search(keyword)
            .Select(p => MapToInventoryProductDto(p, p.Category, isTrackable: true, StockFor(stock, p.Id)))
            .ToList();
    }

    public async Task<IReadOnlyList<InventoryProductDto>> SearchByBrandAsync(
        string keyword,
        CancellationToken cancellationToken = default)
    {
        var stock = await _storeHubClient.GetProductStockAsync(cancellationToken: cancellationToken);

        return _productCacheService.GetAll()
            .Where(p => p.Brand?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true)
            .Select(p => MapToInventoryProductDto(p, p.Category, isTrackable: true, StockFor(stock, p.Id)))
            .ToList();
    }

    #region Private Helpers

    private InventoryProductDto MapToInventoryProductDto(
        ProductDto product,
        string? categoryCode,
        bool isTrackable,
        int quantityInStock)
    {
        return new InventoryProductDto
        {
            Id = product.Id,
            Barcode = product.Barcode,
            Description = product.Name,
            Manufacturer = product.Manufacturer ?? string.Empty,
            Brand = product.Brand ?? string.Empty,
            Category = categoryCode ?? string.Empty,
            UnitPrice = product.UnitPrice,
            QuantityInStock = quantityInStock,
            GroupPrice = product.GroupPrice ?? 0m,
            GroupPriceQuantity = product.GroupPriceQuantity,
            IsTrackable = isTrackable,
            DateCreated = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            DateUpdated = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    /// <summary>
    /// A product absent from the balances has no movements, which is genuinely zero.
    /// </summary>
    private static int StockFor(IReadOnlyDictionary<Guid, int> stock, Guid productId)
        => stock.TryGetValue(productId, out var quantity) ? quantity : 0;

    #endregion
}
