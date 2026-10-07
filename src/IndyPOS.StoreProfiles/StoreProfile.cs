using IndyPOS.Domain.Enums;

namespace IndyPOS.StoreProfiles;

/// <summary>
/// One store a developer can run or test as. Development and tests only: production stores are
/// configured by the installer, never by a profile.
/// </summary>
public sealed record StoreProfile(
    string Key,
    StoreType Type,
    string StoreId,
    string Name,
    string FullName,
    string AddressLine1,
    string AddressLine2,
    string Phone,
    int Code,
    int DevPort,
    IReadOnlyList<StoreProfileProduct> Products,
    IReadOnlyList<string> PaymentMethods)
{
    /// <summary>The cloud client id; CloudApi names every store's client this way.</summary>
    public string CloudClientId => $"store_{StoreId}";

    public string DatabaseName => $"storehub-{Key.ToLowerInvariant()}";

    // PaymentMethods: the catalogue codes this store has switched on, as the real store does. The rest
    // of the catalogue stays seeded but disabled.
}

/// <summary>A seed product. A non-trackable one (a service) is sold without moving stock.</summary>
public sealed record StoreProfileProduct(
    string Barcode,
    string Name,
    string Category,
    decimal UnitPrice,
    int InitialStock,
    bool IsTrackable = true);
