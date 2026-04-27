using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using IndyPOS.Domain.Enums;
using IndyPOS.Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace IndyPOS.Infrastructure.Services;

/// <summary>
/// Provides store identity information from configuration.
/// </summary>
public class StoreIdentityService : IStoreIdentityService
{
    private readonly StoreIdentityOptions _options;
    private readonly Lazy<StoreTypeFeatures> _features;
    private readonly Lazy<TimeZoneInfo> _timeZone;

    public StoreIdentityService(IOptions<StoreIdentityOptions> options)
    {
        _options = options.Value;
        _features = new Lazy<StoreTypeFeatures>(() => StoreTypeFeatures.For(_options.Type));
        _timeZone = new Lazy<TimeZoneInfo>(() => GetTimeZone(_options.TimeZoneId));
    }

    public string StoreId => _options.Id ?? GetDefaultStoreId();

    public string StoreName => _options.Name ?? "Default Store";

    public StoreType StoreType => _options.Type;

    public StoreTypeFeatures Features => _features.Value;

    public TimeZoneInfo TimeZone => _timeZone.Value;

    [Obsolete("Use StoreId (UUID) for identification. StoreCode is kept for barcode generation only.")]
    public int StoreCode => _options.Code;

    public void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.Id))
        {
            throw new InvalidOperationException(
                "Store.Id is not configured. Please set 'Store:Id' in appsettings.json. " +
                "Example: \"Store\": { \"Id\": \"550e8400-e29b-41d4-a716-446655440000\", \"Name\": \"My Store\", \"Type\": \"GeneralHardware\" }");
        }
    }

    /// <summary>
    /// Returns a default store ID for backward compatibility during migration.
    /// New installations should always configure an explicit Store.Id (UUID).
    /// </summary>
    private static string GetDefaultStoreId()
    {
        // Use machine name as fallback for existing single-store installations
        return $"STORE-{Environment.MachineName}".ToUpperInvariant();
    }

    /// <summary>
    /// Gets the TimeZoneInfo for the configured timezone ID.
    /// Falls back to local timezone if the configured ID is invalid.
    /// </summary>
    private static TimeZoneInfo GetTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            // Fall back to local timezone if configured ID is invalid
            return TimeZoneInfo.Local;
        }
    }
}
