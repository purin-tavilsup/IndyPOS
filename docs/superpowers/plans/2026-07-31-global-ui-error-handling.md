# Global UI Error Handling Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** No failure in the WinForms app can show the default English crash dialog or go unrecorded; the operator sees a Thai message carrying a short reference code that also lands in the log.

**Architecture:** A `UiErrorReporter` owns logging and presentation, behind an `IErrorDialog` seam so it is testable without a UI. A `GlobalErrorHandler` attaches it to the three framework channels (`Application.ThreadException`, `AppDomain.UnhandledException`, `TaskScheduler.UnobservedTaskException`), wired from `Program.Main` before `Application.Run`. The existing `ReportErrorHandler` is refolded onto the shared reference-code format so report failures gain a code too.

**Tech Stack:** .NET 10 WinForms, Serilog 4.3.1 (compact JSON to file), xunit 2.9.3, FluentAssertions 8.8.0.

**Spec:** `docs/superpowers/specs/2026-07-31-dialog-error-handling-design.md`

## Global Constraints

- **Namespace clash — fully qualify `System.Windows.Forms`.** The project's root namespace is `IndyPOS.Windows.Forms`, so inside it a bare `Windows.Forms.X` resolves to the project's own namespace and fails to compile. Existing code works around this (`Machine.cs:160` writes `System.Windows.Forms.Application.Run`, `MessageForm.cs` uses a `using DialogResult = ...` alias). Always write `System.Windows.Forms.MessageBox`, `System.Windows.Forms.Application`, etc.
- **File-scoped namespaces** (`namespace Foo;`), matching the rest of the project.
- **New code lives in** `src/IndyPOS.Windows.Forms/UI/Errors/`, namespace `IndyPOS.Windows.Forms.UI.Errors`.
- **`UiErrorReporter` must never throw.** It is the last line of defence; an exception escaping it re-enters the handler that called it. Every step is individually guarded.
- **Thai copy is fixed** (approved verbatim):
  - Recoverable caption: `เกิดข้อผิดพลาด`
  - Recoverable body: `เกิดข้อผิดพลาดที่ไม่คาดคิด\n\nกรุณาลองอีกครั้ง หากยังเกิดปัญหา แจ้งรหัส {reference}`
  - Fatal caption: `เกิดข้อผิดพลาดร้ายแรง`
  - Fatal body: `โปรแกรมต้องปิดตัวลง\n\nแจ้งรหัส {reference}`
- **Reference code format:** `ERR-` + 4 uppercase hex characters. Logged as a structured `ErrorReference` property.
- **The operation name is never shown to the operator** — log only.
- **`MessageBox`, not `MessageForm`,** for the global net. `MessageForm` is a DI singleton whose `ShowDialog` returns early when already visible (`if (Visible) { BringToFront(); return _response; }`), so a net built on it would swallow errors during exactly the cascade it exists to catch.
- **Run tests without `--no-build`**: `dotnet test tests/IndyPOS.Windows.Forms.Tests -c Release`. A solution-level build does not always emit this project's `net10.0-windows` output where `--no-build` looks for it.
- **Branch:** `fix/global-ui-error-handling`, off `development`. Already created; the spec and an unrelated quantity-button fix are already committed on it.

## Three refinements to the spec's interfaces

These are deliberate improvements found while planning. They do not change any approved behaviour.

1. **The reference-code format moves to its own `UiErrorReference` static class.** Both `UiErrorReporter` and `ReportErrorHandler` need it, and `ReportErrorHandler` is a static with no DI. A shared static formatter avoids introducing a service locator just so a static can reach the reporter.
2. **`UiErrorReporter` takes an injected `Serilog.ILogger`** rather than using the static `Log`. This lets tests assert on real `LogEvent`s through a capturing sink instead of mutating global state. `Program.Main` passes `Log.Logger`, so the instance is the same one `Log.CloseAndFlush()` flushes.
3. **`Show(reference, severity)` is split out from `ReportToUser`.** The fatal path must log, *then* flush, *then* show; that ordering is impossible if logging and showing are welded together. `ReportToUser` becomes `Record` + `Show`.

