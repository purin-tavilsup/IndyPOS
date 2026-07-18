# Silent Installer Mode Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a headless `IndyPOS-Setup.exe --silent --store-id <ID>` install path so the VM/CI clean-install cycle and field unattended installs run with no wizard.

**Architecture:** A thin `SilentInstaller` driver reuses the existing headless `InstallationOrchestrator` unchanged. Pure units (arg parser, outcome→exit/marker mapper, secret scrubber) are TDD'd in isolation; the driver wires them plus a thread-safe stdout+ACL-locked-file logger and an overall watchdog timeout. Because the exe is `WinExe` (GUI subsystem), the durable log file is the authoritative result/marker channel and the caller captures the exit code via `Start-Process -Wait -PassThru`.

**Tech Stack:** C# .NET 10 (`net10.0-windows`, WinForms host), xUnit + FluentAssertions, PowerShell (VM harness), Velopack/bootstrapper installer.

**Spec:** `docs/superpowers/specs/2026-07-18-silent-installer-mode-design.md`

## Global Constraints

- Target framework: `net10.0-windows`; `Nullable` enable; `ImplicitUsings` enable (from the bootstrapper csproj — no per-file `using System;` needed for common namespaces).
- Bootstrapper is `<OutputType>WinExe</OutputType>` with `requireAdministrator` manifest and `StartupObject=IndyPOS.Bootstrapper.Program`.
- Test project `IndyPOS.Bootstrapper.Tests` has `InternalsVisibleTo` from the bootstrapper; use xUnit `[Fact]`/`[Theory]` + FluentAssertions `.Should()`. No `#region`, AAA with blank lines, test names `Method_Condition_ShouldExpectedBehavior`.
- **No admin secret on the command line** — no `--app-password`; the bootstrap password stays installer-generated.
- **The password is never emitted to stdout or the log** — only its file path (`CRED_FILE`).
- Marker lines are prefixed `INDYPOS_MARKER ` and consumers read the **last** occurrence.
- Exit codes: `0` success, `1` usage error, `2` install failed, `3` not elevated, `4` timeout.
- New silent-path files live under `installer/IndyPOS.Bootstrapper/Silent/` (namespace `IndyPOS.Bootstrapper.Silent`); `AdminCredentialFile` lives in `Installers/` (namespace `IndyPOS.Bootstrapper.Installers`).
- Commit after every task. `InstallationResult` stays a `class` (do not convert to a record).

---

## File Structure

**New (silent path — `installer/IndyPOS.Bootstrapper/Silent/`):**
- `SilentArgs.cs` — pure arg parser → `ParseResult`.
- `SilentOutcomeMapper.cs` — pure `SilentOutcome` → `(exitCode, markers)`.
- `SecretScrubber.cs` — pure connection-string redaction.
- `ConsoleAttach.cs` — `AttachConsole` P/Invoke (best-effort).
- `SilentInstallLogger.cs` — thread-safe `IProgress<InstallationProgress>` sink (stdout + file).
- `Elevation.cs` — shared `IsElevated()` helper.
- `SilentInstaller.cs` — headless driver (integration point).

**New (`installer/IndyPOS.Bootstrapper/Installers/`):**
- `AdminCredentialFile.cs` — shared ACL-locked cred-file writer (extracted from the wizard).

**Modified:**
- `installer/IndyPOS.Bootstrapper/Installers/DatabaseSetup.cs` — add `TryRestrictFilePermissions` returning success.
- `installer/IndyPOS.Bootstrapper/Installers/InstallationOrchestrator.cs` — additive `InstallationResult.ServiceStarted` + `HealthOk`.
- `installer/IndyPOS.Bootstrapper/UI/InstallationWizard.cs` — call `AdminCredentialFile.Write`; delete the private `WriteAdminSummaryFile`.
- `installer/IndyPOS.Bootstrapper/Program.cs` — `Main` → `static int`; `--silent` branch.
- `scripts/vm-testing/Reset-AndInstall.ps1` — silent invocation + gating rule (replaces the vmconnect handoff).
- `scripts/vm-testing/VMTestConfig.psd1` — add `TestStoreId`.

**Tests (`tests/IndyPOS.Bootstrapper.Tests/`):**
- `Silent/SilentArgsTests.cs`, `Silent/SilentOutcomeMapperTests.cs`, `Silent/SecretScrubberTests.cs`, `Silent/SilentInstallLoggerTests.cs`, `Silent/SilentInstallerTests.cs`, `Installers/AdminCredentialFileTests.cs`.

---

### Task 1: `TryRestrictFilePermissions` + shared `AdminCredentialFile`

**Files:**
- Modify: `installer/IndyPOS.Bootstrapper/Installers/DatabaseSetup.cs:146-179`
- Create: `installer/IndyPOS.Bootstrapper/Installers/AdminCredentialFile.cs`
- Modify: `installer/IndyPOS.Bootstrapper/UI/InstallationWizard.cs:300,365-386`
- Test: `tests/IndyPOS.Bootstrapper.Tests/Installers/AdminCredentialFileTests.cs`

**Interfaces:**
- Produces: `DatabaseSetup.TryRestrictFilePermissions(string filePath) : bool` (internal static); `AdminCredentialFile.Write(InstallationConfig config) : (string Path, bool Locked)`.

- [ ] **Step 1: Write the failing test**

`InstallationConfig.ConfigDirectory` is a computed property under `C:\ProgramData\IndyPOS\v4\Config`; the test writes there (the test process owns the file, so the ACL lock succeeds without elevation) and cleans up in `finally`.

```csharp
using FluentAssertions;
using IndyPOS.Bootstrapper.Installers;
using Xunit;

namespace IndyPOS.Bootstrapper.Tests.Installers;

public class AdminCredentialFileTests
{
    [Fact]
    public void Write_WithSeededAdmin_ShouldWriteCredentialFileAndReturnPath()
    {
        var config = new InstallationConfig { StoreId = "STORE-1" };
        var expectedPath = Path.Combine(config.ConfigDirectory, "admin-credentials.txt");

        try
        {
            var (path, locked) = AdminCredentialFile.Write(config);

            path.Should().Be(expectedPath);
            File.Exists(path).Should().BeTrue();
            var contents = File.ReadAllText(path);
            contents.Should().Contain($"Username: {config.AdminUsername}");
            contents.Should().Contain($"Password: {config.AdminPassword}");
            locked.Should().BeTrue();
        }
        finally
        {
            if (File.Exists(expectedPath)) File.Delete(expectedPath);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests/IndyPOS.Bootstrapper.Tests.csproj --filter "FullyQualifiedName~AdminCredentialFileTests"`
