using IndyPOS.Application.Common.Authorization;
using IndyPOS.Application.Common.Interfaces;

namespace IndyPOS.Presentation.SalesHistory;

/// <summary>
/// What the logged-in user may do with bills (spec §5): reports.view sees any day; sales.reprint
/// alone is held to today. The till reads it from the shared role map, because /auth/me carries no
/// capabilities; StoreHub still enforces the same rules on every call.
/// </summary>
public sealed record SalesHistoryAccess(string UserDisplayName, bool CanViewAnyDay, bool CanReprint)
{
    public bool IsTodayOnly => !CanViewAnyDay;

    public static SalesHistoryAccess For(ILoggedInUser user) => new(
        $"{user.FirstName} {user.LastName}".Trim(),
        RoleCapabilities.HasCapability(user.RoleId, Capability.ReportsView),
        RoleCapabilities.HasCapability(user.RoleId, Capability.SalesReprint));
}
