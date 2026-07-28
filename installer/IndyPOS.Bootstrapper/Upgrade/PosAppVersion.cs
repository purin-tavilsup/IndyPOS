using System.Xml.Linq;

namespace IndyPOS.Bootstrapper.Upgrade;

/// <summary>
/// Reads the POS app version Velopack believes is installed.
/// <para>A 2026-07-26 VM spike established that <c>Setup.exe --silent</c> exits 0 whether it
/// upgraded the app or merely repaired the same version, and that the binaries keep their
/// own build stamp regardless of the package version. Comparing this file before and after
/// is therefore the only honest way to derive POS_UPDATED.</para>
/// </summary>
public static class PosAppVersion
{
    public const string VersionFileName = "sq.version";

    /// <param name="velopackCurrentDirectory">
    /// The Velopack <c>current</c> directory — i.e. <see cref="Installers.InstallationConfig.VelopackInstallPath"/>.
    /// </param>
    /// <returns>The package version, or null when it cannot be established.</returns>
    public static string? Read(string velopackCurrentDirectory)
    {
        if (string.IsNullOrWhiteSpace(velopackCurrentDirectory))
        {
            return null;
        }

        var path = Path.Combine(velopackCurrentDirectory, VersionFileName);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            // sq.version is a nuspec document, not a bare version string. Match on local
            // name so the packaging namespace cannot break this.
            var version = XDocument.Load(path)
                .Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "version")
                ?.Value
                .Trim();

            return string.IsNullOrWhiteSpace(version) ? null : version;
        }
        catch (Exception ex) when (ex is System.Xml.XmlException or IOException)
        {
            return null;
        }
    }
}
