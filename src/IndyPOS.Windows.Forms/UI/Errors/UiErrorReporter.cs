using Serilog;
using Serilog.Events;

namespace IndyPOS.Windows.Forms.UI.Errors;

/// <summary>
/// Records a failure and, when the operator can act on it, shows a Thai message
/// carrying the reference code that was logged alongside it.
/// </summary>
/// <remarks>
/// <para>
/// Nothing here may throw. This is the last line of defence: an exception escaping
/// it would re-enter the handler that called it, and on a till that means a crash
/// mid-sale. Logging and presentation are therefore guarded independently, so a
/// broken dialog still leaves a log entry behind.
/// </para>
/// <para>
/// The logger is injected rather than taken from the static <c>Log</c> so tests can
/// assert on real events without mutating global state. <c>Program.Main</c> passes
/// <c>Log.Logger</c>, so this is the same instance <c>Log.CloseAndFlush()</c> flushes.
/// </para>
/// </remarks>
public sealed class UiErrorReporter
{
    private const string RecoverableCaption = "เกิดข้อผิดพลาด";
    private const string FatalCaption = "เกิดข้อผิดพลาดร้ายแรง";

    private readonly IErrorDialog _dialog;
    private readonly ILogger _logger;

    public UiErrorReporter(IErrorDialog dialog, ILogger logger)
    {
        _dialog = dialog;
        _logger = logger;
    }

    /// <summary>
    /// Writes the failure to the log and returns the reference code. Shows nothing:
    /// the fatal path needs to flush between recording and showing.
    /// </summary>
    public string Record(Exception exception, string operation, UiErrorSeverity severity)
    {
        var reference = UiErrorReference.New();

        try
        {
            _logger.Write(
                LevelFor(severity),
                exception,
                "UI failure {ErrorReference} during {Operation}",
                reference,
                operation);
        }
        catch
        {
            // A logging failure must not become the exception that takes the app
            // down. The caller still gets a reference code to show.
        }

        return reference;
    }

    /// <summary>
    /// Shows the message for a code already returned by <see cref="Record"/>.
    /// Does nothing for <see cref="UiErrorSeverity.Background"/>.
    /// </summary>
    public void Show(string reference, UiErrorSeverity severity)
    {
        if (severity == UiErrorSeverity.Background)
            return;

        try
        {
            _dialog.Show(MessageFor(severity, reference), CaptionFor(severity));
        }
        catch
        {
            // Presentation failed. The log entry is already written, which is the
            // part that matters.
        }
    }

    /// <summary>
    /// Records the failure and tells the operator in one step.
    /// </summary>
    public void ReportToUser(Exception exception, string operation, UiErrorSeverity severity) =>
        Show(Record(exception, operation, severity), severity);

    private static LogEventLevel LevelFor(UiErrorSeverity severity) => severity switch
    {
        UiErrorSeverity.Fatal => LogEventLevel.Fatal,
        UiErrorSeverity.Background => LogEventLevel.Warning,
        _ => LogEventLevel.Error
    };

    private static string CaptionFor(UiErrorSeverity severity) =>
        severity == UiErrorSeverity.Fatal ? FatalCaption : RecoverableCaption;

    private static string MessageFor(UiErrorSeverity severity, string reference) =>
        severity == UiErrorSeverity.Fatal
            ? $"โปรแกรมต้องปิดตัวลง\n\nแจ้งรหัส {reference}"
            : $"เกิดข้อผิดพลาดที่ไม่คาดคิด\n\nกรุณาลองอีกครั้ง หากยังเกิดปัญหา แจ้งรหัส {reference}";
}
