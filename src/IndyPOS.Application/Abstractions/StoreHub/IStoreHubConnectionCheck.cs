namespace IndyPOS.Application.Abstractions.StoreHub;

/// <summary>Asks StoreHub whether it is ready, and says why not, for the first-run wizard.</summary>
public interface IStoreHubConnectionCheck
{
    Task<StoreHubConnectionResult> CheckAsync(CancellationToken cancellationToken = default);
}

public enum StoreHubConnectionStatus
{
    Healthy,
    NotReady,
    Unexpected,
    Unreachable,
    TimedOut
}

public sealed record StoreHubConnectionResult(StoreHubConnectionStatus Status, int? StatusCode = null);
