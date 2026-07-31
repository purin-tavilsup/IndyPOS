using Serilog.Core;
using Serilog.Events;

namespace IndyPOS.Windows.Forms.Tests.UI.Errors;

/// <summary>
/// Collects real Serilog events so tests can assert on levels and structured
/// properties without touching the global <c>Log.Logger</c>.
/// </summary>
internal sealed class CapturingSink : ILogEventSink
{
    public List<LogEvent> Events { get; } = new();

    public void Emit(LogEvent logEvent) => Events.Add(logEvent);

    public string? PropertyValue(string name) =>
        Events.FirstOrDefault()?.Properties.TryGetValue(name, out var value) == true
            ? value.ToString().Trim('"')
            : null;
}
