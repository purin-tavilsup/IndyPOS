using IndyPOS.Application.Common.Authorization;
using IndyPOS.Application.Common.Interfaces;

namespace IndyPOS.Presentation.CashDrawer;

/// <summary>
/// What the logged-in user may do on the ลิ้นชักเก็บเงิน screen: cash.manage opens it for today;
/// reports.view also shows other days. Read from the shared role map, as /auth/me carries no
/// capabilities; StoreHub enforces the same rules on every call.
/// </summary>
public sealed record CashDrawerAccess(bool CanOpen, bool CanViewAnyDay)
{
    public static CashDrawerAccess For(ILoggedInUser user) => new(
        RoleCapabilities.HasCapability(user.RoleId, Capability.CashManage),
        RoleCapabilities.HasCapability(user.RoleId, Capability.ReportsView));
}
