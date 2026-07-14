using System.Reflection;

namespace IndyPOS.Application.Common;

/// <summary>
/// Provides version information for the application.
/// Used by WinForms UI, StoreHub API, and future Velopack integration.
/// </summary>
public static class AppVersion
{
    /// <summary>
    /// Gets version info from the specified assembly.
    /// </summary>
    public static VersionInfo GetVersionInfo(Assembly assembly)
    {
        var version = assembly.GetName().Version ?? new Version(0, 0, 0, 0);

        var informationalVersion = assembly
            .GetCustomAttributes<AssemblyInformationalVersionAttribute>()
            .FirstOrDefault()?.InformationalVersion;

        // Strip source revision hash if present (e.g., "1.0.0+abc123" -> "1.0.0")
        var displayVersion = informationalVersion;
        if (!string.IsNullOrEmpty(displayVersion))
        {
            var plusIndex = displayVersion.IndexOf('+');
            if (plusIndex > 0)
            {
                displayVersion = displayVersion[..plusIndex];
            }
        }

        return new VersionInfo
        {
            Version = version,
            InformationalVersion = informationalVersion ?? version.ToString(),
            DisplayVersion = displayVersion ?? version.ToString(3),
            AssemblyVersion = version.ToString()
        };
    }

    /// <summary>
    /// Gets version info from the entry assembly.
    /// </summary>
    public static VersionInfo GetVersionInfo()
    {
        return GetVersionInfo(Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly());
    }
}

/// <summary>
/// Contains version information for the application.
/// </summary>
public class VersionInfo
{
    /// <summary>
    /// The parsed Version object (e.g., 1.0.0.0).
    /// </summary>
    public required Version Version { get; init; }

    /// <summary>
    /// Full informational version including pre-release and build metadata
    /// (e.g., "1.0.0-beta.1+abc123").
    /// </summary>
    public required string InformationalVersion { get; init; }

    /// <summary>
    /// Display-friendly version string without build metadata
    /// (e.g., "1.0.0-beta.1").
    /// </summary>
    public required string DisplayVersion { get; init; }

    /// <summary>
    /// Assembly version string (e.g., "1.0.0.0").
    /// </summary>
    public required string AssemblyVersion { get; init; }

    /// <summary>
    /// Compares this version to another version string.
    /// Returns true if this version is older than the other.
    /// </summary>
    public bool IsOlderThan(string otherVersion)
    {
        if (System.Version.TryParse(GetVersionPart(otherVersion), out var other))
        {
            return Version < other;
        }
        return false;
    }

    /// <summary>
    /// Extracts the version part from a full version string.
    /// "1.0.0-beta.1+abc123" -> "1.0.0"
    /// </summary>
    private static string GetVersionPart(string version)
    {
        var dashIndex = version.IndexOf('-');
        var plusIndex = version.IndexOf('+');

        var endIndex = Math.Min(
            dashIndex >= 0 ? dashIndex : version.Length,
            plusIndex >= 0 ? plusIndex : version.Length);

        return version[..endIndex];
    }
}
