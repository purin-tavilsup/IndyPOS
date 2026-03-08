using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly StoreHubDbContext _dbContext;

    public ProductRepository(StoreHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetByCategoryAsync(
        string category,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Products.AsNoTracking();

        if (activeOnly)
        {
            query = query.Where(p => p.IsActive);
        }

        return await query
            .Where(p => p.Category == category)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> SearchAsync(
        string searchTerm,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Products.AsNoTracking();

        if (activeOnly)
        {
            query = query.Where(p => p.IsActive);
        }

        var lowerSearchTerm = searchTerm.ToLowerInvariant();

        return await query
            .Where(p => p.Barcode.ToLower().Contains(lowerSearchTerm) ||
                        p.Name.ToLower().Contains(lowerSearchTerm))
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Barcode == barcode, cancellationToken);
    }
}
