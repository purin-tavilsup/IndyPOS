using IndyPOS.Bootstrapper.Installers;
using IndyPOS.Bootstrapper.Upgrade;

namespace IndyPOS.Bootstrapper.Silent;

/// <summary>
/// Headless entry point for `--silent --store-id <ID>`. Reuses
/// FreshInstallOrchestrator unchanged; the durable log file is the authoritative
/// result/marker channel and the exit code is the automation contract.
/// </summary>
public static class SilentInstaller
{
    public static int Run(ParseResult parse)
    {
        ConsoleAttach.TryAttach();

        if (parse.Status == ParseStatus.UsageError)
            return Emit(new UsageErrorOutcome(parse.ErrorMessage ?? "Invalid arguments."), logger: null);

        if (!Elevation.IsElevated())
            return Emit(new NotElevatedOutcome(), logger: null);

        var options = parse.Options!;

        // Detect first, with a placeholder store id: only the computed paths are needed to
        // probe the machine, and StoreId is required on the record.
        var probeConfig = new InstallationConfig
        {
            StoreId = options.StoreId ?? "pending",
            StoreType = options.StoreType,
            Interactive = false
        };

        var detected = InstallModeDetector.Detect(
            new WindowsInstallProbe(probeConfig), probeConfig.StoreHubInstallPath);

        string logPath;
        TextWriter writer;
        try
        {
            (logPath, writer) = OpenLog(probeConfig);
        }
        catch (Exception ex)
        {
            var message = SecretScrubber.Scrub(ex.Message);
            return Emit(new InstallFailed(message), logger: null);
        }

        var logger = new SilentInstallLogger(writer);
        logger.WriteMarker(SilentOutcomeMapper.ModeMarker(detected.Mode));

        var outcome = detected.Mode switch
        {
            InstallMode.Unusable => new UnusableInstall(detected.Reason),
            InstallMode.Upgrade => RunUpgrade(detected, options, logger),
            _ => BuildFreshConfig(options) is { } fresh
                ? RunFreshInstall(fresh, options, logger)
                : new UsageErrorOutcome("--silent requires --store-id <ID> for a fresh install.")
        };

        var exit = Emit(outcome, logger);

        // Dispose (flush + close the file) BEFORE copying to install-latest.log.
        logger.Dispose();
        CopyLatest(probeConfig, logPath);
        return exit;
    }

    // A fresh install still requires an explicit store id: there is nothing to adopt.
    private static InstallationConfig? BuildFreshConfig(SilentInstallOptions options) =>
        options.StoreId is null
            ? null
            : new InstallationConfig
            {
                StoreId = options.StoreId,
                StoreType = options.StoreType,
                Interactive = false
            };

    private static SilentOutcome RunUpgrade(
        DetectedInstall detected, SilentInstallOptions options, SilentInstallLogger logger)
    {
        // Rewriting store identity would orphan every sale recorded under the old id.
        if (options.StoreId is not null &&
            !string.Equals(options.StoreId, detected.StoreId, StringComparison.Ordinal))
        {
            return new UnusableInstall(
                $"--store-id '{options.StoreId}' does not match the installed store " +
                $"'{detected.StoreId}'. Re-run without --store-id to upgrade this store.");
        }

        var config = new InstallationConfig
        {
            StoreId = detected.StoreId!,
            StoreType = detected.StoreType!.Value,
            Interactive = false
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(options.TimeoutMinutes));

        try
        {
            var steps = new WindowsUpgradeSteps(config, logger);
            return new UpgradeOrchestrator(steps, options.SimulateFailure)
                .RunAsync(detected, config, logger, cts.Token)
                .GetAwaiter().GetResult();
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            logger.WriteLine("ERROR: upgrade timed out.");
            return new InstallTimedOut();
        }
        catch (Exception ex)
        {
            var message = SecretScrubber.Scrub(ex.Message);
            logger.WriteLine("ERROR: " + message);
            return new UpgradeFailed(message, RolledBack: false, ServiceStarted: false,
                HealthOk: false, BackupDir: null);
        }
    }

    private static SilentOutcome RunFreshInstall(
        InstallationConfig config, SilentInstallOptions options, SilentInstallLogger logger)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(options.TimeoutMinutes));

        InstallationResult result;
        try
        {
            result = new FreshInstallOrchestrator()
                .InstallAsync(config, logger, cts.Token)
                .GetAwaiter().GetResult();
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            logger.WriteLine("ERROR: installation timed out.");
            return new InstallTimedOut();
        }
        catch (Exception ex)
        {
            var message = SecretScrubber.Scrub(ex.Message);
            logger.WriteLine("ERROR: " + message);
            return new InstallFailed(message);
        }

        if (!result.AdminSeeded)
            return new InstallSucceeded(false, null, false, result.ServiceStarted, result.HealthOk);

        var (path, locked) = AdminCredentialFile.Write(config);
        return new InstallSucceeded(true, path, locked, result.ServiceStarted, result.HealthOk);
    }

    private static int Emit(SilentOutcome outcome, SilentInstallLogger? logger)
    {
        var (exit, markers) = SilentOutcomeMapper.Map(outcome);
        foreach (var marker in markers)
        {
            if (logger is not null) logger.WriteMarker(marker);
            else TryConsole(marker);
        }
        return exit;
    }

    private static (string LogPath, TextWriter Writer) OpenLog(InstallationConfig config)
    {
        Directory.CreateDirectory(config.LogsDirectory);
        var logPath = Path.Combine(
            config.LogsDirectory, $"install-{DateTime.Now:yyyyMMdd-HHmmss}-{Environment.ProcessId}.log");
        File.WriteAllText(logPath, string.Empty);
        DatabaseSetup.TryRestrictFilePermissions(logPath);

        var stream = new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.Read);
        return (logPath, new StreamWriter(stream) { AutoFlush = true });
    }

    private static void CopyLatest(InstallationConfig config, string logPath)
    {
        try
        {
            var latest = Path.Combine(config.LogsDirectory, "install-latest.log");
            File.Copy(logPath, latest, overwrite: true);
            DatabaseSetup.TryRestrictFilePermissions(latest);
        }
        catch { /* best-effort convenience copy */ }
    }

    private static void TryConsole(string text)
    {
        try { Console.Out.WriteLine(text); } catch { /* no console attached */ }
    }
}
