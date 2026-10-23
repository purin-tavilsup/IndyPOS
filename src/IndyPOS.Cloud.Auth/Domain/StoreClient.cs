namespace IndyPOS.Cloud.Auth.Domain;

/// <summary>
/// A store's credentials as Auth sees them: its OpenIddict client, and whether it may get a token. Auth owns this,
/// so CloudApi never reads it; CloudApi disables a store through Auth's store-client API.
/// </summary>
public class StoreClient
{
    public string StoreId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;

    /// <summary>False once the store is deactivated. Final: a store that comes back is registered again, as a new store.</summary>
    public bool Enabled { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? DisabledAtUtc { get; set; }
    public DateTime? LastAuthenticatedAtUtc { get; set; }
}
