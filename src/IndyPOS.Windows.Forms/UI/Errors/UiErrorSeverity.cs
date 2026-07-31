namespace IndyPOS.Windows.Forms.UI.Errors;

/// <summary>
/// How a failure is recorded, and whether <see cref="UiErrorReporter"/> tells the
/// operator about it.
/// </summary>
public enum UiErrorSeverity
{
    /// <summary>
    /// The operation failed but the application carries on. Logged as Error and
    /// shown to the operator.
    /// </summary>
    Recoverable,

    /// <summary>
    /// The process is going down. Logged as Fatal and shown to the operator.
    /// </summary>
    Fatal,

    /// <summary>
    /// Recorded but never shown by the reporter — either nobody was waiting on the
    /// result (an unobserved task, surfaced at GC time long after the fact), or the
    /// caller presents its own dialog. Logged as Warning.
    /// </summary>
    Background
}
