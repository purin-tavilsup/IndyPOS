namespace IndyPOS.Application.Abstractions.StoreHub.Services;

/// <summary>
/// Service for secure password hashing and verification.
/// Implementation uses BCrypt with configurable work factor.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plain text password using BCrypt.
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <returns>BCrypt hash string</returns>
    string Hash(string password);

    /// <summary>
    /// Verifies a plain text password against a BCrypt hash.
    /// </summary>
    /// <param name="password">Plain text password to verify</param>
    /// <param name="hash">BCrypt hash to verify against</param>
    /// <returns>True if password matches, false otherwise</returns>
    bool Verify(string password, string hash);
}
