namespace IndyPOS.Windows.Forms.UI.Errors;

/// <summary>
/// Shows errors with a plain <see cref="System.Windows.Forms.MessageBox"/>.
/// </summary>
/// <remarks>
/// Deliberately not the styled <c>MessageForm</c>. That form is a DI singleton whose
/// <c>ShowDialog</c> returns early when it is already visible, so a global handler
/// built on it would silently swallow the error during exactly the failure cascade
/// the handler exists to catch. A MessageBox stacks, always displays, and holds no
/// reusable state that can be left dirty.
/// </remarks>
internal sealed class MessageBoxErrorDialog : IErrorDialog
{
    public void Show(string message, string caption) =>
        System.Windows.Forms.MessageBox.Show(
            message,
            caption,
            System.Windows.Forms.MessageBoxButtons.OK,
            System.Windows.Forms.MessageBoxIcon.Error);
}
