using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Application.Abstractions.StoreHub.Repositories;

/// <summary>
/// Repository for inventory movement tracking.
/// Movements track all stock changes with audit trail.
/// </summary>
public interface IInventoryMovementRepository
{
    /// <summary>
    /// Adds a new inventory movement.
    /// </summary>
    Task AddAsync(InventoryMovement movement, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current stock balance for a product in a store.
    /// Returns SUM(QuantityDelta) for all movements.
    /// </summary>
    Task<int> GetCurrentBalanceAsync(string storeId, Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current stock for every product in a store that has movements, as
    /// SUM(QuantityDelta) grouped by product. One query, not one per product.
    /// A product with no movements is absent from the dictionary — read it as zero.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, int>> GetBalancesAsync(
        string storeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets movement history for a product, ordered by most recent first.
    /// </summary>
    Task<IReadOnlyList<InventoryMovement>> GetByProductIdAsync(
        string storeId,
        Guid productId,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all movements for a store within a date range.
    /// </summary>
    Task<IReadOnlyList<InventoryMovement>> GetByDateRangeAsync(
        string storeId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);
}
