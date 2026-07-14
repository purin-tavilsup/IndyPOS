using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace IndyPOS.Vault;

/// <summary>
/// Stateless DPAPI seal/unseal for at-rest secrets in config files.
/// Machine-scoped: the installer (admin) encrypts; the service (LocalSystem)
/// decrypts on the same machine. Values are marked <c>DPAPI:&lt;base64&gt;</c> and
/// bound to their config key via per-key entropy.
/// </summary>
[SupportedOSPlatform("windows")]
public static class SecretProtector
{
    private const string Marker = "DPAPI:";

    public static bool IsProtected(string value) =>
        value is not null && value.StartsWith(Marker, StringComparison.Ordinal);

    public static string Protect(string key, string plaintext)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(plaintext);

        if (IsProtected(plaintext))
        {
            throw new InvalidOperationException(
                "Value is already protected; refusing to double-wrap.");
        }

        var encrypted = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(plaintext),
            GetEntropy(key),
            DataProtectionScope.LocalMachine);

        return Marker + Convert.ToBase64String(encrypted);
    }

    public static string Unprotect(string key, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(value);

        if (!IsProtected(value))
        {
            return value;
        }

        var encrypted = Convert.FromBase64String(value[Marker.Length..]);

        var plainBytes = ProtectedData.Unprotect(
            encrypted,
            GetEntropy(key),
            DataProtectionScope.LocalMachine);

        return Encoding.UTF8.GetString(plainBytes);
    }

    // Binds each ciphertext to its config key, so a value protected under one
    // key cannot be decrypted under another (prevents field substitution).
    private static byte[] GetEntropy(string key) =>
        SHA256.HashData(Encoding.UTF8.GetBytes($"IndyPOS:{key}"));
}
