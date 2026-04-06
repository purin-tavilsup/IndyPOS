namespace IndyPOS.Domain.Enums;

/// <summary>
/// Type of store, determines available features.
/// Immutable after installation.
/// </summary>
public enum StoreType
{
    /// <summary>
    /// Full features: PayLater, multiple product types, all payment methods.
    /// </summary>
    GeneralHardware = 1,

    /// <summary>
    /// Limited features: No PayLater, general products only.
    /// </summary>
    Minimart = 2,

    /// <summary>
    /// Limited features: No PayLater, general products only.
    /// </summary>
    CoffeeShop = 3
}
