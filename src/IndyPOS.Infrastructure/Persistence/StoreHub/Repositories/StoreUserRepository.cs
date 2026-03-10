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
}
