using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities.Core;
using Microsoft.Extensions.Logging;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Products.AdjustQuantity;

/// <summary>
/// Handler for adjusting product quantity via inventory movement.
/// Writes the caller's delta straight to an Adjustment movement; nothing is calculated
/// from a balance read.
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
        if (command.Delta == 0)
        {
            throw new ArgumentException("Delta must not be zero.", nameof(command));
        }

        var product = await _productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            throw new InvalidOperationException($"Product with ID {command.ProductId} not found");
        }

        var storeId = _storeIdentityService.StoreId;

        var movement = new InventoryMovement
        {
            Id = Guid.NewGuid(),
            StoreId = storeId,
            ProductId = command.ProductId,
            QuantityDelta = command.Delta,
            Reason = "Adjustment",
            Note = command.Reason ?? "Manual adjustment",
            CreatedUtc = DateTime.UtcNow
        };

        await _movementRepository.AddAsync(movement, cancellationToken);

        // Read back only to report the result. The balance no longer decides what gets
        // written, which is the whole point of taking a delta.
        var newBalance = await _movementRepository.GetCurrentBalanceAsync(
            storeId, command.ProductId, cancellationToken);

        _logger.LogInformation(
            "Adjusted product {ProductId} by {Delta} (new balance {Balance})",
            command.ProductId, command.Delta, newBalance);

        return newBalance;
    }
}
