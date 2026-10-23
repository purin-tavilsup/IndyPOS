using System.Text.Json;

namespace IndyPOS.MigrationTool.Services;

public sealed record StoreIdCheckResult(bool IsMatch, string Message);

/// <summary>
/// Refuses a --store-id that is not the installed StoreHub's own. Every migrated debt event carries the
/// --store-id, and the cloud refuses an event whose store is not the token's, so a typo here would park all
/// of them as Failed. Only an explicitly configured Store:Id counts: StoreHub's machine-name fallback is
/// exactly the misconfiguration this exists to catch.
/// </summary>
public static class StoreHubStoreIdCheck
{
    /// <summary>Where the v4 installer puts StoreHub's configuration (InstallationConfig.StoreHubInstallPath).</summary>
    public const string InstalledConfigPath = @"C:\ProgramData\IndyPOS\v4\StoreHub\appsettings.json";

    public static StoreIdCheckResult Check(string storeId, string storeHubConfigPath)
    {
        var configured = ReadConfiguredStoreId(storeHubConfigPath, out var problem);

        if (configured is null)
            return new StoreIdCheckResult(false, problem!);

        // Ordinal, as the cloud compares them: a case difference is another store.
        return string.Equals(storeId, configured, StringComparison.Ordinal)
            ? new StoreIdCheckResult(true, $"--store-id matches StoreHub's Store:Id '{configured}'.")
            : new StoreIdCheckResult(false,
                $"--store-id '{storeId}' is not this StoreHub's Store:Id '{configured}' (from {storeHubConfigPath}). " +
                "Every migrated event would be refused by the cloud. Use the StoreHub's store id.");
    }

    /// <remarks>
    /// Section and key names are matched ignoring case, as .NET configuration binds them; the installer
    /// writes "Store" and "Id". Read with System.Text.Json so the tool needs no configuration package.
    /// </remarks>
    private static string? ReadConfiguredStoreId(string path, out string? problem)
    {
        problem = null;
        if (!File.Exists(path))
        {
            problem = $"StoreHub's appsettings.json was not found at {path}. Pass its path with --storehub-config.";
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var id = Property(Property(document.RootElement, "Store"), "Id");

            if (id is { ValueKind: JsonValueKind.String } && !string.IsNullOrWhiteSpace(id.Value.GetString()))
                return id.Value.GetString();
        }
        catch (JsonException ex)
        {
            problem = $"StoreHub's configuration at {path} could not be read: {ex.Message}";
            return null;
        }

        problem = $"Store:Id is not set in {path}. StoreHub would fall back to a machine-name id, which the cloud " +
                  "does not know. Set Store:Id to this store's id first.";
        return null;
    }

    private static JsonElement? Property(JsonElement? element, string name)
    {
        if (element is not { ValueKind: JsonValueKind.Object } obj)
            return null;

        foreach (var property in obj.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                return property.Value;
        }

        return null;
    }
}
