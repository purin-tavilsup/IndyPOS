using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace IndyPOS.Bootstrapper.Installers;

/// <summary>
/// Handles database creation and configuration.
/// </summary>
public class DatabaseSetup
{
    private const string KeysDirectory = @"C:\ProgramData\IndyPOS\keys";
    private const string ConfigDirectory = @"C:\ProgramData\IndyPOS\Config";
    private const string LogsDirectory = @"C:\ProgramData\IndyPOS\logs";
    private const string BackupsDirectory = @"C:\ProgramData\IndyPOS\backups";
    private const string StoreHubDirectory = @"C:\Program Files\IndyPOS\StoreHub";

    /// <summary>
    /// Set up the database, user, and configuration files.
    /// </summary>
    public async Task<DatabaseSetupResult> SetupAsync(
        InstallationConfig config,
        string postgresPassword,
        IProgress<string>? log = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Step 1: Create directories
            log?.Report("Creating system directories...");
            CreateDirectories();

            // Step 2: Generate JWT secret
            log?.Report("Generating JWT secret key...");
            var jwtSecret = await GenerateJwtSecretAsync(cancellationToken);

            // Step 3: Create database user and database
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

            // Step 4: Grant permissions and create extensions
            log?.Report("Configuring database permissions...");
            await ConfigureDatabaseAsync(
                config.PostgresBinPath,
                postgresPassword,
                config.DatabaseName,
                config.AppUser,
                cancellationToken);

            // Step 5: Create StoreHub configuration file
            log?.Report("Creating StoreHub configuration...");
            await CreateStoreHubConfigAsync(config, jwtSecret, cancellationToken);

            // Step 6: Create Store configuration template
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

    private static void CreateDirectories()
    {
        var directories = new[]
        {
            ConfigDirectory,
            KeysDirectory,
            LogsDirectory,
            BackupsDirectory,
            StoreHubDirectory
        };

        foreach (var dir in directories)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
    }

    private static async Task<string> GenerateJwtSecretAsync(CancellationToken cancellationToken)
    {
        var keyPath = Path.Combine(KeysDirectory, "storehub.key");

        if (File.Exists(keyPath))
        {
            return await File.ReadAllTextAsync(keyPath, cancellationToken);
        }

        // Generate 64-byte (512-bit) random key
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        var jwtSecret = Convert.ToBase64String(bytes);

        await File.WriteAllTextAsync(keyPath, jwtSecret, cancellationToken);

        // Restrict file permissions (only Administrators and SYSTEM)
        RestrictFilePermissions(keyPath);

        return jwtSecret;
    }

    private static void RestrictFilePermissions(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            var security = fileInfo.GetAccessControl();

            // Remove inheritance
            security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

            // Clear existing rules
            var rules = security.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
            foreach (System.Security.AccessControl.FileSystemAccessRule rule in rules)
            {
                security.RemoveAccessRule(rule);
            }

            // Add Administrators full control
            security.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(
                new System.Security.Principal.SecurityIdentifier(
                    System.Security.Principal.WellKnownSidType.BuiltinAdministratorsSid, null),
                System.Security.AccessControl.FileSystemRights.FullControl,
                System.Security.AccessControl.AccessControlType.Allow));

            // Add SYSTEM full control
            security.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(
                new System.Security.Principal.SecurityIdentifier(
                    System.Security.Principal.WellKnownSidType.LocalSystemSid, null),
                System.Security.AccessControl.FileSystemRights.FullControl,
                System.Security.AccessControl.AccessControlType.Allow));

            fileInfo.SetAccessControl(security);
        }
        catch
        {
            // Ignore permission errors - file is still created
        }
    }

    private static async Task<bool> CreateDatabaseUserAsync(
        string pgBinPath,
        string postgresPassword,
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        // Check if user exists
        var checkResult = await RunPsqlAsync(
            pgBinPath,
            postgresPassword,
            "postgres",
            $"SELECT 1 FROM pg_roles WHERE rolname='{username}'",
            cancellationToken);

        if (checkResult.Output?.Contains("1") == true)
        {
            return false; // User already exists
        }

        // Create user
        var createResult = await RunPsqlAsync(
            pgBinPath,
            postgresPassword,
            "postgres",
            $"CREATE USER {username} WITH PASSWORD '{password}'",
            cancellationToken);

        return createResult.Success;
    }

    private static async Task<bool> CreateDatabaseAsync(
        string pgBinPath,
        string postgresPassword,
        string dbName,
        string owner,
        CancellationToken cancellationToken)
    {
        // Check if database exists
        var checkResult = await RunPsqlAsync(
            pgBinPath,
            postgresPassword,
            "postgres",
            $"SELECT 1 FROM pg_database WHERE datname='{dbName}'",
            cancellationToken);

        if (checkResult.Output?.Contains("1") == true)
        {
            return false; // Database already exists
        }

        // Create database
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
        // Grant privileges
        await RunPsqlAsync(
            pgBinPath,
            postgresPassword,
            dbName,
            $"GRANT ALL PRIVILEGES ON DATABASE {dbName} TO {appUser}",
            cancellationToken);

        // Create uuid-ossp extension
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
            Arguments = $"-h 127.0.0.1 -U postgres -d {database} -tAc \"{sql}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        // Set password via environment variable
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

    private static async Task CreateStoreHubConfigAsync(
        InstallationConfig config,
        string jwtSecret,
        CancellationToken cancellationToken)
    {
        var configPath = Path.Combine(StoreHubDirectory, "appsettings.Production.json");

        var configObject = new
        {
            ConnectionStrings = new
            {
                storehub_db = $"Host=127.0.0.1;Port=5432;Database={config.DatabaseName};Username={config.AppUser};Password={config.AppPassword}"
            },
            LocalToken = new
            {
                SecretKey = jwtSecret,
                Issuer = "IndyPOS.StoreHub",
                Audience = "IndyPOS.POS",
                ExpiryHours = 12
            },
            StoreIdentity = new
            {
                StoreId = config.StoreId
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

        // Fix the property name for connection string (has hyphen)
        json = json.Replace("\"storehub_db\"", "\"storehub-db\"");
        json = json.Replace("\"Microsoft_AspNetCore\"", "\"Microsoft.AspNetCore\"");
        json = json.Replace("\"Microsoft_EntityFrameworkCore\"", "\"Microsoft.EntityFrameworkCore\"");

        await File.WriteAllTextAsync(configPath, json, cancellationToken);
    }

    private static async Task CreateStoreConfigTemplateAsync(CancellationToken cancellationToken)
    {
        var configPath = Path.Combine(ConfigDirectory, "StoreConfiguration.json");

        if (File.Exists(configPath))
        {
            return; // Don't overwrite existing config
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
