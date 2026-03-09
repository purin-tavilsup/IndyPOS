namespace IndyPOS.Infrastructure.Services.StoreHub;

/// <summary>
/// Configuration options for Cloud API OAuth2 authentication.
/// </summary>
public class CloudTokenOptions
{
    public const string SectionName = "CloudApi";

    /// <summary>
    /// Base URL of the Cloud API (e.g., "https://localhost:7180")
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// OAuth2 Client ID assigned during store registration
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// OAuth2 Client Secret assigned during store registration
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Scopes to request when acquiring tokens
    /// </summary>
    public string Scopes { get; set; } = "sync.write master.read";
}
