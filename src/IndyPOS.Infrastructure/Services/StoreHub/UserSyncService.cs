using System.Diagnostics;
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Abstractions.StoreHub.Services;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.Users;
using IndyPOS.Domain.Entities.Core;
using Microsoft.Extensions.Logging;

namespace IndyPOS.Infrastructure.Services.StoreHub;

/// <summary>
/// Syncs users from CloudApi to local StoreHub.
/// Uses version-based incremental sync to avoid clock drift issues.
/// </summary>
public class UserSyncService : IUserSyncService
{
    private readonly ICloudSyncClient _cloudClient;
    private readonly IStoreUserRepository _userRepository;
    private readonly IStoreIdentityService _storeIdentity;
    private readonly ILogger<UserSyncService> _logger;

    public UserSyncService(
        ICloudSyncClient cloudClient,
        IStoreUserRepository userRepository,
        IStoreIdentityService storeIdentity,
        ILogger<UserSyncService> logger)
    {
        _cloudClient = cloudClient;
        _userRepository = userRepository;
        _storeIdentity = storeIdentity;
        _logger = logger;
    }

    public async Task<int> SyncUsersFromCloudAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var storeId = _storeIdentity.StoreId;

        // Get last synced version for incremental sync
        var lastVersion = await _userRepository.GetMaxCloudVersionAsync(storeId, cancellationToken);

        _logger.LogDebug("Starting user sync from CloudApi for store {StoreId}, sinceVersion={Version}",
            storeId, lastVersion);

        // Fetch users from cloud
        var response = await _cloudClient.GetUsersAsync(storeId, lastVersion, cancellationToken);

        if (response is null)
        {
            _logger.LogWarning("User sync failed - cloud unreachable (offline mode)");
            return 0;
        }

        if (response.Count == 0)
        {
            _logger.LogDebug("No user updates from cloud (already up to date)");
            return 0;
        }

        // Upsert each user
        var syncedCount = 0;
        foreach (var cloudUser in response.Users)
        {
            try
            {
                var storeUser = MapToStoreUser(cloudUser, storeId);
                await _userRepository.UpsertByCloudIdAsync(storeUser, cancellationToken);
                syncedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync user {Username} (CloudId={CloudId})",
                    cloudUser.Username, cloudUser.Id);
            }
        }

        stopwatch.Stop();
        _logger.LogInformation(
            "User sync completed: {SyncedCount} users in {Duration}ms (version {FromVersion} → {ToVersion})",
            syncedCount,
            stopwatch.ElapsedMilliseconds,
            lastVersion,
            response.MaxVersion);

        return syncedCount;
    }

    private static StoreUser MapToStoreUser(CloudUserDto cloudUser, string storeId)
    {
        return new StoreUser
        {
            Id = Guid.NewGuid(),
            StoreId = storeId,
            CloudUserId = cloudUser.Id,
            Username = cloudUser.Username,
            FirstName = cloudUser.FirstName,
            LastName = cloudUser.LastName,
            RoleId = cloudUser.RoleId,
            IsActive = cloudUser.IsActive,  // Explicit deactivation handling
            CloudVersion = cloudUser.Version,
            LastSyncedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            LastModifiedAtUtc = DateTime.UtcNow,
            // Password fields left empty - managed locally
            PasswordHash = string.Empty,
            PasswordHashVersion = 0,  // Indicates no password set
            LegacyUserId = 0
        };
    }
}
