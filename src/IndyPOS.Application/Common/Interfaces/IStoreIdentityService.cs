using IndyPOS.Domain.Enums;
using IndyPOS.Domain.ValueObjects;

namespace IndyPOS.Application.Common.Interfaces;

/// <summary>
/// Provides identity information for the current store.
/// Used for multi-store operations and cloud sync.
/// </summary>
public interface IStoreIdentityService
{
    /// <summary>
    /// Gets the unique identifier for this store (UUID format).
    /// Used as a prefix/namespace for cloud sync and cross-store identification.
    /// </summary>
    string StoreId { get; }

    /// <summary>
    /// Gets the display name of the store (e.g., "Bangkok Branch 1").
    /// </summary>
    string StoreName { get; }

    /// <summary>
    /// Gets the type of store.
    /// </summary>
    StoreType StoreType { get; }

    /// <summary>
    /// Gets the feature flags for this store based on its type.
    /// </summary>
    StoreTypeFeatures Features { get; }

    /// <summary>
    /// Gets the numeric store code for barcode generation (e.g., 1, 2, 3).
    /// </summary>
    [Obsolete("Use StoreId (UUID) for identification. StoreCode is kept for barcode generation only.")]
    int StoreCode { get; }

    /// <summary>
    /// Gets the timezone for this store (e.g., "SE Asia Standard Time" for Thailand).
    /// Used for report date range calculations.
    /// </summary>
    TimeZoneInfo TimeZone { get; }

    /// <summary>
    /// Validates that the store is properly configured.
    /// Throws if StoreId is missing or invalid.
    /// </summary>
    void EnsureConfigured();
}
