namespace IndyPOS.Application.Abstractions.Security;

/// <summary>
/// Abstraction for secure secret storage.
/// Implementations should use platform-specific secure storage (DPAPI on Windows, Keychain on macOS, etc.)
/// </summary>
public interface ISecretStorage
{
    /// <summary>
    /// Store a secret value securely.
    /// </summary>
    /// <param name="key">Unique identifier for the secret</param>
    /// <param name="value">Secret value to store</param>
    /// <returns>True if stored successfully, false otherwise</returns>
    Task<bool> SetSecretAsync(string key, string value);

    /// <summary>
    /// Retrieve a secret value.
    /// </summary>
    /// <param name="key">Unique identifier for the secret</param>
    /// <returns>Secret value if found, null otherwise</returns>
    Task<string?> GetSecretAsync(string key);

    /// <summary>
    /// Delete a secret.
    /// </summary>
    /// <param name="key">Unique identifier for the secret</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteSecretAsync(string key);

    /// <summary>
    /// Check if a secret exists.
    /// </summary>
    /// <param name="key">Unique identifier for the secret</param>
    /// <returns>True if secret exists</returns>
    Task<bool> ExistsAsync(string key);
}
