namespace IndyPOS.Application.Abstractions.StoreHub.Services;

using IndyPOS.Application.UseCases.StoreHub.Users;
using IndyPOS.Domain.Entities.Core;

public interface ICloudSyncClient
{
    Task<bool> SendEventAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users from CloudApi for sync.
    /// Returns null if cloud is unreachable.
    /// </summary>
    Task<CloudUserSyncResponse?> GetUsersAsync(
        string storeId,
        long? sinceVersion = null,
        CancellationToken cancellationToken = default);
}
