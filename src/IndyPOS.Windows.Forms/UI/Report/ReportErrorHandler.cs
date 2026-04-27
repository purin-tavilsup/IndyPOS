using IndyPOS.Windows.Forms.UI;
using Serilog;

namespace IndyPOS.Windows.Forms.UI.Report;

internal static class ReportErrorHandler
{
    public static void Show(MessageForm messageForm, Exception exception)
    {
        Log.Warning(exception, "Unable to load report");
        messageForm.ShowDialog($"ไม่สามารถแสดงรายงานได้ Error: {exception.Message}", "ไม่สามารถแสดงรายงานได้");
    }
}
