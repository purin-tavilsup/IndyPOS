namespace IndyPOS.Domain.Entities.Core;

/// <summary>
/// Key-value store settings for a store.
/// Used for barcode counter, store preferences, etc.
/// </summary>
public class StoreSetting
{
    public string Key { get; set; } = default!;
    public string Value { get; set; } = default!;
    public DateTime LastModifiedUtc { get; set; }

    public static StoreSetting Create(string key, string value)
    {
        return new StoreSetting
        {
            Key = key,
            Value = value,
            LastModifiedUtc = DateTime.UtcNow
        };
    }

    public void UpdateValue(string value)
    {
        Value = value;
        LastModifiedUtc = DateTime.UtcNow;
    }
}

/// <summary>
/// Well-known setting keys.
/// </summary>
public static class StoreSettingKeys
{
    public const string BarcodeCounter = "BarcodeCounter";
}
