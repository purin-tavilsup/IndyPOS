using System.Diagnostics;

namespace IndyPOS.Bootstrapper.Upgrade;

public sealed record ProcessRunResult(int ExitCode, string StandardOutput, string StandardError);

/// <summary>Seam over external tools (pg_dump, Velopack Setup.exe) so the sequence is testable.</summary>
public interface IProcessRunner
{
    Task<ProcessRunResult> RunAsync(
        string fileName,
        string arguments,
        IReadOnlyDictionary<string, string>? environment,
        CancellationToken cancellationToken = default);
}

public sealed class ExternalProcessRunner : IProcessRunner
{
    public async Task<ProcessRunResult> RunAsync(
        string fileName,
        string arguments,
        IReadOnlyDictionary<string, string>? environment,
        CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        if (environment is not null)
        {
            foreach (var (key, value) in environment)
            {
                psi.Environment[key] = value;
            }
        }

        using var process = new Process { StartInfo = psi };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await Task.WhenAll(stdoutTask, stderrTask);
        await process.WaitForExitAsync(cancellationToken);

        return new ProcessRunResult(process.ExitCode, await stdoutTask, await stderrTask);
    }
}