Expected: FAIL — `AdminCredentialFile` does not exist (compile error).

- [ ] **Step 3: Refactor `DatabaseSetup.RestrictFilePermissions` to expose success**

In `installer/IndyPOS.Bootstrapper/Installers/DatabaseSetup.cs`, replace the whole `RestrictFilePermissions` method (lines 146-179) with:

```csharp
internal static void RestrictFilePermissions(string filePath) => TryRestrictFilePermissions(filePath);

internal static bool TryRestrictFilePermissions(string filePath)
{
    try
    {
        var fileInfo = new FileInfo(filePath);
        var security = fileInfo.GetAccessControl();

        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

        var rules = security.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
        foreach (System.Security.AccessControl.FileSystemAccessRule rule in rules)
        {
            security.RemoveAccessRule(rule);
        }

        security.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(
            new System.Security.Principal.SecurityIdentifier(
                System.Security.Principal.WellKnownSidType.BuiltinAdministratorsSid, null),
            System.Security.AccessControl.FileSystemRights.FullControl,
            System.Security.AccessControl.AccessControlType.Allow));

        security.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(
            new System.Security.Principal.SecurityIdentifier(
                System.Security.Principal.WellKnownSidType.LocalSystemSid, null),
            System.Security.AccessControl.FileSystemRights.FullControl,
            System.Security.AccessControl.AccessControlType.Allow));

        fileInfo.SetAccessControl(security);
        return true;
    }
    catch
    {
        // Ignore permission errors - file is still created
        return false;
    }
}
```

- [ ] **Step 4: Create `AdminCredentialFile`**

Create `installer/IndyPOS.Bootstrapper/Installers/AdminCredentialFile.cs`:

```csharp
namespace IndyPOS.Bootstrapper.Installers;

/// <summary>
/// Writes the one-time admin bootstrap credential to an ACL-locked file.
/// Shared by the wizard finish screen and the silent installer so both deliver
/// the credential identically.
/// </summary>
public static class AdminCredentialFile
{
    /// <summary>
    /// Writes admin-credentials.txt in the config dir and ACL-locks it to
    /// Administrators + LocalSystem. Returns the path and whether the lock applied.
    /// Never throws — a failed write returns Locked=false so callers can react.
    /// </summary>
    public static (string Path, bool Locked) Write(InstallationConfig config)
    {
        var path = Path.Combine(config.ConfigDirectory, "admin-credentials.txt");
        var contents =
            "IndyPOS initial admin sign-in\r\n" +
            "================================\r\n" +
            $"Username: {config.AdminUsername}\r\n" +
            $"Password: {config.AdminPassword}\r\n\r\n" +
            "This is a one-time password. You will be required to set a new one\r\n" +
            "on first sign-in. Delete this file after you have signed in.\r\n";

        try
        {
            Directory.CreateDirectory(config.ConfigDirectory);
            File.WriteAllText(path, contents);
        }
        catch
        {
            return (path, false);
        }

        return (path, DatabaseSetup.TryRestrictFilePermissions(path));
    }
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests/IndyPOS.Bootstrapper.Tests.csproj --filter "FullyQualifiedName~AdminCredentialFileTests"`
Expected: PASS.

- [ ] **Step 6: Point the wizard at the shared writer**

In `installer/IndyPOS.Bootstrapper/UI/InstallationWizard.cs`:
- Replace the call at line 300 `WriteAdminSummaryFile(_config);` with `AdminCredentialFile.Write(_config);`
- Delete the entire private method `WriteAdminSummaryFile` (lines 365-386).

- [ ] **Step 7: Build to verify the wizard still compiles**

Run: `dotnet build installer/IndyPOS.Bootstrapper/IndyPOS.Bootstrapper.csproj -c Release`
Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 8: Commit**

```bash
git add installer/IndyPOS.Bootstrapper/Installers/DatabaseSetup.cs installer/IndyPOS.Bootstrapper/Installers/AdminCredentialFile.cs installer/IndyPOS.Bootstrapper/UI/InstallationWizard.cs tests/IndyPOS.Bootstrapper.Tests/Installers/AdminCredentialFileTests.cs
git commit -m "refactor(installer): extract shared ACL-locked admin credential writer"
```

---

### Task 2: `SecretScrubber`

**Files:**
- Create: `installer/IndyPOS.Bootstrapper/Silent/SecretScrubber.cs`
- Test: `tests/IndyPOS.Bootstrapper.Tests/Silent/SecretScrubberTests.cs`

**Interfaces:**
- Produces: `SecretScrubber.Scrub(string? text) : string`.

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using IndyPOS.Bootstrapper.Silent;
using Xunit;

namespace IndyPOS.Bootstrapper.Tests.Silent;

public class SecretScrubberTests
{
    [Theory]
    [InlineData("Host=127.0.0.1;Password=SuperSecret;Db=x")]
    [InlineData("Host=127.0.0.1;Pwd=SuperSecret")]
    public void Scrub_WithPasswordAssignment_ShouldRedactValue(string input)
    {
        var result = SecretScrubber.Scrub(input);

        result.Should().NotContain("SuperSecret");
        result.Should().Contain("***REDACTED***");
    }

    [Fact]
    public void Scrub_WithPlainText_ShouldReturnUnchanged()
    {
        SecretScrubber.Scrub("provisioning failed (exit 1)").Should().Be("provisioning failed (exit 1)");
    }

