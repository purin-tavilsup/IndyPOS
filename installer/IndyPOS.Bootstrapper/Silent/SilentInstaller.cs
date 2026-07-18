using IndyPOS.Bootstrapper.Installers;

namespace IndyPOS.Bootstrapper.Silent;

/// <summary>
/// Headless entry point for `--silent --store-id <ID>`. Reuses
/// InstallationOrchestrator unchanged; the durable log file is the authoritative
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
        var config = new InstallationConfig { StoreId = options.StoreId };

        string logPath;
        TextWriter writer;
        try
        {
            (logPath, writer) = OpenLog(config);
        }
        catch (Exception ex)
        {
            var message = SecretScrubber.Scrub(ex.Message);
            return Emit(new InstallFailed(message), logger: null);
        }

        var logger = new SilentInstallLogger(writer);

        var outcome = RunInstall(config, options, logger);
        var exit = Emit(outcome, logger);

        // Dispose (flush + close the file) BEFORE copying to install-latest.log.
        logger.Dispose();
        CopyLatest(config, logPath);
        return exit;
    }

    private static SilentOutcome RunInstall(
        InstallationConfig config, SilentInstallOptions options, SilentInstallLogger logger)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(options.TimeoutMinutes));

        InstallationResult result;
        try
        {
            result = new InstallationOrchestrator()
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
