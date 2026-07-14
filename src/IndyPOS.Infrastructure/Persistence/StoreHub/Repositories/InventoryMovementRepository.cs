using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;

/// <summary>
/// EF Core implementation of inventory movement repository.
/// </summary>
public class InventoryMovementRepository : IInventoryMovementRepository
{
    private readonly StoreHubDbContext _dbContext;

    public InventoryMovementRepository(StoreHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(InventoryMovement movement, CancellationToken cancellationToken = default)
    {
        _dbContext.InventoryMovements.Add(movement);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetCurrentBalanceAsync(
        string storeId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.InventoryMovements
            .Where(m => m.StoreId == storeId && m.ProductId == productId)
            .SumAsync(m => m.QuantityDelta, cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryMovement>> GetByProductIdAsync(
        string storeId,
        Guid productId,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.InventoryMovements
            .AsNoTracking()
            .Where(m => m.StoreId == storeId && m.ProductId == productId)
            .OrderByDescending(m => m.CreatedUtc);

        if (limit.HasValue)
        {
            query = (IOrderedQueryable<InventoryMovement>)query.Take(limit.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryMovement>> GetByDateRangeAsync(
        string storeId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.InventoryMovements
            .AsNoTracking()
            .Where(m => m.StoreId == storeId
                     && m.CreatedUtc >= fromUtc
                     && m.CreatedUtc <= toUtc)
            .OrderByDescending(m => m.CreatedUtc)
            .ToListAsync(cancellationToken);
    }
}
