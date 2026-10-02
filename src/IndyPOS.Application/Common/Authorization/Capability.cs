namespace IndyPOS.Application.Common.Authorization;

/// <summary>
/// Defines all capabilities in the system.
/// Naming convention: {domain}.{action} (e.g., "sales.complete", "products.read")
/// </summary>
public static class Capability
{
    // Sales operations
    public const string SalesComplete = "sales.complete";

    // Find and reprint bills. Alone it is held to today's bills; reports.view lifts that.
    public const string SalesReprint = "sales.reprint";

    // Product operations
    public const string ProductsRead = "products.read";
    public const string ProductsManage = "products.manage";

    // Payment method operations
    public const string PaymentMethodsManage = "payment_methods.manage";

    // Inventory operations
    public const string InventoryAdjust = "inventory.adjust";

    // Sync operations
    public const string SyncViewStatus = "sync.view_status";

    // Admin operations
    public const string AdminStoresRegister = "admin.stores.register";

    // User management (admin)
    public const string UsersRead = "users.read";
    public const string UsersCreate = "users.create";
    public const string UsersUpdate = "users.update";
    public const string UsersDeactivate = "users.deactivate";

    // Report operations
    public const string ReportsView = "reports.view";

    // Cash drawer (ลิ้นชักเก็บเงิน): add/edit/delete/view payouts, floats, repayments and counts
    public const string CashManage = "cash.manage";
}
