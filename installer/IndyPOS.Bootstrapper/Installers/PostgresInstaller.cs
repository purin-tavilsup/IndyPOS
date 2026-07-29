using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.Win32;

namespace IndyPOS.Bootstrapper.Installers;

/// <summary>
/// Handles PostgreSQL detection and silent installation.
/// </summary>
public class PostgresInstaller
{
    // PostgreSQL 18 download URL (update when released)
    // Note: EDB hosts the official Windows installers
    private const string PostgresDownloadUrl =
        "https://get.enterprisedb.com/postgresql/postgresql-18.0-1-windows-x64.exe";

    // Fallback URLs
    private static readonly string[] FallbackUrls =
    {
        "https://sbp.enterprisedb.com/getfile.jsp?fileid=1259017", // May need update
    };

    private const string DefaultInstallPath = @"C:\Program Files\PostgreSQL\18";
    private const int DefaultPort = 5432;

    // Cache the downloaded installer outside %TEMP% so it survives smoke-test
    // re-install cycles. cleanup-v4.ps1 doesn't touch this dir (it's scoped
    // to the v4 install footprint, not the bootstrapper).
    private const string InstallerFileName = "postgresql-18-windows-x64.exe";
    private const long MinValidInstallerSize = 100L * 1024 * 1024; // 100 MB; full file is ~370 MB
    private static readonly string CacheDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "IndyPOS.Bootstrapper",
        "cache");

    /// <summary>
    /// Ensure PostgreSQL 18 is installed.
    /// </summary>
    public async Task<PostgresInstallerResult> EnsureInstalledAsync(
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // Check if already installed
        var existingInstall = FindPostgresInstallation();
        if (existingInstall != null)
        {
            progress?.Report(new DownloadProgress
            {
                StatusMessage = "PostgreSQL already installed",
                Percentage = 1
            });

            return new PostgresInstallerResult
            {
                Success = true,
                WasInstalled = false,
                BinPath = existingInstall.BinPath,
                SuperuserPassword = "" // Unknown for existing install
            };
        }

        // Cache lookup: skip download if a previous run already cached a
        // reasonably-sized installer. Saves ~300 MB on every smoke-test cycle.
        Directory.CreateDirectory(CacheDirectory);
        var installerPath = Path.Combine(CacheDirectory, InstallerFileName);

        if (File.Exists(installerPath) && new FileInfo(installerPath).Length >= MinValidInstallerSize)
        {
            progress?.Report(new DownloadProgress
            {
                StatusMessage = $"Using cached PostgreSQL installer ({new FileInfo(installerPath).Length / 1024 / 1024} MB)",
                Percentage = 0.5
            });
        }
        else
        {
            // Stale partial downloads (< MinValidInstallerSize) get overwritten below.
            progress?.Report(new DownloadProgress
            {
                StatusMessage = "Downloading PostgreSQL 18...",
                Percentage = 0
            });

            try
            {
                await DownloadHelper.DownloadFileAsync(
                    PostgresDownloadUrl,
                    installerPath,
                    progress,
                    cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                // Try fallback URLs
                var downloaded = false;
                foreach (var fallbackUrl in FallbackUrls)
                {
                    try
                    {
                        await DownloadHelper.DownloadFileAsync(
                            fallbackUrl,
                            installerPath,
                            progress,
                            cancellationToken);
                        downloaded = true;
                        break;
                    }
                    catch
                    {
                        // Try next URL
                    }
                }

                if (!downloaded)
                {
                    return new PostgresInstallerResult
                    {
                        Success = false,
                        ErrorMessage = $"Failed to download PostgreSQL: {ex.Message}\n\n" +
                                       "Please download PostgreSQL 18 manually from:\n" +
                                       "https://www.postgresql.org/download/windows/"
                    };
                }
            }
        }

        // Generate secure superuser password
        var superuserPassword = GenerateSecurePassword();

        // Run silent installation
        progress?.Report(new DownloadProgress
        {
            StatusMessage = "Installing PostgreSQL (this may take a few minutes)...",
            Percentage = 0.5
        });

        var installResult = await RunSilentInstallAsync(
            installerPath,
            superuserPassword,
            cancellationToken);

        // Keep the cached installer for next time — even on failure, the
        // installer file itself is fine; only the install attempt failed.
        // To force re-download, delete the cached file manually.

        if (!installResult.Success)
        {
            return installResult;
        }

        // Verify installation
        var verifiedInstall = FindPostgresInstallation();
        if (verifiedInstall == null)
        {
            return new PostgresInstallerResult
            {
                Success = false,
                ErrorMessage = "PostgreSQL installation completed but could not be verified"
            };
        }

        // Wait for PostgreSQL service to start
        progress?.Report(new DownloadProgress
        {
            StatusMessage = "Waiting for PostgreSQL service to start...",
            Percentage = 0.9
        });

        await WaitForPostgresServiceAsync(cancellationToken);

        progress?.Report(new DownloadProgress
        {
            StatusMessage = "PostgreSQL installed successfully",
            Percentage = 1
        });

        return new PostgresInstallerResult
        {
            Success = true,
            WasInstalled = true,
            BinPath = verifiedInstall.BinPath,
            SuperuserPassword = superuserPassword
        };
    }

    /// <summary>
    /// Run PostgreSQL silent installation.
    /// </summary>
    private async Task<PostgresInstallerResult> RunSilentInstallAsync(
        string installerPath,
        string superuserPassword,
        CancellationToken cancellationToken)
    {
        // PostgreSQL installer arguments for unattended install
        // See: https://www.enterprisedb.com/docs/supported-open-source/postgresql/installer/
        var args = $"--mode unattended " +
                   $"--unattendedmodeui minimal " +
                   $"--superpassword \"{superuserPassword}\" " +
                   $"--servicename postgresql-x64-18 " +
                   $"--serviceaccount \"NT AUTHORITY\\NetworkService\" " +
                   $"--serverport {DefaultPort} " +
                   $"--prefix \"{DefaultInstallPath}\" " +
                   $"--datadir \"{DefaultInstallPath}\\data\" " +
                   $"--install_runtimes 0"; // Don't install VC++ runtime (usually present)

        var psi = new ProcessStartInfo
        {
            FileName = installerPath,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        try
        {
            using var process = Process.Start(psi);
            if (process == null)
            {
                return new PostgresInstallerResult
                {
                    Success = false,
                    ErrorMessage = "Failed to start PostgreSQL installer"
                };
            }

            // Wait with timeout. 25 min is generous but accounts for Windows
            // Defender real-time scanning every extracted file — EDB's unpacker
            // writes ~10k files, each triggering a scan.
            var timeoutMinutes = 25;
            var completed = await Task.Run(() =>
                process.WaitForExit((int)TimeSpan.FromMinutes(timeoutMinutes).TotalMilliseconds),
                cancellationToken);

            if (!completed)
            {
                try { process.Kill(); } catch { }
                return new PostgresInstallerResult
                {
                    Success = false,
                    ErrorMessage = $"PostgreSQL installation timed out after {timeoutMinutes} minutes. " +
                                   "If this happens repeatedly, consider temporarily disabling Windows Defender " +
                                   "real-time scanning, or pre-installing PostgreSQL 18 manually."
                };
            }

            if (process.ExitCode != 0)
            {
                var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
                return new PostgresInstallerResult
                {
                    Success = false,
                    ErrorMessage = $"PostgreSQL installer exited with code {process.ExitCode}. {stderr}"
                };
            }

            return new PostgresInstallerResult
            {
                Success = true,
                WasInstalled = true,
                BinPath = Path.Combine(DefaultInstallPath, "bin"),
                SuperuserPassword = superuserPassword
            };
        }
        catch (Exception ex)
        {
            return new PostgresInstallerResult
            {
                Success = false,
                ErrorMessage = $"PostgreSQL installation failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// The bin directory of the PostgreSQL install this machine actually has, or null.
    /// Exposed for the upgrade path's pg_dump fallback: the manifest's PostgresBinPath
    /// can be stale. Note this probes 18 -> 17 -> 16, so callers must assert the major.
    /// </summary>
    internal static string? FindPostgresBinPath() => FindPostgresInstallation()?.BinPath;

    /// <summary>
    /// Find existing PostgreSQL installation. Requires BOTH psql.exe AND the
    /// matching Windows service to be registered — psql.exe alone is a half-
    /// installed state (e.g. installer killed during extraction) that would
    /// fool the next install into thinking Postgres is ready.
    /// </summary>
    private static PostgresInstallInfo? FindPostgresInstallation()
    {
        // Check common locations
        (string Path, string ServiceName)[] possible =
        {
            (@"C:\Program Files\PostgreSQL\18", "postgresql-x64-18"),
            (@"C:\Program Files\PostgreSQL\17", "postgresql-x64-17"),
            (@"C:\Program Files\PostgreSQL\16", "postgresql-x64-16")
        };

        foreach (var (path, serviceName) in possible)
        {
            var binPath = Path.Combine(path, "bin");
            var psqlPath = Path.Combine(binPath, "psql.exe");

            if (File.Exists(psqlPath) && ServiceExists(serviceName))
            {
                return new PostgresInstallInfo
                {
                    InstallPath = path,
                    BinPath = binPath,
                    Version = Path.GetFileName(path) // "18", "17", etc.
                };
            }
        }

        // Check registry for install location
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\PostgreSQL\Installations");

            if (key != null)
            {
                foreach (var subKeyName in key.GetSubKeyNames())
                {
                    using var subKey = key.OpenSubKey(subKeyName);
                    var basePath = subKey?.GetValue("Base Directory") as string;
                    var version = subKey?.GetValue("Version") as string;

                    if (!string.IsNullOrEmpty(basePath) && !string.IsNullOrEmpty(version))
                    {
                        var binPath = Path.Combine(basePath, "bin");
                        var majorVersion = version.Split('.')[0];
                        var serviceName = $"postgresql-x64-{majorVersion}";
                        if (File.Exists(Path.Combine(binPath, "psql.exe")) && ServiceExists(serviceName))
                        {
                            return new PostgresInstallInfo
                            {
                                InstallPath = basePath,
                                BinPath = binPath,
                                Version = version
                            };
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore registry errors
        }

        return null;
    }

    private static bool ServiceExists(string serviceName)
    {
        try
        {
            return System.ServiceProcess.ServiceController.GetServices()
                .Any(s => string.Equals(s.ServiceName, serviceName, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Wait for PostgreSQL service to be ready.
    /// </summary>
    private static async Task WaitForPostgresServiceAsync(CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromMinutes(2);
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Try to connect to PostgreSQL
                var psi = new ProcessStartInfo
                {
                    FileName = Path.Combine(DefaultInstallPath, "bin", "pg_isready.exe"),
                    Arguments = "-h 127.0.0.1 -p 5432",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    process.WaitForExit(5000);
                    if (process.ExitCode == 0)
                    {
                        return; // PostgreSQL is ready
                    }
                }
            }
            catch
            {
                // pg_isready not available or failed
            }

            await Task.Delay(2000, cancellationToken);
        }
    }

    /// <summary>
    /// Generate a secure random password.
    /// </summary>
    private static string GenerateSecurePassword()
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*";
        var bytes = new byte[24];
        RandomNumberGenerator.Fill(bytes);

        var password = new char[24];
        for (var i = 0; i < password.Length; i++)
        {
            password[i] = chars[bytes[i] % chars.Length];
        }

        return new string(password);
    }

    private class PostgresInstallInfo
    {
        public required string InstallPath { get; init; }
        public required string BinPath { get; init; }
        public required string Version { get; init; }
    }
}

/// <summary>
/// Result of PostgreSQL installation.
/// </summary>
public class PostgresInstallerResult
{
    public bool Success { get; init; }
    public bool WasInstalled { get; init; }
    public string BinPath { get; init; } = "";
    public string SuperuserPassword { get; init; } = "";
    public string? ErrorMessage { get; init; }
}
