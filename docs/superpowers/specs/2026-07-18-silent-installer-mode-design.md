# Design: Silent Installer Mode — Unattended Headless Install

**Date:** 2026-07-18
**Status:** Draft (revised after 3-perspective sub-agent review — security / architecture / QA)
**Author:** Pond + Claude (pair)
**Reviewed by:** Security / Architecture / QA sub-agents (2026-07-18)

## Problem

`IndyPOS-Setup.exe` only runs interactively: `Program.Main` checks for admin
rights, then `Application.Run(new InstallationWizard())`. The operator types a
Store ID and clicks through the wizard; every install therefore needs a human at
the keyboard.

This blocks two things:

1. **VM/CI validation.** Every real production bug in this project's history was
   caught by the Hyper-V clean-install cycle — and every one of those runs needs
   a person to drive the wizard via `vmconnect.exe` (~15 min + manual clicks
   each). `Reset-AndInstall.ps1` already automates snapshot restore → copy
   installer → poll manifest → verify, but it stalls in the middle waiting for a
   human to complete the wizard. There is no way to run the full cycle unattended
   or in CI.
2. **Field/customer unattended installs.** A support engineer provisioning a
   store PC cannot script the install; they must remote in and click.

The install *engine* is already headless: `InstallationOrchestrator.InstallAsync`
runs all seven steps with no UI dependency and returns an `InstallationResult`.
Only the *entry path* and the wizard's *output surfaces* (on-screen credential
reveal, on-screen progress log) assume a human is watching.

## Goals

- `IndyPOS-Setup.exe --silent --store-id <ID>` performs a complete install with
  no window and no prompts.
- **No weakening** of the admin-provisioning security model
  (`2026-07-14-admin-provisioning-bootstrap-design.md`): the bootstrap password
  stays installer-generated, single-use, force-rotated, and delivered only via
  the ACL-locked `admin-credentials.txt`.
- A **reliable** machine-readable success/failure contract that survives a
  GUI-subsystem process launched over PSDirect (see Decision D8) so
  `Reset-AndInstall.ps1` and CI can branch on the result.
- Durable, ACL-locked install log on disk so a failed unattended field install
  can be diagnosed after the fact (there was no screen to have watched) without
  exposing secrets on a shared store PC.
- A watchdog so a hung step (e.g. a stalled Postgres download) cannot hang CI
  forever.
- Zero behavioural changes to `InstallationOrchestrator`'s install steps — the
  silent path reuses it; the only change is additive result fields.

## Non-Goals

- **Caller-supplied admin password** (`--app-password`). Rejected: it would put a
  secret on the command line (process listing, PowerShell history, CI logs) and
  reintroduce the human-chosen-password weakness the bootstrap redesign removed.
- **`--credentials-out <path>`** convenience copy. YAGNI for now — the CI harness
  runs as admin/SYSTEM in-guest and can read the canonical ACL-locked file
  directly. Revisit only if a consumer genuinely can't reach the config dir.
- Silent **uninstall** or **upgrade-specific** flags (separate future work).
- Auto-launching the POS app after a silent install (no user to hand off to).
- Removing the pre-existing machine-secret exposure on child-process command
  lines (psql `PASSWORD '…'`, EDB `--superpassword`). This is identical in
  attended mode, out of scope here, and noted as known residual under Security.

## Decisions

