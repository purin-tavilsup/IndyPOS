namespace IndyPOS.Bootstrapper.Installers;

/// <summary>
/// Orchestrates the complete IndyPOS installation process.
/// </summary>
public class InstallationOrchestrator
{
    private readonly FontInstaller _fontInstaller = new();
    private readonly DotNetInstaller _dotNetInstaller = new();
    private readonly VCRedistInstaller _vcRedistInstaller = new();
    private readonly PostgresInstaller _postgresInstaller = new();
    private readonly StoreHubInstaller _storeHubInstaller = new();
    private readonly DatabaseSetup _databaseSetup = new();
    private readonly VelopackLauncher _velopackLauncher = new();

    /// <summary>
    /// Run the complete installation process.
    /// </summary>
    public async Task<InstallationResult> InstallAsync(
        InstallationConfig config,
        IProgress<InstallationProgress> progress,
        CancellationToken cancellationToken = default)
    {
        // Step 0: Install bundled UI fonts (2%). Non-fatal — the app still runs
        // with a fallback face if this fails; it just won't render as designed.
        progress.Report(InstallationProgress.Step(
            "Installing Fonts",
            "Installing IndyPOS UI fonts...",
            2));

        var fontResult = _fontInstaller.Install(
            new Progress<string>(msg => progress.Report(InstallationProgress.Log(msg))));

        progress.Report(fontResult.Success
            ? InstallationProgress.Log($"Fonts ready ({fontResult.Installed} installed, {fontResult.Skipped} already present)")
            : InstallationProgress.Error($"Warning: font installation failed: {fontResult.ErrorMessage}"));

        cancellationToken.ThrowIfCancellationRequested();

        // Step 1: Check/Install .NET Runtime (5%)
        progress.Report(InstallationProgress.Step(
            "Checking Prerequisites",
            "Checking .NET 10 Runtime...",
            5));

        progress.Report(InstallationProgress.Log("Checking for .NET 10 Runtime..."));

        var dotNetResult = await _dotNetInstaller.EnsureInstalledAsync(
            new Progress<string>(msg => progress.Report(InstallationProgress.Log(msg))),
            cancellationToken,
            config.Interactive);

        if (!dotNetResult.Success)
        {
            throw new InstallationException($".NET Runtime installation failed: {dotNetResult.ErrorMessage}");
        }

        progress.Report(InstallationProgress.Log(dotNetResult.WasInstalled
            ? ".NET 10 Runtime installed successfully"
            : ".NET 10 Runtime already installed"));

        cancellationToken.ThrowIfCancellationRequested();

        // Step 1b: Check/Install Visual C++ Redistributable (5-10%)
        // Must run BEFORE PostgreSQL: initdb.exe depends on vcruntime140.dll /
        // msvcp140.dll. Missing on clean Windows 11 → initdb fails with
        // -1073741515 (STATUS_DLL_NOT_FOUND). PostgresInstaller invokes EDB with
        // --install_runtimes 0, so the bootstrapper owns this prerequisite.
        progress.Report(InstallationProgress.Step(
            "Checking Prerequisites",
            "Checking Visual C++ Redistributable...",
            7));

        progress.Report(InstallationProgress.Log("Checking for Visual C++ Redistributable (x64)..."));

        var vcRedistResult = await _vcRedistInstaller.EnsureInstalledAsync(
            new Progress<DownloadProgress>(p =>
            {
                var pct = 5 + (int)(p.Percentage * 5); // 5-10%
                progress.Report(InstallationProgress.Step(
                    "Checking Prerequisites",
                    p.StatusMessage,
                    pct));
            }),
            cancellationToken);

        if (!vcRedistResult.Success)
        {
            throw new InstallationException(
                $"Visual C++ Redistributable installation failed: {vcRedistResult.ErrorMessage}");
        }

        progress.Report(InstallationProgress.Log(vcRedistResult.WasInstalled
            ? "Visual C++ Redistributable installed successfully"
            : "Visual C++ Redistributable already installed"));

        cancellationToken.ThrowIfCancellationRequested();

        // Step 2: Check/Install PostgreSQL (10-40%)
        progress.Report(InstallationProgress.Step(
            "Installing PostgreSQL",
            "Checking for PostgreSQL 18...",
            10));

        progress.Report(InstallationProgress.Log("Checking for PostgreSQL 18..."));

        var pgResult = await _postgresInstaller.EnsureInstalledAsync(
            new Progress<DownloadProgress>(p =>
            {
                var pct = 10 + (int)(p.Percentage * 0.3); // 10-40%
                progress.Report(InstallationProgress.Step(
                    "Installing PostgreSQL",
                    p.StatusMessage,
                    pct));
            }),
            cancellationToken);

        if (!pgResult.Success)
        {
            throw new InstallationException($"PostgreSQL installation failed: {pgResult.ErrorMessage}");
        }

        config.PostgresBinPath = pgResult.BinPath;
        progress.Report(InstallationProgress.Log(pgResult.WasInstalled
            ? "PostgreSQL 18 installed successfully"
            : "PostgreSQL 18 already installed"));

        cancellationToken.ThrowIfCancellationRequested();

        // Step 3: Install StoreHub Service (40-55%)
        // Deliberately BEFORE database setup: StoreHubInstaller extracts the
        // zip which includes a base appsettings.json with safe defaults.
        // DatabaseSetup then writes the real appsettings.json on top in step 4,
        // overwriting the shipped defaults with production values. This is
        // cleaner than the old order, which needed a skip-list workaround to
        // prevent extraction from clobbering the real config.
        progress.Report(InstallationProgress.Step(
            "Installing StoreHub Service",
            "Deploying StoreHub API service...",
            45));

        progress.Report(InstallationProgress.Log("Installing StoreHub service..."));

        var storeHubResult = await _storeHubInstaller.InstallAsync(
            config,
            new Progress<string>(msg => progress.Report(InstallationProgress.Log(msg))),
            cancellationToken);

        if (!storeHubResult.Success)
        {
            throw new InstallationException($"StoreHub installation failed: {storeHubResult.ErrorMessage}");
        }

        progress.Report(InstallationProgress.Log("StoreHub service installed"));

        cancellationToken.ThrowIfCancellationRequested();

        // Step 4: Setup Database (55-70%)
        progress.Report(InstallationProgress.Step(
            "Configuring Database",
            "Creating database and user...",
            60));

        progress.Report(InstallationProgress.Log($"Creating database '{config.DatabaseName}'..."));

        var dbResult = await _databaseSetup.SetupAsync(
            config,
            pgResult.SuperuserPassword,
            new Progress<string>(msg => progress.Report(InstallationProgress.Log(msg))),
            cancellationToken);

        if (!dbResult.Success)
        {
            throw new InstallationException($"Database setup failed: {dbResult.ErrorMessage}");
        }

        progress.Report(InstallationProgress.Log("Database configured successfully"));

        cancellationToken.ThrowIfCancellationRequested();

        // Step 4b: Provision schema (apply migrations + seed admin) as a one-shot
        // console run BEFORE the service starts, so the first service start is
        // instant and can't overrun the 30s SCM start timeout on a fresh DB.
        progress.Report(InstallationProgress.Step(
            "Configuring Database",
            "Provisioning database schema...",
            70));

        progress.Report(InstallationProgress.Log("Provisioning database schema..."));

        var provisionResult = await _storeHubInstaller.ProvisionDatabaseAsync(
            new Progress<string>(msg => progress.Report(InstallationProgress.Log(msg))),
            cancellationToken);

        if (!provisionResult.Success)
        {
            throw new InstallationException($"Database provisioning failed: {provisionResult.ErrorMessage}");
        }

        progress.Report(InstallationProgress.Log("Database schema provisioned"));

        // Remove the plaintext bootstrap admin credential now that it is seeded.
        // Non-fatal: the DB is already provisioned; a failure only leaves the
        // ACL-locked plaintext behind and is retryable on re-run.
        progress.Report(InstallationProgress.Log("Removing bootstrap credential from configuration..."));
        var cleared = await _databaseSetup.RemoveInitialAdminFromConfigAsync(cancellationToken);
        progress.Report(cleared
            ? InstallationProgress.Log("Bootstrap credential removed from configuration")
            : InstallationProgress.Error("Warning: could not remove bootstrap credential from appsettings.json"));

        cancellationToken.ThrowIfCancellationRequested();

        // Step 5: Install WinForms via Velopack (70-90%)
        progress.Report(InstallationProgress.Step(
            "Installing POS Application",
            "Installing IndyPOS application...",
            75));

        progress.Report(InstallationProgress.Log("Installing POS application via Velopack..."));

        var winFormsResult = await _velopackLauncher.InstallAsync(
            config,
            new Progress<int>(pct =>
            {
                var adjustedPct = 75 + (int)(pct * 0.15); // 75-90%
                progress.Report(InstallationProgress.Step(
                    "Installing POS Application",
                    "Installing IndyPOS application...",
                    adjustedPct));
            }),
            cancellationToken);

        if (!winFormsResult.Success)
        {
            throw new InstallationException($"WinForms installation failed: {winFormsResult.ErrorMessage}");
        }

        progress.Report(InstallationProgress.Log("POS application installed"));

        cancellationToken.ThrowIfCancellationRequested();

        // Step 6: Start Services (90-100%)
        progress.Report(InstallationProgress.Step(
            "Starting Services",
            "Starting StoreHub service...",
            95));

        progress.Report(InstallationProgress.Log("Starting StoreHub service..."));

        var startResult = await _storeHubInstaller.StartServiceAsync(cancellationToken);

        if (!startResult.Success)
        {
            progress.Report(InstallationProgress.Error($"Warning: Could not start service: {startResult.ErrorMessage}"));
            // Don't throw - service can be started manually
        }
        else
        {
            progress.Report(InstallationProgress.Log("StoreHub service started"));
        }

        // Verify health
        progress.Report(InstallationProgress.Log("Verifying StoreHub health..."));
        var healthOk = await HealthProbe.IsReadyAsync(config.HealthCheckPort, cancellationToken: cancellationToken);

        if (healthOk)
        {
            progress.Report(InstallationProgress.Log("StoreHub is healthy and responding"));
        }
        else
        {
            progress.Report(InstallationProgress.Error("Warning: StoreHub health check failed"));
        }

        // Step 7: Write install manifest (post-health-check so a partial install
        // doesn't leave a misleading manifest claiming success).
        progress.Report(InstallationProgress.Log("Writing install manifest..."));
        try
        {
            await InstallManifestWriter.WriteAsync(config, cancellationToken);
            progress.Report(InstallationProgress.Log(
                $"Manifest written: {Path.Combine(config.SystemRoot, InstallManifestWriter.ManifestFileName)}"));
        }
        catch (Exception ex)
        {
            // Non-fatal — install succeeded; manifest is a nicety for teardown scripts.
            progress.Report(InstallationProgress.Error(
                $"Warning: Could not write install manifest: {ex.Message}"));
        }

        progress.Report(InstallationProgress.Step(
            "Installation Complete",
            "IndyPOS has been installed successfully!",
            100));

        return new InstallationResult
        {
            AdminSeeded = provisionResult.AdminSeeded,
            ServiceStarted = startResult.Success,
            HealthOk = healthOk
        };
    }

}

/// <summary>
/// Exception thrown when installation fails.
/// </summary>
public class InstallationException : Exception
{
    public InstallationException(string message) : base(message) { }
    public InstallationException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>Outcome of a successful installation, surfaced to the wizard's finish screen.</summary>
public class InstallationResult
{
    public bool AdminSeeded { get; init; }
    public bool ServiceStarted { get; init; }
    public bool HealthOk { get; init; }
}
