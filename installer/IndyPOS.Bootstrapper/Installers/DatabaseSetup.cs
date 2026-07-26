using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using IndyPOS.Bootstrapper.Upgrade;
using IndyPOS.Vault;

namespace IndyPOS.Bootstrapper.Installers;

/// <summary>
/// Handles database creation and configuration.
/// <para>
/// FRESH INSTALL ONLY. Never runs on an upgrade — the role, its password and the
/// DPAPI-protected connection string already exist there, so asking the
/// database-provisioning question at all is what produced the superuser-password
/// failure this guard reports. See the upgrade design spec, sections 2 and 4.
/// </para>
/// </summary>
public class DatabaseSetup
{
    private InstallationConfig? _config;

    private InstallationConfig Config =>
        _config ?? throw new InvalidOperationException(
            "DatabaseSetup has not been configured. Call SetupAsync first.");

    /// <summary>
    /// Set up the database, user, and configuration files.
    /// </summary>
    public async Task<DatabaseSetupResult> SetupAsync(
        InstallationConfig config,
        string postgresPassword,
        IProgress<string>? log = null,
        CancellationToken cancellationToken = default)
    {
        _config = config;

        // PostgresInstaller returns an empty password when it detects an
        // existing install — the silent-install password we generated last
        // time isn't persisted, so we can't reuse it. Without it, psql can't
        // authenticate as the superuser to create the role + database. Fail
        // fast with actionable guidance instead of letting psql hang on a
        // password prompt (psql defaults to interactive prompting when
        // PGPASSWORD is empty, even with stdin redirected).
        if (string.IsNullOrEmpty(postgresPassword))
        {
            var storeInstallExists = StoreHubConfigReader
                .Read(Path.Combine(config.StoreHubInstallPath, "appsettings.json"))
                .Exists;

            return new DatabaseSetupResult
            {
                Success = false,
                ErrorMessage = BuildSuperuserGuardMessage(storeInstallExists)
            };
        }

        try
        {
            log?.Report("Creating system directories...");
            CreateDirectories();

            log?.Report("Generating JWT secret key...");
            var jwtSecret = GenerateJwtSecret();

            log?.Report($"Creating database user '{config.AppUser}'...");
            var userCreated = await CreateDatabaseUserAsync(
                config.PostgresBinPath,
                postgresPassword,
                config.AppUser,
                config.AppPassword,
                cancellationToken);

            if (!userCreated)
            {
                log?.Report("User already exists, continuing...");
            }

            log?.Report($"Creating database '{config.DatabaseName}'...");
            var dbCreated = await CreateDatabaseAsync(
                config.PostgresBinPath,
                postgresPassword,
                config.DatabaseName,
                config.AppUser,
                cancellationToken);

            if (!dbCreated)
            {
                log?.Report("Database already exists, continuing...");
            }

            log?.Report("Configuring database permissions...");
            await ConfigureDatabaseAsync(
                config.PostgresBinPath,
                postgresPassword,
                config.DatabaseName,
                config.AppUser,
                cancellationToken);

            log?.Report("Creating StoreHub configuration...");
            await CreateStoreHubConfigAsync(jwtSecret, cancellationToken);

            log?.Report("Creating store configuration template...");
            await CreateStoreConfigTemplateAsync(cancellationToken);

            return new DatabaseSetupResult
            {
                Success = true,
                JwtSecret = jwtSecret
            };
        }
        catch (Exception ex)
        {
            return new DatabaseSetupResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private void CreateDirectories()
    {
        var directories = new[]
        {
            Config.ConfigDirectory,
            Config.KeysDirectory,
            Config.LogsDirectory,
            Config.BackupsDirectory,
            Config.StoreHubInstallPath
        };

        foreach (var dir in directories)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
    }

    // 64-byte (512-bit) random signing key. No longer persisted to a separate
    // file — it is DPAPI-protected inside appsettings.json (the only consumer).
    // A fresh secret is generated per install; 12h token expiry makes that fine.
    internal static string GenerateJwtSecret()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// The superuser password is deliberately not persisted (spec section 1), so an existing
    /// PostgreSQL cannot be provisioned into. What the operator should do next depends
    /// entirely on whether IndyPOS was ever installed on this machine (which means a store's
    /// sales history is at risk).
    /// </summary>
    internal static string BuildSuperuserGuardMessage(bool storeInstallExists) =>
        storeInstallExists
            ? "PostgreSQL 18 is already installed and this machine has an existing IndyPOS database.\n" +
              "Do NOT remove PostgreSQL — that would destroy the store's sales history.\n" +
              "Upgrade in place instead:\n" +
              "  IndyPOS-Setup.exe --silent\n" +
              "See docs\\operations\\upgrade-procedure.md."
            : "PostgreSQL 18 is already installed, but its superuser password is unknown " +
              "(the installer doesn't persist it across runs). To proceed, either:\n" +
              "  - Uninstall PostgreSQL: scripts\\cleanup-v4.ps1 -Force -RemovePostgres\n" +
              "  - Or remove C:\\Program Files\\PostgreSQL\\18 manually,\n" +
              "then re-run this installer for a clean Postgres install.";

    internal static void RestrictFilePermissions(string filePath) => TryRestrictFilePermissions(filePath);

    internal static bool TryRestrictFilePermissions(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            var security = fileInfo.GetAccessControl();

            security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

            var rules = security.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
            foreach (System.Security.AccessControl.FileSystemAccessRule rule in rules)
            {
                security.RemoveAccessRule(rule);
            }

            security.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(
                new System.Security.Principal.SecurityIdentifier(
                    System.Security.Principal.WellKnownSidType.BuiltinAdministratorsSid, null),
                System.Security.AccessControl.FileSystemRights.FullControl,
                System.Security.AccessControl.AccessControlType.Allow));

            security.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(
                new System.Security.Principal.SecurityIdentifier(
                    System.Security.Principal.WellKnownSidType.LocalSystemSid, null),
                System.Security.AccessControl.FileSystemRights.FullControl,
                System.Security.AccessControl.AccessControlType.Allow));

            fileInfo.SetAccessControl(security);
            return true;
        }
        catch
        {
            // Ignore permission errors - file is still created
            return false;
        }
    }

    private static async Task<bool> CreateDatabaseUserAsync(
        string pgBinPath,
        string postgresPassword,
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        var checkResult = await RunPsqlAsync(
            pgBinPath,
            postgresPassword,
            "postgres",
            $"SELECT 1 FROM pg_roles WHERE rolname='{username}'",
            cancellationToken);

        // The app password is auto-generated fresh on every install. If the role
        // already exists from a prior run, ALTER it to the new password so the
        // connection string and the role stay in sync (avoids an auth mismatch).
        var roleExists = checkResult.Output?.Contains("1") == true;
        var sql = roleExists
            ? $"ALTER USER {username} WITH PASSWORD '{password}'"
            : $"CREATE USER {username} WITH PASSWORD '{password}'";

        await RunPsqlAsync(pgBinPath, postgresPassword, "postgres", sql, cancellationToken);

        return !roleExists;
    }

    private static async Task<bool> CreateDatabaseAsync(
        string pgBinPath,
        string postgresPassword,
        string dbName,
        string owner,
        CancellationToken cancellationToken)
    {
        var checkResult = await RunPsqlAsync(
            pgBinPath,
            postgresPassword,
            "postgres",
            $"SELECT 1 FROM pg_database WHERE datname='{dbName}'",
            cancellationToken);

        if (checkResult.Output?.Contains("1") == true)
        {
            return false;
        }

        var createResult = await RunPsqlAsync(
            pgBinPath,
            postgresPassword,
            "postgres",
            $"CREATE DATABASE {dbName} OWNER {owner}",
            cancellationToken);

        return createResult.Success;
    }

    private static async Task ConfigureDatabaseAsync(
        string pgBinPath,
        string postgresPassword,
        string dbName,
        string appUser,
        CancellationToken cancellationToken)
    {
        await RunPsqlAsync(
            pgBinPath,
            postgresPassword,
            dbName,
            $"GRANT ALL PRIVILEGES ON DATABASE {dbName} TO {appUser}",
            cancellationToken);

        await RunPsqlAsync(
            pgBinPath,
            postgresPassword,
            dbName,
            "CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\"",
            cancellationToken);
    }

    private static async Task<PsqlResult> RunPsqlAsync(
        string pgBinPath,
        string postgresPassword,
        string database,
        string sql,
        CancellationToken cancellationToken)
    {
        var psqlPath = Path.Combine(pgBinPath, "psql.exe");

        var psi = new ProcessStartInfo
        {
            FileName = psqlPath,
            // -w (--no-password) means psql will NEVER prompt for a password.
            // If PGPASSWORD is missing or wrong, psql exits immediately with
            // an error instead of hanging on stdin (which is redirected here
            // and would block forever).
            Arguments = $"-w -h 127.0.0.1 -U postgres -d {database} -tAc \"{sql}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        psi.Environment["PGPASSWORD"] = postgresPassword;

        using var process = Process.Start(psi);
        if (process == null)
        {
            return new PsqlResult { Success = false, Error = "Failed to start psql" };
        }

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        return new PsqlResult
        {
            Success = process.ExitCode == 0,
            Output = output.Trim(),
            Error = error.Trim()
        };
    }

    private async Task CreateStoreHubConfigAsync(string jwtSecret, CancellationToken cancellationToken)
    {
        var configPath = Path.Combine(Config.StoreHubInstallPath, "appsettings.json");

        var json = BuildStoreHubConfigJson(Config, jwtSecret);

        await File.WriteAllTextAsync(configPath, json, cancellationToken);

        // appsettings.json now holds the protected secrets; lock it down to
        // Administrators + LocalSystem as defense-in-depth alongside DPAPI.
        RestrictFilePermissions(configPath);
    }

    /// <summary>
    /// Removes the plaintext bootstrap admin credential from appsettings.json after
    /// the admin has been seeded. Atomic (temp + move) so a crash cannot corrupt the
    /// file that the service reads on start. Re-applies the Administrators/LocalSystem ACL.
    /// Returns false (and leaves the file intact) on any failure — non-fatal.
    /// </summary>
    public async Task<bool> RemoveInitialAdminFromConfigAsync(CancellationToken cancellationToken = default)
    {
        var configPath = Path.Combine(Config.StoreHubInstallPath, "appsettings.json");

        try
        {
            if (!File.Exists(configPath))
            {
                return false;
            }

            var original = await File.ReadAllTextAsync(configPath, cancellationToken);
            var stripped = RemoveInitialAdminNode(original);

            var tempPath = configPath + ".tmp";
            try
            {
                await File.WriteAllTextAsync(tempPath, stripped, cancellationToken);
                File.Move(tempPath, configPath, overwrite: true);
            }
            finally
            {
                // A successful Move consumes the temp file; this only runs on a
                // mid-write/move failure, where we must not leave a stray .tmp behind.
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }

            RestrictFilePermissions(configPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    internal static string BuildStoreHubConfigJson(InstallationConfig config, string jwtSecret)
    {
        var connectionString =
            $"Host=127.0.0.1;Port=5432;Database={config.DatabaseName};Username={config.AppUser};Password={config.AppPassword}";

        var configObject = new
        {
            Urls = $"http://localhost:{config.HealthCheckPort}",
            AllowedHosts = "*",
            ConnectionStrings = new
            {
                storehub_db = SecretProtector.Protect("ConnectionStrings:storehub-db", connectionString)
            },
            LocalToken = new
            {
                SecretKey = SecretProtector.Protect("LocalToken:SecretKey", jwtSecret),
                Issuer = "IndyPOS.StoreHub",
                Audience = "IndyPOS.POS",
                ExpiryHours = 12
            },
            // Section/property must match StoreIdentityOptions (SectionName "Store",
            // property "Id"). Writing "storeIdentity:storeId" leaves Store:Id unbound,
            // so StoreIdentityService silently falls back to the machine name (Bug F).
            Store = new
            {
                Id = config.StoreId,
                Type = config.StoreType.ToString()
            },
            // Plaintext by design: a human-chosen bootstrap credential consumed once by
            // SeedInitialAdminAsync, then stored only as a BCrypt hash in the DB. It is not
            // a machine-to-machine secret, so it is guarded by the appsettings.json ACL
            // (Administrators + LocalSystem) rather than DPAPI.
            InitialAdmin = new
            {
                Username = config.AdminUsername,
                Password = config.AdminPassword
            },
            CloudApi = new
            {
                BaseUrl = "",
                ClientId = "",
                ClientSecret = "",
                Scopes = "sync.write master.read"
            },
            SyncWorker = new
            {
                Enabled = false
            },
            Logging = new
            {
                LogLevel = new
                {
                    Default = "Warning",
                    Microsoft_AspNetCore = "Warning",
                    Microsoft_EntityFrameworkCore = "Warning"
                }
            }
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(configObject, options);

        json = json.Replace("\"storehub_db\"", "\"storehub-db\"");
        json = json.Replace("\"Microsoft_AspNetCore\"", "\"Microsoft.AspNetCore\"");
        json = json.Replace("\"Microsoft_EntityFrameworkCore\"", "\"Microsoft.EntityFrameworkCore\"");

        return json;
    }

    /// <summary>
    /// Returns <paramref name="json"/> with the top-level "initialAdmin" node removed.
    /// All other nodes (including the DPAPI-protected connection string and JWT key)
    /// are preserved exactly. Pure function for testability.
    /// </summary>
    internal static string RemoveInitialAdminNode(string json)
    {
        var root = JsonNode.Parse(json)?.AsObject()
            ?? throw new InvalidOperationException("appsettings.json is not a JSON object.");

        root.Remove("initialAdmin");

        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    private async Task CreateStoreConfigTemplateAsync(CancellationToken cancellationToken)
    {
        var configPath = Path.Combine(Config.ConfigDirectory, "StoreConfiguration.json");

        if (File.Exists(configPath))
        {
            return;
        }

        var configObject = new
        {
            StoreFullName = "My Store",
            StoreName = "My Store",
            StoreAddressLine1 = "123 Main Street",
            StoreAddressLine2 = "City 12345",
            StorePhoneNumber = "000-000-0000",
            PrinterName = "",
            BarcodeScannerDeviceName = "",
            SerialPortName = "COM1",
            Code = 1
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(configObject, options);

        await File.WriteAllTextAsync(configPath, json, cancellationToken);
    }

    private class PsqlResult
    {
        public bool Success { get; init; }
        public string? Output { get; init; }
        public string? Error { get; init; }
    }
}

/// <summary>
/// Result of database setup.
/// </summary>
public class DatabaseSetupResult
{
    public bool Success { get; init; }
    public string JwtSecret { get; init; } = "";
    public string? ErrorMessage { get; init; }
}
