namespace IndyPOS.Infrastructure.Services.StoreHub;

/// <summary>
/// Configuration options for StoreHub integration.
/// </summary>
public class StoreHubOptions
{
    public const string SectionName = "StoreHub";

    /// <summary>
    /// Enable StoreHub mode (use StoreHub API instead of direct SQLite).
    /// When false, legacy SQLite-based services are used.
    /// </summary>
    public bool Enabled { get; set; } = false;

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
