using System.IO;
using System.Reflection;

namespace IndyPOS.Application.Common;

/// <summary>
/// Resolves the versioned data root shared by the v{Major} install
/// (e.g. <c>C:\ProgramData\IndyPOS\v4</c>). Mirrors the installer's
/// <c>InstallationConfig</c> path scheme so the app and installer stay in
/// lockstep — and, because the root keys off the <em>major</em> version,
/// it survives Velopack auto-updates within a major line (v4.0.0 → v4.0.1).
/// </summary>
public static class InstallPaths
{
    private const string BaseDirectory = @"C:\ProgramData\IndyPOS";

    // Major version segment, e.g. "4". Derived once from the assembly version,
    // matching how InstallationConfig derives its own version.
    private static readonly string Major = ResolveMajorVersion();

    public static string SystemRoot => Path.Combine(BaseDirectory, $"v{Major}");
    public static string ConfigDirectory => Path.Combine(SystemRoot, "Config");
    public static string StoreConfigPath => Path.Combine(ConfigDirectory, "StoreConfiguration.json");
    public static string LogsDirectory => Path.Combine(SystemRoot, "logs");
    public static string ReportsDirectory => Path.Combine(SystemRoot, "Reports");

    private static string ResolveMajorVersion()
    {
        var version = typeof(InstallPaths).Assembly.GetName().Version;
        return (version?.Major ?? 4).ToString();
    }
}
