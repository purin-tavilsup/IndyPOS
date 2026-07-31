# Global UI Error Handling — Design Spec

**Date:** 2026-07-31
**Branch:** `fix/global-ui-error-handling` (off `development`)
**Status:** Approved, not implemented

## Goal

No failure in the WinForms app may show the default English crash dialog, and no
failure may go unrecorded. Staff see a Thai message carrying a short reference code;
the code and the full exception go to the log.

## Why

A live install showed this to the operator:

```
Microsoft .NET
  Unhandled exception has occurred in your application. If you click
  Continue, the application will ignore this error and attempt to continue.
  Requested contents should be 12 (without checksum digit) or 13 digits
  long, but got 9.
                       [Details]  [Continue]  [Quit]
```

Three separate defects made that possible, and only the first has been fixed
(`ceecebd`, on the categories branch):

1. The barcode generator produced an unencodable value.
2. `AddNewInventoryProductWithCustomBarcodeForm.GenerateProductBarcodeAsync` is an
   `async void` handler with no `try`, so the exception escaped to the framework.
3. **Nothing in the app wires a global handler**, so WinForms fell back to its own
   dialog — in English, offering the operator a "Quit" button on a till — and the
   failure was never written to Serilog.

This spec addresses 3, which is the one that generalises. Fixing it protects all 56
`async void` handlers at once, including any added later.

### Verified current state

| Fact | Evidence |
|---|---|
| 56 `async void` handlers across 17 files; ~8 have any `try` | `grep 'async void' src/IndyPOS.Windows.Forms` |
| No global wiring of any kind | no match for `ThreadException`, `UnhandledException`, `SetUnhandledExceptionMode` |
| `Program.Main`'s `try/catch` is startup-only | wraps `Launch()`; `Application.Run` is at `Machine.cs:160`, inside it |
| Unhandled UI errors are never logged | WinForms' dialog shows and resumes without touching Serilog |
| A per-area convention exists | `ReportErrorHandler.Show` → `Log.Warning` + Thai `MessageForm` |
| `MessageForm` is a DI singleton | `ConfigureServices.cs:37` `AddSingleton<MessageForm>()` |
| `MessageForm.ShowDialog` returns early when already visible | guard in `MessageForm.cs`: `if (Visible) { BringToFront(); return _response; }` |

Handler counts by file, worst first: `InventoryPanel` 10, `SalesHistoryReportPanel` 6,
`SalePanel` 5, `PayLaterPaymentPanel` 5, `SalesReportPanel` 4,
`PayLaterPaymentsReportPanel` 4, `InvoiceProductsReportPanel` 4.

`SalePanel` is the one that matters most: five unguarded handlers on the till itself.

## Scope

**In scope** — the safety net and its infrastructure:

- Wire `Application.ThreadException`, `AppDomain.UnhandledException` and
  `TaskScheduler.UnobservedTaskException`.
- A reporter that generates the reference code, logs, and shows the Thai message.
- Refold `ReportErrorHandler` onto the reporter so reports gain a reference code.

**Out of scope** — deliberately deferred, not forgotten:

- Per-operation Thai messages for the 56 handlers. A follow-up PR, prioritising
  `SalePanel`, `PayLaterPaymentPanel` and the inventory dialogs.
- Any change to `MessageForm`.
- A specific guard in the barcode dialog — the net now covers it.

## Design

### 1. `IErrorDialog` — the presentation seam

```csharp
public interface IErrorDialog
{
    void Show(string message, string caption);
}
```

`MessageBoxErrorDialog` is the shipping implementation, calling
`MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error)`.

The seam exists so `UiErrorReporter` is testable without a UI.

**Why `MessageBox` and not the styled `MessageForm`:** the singleton returns early
when already visible (see the table above). A global net built on it would *silently
swallow* the error exactly when a failure cascade has another dialog open. `MessageBox`
stacks, always displays, and holds no reusable state that can be left dirty. A safety
net that can drop errors is not a safety net. The styled dialog stays in use on the
per-operation paths, where it works today.

