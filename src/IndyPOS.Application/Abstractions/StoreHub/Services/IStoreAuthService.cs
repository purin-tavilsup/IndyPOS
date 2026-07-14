using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Application.Abstractions.StoreHub.Services;

/// <summary>
/// Service for local POS authentication via StoreHub.
/// Handles BCrypt verification and legacy TripleDES migration.
/// </summary>
public interface IStoreAuthService
{
    /// <summary>
    /// Authenticates a user with username and password.
    /// If user has legacy TripleDES password, migrates to BCrypt on successful login.
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="password">Plain text password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result with JWT token if successful</returns>
    Task<AuthResult> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of authentication attempt.
/// </summary>
public record AuthResult
{
    public bool Success { get; init; }
    public string? Token { get; init; }
    public AuthenticatedUser? User { get; init; }
    public string? ErrorMessage { get; init; }
    public bool MustChangePassword { get; init; }

    public static AuthResult Succeeded(string token, AuthenticatedUser user, bool mustChangePassword = false) =>
        new() { Success = true, Token = token, User = user, MustChangePassword = mustChangePassword };

    public static AuthResult Failed(string errorMessage) =>
        new() { Success = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Authenticated user info returned after successful login.
/// </summary>
public record AuthenticatedUser(
    Guid Id,
    string Username,
    string FirstName,
    string LastName,
    int RoleId,
    string StoreId);
