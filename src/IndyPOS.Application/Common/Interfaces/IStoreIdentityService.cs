namespace IndyPOS.Application.Common.Interfaces;

/// <summary>
/// Provides identity information for the current store.
/// Used for multi-store operations and cloud sync.
/// </summary>
public interface IStoreIdentityService
{
    /// <summary>
    /// Gets the unique identifier for this store (e.g., "STORE-001").
    /// Used as a prefix/namespace for cloud sync and cross-store identification.
    /// </summary>
    string StoreId { get; }

    /// <summary>
    /// Gets the display name of the store (e.g., "Bangkok Branch 1").
    /// </summary>
    string StoreName { get; }

    /// <summary>
    /// Validates that the store is properly configured.
    /// Throws if StoreId is missing or invalid.
    /// </summary>
    void EnsureConfigured();
}
