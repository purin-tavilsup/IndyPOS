using System.Runtime.Versioning;
using System.Security.Cryptography;
using IndyPOS.Vault;
using Microsoft.Extensions.Configuration;

namespace IndyPOS.StoreHub.Configuration;

/// <summary>
/// Decrypts DPAPI-protected configuration values in place at startup, before any
/// consumer (DbContext, JWT) reads them. Unmarked or absent keys are left as-is,
/// so dev/Aspire (which inject an unmarked connection string) are a no-op.
/// </summary>
[SupportedOSPlatform("windows")]
public static class SecretConfigurationExtensions
{
    public static void UnprotectSecrets(this ConfigurationManager configuration, params string[] keys)
    {
        var overrides = new Dictionary<string, string?>();

        foreach (var key in keys)
        {
            var value = configuration[key];
            if (string.IsNullOrWhiteSpace(value) || !SecretProtector.IsProtected(value))
            {
                continue;
            }

            overrides[key] = Decrypt(key, value);
        }

        if (overrides.Count > 0)
        {
            // Appended last → wins over the JSON file source for these keys.
            configuration.AddInMemoryCollection(overrides);
        }
    }

    private static string Decrypt(string key, string value)
    {
        try
        {
            return SecretProtector.Unprotect(key, value);
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            throw new InvalidOperationException(
                $"Could not decrypt configuration key '{key}'. This config was likely created " +
                "on a different machine, or the value is corrupt. The service cannot start.", ex);
        }
    }
}
