using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;

/// <summary>
/// EF Core implementation of store setting repository.
/// </summary>
public class StoreSettingRepository : IStoreSettingRepository
{
    private readonly StoreHubDbContext _dbContext;

    public StoreSettingRepository(StoreHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        var setting = await _dbContext.StoreSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

        return setting?.Value;
    }

    public async Task SetValueAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var setting = await _dbContext.StoreSettings
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

        if (setting is null)
        {
            setting = StoreSetting.Create(key, value);
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
        var setting = await _dbContext.StoreSettings
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

        int newValue;

        if (setting is null)
        {
            newValue = 1;
            setting = StoreSetting.Create(key, newValue.ToString());
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
