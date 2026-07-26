using System.ServiceProcess;
using System.Text.Json;
using Microsoft.Win32;
using IndyPOS.Bootstrapper.Installers;

namespace IndyPOS.Bootstrapper.Upgrade;

/// <summary>
/// Everything <see cref="InstallModeDetector"/> needs to know about the machine.
/// An interface so the ordered rules can be tested without a real install.
/// </summary>
public interface IInstallProbe
{
    bool ManifestExists { get; }
    string? ManifestInstallVersion { get; }

    /// <summary>
    /// A PostgreSQL install carrying the IndyPOS database. Note DatabaseName is NOT
    /// version-scoped, so this is true for a v4 store seen by a v5 installer.
    /// </summary>
    bool PostgresStoreDatabaseExists { get; }

    /// <summary>The registered service's ImagePath, or null when no such service exists.</summary>
    string? ServiceImagePath { get; }

    StoreHubConfigFacts Config { get; }
}

/// <summary>Reads the real machine. Every member is evaluated once, at construction.</summary>
public sealed class WindowsInstallProbe : IInstallProbe
{
    public WindowsInstallProbe(InstallationConfig config)
    {
        var manifestPath = Path.Combine(config.SystemRoot, InstallManifestWriter.ManifestFileName);
        ManifestExists = File.Exists(manifestPath);
        ManifestInstallVersion = ManifestExists ? ReadManifestVersion(manifestPath) : null;

        Config = StoreHubConfigReader.Read(
            Path.Combine(config.StoreHubInstallPath, "appsettings.json"));

        ServiceImagePath = ReadServiceImagePath(config.ServiceName);
        PostgresStoreDatabaseExists = DetectStoreDatabase(config);
    }

    public bool ManifestExists { get; }
    public string? ManifestInstallVersion { get; }
    public bool PostgresStoreDatabaseExists { get; }
    public string? ServiceImagePath { get; }
    public StoreHubConfigFacts Config { get; }

    private static string? ReadManifestVersion(string manifestPath)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));

            // ValueKind check, not a bare GetString(): that throws on a non-string token,
            // and a hand-edited manifest must degrade to "unknown version", never crash.
            return doc.RootElement.TryGetProperty("installVersion", out var v)
                   && v.ValueKind == JsonValueKind.String
                ? v.GetString()
                : null;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    // ServiceController does not expose ImagePath, so read the SCM registry key directly.
    private static string? ReadServiceImagePath(string serviceName)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                $@"SYSTEM\CurrentControlSet\Services\{serviceName}");
            return key?.GetValue("ImagePath") as string;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    // No superuser credential is available here, so this is a filesystem-and-config
    // inference rather than a query: a usable connection string means a provisioned
    // database, and a Postgres data directory means one could exist for another major.
    private static bool DetectStoreDatabase(InstallationConfig config)
    {
        var appSettings = Path.Combine(config.StoreHubInstallPath, "appsettings.json");
        if (StoreHubConfigReader.Read(appSettings).ConnectionStringUsable)
        {
            return true;
        }

        foreach (var major in new[] { "18", "17", "16" })
        {
            var dataDir = $@"C:\Program Files\PostgreSQL\{major}\data\base";
            if (Directory.Exists(dataDir) && OtherMajorStoreConfigExists())
            {
                return true;
            }
        }

        return false;
    }

    // C:\ProgramData\IndyPOS\v{N}\StoreHub\appsettings.json for a major other than ours
    // is the v5-installer-on-a-v4-store case that rule 1 exists to catch.
    private static bool OtherMajorStoreConfigExists()
    {
        var root = @"C:\ProgramData\IndyPOS";
        if (!Directory.Exists(root))
        {
            return false;
        }

        return Directory.EnumerateDirectories(root, "v*")
            .Select(d => Path.Combine(d, "StoreHub", "appsettings.json"))
            .Any(p => StoreHubConfigReader.Read(p).ConnectionStringUsable);
    }
}
