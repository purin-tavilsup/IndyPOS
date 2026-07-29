namespace IndyPOS.Bootstrapper.Upgrade;

/// <summary>
/// Classifies a machine. Rules are evaluated IN ORDER and the first match wins —
/// see the upgrade design spec, section 3. Reordering them reintroduces the data-loss
/// path rule 1 exists to close.
/// </summary>
public static class InstallModeDetector
{
    public static DetectedInstall Detect(IInstallProbe probe, string storeHubInstallPath)
    {
        ArgumentNullException.ThrowIfNull(probe);

        // Rule 1 — must precede the manifest lookup. SystemRoot is version-scoped but
        // DatabaseName is not, so a newer major would read a live store as Fresh and
        // then collide with its database.
        if (probe.PostgresStoreDatabaseExists && !probe.ManifestExists)
        {
            return Unusable(probe,
                "An IndyPOS database exists on this machine but belongs to another IndyPOS " +
                "major version. Do not remove PostgreSQL — it holds the store's sales history. " +
                "Run the installer for the version that owns that install root instead.");
        }

        // Rule 2 — everything an upgrade needs is present and coherent.
        if (probe.ManifestExists)
        {
            if (!probe.Config.ConnectionStringUsable)
            {
                return Unusable(probe,
                    "The StoreHub connection string is missing, empty, or cannot be decrypted on " +
                    "this machine. Restore appsettings.json from a backup before upgrading.");
            }

            if (probe.Config.StoreId is null)
            {
                return Unusable(probe,
                    "Store:Id is missing from appsettings.json. Upgrading without it would seed a " +
                    "second payment-method catalogue under a fallback store id and orphan the " +
                    "store's sales history. Set Store:Id to this store's real id first.");
            }

            if (probe.Config.StoreType is null)
            {
                return Unusable(probe,
                    "Store:Type is missing from appsettings.json (stores installed before " +
                    "2026-07-18 have no such key). It cannot be defaulted: the default is the most " +
                    "permissive store type and would silently re-enable restricted features. " +
                    "Add \"type\": \"GeneralHardware\" (or \"Minimart\") to the \"store\" section " +
                    "and re-run - see docs\\operations\\upgrade-procedure.md. Note --store-type " +
                    "does NOT fix this: the run is refused before arguments are consulted.");
            }

            if (!ImagePathResolvesUnder(probe.ServiceImagePath, storeHubInstallPath))
            {
                return Unusable(probe,
                    "The StoreHub service is not registered, or its ImagePath does not resolve " +
                    $"under {storeHubInstallPath}. Upgrading would deploy binaries the service " +
                    "does not run.");
            }

            return new DetectedInstall(
                InstallMode.Upgrade,
                probe.ManifestInstallVersion,
                probe.Config.StoreId,
                probe.Config.StoreType,
                "An existing IndyPOS install was detected and can be upgraded in place.");
        }

        // Rule 3 — a genuinely bare machine. BOTH must be absent.
        if (!probe.Config.Exists)
        {
            return new DetectedInstall(InstallMode.Fresh, null, null, null,
                "No existing IndyPOS install was found.");
        }

        // Rule 4 — anything else.
        return Unusable(probe,
            "A StoreHub configuration exists but there is no install manifest, so this machine " +
            "is neither a clean install nor a complete one. Inspect " +
            $"{storeHubInstallPath} before proceeding.");
    }

    private static DetectedInstall Unusable(IInstallProbe probe, string reason) =>
        new(InstallMode.Unusable, probe.ManifestInstallVersion, probe.Config.StoreId,
            probe.Config.StoreType, reason);

    // sc.exe records binPath with surrounding quotes when the path contains spaces,
    // and "C:\ProgramData\..." always does not — but a hand-registered service may.
    private static bool ImagePathResolvesUnder(string? imagePath, string installPath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return false;
        }

        var trimmed = imagePath.Trim().Trim('"');

        // Trailing separator turns this into a directory-boundary check, not a bare
        // string prefix -- otherwise a sibling like "...\v4\StoreHubOLD\..." would
        // satisfy a plain StartsWith(installPath) even though it is a different folder.
        var boundary = installPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                       + Path.DirectorySeparatorChar;

        return trimmed.StartsWith(boundary, StringComparison.OrdinalIgnoreCase);
    }
}