## File Structure

| File | Responsibility |
|---|---|
| `src/.../UI/Errors/UiErrorReference.cs` | Generates the `ERR-XXXX` code. The one place the format lives. |
| `src/.../UI/Errors/UiErrorSeverity.cs` | Recoverable / Fatal / Background — log level plus whether the reporter shows anything. |
| `src/.../UI/Errors/IErrorDialog.cs` | Presentation seam. |
| `src/.../UI/Errors/MessageBoxErrorDialog.cs` | Shipping implementation over `System.Windows.Forms.MessageBox`. |
| `src/.../UI/Errors/UiErrorReporter.cs` | Records to Serilog, shows the Thai message. Never throws. |
| `src/.../UI/Errors/GlobalErrorHandler.cs` | Attaches the reporter to the three framework channels. |
| `src/.../Program.cs` *(modify)* | Calls `GlobalErrorHandler.Wire` after `ConfigureLogger()`. |
| `src/.../UI/Report/ReportErrorHandler.cs` *(modify)* | Uses `UiErrorReference` so reports carry a code. |
| `tests/.../UI/Errors/UiErrorReferenceTests.cs` | Code format. |
| `tests/.../UI/Errors/UiErrorReporterTests.cs` | Logging, presentation, never-throw. |
| `tests/.../UI/Errors/FakeErrorDialog.cs` | Captures what would have been shown. |
| `tests/.../UI/Errors/CapturingSink.cs` | Captures `LogEvent`s. |
| `tests/IndyPOS.Windows.Forms.Tests.csproj` *(modify)* | Add `Serilog` 4.3.1. |

---

### Task 1: Reference code

**Files:**
- Create: `src/IndyPOS.Windows.Forms/UI/Errors/UiErrorReference.cs`
- Test: `tests/IndyPOS.Windows.Forms.Tests/UI/Errors/UiErrorReferenceTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `public static class UiErrorReference` with `public static string New()` returning a string matching `^ERR-[0-9A-F]{4}$`.

- [ ] **Step 1: Write the failing test**

Create `tests/IndyPOS.Windows.Forms.Tests/UI/Errors/UiErrorReferenceTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Windows.Forms.UI.Errors;
using Xunit;

namespace IndyPOS.Windows.Forms.Tests.UI.Errors;

public class UiErrorReferenceTests
{
    [Fact]
    public void New_ShouldReturnTheAgreedShape()
    {
        // Staff read this aloud over the phone, so the shape is a contract:
        // ERR- plus four uppercase hex characters.
        UiErrorReference.New().Should().MatchRegex("^ERR-[0-9A-F]{4}$");
    }

    [Fact]
    public void New_ShouldUseHexOnly()
    {
        // Hex avoids the 0/O ambiguity when a code is spoken, because O is not
        // a hex digit. Sample enough times to catch a stray character class.
        for (var i = 0; i < 500; i++)
        {
            UiErrorReference.New()[4..].Should().MatchRegex("^[0-9A-F]{4}$");
        }
    }

