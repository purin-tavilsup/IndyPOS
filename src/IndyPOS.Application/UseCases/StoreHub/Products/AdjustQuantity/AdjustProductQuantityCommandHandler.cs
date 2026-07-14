using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities.Core;
using Microsoft.Extensions.Logging;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Products.AdjustQuantity;

/// <summary>
/// Handler for adjusting product quantity via inventory movement.
/// Creates an Adjustment movement with calculated delta.
/// </summary>
public class AdjustProductQuantityCommandHandler : ICommandHandler<AdjustProductQuantityCommand, int>
{
    private readonly IProductRepository _productRepository;
    private readonly IInventoryMovementRepository _movementRepository;
    private readonly IStoreIdentityService _storeIdentityService;
    private readonly ILogger<AdjustProductQuantityCommandHandler> _logger;

    public AdjustProductQuantityCommandHandler(
        IProductRepository productRepository,
        IInventoryMovementRepository movementRepository,
        IStoreIdentityService storeIdentityService,
        ILogger<AdjustProductQuantityCommandHandler> logger)
    {
        _productRepository = productRepository;
        _movementRepository = movementRepository;
        _storeIdentityService = storeIdentityService;
        _logger = logger;
    }

    public async Task<int> HandleAsync(
        AdjustProductQuantityCommand command,
        CancellationToken cancellationToken = default)
    {
        // Validate product exists
        var product = await _productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            throw new InvalidOperationException($"Product with ID {command.ProductId} not found");
        }

        var storeId = _storeIdentityService.StoreId;

        // Get current balance
        var currentBalance = await _movementRepository.GetCurrentBalanceAsync(
            storeId, command.ProductId, cancellationToken);

        // Calculate delta
        var delta = command.TargetQuantity - currentBalance;

        if (delta == 0)
        {
            _logger.LogDebug(
                "No adjustment needed for product {ProductId}: already at {Quantity}",
                command.ProductId, command.TargetQuantity);
            return command.TargetQuantity;
        }

        // Create adjustment movement
        var movement = new InventoryMovement
        {
            Id = Guid.NewGuid(),
            StoreId = storeId,
            ProductId = command.ProductId,
            QuantityDelta = delta,
            Reason = "Adjustment",
            Note = command.Reason ?? "Manual adjustment",
            CreatedUtc = DateTime.UtcNow
        };

        await _movementRepository.AddAsync(movement, cancellationToken);

        _logger.LogInformation(
            "Adjusted product {ProductId} quantity by {Delta} (from {From} to {To})",
            command.ProductId, delta, currentBalance, command.TargetQuantity);

        return command.TargetQuantity;
    }
}
