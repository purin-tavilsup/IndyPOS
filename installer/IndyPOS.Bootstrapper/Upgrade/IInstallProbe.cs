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

            // ValueKind checks, not a bare GetString()/TryGetProperty(): TryGetProperty
            // throws InvalidOperationException when the root isn't an object (a bare
            // string/number/array manifest), and GetString() throws on a non-string
            // token. A hand-edited manifest must degrade to "unknown version", never crash.
            return doc.RootElement.ValueKind == JsonValueKind.Object
                   && doc.RootElement.TryGetProperty("installVersion", out var v)
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
    // inference rather than a query: a usable connection string in ANOTHER major's
    // StoreHub config means a database already exists for that major. This major's
    // own config is already captured in probe.Config and must be excluded here, or a
    // same-major partial install (config written by DatabaseSetup, manifest not yet
    // written by InstallManifestWriter -- the exact state a crashed run leaves behind)
    // would misread as "belongs to another major" instead of "this install is incomplete".
    private static bool DetectStoreDatabase(InstallationConfig config) =>
        OtherMajorStoreConfigExists(config.SystemRoot);

    private static bool OtherMajorStoreConfigExists(string ownSystemRoot)
    {
        const string root = @"C:\ProgramData\IndyPOS";
        try
        {
            if (!Directory.Exists(root))
            {
                return false;
            }

            return Directory.EnumerateDirectories(root, "v*")
                .Where(d => !string.Equals(d, ownSystemRoot, StringComparison.OrdinalIgnoreCase))
                .Select(d => Path.Combine(d, "StoreHub", "appsettings.json"))
                .Any(p => StoreHubConfigReader.Read(p).ConnectionStringUsable);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
