namespace IndyPOS.CloudApi.Domain;

/// <summary>
/// Master user managed in Cloud.
/// Distributed to stores via GET /master/users/{storeId}.
/// Passwords are NOT stored here - managed locally at each store.
/// </summary>
public class CloudUser
{
    public Guid Id { get; set; }
    public string StoreId { get; set; } = default!;
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public bool IsActive { get; set; } = true;
    public long Version { get; set; }  // Monotonic version for sync safety (avoids clock drift)
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastModifiedAtUtc { get; set; }
}
