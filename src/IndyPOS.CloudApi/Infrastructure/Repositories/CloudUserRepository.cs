using IndyPOS.Application.Abstractions.Cloud.Repositories;
using IndyPOS.CloudApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.CloudApi.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of ICloudUserRepository.
/// Maps between CloudUserEntity (Application layer) and CloudUser (Domain layer).
/// </summary>
public class CloudUserRepository(CloudDbContext dbContext) : ICloudUserRepository
{
    public async Task<CloudUserEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.FindAsync([id], cancellationToken);
        return user is null ? null : ToEntity(user);
    }

    public async Task<CloudUserEntity?> GetByUsernameAsync(
        string storeId,
        string username,
        CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.StoreId == storeId && u.Username == username, cancellationToken);
        return user is null ? null : ToEntity(user);
    }

    public async Task<IReadOnlyList<CloudUserEntity>> GetByStoreIdAsync(
        string storeId,
        bool? activeOnly = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Users.Where(u => u.StoreId == storeId);

        if (activeOnly == true)
        {
            query = query.Where(u => u.IsActive);
        }

        var users = await query.OrderBy(u => u.Username).ToListAsync(cancellationToken);
        return users.Select(ToEntity).ToList();
    }

    public async Task<IReadOnlyList<CloudUserEntity>> GetAllAsync(
        string? storeId = null,
        bool? activeOnly = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Users.AsQueryable();

        if (!string.IsNullOrEmpty(storeId))
        {
            query = query.Where(u => u.StoreId == storeId);
        }

        if (activeOnly == true)
        {
            query = query.Where(u => u.IsActive);
        }

        var users = await query
            .OrderBy(u => u.StoreId)
            .ThenBy(u => u.Username)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return users.Select(ToEntity).ToList();
    }

    public async Task<int> CountAsync(
        string? storeId = null,
        bool? activeOnly = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Users.AsQueryable();

        if (!string.IsNullOrEmpty(storeId))
        {
            query = query.Where(u => u.StoreId == storeId);
        }

        if (activeOnly == true)
        {
            query = query.Where(u => u.IsActive);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task AddAsync(CloudUserEntity entity, CancellationToken cancellationToken = default)
    {
        var user = ToDomain(entity);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(CloudUserEntity entity, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.FindAsync([entity.Id], cancellationToken);
        if (user is null)
        {
            throw new InvalidOperationException($"User with ID '{entity.Id}' not found.");
        }

        // Update fields
        user.Username = entity.Username;
        user.FirstName = entity.FirstName;
        user.LastName = entity.LastName;
        user.RoleId = entity.RoleId;
        user.IsActive = entity.IsActive;
        user.Version = entity.Version;
        user.LastModifiedAtUtc = entity.LastModifiedAtUtc;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<long> GetNextVersionAsync(string storeId, CancellationToken cancellationToken = default)
    {
        var maxVersion = await dbContext.Users
            .Where(u => u.StoreId == storeId)
            .MaxAsync(u => (long?)u.Version, cancellationToken) ?? 0;

        return maxVersion + 1;
    }

    public async Task<bool> StoreExistsAsync(string storeId, CancellationToken cancellationToken = default)
    {
        return await dbContext.StoreConfigs.AnyAsync(s => s.StoreId == storeId, cancellationToken);
    }

    // Mapping methods
    private static CloudUserEntity ToEntity(CloudUser user) => new()
    {
        Id = user.Id,
        StoreId = user.StoreId,
        Username = user.Username,
        FirstName = user.FirstName,
        LastName = user.LastName,
        RoleId = user.RoleId,
        IsActive = user.IsActive,
        Version = user.Version,
        CreatedAtUtc = user.CreatedAtUtc,
        LastModifiedAtUtc = user.LastModifiedAtUtc
    };

    private static CloudUser ToDomain(CloudUserEntity entity) => new()
    {
        Id = entity.Id,
        StoreId = entity.StoreId,
        Username = entity.Username,
        FirstName = entity.FirstName,
        LastName = entity.LastName,
        RoleId = entity.RoleId,
        IsActive = entity.IsActive,
        Version = entity.Version,
        CreatedAtUtc = entity.CreatedAtUtc,
        LastModifiedAtUtc = entity.LastModifiedAtUtc
    };
}
