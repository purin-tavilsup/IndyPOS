using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;

/// <summary>
/// EF Core implementation of store setting repository.
/// </summary>
public class StoreSettingRepository : IStoreSettingRepository
{
    private readonly StoreHubDbContext _dbContext;
    private readonly IStoreIdentityService _storeIdentity;

    public StoreSettingRepository(StoreHubDbContext dbContext, IStoreIdentityService storeIdentity)
    {
        _dbContext = dbContext;
        _storeIdentity = storeIdentity;
    }

    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        var storeId = _storeIdentity.StoreId;
        var setting = await _dbContext.StoreSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.StoreId == storeId && s.Key == key, cancellationToken);

        return setting?.Value;
    }

    public async Task SetValueAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var storeId = _storeIdentity.StoreId;
        var setting = await _dbContext.StoreSettings
            .FirstOrDefaultAsync(s => s.StoreId == storeId && s.Key == key, cancellationToken);

        if (setting is null)
        {
            setting = StoreSetting.Create(storeId, key, value);
            _dbContext.StoreSettings.Add(setting);
        }
        else
        {
            setting.UpdateValue(value);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> IncrementAsync(string key, CancellationToken cancellationToken = default)
    {
        var storeId = _storeIdentity.StoreId;
        var setting = await _dbContext.StoreSettings
            .FirstOrDefaultAsync(s => s.StoreId == storeId && s.Key == key, cancellationToken);

        int newValue;

        if (setting is null)
        {
            newValue = 1;
            setting = StoreSetting.Create(storeId, key, newValue.ToString());
            _dbContext.StoreSettings.Add(setting);
        }
        else
        {
            var currentValue = int.TryParse(setting.Value, out var parsed) ? parsed : 0;
            newValue = currentValue + 1;
            setting.UpdateValue(newValue.ToString());
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return newValue;
    }
}
