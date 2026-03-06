using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using Microsoft.Extensions.Options;

namespace IndyPOS.Infrastructure.Services;

/// <summary>
/// Provides store identity information from configuration.
/// </summary>
public class StoreIdentityService : IStoreIdentityService
{
    private readonly StoreIdentityOptions _options;

    public StoreIdentityService(IOptions<StoreIdentityOptions> options)
    {
        _options = options.Value;
    }

    public string StoreId => _options.Id ?? GetDefaultStoreId();

    public string StoreName => _options.Name ?? "Default Store";

    public void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.Id))
        {
            throw new InvalidOperationException(
                "Store.Id is not configured. Please set 'Store:Id' in appsettings.json. " +
                "Example: \"Store\": { \"Id\": \"STORE-001\", \"Name\": \"My Store\" }");
        }
    }

    /// <summary>
    /// Returns a default store ID for backward compatibility during migration.
    /// New installations should always configure an explicit Store.Id.
    /// </summary>
    private static string GetDefaultStoreId()
    {
        // Use machine name as fallback for existing single-store installations
        return $"STORE-{Environment.MachineName}".ToUpperInvariant();
    }
}
