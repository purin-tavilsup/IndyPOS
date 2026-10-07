using System.Text.Json;
using IndyPOS.StoreProfiles;

/// <summary>
/// Per-store files the till and StoreHub need in dev, written under the AppHost's obj folder so the
/// developer's real C:\ProgramData\IndyPOS files are never read or touched.
/// </summary>
internal static class DevStoreFiles
{
    public static string StoreConfigurationPath(string appHostDirectory, StoreProfile profile)
    {
        var path = Path.Combine(StoreDirectory(appHostDirectory, profile), "StoreConfiguration.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new
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
        }, new JsonSerializerOptions { WriteIndented = true }));
        return path;
    }

    public static string SecretsDirectory(string appHostDirectory, StoreProfile profile) =>
        Directory.CreateDirectory(Path.Combine(StoreDirectory(appHostDirectory, profile), "secrets")).FullName;

    private static string StoreDirectory(string appHostDirectory, StoreProfile profile) =>
        Directory.CreateDirectory(Path.Combine(appHostDirectory, "obj", "dev-stores", profile.Key)).FullName;
}
