namespace IndyPOS.Application.Abstractions.StoreHub.Services;

using IndyPOS.Application.UseCases.Cloud.Sync.Heartbeat;

public interface ICloudHeartbeatClient
{
    /// <summary>
    /// Sends one heartbeat. Returns the cloud's reply, or null when the beat was missed for any reason (no token,
    /// network, timeout, a failure status). Never throws for a cloud or network failure.
    /// </summary>
    Task<HeartbeatResponse?> SendAsync(HeartbeatRequest request, CancellationToken cancellationToken = default);
}
