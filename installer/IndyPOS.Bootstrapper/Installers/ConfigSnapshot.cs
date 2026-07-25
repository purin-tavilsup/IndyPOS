namespace IndyPOS.Bootstrapper.Installers;

/// <summary>
/// Remembers a config file's exact bytes so a failed step can put them back.
/// <para>
/// Extracting the StoreHub package overwrites <c>appsettings.json</c> with the shipped
/// template, and <see cref="DatabaseSetup"/> only rewrites the real values afterwards. If
/// extraction dies in between — as it does when a running service holds a DLL open — an
/// upgraded store is left with a template config and cannot start after the next reboot.
/// Restoring on failure keeps a failed upgrade non-destructive.
/// </para>
/// </summary>
internal sealed class ConfigSnapshot
{
    private readonly string _path;
    private readonly byte[]? _content;

    private ConfigSnapshot(string path, byte[]? content)
    {
        _path = path;
        _content = content;
    }

    /// <summary>Captures <paramref name="path"/>, or the fact that it did not exist.</summary>
    public static ConfigSnapshot Capture(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var content = File.Exists(path) ? File.ReadAllBytes(path) : null;

        return new ConfigSnapshot(path, content);
    }

    /// <summary>
    /// Puts the captured state back. Deletes the file if it did not exist when captured,
    /// so a failed first install does not leave a half-configured template behind.
    /// Idempotent, and never throws — it runs on the failure path, where the original
    /// error is the one worth reporting.
    /// </summary>
    public void Restore()
    {
        try
        {
            if (_content is null)
            {
                if (File.Exists(_path))
                {
                    File.Delete(_path);
                }

                return;
            }

            File.WriteAllBytes(_path, _content);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Nothing useful to do here: the caller is already failing the install.
        }
    }
}
