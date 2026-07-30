using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Products.Update;

/// <summary>
/// Handler for updating an existing product in StoreHub.
/// </summary>
public class UpdateProductCommandHandler : ICommandHandler<UpdateProductCommand, ProductDto>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductCategoryRepository _categoryRepository;
    private readonly IStoreIdentityService _storeIdentityService;
    private readonly ILogger<UpdateProductCommandHandler> _logger;

    public UpdateProductCommandHandler(
        IProductRepository productRepository,
        IProductCategoryRepository categoryRepository,
        IStoreIdentityService storeIdentityService,
        ILogger<UpdateProductCommandHandler> logger)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _storeIdentityService = storeIdentityService;
        _logger = logger;
    }

    public async Task<ProductDto> HandleAsync(
        UpdateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        // Validate product exists
        var existingProduct = await _productRepository.GetByIdAsync(command.Id, cancellationToken);
        if (existingProduct is null)
        {
            throw new ProductNotFoundException($"Product with ID {command.Id} not found");
        }

        // Validate barcode uniqueness (excluding this product)
        var barcodeExists = await _productRepository.ExistsByBarcodeAsync(
            command.Barcode,
            excludeId: command.Id,
            cancellationToken: cancellationToken);
        if (barcodeExists)
        {
            throw new InvalidOperationException($"Product with barcode '{command.Barcode}' already exists");
        }

        // Store-type gating driven by the category's Kind. The category must exist: an unknown
        // code would file the product under something no report or picker can resolve.
        var category = await _categoryRepository.GetByCodeAsync(command.Category, cancellationToken)
            ?? throw new UnknownProductCategoryException(
                $"Product category '{command.Category}' is not in this store's catalogue.");

        if (!ProductCategoryPolicy.IsUsable(category.Kind, _storeIdentityService.Features))
        {
            _logger.LogWarning(
                "Product update rejected: Kind={Kind}, StoreType={StoreType}, ProductId={ProductId}",
                category.Kind, _storeIdentityService.StoreType, command.Id);
            throw new InvalidOperationException(
                $"{category.DisplayName} products are not available for {_storeIdentityService.StoreType} stores.");
        }

        // Update product
        var updatedProduct = new Product
        {
            Id = command.Id,
            Barcode = command.Barcode,
            Name = command.Name,
            Description = command.Description,
            Category = command.Category,
            Brand = command.Brand,
            Manufacturer = command.Manufacturer,
            UnitPrice = command.UnitPrice,
            GroupPrice = command.GroupPrice,
            GroupPriceQuantity = command.GroupPriceQuantity,
            IsActive = existingProduct.IsActive,
            CreatedUtc = existingProduct.CreatedUtc,
            LastModifiedUtc = DateTime.UtcNow
        };

        await _productRepository.UpdateAsync(updatedProduct, cancellationToken);

        _logger.LogInformation("Updated product {ProductId}", command.Id);

        return updatedProduct.ToDto();
    }
}
