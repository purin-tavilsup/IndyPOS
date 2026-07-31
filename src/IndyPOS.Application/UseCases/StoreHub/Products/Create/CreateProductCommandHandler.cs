using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Products.Create;

/// <summary>
/// Handler for creating a new product in StoreHub.
/// Creates initial inventory movement if InitialQuantity is provided.
/// </summary>
public class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, ProductDto>
{
    private readonly IProductRepository _productRepository;
    private readonly IInventoryMovementRepository _movementRepository;
    private readonly IProductCategoryRepository _categoryRepository;
    private readonly IStoreIdentityService _storeIdentityService;
    private readonly ILogger<CreateProductCommandHandler> _logger;

    public CreateProductCommandHandler(
        IProductRepository productRepository,
        IInventoryMovementRepository movementRepository,
        IProductCategoryRepository categoryRepository,
        IStoreIdentityService storeIdentityService,
        ILogger<CreateProductCommandHandler> logger)
    {
        _productRepository = productRepository;
        _movementRepository = movementRepository;
        _categoryRepository = categoryRepository;
        _storeIdentityService = storeIdentityService;
        _logger = logger;
    }

    public async Task<ProductDto> HandleAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        // Validate barcode uniqueness
        var barcodeExists = await _productRepository.ExistsByBarcodeAsync(command.Barcode, cancellationToken: cancellationToken);
        if (barcodeExists)
        {
            throw new InvalidOperationException($"Product with barcode '{command.Barcode}' already exists");
        }

        // Store-type gating driven by the category's Kind. The category must exist: an unknown
        // code would file the product under something no report or picker can resolve.
        var category = await _categoryRepository.GetByCodeAsync(command.Category, cancellationToken);

        // A disabled category is treated exactly as an unknown one (spec section 7). The picker
        // also hides it, but the server is the boundary — the whole point of this epic was to
        // stop trusting the client's idea of what a valid category is.
        if (category is null || !category.IsEnabled)
        {
            throw new UnknownProductCategoryException(
                $"Product category '{command.Category}' is not available in this store's catalogue.");
        }

        if (!ProductCategoryPolicy.IsUsable(category.Kind, _storeIdentityService.Features))
        {
            _logger.LogWarning(
                "Product creation rejected: Kind={Kind}, StoreType={StoreType}, Barcode={Barcode}",
                category.Kind, _storeIdentityService.StoreType, command.Barcode);
            throw new InvalidOperationException(
                $"{category.DisplayName} products are not available for {_storeIdentityService.StoreType} stores.");
        }

        // Create product
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Barcode = command.Barcode,
            Name = command.Name,
            Description = command.Description,
            Category = command.Category,
            Brand = command.Brand,
            Manufacturer = command.Manufacturer,
            UnitPrice = command.UnitPrice,
            GroupPrice = command.GroupPrice,
            GroupPriceQuantity = command.GroupPriceQuantity,
            IsActive = true,
            CreatedUtc = DateTime.UtcNow,
            LastModifiedUtc = DateTime.UtcNow
        };

        await _productRepository.AddAsync(product, cancellationToken);

        _logger.LogInformation("Created product {ProductId} with barcode {Barcode}", product.Id, product.Barcode);

        // Create initial inventory movement if quantity provided
        if (command.InitialQuantity.HasValue && command.InitialQuantity.Value > 0)
        {
            var movement = new InventoryMovement
            {
                Id = Guid.NewGuid(),
                StoreId = _storeIdentityService.StoreId,
                ProductId = product.Id,
                QuantityDelta = command.InitialQuantity.Value,
                Reason = "InitialStock",
                Note = "Initial stock on product creation",
                CreatedUtc = DateTime.UtcNow
            };

            await _movementRepository.AddAsync(movement, cancellationToken);

            _logger.LogInformation(
                "Created initial stock movement for product {ProductId}: {Quantity} units",
                product.Id, command.InitialQuantity.Value);
        }

        return product.ToDto();
    }
}
