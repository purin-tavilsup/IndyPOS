using IndyPOS.Windows.Forms.UI;
using IndyPOS.Windows.Forms.UI.Errors;
using Serilog;

namespace IndyPOS.Windows.Forms.UI.Report;

/// <summary>
/// Presents a failed report load in the app's styled dialog, and records it with the
/// same reference code the operator sees — so a support call about a report narrows
/// to one log entry, exactly as it does for errors caught by the global handler.
/// </summary>
internal static class ReportErrorHandler
{
    public static void Show(MessageForm messageForm, Exception exception)
    {
        var reference = UiErrorReference.New();

        Log.Warning(exception, "Unable to load report {ErrorReference}", reference);

        messageForm.ShowDialog(
            $"ไม่สามารถแสดงรายงานได้\n\nแจ้งรหัส {reference}",
            "ไม่สามารถแสดงรายงานได้");
    }
}
