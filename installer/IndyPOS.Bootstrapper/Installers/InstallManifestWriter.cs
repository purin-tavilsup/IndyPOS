using System.Text.Json;

namespace IndyPOS.Bootstrapper.Installers;

/// <summary>
/// Writes the post-install manifest to $SystemRoot\install-manifest.json.
/// Idempotent — overwrites any existing manifest. Schema is camelCase JSON.
/// </summary>
public static class InstallManifestWriter
{
    public const string ManifestFileName = "install-manifest.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task WriteAsync(
        InstallationConfig config,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(config.SystemRoot))
        {
            Directory.CreateDirectory(config.SystemRoot);
        }

        var manifestPath = Path.Combine(config.SystemRoot, ManifestFileName);
        var manifest = InstallManifest.From(config);
        var json = JsonSerializer.Serialize(manifest, SerializerOptions);

        await File.WriteAllTextAsync(manifestPath, json, cancellationToken);
    }
}