### 2. `UiErrorReporter` — code, log, show

```csharp
public enum UiErrorSeverity
{
    Recoverable,  // logs Error   - the operation failed, the app carries on
    Fatal,        // logs Fatal   - the process is going down
    Background    // logs Warning - nobody was waiting; never shown to the user
}

public sealed class UiErrorReporter(IErrorDialog dialog)
{
    public string Record(Exception exception, string operation, UiErrorSeverity severity);
    public void ReportToUser(Exception exception, string operation, UiErrorSeverity severity);
}
```

- `Record` writes the Serilog entry at the severity's level and returns the reference
  code. It never shows anything.
- `ReportToUser` calls `Record`, then shows the Thai message for that severity.
  Called with `Background` it logs and shows nothing, so a miswiring degrades to
  log-only rather than surfacing a dialog the user cannot act on.

A severity enum rather than a `bool fatal`, because there are three log levels to
express, not two.

**Invariant: neither method may ever throw.** This is the last line of defence; an
exception escaping it would re-enter the very handler that called it. Every step is
individually guarded, and a failure to *show* still leaves the log entry written.

Constructed with `new` in `Program.Main`, not resolved from DI, so it works even when
host construction is what failed.

### 3. Reference code

`ERR-` followed by four uppercase hex characters from a fresh `Guid`:

```csharp
$"ERR-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}"
```

Logged as a structured `ErrorReference` property so it is greppable in the
compact-JSON log. It identifies an *occurrence*, not an error type. Collisions are
harmless — entries also carry timestamps, and the code only has to narrow a phone
call to the right entry.

Hex avoids the read-aloud ambiguity of `0`/`O`, since `O` is not a hex digit.

### 4. `GlobalErrorHandler` — the wiring

```csharp
internal static class GlobalErrorHandler
{
    public static void Wire(UiErrorReporter reporter);
}
```

Kept out of `Program.cs` because that file is `[ExcludeFromCodeCoverage]`.

| Channel | Thread | Severity | Action |
|---|---|---|---|
| `Application.ThreadException` | UI | `Recoverable` | `ReportToUser` → log `Error`, Thai dialog, app continues |
| `AppDomain.UnhandledException` | Any | `Fatal` | `Record` → log `Fatal`, **`Log.CloseAndFlush()`**, then best-effort dialog, process ends |
| `TaskScheduler.UnobservedTaskException` | Finalizer | `Background` | `Record` → log `Warning`, `SetObserved()`, **no dialog** |

The fatal path calls `Record` and shows the dialog itself, rather than `ReportToUser`,
so that the flush provably happens between the two.

Flush before the dialog on the fatal path: getting the entry onto disk must not
depend on a `MessageBox` call succeeding on a dying process.

No dialog for unobserved tasks — the event fires at GC finalization, arbitrarily
long after the fact, so a dialog would name an operation the user has moved on from.
The log entry is the whole value.

`Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException)` is set
unconditionally, including under a debugger. Differing dev/production behaviour is its
own source of bugs; a developer wanting to break at the throw site can use the
debugger's first-chance exception settings.

### 5. `Program.Main` — call site

```csharp
ApplicationConfiguration.Initialize();
ClosePreviousProcesses();
ConfigureLogger();
GlobalErrorHandler.Wire(new UiErrorReporter(new MessageBoxErrorDialog()));  // new
```

After `ConfigureLogger()` so the first thing the handler can do is log; before
`Launch()` and therefore before `Application.Run`.

### 6. `ReportErrorHandler` — refolded

Keeps its signature and its `MessageForm` presentation, so no visual regression on a
path that works. Its logging goes through `UiErrorReporter.Record`, and the returned
code is appended to the Thai message. Reports therefore gain a reference code and a
consistent log shape without changing how they look.

## Thai copy

