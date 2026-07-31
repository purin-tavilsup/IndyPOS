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

    // 3 was CoffeeShop, removed 2026-07-29 - its products and services differ enough to
    // deserve a dedicated app. Do NOT reuse the value: any store row still carrying 3 must
    // fail to bind rather than silently become a different type.

    /// <summary>
    /// Gift/lifestyle shop. Same feature flags as <see cref="Minimart"/> today; it exists as its
    /// own type because its product categories differ, and services plus reporting are expected
    /// to diverge. Payment methods are expected to CONVERGE with Minimart, so do not add a
    /// payment flag here.
    /// </summary>
    MimyShop = 4
}
