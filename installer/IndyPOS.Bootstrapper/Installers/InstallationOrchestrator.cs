namespace IndyPOS.Bootstrapper.Installers;

/// <summary>
/// Orchestrates the complete IndyPOS installation process.
/// </summary>
public class InstallationOrchestrator
{
    private readonly DotNetInstaller _dotNetInstaller = new();
    private readonly PostgresInstaller _postgresInstaller = new();
    private readonly StoreHubInstaller _storeHubInstaller = new();
    private readonly DatabaseSetup _databaseSetup = new();
    private readonly VelopackLauncher _velopackLauncher = new();

    /// <summary>
    /// Run the complete installation process.
    /// </summary>
    public async Task InstallAsync(
        InstallationConfig config,
        IProgress<InstallationProgress> progress,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Check/Install .NET Runtime (5%)
        progress.Report(InstallationProgress.Step(
            "Checking Prerequisites",
            "Checking .NET 10 Runtime...",
            5));

        progress.Report(InstallationProgress.Log("Checking for .NET 10 Runtime..."));

        var dotNetResult = await _dotNetInstaller.EnsureInstalledAsync(
            new Progress<string>(msg => progress.Report(InstallationProgress.Log(msg))),
            cancellationToken);

        if (!dotNetResult.Success)
        {
            throw new InstallationException($".NET Runtime installation failed: {dotNetResult.ErrorMessage}");
        }

        progress.Report(InstallationProgress.Log(dotNetResult.WasInstalled
            ? ".NET 10 Runtime installed successfully"
            : ".NET 10 Runtime already installed"));

        cancellationToken.ThrowIfCancellationRequested();

        // Step 2: Check/Install PostgreSQL (5-40%)
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

        // Step 3: Setup Database (40-55%)
        progress.Report(InstallationProgress.Step(
            "Configuring Database",
            "Creating database and user...",
            45));

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

        // Step 4: Install StoreHub Service (55-70%)
        progress.Report(InstallationProgress.Step(
            "Installing StoreHub Service",
            "Deploying StoreHub API service...",
            60));

        progress.Report(InstallationProgress.Log("Installing StoreHub service..."));

        var storeHubResult = await _storeHubInstaller.InstallAsync(
            config,
            dbResult.JwtSecret,
            new Progress<string>(msg => progress.Report(InstallationProgress.Log(msg))),
            cancellationToken);

        if (!storeHubResult.Success)
        {
            throw new InstallationException($"StoreHub installation failed: {storeHubResult.ErrorMessage}");
        }

        progress.Report(InstallationProgress.Log("StoreHub service installed"));

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
        var healthOk = await VerifyStoreHubHealthAsync(config.HealthCheckPort, cancellationToken);

        if (healthOk)
        {
            progress.Report(InstallationProgress.Log("StoreHub is healthy and responding"));
        }
        else
        {
            progress.Report(InstallationProgress.Error("Warning: StoreHub health check failed"));
        }

        progress.Report(InstallationProgress.Step(
            "Installation Complete",
            "IndyPOS has been installed successfully!",
            100));
    }

    private static async Task<bool> VerifyStoreHubHealthAsync(int port, CancellationToken cancellationToken)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var healthUrl = $"http://localhost:{port}/health/live";

        for (var i = 0; i < 5; i++)
        {
            try
            {
                var response = await client.GetAsync(healthUrl, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            catch
            {
                // Retry
            }

            await Task.Delay(2000, cancellationToken);
        }

        return false;
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
