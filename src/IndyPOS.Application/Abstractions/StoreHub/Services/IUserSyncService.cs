namespace IndyPOS.Application.Abstractions.StoreHub.Services;

/// <summary>
/// Service for syncing users from CloudApi to local StoreHub.
/// </summary>
public interface IUserSyncService
{
    /// <summary>
    /// Syncs users from CloudApi to local StoreHub.
    /// Uses incremental sync based on CloudVersion.
    /// </summary>
    /// <returns>Number of users synced.</returns>
    Task<int> SyncUsersFromCloudAsync(CancellationToken cancellationToken = default);
}