| # | Decision | Rationale |
|---|----------|-----------|
| D1 | Design for **both** field + CI, holding the stricter **field-grade** security bar | One code path, safe everywhere; CI is just the field path run in a throwaway VM. |
| D2 | Credential surface = **ACL-locked `admin-credentials.txt`** (source of truth) + non-secret markers | The file is identical to the attended path; markers give automation a signal without leaking the password. |
| D3 | **No caller-supplied password**; installer generates as today | Keeps the single-use / force-rotate property; no admin secret on the command line. |
| D4 | Progress → durable log file (authoritative) **and** stdout (best-effort live view) | Live view for an elevated prompt / captured console; durable file for field post-mortem and — per D8 — the reliable channel. |
| D5 | Soft failures (service didn't start, health check failed) **stay non-fatal** (exit 0) but surface as markers; the **verifier**, not the orchestrator's marker, is the CI health authority | Parity with the wizard's "install completed with a warning"; avoids the 10s-marker-vs-30s-verifier race producing contradictory CI results. The harness gating rule (below) is what actually fails a broken install. |
| D6 | Silent mode does **not** auto-launch the POS app | Unattended — nothing to hand off to. |
| D7 | Silent mode never shows a modal; preconditions fail via **exit code**, not MessageBox | A popup would hang an unattended run forever. |
| D8 | **The durable log file is the authoritative result + marker channel.** stdout is best-effort only. Exit code is captured by the caller via `Start-Process -Wait -PassThru`. | The bootstrapper is `WinExe` (GUI subsystem). `AttachConsole(ATTACH_PARENT_PROCESS)` does not reach a PSDirect host (`wsmprovhost` has no console), and a GUI exe launched via `&`/`Invoke-Command` returns immediately without a reliable `$LASTEXITCODE`. Relying on stdout/`$LASTEXITCODE` would silently break the CI payoff. |
| D9 | Markers are emitted with a unique line prefix `INDYPOS_MARKER ` and parsers take the **last** occurrence | The bare `ADMIN_SEEDED=` token already exists as a child-process protocol (`StoreHub.exe migrate`, `StoreHubInstaller` greps it); a prefix + last-wins removes any collision with relayed/echoed progress lines. |
| D10 | `SilentInstaller` owns an overall **watchdog timeout** (default 45 min, `--timeout-minutes` override) | Attended mode has a Cancel button; silent has none, and dropping the vmconnect/manifest-poll handoff removes the old 30-min outer bound. 45 min clears the internal 25-min Postgres timeout with margin. |
| D11 | ACL-lock the durable log (Administrators + SYSTEM) and **scrub persisted failure output** | The log by design captures step output and, on failure, child-process stderr (which can contain a connection string). ProgramData is `Users`-readable by default; without this the log is a plaintext leak on a shared POS terminal. |
| D12 | Verify the cred-file ACL was actually applied; if not, emit **`CRED_LOCKED=false`** (exit stays 0) | `RestrictFilePermissions` swallows all failures. In silent mode the file is the *only* carrier of the password, so a silently-unlocked file must be signalled explicitly rather than masked; the harness decides whether to treat it as fatal. |

## CLI contract

```
IndyPOS-Setup.exe --silent --store-id <ID> [--timeout-minutes <N>]
```

- `--silent` absent → today's `InstallationWizard`, entirely unchanged.
- `--silent` present, `--store-id` present and non-blank → headless install.
- `--silent` present, `--store-id` missing/blank/whitespace → usage error (exit 1).
- `--timeout-minutes <N>` optional, default 45; must be a positive integer.
- Flag **names** are case-insensitive (`--SILENT`, `--Store-Id`); the store-id
  **value** is preserved verbatim (only trimmed), never lower-cased.
- Both `--store-id <value>` and `--store-id=<value>` forms accepted.
- Unknown flags alongside `--silent` → usage error (fail fast, don't guess).

### Precondition: caller must already be elevated

`app.manifest` is `requireAdministrator`, so Windows will not start the process
non-elevated without a UAC consent prompt — which would hang an unattended run.
**The silent caller must already hold an elevated token** (PSDirect in-guest runs
as admin/SYSTEM, so CI is fine). The internal `IsRunningAsAdmin()` check remains
as defense-in-depth and maps to exit 3, but the real contract is "invoke from an
elevated session."

### Exit codes

| Exit | Meaning |
|------|---------|
| `0` | Install completed — admin seeded **or** already existed; all steps ran |
| `1` | Usage error — `--store-id` missing/blank, unknown flag, bad `--timeout-minutes` |
| `2` | Install failed — `InstallationOrchestrator` threw |
| `3` | Not elevated (defense-in-depth; normally unreachable behind the UAC manifest) |
| `4` | Timed out — watchdog fired before the install completed |

Exit code (captured via `Start-Process -Wait -PassThru`) is the primary
automation contract. Soft warnings never change it (see the harness rule below).

### Markers (non-secret, prefixed, written to log + stdout)

Emitted on their own lines, prefixed, after the streamed progress:

```
INDYPOS_MARKER RESULT=success            # or: failed | timeout
INDYPOS_MARKER ADMIN_SEEDED=true         # or: false (admin already existed → no cred file)
INDYPOS_MARKER CRED_FILE=C:\ProgramData\IndyPOS\v4\Config\admin-credentials.txt
INDYPOS_MARKER CRED_LOCKED=true          # or: false → cred file could NOT be ACL-locked (D12)
INDYPOS_MARKER SERVICE_STARTED=true      # or: false
INDYPOS_MARKER HEALTH=ok                 # or: failed (advisory — verifier is the gate, D5)
INDYPOS_MARKER RESET_HINT=<msg>          # only when ADMIN_SEEDED=false: how to recover via reset-admin
```

- The password itself is **never** emitted; `CRED_FILE` points at the ACL-locked
  file. `CRED_FILE` / `CRED_LOCKED` are present only when `ADMIN_SEEDED=true`.
- On a hard failure (exit 2) or timeout (exit 4), the orchestrator never returns
  an `InstallationResult`, so only `RESULT=failed|timeout` is guaranteed — the
  other markers are **absent**. Parsers must tolerate missing markers.

## Architecture

### Entry path (`Program.cs`)

`Main` becomes `static int` and parses `args` before anything else:

```
int Main(args):
    if args has "--silent":
        return SilentInstaller.Run(args)     // no ApplicationConfiguration.Initialize, no Application.Run
    // interactive path, unchanged:
    if not elevated: MessageBox + return 0
    ApplicationConfiguration.Initialize()
    Application.Run(new InstallationWizard())
    return 0
```

Keep `Main` thin — parsing lives in `SilentArgs`, the run in `SilentInstaller`.
The silent branch installs no `WindowsFormsSynchronizationContext`, so blocking
on the orchestrator via `.GetAwaiter().GetResult()` is safe (no message-loop
deadlock). Do not add a sync context to this path.

### New units

| Unit | Responsibility | Depends on |
|------|----------------|------------|
| `SilentInstallOptions` (record) + `SilentArgs.Parse(args)` → `ParseOutcome` | Pure args → options / usage-error. No I/O. | — |
| `SilentInstaller` | Headless driver: elevation check → build config → create+lock log → run orchestrator under a watchdog CTS → write+verify cred file → map outcome to exit code + markers. | `InstallationOrchestrator`, `AdminCredentialFile`, `SilentInstallLogger`, `SilentOutcomeMapper` |
| `SilentOutcomeMapper.Map(outcome)` → `(int exitCode, IReadOnlyList<string> markers)` | **Pure** mapping over an outcome union (below). No I/O — fully unit-testable. | — |
| `SilentInstallLogger` (`IProgress<InstallationProgress>`) | Thread-safe fan-out sink: each progress line → stdout (best-effort) + the on-disk log, under a lock, flushed per line. Scrubs/bounds failure text before persisting. | `SecretScrubber` |
| `AdminCredentialFile.Write(config)` → `string path` | Shared helper extracted from the wizard's `WriteAdminSummaryFile`: writes `admin-credentials.txt`, `RestrictFilePermissions`, **verifies the ACL**, returns the path + a locked flag. | `DatabaseSetup.RestrictFilePermissions` |
| `ConsoleAttach.TryAttach()` | `AttachConsole(ATTACH_PARENT_PROCESS)` P/Invoke; best-effort no-op when no parent console. | kernel32 |
| `SecretScrubber.Scrub(text)` | Best-effort redaction of connection-string-shaped substrings before persisting failure output. | — |

### Outcome model (for a pure, testable mapper)

The driver reduces a run to one of:

```
SilentOutcome =
  | Success(InstallationResult result, bool credLocked)
  | InstallFailed(string message)        // orchestrator threw
  | TimedOut
  | UsageError(string message)
  | NotElevated
```

`SilentOutcomeMapper.Map` turns any of these into `(exitCode, markers[])` with no
I/O, so all permutations — including hard-fail and timeout — are unit-tested
without running an install.

### Refactor: shared credential-file writer

`WriteAdminSummaryFile` currently lives as a `private static` in
`InstallationWizard`. Extract it to `AdminCredentialFile.Write(config)` (returns
the written path + whether the ACL verified), so the wizard and `SilentInstaller`
share one implementation (DRY — second consumer). The wizard calls the extracted
method; on-screen behaviour is unchanged.

### Additive orchestrator change (wizard-safe)

`InstallAsync` already computes `startResult.Success` and `healthOk` at its single
`return` (`InstallationOrchestrator.cs:249,263,295`). Extend `InstallationResult`
(kept a `class` with `init` setters — do **not** modernize to a record) with
`ServiceStarted` and `HealthOk` bools populated there. The wizard ignores them;
the silent path uses them for `SERVICE_STARTED` / `HEALTH` markers instead of
re-parsing log strings. No step logic changes.

## Data / control flow (silent run)

```
1. ConsoleAttach.TryAttach()                     # best-effort; stdout is convenience only
2. outcome = SilentArgs.Parse(args)
     usage error?  -> stderr message; print RESULT=failed marker to stdout; return 1
3. elevation check
     not admin?    -> stderr message; return 3        # no file/log side effects yet
4. config = new InstallationConfig { StoreId = options.StoreId.Trim() }
5. create + ACL-lock <SystemRoot>\logs\ ; open install-<ts>-<pid>.log (+ install-latest.log)
6. logger = new SilentInstallLogger(logFile)
7. cts = new CTS(timeout)                          # watchdog (D10)
   try:
       result = await orchestrator.InstallAsync(config, logger, cts.Token)
   catch OperationCanceledException when cts timed out:
       outcome = TimedOut                          # RESULT=timeout; return 4
   catch Exception ex:
       outcome = InstallFailed(Scrub(ex.Message))  # RESULT=failed;  return 2
8. if result.AdminSeeded:
       (path, locked) = AdminCredentialFile.Write(config)
       markers += CRED_FILE=path, CRED_LOCKED=locked   # CRED_LOCKED=false is the warning signal; exit stays 0
   else:
       markers += RESET_HINT=<run "IndyPOS.StoreHub.exe reset-admin" to reissue a bootstrap password>
9. emit markers (RESULT/ADMIN_SEEDED/SERVICE_STARTED/HEALTH/…) to log + stdout
10. return 0
```

Ordering note: the logs directory is created **only after** the elevation check
passes (step 5), so a usage/not-elevated exit leaves no side effects.

## Error handling

- **Usage / not-elevated:** message to stderr, exit 1 / 3, no file or log side
  effects.
- **Watchdog timeout:** cancel the orchestrator, `RESULT=timeout`, exit 4. The
  harness also keeps its own outer `Invoke-Command` timeout as belt-and-braces.
- **Orchestrator throws:** logger persists a **scrubbed, bounded** message (raw
  child-process stderr is *not* written verbatim to the durable log — write a
  generic "step X failed (exit N); see StoreHub logs at <path>" line and scrub
  connection-string-shaped text), `RESULT=failed`, exit 2. Partial-install
  cleanup is out of scope (matches the wizard).
- **Cred-file write / ACL failure:** non-fatal to the install (matches wizard),
  but reported: `CRED_LOCKED=false` and a warning in the log. The bootstrap
  password is recoverable via `reset-admin`, so a lost/unlocked file is not a
  lockout. (Caveat: `reset-admin` prints the new password to stdout — its output
  is sensitive and must not be captured into durable/CI logs.)
- **Console attach fails** (detached / PSDirect host): proceed silently; the
  ACL-locked log file is the authoritative record (D8).

## Logging

- Log file: `<SystemRoot>\logs\install-YYYYMMDD-HHMMSS-<pid>.log`
  (`C:\ProgramData\IndyPOS\v4\logs\…`), plus a stable `install-latest.log`
  (copy/rewrite) so a consumer can read a deterministic path without globbing.
  The `<pid>` suffix avoids same-second collisions on CI retry loops.
- The logs directory and both files are ACL-locked (Administrators + SYSTEM) via
  `RestrictFilePermissions` (D11).
- Every `InstallationProgress` line is timestamped and written to stdout + file
  under a lock, flushed per line (so an early hard crash still leaves a
  diagnosable log). Error-flagged lines get a `WARNING:`/`ERROR:` prefix.
- Markers are written last, to both surfaces, with the `INDYPOS_MARKER ` prefix.

## Security

- **Happy-path progress carries no secret** — verified across all seven steps:
  `InstallationOrchestrator` and the installers log labels ("Creating database
  user 'indypos_app'…"), never the values; `StoreHub.exe migrate` emits only
  `ADMIN_SEEDED=`.
- **Failure output is scrubbed + bounded** before it reaches the durable log or
  stdout (D11) — closes the one plausible leak (child-process stderr containing a
  connection string).
- **Durable log is ACL-locked** (D11) — no plaintext step output readable by a
  cashier on a shared terminal.
- **Cred-file ACL is verified** (D12) — a silently-unlocked password file is
  reported (`CRED_LOCKED=false`), never masked as success.
- **No admin secret on the command line** — `--silent --store-id <ID>` contains
  no secret; PowerShell history stays clean.
- **Known residual (pre-existing, not worsened):** the DB app-user password
  (psql `PASSWORD '…'`, `DatabaseSetup.cs:201,275`) and the Postgres superuser
  password (EDB `--superpassword`, `PostgresInstaller.cs:198`) are visible to
  `Win32_Process` during the install window. Identical in attended mode; tracked
  separately, out of scope here.

## Testing

### Unit (no Docker / no VM)

- `SilentArgs.Parse`:
  - `--silent --store-id ABC` and `--store-id=ABC` → `StoreId=ABC`.
  - `--silent` with no / blank / whitespace store id → usage error.
  - `--store-id` appearing last with no following value → usage error.
  - unknown flag alongside `--silent` → usage error (must **not** fire on the
    store-id's value token).
  - `--timeout-minutes` absent → default; non-integer / ≤0 → usage error.
  - flag-name case-insensitivity; store-id value preserved verbatim + trimmed.
  - no `--silent` → "interactive" outcome.
- `SilentOutcomeMapper.Map` over the outcome union: Success(seeded, locked),
  Success(seeded, **unlocked** → CRED_LOCKED=false, still exit 0),
  Success(already-existed → no CRED_FILE, RESET_HINT present), InstallFailed
  (exit 2, only RESULT=failed), TimedOut (exit 4), UsageError (exit 1),
  NotElevated (exit 3) → each asserts the exact exit code + marker set.
- `SecretScrubber.Scrub`: connection-string-shaped input is redacted; ordinary
  text passes through.
- `AdminCredentialFile.Write` round-trip in a temp dir: file contents include
  username + password + one-time notice; returns the written path.

### Integration (VM — the payoff)

Extend `Reset-AndInstall.ps1` to run silent install in-guest and gate on a
**concrete, explicit rule**:

- Launch: `Start-Process -Wait -PassThru IndyPOS-Setup.exe --silent --store-id
  <VMTestConfig store id>` (in-guest, over PSDirect) → capture `.ExitCode`.
- Read markers from the **durable log** (`install-latest.log`), not stdout (D8).
- **Pass/fail rule (authoritative):** FAIL if
  `ExitCode ≠ 0 OR RESULT≠success OR SERVICE_STARTED=false`. Treat `HEALTH` as
  advisory; the existing `Test-IndyPOSInstallation.ps1` verifier (30s health
  window, flips `OverallPass` on failure) is the health authority and runs as the
  second gate.
- Then read the cred file (path from the `CRED_FILE` marker — never a hardcoded
  `v4`) and exercise the login / force-change flow, as the manual VM run does.
- Keep an outer `Invoke-Command` timeout as belt-and-braces to D10.

This turns the whole clean-install cycle into one unattended command.

## Impact / files

| File | Change |
|------|--------|
| `installer/IndyPOS.Bootstrapper/Program.cs` | `Main` → `static int`; parse args; branch to `SilentInstaller` on `--silent`. |
| `installer/IndyPOS.Bootstrapper/Silent/SilentArgs.cs` (new) | `SilentInstallOptions` record + `Parse`. |
| `installer/IndyPOS.Bootstrapper/Silent/SilentInstaller.cs` (new) | Headless driver + watchdog. |
| `installer/IndyPOS.Bootstrapper/Silent/SilentOutcomeMapper.cs` (new) | Pure outcome → exit-code + markers. |
| `installer/IndyPOS.Bootstrapper/Silent/SilentInstallLogger.cs` (new) | Thread-safe stdout + ACL-locked-file `IProgress` sink. |
| `installer/IndyPOS.Bootstrapper/Silent/ConsoleAttach.cs` (new) | `AttachConsole` P/Invoke. |
| `installer/IndyPOS.Bootstrapper/Silent/SecretScrubber.cs` (new) | Redact connection-string-shaped failure text. |
| `installer/IndyPOS.Bootstrapper/Installers/AdminCredentialFile.cs` (new) | Extracted shared cred-file writer (+ ACL verify, returns path). |
| `installer/IndyPOS.Bootstrapper/UI/InstallationWizard.cs` | Call `AdminCredentialFile.Write` instead of the private method. |
| `installer/IndyPOS.Bootstrapper/Installers/InstallationOrchestrator.cs` | Additive: `InstallationResult.ServiceStarted` + `HealthOk`. |
| `scripts/vm-testing/Reset-AndInstall.ps1` | Drive silent install (`Start-Process -Wait -PassThru`); parse markers from log; apply the gating rule; drop the vmconnect handoff. |
| `scripts/vm-testing/VMTestConfig.psd1` | Read cred/manifest paths from markers, not a hardcoded `v4` (guard the major-version bump). |
| `tests/IndyPOS.Bootstrapper.Tests/…` | Arg-parse, outcome-mapper, scrubber, cred-file unit tests. |

## Open questions

None outstanding — CLI shape, credential surface, log surface, the
GUI-subsystem/PSDirect channel, exit-code semantics, watchdog, and the harness
gating rule are all settled (brainstorming + sub-agent review).
