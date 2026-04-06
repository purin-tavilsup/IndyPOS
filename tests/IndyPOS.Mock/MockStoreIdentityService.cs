using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Enums;
using IndyPOS.Domain.ValueObjects;

namespace IndyPOS.Mock;

/// <summary>
/// Mock implementation of IStoreIdentityService for unit tests.
/// Default configuration is GeneralHardware with all features enabled.
/// </summary>
public class MockStoreIdentityService : IStoreIdentityService
{
    public string StoreId { get; set; } = "test-store";
    public string StoreName { get; set; } = "Test Store";
    public StoreType StoreType { get; set; } = StoreType.GeneralHardware;
    public StoreTypeFeatures Features => StoreTypeFeatures.For(StoreType);

    [Obsolete("Use StoreId (UUID) for identification.")]
    public int StoreCode { get; set; } = 1;

    public void EnsureConfigured() { }

    /// <summary>
    /// Creates a mock configured for GeneralHardware store (all features enabled).
    /// </summary>
    public static MockStoreIdentityService GeneralHardware() => new()
    {
        StoreType = StoreType.GeneralHardware
    };

    /// <summary>
    /// Creates a mock configured for Minimart store (PayLater disabled).
    /// </summary>
    public static MockStoreIdentityService Minimart() => new()
    {
        StoreType = StoreType.Minimart
    };

    /// <summary>
    /// Creates a mock configured for CoffeeShop store (PayLater disabled).
    /// </summary>
    public static MockStoreIdentityService CoffeeShop() => new()
    {
        StoreType = StoreType.CoffeeShop
    };
}
