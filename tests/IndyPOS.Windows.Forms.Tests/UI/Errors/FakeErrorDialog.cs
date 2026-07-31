using IndyPOS.Windows.Forms.UI.Errors;

namespace IndyPOS.Windows.Forms.Tests.UI.Errors;

/// <summary>
/// Records what the reporter asked to display. Optionally throws, to prove the
/// reporter survives a broken dialog.
/// </summary>
internal sealed class FakeErrorDialog : IErrorDialog
{
    private readonly bool _throwOnShow;

    public FakeErrorDialog(bool throwOnShow = false) => _throwOnShow = throwOnShow;

    public List<(string Message, string Caption)> Shown { get; } = new();

    public void Show(string message, string caption)
    {
        Shown.Add((message, caption));

        if (_throwOnShow)
            throw new InvalidOperationException("dialog is broken");
    }
}
