namespace IndyPOS.CloudApi.Domain;

/// <summary>
/// Per-store configuration managed in Cloud.
/// Distributed to stores via GET /master/config.
/// Also contains OAuth2 client credentials for store authentication.
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

    // OAuth2 Client Credentials
    public string? ClientId { get; set; }
    public string? ClientSecretHash { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastAuthenticatedAtUtc { get; set; }
}
