namespace IndyPOS.Windows.Forms.UI.Errors;

/// <summary>
/// Shows an error to the operator. This seam exists so <see cref="UiErrorReporter"/>
/// can be tested without a UI.
/// </summary>
public interface IErrorDialog
{
    void Show(string message, string caption);
}
