namespace IndyPOS.Application.Common.Authorization;

using IndyPOS.Application.Common.Enums;

/// <summary>
/// Maps UserRoles to their allowed capabilities.
/// This is the single source of truth for RBAC.
/// </summary>
public static class RoleCapabilities
{
    private static readonly HashSet<string> Empty = [];

    private static readonly Dictionary<UserRole, HashSet<string>> _roleCapabilities = new()
    {
        [UserRole.Cashier] =
        [
            Capability.ProductsRead,
            Capability.SalesComplete,
        ],

        [UserRole.StoreManager] =
        [
            Capability.ProductsRead,
            Capability.ProductsManage,
            Capability.InventoryAdjust,
            Capability.SalesComplete,
            Capability.SyncViewStatus,
            Capability.ReportsView,
        ],

        [UserRole.SystemAdmin] =
        [
            Capability.ProductsRead,
            Capability.ProductsManage,
            Capability.InventoryAdjust,
            Capability.SalesComplete,
            Capability.SyncViewStatus,
            Capability.AdminStoresRegister,
            Capability.UsersRead,
            Capability.UsersCreate,
            Capability.UsersUpdate,
            Capability.UsersDeactivate,
            Capability.ReportsView,
        ]
    };

    /// <summary>
    /// Checks if a role has a specific capability.
    /// </summary>
    public static bool HasCapability(int roleId, string capability)
    {
        // TryGetValue handles invalid roleId by returning false
        return _roleCapabilities.TryGetValue((UserRole)roleId, out var caps)
               && caps.Contains(capability);
    }

    /// <summary>
    /// Gets all capabilities for a role.
    /// </summary>
    public static IReadOnlySet<string> GetCapabilities(UserRole role)
    {
        return _roleCapabilities.TryGetValue(role, out var caps) ? caps : Empty;
    }
}
