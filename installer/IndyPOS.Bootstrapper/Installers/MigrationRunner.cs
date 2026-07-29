using System.Diagnostics;

namespace IndyPOS.Bootstrapper.Installers;

public sealed record MigrationRunResult(bool Success, bool AdminSeeded, string? ErrorMessage);

/// <summary>
/// Runs the StoreHub app's one-shot <c>migrate</c> command (apply EF migrations, seed the
/// initial admin, exit).
/// <para>USED BY BOTH INSTALL PATHS — fresh install and in-place upgrade. This is NOT
/// <see cref="DatabaseSetup"/>, which creates the role, the database and the connection
/// string and runs on a fresh install only.</para>
/// <para>Doing this as a console step BEFORE the service starts keeps the first service
/// start instant, so a schema build can't overrun the 30s SCM start timeout (error 1053).</para>
/// </summary>
public static class MigrationRunner
{
    public static async Task<MigrationRunResult> RunAsync(
        string storeHubInstallPath,
        IProgress<string>? log = null,
        CancellationToken cancellationToken = default)
    {
        var exePath = Path.Combine(storeHubInstallPath, "IndyPOS.StoreHub.exe");
        if (!File.Exists(exePath))
        {
            return new MigrationRunResult(false, false, $"StoreHub executable not found at {exePath}");
        }

        try
        {
            log?.Report("Applying database migrations and seeding admin...");

            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = "migrate",
                WorkingDirectory = storeHubInstallPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            // Production is the host default when unset, but be explicit so a stray dev
            // env var can't divert provisioning to the EnsureCreated path.
            psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";

            using var process = new Process { StartInfo = psi };
            process.Start();

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await Task.WhenAll(stdoutTask, stderrTask);
            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            await process.WaitForExitAsync(cancellationToken);

            return process.ExitCode != 0
                ? new MigrationRunResult(false, false,
                    $"Database provisioning failed (exit {process.ExitCode}): {stderr.Trim()}")
                : new MigrationRunResult(true, ParseAdminSeeded(stdout), null);
        }
        catch (Exception ex)
        {
            return new MigrationRunResult(false, false, $"Database provisioning failed: {ex.Message}");
        }
    }

    internal static bool ParseAdminSeeded(string stdout) =>
        stdout.Contains("ADMIN_SEEDED=true", StringComparison.OrdinalIgnoreCase);
}
