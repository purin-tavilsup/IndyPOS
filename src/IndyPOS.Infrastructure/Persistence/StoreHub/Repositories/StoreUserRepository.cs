using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;

public class StoreUserRepository : IStoreUserRepository
{
    private readonly StoreHubDbContext _dbContext;

    public StoreUserRepository(StoreHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StoreUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StoreUsers
                               .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
    }

    public async Task<StoreUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StoreUsers
                               .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<StoreUser?> GetByLegacyIdAsync(int legacyUserId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StoreUsers
                               .FirstOrDefaultAsync(u => u.LegacyUserId == legacyUserId, cancellationToken);
    }

    public async Task<IReadOnlyList<StoreUser>> GetAllAsync(string storeId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StoreUsers
                               .AsNoTracking()
                               .Where(u => u.StoreId == storeId)
                               .OrderBy(u => u.FirstName)
                               .ThenBy(u => u.LastName)
                               .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StoreUser>> GetActiveAsync(string storeId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StoreUsers
                               .AsNoTracking()
                               .Where(u => u.StoreId == storeId && u.IsActive)
                               .OrderBy(u => u.FirstName)
                               .ThenBy(u => u.LastName)
                               .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(StoreUser user, CancellationToken cancellationToken = default)
    {
        _dbContext.StoreUsers.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdatePasswordHashAsync(Guid id, string newHash, int version, CancellationToken cancellationToken = default)
    {
        await _dbContext.StoreUsers
                        .Where(u => u.Id == id)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(u => u.PasswordHash, newHash)
                            .SetProperty(u => u.PasswordHashVersion, version)
                            .SetProperty(u => u.LastModifiedAtUtc, DateTime.UtcNow),
                            cancellationToken);
    }

    public async Task UpdateLastLoginAsync(Guid id, DateTime loginTimeUtc, CancellationToken cancellationToken = default)
    {
        await _dbContext.StoreUsers
                        .Where(u => u.Id == id)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(u => u.LastLoginAtUtc, loginTimeUtc),
                            cancellationToken);
    }

    public async Task<StoreUser?> GetByCloudIdAsync(Guid cloudUserId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StoreUsers
                               .FirstOrDefaultAsync(u => u.CloudUserId == cloudUserId, cancellationToken);
    }

    public async Task UpsertByCloudIdAsync(StoreUser user, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.StoreUsers
            .FirstOrDefaultAsync(u => u.CloudUserId == user.CloudUserId, cancellationToken);

        if (existing is null)
        {
            // Insert new user
            _dbContext.StoreUsers.Add(user);
        }
        else
        {
            // Update existing user (preserve password and local-only fields)
            existing.Username = user.Username;
            existing.FirstName = user.FirstName;
            existing.LastName = user.LastName;
            existing.RoleId = user.RoleId;
            existing.IsActive = user.IsActive;
            existing.CloudVersion = user.CloudVersion;
            existing.LastSyncedAtUtc = user.LastSyncedAtUtc;
            existing.LastModifiedAtUtc = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<long> GetMaxCloudVersionAsync(string storeId, CancellationToken cancellationToken = default)
    {
        var maxVersion = await _dbContext.StoreUsers
            .Where(u => u.StoreId == storeId && u.CloudUserId != null)
            .MaxAsync(u => (long?)u.CloudVersion, cancellationToken);

        return maxVersion ?? 0;
    }
}
