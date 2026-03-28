using IndyPOS.Application.Abstractions.Security;
using Microsoft.Extensions.Options;

namespace IndyPOS.Infrastructure.Services.StoreHub;

/// <summary>
/// Secure wrapper for CloudTokenOptions that retrieves ClientSecret from DPAPI storage.
/// Use this instead of CloudTokenOptions when you need runtime secret access.
/// </summary>
public class SecureCloudTokenOptions
{
    private readonly CloudTokenOptions _options;
    private readonly ISecretStorage _secretStorage;

    /// <summary>
    /// Secret key used to store the OAuth2 client secret in DPAPI.
    /// </summary>
    public const string ClientSecretKey = "CloudApi:ClientSecret";

    public SecureCloudTokenOptions(
        IOptions<CloudTokenOptions> options,
        ISecretStorage secretStorage)
    {
        _options = options.Value;
        _secretStorage = secretStorage;
    }

    /// <summary>
    /// OAuth2 Client ID (from config, not sensitive).
    /// </summary>
    public string ClientId => _options.ClientId;

    /// <summary>
    /// Scopes to request when acquiring tokens.
    /// </summary>
    public string Scopes => _options.Scopes;

    /// <summary>
    /// Get the OAuth2 client secret from secure storage.
    /// Falls back to config value if not found in secure storage.
    /// </summary>
    public async Task<string> GetClientSecretAsync()
    {
        // Try secure storage first
        var secret = await _secretStorage.GetSecretAsync(ClientSecretKey);

        if (!string.IsNullOrEmpty(secret))
        {
            return secret;
        }

        // Fall back to config (for development or migration)
        return _options.ClientSecret;
    }

    /// <summary>
    /// Store the client secret in secure storage (DPAPI).
    /// Call this during store registration or secret rotation.
    /// </summary>
    public async Task<bool> SetClientSecretAsync(string clientSecret)
    {
        return await _secretStorage.SetSecretAsync(ClientSecretKey, clientSecret);
    }

    /// <summary>
    /// Check if client secret is stored securely (vs config fallback).
    /// </summary>
    public async Task<bool> IsClientSecretSecuredAsync()
    {
        return await _secretStorage.ExistsAsync(ClientSecretKey);
    }
}
