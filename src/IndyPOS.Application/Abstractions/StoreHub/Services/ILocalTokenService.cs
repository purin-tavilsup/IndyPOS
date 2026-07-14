using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Application.Abstractions.StoreHub.Services;

/// <summary>
/// Service for generating local JWT tokens for POS authentication.
/// Tokens are used for StoreHub API authorization (12-hour expiry).
/// </summary>
public interface ILocalTokenService
{
    /// <summary>
    /// Generates a JWT token for an authenticated user.
    /// </summary>
    /// <param name="user">Authenticated store user</param>
    /// <returns>JWT token string</returns>
    string GenerateToken(StoreUser user);

    /// <summary>
    /// Validates a JWT token and extracts user claims.
    /// </summary>
    /// <param name="token">JWT token to validate</param>
    /// <returns>User claims if valid, null if invalid or expired</returns>
    TokenClaims? ValidateToken(string token);
}

/// <summary>
/// Claims extracted from a valid JWT token.
/// </summary>
public record TokenClaims(
    Guid UserId,
    string Username,
    int RoleId,
    string StoreId,
    DateTime ExpiresAt);
