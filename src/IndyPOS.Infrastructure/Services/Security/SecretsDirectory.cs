using Microsoft.Extensions.Configuration;

namespace IndyPOS.Infrastructure.Services.Security;

/// <summary>Where StoreHub keeps its DPAPI secrets.</summary>
public static class SecretsDirectory
{
    /// <summary>
    /// %ProgramData%\IndyPOS\Secrets, unless Secrets:Directory names another folder: a dev store must
    /// never read an installed StoreHub's secrets on the same machine (they are read before config).
    /// A blank value counts as unset: it would otherwise be a path relative to the working directory.
    /// </summary>
    public static string Resolve(IConfiguration configuration) =>
        configuration["Secrets:Directory"] is { } directory && !string.IsNullOrWhiteSpace(directory)
            ? directory
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "IndyPOS", "Secrets");
}
