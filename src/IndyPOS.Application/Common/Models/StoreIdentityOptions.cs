namespace IndyPOS.Application.Common.Models;

/// <summary>
/// Configuration options for store identity.
/// Bound from appsettings.json "Store" section.
/// </summary>
public class StoreIdentityOptions
{
    public const string SectionName = "Store";

    /// <summary>
    /// Unique identifier for this store (e.g., "STORE-001").
    /// Required for cloud sync operations.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Display name for the store (e.g., "Bangkok Branch 1").
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Path to the store configuration JSON file.
    /// </summary>
    public string? ConfigPath { get; set; }

    /// <summary>
    /// Numeric code for barcode prefix (e.g., 1, 2, 3).
    /// Used in barcode generation: {Code}{Sequence:D8}
    /// </summary>
    public int Code { get; set; } = 1;
}
