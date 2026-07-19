using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;

public class PaymentMethodRepository : IPaymentMethodRepository
{
    private readonly StoreHubDbContext _dbContext;
    private readonly IStoreIdentityService _storeIdentity;

    public PaymentMethodRepository(StoreHubDbContext dbContext, IStoreIdentityService storeIdentity)
    {
        _dbContext = dbContext;
        _storeIdentity = storeIdentity;
    }

    public async Task<IReadOnlyList<PaymentMethod>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var storeId = _storeIdentity.StoreId;
        return await _dbContext.PaymentMethods.AsNoTracking()
            .Where(m => m.StoreId == storeId)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<PaymentMethod?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var storeId = _storeIdentity.StoreId;
        return await _dbContext.PaymentMethods
            .FirstOrDefaultAsync(m => m.StoreId == storeId && m.Code == code, cancellationToken);
    }

    public async Task AddAsync(PaymentMethod method, CancellationToken cancellationToken = default)
    {
        _dbContext.PaymentMethods.Add(method);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PaymentMethod method, CancellationToken cancellationToken = default)
    {
        _dbContext.PaymentMethods.Update(method);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
