using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace IndyPOS.StoreProfiles;

/// <summary>Files a dev store's till needs, generated from its profile.</summary>
public static class StoreProfileFiles
{
    // The same shape the till's JsonService reads (camelCase, Thai kept readable). PascalCase keys are
    // silently ignored by it and leave an empty receipt header.
    private static readonly JsonSerializerOptions TillJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        WriteIndented = true
    };

    /// <summary>The till's StoreConfiguration.json (receipt header, printer, barcode prefix).</summary>
    public static string StoreConfigurationJson(StoreProfile profile) =>
        JsonSerializer.Serialize(new
        {
            StoreFullName = profile.FullName,
            StoreName = profile.Name,
            StoreAddressLine1 = profile.AddressLine1,
            StoreAddressLine2 = profile.AddressLine2,
            StorePhoneNumber = profile.Phone,
            PrinterName = "XP-58",
            BarcodeScannerDeviceName = "",
            SerialPortName = "COM1",
            profile.Code
        }, TillJson);
}
