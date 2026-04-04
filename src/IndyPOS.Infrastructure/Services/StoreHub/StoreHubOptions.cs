namespace IndyPOS.Infrastructure.Services.StoreHub;

/// <summary>
/// Configuration options for StoreHub client (WinForms → StoreHub API).
/// </summary>
public class StoreHubOptions
{
    public const string SectionName = "StoreHub";

    /// <summary>
    /// Base URL of the StoreHub API (e.g., "http://localhost:5000").
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:5000";

    /// <summary>
    /// Timeout for HTTP requests to StoreHub in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Auto-sync products on startup.
    /// </summary>
    public bool AutoSyncProductsOnStartup { get; set; } = true;
}
