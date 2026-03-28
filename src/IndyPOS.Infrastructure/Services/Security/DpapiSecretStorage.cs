using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using IndyPOS.Application.Abstractions.Security;
using Microsoft.Extensions.Logging;

namespace IndyPOS.Infrastructure.Services.Security;

/// <summary>
/// Windows DPAPI-based secret storage.
/// Secrets are encrypted using the current user's credentials and stored in files.
/// Secrets are machine and user-specific - they cannot be decrypted on another machine.
/// </summary>
[SupportedOSPlatform("windows")]
public class DpapiSecretStorage : ISecretStorage
{
    private readonly string _secretsDirectory;
    private readonly ILogger<DpapiSecretStorage> _logger;
    private readonly DataProtectionScope _scope;

    /// <summary>
    /// Create DPAPI secret storage.
    /// </summary>
    /// <param name="secretsDirectory">Directory to store encrypted secrets</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="scope">Protection scope (CurrentUser or LocalMachine)</param>
    public DpapiSecretStorage(
        string secretsDirectory,
        ILogger<DpapiSecretStorage> logger,
        DataProtectionScope scope = DataProtectionScope.CurrentUser)
    {
        _secretsDirectory = secretsDirectory;
        _logger = logger;
        _scope = scope;

        // Ensure directory exists
        if (!Directory.Exists(_secretsDirectory))
        {
            Directory.CreateDirectory(_secretsDirectory);
        }
    }

    public Task<bool> SetSecretAsync(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            var filePath = GetSecretFilePath(key);
            var plainBytes = Encoding.UTF8.GetBytes(value);

            // Encrypt using DPAPI
            var encryptedBytes = ProtectedData.Protect(
                plainBytes,
                GetEntropy(key),
                _scope);

            // Write encrypted data to file
            File.WriteAllBytes(filePath, encryptedBytes);

            _logger.LogDebug("Secret '{Key}' stored successfully", key);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store secret '{Key}'", key);
            return Task.FromResult(false);
        }
    }

    public Task<string?> GetSecretAsync(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var filePath = GetSecretFilePath(key);

            if (!File.Exists(filePath))
            {
                _logger.LogDebug("Secret '{Key}' not found", key);
                return Task.FromResult<string?>(null);
            }

            var encryptedBytes = File.ReadAllBytes(filePath);

            // Decrypt using DPAPI
            var plainBytes = ProtectedData.Unprotect(
                encryptedBytes,
                GetEntropy(key),
                _scope);

            var value = Encoding.UTF8.GetString(plainBytes);

            _logger.LogDebug("Secret '{Key}' retrieved successfully", key);
            return Task.FromResult<string?>(value);
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Failed to decrypt secret '{Key}' - may have been created by a different user", key);
            return Task.FromResult<string?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve secret '{Key}'", key);
            return Task.FromResult<string?>(null);
        }
    }

    public Task<bool> DeleteSecretAsync(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var filePath = GetSecretFilePath(key);

            if (!File.Exists(filePath))
            {
                return Task.FromResult(false);
            }

            File.Delete(filePath);
            _logger.LogDebug("Secret '{Key}' deleted", key);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete secret '{Key}'", key);
            return Task.FromResult(false);
        }
    }

    public Task<bool> ExistsAsync(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var filePath = GetSecretFilePath(key);
        return Task.FromResult(File.Exists(filePath));
    }

    /// <summary>
    /// Get file path for a secret key.
    /// </summary>
    private string GetSecretFilePath(string key)
    {
        // Sanitize key for file name
        var safeKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(key))
            .Replace('/', '_')
            .Replace('+', '-')
            .Replace('=', '.');

        return Path.Combine(_secretsDirectory, $"{safeKey}.secret");
    }

    /// <summary>
    /// Generate additional entropy for DPAPI based on secret key.
    /// This adds an extra layer of security - secrets are tied to their key name.
    /// </summary>
    private static byte[] GetEntropy(string key)
    {
        // Use SHA256 of key name as entropy
        return SHA256.HashData(Encoding.UTF8.GetBytes($"IndyPOS:{key}"));
    }
}
