namespace IndyPOS.Infrastructure.Services.StoreHub;

/// <summary>
/// Exception thrown when StoreHub API communication fails.
/// </summary>
public class StoreHubClientException : Exception
{
    public StoreHubClientException(string message) : base(message) { }
    public StoreHubClientException(string message, Exception innerException) : base(message, innerException) { }
}
