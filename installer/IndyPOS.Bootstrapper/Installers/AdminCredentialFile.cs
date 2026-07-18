namespace IndyPOS.Bootstrapper.Installers;

/// <summary>
/// Writes the one-time admin bootstrap credential to an ACL-locked file.
/// Shared by the wizard finish screen and the silent installer so both deliver
/// the credential identically.
/// </summary>
public static class AdminCredentialFile
{
    /// <summary>
    /// Writes admin-credentials.txt in the config dir and ACL-locks it to
    /// Administrators + LocalSystem. Returns the path and whether the lock applied.
    /// Never throws — a failed write returns Locked=false so callers can react.
    /// </summary>
    public static (string Path, bool Locked) Write(InstallationConfig config)
    {
        var path = Path.Combine(config.ConfigDirectory, "admin-credentials.txt");
        var contents =
            "IndyPOS initial admin sign-in\r\n" +
            "================================\r\n" +
            $"Username: {config.AdminUsername}\r\n" +
            $"Password: {config.AdminPassword}\r\n\r\n" +
            "This is a one-time password. You will be required to set a new one\r\n" +
            "on first sign-in. Delete this file after you have signed in.\r\n";

        try
        {
            Directory.CreateDirectory(config.ConfigDirectory);
            File.WriteAllText(path, contents);
        }
        catch
        {
            return (path, false);
        }

        return (path, DatabaseSetup.TryRestrictFilePermissions(path));
    }
}
