namespace IndyPOS.CloudApi.Domain;

/// <summary>
/// Per-store configuration managed in Cloud.
/// Distributed to stores via GET /master/config.
/// ClientId is the store's client in Cloud.Auth, recorded here for reference; Auth holds the secret and decides
/// whether the store gets a token.
/// </summary>
public class CloudStoreConfig
{
    public string StoreId { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public string StoreFullName { get; set; } = string.Empty;
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? PhoneNumber { get; set; }
    public string? PrinterName { get; set; }
    public DateTime LastModifiedAtUtc { get; set; }

    public string? ClientId { get; set; }
    public bool IsActive { get; set; } = true;
}
