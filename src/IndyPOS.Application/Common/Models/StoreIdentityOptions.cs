using IndyPOS.Domain.Enums;

namespace IndyPOS.Application.Common.Models;

/// <summary>
/// Configuration options for store identity.
/// Bound from appsettings.json "Store" section.
/// </summary>
public class StoreIdentityOptions
{
    public const string SectionName = "Store";

    /// <summary>
    /// Unique identifier for this store (UUID format, e.g., "550e8400-e29b-41d4-a716-446655440000").
    /// Provided by System Admin at installation. Required for cloud sync operations.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Display name for the store (e.g., "Bangkok Branch 1").
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Type of store. Determines available features. Immutable after installation.
    /// </summary>
    public StoreType Type { get; set; } = StoreType.GeneralHardware;

    /// <summary>
    /// Path to the store configuration JSON file.
    /// </summary>
    public string? ConfigPath { get; set; }

    /// <summary>
    /// Numeric code for barcode prefix (e.g., 1, 2, 3).
    /// Used in barcode generation: {Code}{Sequence:D8}
    /// </summary>
    [Obsolete("Use Id (UUID) for store identification. Code is kept for barcode generation only.")]
    public int Code { get; set; } = 1;
}
