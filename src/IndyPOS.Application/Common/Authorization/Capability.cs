namespace IndyPOS.Application.Common.Authorization;

/// <summary>
/// Defines all capabilities in the system.
/// Naming convention: {domain}.{action} (e.g., "sales.complete", "products.read")
/// </summary>
public static class Capability
{
    // Sales operations
    public const string SalesComplete = "sales.complete";

    // Product operations
    public const string ProductsRead = "products.read";

    // Sync operations
    public const string SyncViewStatus = "sync.view_status";

    // Admin operations
    public const string AdminStoresRegister = "admin.stores.register";
}
