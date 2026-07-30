using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;

public class ProductCategoryRepository : IProductCategoryRepository
{
    private readonly StoreHubDbContext _dbContext;
    private readonly IStoreIdentityService _storeIdentity;

    public ProductCategoryRepository(StoreHubDbContext dbContext, IStoreIdentityService storeIdentity)
    {
        _dbContext = dbContext;
        _storeIdentity = storeIdentity;
    }

    public async Task<IReadOnlyList<ProductCategory>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var storeId = _storeIdentity.StoreId;
        return await _dbContext.ProductCategories.AsNoTracking()
            .Where(c => c.StoreId == storeId)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Exact, case-SENSITIVE match on Code. Codes are generated from
    /// <c>ProductCategoryCodes</c> constants, never typed by a user, so exact is the contract —
    /// and it must stay consistent with every other place a code is compared. Do not "fix" this
    /// to case-insensitive: a store that accepts <c>plumbingmaterials</c> here would write a
    /// value the catalogue cannot resolve.
    /// </summary>
    public async Task<ProductCategory?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var storeId = _storeIdentity.StoreId;
        return await _dbContext.ProductCategories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.StoreId == storeId && c.Code == code, cancellationToken);
    }

    public async Task AddAsync(ProductCategory category, CancellationToken cancellationToken = default)
    {
        // Force the current store's identity — never trust the caller's StoreId.
        category.StoreId = _storeIdentity.StoreId;

        _dbContext.ProductCategories.Add(category);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