    [Fact]
    public void Scrub_WithNull_ShouldReturnEmpty()
    {
        SecretScrubber.Scrub(null).Should().BeEmpty();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests/IndyPOS.Bootstrapper.Tests.csproj --filter "FullyQualifiedName~SecretScrubberTests"`
Expected: FAIL — `SecretScrubber` does not exist.

- [ ] **Step 3: Implement `SecretScrubber`**

Create `installer/IndyPOS.Bootstrapper/Silent/SecretScrubber.cs`:

```csharp
using System.Text.RegularExpressions;

namespace IndyPOS.Bootstrapper.Silent;

/// <summary>
/// Best-effort redaction of connection-string-shaped secrets from text before it
/// is persisted to the install log or echoed to stdout on a failure path.
/// </summary>
public static partial class SecretScrubber
{
    // Password=... or Pwd=... up to the next ';' or end of string (case-insensitive).
    [GeneratedRegex(@"(?i)\b(password|pwd)\s*=\s*[^;]*")]
    private static partial Regex PasswordAssignment();

    public static string Scrub(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return PasswordAssignment().Replace(text, "$1=***REDACTED***");
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests/IndyPOS.Bootstrapper.Tests.csproj --filter "FullyQualifiedName~SecretScrubberTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add installer/IndyPOS.Bootstrapper/Silent/SecretScrubber.cs tests/IndyPOS.Bootstrapper.Tests/Silent/SecretScrubberTests.cs
git commit -m "feat(installer): add SecretScrubber for install-log failure output"
```

---

### Task 3: `SilentArgs` parser

**Files:**
- Create: `installer/IndyPOS.Bootstrapper/Silent/SilentArgs.cs`
- Test: `tests/IndyPOS.Bootstrapper.Tests/Silent/SilentArgsTests.cs`

**Interfaces:**
- Produces:
  - `record SilentInstallOptions(string StoreId, int TimeoutMinutes)`
  - `enum ParseStatus { Silent, NotSilent, UsageError }`
  - `record ParseResult(ParseStatus Status, SilentInstallOptions? Options, string? ErrorMessage)` with factories `Silent(options)`, `NotSilent()`, `Usage(message)`
  - `SilentArgs.Parse(string[] args) : ParseResult`

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using IndyPOS.Bootstrapper.Silent;
using Xunit;

namespace IndyPOS.Bootstrapper.Tests.Silent;

public class SilentArgsTests
{
    [Theory]
    [InlineData(new[] { "--silent", "--store-id", "ABC" })]
    [InlineData(new[] { "--silent", "--store-id=ABC" })]
    [InlineData(new[] { "--SILENT", "--Store-Id", "ABC" })]
    public void Parse_WithSilentAndStoreId_ShouldReturnSilentWithStoreId(string[] args)
    {
        var result = SilentArgs.Parse(args);

        result.Status.Should().Be(ParseStatus.Silent);
        result.Options!.StoreId.Should().Be("ABC");
        result.Options.TimeoutMinutes.Should().Be(45);
    }

    [Fact]
    public void Parse_WithStoreIdValue_ShouldPreserveCaseAndTrim()
    {
        var result = SilentArgs.Parse(new[] { "--silent", "--store-id", "  Rungrat-001  " });

        result.Options!.StoreId.Should().Be("Rungrat-001");
    }

    [Fact]
    public void Parse_WithTimeoutOverride_ShouldUseIt()
    {
        var result = SilentArgs.Parse(new[] { "--silent", "--store-id", "A", "--timeout-minutes", "10" });

        result.Options!.TimeoutMinutes.Should().Be(10);
    }

    [Theory]
    [InlineData(new[] { "--silent" })]
    [InlineData(new[] { "--silent", "--store-id" })]
    [InlineData(new[] { "--silent", "--store-id", " " })]
    [InlineData(new[] { "--silent", "--store-id", "A", "--timeout-minutes", "0" })]
    [InlineData(new[] { "--silent", "--store-id", "A", "--timeout-minutes", "notanumber" })]
    [InlineData(new[] { "--silent", "--store-id", "A", "--bogus" })]
    public void Parse_WithBadSilentArgs_ShouldReturnUsageError(string[] args)
    {
        SilentArgs.Parse(args).Status.Should().Be(ParseStatus.UsageError);
    }

    [Theory]
    [InlineData(new string[0])]
    [InlineData(new[] { "--store-id", "ABC" })]
    public void Parse_WithoutSilent_ShouldReturnNotSilent(string[] args)
    {
        SilentArgs.Parse(args).Status.Should().Be(ParseStatus.NotSilent);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests/IndyPOS.Bootstrapper.Tests.csproj --filter "FullyQualifiedName~SilentArgsTests"`
Expected: FAIL — `SilentArgs` does not exist.

- [ ] **Step 3: Implement `SilentArgs`**

Create `installer/IndyPOS.Bootstrapper/Silent/SilentArgs.cs`:

```csharp
namespace IndyPOS.Bootstrapper.Silent;

public sealed record SilentInstallOptions(string StoreId, int TimeoutMinutes);

public enum ParseStatus { Silent, NotSilent, UsageError }

public sealed record ParseResult(ParseStatus Status, SilentInstallOptions? Options, string? ErrorMessage)
{
    public static ParseResult Silent(SilentInstallOptions options) => new(ParseStatus.Silent, options, null);
    public static ParseResult NotSilent() => new(ParseStatus.NotSilent, null, null);
    public static ParseResult Usage(string message) => new(ParseStatus.UsageError, null, message);
}

/// <summary>
/// Pure parser for the silent-install command line. No I/O — fully unit-testable.
/// Flag NAMES are case-insensitive; the --store-id VALUE is preserved verbatim
/// (only trimmed).
/// </summary>
public static class SilentArgs
{
    private const int DefaultTimeoutMinutes = 45;

    public static ParseResult Parse(string[] args)
    {
        var isSilent = args.Any(a => NameOf(a).Equals("--silent", StringComparison.OrdinalIgnoreCase));
        if (!isSilent) return ParseResult.NotSilent();

        string? storeId = null;
        var timeoutMinutes = DefaultTimeoutMinutes;

        for (var i = 0; i < args.Length; i++)
        {
            var name = NameOf(args[i]);
            var inlineValue = InlineValueOf(args[i]);

            switch (name.ToLowerInvariant())
            {
                case "--silent":
                    break;

                case "--store-id":
                    if (!TryReadValue(args, ref i, inlineValue, out storeId))
                        return ParseResult.Usage("--store-id requires a value.");
                    break;

                case "--timeout-minutes":
                    if (!TryReadValue(args, ref i, inlineValue, out var raw)
                        || !int.TryParse(raw, out timeoutMinutes)
                        || timeoutMinutes <= 0)
                        return ParseResult.Usage("--timeout-minutes requires a positive integer.");
                    break;

                default:
                    return ParseResult.Usage($"Unknown argument: {args[i]}");
            }
        }

        storeId = storeId?.Trim();
        if (string.IsNullOrWhiteSpace(storeId))
            return ParseResult.Usage("--silent requires --store-id <ID>.");

        return ParseResult.Silent(new SilentInstallOptions(storeId, timeoutMinutes));
    }

    private static string NameOf(string arg)
    {
        var eq = arg.IndexOf('=');
        return eq >= 0 ? arg[..eq] : arg;
    }

    private static string? InlineValueOf(string arg)
    {
        var eq = arg.IndexOf('=');
        return eq >= 0 ? arg[(eq + 1)..] : null;
    }

    // Reads the value for a flag: the inline "=value" if present, else the next
    // token (which must exist and not itself be a flag). Advances i past a
    // consumed next token so it is not re-parsed as unknown.
    private static bool TryReadValue(string[] args, ref int i, string? inlineValue, out string? value)
    {
        if (inlineValue is not null)
        {
            value = inlineValue;
            return true;
        }

        if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
        {
            value = args[++i];
            return true;
        }

        value = null;
        return false;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests/IndyPOS.Bootstrapper.Tests.csproj --filter "FullyQualifiedName~SilentArgsTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add installer/IndyPOS.Bootstrapper/Silent/SilentArgs.cs tests/IndyPOS.Bootstrapper.Tests/Silent/SilentArgsTests.cs
git commit -m "feat(installer): add silent-install argument parser"
```

---

### Task 4: `SilentOutcomeMapper`

**Files:**
- Create: `installer/IndyPOS.Bootstrapper/Silent/SilentOutcomeMapper.cs`
- Test: `tests/IndyPOS.Bootstrapper.Tests/Silent/SilentOutcomeMapperTests.cs`

**Interfaces:**
- Produces:
  - `abstract record SilentOutcome` with `InstallSucceeded(bool AdminSeeded, string? CredFile, bool CredLocked, bool ServiceStarted, bool HealthOk)`, `InstallFailed(string Message)`, `InstallTimedOut`, `UsageErrorOutcome(string Message)`, `NotElevatedOutcome`
  - `SilentOutcomeMapper.Map(SilentOutcome outcome) : (int ExitCode, IReadOnlyList<string> Markers)`

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using IndyPOS.Bootstrapper.Silent;
using Xunit;

namespace IndyPOS.Bootstrapper.Tests.Silent;

public class SilentOutcomeMapperTests
{
    [Fact]
    public void Map_WithSeededLockedSuccess_ShouldReturnZeroAndAllMarkers()
    {
        var outcome = new InstallSucceeded(AdminSeeded: true, CredFile: @"C:\x\admin-credentials.txt",
            CredLocked: true, ServiceStarted: true, HealthOk: true);

        var (exit, markers) = SilentOutcomeMapper.Map(outcome);

        exit.Should().Be(0);
        markers.Should().Contain("INDYPOS_MARKER RESULT=success");
        markers.Should().Contain("INDYPOS_MARKER ADMIN_SEEDED=true");
        markers.Should().Contain(@"INDYPOS_MARKER CRED_FILE=C:\x\admin-credentials.txt");
        markers.Should().Contain("INDYPOS_MARKER CRED_LOCKED=true");
        markers.Should().Contain("INDYPOS_MARKER SERVICE_STARTED=true");
        markers.Should().Contain("INDYPOS_MARKER HEALTH=ok");
    }

    [Fact]
    public void Map_WithUnlockedCredFile_ShouldStayZeroButFlagUnlocked()
    {
        var outcome = new InstallSucceeded(true, @"C:\x\admin-credentials.txt", CredLocked: false, true, true);

        var (exit, markers) = SilentOutcomeMapper.Map(outcome);

        exit.Should().Be(0);
        markers.Should().Contain("INDYPOS_MARKER CRED_LOCKED=false");
    }

    [Fact]
    public void Map_WithAdminAlreadyExisting_ShouldOmitCredFileAndEmitResetHint()
    {
        var outcome = new InstallSucceeded(AdminSeeded: false, CredFile: null, CredLocked: false,
            ServiceStarted: true, HealthOk: false);

        var (exit, markers) = SilentOutcomeMapper.Map(outcome);

        exit.Should().Be(0);
        markers.Should().Contain("INDYPOS_MARKER ADMIN_SEEDED=false");
        markers.Should().NotContain(m => m.StartsWith("INDYPOS_MARKER CRED_FILE="));
        markers.Should().Contain(m => m.StartsWith("INDYPOS_MARKER RESET_HINT="));
        markers.Should().Contain("INDYPOS_MARKER HEALTH=failed");
    }

    [Fact]
    public void Map_WithInstallFailed_ShouldReturnTwoAndOnlyResultMarker()
    {
        var (exit, markers) = SilentOutcomeMapper.Map(new InstallFailed("boom"));

        exit.Should().Be(2);
        markers.Should().ContainSingle().Which.Should().Be("INDYPOS_MARKER RESULT=failed");
    }

    [Fact]
    public void Map_WithTimeout_ShouldReturnFour()
    {
        var (exit, markers) = SilentOutcomeMapper.Map(new InstallTimedOut());

        exit.Should().Be(4);
        markers.Should().ContainSingle().Which.Should().Be("INDYPOS_MARKER RESULT=timeout");
    }

    [Fact]
    public void Map_WithUsageError_ShouldReturnOne()
    {
        SilentOutcomeMapper.Map(new UsageErrorOutcome("bad")).ExitCode.Should().Be(1);
    }

    [Fact]
    public void Map_WithNotElevated_ShouldReturnThree()
    {
        SilentOutcomeMapper.Map(new NotElevatedOutcome()).ExitCode.Should().Be(3);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests/IndyPOS.Bootstrapper.Tests.csproj --filter "FullyQualifiedName~SilentOutcomeMapperTests"`
Expected: FAIL — types do not exist.

- [ ] **Step 3: Implement the outcome model + mapper**

Create `installer/IndyPOS.Bootstrapper/Silent/SilentOutcomeMapper.cs`:

```csharp
namespace IndyPOS.Bootstrapper.Silent;

public abstract record SilentOutcome;

public sealed record InstallSucceeded(
    bool AdminSeeded, string? CredFile, bool CredLocked, bool ServiceStarted, bool HealthOk) : SilentOutcome;

public sealed record InstallFailed(string Message) : SilentOutcome;

public sealed record InstallTimedOut : SilentOutcome;

public sealed record UsageErrorOutcome(string Message) : SilentOutcome;

public sealed record NotElevatedOutcome : SilentOutcome;

/// <summary>
/// Pure mapping from a run's outcome to an exit code + the non-secret marker
/// lines. No I/O — every permutation is unit-testable without running an install.
/// </summary>
public static class SilentOutcomeMapper
{
    private const string Prefix = "INDYPOS_MARKER ";
    private const string ResetHint =
        "run \"IndyPOS.StoreHub.exe reset-admin\" from the StoreHub install dir to reissue a bootstrap password";

    public static (int ExitCode, IReadOnlyList<string> Markers) Map(SilentOutcome outcome) => outcome switch
    {
        InstallSucceeded s => (0, SuccessMarkers(s)),
        InstallFailed => (2, [Prefix + "RESULT=failed"]),
        InstallTimedOut => (4, [Prefix + "RESULT=timeout"]),
        UsageErrorOutcome => (1, [Prefix + "RESULT=failed"]),
        NotElevatedOutcome => (3, [Prefix + "RESULT=failed"]),
        _ => throw new ArgumentOutOfRangeException(nameof(outcome))
    };

    private static IReadOnlyList<string> SuccessMarkers(InstallSucceeded s)
    {
        var markers = new List<string>
        {
            Prefix + "RESULT=success",
            Prefix + $"ADMIN_SEEDED={Lower(s.AdminSeeded)}"
        };

        if (s.AdminSeeded)
        {
            markers.Add(Prefix + $"CRED_FILE={s.CredFile}");
            markers.Add(Prefix + $"CRED_LOCKED={Lower(s.CredLocked)}");
        }
        else
        {
            markers.Add(Prefix + $"RESET_HINT={ResetHint}");
        }

        markers.Add(Prefix + $"SERVICE_STARTED={Lower(s.ServiceStarted)}");
        markers.Add(Prefix + $"HEALTH={(s.HealthOk ? "ok" : "failed")}");
        return markers;
    }

    private static string Lower(bool value) => value ? "true" : "false";
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests/IndyPOS.Bootstrapper.Tests.csproj --filter "FullyQualifiedName~SilentOutcomeMapperTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add installer/IndyPOS.Bootstrapper/Silent/SilentOutcomeMapper.cs tests/IndyPOS.Bootstrapper.Tests/Silent/SilentOutcomeMapperTests.cs
git commit -m "feat(installer): add silent outcome->exit/marker mapper"
```

---

### Task 5: `ConsoleAttach` + `SilentInstallLogger`

**Files:**
- Create: `installer/IndyPOS.Bootstrapper/Silent/ConsoleAttach.cs`
- Create: `installer/IndyPOS.Bootstrapper/Silent/SilentInstallLogger.cs`
- Test: `tests/IndyPOS.Bootstrapper.Tests/Silent/SilentInstallLoggerTests.cs`

**Interfaces:**
- Consumes: `IProgress<InstallationProgress>`, `InstallationProgress` (from `IndyPOS.Bootstrapper.Installers`).
- Produces:
  - `ConsoleAttach.TryAttach() : bool` (internal static)
  - `SilentInstallLogger(TextWriter file) : IProgress<InstallationProgress>, IDisposable` with `WriteLine(string)` (timestamped) and `WriteMarker(string)` (raw).

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using IndyPOS.Bootstrapper.Installers;
using IndyPOS.Bootstrapper.Silent;
using Xunit;

namespace IndyPOS.Bootstrapper.Tests.Silent;

public class SilentInstallLoggerTests
{
    [Fact]
    public void Report_WithLogMessage_ShouldWriteTimestampedLineToFile()
    {
        var sink = new StringWriter();
        using var logger = new SilentInstallLogger(sink);

        logger.Report(InstallationProgress.Log("Installing PostgreSQL"));

        sink.ToString().Should().Contain("Installing PostgreSQL");
    }

    [Fact]
    public void Report_WithErrorProgress_ShouldPrefixWarning()
    {
        var sink = new StringWriter();
        using var logger = new SilentInstallLogger(sink);

        logger.Report(InstallationProgress.Error("service failed"));

        sink.ToString().Should().Contain("WARNING: service failed");
    }

    [Fact]
    public void WriteMarker_ShouldWriteRawLineWithoutTimestamp()
    {
        var sink = new StringWriter();
        using var logger = new SilentInstallLogger(sink);

        logger.WriteMarker("INDYPOS_MARKER RESULT=success");

        var lines = sink.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        lines.Should().Contain("INDYPOS_MARKER RESULT=success");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests/IndyPOS.Bootstrapper.Tests.csproj --filter "FullyQualifiedName~SilentInstallLoggerTests"`
Expected: FAIL — types do not exist.

- [ ] **Step 3: Implement `ConsoleAttach`**

Create `installer/IndyPOS.Bootstrapper/Silent/ConsoleAttach.cs`:

```csharp
using System.Runtime.InteropServices;

namespace IndyPOS.Bootstrapper.Silent;

/// <summary>
/// Best-effort attach to the launching console so Console output reaches the
/// caller. A no-op (returns false) when there is no parent console — e.g. a
/// PowerShell Direct host — in which case the durable log file is the record.
/// </summary>
internal static partial class ConsoleAttach
{
    private const int ATTACH_PARENT_PROCESS = -1;

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AttachConsole(int dwProcessId);

    public static bool TryAttach()
    {
        try { return AttachConsole(ATTACH_PARENT_PROCESS); }
        catch { return false; }
    }
}
```

- [ ] **Step 4: Implement `SilentInstallLogger`**

Create `installer/IndyPOS.Bootstrapper/Silent/SilentInstallLogger.cs`:

```csharp
using IndyPOS.Bootstrapper.Installers;

namespace IndyPOS.Bootstrapper.Silent;

/// <summary>
/// Thread-safe progress sink for the silent installer. Each line goes to the
/// durable log file (authoritative) and, best-effort, stdout. Report runs inline
/// on threadpool continuation threads, so all writes are serialized under a lock
/// and flushed per line — an early crash still leaves a diagnosable log.
/// </summary>
public sealed class SilentInstallLogger(TextWriter file) : IProgress<InstallationProgress>, IDisposable
{
    private readonly object _gate = new();

    public void Report(InstallationProgress value)
    {
        if (!string.IsNullOrEmpty(value.LogMessage))
        {
            WriteLine((value.IsError ? "WARNING: " : string.Empty) + value.LogMessage);
        }
        else if (!string.IsNullOrEmpty(value.StepName))
        {
            WriteLine($"[{value.StepName}] {value.StatusMessage}");
        }
    }

    public void WriteLine(string line) => Emit($"[{DateTime.Now:HH:mm:ss}] {line}");

    public void WriteMarker(string marker) => Emit(marker);

    private void Emit(string text)
    {
        lock (_gate)
        {
            file.WriteLine(text);
            file.Flush();
            try { Console.Out.WriteLine(text); } catch { /* no console attached */ }
        }
    }

    public void Dispose()
    {
        lock (_gate) { file.Dispose(); }
    }
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests/IndyPOS.Bootstrapper.Tests.csproj --filter "FullyQualifiedName~SilentInstallLoggerTests"`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add installer/IndyPOS.Bootstrapper/Silent/ConsoleAttach.cs installer/IndyPOS.Bootstrapper/Silent/SilentInstallLogger.cs tests/IndyPOS.Bootstrapper.Tests/Silent/SilentInstallLoggerTests.cs
git commit -m "feat(installer): add console-attach + thread-safe silent install logger"
```

---

### Task 6: `InstallationResult` additive fields

**Files:**
- Modify: `installer/IndyPOS.Bootstrapper/Installers/InstallationOrchestrator.cs:290-296,336-340`
- Test: `tests/IndyPOS.Bootstrapper.Tests/Installers/InstallationResultTests.cs` (new)

**Interfaces:**
- Produces: `InstallationResult.ServiceStarted : bool` and `InstallationResult.HealthOk : bool` (init-only).

- [ ] **Step 1: Write the failing test**

Create `tests/IndyPOS.Bootstrapper.Tests/Installers/InstallationResultTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Bootstrapper.Installers;
using Xunit;

namespace IndyPOS.Bootstrapper.Tests.Installers;

public class InstallationResultTests
{
    [Fact]
    public void InstallationResult_ShouldCarryServiceAndHealthFlags()
    {
        var result = new InstallationResult { AdminSeeded = true, ServiceStarted = true, HealthOk = false };

        result.AdminSeeded.Should().BeTrue();
        result.ServiceStarted.Should().BeTrue();
        result.HealthOk.Should().BeFalse();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests/IndyPOS.Bootstrapper.Tests.csproj --filter "FullyQualifiedName~InstallationResultTests"`
Expected: FAIL — `ServiceStarted`/`HealthOk` do not exist.

- [ ] **Step 3: Add the fields and populate them**

In `installer/IndyPOS.Bootstrapper/Installers/InstallationOrchestrator.cs`, replace the `InstallationResult` class (lines 336-340) with:

```csharp
/// <summary>Outcome of a successful installation, surfaced to the wizard's finish screen.</summary>
public class InstallationResult
{
    public bool AdminSeeded { get; init; }
    public bool ServiceStarted { get; init; }
    public bool HealthOk { get; init; }
}
```

Then update the single `return` at line 295 to populate them:

```csharp
        return new InstallationResult
        {
            AdminSeeded = provisionResult.AdminSeeded,
            ServiceStarted = startResult.Success,
            HealthOk = healthOk
        };
```

(`startResult.Success` and `healthOk` are already in scope at this point — assigned at lines 249 and 263.)

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests/IndyPOS.Bootstrapper.Tests.csproj --filter "FullyQualifiedName~InstallationResultTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add installer/IndyPOS.Bootstrapper/Installers/InstallationOrchestrator.cs tests/IndyPOS.Bootstrapper.Tests/Installers/InstallationResultTests.cs
git commit -m "feat(installer): surface ServiceStarted/HealthOk on InstallationResult"
```

---

### Task 7: `Elevation`, `SilentInstaller`, and the `Program` entry branch

**Files:**
- Create: `installer/IndyPOS.Bootstrapper/Silent/Elevation.cs`
- Create: `installer/IndyPOS.Bootstrapper/Silent/SilentInstaller.cs`
- Modify: `installer/IndyPOS.Bootstrapper/Program.cs`
- Test: `tests/IndyPOS.Bootstrapper.Tests/Silent/SilentInstallerTests.cs`

**Interfaces:**
- Consumes: `SilentArgs`, `ParseResult`, `ParseStatus`, `SilentInstallOptions`, `SilentOutcomeMapper`, all `SilentOutcome` records, `SilentInstallLogger`, `ConsoleAttach`, `SecretScrubber`, `AdminCredentialFile`, `InstallationOrchestrator`, `InstallationConfig`, `DatabaseSetup.TryRestrictFilePermissions`.
- Produces: `Elevation.IsElevated() : bool`; `SilentInstaller.Run(ParseResult parse) : int`.

- [ ] **Step 1: Write the failing test**

The install path is VM-validated (Task 9); the deterministic unit seam is the pre-install branch — a usage error returns exit 1 without touching the orchestrator or filesystem.

```csharp
using FluentAssertions;
using IndyPOS.Bootstrapper.Silent;
using Xunit;

namespace IndyPOS.Bootstrapper.Tests.Silent;

public class SilentInstallerTests
{
    [Fact]
    public void Run_WithUsageErrorParseResult_ShouldReturnExitOne()
    {
        var parse = ParseResult.Usage("--silent requires --store-id <ID>.");

        var exit = SilentInstaller.Run(parse);

        exit.Should().Be(1);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests/IndyPOS.Bootstrapper.Tests.csproj --filter "FullyQualifiedName~SilentInstallerTests"`
Expected: FAIL — `SilentInstaller` does not exist.

- [ ] **Step 3: Implement `Elevation`**

Create `installer/IndyPOS.Bootstrapper/Silent/Elevation.cs`:

```csharp
using System.Security.Principal;

namespace IndyPOS.Bootstrapper.Silent;

/// <summary>Shared elevation check for the interactive and silent entry paths.</summary>
public static class Elevation
{
    public static bool IsElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }
}
```

- [ ] **Step 4: Implement `SilentInstaller`**

Create `installer/IndyPOS.Bootstrapper/Silent/SilentInstaller.cs`:

```csharp
using IndyPOS.Bootstrapper.Installers;

namespace IndyPOS.Bootstrapper.Silent;

/// <summary>
/// Headless entry point for `--silent --store-id <ID>`. Reuses
/// InstallationOrchestrator unchanged; the durable log file is the authoritative
/// result/marker channel and the exit code is the automation contract.
/// </summary>
public static class SilentInstaller
{
    public static int Run(ParseResult parse)
    {
        ConsoleAttach.TryAttach();

        if (parse.Status == ParseStatus.UsageError)
            return Emit(new UsageErrorOutcome(parse.ErrorMessage ?? "Invalid arguments."), logger: null);

        if (!Elevation.IsElevated())
            return Emit(new NotElevatedOutcome(), logger: null);

        var options = parse.Options!;
        var config = new InstallationConfig { StoreId = options.StoreId };

        var (logPath, writer) = OpenLog(config);
        var logger = new SilentInstallLogger(writer);

        var outcome = RunInstall(config, options, logger);
        var exit = Emit(outcome, logger);

        // Dispose (flush + close the file) BEFORE copying to install-latest.log.
        logger.Dispose();
        CopyLatest(config, logPath);
        return exit;
    }

    private static SilentOutcome RunInstall(
        InstallationConfig config, SilentInstallOptions options, SilentInstallLogger logger)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(options.TimeoutMinutes));

        InstallationResult result;
        try
        {
            result = new InstallationOrchestrator()
                .InstallAsync(config, logger, cts.Token)
                .GetAwaiter().GetResult();
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            logger.WriteLine("ERROR: installation timed out.");
            return new InstallTimedOut();
        }
        catch (Exception ex)
        {
            var message = SecretScrubber.Scrub(ex.Message);
            logger.WriteLine("ERROR: " + message);
            return new InstallFailed(message);
        }

        if (!result.AdminSeeded)
            return new InstallSucceeded(false, null, false, result.ServiceStarted, result.HealthOk);

        var (path, locked) = AdminCredentialFile.Write(config);
        return new InstallSucceeded(true, path, locked, result.ServiceStarted, result.HealthOk);
    }

    private static int Emit(SilentOutcome outcome, SilentInstallLogger? logger)
    {
        var (exit, markers) = SilentOutcomeMapper.Map(outcome);
        foreach (var marker in markers)
        {
            if (logger is not null) logger.WriteMarker(marker);
            else TryConsole(marker);
        }
        return exit;
    }

    private static (string LogPath, TextWriter Writer) OpenLog(InstallationConfig config)
    {
        Directory.CreateDirectory(config.LogsDirectory);
        var logPath = Path.Combine(
            config.LogsDirectory, $"install-{DateTime.Now:yyyyMMdd-HHmmss}-{Environment.ProcessId}.log");
        File.WriteAllText(logPath, string.Empty);
        DatabaseSetup.TryRestrictFilePermissions(logPath);

        var stream = new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.Read);
        return (logPath, new StreamWriter(stream) { AutoFlush = true });
    }

    private static void CopyLatest(InstallationConfig config, string logPath)
    {
        try
        {
            var latest = Path.Combine(config.LogsDirectory, "install-latest.log");
            File.Copy(logPath, latest, overwrite: true);
            DatabaseSetup.TryRestrictFilePermissions(latest);
        }
        catch { /* best-effort convenience copy */ }
    }

    private static void TryConsole(string text)
    {
        try { Console.Out.WriteLine(text); } catch { /* no console attached */ }
    }
}
```

- [ ] **Step 5: Wire the `Program` entry branch**

Replace the entire contents of `installer/IndyPOS.Bootstrapper/Program.cs` with:

```csharp
using IndyPOS.Bootstrapper.Silent;
using IndyPOS.Bootstrapper.UI;

namespace IndyPOS.Bootstrapper;

internal static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        var parse = SilentArgs.Parse(args);
        if (parse.Status != ParseStatus.NotSilent)
        {
            // Headless path: no message loop, no ApplicationConfiguration.Initialize.
            return SilentInstaller.Run(parse);
        }

        ApplicationConfiguration.Initialize();

        if (!Elevation.IsElevated())
        {
            MessageBox.Show(
                "IndyPOS Setup requires administrator privileges.\n\n" +
                "Please right-click and select 'Run as administrator'.",
                "Administrator Required",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return 0;
        }

        Application.Run(new InstallationWizard());
        return 0;
    }
}
```

- [ ] **Step 6: Run test + build to verify**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests/IndyPOS.Bootstrapper.Tests.csproj --filter "FullyQualifiedName~SilentInstallerTests"`
Expected: PASS.

Run: `dotnet build installer/IndyPOS.Bootstrapper/IndyPOS.Bootstrapper.csproj -c Release`
Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 7: Commit**

```bash
git add installer/IndyPOS.Bootstrapper/Silent/Elevation.cs installer/IndyPOS.Bootstrapper/Silent/SilentInstaller.cs installer/IndyPOS.Bootstrapper/Program.cs tests/IndyPOS.Bootstrapper.Tests/Silent/SilentInstallerTests.cs
git commit -m "feat(installer): headless silent installer entry path"
```

---

### Task 8: Wire silent mode into the VM harness

**Files:**
- Modify: `scripts/vm-testing/VMTestConfig.psd1`
- Modify: `scripts/vm-testing/Reset-AndInstall.ps1:230-268,326`

**Interfaces:**
- Consumes: `SilentInstaller` via `IndyPOS-Setup.exe --silent --store-id <TestStoreId>`; markers from `install-latest.log`.

- [ ] **Step 1: Add the test store id to the harness config**

In `scripts/vm-testing/VMTestConfig.psd1`, add a `TestStoreId` entry (match the value used in the manual runs):

```powershell
    TestStoreId = 'Rungrat-001'
```

- [ ] **Step 2: Replace the wizard handoff with a silent invocation**

In `scripts/vm-testing/Reset-AndInstall.ps1`, replace the entire `Invoke-WizardHandoff` function (lines 230-268) with:

```powershell
function Invoke-SilentInstall {
    param($Credential)

    Write-Section '5. Run installer (silent)'

    $guestInstaller = Join-Path $Config.GuestStagingDir $Config.GuestInstallerName
    $storeId        = $Config.TestStoreId
    $logDir         = "C:\ProgramData\IndyPOS\v4\logs"
    $latestLog      = Join-Path $logDir 'install-latest.log'

    Write-Info "Running (in guest): $guestInstaller --silent --store-id $storeId"

    $run = Invoke-Command -VMName $Config.VMName -Credential $Credential -ScriptBlock {
        param($exe, $id)
        $p = Start-Process -FilePath $exe -ArgumentList '--silent', '--store-id', $id -Wait -PassThru
        [pscustomobject]@{ ExitCode = $p.ExitCode }
    } -ArgumentList $guestInstaller, $storeId

    Write-Info "Installer exit code: $($run.ExitCode)"

    $markers = Invoke-Command -VMName $Config.VMName -Credential $Credential -ScriptBlock {
        param($p)
        if (Test-Path $p) { Get-Content $p | Where-Object { $_ -like 'INDYPOS_MARKER *' } } else { @() }
    } -ArgumentList $latestLog

    # Parse the LAST occurrence of each marker key (D9).
    $marker = @{}
    foreach ($line in $markers) {
        if ($line -match '^INDYPOS_MARKER\s+([A-Z_]+)=(.*)$') { $marker[$Matches[1]] = $Matches[2] }
    }
    foreach ($k in $marker.Keys) { Write-Info "marker $k=$($marker[$k])" }

    # Authoritative gating rule: exit code + RESULT + service.
    $failed = ($run.ExitCode -ne 0) -or ($marker['RESULT'] -ne 'success') -or ($marker['SERVICE_STARTED'] -eq 'false')
    if ($failed) {
        throw "Silent install failed (exit=$($run.ExitCode), RESULT=$($marker['RESULT']), SERVICE_STARTED=$($marker['SERVICE_STARTED'])). See $latestLog in the guest."
    }
    if ($marker['CRED_LOCKED'] -eq 'false') {
        Write-Warn2 "Credential file could not be ACL-locked (CRED_LOCKED=false)."
    }
    Write-OK "Silent install completed (RESULT=success)."
}
```

- [ ] **Step 3: Call the new function from Main**

In `scripts/vm-testing/Reset-AndInstall.ps1`, replace line 326 `Invoke-WizardHandoff -Credential $cred` with:

```powershell
    Invoke-SilentInstall -Credential $cred
```

- [ ] **Step 4: Parse-check the script**

Run: `pwsh -NoProfile -Command "$null = [System.Management.Automation.Language.Parser]::ParseFile('scripts/vm-testing/Reset-AndInstall.ps1', [ref]$null, [ref]$null); 'parse-ok'"`
Expected: prints `parse-ok` (no parse errors).

- [ ] **Step 5: Commit**

```bash
git add scripts/vm-testing/VMTestConfig.psd1 scripts/vm-testing/Reset-AndInstall.ps1
git commit -m "test(installer): drive VM harness via silent install (no vmconnect handoff)"
```

---

### Task 9: Full build, installer rebuild, and VM validation

**Files:**
- No source changes — verification only.

- [ ] **Step 1: Run the full bootstrapper test suite**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests/IndyPOS.Bootstrapper.Tests.csproj`
Expected: all pass (prior baseline 52 pass / 8 skip, plus the new silent-path tests; 0 failures).

- [ ] **Step 2: Build the whole solution**

Run: `dotnet build -c Release`
Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 3: Rebuild the distributable installer**

Run: `pwsh -File scripts/publish.ps1` then `pwsh -File installer/build-installer.ps1`
Expected: `publish\IndyPOS-Setup.exe` produced (~190 MB), v4.0.0.0.

- [ ] **Step 4: Run the unattended VM clean-install cycle**

Run: `pwsh -File scripts/vm-testing/Reset-AndInstall.ps1 -KeepRunning`
Expected: no vmconnect handoff; `RESULT: PASS` from the verifier; installer exit 0; markers show `RESULT=success`, `ADMIN_SEEDED=true`, `SERVICE_STARTED=true`, `CRED_LOCKED=true`.

- [ ] **Step 5: Confirm the credential + force-change flow (manual, in-guest)**

Read the cred file path from the `CRED_FILE` marker, read the file (as admin in-guest), and confirm login → forced password change works via the WinForms app (as in the prior manual VM validation). Confirm a re-run of the silent installer over the same install emits `ADMIN_SEEDED=false` + a `RESET_HINT` marker and writes no new cred file.

- [ ] **Step 6: Commit any doc/status updates**

```bash
git add .claude/STATUS.md
git commit -m "docs(indypos): silent installer mode validated in VM"
```

---

## Notes for the implementer

- **Do not** modify `InstallationOrchestrator`'s install steps — only the additive result fields in Task 6.
- The `--silent` path must never call `Application.Run` or show a `MessageBox` — an unattended run would hang.
- Markers on the failure/timeout paths are intentionally minimal (`RESULT=failed|timeout` only); the harness parser tolerates missing markers.
- `DateTime.Now` is fine in this application code (the workflow-script restriction does not apply here).
