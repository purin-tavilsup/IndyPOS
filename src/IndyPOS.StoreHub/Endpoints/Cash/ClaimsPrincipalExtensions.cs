using System.Security.Claims;
using IndyPOS.Application.Common.Authorization;

namespace IndyPOS.StoreHub.Endpoints.Cash;

internal static class ClaimsPrincipalExtensions
{
    /// <summary>The login token's user id (NameIdentifier, or the raw "sub"), or null if absent or not a Guid.</summary>
    public static Guid? FindUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(value, out var id) ? id : null;
    }

    /// <summary>Only call behind <see cref="RequireUserIdFilter"/>, which guarantees the id exists.</summary>
    public static Guid GetRequiredUserId(this ClaimsPrincipal user) =>
        user.FindUserId() ?? throw new InvalidOperationException("RequireUserIdFilter must run before this endpoint.");

    /// <summary>Whether the token's role grants a capability — the same lookup the policies use.</summary>
    public static bool HasCapability(this ClaimsPrincipal user, string capability) =>
        int.TryParse(user.FindFirst("role_id")?.Value, out var roleId)
        && RoleCapabilities.HasCapability(roleId, capability);
}
