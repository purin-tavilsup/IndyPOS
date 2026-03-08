namespace IndyPOS.Application.Abstractions.StoreHub.Services;

using IndyPOS.Domain.Entities.Core;

public interface ICloudSyncClient
{
    Task<bool> SendEventAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default);
}
