using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Application.Abstractions.StoreHub.Repositories;

/// <summary>
/// Repository interface for StoreUser operations.
/// </summary>
public interface IStoreUserRepository
{
    /// <summary>
    /// Gets a user by username.
    /// </summary>
    Task<StoreUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    Task<StoreUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by legacy SQLite UserId (for migration).
    /// </summary>
    Task<StoreUser?> GetByLegacyIdAsync(int legacyUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users for a store.
    /// </summary>
    Task<IReadOnlyList<StoreUser>> GetAllAsync(string storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active users for a store.
    /// </summary>
    Task<IReadOnlyList<StoreUser>> GetActiveAsync(string storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new user.
    /// </summary>
    Task AddAsync(StoreUser user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the password hash and version after BCrypt migration.
    /// </summary>
    Task UpdatePasswordHashAsync(Guid id, string newHash, int version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the last login timestamp.
    /// </summary>
    Task UpdateLastLoginAsync(Guid id, DateTime loginTimeUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by CloudUserId (for sync).
    /// </summary>
    Task<StoreUser?> GetByCloudIdAsync(Guid cloudUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts a user by CloudUserId. Insert if new, update if exists.
    /// </summary>
    Task UpsertByCloudIdAsync(StoreUser user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the maximum CloudVersion for incremental sync.
    /// </summary>
    Task<long> GetMaxCloudVersionAsync(string storeId, CancellationToken cancellationToken = default);
}
