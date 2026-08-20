namespace IndyPOS.Application.Common.Models;

/// <summary>
/// Startup guard for the shared JWT signing key. Both API hosts fall back to
/// <see cref="LocalTokenOptions.BuiltInDefaultSecretKey"/> when no LocalToken section is configured,
/// and that literal is published in a public repository — so outside development, running on it
/// would let anyone mint a token for the capability-protected admin endpoints.
/// </summary>
public static class LocalTokenOptionsValidator
{
    /// <summary>
    /// Throws unless the host has been given a real signing key. Development is the only exemption:
    /// every other environment name — Production, Testing, Staging, or a typo — must supply one, so
    /// a mistyped ASPNETCORE_ENVIRONMENT cannot silently disable the guard.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// The key is missing or is still the built-in default, and this is not development.
    /// </exception>
    public static void EnsureProductionSafe(LocalTokenOptions options, bool isDevelopment)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (isDevelopment || !options.UsesBuiltInDefaultSecretKey)
        {
            return;
        }

        throw new InvalidOperationException(
            $"{LocalTokenOptions.SectionName}:SecretKey is not configured, or is still the built-in " +
            "default that ships in this public repository. It signs the tokens that guard the admin " +
            "endpoints, so the host will not start outside Development without a real one. Set the " +
            $"environment variable {LocalTokenOptions.SectionName}__SecretKey, or the " +
            $"{LocalTokenOptions.SectionName}:SecretKey entry in appsettings.json — on an installed " +
            "till the installer's DatabaseSetup step writes a DPAPI-protected key there.");
    }
}
