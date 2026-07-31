using IndyPOS.Domain.Enums;

namespace IndyPOS.Domain.ValueObjects;

/// <summary>
/// Feature flags for a store type.
/// Determines what functionality is available based on store type.
/// </summary>
public record StoreTypeFeatures
{
    /// <summary>
    /// Whether PayLater (deferred payment) is enabled.
    /// </summary>
    public bool PayLaterEnabled { get; init; }

    /// <summary>
    /// Whether multiple product types (Hardware, General, etc.) are enabled.
    /// When false, only General products are available.
    /// </summary>
    public bool MultipleProductTypesEnabled { get; init; }

    /// <summary>
    /// Gets the feature set for a given store type.
    /// </summary>
    public static StoreTypeFeatures For(StoreType storeType) => storeType switch
    {
        StoreType.GeneralHardware => new StoreTypeFeatures
        {
            PayLaterEnabled = true,
            MultipleProductTypesEnabled = true
        },
        StoreType.Minimart => new StoreTypeFeatures
        {
            PayLaterEnabled = false,
            MultipleProductTypesEnabled = false
        },
        StoreType.MimyShop => new StoreTypeFeatures
        {
            PayLaterEnabled = false,
            MultipleProductTypesEnabled = false
        },
        _ => throw new ArgumentOutOfRangeException(nameof(storeType), storeType, "Unknown store type")
    };
}
