using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.InventoryProducts;
using IndyPOS.Application.UseCases.StoreHub.Products;
using IndyPOS.Application.UseCases.StoreHub.Products.AdjustQuantity;
using IndyPOS.Application.UseCases.StoreHub.Products.Create;
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

        return MapToInventoryProductDto(result, request.Category, isTrackable: true, request.QuantityInStock);
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
        int targetQuantity,
        string reason,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adjusting quantity for product: {Id}, Target: {Quantity}", productId, targetQuantity);

        var request = new AdjustQuantityRequest(targetQuantity, reason);
        var result = await _storeHubClient.AdjustProductQuantityAsync(productId, request, cancellationToken);

        // Update cache
        _productCacheService.UpsertProduct(result);

        // Publish event for UI refresh
        _eventAggregator.GetEvent<InventoryProductUpdatedEvent>().Publish(result.Id);

        _logger.LogInformation("Product quantity adjusted: {Id}, Target: {Quantity}", result.Id, targetQuantity);

        return MapToInventoryProductDto(result, result.Category, isTrackable: true, targetQuantity);
    }

    public async Task<string> GenerateBarcodeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating next barcode");

        var barcode = await _storeHubClient.GenerateBarcodeAsync(cancellationToken);

        _logger.LogInformation("Barcode generated: {Barcode}", barcode);

        return barcode;
    }

    public Task<InventoryProductDto> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        var product = _productCacheService.GetByBarcode(barcode);

        if (product is null)
        {
            throw new KeyNotFoundException($"Product not found with barcode: {barcode}");
        }

        return Task.FromResult(MapToInventoryProductDto(product, product.Category, isTrackable: true));
    }

    public Task<IReadOnlyList<InventoryProductDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = _productCacheService.GetAll()
            .Select(p => MapToInventoryProductDto(p, p.Category, isTrackable: true))
            .ToList();

        return Task.FromResult<IReadOnlyList<InventoryProductDto>>(result);
    }

    public Task<IReadOnlyList<InventoryProductDto>> GetByCategoryAsync(
        string categoryCode,
        CancellationToken cancellationToken = default)
    {
        var result = _productCacheService.GetAll()
            .Where(p => string.Equals(p.Category, categoryCode, StringComparison.OrdinalIgnoreCase))
            .Select(p => MapToInventoryProductDto(p, categoryCode, isTrackable: true))
            .ToList();

        return Task.FromResult<IReadOnlyList<InventoryProductDto>>(result);
    }

    public Task<IReadOnlyList<InventoryProductDto>> SearchByDescriptionAsync(
        string keyword,
        CancellationToken cancellationToken = default)
    {
        var products = _productCacheService.Search(keyword);

        var result = products
            .Select(p => MapToInventoryProductDto(p, p.Category, isTrackable: true))
            .ToList();

        return Task.FromResult<IReadOnlyList<InventoryProductDto>>(result);
    }

    public Task<IReadOnlyList<InventoryProductDto>> SearchByBrandAsync(
        string keyword,
        CancellationToken cancellationToken = default)
    {
        var products = _productCacheService.GetAll()
            .Where(p => p.Brand?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        var result = products
            .Select(p => MapToInventoryProductDto(p, p.Category, isTrackable: true))
            .ToList();

        return Task.FromResult<IReadOnlyList<InventoryProductDto>>(result);
    }

    #region Private Helpers

    private InventoryProductDto MapToInventoryProductDto(
        ProductDto product,
        string categoryCode,
        bool isTrackable,
        int? quantityOverride = null)
    {
        return new InventoryProductDto
        {
            Id = product.Id,
            Barcode = product.Barcode,
            Description = product.Name,
            Manufacturer = product.Manufacturer ?? string.Empty,
            Brand = product.Brand ?? string.Empty,
            Category = categoryCode,
            UnitPrice = product.UnitPrice,
            QuantityInStock = quantityOverride ?? 0, // TODO: Get from StoreHub when available
            GroupPrice = product.GroupPrice ?? 0m,
            GroupPriceQuantity = product.GroupPriceQuantity,
            IsTrackable = isTrackable,
            DateCreated = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            DateUpdated = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    #endregion
}
