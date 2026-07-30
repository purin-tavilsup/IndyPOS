using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities.Core;
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
    private readonly IStoreIdentityService _storeIdentityService;
    private readonly ILogger<CreateProductCommandHandler> _logger;

    public CreateProductCommandHandler(
        IProductRepository productRepository,
        IInventoryMovementRepository movementRepository,
        IStoreIdentityService storeIdentityService,
        ILogger<CreateProductCommandHandler> logger)
    {
        _productRepository = productRepository;
        _movementRepository = movementRepository;
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

        // Store-type gating: a general-only store (e.g. Minimart) may not carry Hardware products.
        var isHardware = string.Equals(command.Category, nameof(Common.Enums.ProductCategory.Hardware), StringComparison.OrdinalIgnoreCase);
        if (isHardware && !_storeIdentityService.Features.MultipleProductTypesEnabled)
        {
            _logger.LogWarning("Hardware product creation rejected: StoreType={StoreType}, Barcode={Barcode}",
                _storeIdentityService.StoreType, command.Barcode);
            throw new InvalidOperationException(
                $"Hardware products are not available for {_storeIdentityService.StoreType} stores.");
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
