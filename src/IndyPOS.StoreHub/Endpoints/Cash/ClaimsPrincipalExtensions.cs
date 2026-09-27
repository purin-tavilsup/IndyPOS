using System.Security.Claims;

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
}
