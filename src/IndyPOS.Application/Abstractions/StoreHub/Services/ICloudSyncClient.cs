namespace IndyPOS.Application.Abstractions.StoreHub.Services;

using IndyPOS.Application.UseCases.StoreHub.Users;
using IndyPOS.Domain.Entities.Core;

public interface ICloudSyncClient
{
    /// <summary>Sends one outbox event. Never throws for a cloud or network failure; it reports one.</summary>
    Task<SyncSendResult> SendEventAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users from CloudApi for sync.
    /// Returns null if cloud is unreachable.
    /// </summary>
    Task<CloudUserSyncResponse?> GetUsersAsync(
        string storeId,
        long? sinceVersion = null,
        CancellationToken cancellationToken = default);
}
