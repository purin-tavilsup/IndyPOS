namespace IndyPOS.Application.Abstractions.Cloud.Repositories;

/// <summary>
/// Repository for managing cloud users (master user data).
/// Implemented by CloudApi, consumed by admin user management commands.
/// </summary>
public interface ICloudUserRepository
{
    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    Task<CloudUserEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by username within a specific store.
    /// </summary>
    Task<CloudUserEntity?> GetByUsernameAsync(
        string storeId,
        string username,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users for a specific store.
    /// </summary>
    Task<IReadOnlyList<CloudUserEntity>> GetByStoreIdAsync(
        string storeId,
        bool? activeOnly = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users with optional filtering and pagination.
    /// </summary>
    Task<IReadOnlyList<CloudUserEntity>> GetAllAsync(
        string? storeId = null,
        bool? activeOnly = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of users matching the filter.
    /// </summary>
    Task<int> CountAsync(
        string? storeId = null,
        bool? activeOnly = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new user.
    /// </summary>
    Task AddAsync(CloudUserEntity user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    Task UpdateAsync(CloudUserEntity user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the next version number for a store (for sync safety).
    /// </summary>
    Task<long> GetNextVersionAsync(string storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a store exists (for validation).
    /// </summary>
    Task<bool> StoreExistsAsync(string storeId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Entity representing a cloud user (master user data).
/// Synced to StoreHub via GET /master/users/{storeId}.
/// Passwords are NOT stored here - managed locally at each store.
/// </summary>
public class CloudUserEntity
{
    public Guid Id { get; set; }
    public string StoreId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public bool IsActive { get; set; } = true;
    public long Version { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastModifiedAtUtc { get; set; }
}
