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
        File.WriteAllText(path, StoreProfileFiles.StoreConfigurationJson(profile));
        return path;
    }

    public static string TillLogsDirectory(string appHostDirectory, StoreProfile profile) =>
        Directory.CreateDirectory(Path.Combine(StoreDirectory(appHostDirectory, profile), "till-logs")).FullName;

    public static string SecretsDirectory(string appHostDirectory, StoreProfile profile) =>
        Directory.CreateDirectory(Path.Combine(StoreDirectory(appHostDirectory, profile), "secrets")).FullName;

    private static string StoreDirectory(string appHostDirectory, StoreProfile profile) =>
        Directory.CreateDirectory(Path.Combine(appHostDirectory, "obj", "dev-stores", profile.Key)).FullName;
}
