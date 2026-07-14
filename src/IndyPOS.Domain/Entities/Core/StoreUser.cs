namespace IndyPOS.Domain.Entities.Core;

/// <summary>
/// Local user entity for StoreHub authentication.
/// Supports BCrypt password hashing with migration from legacy TripleDES.
/// </summary>
public class StoreUser
{
    public Guid Id { get; set; }

    /// <summary>
    /// Store identifier from IStoreIdentityService.
    /// </summary>
    public string StoreId { get; set; } = default!;

    /// <summary>
    /// Legacy SQLite UserId for migration. UNIQUE constraint prevents duplicates.
    /// </summary>
    public int LegacyUserId { get; set; }

    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password hash (BCrypt or legacy TripleDES based on PasswordHashVersion).
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// 1 = Legacy TripleDES (needs migration), 2 = BCrypt.
    /// </summary>
    public int PasswordHashVersion { get; set; } = 2;

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// User role: Cashier=1, StoreManager=2, SystemAdmin=3.
    /// </summary>
    public int RoleId { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When true, the user must rotate their password before any other action.
    /// Set on the seeded bootstrap admin; cleared on first successful change.
    /// </summary>
    public bool MustChangePassword { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastModifiedAtUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }

    /// <summary>
    /// Link to CloudApi user for sync (Epic S2).
    /// </summary>
    public Guid? CloudUserId { get; set; }

    /// <summary>
    /// Last time this user was synced from CloudApi.
    /// </summary>
    public DateTime? LastSyncedAtUtc { get; set; }

    /// <summary>
    /// Version from CloudApi for incremental sync (avoids clock drift).
    /// </summary>
    public long CloudVersion { get; set; }
}