    [Fact]
    public void New_CalledRepeatedly_ShouldVaryPerOccurrence()
    {
        // The code identifies an occurrence, not an error type, so two calls
        // must not return the same value in practice.
        var codes = Enumerable.Range(0, 200).Select(_ => UiErrorReference.New()).ToHashSet();

        codes.Should().HaveCountGreaterThan(150, "codes identify occurrences, not types");
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests/IndyPOS.Windows.Forms.Tests -c Release --filter "FullyQualifiedName~UiErrorReferenceTests"`

Expected: FAIL to compile — `error CS0246: The type or namespace name 'UiErrorReference' could not be found`.

- [ ] **Step 3: Write the implementation**

Create `src/IndyPOS.Windows.Forms/UI/Errors/UiErrorReference.cs`:

```csharp
namespace IndyPOS.Windows.Forms.UI.Errors;

/// <summary>
/// Generates the short reference an operator reads back over the phone. The same
/// value is written to the log as an <c>ErrorReference</c> property, so a support
/// call narrows to a single entry.
/// </summary>
/// <remarks>
/// Four hex characters is 65,536 values. Collisions are harmless: entries also
/// carry a timestamp, and the code only has to narrow a search, not be unique
/// forever. Hex also sidesteps the spoken 0/O ambiguity, since O is not a hex digit.
/// </remarks>
public static class UiErrorReference
{
    private const int SignificantCharacters = 4;

    public static string New() =>
        $"ERR-{Guid.NewGuid().ToString("N")[..SignificantCharacters].ToUpperInvariant()}";
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test tests/IndyPOS.Windows.Forms.Tests -c Release --filter "FullyQualifiedName~UiErrorReferenceTests"`

Expected: PASS, 3 tests.

- [ ] **Step 5: Commit**

```bash
git add src/IndyPOS.Windows.Forms/UI/Errors/UiErrorReference.cs \
        tests/IndyPOS.Windows.Forms.Tests/UI/Errors/UiErrorReferenceTests.cs
git commit -m "feat(pos): add error reference codes for operator support calls"
```

---

### Task 2: The reporter

**Files:**
- Create: `src/IndyPOS.Windows.Forms/UI/Errors/UiErrorSeverity.cs`
- Create: `src/IndyPOS.Windows.Forms/UI/Errors/IErrorDialog.cs`
- Create: `src/IndyPOS.Windows.Forms/UI/Errors/UiErrorReporter.cs`
- Create: `tests/IndyPOS.Windows.Forms.Tests/UI/Errors/FakeErrorDialog.cs`
- Create: `tests/IndyPOS.Windows.Forms.Tests/UI/Errors/CapturingSink.cs`
- Create: `tests/IndyPOS.Windows.Forms.Tests/UI/Errors/UiErrorReporterTests.cs`
- Modify: `tests/IndyPOS.Windows.Forms.Tests/IndyPOS.Windows.Forms.Tests.csproj`

**Interfaces:**
- Consumes: `UiErrorReference.New()` from Task 1.
- Produces:
  - `public enum UiErrorSeverity { Recoverable, Fatal, Background }`
  - `public interface IErrorDialog { void Show(string message, string caption); }`
  - `public sealed class UiErrorReporter` with constructor `(IErrorDialog dialog, Serilog.ILogger logger)` and methods `string Record(Exception exception, string operation, UiErrorSeverity severity)`, `void Show(string reference, UiErrorSeverity severity)`, `void ReportToUser(Exception exception, string operation, UiErrorSeverity severity)`.

- [ ] **Step 1: Add the Serilog package reference to the test project**

In `tests/IndyPOS.Windows.Forms.Tests/IndyPOS.Windows.Forms.Tests.csproj`, add to the existing `<ItemGroup>` of `PackageReference` entries, keeping alphabetical order (after `Moq`):

```xml
    <PackageReference Include="Serilog" Version="4.3.1" />
```

Version 4.3.1 matches `src/IndyPOS.Windows.Forms/IndyPOS.Windows.Forms.csproj:118`. The tests need `Serilog.Core.ILogEventSink` and `Serilog.Events.LogEvent` directly, so rely on an explicit reference rather than a transitive one.

- [ ] **Step 2: Write the test doubles**

Create `tests/IndyPOS.Windows.Forms.Tests/UI/Errors/FakeErrorDialog.cs`:

```csharp
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
```

Create `tests/IndyPOS.Windows.Forms.Tests/UI/Errors/CapturingSink.cs`:

```csharp
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
        Events.SingleOrDefault()?.Properties.TryGetValue(name, out var value) == true
            ? value.ToString().Trim('"')
            : null;
}
```

- [ ] **Step 3: Write the failing tests**

Create `tests/IndyPOS.Windows.Forms.Tests/UI/Errors/UiErrorReporterTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Windows.Forms.UI.Errors;
using Serilog;
using Serilog.Events;
using Xunit;

namespace IndyPOS.Windows.Forms.Tests.UI.Errors;

public class UiErrorReporterTests
{
    private readonly CapturingSink _sink = new();

    private UiErrorReporter CreateSut(FakeErrorDialog dialog)
    {
        var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(_sink)
            .CreateLogger();

        return new UiErrorReporter(dialog, logger);
    }

    [Fact]
    public void Record_ShouldReturnAReferenceCodeInTheExpectedShape()
    {
        var sut = CreateSut(new FakeErrorDialog());

        var reference = sut.Record(new InvalidOperationException("boom"), "save product", UiErrorSeverity.Recoverable);

        reference.Should().MatchRegex("^ERR-[0-9A-F]{4}$");
    }

    [Fact]
    public void Record_ShouldNotShowAnything()
    {
        // The fatal path logs, flushes, then shows. That ordering is only possible
        // if recording never presents anything by itself.
        var dialog = new FakeErrorDialog();

        CreateSut(dialog).Record(new Exception("boom"), "save product", UiErrorSeverity.Fatal);

        dialog.Shown.Should().BeEmpty();
    }

    [Fact]
    public void ReportToUser_ShouldShowTheSameCodeItLogged()
    {
        // The whole support workflow depends on this: the code on screen must be
        // findable in the log.
        var dialog = new FakeErrorDialog();

        CreateSut(dialog).ReportToUser(new Exception("boom"), "save product", UiErrorSeverity.Recoverable);

        var logged = _sink.PropertyValue("ErrorReference");

        logged.Should().NotBeNull();
        dialog.Shown.Should().ContainSingle();
        dialog.Shown[0].Message.Should().Contain(logged!);
    }

    [Fact]
    public void Record_ForSeparateOccurrences_ShouldReturnDifferentCodes()
    {
        var sut = CreateSut(new FakeErrorDialog());

        var first = sut.Record(new Exception("a"), "op", UiErrorSeverity.Recoverable);
        var second = sut.Record(new Exception("b"), "op", UiErrorSeverity.Recoverable);

        second.Should().NotBe(first);
    }

    [Fact]
    public void ReportToUser_ShouldNotShowTheOperationNameToTheUser()
    {
        // Staff should not read internal method names off a till screen.
        var dialog = new FakeErrorDialog();

        CreateSut(dialog).ReportToUser(new Exception("boom"), "GenerateProductBarcodeAsync", UiErrorSeverity.Recoverable);

        dialog.Shown[0].Message.Should().NotContain("GenerateProductBarcodeAsync");
        dialog.Shown[0].Caption.Should().NotContain("GenerateProductBarcodeAsync");
    }

    [Fact]
    public void Record_ShouldLogTheOperationNameForDiagnosis()
    {
        CreateSut(new FakeErrorDialog())
            .Record(new Exception("boom"), "GenerateProductBarcodeAsync", UiErrorSeverity.Recoverable);

        _sink.PropertyValue("Operation").Should().Be("GenerateProductBarcodeAsync");
    }

    [Theory]
    [InlineData(UiErrorSeverity.Recoverable, LogEventLevel.Error)]
    [InlineData(UiErrorSeverity.Fatal, LogEventLevel.Fatal)]
    [InlineData(UiErrorSeverity.Background, LogEventLevel.Warning)]
    public void Record_ShouldLogAtTheLevelForTheSeverity(UiErrorSeverity severity, LogEventLevel expected)
    {
        CreateSut(new FakeErrorDialog()).Record(new Exception("boom"), "op", severity);

        _sink.Events.Should().ContainSingle();
        _sink.Events[0].Level.Should().Be(expected);
    }

    [Fact]
    public void Record_ShouldAttachTheException()
    {
        var exception = new InvalidOperationException("boom");

        CreateSut(new FakeErrorDialog()).Record(exception, "op", UiErrorSeverity.Recoverable);

        _sink.Events[0].Exception.Should().BeSameAs(exception);
    }

    [Fact]
    public void ReportToUser_WhenFatal_ShouldUseTheFatalCaption()
    {
        var dialog = new FakeErrorDialog();

        CreateSut(dialog).ReportToUser(new Exception("boom"), "op", UiErrorSeverity.Fatal);

        dialog.Shown[0].Caption.Should().Be("เกิดข้อผิดพลาดร้ายแรง");
        dialog.Shown[0].Message.Should().Contain("โปรแกรมต้องปิดตัวลง");
    }

    [Fact]
    public void ReportToUser_WhenRecoverable_ShouldUseTheRecoverableCaption()
    {
        var dialog = new FakeErrorDialog();

        CreateSut(dialog).ReportToUser(new Exception("boom"), "op", UiErrorSeverity.Recoverable);

        dialog.Shown[0].Caption.Should().Be("เกิดข้อผิดพลาด");
        dialog.Shown[0].Message.Should().Contain("กรุณาลองอีกครั้ง");
    }

    [Fact]
    public void ReportToUser_WhenBackground_ShouldNotShowAnything()
    {
        // Background means either nobody was waiting on the result, or the caller
        // presents its own dialog. Either way the reporter stays silent.
        var dialog = new FakeErrorDialog();

        CreateSut(dialog).ReportToUser(new Exception("boom"), "op", UiErrorSeverity.Background);

        dialog.Shown.Should().BeEmpty();
        _sink.Events.Should().ContainSingle("the failure is still recorded");
    }

    [Fact]
    public void ReportToUser_WhenTheDialogItselfThrows_ShouldNotPropagate()
    {
        // The reporter is the last line of defence. If it threw, the exception
        // would re-enter the handler that called it.
        var sut = CreateSut(new FakeErrorDialog(throwOnShow: true));

        var act = () => sut.ReportToUser(new Exception("boom"), "op", UiErrorSeverity.Recoverable);

        act.Should().NotThrow();
    }

    [Fact]
    public void ReportToUser_WhenTheDialogThrows_ShouldStillHaveLogged()
    {
        var sut = CreateSut(new FakeErrorDialog(throwOnShow: true));

        sut.ReportToUser(new Exception("boom"), "op", UiErrorSeverity.Recoverable);

        _sink.Events.Should().ContainSingle("the log entry must survive a broken dialog");
    }

    [Fact]
    public void Show_WithABackgroundSeverity_ShouldNotShowAnything()
    {
        var dialog = new FakeErrorDialog();

        CreateSut(dialog).Show("ERR-1234", UiErrorSeverity.Background);

        dialog.Shown.Should().BeEmpty();
    }
}
```

- [ ] **Step 4: Run the tests to verify they fail**

Run: `dotnet test tests/IndyPOS.Windows.Forms.Tests -c Release --filter "FullyQualifiedName~UiErrorReporterTests"`

Expected: FAIL to compile — `error CS0246: The type or namespace name 'UiErrorSeverity' could not be found` and the same for `IErrorDialog` and `UiErrorReporter`.

- [ ] **Step 5: Write the severity enum**

Create `src/IndyPOS.Windows.Forms/UI/Errors/UiErrorSeverity.cs`:

```csharp
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
```

- [ ] **Step 6: Write the dialog seam**

Create `src/IndyPOS.Windows.Forms/UI/Errors/IErrorDialog.cs`:

```csharp
namespace IndyPOS.Windows.Forms.UI.Errors;

/// <summary>
/// Shows an error to the operator. This seam exists so <see cref="UiErrorReporter"/>
/// can be tested without a UI.
/// </summary>
public interface IErrorDialog
{
    void Show(string message, string caption);
}
```

- [ ] **Step 7: Write the reporter**

Create `src/IndyPOS.Windows.Forms/UI/Errors/UiErrorReporter.cs`:

```csharp
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
```

- [ ] **Step 8: Run the tests to verify they pass**

Run: `dotnet test tests/IndyPOS.Windows.Forms.Tests -c Release --filter "FullyQualifiedName~UiErrorReporterTests"`

Expected: PASS, 15 tests.

- [ ] **Step 9: Run the whole test project**

Run: `dotnet test tests/IndyPOS.Windows.Forms.Tests -c Release`

Expected: PASS, 18 tests (3 from Task 1 plus 15 here). These are the first tests this project has ever run.

- [ ] **Step 10: Commit**

```bash
git add src/IndyPOS.Windows.Forms/UI/Errors/UiErrorSeverity.cs \
        src/IndyPOS.Windows.Forms/UI/Errors/IErrorDialog.cs \
        src/IndyPOS.Windows.Forms/UI/Errors/UiErrorReporter.cs \
        tests/IndyPOS.Windows.Forms.Tests/UI/Errors/ \
        tests/IndyPOS.Windows.Forms.Tests/IndyPOS.Windows.Forms.Tests.csproj
git commit -m "feat(pos): add a UI error reporter that logs and never throws"
```

---

### Task 3: Wire the safety net

**Files:**
- Create: `src/IndyPOS.Windows.Forms/UI/Errors/MessageBoxErrorDialog.cs`
- Create: `src/IndyPOS.Windows.Forms/UI/Errors/GlobalErrorHandler.cs`
- Modify: `src/IndyPOS.Windows.Forms/Program.cs`

**Interfaces:**
- Consumes: `UiErrorReporter`, `IErrorDialog`, `UiErrorSeverity` from Task 2.
- Produces: `internal static class GlobalErrorHandler` with `public static void Wire(UiErrorReporter reporter)`; `internal sealed class MessageBoxErrorDialog : IErrorDialog`.

**Note on coverage:** this task has no unit tests, deliberately. It attaches static framework events, which cannot be meaningfully asserted in-process, so it is kept as a thin adapter with no logic of its own — everything it could get wrong lives in Task 2's tested reporter. Task 5 verifies it on the VM. Do not add a test that merely calls `Wire` and asserts nothing.

- [ ] **Step 1: Write the MessageBox dialog**

Create `src/IndyPOS.Windows.Forms/UI/Errors/MessageBoxErrorDialog.cs`:

```csharp
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
```

- [ ] **Step 2: Write the wiring**

Create `src/IndyPOS.Windows.Forms/UI/Errors/GlobalErrorHandler.cs`:

```csharp
using Serilog;

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

            // Record, flush, then show. The process is going down, so getting the
            // entry onto disk must not depend on a MessageBox call succeeding.
            var reference = reporter.Record(exception, "unhandled domain exception", UiErrorSeverity.Fatal);

            Log.CloseAndFlush();

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
```

- [ ] **Step 3: Wire it from Program.Main**

In `src/IndyPOS.Windows.Forms/Program.cs`, add the using alongside the existing ones:

```csharp
using IndyPOS.Windows.Forms.UI.Errors;
```

Then change this block (currently `Program.cs:41-44`):

```csharp
		ApplicationConfiguration.Initialize();

		ClosePreviousProcesses();
		ConfigureLogger();
```

to:

```csharp
		ApplicationConfiguration.Initialize();

		ClosePreviousProcesses();
		ConfigureLogger();

		// After ConfigureLogger so the handler can log from its first invocation, and
		// before Launch() — which reaches Application.Run at Machine.cs:160. Built with
		// new rather than resolved from DI so it still works when host construction is
		// what failed.
		GlobalErrorHandler.Wire(new UiErrorReporter(new MessageBoxErrorDialog(), Log.Logger));
```

- [ ] **Step 4: Build**

Run: `dotnet build src/IndyPOS.Windows.Forms -c Release`

Expected: `0 Error(s)`. Warning count should be unchanged from before this task (56 at the time of writing) — a new warning means something needs attention.

- [ ] **Step 5: Confirm the whole solution still builds and all suites pass**

Run: `dotnet build -c Release`

Expected: `0 Error(s)`.

Run: `dotnet test tests/IndyPOS.Windows.Forms.Tests -c Release`

Expected: PASS, 18 tests.

- [ ] **Step 6: Commit**

```bash
git add src/IndyPOS.Windows.Forms/UI/Errors/MessageBoxErrorDialog.cs \
        src/IndyPOS.Windows.Forms/UI/Errors/GlobalErrorHandler.cs \
        src/IndyPOS.Windows.Forms/Program.cs
git commit -m "feat(pos): catch and log every unhandled UI exception"
```

---

### Task 4: Refold ReportErrorHandler

**Files:**
- Modify: `src/IndyPOS.Windows.Forms/UI/Report/ReportErrorHandler.cs`

**Interfaces:**
- Consumes: `UiErrorReference.New()` from Task 1.
- Produces: no signature change — `ReportErrorHandler.Show(MessageForm, Exception)` stays as-is for its **10 existing call sites** across `InvoiceProductsReportPanel`, `PayLaterPaymentsReportPanel`, `SalesHistoryReportPanel` and `SalesReportPanel`.

**Why not use `UiErrorReporter` here:** `ReportErrorHandler` is a static with no DI access, and report panels present their own styled `MessageForm`. Reaching the reporter from a static would need a service locator to gain nothing — the shared part worth reusing is the reference-code format, which Task 1 already extracted. Logging stays at Warning, as today.

- [ ] **Step 1: Update the handler**

Replace the whole body of `src/IndyPOS.Windows.Forms/UI/Report/ReportErrorHandler.cs` with:

```csharp
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
```

Note the raw `exception.Message` is gone from the operator's view — it now lives only in the log, consistent with the global handler.

- [ ] **Step 2: Confirm every call site still compiles**

Run: `dotnet build src/IndyPOS.Windows.Forms -c Release`

Expected: `0 Error(s)`. The signature is unchanged, so all existing callers are unaffected. Confirm they are still found:

Run: `grep -rn "ReportErrorHandler.Show" src/IndyPOS.Windows.Forms --include=*.cs`

Expected: the same call sites as before the change, all compiling.

- [ ] **Step 3: Commit**

```bash
git add src/IndyPOS.Windows.Forms/UI/Report/ReportErrorHandler.cs
git commit -m "refactor(pos): give report failures a reference code"
```

---

### Task 5: Verify on the VM

**Files:** none — this task produces evidence, not code.

**Interfaces:**
- Consumes: everything from Tasks 1-4.
- Produces: a verification record to paste into `.claude/STATUS.md`.

**Why this task exists:** Task 3's wiring cannot be unit-tested. This is the gate that actually proves the net works, so it is a task rather than an afterthought. Do not mark the plan complete without it.

- [ ] **Step 1: Publish the WinForms app**

Run: `./scripts/publish.ps1`

Expected: `publish/WinForms/` is refreshed. Confirm `IndyPOS.Windows.Forms.exe`'s `ProductVersion` carries the current git short hash, so you can prove the swap took.

- [ ] **Step 2: Hot-swap the binaries into the VM**

The VM must already have a healthy install. This avoids a full reinstall:

```powershell
$Config = Import-PowerShellDataFile 'C:\personal\IndyPOS\scripts\vm-testing\VMTestConfig.psd1'
$cred = Import-Clixml (Join-Path $env:LOCALAPPDATA $Config.CredentialCacheRelativePath)
$session = New-PSSession -VMName $Config.VMName -Credential $cred
$cur = 'C:\Users\IndyPOSAdmin\AppData\Local\IndyPOS.POS.v4\current'
Copy-Item -Path 'C:\personal\IndyPOS\publish\WinForms\*' -Destination $cur -ToSession $session -Recurse -Force
Remove-PSSession $session
```

Close the POS app in the guest first, or the copy will fail on locked DLLs.

- [ ] **Step 3: Trigger a recoverable failure**

In the guest, open the POS and force a handler to throw. The cheapest reliable trigger: stop the StoreHub service, then use a dialog that fetches from it.

```powershell
Stop-Service -Name 'IndyPOS.StoreHub.v4' -Force
```

Expected:
- A **Thai** dialog appears: caption `เกิดข้อผิดพลาด`, body containing `กรุณาลองอีกครั้ง` and a code of the form `ERR-XXXX`.
- **No** English "Unhandled exception has occurred in your application" box.
- The app is still usable afterwards — not killed, no Quit button offered.

Restart the service when done: `Start-Service -Name 'IndyPOS.StoreHub.v4'`

- [ ] **Step 4: Confirm the same code reached the log**

```powershell
$log = Get-ChildItem 'C:\ProgramData\IndyPOS\v4\logs' -Filter '*.json' |
       Sort-Object LastWriteTime -Descending | Select-Object -First 1
Select-String -Path $log.FullName -Pattern 'ERR-' | Select-Object -Last 5
```

Expected: an entry at `Error` level whose `ErrorReference` property matches the code shown on screen, carrying the full exception and an `Operation` property. Before this work such an entry did not exist at all.

- [ ] **Step 5: Confirm a second failure gets a different code**

Repeat step 3 and check the new code differs from the first. This proves codes identify occurrences, so two support calls cannot be conflated.

- [ ] **Step 6: Record the result**

Add a short verification block to `.claude/STATUS.md` stating what was triggered, the codes observed, that no English dialog appeared, and that the app survived. Note honestly that the `AppDomain.UnhandledException` and `TaskScheduler.UnobservedTaskException` paths were **not** exercised on the VM unless you actually managed to trigger them — do not imply coverage you did not obtain.

- [ ] **Step 7: Commit the status update**

```bash
git add .claude/STATUS.md
git commit -m "docs(session): record VM verification of the UI error net"
```

Note: `.claude/` is gitignored on this repo as of `5d87b1f`, so this commit may be a no-op. If `git status` shows nothing to commit, that is expected — keep the record in the file anyway for the next session.

---

## Self-Review

**1. Spec coverage**

| Spec requirement | Task |
|---|---|
| `IErrorDialog` + `MessageBoxErrorDialog` | 2 (seam), 3 (implementation) |
| `UiErrorReporter`, never-throws | 2 |
| Reference code `ERR-` + 4 hex, structured `ErrorReference` | 1, asserted in 2 |
| `GlobalErrorHandler.Wire`, three channels | 3 |
| `Application.ThreadException` → Error, dialog, continue | 3 |
| `AppDomain.UnhandledException` → Fatal, flush **then** show | 3 |
| `TaskScheduler.UnobservedTaskException` → Warning, `SetObserved`, no dialog | 3 |
| `SetUnhandledExceptionMode(CatchException)` unconditionally | 3 |
| `Program.Main` call site after `ConfigureLogger` | 3 |
| `ReportErrorHandler` refolded | 4 |
| Thai copy, both severities | 2 (constants), asserted in 2 |
| Operation name logged but not shown | 2 |
| Six named spec tests | 2 (all present, plus nine more) |
| VM verification | 5 |
| Out of scope: 56 per-operation handlers, `MessageForm` changes | not planned, correctly |

No gaps.

**2. Placeholder scan**

No TBD/TODO. Every code step carries complete, compiling code. No "similar to Task N" references. No "add appropriate error handling" — the error handling *is* the deliverable and its behaviour is specified per severity.

**3. Type consistency**

- `UiErrorReference.New()` — defined Task 1, used Tasks 2 and 4. Consistent.
- `UiErrorSeverity { Recoverable, Fatal, Background }` — defined Task 2, used 2 and 3. Consistent.
- `IErrorDialog.Show(string message, string caption)` — parameter order identical in the interface, `MessageBoxErrorDialog`, `FakeErrorDialog`, and every assertion.
- `UiErrorReporter.Record(Exception, string, UiErrorSeverity) -> string`, `Show(string, UiErrorSeverity)`, `ReportToUser(Exception, string, UiErrorSeverity)` — signatures identical across the Interfaces block, the implementation in Task 2, and the call sites in Task 3.
- Structured property names `ErrorReference` and `Operation` — the message template in Task 2 declares both; `CapturingSink.PropertyValue` reads them by those exact names; Task 4's template uses `ErrorReference` identically.

One deliberate asymmetry, flagged so it is not "fixed" by mistake: Task 4 logs through the static `Log` while Task 2's reporter uses an injected `ILogger`. `ReportErrorHandler` is a static with no DI, and `Program.Main` passes `Log.Logger` into the reporter, so both end up at the same sink.

## Execution Handoff

Two execution options:

1. **Subagent-Driven (recommended)** — a fresh subagent per task, review between tasks, fast iteration.
2. **Inline Execution** — execute in this session with checkpoints for review.
