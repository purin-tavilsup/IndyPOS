using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly StoreHubDbContext _dbContext;
    private readonly IStoreIdentityService _storeIdentity;

    public ProductRepository(StoreHubDbContext dbContext, IStoreIdentityService storeIdentity)
    {
        _dbContext = dbContext;
        _storeIdentity = storeIdentity;
    }

    private IQueryable<Product> StoreProducts => _dbContext.Products
        .Where(p => p.StoreId == _storeIdentity.StoreId);

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await StoreProducts
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await StoreProducts
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
        var query = StoreProducts.AsNoTracking();

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
        var query = StoreProducts.AsNoTracking();

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
        return await StoreProducts
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        return await StoreProducts
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Barcode == barcode, cancellationToken);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        product.StoreId = _storeIdentity.StoreId;
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        var existing = await StoreProducts
            .FirstOrDefaultAsync(p => p.Id == product.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Product with ID {product.Id} not found");

        // Update all editable fields (StoreId is immutable)
        existing.Barcode = product.Barcode;
        existing.Name = product.Name;
        existing.Description = product.Description;
        existing.Manufacturer = product.Manufacturer;
        existing.Brand = product.Brand;
        existing.Category = product.Category;
        existing.UnitPrice = product.UnitPrice;
        existing.GroupPrice = product.GroupPrice;
        existing.GroupPriceQuantity = product.GroupPriceQuantity;
        existing.IsActive = product.IsActive;
        existing.LastModifiedUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await StoreProducts
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null)
        {
            return; // Already deleted or never existed
        }

        product.IsActive = false;
        product.LastModifiedUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsByBarcodeAsync(
        string barcode,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = StoreProducts
            .Where(p => p.Barcode == barcode);

        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
