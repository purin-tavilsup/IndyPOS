using IndyPOS.Bootstrapper.Installers;

namespace IndyPOS.Bootstrapper.Silent;

/// <summary>
/// Thread-safe progress sink for the silent installer. Each line goes to the
/// durable log file (authoritative) and, best-effort, stdout. Report runs inline
/// on threadpool continuation threads, so all writes are serialized under a lock
/// and flushed per line — an early crash still leaves a diagnosable log.
/// </summary>
public sealed class SilentInstallLogger(TextWriter file)
    : IProgress<InstallationProgress>, IProgress<string>, IDisposable
{
    private readonly object _gate = new();

    // The upgrade's step units log plain strings (WindowsUpgradeSteps), while the
    // orchestrators report structured progress. One sink serves both.
    public void Report(string value) => WriteLine(value);

    public void Report(InstallationProgress value)
    {
        if (!string.IsNullOrEmpty(value.LogMessage))
        {
            WriteLine((value.IsError ? "WARNING: " : string.Empty) + value.LogMessage);
        }
        else if (!string.IsNullOrEmpty(value.StepName))
        {
            WriteLine($"[{value.StepName}] {value.StatusMessage}");
        }
    }

    public void WriteLine(string line) => Emit($"[{DateTime.Now:HH:mm:ss}] {line}");

    public void WriteMarker(string marker) => Emit(marker);

    private void Emit(string text)
    {
        lock (_gate)
        {
            file.WriteLine(text);
            file.Flush();
            try { Console.Out.WriteLine(text); } catch { /* no console attached */ }
        }
    }

    public void Dispose()
    {
        lock (_gate) { file.Dispose(); }
    }
}
