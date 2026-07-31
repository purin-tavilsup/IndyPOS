namespace IndyPOS.Windows.Forms.UI.Errors;

/// <summary>
/// Attaches a <see cref="UiErrorReporter"/> to the three channels through which an
/// unhandled exception can reach the framework. Without this the app shows the
/// default English "Unhandled exception" dialog — offering an operator a Quit button
/// on a till — and writes nothing to the log.
/// </summary>
/// <remarks>
/// Kept out of <c>Program.cs</c> because that file is <c>[ExcludeFromCodeCoverage]</c>.
/// This class is a thin adapter by design: all the logic lives in the reporter, which
/// is unit-tested.
/// </remarks>
internal static class GlobalErrorHandler
{
    public static void Wire(UiErrorReporter reporter)
    {
        ArgumentNullException.ThrowIfNull(reporter);

        // Route UI-thread exceptions to ThreadException rather than the default
        // dialog. Set unconditionally, including under a debugger: differing
        // dev/production behaviour is its own source of bugs, and a developer who
        // wants to stop at the throw site can use first-chance exception settings.
        System.Windows.Forms.Application.SetUnhandledExceptionMode(
            System.Windows.Forms.UnhandledExceptionMode.CatchException);

        System.Windows.Forms.Application.ThreadException += (_, args) =>
            reporter.ReportToUser(args.Exception, "UI thread", UiErrorSeverity.Recoverable);

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var exception = args.ExceptionObject as Exception
                            ?? new InvalidOperationException(
                                $"Non-exception object thrown: {args.ExceptionObject}");

            // Record, then show. The process is going down, so getting the entry
            // onto disk must not depend on a MessageBox call succeeding — which is
            // why Record and Show stay two separate calls rather than ReportToUser.
            var reference = reporter.Record(exception, "unhandled domain exception", UiErrorSeverity.Fatal);

            // No CloseAndFlush here. Program.cs wires the file sink unbuffered
            // (WriteTo.File defaults `buffered` to false), so the Fatal entry above
            // is already durable on disk before this line runs. Flushing here would
            // dispose the very Log.Logger instance `reporter` still holds — and that
            // any later failure on this thread still needs — for no durability gain.
            reporter.Show(reference, UiErrorSeverity.Fatal);
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            // No dialog: this fires at GC finalization, arbitrarily long after the
            // fact, so it would name an operation the operator has moved on from.
            reporter.Record(args.Exception, "unobserved task", UiErrorSeverity.Background);

            args.SetObserved();
        };
    }
}
