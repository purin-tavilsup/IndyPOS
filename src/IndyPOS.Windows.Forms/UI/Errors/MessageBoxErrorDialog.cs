namespace IndyPOS.Windows.Forms.UI.Errors;

/// <summary>
/// Shows errors with a plain <see cref="System.Windows.Forms.MessageBox"/>.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately not the styled <c>MessageForm</c>. That form is a DI singleton whose
/// <c>ShowDialog</c> returns early when it is already visible, so a global handler
/// built on it would silently swallow the error during exactly the failure cascade
/// the handler exists to catch. A MessageBox stacks, always displays, and holds no
/// reusable state that can be left dirty.
/// </para>
/// <para>
/// Known limitation, accepted deliberately: <c>MessageBox.Show</c> with no owner
/// uses <c>GetActiveWindow()</c> for the calling thread. On a background thread that
/// is null, so the box comes up unowned and not topmost — while <c>MessageForm</c>
/// and <c>ChangePasswordForm</c> both set <c>TopMost = true</c>. A background-thread
/// crash while one of those is open can therefore leave this fatal message sitting
/// behind it. Marshalling to the UI thread on a dying process risks a deadlock,
/// which is worse, so this is not fixed.
/// </para>
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
