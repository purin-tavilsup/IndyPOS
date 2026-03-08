namespace IndyPOS.Infrastructure.Services.StoreHub;

using IndyPOS.Application.Abstractions.StoreHub.Services;
using IndyPOS.Domain.Entities.Core;
using Microsoft.Extensions.Logging;

/// <summary>
/// Stub implementation that logs events instead of sending to cloud.
/// Used until Cloud API (Epic F) is implemented.
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
}