| Case | Caption | Body |
|---|---|---|
| Recoverable | `เกิดข้อผิดพลาด` | `เกิดข้อผิดพลาดที่ไม่คาดคิด`<br><br>`กรุณาลองอีกครั้ง หากยังเกิดปัญหา แจ้งรหัส {code}` |
| Fatal | `เกิดข้อผิดพลาดร้ายแรง` | `โปรแกรมต้องปิดตัวลง`<br><br>`แจ้งรหัส {code}` |

The operation name is **not** shown to the operator — it goes to the log only. Staff
should not read internal method names off a till screen.

## Testing

First tests in `tests/IndyPOS.Windows.Forms.Tests`, which is fully configured
(xunit 2.9.3, FluentAssertions 8.8.0, Moq, AutoFixture) but contains no tests today.

`UiErrorReporterTests`, against a fake `IErrorDialog` that captures what it was asked
to show:

| Test | Pins |
|---|---|
| `Record_ShouldReturnAReferenceCodeInTheExpectedShape` | matches `^ERR-[0-9A-F]{4}$` |
| `ReportToUser_ShouldShowTheSameCodeItLogged` | the code on screen is the code in the log |
| `Record_ForSeparateOccurrences_ShouldReturnDifferentCodes` | identifies occurrence, not type |
| `ReportToUser_ShouldNotShowTheOperationNameToTheUser` | internals stay out of the dialog |
| `ReportToUser_WhenTheDialogItselfThrows_ShouldNotPropagate` | the never-throw invariant |
| `ReportToUser_WhenFatal_ShouldUseTheFatalCaption` | the severities are distinguishable on screen |
| `ReportToUser_WhenBackground_ShouldNotShowAnything` | an unactionable failure never reaches the user |
| `Record_ShouldNeverShowAnything` | logging and showing stay separable, which the fatal path depends on |

**Honest coverage limit:** `GlobalErrorHandler.Wire` attaches static framework events
and cannot be meaningfully unit-tested. It is deliberately a thin adapter — all logic
it could get wrong lives in `UiErrorReporter`, which is tested. The wiring itself is
verified manually on the VM (below). This spec does not claim automated coverage of it.

### VM verification

On the Hyper-V test VM, with a debug hook or a deliberately broken input:

1. Trigger a handler failure → Thai dialog with a code, **no English crash box**, app
   still usable afterwards.
2. Confirm the same code appears in `C:\ProgramData\IndyPOS\v4\logs\log<date>.json`
   with the full exception and the operation name.
3. Confirm a second, different failure yields a different code.

## Risks

| Risk | Mitigation |
|---|---|
| The net masks bugs during development | Every occurrence is logged at `Error` with a stack trace; nothing is swallowed silently. This is strictly more visibility than today, where nothing is logged at all. |
| A cascade spams dialogs | A modal `MessageBox` pumps its own message loop, so timers, paint handlers and marshalled callbacks keep firing and the error path can be re-entered on the same thread — the loop is *not* blocked. It is state-safe regardless: `UiErrorReporter` holds no mutable state (both fields readonly, all methods use locals, Serilog is thread-safe), so the worst case is stacked dialogs, not corruption. Accepted for this PR; if it proves real, the mitigation is a `[ThreadStatic]` "already showing" guard, deferred to the follow-up PR already scoped above. |
| `MessageBox` looks unstyled next to the rest of the app | Accepted deliberately for robustness. Per-operation paths keep `MessageForm`. |
| Reduced pressure to add per-operation handling | The follow-up PR is named in Scope, and generic messages are visibly worse than specific ones, so the incentive stays. |

## Decisions resolved during design

1. **Both layers, net first.** Goal is never-crash *and* useful Thai messages; the net
   ships first because it protects all 56 handlers immediately.
2. **Thai message + short reference code**, raw exception text to the log only.
3. **Safety net only in this PR**; per-operation messages deferred.
4. **`MessageBox` for the net**, because the `MessageForm` singleton can swallow.
5. **Catch always, including under a debugger**, for consistency.
