namespace IndyPOS.Infrastructure.Services.StoreHub;

using IndyPOS.Application.Abstractions.StoreHub.Services;
using IndyPOS.Application.UseCases.StoreHub.Users;
using IndyPOS.Domain.Entities.Core;
using Microsoft.Extensions.Logging;

/// <summary>
/// Stub implementation that logs events instead of sending to cloud.
/// Used for development/testing without Cloud API.
/// </summary>
public class StubCloudSyncClient(ILogger<StubCloudSyncClient> logger) : ICloudSyncClient
{
    public Task<bool> SendEventAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "[STUB] Would send event {EventId} ({Type}) for store {StoreId} to cloud",
            outboxEvent.Id,
            outboxEvent.Type,
            outboxEvent.StoreId);

        // Simulate successful sync for now
        return Task.FromResult(true);
    }

    public Task<CloudUserSyncResponse?> GetUsersAsync(
        string storeId,
        long? sinceVersion = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "[STUB] Would fetch users for store {StoreId} (sinceVersion={Version})",
            storeId,
            sinceVersion);

        // Return empty response (no users to sync in stub mode)
        return Task.FromResult<CloudUserSyncResponse?>(
            new CloudUserSyncResponse(0, [], sinceVersion ?? 0, DateTime.UtcNow));
    }
}
