# Installer In-Place Upgrade Support — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `IndyPOS-Setup.exe --silent` upgrade a machine that already runs IndyPOS, instead of failing destructively, while leaving the fresh-install path byte-for-byte unchanged.

**Architecture:** A detector classifies the machine as `Fresh` / `Upgrade` / `Unusable`, and a single router in the silent entry point picks one of two orchestrators. `InstallationOrchestrator` is renamed `FreshInstallOrchestrator` and frozen; a new `UpgradeOrchestrator` runs a different algorithm (back up → deploy → restore config → migrate → start → verify, with rollback). Both compose the same extracted step units: `ServiceControl`, `StoreHubPayload`, `MigrationRunner`, `HealthProbe`, `ConfigSnapshot`. `DatabaseSetup` never runs on an upgrade.

**Tech Stack:** C# .NET 10 (`net10.0-windows`), xUnit + FluentAssertions + NSubstitute, PowerShell 7 on the host / PowerShell 5.1 in the guest, PostgreSQL 18 (`pg_dump`/`pg_restore`), Velopack 0.0.1298, DPAPI via `IndyPOS.Vault.SecretProtector`.

**Spec:** `docs/superpowers/specs/2026-07-25-installer-upgrade-support-design.md` (no open questions; §12 resolved by the 2026-07-26 VM spike).

## Global Constraints

- **The fresh-install path must not change behaviour.** After the extraction tasks, `git diff` on `FreshInstallOrchestrator.cs` must show *only* the rename and delegation to extracted units — no reordering, no new conditionals. Its fresh-only branches (`sc create`, first-time directory creation, `DatabaseSetup`, the Postgres install) never execute on the upgrade VM snapshot, so no amount of upgrade testing would catch a regression there.
- **`DatabaseSetup` is fresh-install only.** It must never be called from the upgrade path. The role, password and DPAPI connection string already exist on an upgrade.
- **The PostgreSQL superuser password is never persisted.** Decided 2026-07-25. No task may add storage for it.
- **Migrations are forward-only.** Every migration in a release must be old-binary-compatible: additive only, new columns nullable or defaulted, no renames, no drops. Rollback restores binaries and config, not schema.
- **Secrets never reach markers or logs.** All outbound text passes through `SecretScrubber.Scrub`. Markers are the `INDYPOS_MARKER <KEY>=<value>` format only.
- **No new `RESULT` values.** The existing set is `success` / `failed` / `timeout` (`InstallTimedOut` already emits the third — the spec's "keeps its two values" predates checking the code). Every upgrade outcome reuses `success` or `failed`, so the harness gating rule is unchanged. Other markers say *what* succeeded.
- **Exit codes are a frozen contract for 0–4**: `0` success, `1` usage error, `2` failed, `3` not elevated, `4` timeout. New outcomes take `5` and `6`.
- **Config path constants:** StoreHub config lives at `<StoreHubInstallPath>\appsettings.json`. Its JSON is camelCase: `connectionStrings["storehub-db"]`, `store.id`, `store.type`. Read keys **case-insensitively** — ASP.NET config binding is case-insensitive and a hand-edited file may use PascalCase.
- **DPAPI entropy** is `SHA256("IndyPOS:" + key)` at `LocalMachine` scope; the connection-string key is exactly `ConnectionStrings:storehub-db`.
- **Test naming:** `Method_Condition_ShouldExpectedBehavior`. xUnit `[Fact]` / `[Theory]` + `[InlineData]`, FluentAssertions, Arrange-Act-Assert separated by blank lines. No `#region`.
- **Guest scripts must be ASCII or BOM-encoded.** The VM runs PowerShell 5.1 with a Windows-1252 codepage; em-dashes and other non-ASCII characters make a script unparseable there.
- **Commit style:** conventional commits, one logical unit per commit.

---

## File Structure

**New — `installer/IndyPOS.Bootstrapper/Upgrade/`**

| File | Responsibility |
|---|---|
| `StoreHubConfigReader.cs` | Reads facts out of an existing `appsettings.json`: store id, store type, whether the connection string is usable. Shared by detection and preflight — one code path, per spec §3. |
| `InstallMode.cs` | `InstallMode` enum + `DetectedInstall` record. Data only. |
| `IInstallProbe.cs` | The machine-state seam the detector reads through (manifest, service ImagePath, Postgres/database presence, config facts) + its Windows implementation. |
| `InstallModeDetector.cs` | The four ordered rules. Pure given a probe. |
| `PosAppVersion.cs` | Reads Velopack's `current\sq.version` to derive `POS_UPDATED` (spec §12). |
| `UpgradeBackup.cs` | `pg_dump` + StoreHub tree copy, ACL lock, integrity check, retention prune, and delete-then-copy restore. |
| `UpgradePreflight.cs` | Step 1 of the sequence: downgrade refusal, `pg_dump` location, free space, running-app check, prerequisite ensures. |
| `UpgradeOrchestrator.cs` | The upgrade sequence and its failure/rollback handling. |
| `SimulatedFailure.cs` | Runtime-selected fault hook so one installer build serves VM cases 1 and 2. |

**New — `installer/IndyPOS.Bootstrapper/Installers/`** (extracted shared units)

| File | Responsibility |
|---|---|
| `ServiceControl.cs` | Stop / start / query the StoreHub Windows service. Used by both paths. |
| `StoreHubPayload.cs` | Extract the embedded StoreHub payload over an install directory. Used by both paths. |
| `MigrationRunner.cs` | Runs `IndyPOS.StoreHub.exe migrate`. Used by both paths. Deliberately *not* named `SchemaProvisioner` — `DatabaseSetup` is fresh-only and the two must never be confused. |
| `HealthProbe.cs` | Polls `/health/ready`. Used by both paths. |

**Modified**

| File | Change |
|---|---|
| `Installers/InstallationOrchestrator.cs` → `Installers/FreshInstallOrchestrator.cs` | Rename + delegate to extracted units. Frozen thereafter. |
| `Installers/StoreHubInstaller.cs` | Delegates to `ServiceControl` / `StoreHubPayload` / `MigrationRunner`. |
| `Installers/DatabaseSetup.cs` | Fresh-only banner; superuser-guard message must stop recommending `cleanup-v4.ps1 -Force -RemovePostgres` when a store database exists (spec §8). |
| `Installers/PostgresInstaller.cs` | Expose `FindPostgresInstallation` (currently `private static` returning a private nested type) so `UpgradePreflight` can fall back to it. |
| `Silent/SilentOutcomeMapper.cs` | New sibling outcome records + exit codes; no widening of `InstallSucceeded`. |
| `Silent/SilentArgs.cs` | `--store-id` optional; `--simulate-failure` hidden flag. |
| `Silent/SilentInstaller.cs` | Becomes the single router: detect once, emit `MODE=`, dispatch. |
| `UI/InstallationWizard.cs`, `Program.cs` | Wizard detects an existing install and refuses with the `--silent` command. |
| `scripts/vm-testing/Reset-AndInstall.ps1` | `-SnapshotName`, expect-rollback mode, assert against the fresh timestamped log. |
| `scripts/vm-testing/Test-IndyPOSInstallation.ps1` | Scripted database assertions via the DPAPI-decrypted connection string. |
| `docs/operations/upgrade-procedure.md` (new), `update-procedure.md` | Operator runbook, `Unusable` recovery per rule, `pg_restore` command, forward-only migration release gate. |

**Tests — `tests/IndyPOS.Bootstrapper.Tests/Upgrade/`**: one file per unit, matching the existing `Installers/` and `Silent/` layout.

---

## Task 1: StoreHubConfigReader

The one code path that answers "what does this machine's existing StoreHub config say?". Detection (§3) and preflight (§4 step 1) both consume it, so a disagreement between them is impossible by construction.

**Files:**
- Create: `installer/IndyPOS.Bootstrapper/Upgrade/StoreHubConfigReader.cs`
- Test: `tests/IndyPOS.Bootstrapper.Tests/Upgrade/StoreHubConfigReaderTests.cs`

**Interfaces:**
- Consumes: `IndyPOS.Vault.SecretProtector.Unprotect(string key, string value)`, `IndyPOS.Domain.Enums.StoreType`
- Produces:
  - `sealed record StoreHubConfigFacts(bool Exists, bool ConnectionStringUsable, string? StoreId, StoreType? StoreType)`
  - `static class StoreHubConfigReader` with `const string ConnectionStringKey = "ConnectionStrings:storehub-db"` and
    `static StoreHubConfigFacts Read(string appSettingsPath, Func<string, string, string>? unprotect = null)`

- [ ] **Step 1: Write the failing tests**

Create `tests/IndyPOS.Bootstrapper.Tests/Upgrade/StoreHubConfigReaderTests.cs`:

```csharp
using System.Security.Cryptography;
using FluentAssertions;
using IndyPOS.Bootstrapper.Upgrade;
using IndyPOS.Domain.Enums;

namespace IndyPOS.Bootstrapper.Tests.Upgrade;

public class StoreHubConfigReaderTests : IDisposable
{
    private readonly string _dir =
        Path.Combine(Path.GetTempPath(), "indypos-cfgread-" + Guid.NewGuid().ToString("N"));

    public StoreHubConfigReaderTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    // Tests never touch real DPAPI: a machine-scoped blob cannot be authored
    // portably, and the reader's contract is "whatever unprotect does".
    private static string PassThrough(string key, string value) => value;

    private static string Fail(string key, string value) =>
        throw new CryptographicException("Key not valid for use in specified state.");

    private string Write(string json)
    {
        var path = Path.Combine(_dir, "appsettings.json");
        File.WriteAllText(path, json);
        return path;
    }

    [Fact]
    public void Read_WhenFileIsAbsent_ShouldReportNotExists()
    {
        var facts = StoreHubConfigReader.Read(Path.Combine(_dir, "appsettings.json"), PassThrough);

        facts.Exists.Should().BeFalse();
        facts.ConnectionStringUsable.Should().BeFalse();
        facts.StoreId.Should().BeNull();
        facts.StoreType.Should().BeNull();
    }

    [Fact]
    public void Read_WithARealStoreConfig_ShouldReturnEveryFact()
    {
        var path = Write("""
        {
          "connectionStrings": { "storehub-db": "DPAPI:AQAAANCM" },
          "store": { "id": "Rungrat-001", "type": "Minimart" }
        }
        """);

        var facts = StoreHubConfigReader.Read(path, PassThrough);

        facts.Exists.Should().BeTrue();
        facts.ConnectionStringUsable.Should().BeTrue();
        facts.StoreId.Should().Be("Rungrat-001");
        facts.StoreType.Should().Be(StoreType.Minimart);
    }

    [Fact]
    public void Read_WithPascalCaseKeys_ShouldStillBindThem()
    {
        // ASP.NET config binding is case-insensitive, so a hand-edited file is valid.
        var path = Write("""
        {
          "ConnectionStrings": { "storehub-db": "Host=127.0.0.1" },
          "Store": { "Id": "Rungrat-001", "Type": "GeneralHardware" }
        }
        """);

        var facts = StoreHubConfigReader.Read(path, PassThrough);

        facts.StoreId.Should().Be("Rungrat-001");
        facts.StoreType.Should().Be(StoreType.GeneralHardware);
    }

    [Fact]
    public void Read_WhenDpapiValueFailsToDecrypt_ShouldReportUnusable()
    {
        // A value protected on another machine. That store is Unusable, not upgradeable.
        var path = Write("""
        {
          "connectionStrings": { "storehub-db": "DPAPI:AQAAANCM" },
          "store": { "id": "Rungrat-001", "type": "Minimart" }
        }
        """);

        var facts = StoreHubConfigReader.Read(path, Fail);

        facts.Exists.Should().BeTrue();
        facts.ConnectionStringUsable.Should().BeFalse();
    }

    [Fact]
    public void Read_WhenDpapiValueIsNotBase64_ShouldReportUnusable()
    {
        // Unprotect throws FormatException, not CryptographicException, on bad base64.
        var path = Write("""{ "connectionStrings": { "storehub-db": "DPAPI:not-base64!!" } }""");

        var facts = StoreHubConfigReader.Read(
            path, (_, _) => throw new FormatException("Invalid base64."));

        facts.ConnectionStringUsable.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Read_WithBlankConnectionString_ShouldReportUnusable(string value)
    {
        var path = Write($$"""{ "connectionStrings": { "storehub-db": "{{value}}" } }""");

        StoreHubConfigReader.Read(path, PassThrough).ConnectionStringUsable.Should().BeFalse();
    }

    [Fact]
    public void Read_WithBlankStoreId_ShouldReturnNullStoreId()
    {
        // Empty is as dangerous as absent: StoreIdentityService falls back to the
        // machine name, which would orphan the store's sales history.
        var path = Write("""{ "store": { "id": "   ", "type": "Minimart" } }""");

        StoreHubConfigReader.Read(path, PassThrough).StoreId.Should().BeNull();
    }

    [Fact]
    public void Read_WithAbsentStoreType_ShouldReturnNullStoreType()
    {
        // Stores installed before 2026-07-18 have no Store:Type key.
        var path = Write("""{ "store": { "id": "Rungrat-001" } }""");

        StoreHubConfigReader.Read(path, PassThrough).StoreType.Should().BeNull();
    }

    [Fact]
    public void Read_WithUnparseableStoreType_ShouldReturnNullStoreType()
    {
        var path = Write("""{ "store": { "id": "Rungrat-001", "type": "Bakery" } }""");

        StoreHubConfigReader.Read(path, PassThrough).StoreType.Should().BeNull();
    }

    [Fact]
    public void Read_WithMalformedJson_ShouldReportExistsButUnusable()
    {
        var path = Write("{ this is not json");

        var facts = StoreHubConfigReader.Read(path, PassThrough);

        facts.Exists.Should().BeTrue();
        facts.ConnectionStringUsable.Should().BeFalse();
        facts.StoreId.Should().BeNull();
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests --filter FullyQualifiedName~StoreHubConfigReaderTests`
Expected: FAIL — `StoreHubConfigReader` does not exist (compile error CS0246).

- [ ] **Step 3: Write the implementation**

Create `installer/IndyPOS.Bootstrapper/Upgrade/StoreHubConfigReader.cs`:

```csharp
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using IndyPOS.Domain.Enums;
using IndyPOS.Vault;

namespace IndyPOS.Bootstrapper.Upgrade;

/// <summary>
/// What an existing StoreHub <c>appsettings.json</c> says about the store on this
/// machine. Detection (spec section 3) and upgrade preflight (section 4 step 1) both read
/// through this one type, so the two can never disagree about whether a store is
/// upgradeable.
/// </summary>
public sealed record StoreHubConfigFacts(
    bool Exists,
    bool ConnectionStringUsable,
    string? StoreId,
    StoreType? StoreType);

public static class StoreHubConfigReader
{
    /// <summary>Config key the connection string is DPAPI-bound to. Entropy depends on it.</summary>
    public const string ConnectionStringKey = "ConnectionStrings:storehub-db";

    private static readonly StoreHubConfigFacts Absent = new(false, false, null, null);

    /// <param name="unprotect">
    /// Seam for the DPAPI round-trip, so tests need not author a machine-scoped blob.
    /// Defaults to <see cref="SecretProtector.Unprotect"/>.
    /// </param>
    public static StoreHubConfigFacts Read(
        string appSettingsPath,
        Func<string, string, string>? unprotect = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appSettingsPath);

        if (!File.Exists(appSettingsPath))
        {
            return Absent;
        }

        JsonObject? root;
        try
        {
            root = JsonNode.Parse(File.ReadAllText(appSettingsPath))?.AsObject();
        }
        catch (Exception ex) when (ex is JsonException or IOException or InvalidOperationException)
        {
            // The file is there but unreadable — that is an Unusable store, not a fresh one.
            return new StoreHubConfigFacts(Exists: true, false, null, null);
        }

        if (root is null)
        {
            return new StoreHubConfigFacts(Exists: true, false, null, null);
        }

        var store = Section(root, "store");

        return new StoreHubConfigFacts(
            Exists: true,
            ConnectionStringUsable: IsConnectionStringUsable(root, unprotect ?? SecretProtector.Unprotect),
            StoreId: NonBlank(Value(store, "id")),
            StoreType: ParseStoreType(Value(store, "type")));
    }

    private static bool IsConnectionStringUsable(JsonObject root, Func<string, string, string> unprotect)
    {
        var value = Value(Section(root, "connectionStrings"), "storehub-db");

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (!SecretProtector.IsProtected(value))
        {
            return true;
        }

        try
        {
            return !string.IsNullOrWhiteSpace(unprotect(ConnectionStringKey, value));
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            // Sealed on another machine. Nothing here can recover it.
            return false;
        }
    }

    // Config binding is case-insensitive, so a hand-edited PascalCase file must still bind.
    private static JsonObject? Section(JsonObject root, string name) =>
        root.FirstOrDefault(p => string.Equals(p.Key, name, StringComparison.OrdinalIgnoreCase))
            .Value as JsonObject;

    private static string? Value(JsonObject? section, string name) =>
        section?.FirstOrDefault(p => string.Equals(p.Key, name, StringComparison.OrdinalIgnoreCase))
               .Value?.GetValue<string>();

    private static string? NonBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    // Only a defined enum NAME counts; Enum.TryParse would also accept "2".
    private static StoreType? ParseStoreType(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var matched = Enum.GetNames<StoreType>()
            .FirstOrDefault(n => n.Equals(value, StringComparison.OrdinalIgnoreCase));

        return matched is null ? null : Enum.Parse<StoreType>(matched);
    }
}
```

Add `using System.Text.Json;` for `JsonException`.

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests --filter FullyQualifiedName~StoreHubConfigReaderTests`
Expected: PASS, 10 tests.

- [ ] **Step 5: Commit**

```bash
git add installer/IndyPOS.Bootstrapper/Upgrade/StoreHubConfigReader.cs tests/IndyPOS.Bootstrapper.Tests/Upgrade/StoreHubConfigReaderTests.cs
git commit -m "feat(installer): read store facts from an existing StoreHub config"
```

---

## Task 2: Close the cleanup-script data-loss path

Spec §8, and independent of everything else. The superuser guard currently tells the operator to run `scripts\cleanup-v4.ps1 -Force -RemovePostgres`, which calls `DROP DATABASE IF EXISTS indypos_storehub` unconditionally unless `-SkipDatabase`. An operator following the installer's own guidance destroys every sale the store has recorded.

**Files:**
- Modify: `installer/IndyPOS.Bootstrapper/Installers/DatabaseSetup.cs:9-18` (banner), `:38-50` (guard message)
- Test: `tests/IndyPOS.Bootstrapper.Tests/Installers/DatabaseSetupTests.cs`

**Interfaces:**
- Consumes: `StoreHubConfigReader.Read` (Task 1)
- Produces: `internal static string DatabaseSetup.BuildSuperuserGuardMessage(bool storeDatabaseExists)`

- [ ] **Step 1: Write the failing tests**

Append to `tests/IndyPOS.Bootstrapper.Tests/Installers/DatabaseSetupTests.cs` (keep the file's existing `using`s; add `using IndyPOS.Bootstrapper.Installers;` if absent):

```csharp
    [Fact]
    public void BuildSuperuserGuardMessage_WhenAStoreDatabaseExists_ShouldNeverRecommendTheCleanupScript()
    {
        // cleanup-v4.ps1 drops indypos_storehub unconditionally unless -SkipDatabase.
        // Recommending it to a live store destroys every sale ever recorded.
        var message = DatabaseSetup.BuildSuperuserGuardMessage(storeDatabaseExists: true);

        message.Should().NotContain("cleanup-v4");
        message.Should().NotContain("-RemovePostgres");
    }

    [Fact]
    public void BuildSuperuserGuardMessage_WhenAStoreDatabaseExists_ShouldPointAtTheUpgradeCommand()
    {
        var message = DatabaseSetup.BuildSuperuserGuardMessage(storeDatabaseExists: true);

        message.Should().Contain("--silent");
        message.Should().Contain("existing IndyPOS database");
    }

    [Fact]
    public void BuildSuperuserGuardMessage_OnABareMachine_ShouldStillOfferTheCleanupScript()
    {
        // No store data to lose here, so the fast path stays available.
        var message = DatabaseSetup.BuildSuperuserGuardMessage(storeDatabaseExists: false);

        message.Should().Contain("cleanup-v4.ps1");
    }
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests --filter FullyQualifiedName~DatabaseSetupTests`
Expected: FAIL — `BuildSuperuserGuardMessage` does not exist.

- [ ] **Step 3: Write the implementation**

In `installer/IndyPOS.Bootstrapper/Installers/DatabaseSetup.cs`, replace the class summary at lines 9-11 with the fresh-only banner:

```csharp
/// <summary>
/// Handles database creation and configuration.
/// <para>
/// FRESH INSTALL ONLY. Never runs on an upgrade — the role, its password and the
/// DPAPI-protected connection string already exist there, so asking the
/// database-provisioning question at all is what produced the superuser-password
/// failure this guard reports. See the upgrade design spec, sections 2 and 4.
/// </para>
/// </summary>
```

Replace the guard block at lines 38-50 with:

```csharp
        if (string.IsNullOrEmpty(postgresPassword))
        {
            var storeDatabaseExists = StoreHubConfigReader
                .Read(Path.Combine(config.StoreHubInstallPath, "appsettings.json"))
                .ConnectionStringUsable;

            return new DatabaseSetupResult
            {
                Success = false,
                ErrorMessage = BuildSuperuserGuardMessage(storeDatabaseExists)
            };
        }
```

Add the message builder next to `GenerateJwtSecret`:

```csharp
    /// <summary>
    /// The superuser password is deliberately not persisted (spec section 1), so an existing
    /// PostgreSQL cannot be provisioned into. What the operator should do next depends
    /// entirely on whether there is a store database to protect.
    /// </summary>
    internal static string BuildSuperuserGuardMessage(bool storeDatabaseExists) =>
        storeDatabaseExists
            ? "PostgreSQL 18 is already installed and this machine has an existing IndyPOS database.\n" +
              "Do NOT remove PostgreSQL — that would destroy the store's sales history.\n" +
              "Upgrade in place instead:\n" +
              "  IndyPOS-Setup.exe --silent\n" +
              "See docs\\operations\\upgrade-procedure.md."
            : "PostgreSQL 18 is already installed, but its superuser password is unknown " +
              "(the installer doesn't persist it across runs). To proceed, either:\n" +
              "  - Uninstall PostgreSQL: scripts\\cleanup-v4.ps1 -Force -RemovePostgres\n" +
              "  - Or remove C:\\Program Files\\PostgreSQL\\18 manually,\n" +
              "then re-run this installer for a clean Postgres install.";
```

Add `using IndyPOS.Bootstrapper.Upgrade;` to the file's usings.

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests --filter FullyQualifiedName~DatabaseSetupTests`
Expected: PASS — the 3 new tests plus every pre-existing `DatabaseSetupTests` case.

- [ ] **Step 5: Commit**

```bash
git add installer/IndyPOS.Bootstrapper/Installers/DatabaseSetup.cs tests/IndyPOS.Bootstrapper.Tests/Installers/DatabaseSetupTests.cs
git commit -m "fix(installer): stop recommending a cleanup that drops the store database"
```

---

## Task 3: Install-mode detection

Spec §3. Three outcomes, not two — a machine can be neither freshly installable nor upgradeable, and collapsing `Unusable` into `Fresh` sends the run *past* the mutation line.

**Files:**
- Create: `installer/IndyPOS.Bootstrapper/Upgrade/InstallMode.cs`, `installer/IndyPOS.Bootstrapper/Upgrade/IInstallProbe.cs`, `installer/IndyPOS.Bootstrapper/Upgrade/InstallModeDetector.cs`
- Test: `tests/IndyPOS.Bootstrapper.Tests/Upgrade/InstallModeDetectorTests.cs`

**Interfaces:**
- Consumes: `StoreHubConfigFacts` (Task 1), `InstallationConfig.SystemRoot` / `.StoreHubInstallPath` / `.ServiceName`, `InstallManifestWriter.ManifestFileName`
- Produces:
  - `enum InstallMode { Fresh, Upgrade, Unusable }`
  - `sealed record DetectedInstall(InstallMode Mode, string? InstalledVersion, string? StoreId, StoreType? StoreType, string Reason)`
  - `interface IInstallProbe { bool ManifestExists { get; } string? ManifestInstallVersion { get; } bool PostgresStoreDatabaseExists { get; } string? ServiceImagePath { get; } StoreHubConfigFacts Config { get; } }`
  - `sealed class WindowsInstallProbe : IInstallProbe` with `WindowsInstallProbe(InstallationConfig config)`
  - `static class InstallModeDetector` with `static DetectedInstall Detect(IInstallProbe probe, string storeHubInstallPath)`

- [ ] **Step 1: Write the failing tests**

Create `tests/IndyPOS.Bootstrapper.Tests/Upgrade/InstallModeDetectorTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Bootstrapper.Upgrade;
using IndyPOS.Domain.Enums;

namespace IndyPOS.Bootstrapper.Tests.Upgrade;

public class InstallModeDetectorTests
{
    private const string InstallPath = @"C:\ProgramData\IndyPOS\v4\StoreHub";

    private sealed class FakeProbe : IInstallProbe
    {
        public bool ManifestExists { get; init; }
        public string? ManifestInstallVersion { get; init; }
        public bool PostgresStoreDatabaseExists { get; init; }
        public string? ServiceImagePath { get; init; }
        public StoreHubConfigFacts Config { get; init; } = new(false, false, null, null);
    }

    private static FakeProbe HealthyUpgrade() => new()
    {
        ManifestExists = true,
        ManifestInstallVersion = "4.0.0",
        PostgresStoreDatabaseExists = true,
        ServiceImagePath = InstallPath + @"\IndyPOS.StoreHub.exe",
        Config = new StoreHubConfigFacts(true, true, "Rungrat-001", StoreType.Minimart)
    };

    [Fact]
    public void Detect_OnABareMachine_ShouldReturnFresh()
    {
        var result = InstallModeDetector.Detect(new FakeProbe(), InstallPath);

        result.Mode.Should().Be(InstallMode.Fresh);
    }

    [Fact]
    public void Detect_OnAHealthyInstall_ShouldReturnUpgradeWithEveryDetectedValue()
    {
        var result = InstallModeDetector.Detect(HealthyUpgrade(), InstallPath);

        result.Mode.Should().Be(InstallMode.Upgrade);
        result.InstalledVersion.Should().Be("4.0.0");
        result.StoreId.Should().Be("Rungrat-001");
        result.StoreType.Should().Be(StoreType.Minimart);
    }

    [Fact]
    public void Detect_WithAStoreDatabaseButNoManifestForThisMajor_ShouldReturnUnusable()
    {
        // Rule 1, and it must beat rule 3. A v5 installer on a live v4 store finds no
        // manifest under v5\ and would otherwise read as Fresh -- but DatabaseName is
        // NOT version-scoped, so that "side-by-side" install collides with the v4 database.
        var probe = new FakeProbe { PostgresStoreDatabaseExists = true, ManifestExists = false };

        var result = InstallModeDetector.Detect(probe, InstallPath);

        result.Mode.Should().Be(InstallMode.Unusable);
        result.Reason.Should().Contain("another IndyPOS major version");
    }

    [Fact]
    public void Detect_WithAStoreDatabaseAndNoManifest_ShouldNotRecommendTheCleanupScript()
    {
        var probe = new FakeProbe { PostgresStoreDatabaseExists = true, ManifestExists = false };

        InstallModeDetector.Detect(probe, InstallPath).Reason.Should().NotContain("cleanup-v4");
    }

    [Fact]
    public void Detect_WithAnEmptyStoreId_ShouldReturnUnusable()
    {
        // StoreIdentityService falls back to STORE-{MachineName}, and payment_method is
        // keyed on (StoreId, Code) -- the seeder would insert a second full catalogue and
        // every later sale would be written against it.
        var probe = HealthyUpgrade() with
        {
            Config = new StoreHubConfigFacts(true, true, null, StoreType.Minimart)
        };

        var result = InstallModeDetector.Detect(probe, InstallPath);

        result.Mode.Should().Be(InstallMode.Unusable);
        result.Reason.Should().Contain("Store:Id");
    }

    [Fact]
    public void Detect_WithAnAbsentStoreType_ShouldReturnUnusable()
    {
        // Added 2026-07-18. Absent binds to GeneralHardware -- the MOST permissive value --
        // which would silently re-enable PayLater and Hardware products on a Minimart.
        var probe = HealthyUpgrade() with
        {
            Config = new StoreHubConfigFacts(true, true, "Rungrat-001", null)
        };

        var result = InstallModeDetector.Detect(probe, InstallPath);

        result.Mode.Should().Be(InstallMode.Unusable);
        result.Reason.Should().Contain("Store:Type");
    }

    [Fact]
    public void Detect_WithAnUndecryptableConnectionString_ShouldReturnUnusable()
    {
        var probe = HealthyUpgrade() with
        {
            Config = new StoreHubConfigFacts(true, false, "Rungrat-001", StoreType.Minimart)
        };

        var result = InstallModeDetector.Detect(probe, InstallPath);

        result.Mode.Should().Be(InstallMode.Unusable);
        result.Reason.Should().Contain("connection string");
    }

    [Fact]
    public void Detect_WhenTheServiceIsNotRegistered_ShouldReturnUnusable()
    {
        var probe = HealthyUpgrade() with { ServiceImagePath = null };

        var result = InstallModeDetector.Detect(probe, InstallPath).Mode.Should().Be(InstallMode.Unusable);
    }

    [Fact]
    public void Detect_WhenTheServicePointsSomewhereElse_ShouldReturnUnusable()
    {
        var probe = HealthyUpgrade() with
        {
            ServiceImagePath = @"C:\Some\Other\Place\IndyPOS.StoreHub.exe"
        };

        InstallModeDetector.Detect(probe, InstallPath).Mode.Should().Be(InstallMode.Unusable);
    }

    [Fact]
    public void Detect_WithAQuotedServiceImagePath_ShouldStillMatch()
    {
        // sc.exe records binPath with quotes when the path contains spaces.
        var probe = HealthyUpgrade() with
        {
            ServiceImagePath = "\"" + InstallPath + "\\IndyPOS.StoreHub.exe\""
        };

        InstallModeDetector.Detect(probe, InstallPath).Mode.Should().Be(InstallMode.Upgrade);
    }

    [Fact]
    public void Detect_WithAManifestButNoConfig_ShouldReturnUnusable()
    {
        // Rule 4. This is the exact state the second failed VM run left behind.
        var probe = new FakeProbe
        {
            ManifestExists = true,
            ManifestInstallVersion = "4.0.0",
            ServiceImagePath = InstallPath + @"\IndyPOS.StoreHub.exe",
            Config = new StoreHubConfigFacts(true, false, null, null)
        };

        InstallModeDetector.Detect(probe, InstallPath).Mode.Should().Be(InstallMode.Unusable);
    }

    [Fact]
    public void Detect_WithConfigButNoManifest_ShouldReturnUnusableNotFresh()
    {
        // Rule 3 requires BOTH absent. A leftover config means this is not a bare machine.
        var probe = new FakeProbe
        {
            Config = new StoreHubConfigFacts(true, true, "Rungrat-001", StoreType.Minimart)
        };

        InstallModeDetector.Detect(probe, InstallPath).Mode.Should().Be(InstallMode.Unusable);
    }
}
```

Note: the `Detect_WhenTheServiceIsNotRegistered_ShouldReturnUnusable` body above assigns to an unused local — write it as a plain expression statement:

```csharp
        InstallModeDetector.Detect(probe, InstallPath).Mode.Should().Be(InstallMode.Unusable);
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests --filter FullyQualifiedName~InstallModeDetectorTests`
Expected: FAIL — `InstallMode`, `IInstallProbe`, `InstallModeDetector` do not exist.

- [ ] **Step 3: Write the data types**

Create `installer/IndyPOS.Bootstrapper/Upgrade/InstallMode.cs`:

```csharp
using IndyPOS.Domain.Enums;

namespace IndyPOS.Bootstrapper.Upgrade;

/// <summary>
/// How this machine should be treated. Three outcomes, not two: a machine can be
/// neither freshly installable nor upgradeable, and treating that as Fresh sends the
/// run past the point where it starts mutating the install.
/// </summary>
public enum InstallMode
{
    Fresh,
    Upgrade,
    Unusable
}

/// <param name="InstalledVersion">From install-manifest.json. Null unless a manifest was found.</param>
/// <param name="StoreId">From appsettings.json — the manifest does not record it.</param>
/// <param name="Reason">Operator-facing explanation. Surfaced on Unusable.</param>
public sealed record DetectedInstall(
    InstallMode Mode,
    string? InstalledVersion,
    string? StoreId,
    StoreType? StoreType,
    string Reason);
```

- [ ] **Step 4: Write the probe**

Create `installer/IndyPOS.Bootstrapper/Upgrade/IInstallProbe.cs`:

```csharp
using System.ServiceProcess;
using System.Text.Json;
using Microsoft.Win32;
using IndyPOS.Bootstrapper.Installers;

namespace IndyPOS.Bootstrapper.Upgrade;

/// <summary>
/// Everything <see cref="InstallModeDetector"/> needs to know about the machine.
/// An interface so the ordered rules can be tested without a real install.
/// </summary>
public interface IInstallProbe
{
    bool ManifestExists { get; }
    string? ManifestInstallVersion { get; }

    /// <summary>
    /// A PostgreSQL install carrying the IndyPOS database. Note DatabaseName is NOT
    /// version-scoped, so this is true for a v4 store seen by a v5 installer.
    /// </summary>
    bool PostgresStoreDatabaseExists { get; }

    /// <summary>The registered service's ImagePath, or null when no such service exists.</summary>
    string? ServiceImagePath { get; }

    StoreHubConfigFacts Config { get; }
}

/// <summary>Reads the real machine. Every member is evaluated once, at construction.</summary>
public sealed class WindowsInstallProbe : IInstallProbe
{
    public WindowsInstallProbe(InstallationConfig config)
    {
        var manifestPath = Path.Combine(config.SystemRoot, InstallManifestWriter.ManifestFileName);
        ManifestExists = File.Exists(manifestPath);
        ManifestInstallVersion = ManifestExists ? ReadManifestVersion(manifestPath) : null;

        Config = StoreHubConfigReader.Read(
            Path.Combine(config.StoreHubInstallPath, "appsettings.json"));

        ServiceImagePath = ReadServiceImagePath(config.ServiceName);
        PostgresStoreDatabaseExists = DetectStoreDatabase(config);
    }

    public bool ManifestExists { get; }
    public string? ManifestInstallVersion { get; }
    public bool PostgresStoreDatabaseExists { get; }
    public string? ServiceImagePath { get; }
    public StoreHubConfigFacts Config { get; }

    private static string? ReadManifestVersion(string manifestPath)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
            return doc.RootElement.TryGetProperty("installVersion", out var v) ? v.GetString() : null;
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            return null;
        }
    }

    // ServiceController does not expose ImagePath, so read the SCM registry key directly.
    private static string? ReadServiceImagePath(string serviceName)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                $@"SYSTEM\CurrentControlSet\Services\{serviceName}");
            return key?.GetValue("ImagePath") as string;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    // No superuser credential is available here, so this is a filesystem-and-config
    // inference rather than a query: a usable connection string means a provisioned
    // database, and a Postgres data directory means one could exist for another major.
    private static bool DetectStoreDatabase(InstallationConfig config)
    {
        var appSettings = Path.Combine(config.StoreHubInstallPath, "appsettings.json");
        if (StoreHubConfigReader.Read(appSettings).ConnectionStringUsable)
        {
            return true;
        }

        foreach (var major in new[] { "18", "17", "16" })
        {
            var dataDir = $@"C:\Program Files\PostgreSQL\{major}\data\base";
            if (Directory.Exists(dataDir) && OtherMajorStoreConfigExists())
            {
                return true;
            }
        }

        return false;
    }

    // C:\ProgramData\IndyPOS\v{N}\StoreHub\appsettings.json for a major other than ours
    // is the v5-installer-on-a-v4-store case that rule 1 exists to catch.
    private static bool OtherMajorStoreConfigExists()
    {
        var root = @"C:\ProgramData\IndyPOS";
        if (!Directory.Exists(root))
        {
            return false;
        }

        return Directory.EnumerateDirectories(root, "v*")
            .Select(d => Path.Combine(d, "StoreHub", "appsettings.json"))
            .Any(p => StoreHubConfigReader.Read(p).ConnectionStringUsable);
    }
}
```

`Microsoft.Win32.Registry` comes from the `Microsoft.Win32.Registry` package; on `net10.0-windows` it is already available via the Windows Desktop framework reference — verify the project builds and add the package only if it does not.

- [ ] **Step 5: Write the detector**

Create `installer/IndyPOS.Bootstrapper/Upgrade/InstallModeDetector.cs`:

```csharp
namespace IndyPOS.Bootstrapper.Upgrade;

/// <summary>
/// Classifies a machine. Rules are evaluated IN ORDER and the first match wins —
/// see the upgrade design spec, section 3. Reordering them reintroduces the data-loss
/// path rule 1 exists to close.
/// </summary>
public static class InstallModeDetector
{
    public static DetectedInstall Detect(IInstallProbe probe, string storeHubInstallPath)
    {
        ArgumentNullException.ThrowIfNull(probe);

        // Rule 1 — must precede the manifest lookup. SystemRoot is version-scoped but
        // DatabaseName is not, so a newer major would read a live store as Fresh and
        // then collide with its database.
        if (probe.PostgresStoreDatabaseExists && !probe.ManifestExists)
        {
            return Unusable(probe,
                "An IndyPOS database exists on this machine but belongs to another IndyPOS " +
                "major version. Do not remove PostgreSQL — it holds the store's sales history. " +
                "Run the installer for the version that owns that install root instead.");
        }

        // Rule 2 — everything an upgrade needs is present and coherent.
        if (probe.ManifestExists)
        {
            if (!probe.Config.ConnectionStringUsable)
            {
                return Unusable(probe,
                    "The StoreHub connection string is missing, empty, or cannot be decrypted on " +
                    "this machine. Restore appsettings.json from a backup before upgrading.");
            }

            if (probe.Config.StoreId is null)
            {
                return Unusable(probe,
                    "Store:Id is missing from appsettings.json. Upgrading without it would seed a " +
                    "second payment-method catalogue under a fallback store id and orphan the " +
                    "store's sales history. Set Store:Id to this store's real id first.");
            }

            if (probe.Config.StoreType is null)
            {
                return Unusable(probe,
                    "Store:Type is missing from appsettings.json (stores installed before " +
                    "2026-07-18 have no such key). It cannot be defaulted: the default is the most " +
                    "permissive store type and would silently re-enable restricted features. " +
                    "Re-run with --store-type <GeneralHardware|Minimart> to set it.");
            }

            if (!ImagePathResolvesUnder(probe.ServiceImagePath, storeHubInstallPath))
            {
                return Unusable(probe,
                    "The StoreHub service is not registered, or its ImagePath does not resolve " +
                    $"under {storeHubInstallPath}. Upgrading would deploy binaries the service " +
                    "does not run.");
            }

            return new DetectedInstall(
                InstallMode.Upgrade,
                probe.ManifestInstallVersion,
                probe.Config.StoreId,
                probe.Config.StoreType,
                "An existing IndyPOS install was detected and can be upgraded in place.");
        }

        // Rule 3 — a genuinely bare machine. BOTH must be absent.
        if (!probe.Config.Exists)
        {
            return new DetectedInstall(InstallMode.Fresh, null, null, null,
                "No existing IndyPOS install was found.");
        }

        // Rule 4 — anything else.
        return Unusable(probe,
            "A StoreHub configuration exists but there is no install manifest, so this machine " +
            "is neither a clean install nor a complete one. Inspect " +
            $"{storeHubInstallPath} before proceeding.");
    }

    private static DetectedInstall Unusable(IInstallProbe probe, string reason) =>
        new(InstallMode.Unusable, probe.ManifestInstallVersion, probe.Config.StoreId,
            probe.Config.StoreType, reason);

    // sc.exe records binPath with surrounding quotes when the path contains spaces,
    // and "C:\ProgramData\..." always does not — but a hand-registered service may.
    private static bool ImagePathResolvesUnder(string? imagePath, string installPath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return false;
        }

        var trimmed = imagePath.Trim().Trim('"');

        return trimmed.StartsWith(installPath, StringComparison.OrdinalIgnoreCase);
    }
}
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests --filter FullyQualifiedName~InstallModeDetectorTests`
Expected: PASS, 12 tests.

- [ ] **Step 7: Commit**

```bash
git add installer/IndyPOS.Bootstrapper/Upgrade/ tests/IndyPOS.Bootstrapper.Tests/Upgrade/InstallModeDetectorTests.cs
git commit -m "feat(installer): classify a machine as fresh, upgradeable, or unusable"
```

---

## Task 4: Extract ServiceControl and StoreHubPayload

Spec §2. Both paths stop the service and lay down the payload, but in a different order relative to everything else. Pulling them out of `StoreHubInstaller` is what lets the upgrade sequence reorder without touching the fresh path.

**This is a pure refactor.** `StoreHubInstaller` must behave identically afterwards; its existing tests are the proof and must not be edited.

**Files:**
- Create: `installer/IndyPOS.Bootstrapper/Installers/ServiceControl.cs`, `installer/IndyPOS.Bootstrapper/Installers/StoreHubPayload.cs`
- Modify: `installer/IndyPOS.Bootstrapper/Installers/StoreHubInstaller.cs` (`StopExistingServiceAsync`, `StartServiceAsync`, `ExtractStoreHubBinariesAsync`, `CopyDirectoryAsync`)
- Test: `tests/IndyPOS.Bootstrapper.Tests/Installers/StoreHubPayloadTests.cs`

**Interfaces:**
- Consumes: `InstallationConfig.ServiceName`, `InstallationConfig.StoreHubInstallPath`
- Produces:
  - `sealed class ServiceControl(string serviceName)` with
    `Task<bool> StopAsync(TimeSpan timeout, CancellationToken ct)`,
    `Task<bool> StartAsync(TimeSpan timeout, CancellationToken ct)`,
    `bool Exists()`,
    `bool IsRunning()`
  - `static class StoreHubPayload` with
    `static Task<bool> ExtractAsync(string destinationPath, IProgress<string>? log, CancellationToken ct)`

- [ ] **Step 1: Write the failing test for the extractable seam**

`ServiceControl` wraps `ServiceController` and has no seam — it is VM-tested, per spec §10 ("No seam — the VM's job"). `StoreHubPayload` does have one: the external-folder fallback.

Create `tests/IndyPOS.Bootstrapper.Tests/Installers/StoreHubPayloadTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Bootstrapper.Installers;

namespace IndyPOS.Bootstrapper.Tests.Installers;

public class StoreHubPayloadTests : IDisposable
{
    private readonly string _dir =
        Path.Combine(Path.GetTempPath(), "indypos-payload-" + Guid.NewGuid().ToString("N"));

    public StoreHubPayloadTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    [Fact]
    public async Task ExtractAsync_WithNoPayloadAvailable_ShouldReturnFalse()
    {
        // The test host has no embedded StoreHub.zip and no sibling folder, so this
        // exercises the "binaries not found" branch without a 67 MB fixture.
        var dest = Path.Combine(_dir, "StoreHub");
        Directory.CreateDirectory(dest);

        var result = await StoreHubPayload.ExtractAsync(dest, log: null, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExtractAsync_WithNoPayloadAvailable_ShouldReportEveryLocationItTried()
    {
        var messages = new List<string>();
        var dest = Path.Combine(_dir, "StoreHub");
        Directory.CreateDirectory(dest);

        await StoreHubPayload.ExtractAsync(
            dest, new Progress<string>(messages.Add), CancellationToken.None);

        // Progress<T> marshals asynchronously; drain before asserting.
        await Task.Delay(50);
        messages.Should().Contain(m => m.Contains("StoreHub.zip"));
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests --filter FullyQualifiedName~StoreHubPayloadTests`
Expected: FAIL — `StoreHubPayload` does not exist.

- [ ] **Step 3: Create ServiceControl**

Create `installer/IndyPOS.Bootstrapper/Installers/ServiceControl.cs`. Move the bodies of `StoreHubInstaller.StopExistingServiceAsync` and `StartServiceAsync` verbatim — no behaviour changes:

```csharp
using System.ServiceProcess;

namespace IndyPOS.Bootstrapper.Installers;

/// <summary>
/// Stop / start / query the StoreHub Windows service.
/// <para>USED BY BOTH INSTALL PATHS — fresh install and in-place upgrade. A change here
/// ships to every store on the next release, not just to upgrades.</para>
/// </summary>
public sealed class ServiceControl(string serviceName)
{
    private readonly string _serviceName = serviceName;

    public bool Exists() =>
        ServiceController.GetServices().Any(s => s.ServiceName == _serviceName);

    public bool IsRunning()
    {
        try
        {
            using var sc = new ServiceController(_serviceName);
            return sc.Status == ServiceControllerStatus.Running;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>Returns true when the service is stopped afterwards, including when absent.</summary>
    public async Task<bool> StopAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        try
        {
            using var sc = new ServiceController(_serviceName);

            if (sc.Status is ServiceControllerStatus.Running or ServiceControllerStatus.StartPending)
            {
                sc.Stop();
                await Task.Run(
                    () => sc.WaitForStatus(ServiceControllerStatus.Stopped, timeout),
                    cancellationToken);
            }

            return true;
        }
        catch (InvalidOperationException)
        {
            // Service doesn't exist - that's fine.
            return true;
        }
        catch (System.ServiceProcess.TimeoutException)
        {
            return false;
        }
    }

    public async Task<bool> StartAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        try
        {
            using var sc = new ServiceController(_serviceName);

            if (sc.Status == ServiceControllerStatus.Running)
            {
                return true;
            }

            sc.Start();
            await Task.Run(
                () => sc.WaitForStatus(ServiceControllerStatus.Running, timeout),
                cancellationToken);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
```

- [ ] **Step 4: Create StoreHubPayload**

Create `installer/IndyPOS.Bootstrapper/Installers/StoreHubPayload.cs`. Move `ExtractStoreHubBinariesAsync` and `CopyDirectoryAsync` verbatim, taking `destinationPath` as a parameter instead of reading `Config`:

```csharp
using System.IO.Compression;

namespace IndyPOS.Bootstrapper.Installers;

/// <summary>
/// Lays the StoreHub payload down over an install directory.
/// <para>USED BY BOTH INSTALL PATHS — fresh install and in-place upgrade.</para>
/// <para>Extraction is per-entry with <c>overwrite: true</c> and has no clean step, so the
/// result is old-union-new. Rollback must therefore delete before copying back
/// (see the upgrade design spec, section 5).</para>
/// </summary>
public static class StoreHubPayload
{
    public const string ResourceName = "IndyPOS.Bootstrapper.Resources.StoreHub.zip";

    public static async Task<bool> ExtractAsync(
        string destinationPath,
        IProgress<string>? log = null,
        CancellationToken cancellationToken = default)
    {
        var assembly = typeof(StoreHubPayload).Assembly;

        using var resourceStream = assembly.GetManifestResourceStream(ResourceName);

        if (resourceStream != null)
        {
            log?.Report("Extracting from embedded resources...");

            using var archive = new ZipArchive(resourceStream, ZipArchiveMode.Read);
            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrEmpty(entry.Name))
                {
                    continue;
                }

                var destPath = Path.Combine(destinationPath, entry.FullName);
                var destDir = Path.GetDirectoryName(destPath);

                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                entry.ExtractToFile(destPath, overwrite: true);
            }

            return true;
        }

        var externalZip = Path.Combine(AppContext.BaseDirectory, "StoreHub.zip");

        if (File.Exists(externalZip))
        {
            log?.Report("Extracting from external package...");
            ZipFile.ExtractToDirectory(externalZip, destinationPath, overwriteFiles: true);
            return true;
        }

        var externalFolder = Path.Combine(AppContext.BaseDirectory, "StoreHub");

        if (Directory.Exists(externalFolder))
        {
            log?.Report("Copying from external folder...");
            await CopyDirectoryAsync(externalFolder, destinationPath, cancellationToken);
            return true;
        }

        log?.Report("ERROR: StoreHub binaries not found!");
        log?.Report("Expected locations:");
        log?.Report($"  - Embedded resource: {ResourceName}");
        log?.Report($"  - External zip: {externalZip}");
        log?.Report($"  - External folder: {externalFolder}");

        return false;
    }

    private static async Task CopyDirectoryAsync(
        string sourceDir,
        string destDir,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
        }

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            await using var sourceStream = File.OpenRead(file);
            await using var destStream = File.Create(destFile);
            await sourceStream.CopyToAsync(destStream, cancellationToken);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            await CopyDirectoryAsync(dir, destSubDir, cancellationToken);
        }
    }
}
```

- [ ] **Step 5: Point StoreHubInstaller at the extracted units**

In `installer/IndyPOS.Bootstrapper/Installers/StoreHubInstaller.cs`:

- Delete the private `ExtractStoreHubBinariesAsync`, `CopyDirectoryAsync`, and `StopExistingServiceAsync` methods.
- Replace the `StopExistingServiceAsync(cancellationToken)` call inside `InstallAsync` with:

```csharp
            await new ServiceControl(Config.ServiceName)
                .StopAsync(TimeSpan.FromSeconds(30), cancellationToken);
```

- Replace the `ExtractStoreHubBinariesAsync(log, cancellationToken)` call with:

```csharp
            var extractResult = await StoreHubPayload.ExtractAsync(
                Config.StoreHubInstallPath, log, cancellationToken);
```

- Replace the body of `StartServiceAsync` with a delegation that preserves its result shape:

```csharp
    public async Task<StoreHubInstallerResult> StartServiceAsync(CancellationToken cancellationToken = default)
    {
        var started = await new ServiceControl(Config.ServiceName)
            .StartAsync(TimeSpan.FromSeconds(60), cancellationToken);

        return started
            ? new StoreHubInstallerResult { Success = true }
            : new StoreHubInstallerResult
            {
                Success = false,
                ErrorMessage = $"Failed to start service: {Config.ServiceName}"
            };
    }
```

Remove the now-unused `using System.ServiceProcess;` and `using System.IO.Compression;` if the compiler flags them.

- [ ] **Step 6: Run the full bootstrapper suite**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests`
Expected: PASS — the 2 new `StoreHubPayloadTests` plus the pre-existing baseline of **101 pass / 8 skip**, now 103 pass / 8 skip. Any pre-existing test that changed result means the refactor was not behaviour-preserving; fix the refactor, not the test.

- [ ] **Step 7: Commit**

```bash
git add installer/IndyPOS.Bootstrapper/Installers/ServiceControl.cs installer/IndyPOS.Bootstrapper/Installers/StoreHubPayload.cs installer/IndyPOS.Bootstrapper/Installers/StoreHubInstaller.cs tests/IndyPOS.Bootstrapper.Tests/Installers/StoreHubPayloadTests.cs
git commit -m "refactor(installer): extract ServiceControl and StoreHubPayload as shared units"
```

---

## Task 5: Extract MigrationRunner and HealthProbe

Spec §2. `MigrationRunner` deliberately does **not** carry the name `SchemaProvisioner` — it runs on both paths, whereas `DatabaseSetup` is fresh-only, and shared vocabulary between the two invites exactly the confusion that produced both original failures.

**Files:**
- Create: `installer/IndyPOS.Bootstrapper/Installers/MigrationRunner.cs`, `installer/IndyPOS.Bootstrapper/Installers/HealthProbe.cs`
- Modify: `installer/IndyPOS.Bootstrapper/Installers/StoreHubInstaller.cs` (`ProvisionDatabaseAsync`), `installer/IndyPOS.Bootstrapper/Installers/InstallationOrchestrator.cs` (`VerifyStoreHubHealthAsync`)
- Test: `tests/IndyPOS.Bootstrapper.Tests/Installers/MigrationRunnerTests.cs`

**Interfaces:**
- Consumes: nothing new
- Produces:
  - `sealed record MigrationRunResult(bool Success, bool AdminSeeded, string? ErrorMessage)`
  - `static class MigrationRunner` with
    `static Task<MigrationRunResult> RunAsync(string storeHubInstallPath, IProgress<string>? log, CancellationToken ct)` and
    `internal static bool ParseAdminSeeded(string stdout)`
  - `static class HealthProbe` with
    `static Task<bool> IsReadyAsync(int port, int attempts = 5, CancellationToken ct = default)`

- [ ] **Step 1: Write the failing tests**

Create `tests/IndyPOS.Bootstrapper.Tests/Installers/MigrationRunnerTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Bootstrapper.Installers;

namespace IndyPOS.Bootstrapper.Tests.Installers;

public class MigrationRunnerTests
{
    [Theory]
    [InlineData("ADMIN_SEEDED=true", true)]
    [InlineData("admin_seeded=TRUE", true)]
    [InlineData("noise\nADMIN_SEEDED=true\nmore noise", true)]
    [InlineData("ADMIN_SEEDED=false", false)]
    [InlineData("", false)]
    public void ParseAdminSeeded_WithVariousStdout_ShouldReturnExpected(string stdout, bool expected)
    {
        MigrationRunner.ParseAdminSeeded(stdout).Should().Be(expected);
    }

    [Fact]
    public async Task RunAsync_WhenTheExecutableIsMissing_ShouldFailWithTheProbedPath()
    {
        var missing = Path.Combine(Path.GetTempPath(), "indypos-no-such-" + Guid.NewGuid().ToString("N"));

        var result = await MigrationRunner.RunAsync(missing, log: null, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("IndyPOS.StoreHub.exe");
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests --filter FullyQualifiedName~MigrationRunnerTests`
Expected: FAIL — `MigrationRunner` does not exist.

- [ ] **Step 3: Create MigrationRunner**

Create `installer/IndyPOS.Bootstrapper/Installers/MigrationRunner.cs`, moving the body of `StoreHubInstaller.ProvisionDatabaseAsync` verbatim:

```csharp
using System.Diagnostics;

namespace IndyPOS.Bootstrapper.Installers;

public sealed record MigrationRunResult(bool Success, bool AdminSeeded, string? ErrorMessage);

/// <summary>
/// Runs the StoreHub app's one-shot <c>migrate</c> command (apply EF migrations, seed the
/// initial admin, exit).
/// <para>USED BY BOTH INSTALL PATHS — fresh install and in-place upgrade. This is NOT
/// <see cref="DatabaseSetup"/>, which creates the role, the database and the connection
/// string and runs on a fresh install only.</para>
/// <para>Doing this as a console step BEFORE the service starts keeps the first service
/// start instant, so a schema build can't overrun the 30s SCM start timeout (error 1053).</para>
/// </summary>
public static class MigrationRunner
{
    public static async Task<MigrationRunResult> RunAsync(
        string storeHubInstallPath,
        IProgress<string>? log = null,
        CancellationToken cancellationToken = default)
    {
        var exePath = Path.Combine(storeHubInstallPath, "IndyPOS.StoreHub.exe");
        if (!File.Exists(exePath))
        {
            return new MigrationRunResult(false, false, $"StoreHub executable not found at {exePath}");
        }

        try
        {
            log?.Report("Applying database migrations and seeding admin...");

            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = "migrate",
                WorkingDirectory = storeHubInstallPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            // Production is the host default when unset, but be explicit so a stray dev
            // env var can't divert provisioning to the EnsureCreated path.
            psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";

            using var process = new Process { StartInfo = psi };
            process.Start();

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await Task.WhenAll(stdoutTask, stderrTask);
            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            await process.WaitForExitAsync(cancellationToken);

            return process.ExitCode != 0
                ? new MigrationRunResult(false, false,
                    $"Database provisioning failed (exit {process.ExitCode}): {stderr.Trim()}")
                : new MigrationRunResult(true, ParseAdminSeeded(stdout), null);
        }
        catch (Exception ex)
        {
            return new MigrationRunResult(false, false, $"Database provisioning failed: {ex.Message}");
        }
    }

    internal static bool ParseAdminSeeded(string stdout) =>
        stdout.Contains("ADMIN_SEEDED=true", StringComparison.OrdinalIgnoreCase);
}
```

- [ ] **Step 4: Create HealthProbe**

Create `installer/IndyPOS.Bootstrapper/Installers/HealthProbe.cs`, moving `InstallationOrchestrator.VerifyStoreHubHealthAsync` verbatim:

```csharp
namespace IndyPOS.Bootstrapper.Installers;

/// <summary>
/// Polls StoreHub's readiness endpoint.
/// <para>USED BY BOTH INSTALL PATHS — fresh install and in-place upgrade. The upgrade
/// path also runs it AFTER a rollback: starting the service is not evidence it came
/// back (see the upgrade design spec, section 5).</para>
/// </summary>
public static class HealthProbe
{
    public static async Task<bool> IsReadyAsync(
        int port,
        int attempts = 5,
        CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        // /health/ready is StoreHub's DB-aware readiness probe. /health and /alive are
        // dev-only (Aspire's IsDevelopment() guard in ServiceDefaults).
        var healthUrl = $"http://localhost:{port}/health/ready";

        for (var i = 0; i < attempts; i++)
        {
            try
            {
                var response = await client.GetAsync(healthUrl, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            catch
            {
                // Retry
            }

            await Task.Delay(2000, cancellationToken);
        }

        return false;
    }
}
```

- [ ] **Step 5: Point the callers at the extracted units**

In `StoreHubInstaller.cs`, replace the body of `ProvisionDatabaseAsync` with a delegation that keeps its existing return type:

```csharp
    public async Task<ProvisionDatabaseResult> ProvisionDatabaseAsync(
        IProgress<string>? log = null,
        CancellationToken cancellationToken = default)
    {
        var result = await MigrationRunner.RunAsync(Config.StoreHubInstallPath, log, cancellationToken);

        return new ProvisionDatabaseResult
        {
            Success = result.Success,
            AdminSeeded = result.AdminSeeded,
            ErrorMessage = result.ErrorMessage
        };
    }
```

Keep `internal static bool ParseAdminSeeded(string stdout) => MigrationRunner.ParseAdminSeeded(stdout);` on `StoreHubInstaller` so its existing tests still compile. Remove `using System.Diagnostics;` if unused.

In `InstallationOrchestrator.cs`, delete the private `VerifyStoreHubHealthAsync` and change its call site to:

```csharp
        var healthOk = await HealthProbe.IsReadyAsync(config.HealthCheckPort, cancellationToken: cancellationToken);
```

- [ ] **Step 6: Run the full bootstrapper suite**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests`
Expected: PASS — 109 pass / 8 skip (103 + 6 new). No pre-existing test may change result.

- [ ] **Step 7: Commit**

```bash
git add installer/IndyPOS.Bootstrapper/Installers/MigrationRunner.cs installer/IndyPOS.Bootstrapper/Installers/HealthProbe.cs installer/IndyPOS.Bootstrapper/Installers/StoreHubInstaller.cs installer/IndyPOS.Bootstrapper/Installers/InstallationOrchestrator.cs tests/IndyPOS.Bootstrapper.Tests/Installers/MigrationRunnerTests.cs
git commit -m "refactor(installer): extract MigrationRunner and HealthProbe as shared units"
```

---

## Task 6: Rename InstallationOrchestrator to FreshInstallOrchestrator

Spec §2, and deliberately **its own mechanical commit**. The unprefixed name reads as "the orchestrator" and the new one as a special case, which is precisely backwards.

**This task changes no logic.** It is the refactor safety gate: after it, `git diff` on this file across Tasks 4–6 must show only the rename and the delegation edits already made, with no reordering and no new conditionals.

**Files:**
- Rename: `installer/IndyPOS.Bootstrapper/Installers/InstallationOrchestrator.cs` → `installer/IndyPOS.Bootstrapper/Installers/FreshInstallOrchestrator.cs`
- Modify: `installer/IndyPOS.Bootstrapper/Silent/SilentInstaller.cs:61`, `installer/IndyPOS.Bootstrapper/UI/InstallationWizard.cs:11,41`
- Rename: `tests/IndyPOS.Bootstrapper.Tests/Installers/InstallationOrchestratorTests.cs` → `tests/IndyPOS.Bootstrapper.Tests/Installers/FreshInstallOrchestratorTests.cs`

**Interfaces:**
- Consumes: everything from Tasks 4–5
- Produces: `class FreshInstallOrchestrator` with the unchanged
  `Task<InstallationResult> InstallAsync(InstallationConfig config, IProgress<InstallationProgress> progress, CancellationToken ct)`.
  `InstallationException` and `InstallationResult` keep their names and stay in this file.

- [ ] **Step 1: Rename the file with git so history follows**

```bash
git mv installer/IndyPOS.Bootstrapper/Installers/InstallationOrchestrator.cs installer/IndyPOS.Bootstrapper/Installers/FreshInstallOrchestrator.cs
git mv tests/IndyPOS.Bootstrapper.Tests/Installers/InstallationOrchestratorTests.cs tests/IndyPOS.Bootstrapper.Tests/Installers/FreshInstallOrchestratorTests.cs
```

- [ ] **Step 2: Rename the type and add the frozen-path note**

In `FreshInstallOrchestrator.cs`, change `public class InstallationOrchestrator` to `public class FreshInstallOrchestrator` and replace the class summary with:

```csharp
/// <summary>
/// Orchestrates a FRESH IndyPOS installation onto a machine with no existing install.
/// <para>An in-place upgrade is a different algorithm with different safety requirements,
/// not a variation on this one — see <c>UpgradeOrchestrator</c>. Forcing both through one
/// flow is what produced the locked-DLL failure this design exists to fix.</para>
/// <para>FROZEN: this is the only production-validated path, and its fresh-only branches
/// (sc create, first-time directory creation, DatabaseSetup, the Postgres install) never
/// execute on the upgrade VM snapshot. Change it only for a fresh-install reason.</para>
/// </summary>
```

In `FreshInstallOrchestratorTests.cs`, rename the test class to `FreshInstallOrchestratorTests` and update every `new InstallationOrchestrator()` to `new FreshInstallOrchestrator()`.

- [ ] **Step 3: Update the two call sites**

`Silent/SilentInstaller.cs:61`:

```csharp
            result = new FreshInstallOrchestrator()
```

`UI/InstallationWizard.cs` — line 11 field declaration and line 41 construction:

```csharp
    private readonly FreshInstallOrchestrator _orchestrator;
```

```csharp
        _orchestrator = new FreshInstallOrchestrator();
```

- [ ] **Step 4: Verify nothing still references the old name**

Run: `git grep -n "InstallationOrchestrator"`
Expected: no matches outside `docs/` and `.claude/`. If a match remains in `installer/` or `tests/`, fix it.

- [ ] **Step 5: Run the build and the full bootstrapper suite**

Run: `dotnet build installer/IndyPOS.Bootstrapper/IndyPOS.Bootstrapper.csproj -c Release`
Expected: 0 errors.

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests`
Expected: PASS — 109 pass / 8 skip, identical to Task 5.

- [ ] **Step 6: Confirm the refactor gate**

Run: `git diff --stat HEAD~3 -- installer/IndyPOS.Bootstrapper/Installers/FreshInstallOrchestrator.cs`
Read the diff. It must contain only: the class rename, the doc comment, the `HealthProbe.IsReadyAsync` delegation, and the deleted `VerifyStoreHubHealthAsync`. **If it contains any step reordering, any new conditional, or any change to the fresh-only branches, revert and redo.**

- [ ] **Step 7: Commit**

```bash
git add -A installer/IndyPOS.Bootstrapper tests/IndyPOS.Bootstrapper.Tests
git commit -m "refactor(installer): rename InstallationOrchestrator to FreshInstallOrchestrator"
```

---

## Task 7: Read the installed POS app version

Spec §12. The 2026-07-26 spike measured exit code `0` for **both** a real upgrade and a same-version repair, so `POS_UPDATED` cannot be inferred from it. The app binaries keep their own build stamp regardless of the package version, so Velopack's `current\sq.version` is the only truthful record of what is installed.

`sq.version` is a **nuspec XML document**, not a bare version string — the spike confirmed this. Parsing it as text would silently produce garbage.

**Files:**
- Create: `installer/IndyPOS.Bootstrapper/Upgrade/PosAppVersion.cs`
- Test: `tests/IndyPOS.Bootstrapper.Tests/Upgrade/PosAppVersionTests.cs`

**Interfaces:**
- Consumes: `InstallationConfig.VelopackInstallPath` (already ends in `current`)
- Produces: `static class PosAppVersion` with
  `const string VersionFileName = "sq.version"` and
  `static string? Read(string velopackCurrentDirectory)`

- [ ] **Step 1: Write the failing tests**

Create `tests/IndyPOS.Bootstrapper.Tests/Upgrade/PosAppVersionTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Bootstrapper.Upgrade;

namespace IndyPOS.Bootstrapper.Tests.Upgrade;

public class PosAppVersionTests : IDisposable
{
    private readonly string _dir =
        Path.Combine(Path.GetTempPath(), "indypos-sqver-" + Guid.NewGuid().ToString("N"));

    public PosAppVersionTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    // Verbatim shape captured from the VM on 2026-07-26.
    private const string RealSqVersion = """
        <?xml version="1.0" encoding="utf-8"?>
        <package xmlns="http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd">
        <metadata>
        <id>IndyPOS.POS.v4</id>
        <title>IndyPOS.POS.v4</title>
        <version>4.0.1</version>
        <channel>stable</channel>
        <mainExe>IndyPOS.Windows.Forms.exe</mainExe>
        </metadata>
        </package>
        """;

    [Fact]
    public void Read_WithARealSqVersionFile_ShouldReturnThePackageVersion()
    {
        File.WriteAllText(Path.Combine(_dir, "sq.version"), RealSqVersion);

        PosAppVersion.Read(_dir).Should().Be("4.0.1");
    }

    [Fact]
    public void Read_WhenTheFileIsAbsent_ShouldReturnNull()
    {
        PosAppVersion.Read(_dir).Should().BeNull();
    }

    [Fact]
    public void Read_WhenTheDirectoryIsAbsent_ShouldReturnNull()
    {
        PosAppVersion.Read(Path.Combine(_dir, "nope")).Should().BeNull();
    }

    [Fact]
    public void Read_WithMalformedXml_ShouldReturnNull()
    {
        // Never throw on the reporting path: POS_UPDATED is telemetry, not a gate.
        File.WriteAllText(Path.Combine(_dir, "sq.version"), "<package><metadata>");

        PosAppVersion.Read(_dir).Should().BeNull();
    }

    [Fact]
    public void Read_WithNoVersionElement_ShouldReturnNull()
    {
        File.WriteAllText(Path.Combine(_dir, "sq.version"),
            """<package xmlns="http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"><metadata><id>x</id></metadata></package>""");

        PosAppVersion.Read(_dir).Should().BeNull();
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests --filter FullyQualifiedName~PosAppVersionTests`
Expected: FAIL — `PosAppVersion` does not exist.

- [ ] **Step 3: Write the implementation**

Create `installer/IndyPOS.Bootstrapper/Upgrade/PosAppVersion.cs`:

```csharp
using System.Xml.Linq;

namespace IndyPOS.Bootstrapper.Upgrade;

/// <summary>
/// Reads the POS app version Velopack believes is installed.
/// <para>A 2026-07-26 VM spike established that <c>Setup.exe --silent</c> exits 0 whether it
/// upgraded the app or merely repaired the same version, and that the binaries keep their
/// own build stamp regardless of the package version. Comparing this file before and after
/// is therefore the only honest way to derive POS_UPDATED.</para>
/// </summary>
public static class PosAppVersion
{
    public const string VersionFileName = "sq.version";

    /// <param name="velopackCurrentDirectory">
    /// The Velopack <c>current</c> directory — i.e. <see cref="Installers.InstallationConfig.VelopackInstallPath"/>.
    /// </param>
    /// <returns>The package version, or null when it cannot be established.</returns>
    public static string? Read(string velopackCurrentDirectory)
    {
        if (string.IsNullOrWhiteSpace(velopackCurrentDirectory))
        {
            return null;
        }

        var path = Path.Combine(velopackCurrentDirectory, VersionFileName);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            // sq.version is a nuspec document, not a bare version string. Match on local
            // name so the packaging namespace cannot break this.
            var version = XDocument.Load(path)
                .Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "version")
                ?.Value
                .Trim();

            return string.IsNullOrWhiteSpace(version) ? null : version;
        }
        catch (Exception ex) when (ex is System.Xml.XmlException or IOException)
        {
            return null;
        }
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests --filter FullyQualifiedName~PosAppVersionTests`
Expected: PASS, 5 tests.

- [ ] **Step 5: Commit**

```bash
git add installer/IndyPOS.Bootstrapper/Upgrade/PosAppVersion.cs tests/IndyPOS.Bootstrapper.Tests/Upgrade/PosAppVersionTests.cs
git commit -m "feat(installer): read the installed POS version from Velopack sq.version"
```

---

## Task 8: UpgradeBackup

Spec §4 step 3, §5 and §6. This is the artifact a failed upgrade is recovered from, so every one of its guarantees is load-bearing: ACL-locked (the dump is the entire sales history plus BCrypt admin hashes), integrity-checked (a disk-full `pg_dump` leaves a truncated file that still satisfies "present"), pruned (roughly 130 MB per stamp, forever, on a small retail PC nobody watches), and restored **delete-then-copy** (extraction is per-entry overwrite with no clean step, so the post-deploy tree is old ∪ new).

**Files:**
- Create: `installer/IndyPOS.Bootstrapper/Upgrade/UpgradeBackup.cs`
- Test: `tests/IndyPOS.Bootstrapper.Tests/Upgrade/UpgradeBackupTests.cs`

**Interfaces:**
- Consumes: `DatabaseSetup.TryRestrictFilePermissions(string)` (existing, `internal static`, returns `bool`)
- Produces:
  - `interface IProcessRunner { Task<ProcessRunResult> RunAsync(string fileName, string arguments, IReadOnlyDictionary<string, string>? environment, CancellationToken ct); }`
  - `sealed record ProcessRunResult(int ExitCode, string StandardOutput, string StandardError)`
  - `sealed class ExternalProcessRunner : IProcessRunner`
  - `sealed record BackupResult(bool Success, string? StampDirectory, bool Locked, string? ErrorMessage)`
  - `sealed class UpgradeBackup` with constructor
    `UpgradeBackup(string backupsDirectory, string storeHubInstallPath, IProcessRunner processRunner, Func<string> stampFactory, Func<string, bool>? lockPath = null)`
    and members
    `const int MinimumDumpBytes = 1024`, `const int RetainedStamps = 2`,
    `Task<BackupResult> CreateAsync(string pgDumpPath, string connectionString, CancellationToken ct)`,
    `void RestoreStoreHubTree(string stampDirectory)`,
    `int Prune()`

- [ ] **Step 1: Write the failing tests**

Create `tests/IndyPOS.Bootstrapper.Tests/Upgrade/UpgradeBackupTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Bootstrapper.Upgrade;

namespace IndyPOS.Bootstrapper.Tests.Upgrade;

public class UpgradeBackupTests : IDisposable
{
    private readonly string _root =
        Path.Combine(Path.GetTempPath(), "indypos-backup-" + Guid.NewGuid().ToString("N"));

    private readonly string _backups;
    private readonly string _storeHub;
    private readonly List<string> _lockedPaths = [];

    public UpgradeBackupTests()
    {
        _backups = Path.Combine(_root, "backups");
        _storeHub = Path.Combine(_root, "StoreHub");
        Directory.CreateDirectory(_backups);
        Directory.CreateDirectory(_storeHub);
        File.WriteAllText(Path.Combine(_storeHub, "IndyPOS.StoreHub.exe"), "old binary");
        File.WriteAllText(Path.Combine(_storeHub, "appsettings.json"), """{"store":{"id":"Rungrat-001"}}""");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    // Writes a dump of the requested size instead of shelling out to pg_dump.
    private sealed class FakePgDump(int exitCode, int dumpBytes) : IProcessRunner
    {
        public string? LastArguments { get; private set; }

        public Task<ProcessRunResult> RunAsync(
            string fileName, string arguments,
            IReadOnlyDictionary<string, string>? environment, CancellationToken ct)
        {
            LastArguments = arguments;

            // -f <path> is the last argument; mirror what pg_dump would produce.
            var marker = "-f \"";
            var start = arguments.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
            var path = arguments[start..arguments.IndexOf('"', start)];

            if (dumpBytes > 0)
            {
                File.WriteAllBytes(path, new byte[dumpBytes]);
            }

            return Task.FromResult(new ProcessRunResult(exitCode, "", exitCode == 0 ? "" : "boom"));
        }
    }

    private UpgradeBackup Build(IProcessRunner runner, string stamp = "20260726-010203") =>
        new(_backups, _storeHub, runner, () => stamp, p => { _lockedPaths.Add(p); return true; });

    private const string Conn = "Host=127.0.0.1;Port=5432;Database=indypos_storehub;Username=indypos_app;Password=s3cret";

    [Fact]
    public async Task CreateAsync_OnSuccess_ShouldCopyTheStoreHubTreeIntoTheStamp()
    {
        var result = await Build(new FakePgDump(0, 4096)).CreateAsync("pg_dump.exe", Conn, CancellationToken.None);

        result.Success.Should().BeTrue();
        File.ReadAllText(Path.Combine(result.StampDirectory!, "StoreHub", "IndyPOS.StoreHub.exe"))
            .Should().Be("old binary");
    }

    [Fact]
    public async Task CreateAsync_OnSuccess_ShouldWriteTheDumpBesideTheTree()
    {
        var result = await Build(new FakePgDump(0, 4096)).CreateAsync("pg_dump.exe", Conn, CancellationToken.None);

        File.Exists(Path.Combine(result.StampDirectory!, "storehub.dump")).Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_OnSuccess_ShouldLockTheStampAndBothArtifacts()
    {
        // BackupsDirectory inherits ProgramData's DACL, which grants BUILTIN\Users read.
        // The dump is the whole sales history plus BCrypt admin hashes.
        var result = await Build(new FakePgDump(0, 4096)).CreateAsync("pg_dump.exe", Conn, CancellationToken.None);

        result.Locked.Should().BeTrue();
        _lockedPaths.Should().Contain(result.StampDirectory!);
        _lockedPaths.Should().Contain(Path.Combine(result.StampDirectory!, "storehub.dump"));
        _lockedPaths.Should().Contain(Path.Combine(result.StampDirectory!, "StoreHub", "appsettings.json"));
    }

    [Fact]
    public async Task CreateAsync_WhenPgDumpFails_ShouldReportFailure()
    {
        var result = await Build(new FakePgDump(1, 4096)).CreateAsync("pg_dump.exe", Conn, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("pg_dump");
    }

    [Fact]
    public async Task CreateAsync_WhenTheDumpIsTruncated_ShouldRejectIt()
    {
        // A disk-full pg_dump can exit 0 having written almost nothing; "present" is not
        // "restorable", and BACKUP_DIR would otherwise advertise a dead artifact.
        var result = await Build(new FakePgDump(0, 10)).CreateAsync("pg_dump.exe", Conn, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("too small");
    }

    [Fact]
    public async Task CreateAsync_ShouldNeverPutThePasswordOnTheCommandLine()
    {
        // A command line is visible to every local process.
        var runner = new FakePgDump(0, 4096);
        await Build(runner).CreateAsync("pg_dump.exe", Conn, CancellationToken.None);

        runner.LastArguments.Should().NotContain("s3cret");
    }

    [Fact]
    public async Task CreateAsync_ShouldUseCustomFormatSoPgRestoreCanReadIt()
    {
        var runner = new FakePgDump(0, 4096);
        await Build(runner).CreateAsync("pg_dump.exe", Conn, CancellationToken.None);

        runner.LastArguments.Should().Contain("-Fc");
    }

    [Fact]
    public void RestoreStoreHubTree_ShouldDeleteBeforeCopying()
    {
        // Extraction is per-entry overwrite with no clean step, so after a deploy the tree
        // is old-union-new. Copying over that leaves new-version files behind.
        var stamp = Path.Combine(_backups, "20260726-010203");
        Directory.CreateDirectory(Path.Combine(stamp, "StoreHub"));
        File.WriteAllText(Path.Combine(stamp, "StoreHub", "IndyPOS.StoreHub.exe"), "old binary");

        File.WriteAllText(Path.Combine(_storeHub, "BrandNew.dll"), "from the failed upgrade");

        Build(new FakePgDump(0, 4096)).RestoreStoreHubTree(stamp);

        File.Exists(Path.Combine(_storeHub, "BrandNew.dll")).Should().BeFalse();
        File.ReadAllText(Path.Combine(_storeHub, "IndyPOS.StoreHub.exe")).Should().Be("old binary");
    }

    [Fact]
    public void RestoreStoreHubTree_WhenTheBackupIsMissing_ShouldThrow()
    {
        // Silently doing nothing here would report a rollback that never happened.
        var act = () => Build(new FakePgDump(0, 4096))
            .RestoreStoreHubTree(Path.Combine(_backups, "no-such-stamp"));

        act.Should().Throw<DirectoryNotFoundException>();
    }

    [Fact]
    public void Prune_WithMoreThanTheRetainedStamps_ShouldKeepOnlyTheNewest()
    {
        foreach (var stamp in new[] { "20260101-000000", "20260201-000000", "20260301-000000", "20260401-000000" })
        {
            Directory.CreateDirectory(Path.Combine(_backups, stamp));
        }

        var removed = Build(new FakePgDump(0, 4096)).Prune();

        removed.Should().Be(2);
        Directory.GetDirectories(_backups).Select(Path.GetFileName)
            .Should().BeEquivalentTo("20260301-000000", "20260401-000000");
    }

    [Fact]
    public void Prune_WithFewerThanTheRetainedStamps_ShouldRemoveNothing()
    {
        Directory.CreateDirectory(Path.Combine(_backups, "20260101-000000"));

        Build(new FakePgDump(0, 4096)).Prune().Should().Be(0);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests --filter FullyQualifiedName~UpgradeBackupTests`
Expected: FAIL — `UpgradeBackup`, `IProcessRunner`, `ProcessRunResult` do not exist.

- [ ] **Step 3: Write the process-runner seam**

Create `installer/IndyPOS.Bootstrapper/Upgrade/IProcessRunner.cs`:

```csharp
using System.Diagnostics;

namespace IndyPOS.Bootstrapper.Upgrade;

public sealed record ProcessRunResult(int ExitCode, string StandardOutput, string StandardError);

/// <summary>Seam over external tools (pg_dump, Velopack Setup.exe) so the sequence is testable.</summary>
public interface IProcessRunner
{
    Task<ProcessRunResult> RunAsync(
        string fileName,
        string arguments,
        IReadOnlyDictionary<string, string>? environment,
        CancellationToken cancellationToken = default);
}

public sealed class ExternalProcessRunner : IProcessRunner
{
    public async Task<ProcessRunResult> RunAsync(
        string fileName,
        string arguments,
        IReadOnlyDictionary<string, string>? environment,
        CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        if (environment is not null)
        {
            foreach (var (key, value) in environment)
            {
                psi.Environment[key] = value;
            }
        }

        using var process = new Process { StartInfo = psi };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await Task.WhenAll(stdoutTask, stderrTask);
        await process.WaitForExitAsync(cancellationToken);

        return new ProcessRunResult(process.ExitCode, await stdoutTask, await stderrTask);
    }
}
```

- [ ] **Step 4: Write UpgradeBackup**

Create `installer/IndyPOS.Bootstrapper/Upgrade/UpgradeBackup.cs`:

```csharp
using Npgsql;
using IndyPOS.Bootstrapper.Installers;

namespace IndyPOS.Bootstrapper.Upgrade;

public sealed record BackupResult(bool Success, string? StampDirectory, bool Locked, string? ErrorMessage);

/// <summary>
/// Takes and restores the pre-upgrade recovery artifacts (see the upgrade design spec,
/// sections 4, 5 and 6). Everything here runs ABOVE the mutation line except
/// <see cref="RestoreStoreHubTree"/>, which runs on the rollback path.
/// </summary>
public sealed class UpgradeBackup(
    string backupsDirectory,
    string storeHubInstallPath,
    IProcessRunner processRunner,
    Func<string> stampFactory,
    Func<string, bool>? lockPath = null)
{
    public const string DumpFileName = "storehub.dump";

    /// <summary>A pg_dump that exits 0 having written less than this did not really succeed.</summary>
    public const int MinimumDumpBytes = 1024;

    /// <summary>Roughly 130 MB per stamp, forever, on a small unattended retail PC.</summary>
    public const int RetainedStamps = 2;

    private readonly Func<string, bool> _lockPath =
        lockPath ?? DatabaseSetup.TryRestrictFilePermissions;

    public async Task<BackupResult> CreateAsync(
        string pgDumpPath,
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        var stampDir = Path.Combine(backupsDirectory, stampFactory());

        try
        {
            // BackupsDirectory exists in InstallationConfig and the manifest, but only
            // DatabaseSetup.CreateDirectories guarantees it — and that never runs here.
            Directory.CreateDirectory(stampDir);

            var dumpPath = Path.Combine(stampDir, DumpFileName);
            var dumpFailure = await RunPgDumpAsync(pgDumpPath, connectionString, dumpPath, cancellationToken);
            if (dumpFailure is not null)
            {
                return new BackupResult(false, stampDir, false, dumpFailure);
            }

            var treeDestination = Path.Combine(stampDir, "StoreHub");
            CopyDirectory(storeHubInstallPath, treeDestination);

            var locked = LockTree(stampDir, dumpPath, treeDestination);

            Prune();

            return new BackupResult(true, stampDir, locked, null);
        }
        catch (Exception ex)
        {
            return new BackupResult(false, stampDir, false, ex.Message);
        }
    }

    /// <returns>An error message, or null on success.</returns>
    private async Task<string?> RunPgDumpAsync(
        string pgDumpPath, string connectionString, string dumpPath, CancellationToken cancellationToken)
    {
        // NpgsqlConnectionStringBuilder, not string splitting: the password may contain
        // anything, and a mis-split silently dumps the wrong database.
        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        var arguments =
            $"--host={builder.Host} --port={builder.Port} --username={builder.Username} " +
            $"--dbname={builder.Database} --no-password -Fc -f \"{dumpPath}\"";

        // PGPASSWORD via the environment, never the command line: a command line is
        // visible to every local process.
        var environment = new Dictionary<string, string> { ["PGPASSWORD"] = builder.Password ?? "" };

        var run = await processRunner.RunAsync(pgDumpPath, arguments, environment, cancellationToken);

        if (run.ExitCode != 0)
        {
            return $"pg_dump failed (exit {run.ExitCode}): {run.StandardError.Trim()}";
        }

        if (!File.Exists(dumpPath))
        {
            return "pg_dump reported success but produced no file.";
        }

        var length = new FileInfo(dumpPath).Length;
        return length < MinimumDumpBytes
            ? $"pg_dump produced a file that is too small to be a valid dump ({length} bytes)."
            : null;
    }

    private bool LockTree(string stampDir, string dumpPath, string treeDestination)
    {
        var locked = _lockPath(stampDir) & _lockPath(dumpPath);

        foreach (var file in Directory.EnumerateFiles(treeDestination, "*", SearchOption.AllDirectories))
        {
            locked &= _lockPath(file);
        }

        return locked;
    }

    /// <summary>
    /// Puts the backed-up StoreHub tree back. DELETE-then-copy: extraction overwrites
    /// per entry with no clean step, so the failed tree is old-union-new and copying over
    /// it would strand new-version files (see the design spec, section 5).
    /// </summary>
    public void RestoreStoreHubTree(string stampDirectory)
    {
        var source = Path.Combine(stampDirectory, "StoreHub");

        if (!Directory.Exists(source))
        {
            throw new DirectoryNotFoundException(
                $"No StoreHub backup at {source}; cannot roll back. The database dump, if any, " +
                $"is still at {Path.Combine(stampDirectory, DumpFileName)}.");
        }

        if (Directory.Exists(storeHubInstallPath))
        {
            Directory.Delete(storeHubInstallPath, recursive: true);
        }

        CopyDirectory(source, storeHubInstallPath);
    }

    /// <summary>Keeps the newest <see cref="RetainedStamps"/> stamps. Returns how many it removed.</summary>
    public int Prune()
    {
        if (!Directory.Exists(backupsDirectory))
        {
            return 0;
        }

        // Stamp names are yyyyMMdd-HHmmss, so ordinal descending IS newest-first, and it
        // does not depend on directory timestamps a restore may have rewritten.
        var stale = Directory.GetDirectories(backupsDirectory)
            .OrderByDescending(Path.GetFileName, StringComparer.Ordinal)
            .Skip(RetainedStamps)
            .ToList();

        foreach (var dir in stale)
        {
            try
            {
                Directory.Delete(dir, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Retention is housekeeping; never fail an upgrade over it.
            }
        }

        return stale.Count;
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), overwrite: true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            CopyDirectory(dir, Path.Combine(destDir, Path.GetFileName(dir)));
        }
    }
}
```

`DatabaseSetup.TryRestrictFilePermissions` is `internal static` and both types are in the same assembly, so no accessibility change is needed. It is documented for files; confirm it also works on a directory — `FileInfo.GetAccessControl` on a directory path throws, so if the `_lockPath(stampDir)` call fails at runtime, add a `TryRestrictDirectoryPermissions` sibling using `DirectoryInfo`/`DirectorySecurity` with the same two SIDs and `SetAccessRuleProtection(true, false)`.

Add `<PackageReference Include="Npgsql" />` to `installer/IndyPOS.Bootstrapper/IndyPOS.Bootstrapper.csproj` if it is not already referenced (check first: `git grep -n Npgsql installer/IndyPOS.Bootstrapper/IndyPOS.Bootstrapper.csproj`).

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests --filter FullyQualifiedName~UpgradeBackupTests`
Expected: PASS, 11 tests.

- [ ] **Step 6: Commit**

```bash
git add installer/IndyPOS.Bootstrapper/Upgrade/IProcessRunner.cs installer/IndyPOS.Bootstrapper/Upgrade/UpgradeBackup.cs installer/IndyPOS.Bootstrapper/IndyPOS.Bootstrapper.csproj tests/IndyPOS.Bootstrapper.Tests/Upgrade/UpgradeBackupTests.cs
git commit -m "feat(installer): back up the database and StoreHub tree before an upgrade"
```

---

## Task 9: UpgradePreflight

Spec §4 step 1. Everything here runs **above the mutation line** — a failure means nothing has been touched and the store is still serving on its current version.

The prerequisite ensures are not optional padding. The bundled FC Subject fonts were only added on 2026-07-14, so a store installed before that build would receive a UI-polish release and render it in a fallback face — a visible regression delivered *by* the upgrade. A future .NET major bump would otherwise deploy binaries the machine cannot run, surfacing as a dead service *after* the mutation line.

**Files:**
- Create: `installer/IndyPOS.Bootstrapper/Upgrade/UpgradePreflight.cs`
- Modify: `installer/IndyPOS.Bootstrapper/Installers/PostgresInstaller.cs` (expose the finder)
- Test: `tests/IndyPOS.Bootstrapper.Tests/Upgrade/UpgradePreflightTests.cs`

**Interfaces:**
- Consumes: `DetectedInstall` (Task 3), `InstallationConfig`, `FontInstaller.Install`, `DotNetInstaller.EnsureInstalledAsync`, `VCRedistInstaller.EnsureInstalledAsync`
- Produces:
  - `sealed record PreflightResult(bool Ok, string? PgDumpPath, string? ConnectionString, string? FailureReason, bool IsDowngrade)`
  - `static class UpgradePreflight` with
    `static (bool Ok, string? Reason, bool IsDowngrade) CheckVersions(string? installedVersion, string installerVersion)`,
    `static bool IsPosAppRunning()`,
    `static string? LocatePgDump(string manifestBinPath, int expectedMajor)`
  - On `PostgresInstaller`: `internal static string? FindPostgresBinPath()` — a thin, testable wrapper over the existing private `FindPostgresInstallation()` that returns just `BinPath`

- [ ] **Step 1: Write the failing tests**

Create `tests/IndyPOS.Bootstrapper.Tests/Upgrade/UpgradePreflightTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Bootstrapper.Upgrade;

namespace IndyPOS.Bootstrapper.Tests.Upgrade;

public class UpgradePreflightTests : IDisposable
{
    private readonly string _dir =
        Path.Combine(Path.GetTempPath(), "indypos-preflight-" + Guid.NewGuid().ToString("N"));

    public UpgradePreflightTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    [Fact]
    public void CheckVersions_WhenTheInstallerIsNewer_ShouldPass()
    {
        var (ok, _, isDowngrade) = UpgradePreflight.CheckVersions("4.0.0", "4.1.0");

        ok.Should().BeTrue();
        isDowngrade.Should().BeFalse();
    }

    [Fact]
    public void CheckVersions_WithTheSameVersion_ShouldPassAsARepair()
    {
        // Directory.Build.props pins the version and needs a manual bump, so a
        // same-version run is the common case during development. The 2026-07-26 spike
        // measured it as a safe 2.5s repair.
        var (ok, _, isDowngrade) = UpgradePreflight.CheckVersions("4.0.0", "4.0.0");

        ok.Should().BeTrue();
        isDowngrade.Should().BeFalse();
    }

    [Fact]
    public void CheckVersions_WhenTheInstallerIsOlder_ShouldRefuse()
    {
        var (ok, reason, isDowngrade) = UpgradePreflight.CheckVersions("4.1.0", "4.0.0");

        ok.Should().BeFalse();
        isDowngrade.Should().BeTrue();
        reason.Should().Contain("4.1.0");
    }

    [Fact]
    public void CheckVersions_WithAnUnreadableInstalledVersion_ShouldPass()
    {
        // A manifest without installVersion is decorative, not a blocker.
        UpgradePreflight.CheckVersions(null, "4.0.0").Ok.Should().BeTrue();
    }

    [Fact]
    public void LocatePgDump_WhenTheManifestPathIsGood_ShouldUseIt()
    {
        var bin = Path.Combine(_dir, "18", "bin");
        Directory.CreateDirectory(bin);
        File.WriteAllText(Path.Combine(bin, "pg_dump.exe"), "");

        UpgradePreflight.LocatePgDump(bin, expectedMajor: 18)
            .Should().Be(Path.Combine(bin, "pg_dump.exe"));
    }

    [Fact]
    public void LocatePgDump_WhenTheManifestPathIsStale_ShouldReturnNullRatherThanAWrongMajor()
    {
        // FindPostgresInstallation probes 18 -> 17 -> 16; a pg_dump from a different
        // major can refuse the dump outright, so assert the major rather than guess.
        var bin = Path.Combine(_dir, "16", "bin");
        Directory.CreateDirectory(bin);
        File.WriteAllText(Path.Combine(bin, "pg_dump.exe"), "");

        UpgradePreflight.LocatePgDump(bin, expectedMajor: 18).Should().BeNull();
    }

    [Fact]
    public void LocatePgDump_WhenTheManifestPathDoesNotExist_ShouldReturnNull()
    {
        UpgradePreflight.LocatePgDump(Path.Combine(_dir, "nope"), expectedMajor: 18).Should().BeNull();
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests --filter FullyQualifiedName~UpgradePreflightTests`
Expected: FAIL — `UpgradePreflight` does not exist.

- [ ] **Step 3: Expose the Postgres finder**

In `installer/IndyPOS.Bootstrapper/Installers/PostgresInstaller.cs`, add next to the existing private `FindPostgresInstallation()`:

```csharp
    /// <summary>
    /// The bin directory of the PostgreSQL install this machine actually has, or null.
    /// Exposed for the upgrade path's pg_dump fallback: the manifest's PostgresBinPath
    /// can be stale. Note this probes 18 -> 17 -> 16, so callers must assert the major.
    /// </summary>
    internal static string? FindPostgresBinPath() => FindPostgresInstallation()?.BinPath;
```

- [ ] **Step 4: Write UpgradePreflight**

Create `installer/IndyPOS.Bootstrapper/Upgrade/UpgradePreflight.cs`:

```csharp
using System.Diagnostics;
using IndyPOS.Bootstrapper.Installers;

namespace IndyPOS.Bootstrapper.Upgrade;

public sealed record PreflightResult(
    bool Ok,
    string? PgDumpPath,
    string? ConnectionString,
    string? FailureReason,
    bool IsDowngrade);

/// <summary>
/// Step 1 of the upgrade sequence. Everything here runs ABOVE the mutation line: a
/// failure leaves the store untouched and still serving its current version.
/// </summary>
public static class UpgradePreflight
{
    /// <summary>Enough headroom for the dump plus a full copy of the StoreHub tree.</summary>
    public const long RequiredFreeBytes = 2L * 1024 * 1024 * 1024;

    private const string PosProcessName = "IndyPOS.Windows.Forms";

    /// <summary>
    /// Refuse a downgrade; allow a same-version repair. Rollback restores binaries and
    /// config but not schema, so running older binaries against a newer schema is exactly
    /// the state the forward-only migration rule cannot protect.
    /// </summary>
    public static (bool Ok, string? Reason, bool IsDowngrade) CheckVersions(
        string? installedVersion, string installerVersion)
    {
        if (!Version.TryParse(installedVersion, out var installed) ||
            !Version.TryParse(installerVersion, out var installing))
        {
            return (true, null, false);
        }

        return installing < installed
            ? (false,
               $"This installer is version {installerVersion} but the machine already runs " +
               $"{installedVersion}. Downgrading is refused: it would leave older binaries " +
               "against a newer database schema.",
               true)
            : (true, null, false);
    }

    /// <summary>
    /// Two Velopack processes on one install root can leave the POS unlaunchable, and the
    /// POS app self-updates independently, so it may be mid-update already.
    /// </summary>
    public static bool IsPosAppRunning() =>
        Process.GetProcessesByName(PosProcessName).Length > 0;

    /// <summary>
    /// The manifest's PostgresBinPath can be stale, so fall back to probing — but assert
    /// the major matches, because the fallback happily returns 17 or 16.
    /// </summary>
    public static string? LocatePgDump(string manifestBinPath, int expectedMajor)
    {
        var candidate = Probe(manifestBinPath, expectedMajor);
        if (candidate is not null)
        {
            return candidate;
        }

        var discovered = PostgresInstaller.FindPostgresBinPath();
        return discovered is null ? null : Probe(discovered, expectedMajor);
    }

    private static string? Probe(string binPath, int expectedMajor)
    {
        if (string.IsNullOrWhiteSpace(binPath))
        {
            return null;
        }

        var exe = Path.Combine(binPath, "pg_dump.exe");
        if (!File.Exists(exe))
        {
            return null;
        }

        // Layout is ...\PostgreSQL\<major>\bin, so the grandparent names the major.
        var majorDir = Path.GetFileName(Path.GetDirectoryName(binPath.TrimEnd(Path.DirectorySeparatorChar)));

        return int.TryParse(majorDir, out var major) && major == expectedMajor ? exe : null;
    }

    public static bool HasFreeSpace(string path)
    {
        try
        {
            return new DriveInfo(Path.GetPathRoot(Path.GetFullPath(path))!).AvailableFreeSpace
                   >= RequiredFreeBytes;
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
        {
            // Unknowable is not the same as insufficient; let the dump's own integrity
            // check be the backstop rather than blocking a legitimate upgrade.
            return true;
        }
    }
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests --filter FullyQualifiedName~UpgradePreflightTests`
Expected: PASS, 7 tests.

- [ ] **Step 6: Commit**

```bash
git add installer/IndyPOS.Bootstrapper/Upgrade/UpgradePreflight.cs installer/IndyPOS.Bootstrapper/Installers/PostgresInstaller.cs tests/IndyPOS.Bootstrapper.Tests/Upgrade/UpgradePreflightTests.cs
git commit -m "feat(installer): add upgrade preflight checks above the mutation line"
```

---

## Task 10: Upgrade outcomes and markers

Spec §7. `SilentOutcomeMapper.Map` ends in `_ => throw new ArgumentOutOfRangeException`, so every new outcome needs an explicit arm. New **sibling records** rather than widening `InstallSucceeded`, which is already a five-field positional record belonging to the frozen fresh path.

The admin-marker suppression is not cosmetic: `SuccessMarkers` always emits `ADMIN_SEEDED`, which on an upgrade is necessarily `false` (a fresh install stripped the `InitialAdmin` block, so the seeder skips), and the `else` branch then emits `RESET_HINT=run "IndyPOS.StoreHub.exe reset-admin"…` — advising a bootstrap-password reissue on a store whose admin is perfectly fine.

**Files:**
- Modify: `installer/IndyPOS.Bootstrapper/Silent/SilentOutcomeMapper.cs`
- Test: `tests/IndyPOS.Bootstrapper.Tests/Silent/SilentOutcomeMapperTests.cs`

**Interfaces:**
- Consumes: nothing new
- Produces, all `: SilentOutcome`:
  - `sealed record UpgradeSucceeded(string? FromVersion, string ToVersion, bool ServiceStarted, bool HealthOk, string BackupDir, bool BackupLocked, bool PosUpdated)`
  - `sealed record UpgradeFailed(string Message, bool RolledBack, bool ServiceStarted, bool HealthOk, string? BackupDir)`
  - `sealed record UnusableInstall(string Reason)`
  - `sealed record DowngradeRefused(string InstalledVersion, string InstallerVersion)`
  - `static string SilentOutcomeMapper.ModeMarker(InstallMode mode)`

- [ ] **Step 1: Write the failing tests**

Append to `tests/IndyPOS.Bootstrapper.Tests/Silent/SilentOutcomeMapperTests.cs` (add `using IndyPOS.Bootstrapper.Upgrade;`):

```csharp
    [Fact]
    public void Map_WithUpgradeSucceeded_ShouldReturnZeroAndTheUpgradeMarkers()
    {
        var outcome = new UpgradeSucceeded("4.0.0", "4.1.0", ServiceStarted: true, HealthOk: true,
            BackupDir: @"C:\ProgramData\IndyPOS\v4\backups\20260726-010203", BackupLocked: true,
            PosUpdated: true);

        var (exit, markers) = SilentOutcomeMapper.Map(outcome);

        exit.Should().Be(0);
        markers.Should().Contain("INDYPOS_MARKER RESULT=success");
        markers.Should().Contain("INDYPOS_MARKER SERVICE_STARTED=true");
        markers.Should().Contain("INDYPOS_MARKER HEALTH=ok");
        markers.Should().Contain(@"INDYPOS_MARKER BACKUP_DIR=C:\ProgramData\IndyPOS\v4\backups\20260726-010203");
        markers.Should().Contain("INDYPOS_MARKER BACKUP_LOCKED=true");
        markers.Should().Contain("INDYPOS_MARKER POS_UPDATED=true");
        markers.Should().Contain("INDYPOS_MARKER FROM_VERSION=4.0.0");
        markers.Should().Contain("INDYPOS_MARKER TO_VERSION=4.1.0");
    }

    [Fact]
    public void Map_WithUpgradeSucceeded_ShouldSuppressTheFreshPathAdminMarkers()
    {
        // ADMIN_SEEDED is necessarily false on an upgrade, and its else-branch would
        // advise reissuing a bootstrap password for a store whose admin is fine.
        var outcome = new UpgradeSucceeded("4.0.0", "4.1.0", true, true, @"C:\b", true, true);

        var (_, markers) = SilentOutcomeMapper.Map(outcome);

        markers.Should().NotContain(m => m.StartsWith("INDYPOS_MARKER ADMIN_SEEDED="));
        markers.Should().NotContain(m => m.StartsWith("INDYPOS_MARKER RESET_HINT="));
        markers.Should().NotContain(m => m.StartsWith("INDYPOS_MARKER CRED_FILE="));
    }

    [Fact]
    public void Map_WithAPosOnlyFailure_ShouldStillReportTheServerAsServing()
    {
        // Spec section 5 row 8: no rollback -- the server is upgraded and serving. The 2026-07-26
        // spike confirmed the POS step cannot disturb the service.
        var outcome = new UpgradeFailed("Velopack exited 1", RolledBack: false,
            ServiceStarted: true, HealthOk: true, BackupDir: @"C:\b");

        var (exit, markers) = SilentOutcomeMapper.Map(outcome);

        exit.Should().Be(2);
        markers.Should().Contain("INDYPOS_MARKER RESULT=failed");
        markers.Should().Contain("INDYPOS_MARKER SERVICE_STARTED=true");
        markers.Should().Contain("INDYPOS_MARKER POS_UPDATED=false");
        markers.Should().Contain("INDYPOS_MARKER ROLLED_BACK=false");
    }

    [Fact]
    public void Map_WithARolledBackFailure_ShouldReportThePostRollbackHealth()
    {
        // Starting the service is not evidence it came back; without the probe a rollback
        // can claim the store resumed while the store is dead.
        var outcome = new UpgradeFailed("migrate failed", RolledBack: true,
            ServiceStarted: true, HealthOk: false, BackupDir: @"C:\b\20260726-010203");

        var (exit, markers) = SilentOutcomeMapper.Map(outcome);

        exit.Should().Be(2);
        markers.Should().Contain("INDYPOS_MARKER ROLLED_BACK=true");
        markers.Should().Contain("INDYPOS_MARKER HEALTH=failed");
        markers.Should().Contain(@"INDYPOS_MARKER BACKUP_DIR=C:\b\20260726-010203");
    }

    [Fact]
    public void Map_WithAnUnusableInstall_ShouldReturnFiveAndCarryTheReason()
    {
        var (exit, markers) = SilentOutcomeMapper.Map(new UnusableInstall("Store:Type is missing"));

        exit.Should().Be(5);
        markers.Should().Contain("INDYPOS_MARKER RESULT=failed");
        markers.Should().Contain("INDYPOS_MARKER REASON=Store:Type is missing");
    }

    [Fact]
    public void Map_WithADowngrade_ShouldReturnSix()
    {
        var (exit, markers) = SilentOutcomeMapper.Map(new DowngradeRefused("4.1.0", "4.0.0"));

        exit.Should().Be(6);
        markers.Should().Contain("INDYPOS_MARKER RESULT=failed");
        markers.Should().Contain(m => m.Contains("4.1.0") && m.Contains("4.0.0"));
    }

    [Theory]
    [InlineData(InstallMode.Fresh, "INDYPOS_MARKER MODE=fresh")]
    [InlineData(InstallMode.Upgrade, "INDYPOS_MARKER MODE=upgrade")]
    [InlineData(InstallMode.Unusable, "INDYPOS_MARKER MODE=unusable")]
    public void ModeMarker_ForEachMode_ShouldEmitTheLowercaseName(InstallMode mode, string expected)
    {
        // Months later, a store's own install-latest.log must answer "which path ran"
        // without anyone reading C#.
        SilentOutcomeMapper.ModeMarker(mode).Should().Be(expected);
    }

    [Fact]
    public void Map_ForEveryOutcomeType_ShouldHaveAnExplicitArm()
    {
        // Map ends in a throwing discard, so a new outcome silently crashes the run.
        var outcomes = new SilentOutcome[]
        {
            new InstallSucceeded(true, @"C:\x", true, true, true),
            new InstallFailed("x"),
            new InstallTimedOut(),
            new UsageErrorOutcome("x"),
            new NotElevatedOutcome(),
            new UpgradeSucceeded("4.0.0", "4.1.0", true, true, @"C:\b", true, true),
            new UpgradeFailed("x", false, true, true, @"C:\b"),
            new UnusableInstall("x"),
            new DowngradeRefused("4.1.0", "4.0.0")
        };

        foreach (var outcome in outcomes)
        {
            var act = () => SilentOutcomeMapper.Map(outcome);
            act.Should().NotThrow($"{outcome.GetType().Name} needs an explicit arm");
        }
    }
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests --filter FullyQualifiedName~SilentOutcomeMapperTests`
Expected: FAIL — the new outcome records do not exist.

- [ ] **Step 3: Write the implementation**

In `installer/IndyPOS.Bootstrapper/Silent/SilentOutcomeMapper.cs`, add `using IndyPOS.Bootstrapper.Upgrade;` and the new records beside the existing ones:

```csharp
/// <summary>
/// An in-place upgrade completed. Sibling of <see cref="InstallSucceeded"/> rather than a
/// widening of it: that record is a five-field positional type belonging to the frozen
/// fresh path, and an upgrade reports a genuinely different set of facts.
/// </summary>
public sealed record UpgradeSucceeded(
    string? FromVersion, string ToVersion, bool ServiceStarted, bool HealthOk,
    string BackupDir, bool BackupLocked, bool PosUpdated) : SilentOutcome;

/// <param name="ServiceStarted">Post-rollback when <paramref name="RolledBack"/> is true.</param>
/// <param name="HealthOk">Post-rollback when <paramref name="RolledBack"/> is true.</param>
public sealed record UpgradeFailed(
    string Message, bool RolledBack, bool ServiceStarted, bool HealthOk,
    string? BackupDir) : SilentOutcome;

public sealed record UnusableInstall(string Reason) : SilentOutcome;

public sealed record DowngradeRefused(string InstalledVersion, string InstallerVersion) : SilentOutcome;
```

Extend `Map` and add the two new marker builders plus `ModeMarker`:

```csharp
    public static (int ExitCode, IReadOnlyList<string> Markers) Map(SilentOutcome outcome) => outcome switch
    {
        InstallSucceeded s => (0, SuccessMarkers(s)),
        InstallFailed => (2, [Prefix + "RESULT=failed"]),
        InstallTimedOut => (4, [Prefix + "RESULT=timeout"]),
        UsageErrorOutcome u => (1, [Prefix + "RESULT=failed", Prefix + $"REASON={u.Message}"]),
        NotElevatedOutcome => (3, [Prefix + "RESULT=failed"]),
        UpgradeSucceeded u => (0, UpgradeSuccessMarkers(u)),
        UpgradeFailed u => (2, UpgradeFailureMarkers(u)),
        UnusableInstall u => (5, [Prefix + "RESULT=failed", Prefix + $"REASON={u.Reason}"]),
        DowngradeRefused d => (6, [
            Prefix + "RESULT=failed",
            Prefix + $"REASON=Installed version {d.InstalledVersion} is newer than this " +
                     $"installer ({d.InstallerVersion}); downgrade refused."]),
        _ => throw new ArgumentOutOfRangeException(nameof(outcome))
    };

    /// <summary>
    /// Written to the log by the router before either orchestrator runs, so months later a
    /// store's own install-latest.log answers "which path ran".
    /// </summary>
    public static string ModeMarker(InstallMode mode) =>
        Prefix + "MODE=" + mode.ToString().ToLowerInvariant();

    // No ADMIN_SEEDED / RESET_HINT / CRED_FILE here: those belong to the fresh path, and
    // on an upgrade the reset hint actively misleads (spec section 7).
    private static IReadOnlyList<string> UpgradeSuccessMarkers(UpgradeSucceeded u) =>
    [
        Prefix + "RESULT=success",
        Prefix + $"FROM_VERSION={u.FromVersion ?? "unknown"}",
        Prefix + $"TO_VERSION={u.ToVersion}",
        Prefix + $"SERVICE_STARTED={Lower(u.ServiceStarted)}",
        Prefix + $"HEALTH={(u.HealthOk ? "ok" : "failed")}",
        Prefix + $"BACKUP_DIR={u.BackupDir}",
        Prefix + $"BACKUP_LOCKED={Lower(u.BackupLocked)}",
        Prefix + $"POS_UPDATED={Lower(u.PosUpdated)}"
    ];

    private static IReadOnlyList<string> UpgradeFailureMarkers(UpgradeFailed u)
    {
        var markers = new List<string>
        {
            Prefix + "RESULT=failed",
            Prefix + $"ROLLED_BACK={Lower(u.RolledBack)}",
            Prefix + $"SERVICE_STARTED={Lower(u.ServiceStarted)}",
            Prefix + $"HEALTH={(u.HealthOk ? "ok" : "failed")}",
            Prefix + "POS_UPDATED=false"
        };

        if (u.BackupDir is not null)
        {
            markers.Add(Prefix + $"BACKUP_DIR={u.BackupDir}");
        }

        return markers;
    }
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests --filter FullyQualifiedName~SilentOutcomeMapperTests`
Expected: PASS — 8 new tests plus the 7 pre-existing ones, all 15.

- [ ] **Step 5: Commit**

```bash
git add installer/IndyPOS.Bootstrapper/Silent/SilentOutcomeMapper.cs tests/IndyPOS.Bootstrapper.Tests/Silent/SilentOutcomeMapperTests.cs
git commit -m "feat(installer): add upgrade outcomes, exit codes and markers"
```

---

## Task 11: UpgradeOrchestrator

Spec §4 and §5. The whole sequence, and the mutation line is the design's load-bearing idea: steps 0–3 touch nothing, so a failure there is a non-event; steps 4–7 roll back; step 8 does not.

Step 5 is the heart of it. `DatabaseSetup` never runs, so there is no superuser password to need, no role to create and no connection string to generate — which is what makes the original Failure 2 *disappear* rather than be worked around.

**Files:**
- Create: `installer/IndyPOS.Bootstrapper/Upgrade/UpgradeOrchestrator.cs`, `installer/IndyPOS.Bootstrapper/Upgrade/SimulatedFailure.cs`
- Test: `tests/IndyPOS.Bootstrapper.Tests/Upgrade/UpgradeOrchestratorTests.cs`

**Interfaces:**
- Consumes: `DetectedInstall`, `UpgradeBackup`, `UpgradePreflight`, `PosAppVersion`, `ServiceControl`, `StoreHubPayload`, `MigrationRunner`, `HealthProbe`, `ConfigSnapshot`, `InstallManifestWriter`, and the upgrade outcome records
- Produces:
  - `enum UpgradeStage { Preflight, Stop, Backup, Deploy, RestoreConfig, Migrate, Start, PosApp }`
  - `static class SimulatedFailure` with `static UpgradeStage? Parse(string? value)` and `const string ArgumentName = "--simulate-failure"`
  - `interface IUpgradeSteps` — the seam the orchestrator drives, so the sequence and its rollback are unit-testable without a real machine
  - `sealed class UpgradeOrchestrator(IUpgradeSteps steps, UpgradeStage? simulateFailure = null)` with
    `Task<SilentOutcome> RunAsync(DetectedInstall detected, InstallationConfig config, IProgress<InstallationProgress> progress, CancellationToken ct)`

- [ ] **Step 1: Write the failing tests**

Create `tests/IndyPOS.Bootstrapper.Tests/Upgrade/UpgradeOrchestratorTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Bootstrapper.Installers;
using IndyPOS.Bootstrapper.Silent;
using IndyPOS.Bootstrapper.Upgrade;
using IndyPOS.Domain.Enums;

namespace IndyPOS.Bootstrapper.Tests.Upgrade;

public class UpgradeOrchestratorTests
{
    private const string BackupDir = @"C:\ProgramData\IndyPOS\v4\backups\20260726-010203";

    private static readonly DetectedInstall Detected = new(
        InstallMode.Upgrade, "4.0.0", "Rungrat-001", StoreType.Minimart, "ok");

    private static InstallationConfig Config() => new() { StoreId = "Rungrat-001", Interactive = false };

    private sealed class FakeSteps : IUpgradeSteps
    {
        public List<string> Calls { get; } = [];

        public bool PreflightOk { get; init; } = true;
        public string PreflightReason { get; init; } = "preflight failed";
        public bool IsDowngrade { get; init; }
        public bool BackupOk { get; init; } = true;
        public bool DeployOk { get; init; } = true;
        public bool MigrateOk { get; init; } = true;
        public bool StartOk { get; init; } = true;
        public bool HealthOk { get; set; } = true;
        public bool PostRollbackHealthOk { get; init; } = true;
        public bool PosOk { get; init; } = true;
        public string? PosVersionAfter { get; init; } = "4.1.0";
        public bool RestoreThrows { get; init; }

        public Task<PreflightResult> PreflightAsync(DetectedInstall d, InstallationConfig c, CancellationToken ct)
        {
            Calls.Add("preflight");
            return Task.FromResult(new PreflightResult(
                PreflightOk, @"C:\pg\bin\pg_dump.exe", "Host=127.0.0.1",
                PreflightOk ? null : PreflightReason, IsDowngrade));
        }

        public Task<bool> StopServiceAsync(CancellationToken ct)
        {
            Calls.Add("stop");
            return Task.FromResult(true);
        }

        public Task<BackupResult> BackupAsync(string pgDump, string conn, CancellationToken ct)
        {
            Calls.Add("backup");
            return Task.FromResult(new BackupResult(BackupOk, BackupDir, true,
                BackupOk ? null : "pg_dump failed"));
        }

        public ConfigSnapshotHandle CaptureConfig()
        {
            Calls.Add("capture");
            return new ConfigSnapshotHandle(() => Calls.Add("restore-config"));
        }

        public Task<bool> DeployAsync(CancellationToken ct)
        {
            Calls.Add("deploy");
            return Task.FromResult(DeployOk);
        }

        public Task<MigrationRunResult> MigrateAsync(CancellationToken ct)
        {
            Calls.Add("migrate");
            return Task.FromResult(new MigrationRunResult(MigrateOk, false, MigrateOk ? null : "42703"));
        }

        public Task<bool> StartServiceAsync(CancellationToken ct)
        {
            Calls.Add("start");
            return Task.FromResult(StartOk);
        }

        public Task<bool> HealthAsync(CancellationToken ct)
        {
            Calls.Add("health");
            return Task.FromResult(Calls.Count(c => c == "health") > 1 ? PostRollbackHealthOk : HealthOk);
        }

        public string? ReadPosVersion()
        {
            Calls.Add("pos-version");
            return Calls.Count(c => c == "pos-version") == 1 ? "4.0.0" : PosVersionAfter;
        }

        public Task<bool> InstallPosAppAsync(CancellationToken ct)
        {
            Calls.Add("pos-install");
            return Task.FromResult(PosOk);
        }

        public void RestoreStoreHubTree(string stampDir)
        {
            Calls.Add("restore-tree");
            if (RestoreThrows) throw new DirectoryNotFoundException("no backup");
        }

        public Task WriteManifestAsync(CancellationToken ct)
        {
            Calls.Add("manifest");
            return Task.CompletedTask;
        }
    }

    private static async Task<SilentOutcome> Run(FakeSteps steps, UpgradeStage? fault = null) =>
        await new UpgradeOrchestrator(steps, fault).RunAsync(
            Detected, Config(), new Progress<InstallationProgress>(_ => { }), CancellationToken.None);

    [Fact]
    public async Task RunAsync_OnTheHappyPath_ShouldSucceed()
    {
        var outcome = await Run(new FakeSteps());

        outcome.Should().BeOfType<UpgradeSucceeded>()
            .Which.BackupDir.Should().Be(BackupDir);
    }

    [Fact]
    public async Task RunAsync_ShouldStopTheServiceBeforeDumping()
    {
        // A sale completed between the dump and the restart would exist only in the live
        // database, and "restore the dump" is the documented recovery -- so that window
        // would be silently discarded.
        var steps = new FakeSteps();

        await Run(steps);

        steps.Calls.IndexOf("stop").Should().BeLessThan(steps.Calls.IndexOf("backup"));
    }

    [Fact]
    public async Task RunAsync_ShouldRestoreConfigAfterDeploy()
    {
        // Extraction overwrites appsettings.json with the package template; step 5 is what
        // puts the store's real config back, and it must land before migrate reads it.
        var steps = new FakeSteps();

        await Run(steps);

        steps.Calls.IndexOf("deploy").Should().BeLessThan(steps.Calls.IndexOf("restore-config"));
        steps.Calls.IndexOf("restore-config").Should().BeLessThan(steps.Calls.IndexOf("migrate"));
    }

    [Fact]
    public async Task RunAsync_ShouldNeverCallDatabaseSetup()
    {
        // Enforced structurally: IUpgradeSteps exposes no such member. This test documents
        // the invariant so a future widening of the seam is a deliberate act.
        typeof(IUpgradeSteps).GetMethods().Select(m => m.Name)
            .Should().NotContain(n => n.Contains("DatabaseSetup", StringComparison.OrdinalIgnoreCase));

        await Task.CompletedTask;
    }

    [Fact]
    public async Task RunAsync_WhenPreflightFails_ShouldNotTouchAnything()
    {
        var steps = new FakeSteps { PreflightOk = false };

        var outcome = await Run(steps);

        outcome.Should().BeOfType<UpgradeFailed>().Which.RolledBack.Should().BeFalse();
        steps.Calls.Should().NotContain("stop");
        steps.Calls.Should().NotContain("deploy");
    }

    [Fact]
    public async Task RunAsync_OnADowngrade_ShouldReturnDowngradeRefused()
    {
        var outcome = await Run(new FakeSteps { PreflightOk = false, IsDowngrade = true });

        outcome.Should().BeOfType<DowngradeRefused>();
    }

    [Fact]
    public async Task RunAsync_WhenTheBackupFails_ShouldStopBeforeTheMutationLine()
    {
        var steps = new FakeSteps { BackupOk = false };

        var outcome = await Run(steps);

        outcome.Should().BeOfType<UpgradeFailed>().Which.RolledBack.Should().BeFalse();
        steps.Calls.Should().NotContain("deploy");
    }

    [Fact]
    public async Task RunAsync_WhenDeployFails_ShouldRollBackAndProbeHealth()
    {
        var steps = new FakeSteps { DeployOk = false };

        var outcome = await Run(steps);

        var failure = outcome.Should().BeOfType<UpgradeFailed>().Subject;
        failure.RolledBack.Should().BeTrue();
        steps.Calls.Should().ContainInOrder("restore-tree", "restore-config", "start", "health");
    }

    [Fact]
    public async Task RunAsync_WhenMigrateFails_ShouldRollBack()
    {
        var steps = new FakeSteps { MigrateOk = false };

        var outcome = await Run(steps);

        outcome.Should().BeOfType<UpgradeFailed>().Which.RolledBack.Should().BeTrue();
        steps.Calls.Should().Contain("restore-tree");
    }

    [Fact]
    public async Task RunAsync_WhenHealthFailsAfterStart_ShouldRollBack()
    {
        var steps = new FakeSteps { HealthOk = false };

        var outcome = await Run(steps);

        outcome.Should().BeOfType<UpgradeFailed>().Which.RolledBack.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_WhenTheRollbackItselfIsUnhealthy_ShouldReportItRatherThanClaimRecovery()
    {
        var steps = new FakeSteps { DeployOk = false, PostRollbackHealthOk = false };

        var failure = (UpgradeFailed)await Run(steps);

        failure.RolledBack.Should().BeTrue();
        failure.HealthOk.Should().BeFalse();
        failure.Message.Should().Contain(BackupDir);
    }

    [Fact]
    public async Task RunAsync_WhenTheBackupTreeIsGone_ShouldReportTheRollbackDidNotHappen()
    {
        var steps = new FakeSteps { DeployOk = false, RestoreThrows = true };

        var failure = (UpgradeFailed)await Run(steps);

        failure.RolledBack.Should().BeFalse();
    }

    [Fact]
    public async Task RunAsync_WhenThePosStepFails_ShouldNotRollBackTheServer()
    {
        // Spec section 5 row 8, confirmed by the 2026-07-26 spike: the service survives a
        // full POS package swap, so the two are genuinely independent.
        var steps = new FakeSteps { PosOk = false };

        var failure = (UpgradeFailed)await Run(steps);

        failure.RolledBack.Should().BeFalse();
        failure.ServiceStarted.Should().BeTrue();
        steps.Calls.Should().NotContain("restore-tree");
    }

    [Fact]
    public async Task RunAsync_WhenThePosVersionDidNotChange_ShouldReportPosUpdatedFalse()
    {
        // A same-version repair exits 0 too (spike, section 12), so the exit code cannot
        // be trusted -- only sq.version before vs after can.
        var steps = new FakeSteps { PosVersionAfter = "4.0.0" };

        var success = (UpgradeSucceeded)await Run(steps);

        success.PosUpdated.Should().BeFalse();
    }

    [Fact]
    public async Task RunAsync_WhenThePosVersionChanged_ShouldReportPosUpdatedTrue()
    {
        var success = (UpgradeSucceeded)await Run(new FakeSteps { PosVersionAfter = "4.1.0" });

        success.PosUpdated.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_WithASimulatedDeployFailure_ShouldRollBack()
    {
        // The fault hook is runtime-selected so ONE 190 MB installer build serves VM
        // cases 1 and 2 in a single session.
        var steps = new FakeSteps();

        var outcome = await Run(steps, UpgradeStage.Deploy);

        outcome.Should().BeOfType<UpgradeFailed>().Which.RolledBack.Should().BeTrue();
    }

    [Theory]
    [InlineData("deploy", UpgradeStage.Deploy)]
    [InlineData("MIGRATE", UpgradeStage.Migrate)]
    [InlineData("start", UpgradeStage.Start)]
    [InlineData("nonsense", null)]
    [InlineData(null, null)]
    public void Parse_WithVariousValues_ShouldReturnExpected(string? value, UpgradeStage? expected)
    {
        SimulatedFailure.Parse(value).Should().Be(expected);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests --filter FullyQualifiedName~UpgradeOrchestratorTests`
Expected: FAIL — `UpgradeOrchestrator`, `IUpgradeSteps`, `UpgradeStage`, `SimulatedFailure`, `ConfigSnapshotHandle` do not exist.

- [ ] **Step 3: Write the fault hook**

Create `installer/IndyPOS.Bootstrapper/Upgrade/SimulatedFailure.cs`:

```csharp
namespace IndyPOS.Bootstrapper.Upgrade;

public enum UpgradeStage
{
    Preflight,
    Stop,
    Backup,
    Deploy,
    RestoreConfig,
    Migrate,
    Start,
    PosApp
}

/// <summary>
/// Runtime-selected fault injection for VM case 2 (forced mid-upgrade failure).
/// <para>Runtime, not compile-time, deliberately: at roughly 10 minutes per cycle plus a
/// 190 MB build, one binary serving both VM cases is the difference between one evening
/// and two. Undocumented in help output — it is a test hook, not a feature.</para>
/// </summary>
public static class SimulatedFailure
{
    public const string ArgumentName = "--simulate-failure";

    public static UpgradeStage? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var matched = Enum.GetNames<UpgradeStage>()
            .FirstOrDefault(n => n.Equals(value, StringComparison.OrdinalIgnoreCase));

        return matched is null ? null : Enum.Parse<UpgradeStage>(matched);
    }
}
```

- [ ] **Step 4: Write the step seam**

Create the seam and its Windows implementation at the top of `installer/IndyPOS.Bootstrapper/Upgrade/UpgradeOrchestrator.cs`:

```csharp
using IndyPOS.Bootstrapper.Installers;
using IndyPOS.Bootstrapper.Silent;

namespace IndyPOS.Bootstrapper.Upgrade;

/// <summary>A captured config, restorable once. Wraps ConfigSnapshot for testability.</summary>
public sealed class ConfigSnapshotHandle(Action restore)
{
    public void Restore() => restore();
}

/// <summary>
/// The individual step units the upgrade sequence drives. An interface so the ORDER and
/// the rollback decisions — the parts that actually failed in production — are unit-testable
/// without a service, a database or a 190 MB installer.
/// <para>There is deliberately no DatabaseSetup member: it is fresh-install only.</para>
/// </summary>
public interface IUpgradeSteps
{
    Task<PreflightResult> PreflightAsync(DetectedInstall detected, InstallationConfig config, CancellationToken ct);
    Task<bool> StopServiceAsync(CancellationToken ct);
    Task<BackupResult> BackupAsync(string pgDumpPath, string connectionString, CancellationToken ct);
    ConfigSnapshotHandle CaptureConfig();
    Task<bool> DeployAsync(CancellationToken ct);
    Task<MigrationRunResult> MigrateAsync(CancellationToken ct);
    Task<bool> StartServiceAsync(CancellationToken ct);
    Task<bool> HealthAsync(CancellationToken ct);
    string? ReadPosVersion();
    Task<bool> InstallPosAppAsync(CancellationToken ct);
    void RestoreStoreHubTree(string stampDirectory);
    Task WriteManifestAsync(CancellationToken ct);
}
```

- [ ] **Step 5: Write the orchestrator**

Append to the same file:

```csharp
/// <summary>
/// The in-place upgrade sequence (upgrade design spec, sections 4 and 5).
/// <para>Steps 0-3 mutate nothing: a failure there is a non-event. Steps 4-7 roll back.
/// Step 8 does not — the server is upgraded and serving, and the 2026-07-26 spike confirmed
/// the POS step cannot disturb it.</para>
/// </summary>
public sealed class UpgradeOrchestrator(IUpgradeSteps steps, UpgradeStage? simulateFailure = null)
{
    public async Task<SilentOutcome> RunAsync(
        DetectedInstall detected,
        InstallationConfig config,
        IProgress<InstallationProgress> progress,
        CancellationToken cancellationToken = default)
    {
        // --- Step 1: preflight ------------------------------------------------
        progress.Report(InstallationProgress.Step("Upgrading", "Checking this machine...", 5));

        var preflight = await steps.PreflightAsync(detected, config, cancellationToken);
        Fault(UpgradeStage.Preflight);

        if (!preflight.Ok)
        {
            return preflight.IsDowngrade
                ? new DowngradeRefused(detected.InstalledVersion ?? "unknown", config.InstallVersion)
                : new UpgradeFailed(preflight.FailureReason ?? "Preflight failed.",
                    RolledBack: false, ServiceStarted: true, HealthOk: true, BackupDir: null);
        }

        var posVersionBefore = steps.ReadPosVersion();

        // --- Step 2: stop the service ----------------------------------------
        // Above the mutation line: stopping is reversible, and dumping a live database
        // would silently discard any sale completed before the restart.
        progress.Report(InstallationProgress.Step("Upgrading", "Stopping StoreHub...", 10));
        await steps.StopServiceAsync(cancellationToken);
        Fault(UpgradeStage.Stop);

        // --- Step 3: back up --------------------------------------------------
        progress.Report(InstallationProgress.Step("Upgrading", "Backing up database and binaries...", 20));

        var backup = await steps.BackupAsync(preflight.PgDumpPath!, preflight.ConnectionString!, cancellationToken);
        Fault(UpgradeStage.Backup);

        if (!backup.Success)
        {
            // Nothing has been mutated, but the service is stopped — put it back, and
            // report what actually happened rather than assuming it came up.
            var restarted = await steps.StartServiceAsync(cancellationToken);
            return new UpgradeFailed(backup.ErrorMessage ?? "Backup failed.",
                RolledBack: false, ServiceStarted: restarted,
                HealthOk: restarted && await steps.HealthAsync(cancellationToken), BackupDir: null);
        }

        // ============ EVERYTHING BELOW THIS LINE MUTATES THE INSTALL ============

        var snapshot = steps.CaptureConfig();

        try
        {
            // --- Step 4: deploy -----------------------------------------------
            progress.Report(InstallationProgress.Step("Upgrading", "Deploying StoreHub...", 40));
            Fault(UpgradeStage.Deploy);

            if (!await steps.DeployAsync(cancellationToken))
            {
                return await RollBackAsync("Failed to deploy the StoreHub payload.",
                    backup.StampDirectory!, snapshot, cancellationToken);
            }

            // --- Step 5: restore the store's real config ----------------------
            // The heart of the design. Extraction just wrote the package template over
            // appsettings.json, and DatabaseSetup — which would have rewritten the real
            // values on a fresh install — never runs here.
            progress.Report(InstallationProgress.Step("Upgrading", "Restoring store configuration...", 55));
            snapshot.Restore();
            Fault(UpgradeStage.RestoreConfig);

            // --- Step 6: migrate ----------------------------------------------
            progress.Report(InstallationProgress.Step("Upgrading", "Applying database migrations...", 65));
            Fault(UpgradeStage.Migrate);

            var migration = await steps.MigrateAsync(cancellationToken);
            if (!migration.Success)
            {
                return await RollBackAsync(migration.ErrorMessage ?? "Migration failed.",
                    backup.StampDirectory!, snapshot, cancellationToken);
            }

            // --- Step 7: start + verify ---------------------------------------
            progress.Report(InstallationProgress.Step("Upgrading", "Starting StoreHub...", 80));
            Fault(UpgradeStage.Start);

            var started = await steps.StartServiceAsync(cancellationToken);
            var healthy = started && await steps.HealthAsync(cancellationToken);

            if (!healthy)
            {
                return await RollBackAsync("StoreHub did not become healthy after the upgrade.",
                    backup.StampDirectory!, snapshot, cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return await RollBackAsync(ex.Message, backup.StampDirectory!, snapshot, cancellationToken);
        }

        // ==================== SERVER UPGRADE COMPLETE ====================

        // --- Step 8: the POS app. No rollback past this point. ----------------
        progress.Report(InstallationProgress.Step("Upgrading", "Updating the POS application...", 90));
        Fault(UpgradeStage.PosApp);

        var posOk = await steps.InstallPosAppAsync(cancellationToken);
        if (!posOk)
        {
            return new UpgradeFailed(
                "The StoreHub service upgraded and is serving, but the POS application " +
                "update failed. Re-run the installer to retry it; the store can keep trading.",
                RolledBack: false, ServiceStarted: true, HealthOk: true, backup.StampDirectory);
        }

        // POS_UPDATED cannot come from the exit code: the spike measured 0 for both a real
        // upgrade and a same-version repair. sq.version is the only truthful record.
        var posUpdated = steps.ReadPosVersion() is { } after &&
                         !string.Equals(after, posVersionBefore, StringComparison.Ordinal);

        // --- Steps 9-10: manifest + markers. Non-fatal; the next run repairs a stale one.
        try
        {
            await steps.WriteManifestAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            progress.Report(InstallationProgress.Error($"Warning: could not write install manifest: {ex.Message}"));
        }

        progress.Report(InstallationProgress.Step("Upgrade Complete", "IndyPOS has been upgraded.", 100));

        return new UpgradeSucceeded(
            detected.InstalledVersion, config.InstallVersion,
            ServiceStarted: true, HealthOk: true,
            backup.StampDirectory!, backup.Locked, posUpdated);
    }

    /// <summary>
    /// Delete-then-copy the binaries back, put the config back, restart, and PROVE it came
    /// back. Without the probe a rollback can report "the store resumed on its previous
    /// version" while the store is dead.
    /// </summary>
    private async Task<UpgradeFailed> RollBackAsync(
        string reason, string stampDirectory, ConfigSnapshotHandle snapshot, CancellationToken cancellationToken)
    {
        try
        {
            steps.RestoreStoreHubTree(stampDirectory);
        }
        catch (Exception ex)
        {
            return new UpgradeFailed(
                $"{reason} The rollback then failed as well ({ex.Message}). This store needs " +
                $"manual recovery; the database dump is at {stampDirectory}.",
                RolledBack: false, ServiceStarted: false, HealthOk: false, stampDirectory);
        }

        snapshot.Restore();

        var started = await steps.StartServiceAsync(cancellationToken);
        var healthy = started && await steps.HealthAsync(cancellationToken);

        var message = healthy
            ? $"{reason} The upgrade was rolled back and the store is serving its previous version."
            : $"{reason} The upgrade was rolled back but StoreHub is not healthy. Restore the " +
              $"database dump at {stampDirectory} — see docs\\operations\\upgrade-procedure.md.";

        return new UpgradeFailed(message, RolledBack: true, started, healthy, stampDirectory);
    }

    private void Fault(UpgradeStage stage)
    {
        if (simulateFailure == stage)
        {
            throw new InvalidOperationException($"Simulated failure at stage '{stage}' (test hook).");
        }
    }
}
```

Note the `Fault(UpgradeStage.Deploy)` call sits *before* `DeployAsync`, so the simulated failure is thrown inside the `try` and takes the rollback path — which is exactly what VM case 2 must exercise.

- [ ] **Step 6: Write the Windows implementation of the seam**

Create `installer/IndyPOS.Bootstrapper/Upgrade/WindowsUpgradeSteps.cs`:

```csharp
using IndyPOS.Bootstrapper.Installers;
using IndyPOS.Vault;

namespace IndyPOS.Bootstrapper.Upgrade;

/// <summary>Binds <see cref="IUpgradeSteps"/> to the real machine.</summary>
public sealed class WindowsUpgradeSteps(InstallationConfig config, IProgress<string> log) : IUpgradeSteps
{
    private const int PostgresMajor = 18;

    private readonly ServiceControl _service = new(config.ServiceName);
    private readonly UpgradeBackup _backup = new(
        config.BackupsDirectory,
        config.StoreHubInstallPath,
        new ExternalProcessRunner(),
        () => DateTime.Now.ToString("yyyyMMdd-HHmmss"));

    private string ConfigPath => Path.Combine(config.StoreHubInstallPath, "appsettings.json");

    public async Task<PreflightResult> PreflightAsync(
        DetectedInstall detected, InstallationConfig cfg, CancellationToken ct)
    {
        var (ok, reason, isDowngrade) = UpgradePreflight.CheckVersions(
            detected.InstalledVersion, cfg.InstallVersion);
        if (!ok)
        {
            return new PreflightResult(false, null, null, reason, isDowngrade);
        }

        if (UpgradePreflight.IsPosAppRunning())
        {
            return new PreflightResult(false, null, null,
                "The IndyPOS POS application is running. Close it on this machine and re-run.", false);
        }

        if (!UpgradePreflight.HasFreeSpace(cfg.BackupsDirectory))
        {
            return new PreflightResult(false, null, null,
                "Not enough free disk space to back up the database and binaries.", false);
        }

        var pgDump = UpgradePreflight.LocatePgDump(cfg.PostgresBinPath, PostgresMajor);
        if (pgDump is null)
        {
            return new PreflightResult(false, null, null,
                $"pg_dump.exe for PostgreSQL {PostgresMajor} could not be located.", false);
        }

        var connectionString = ReadConnectionString();
        if (connectionString is null)
        {
            return new PreflightResult(false, null, null,
                "The StoreHub connection string could not be decrypted on this machine.", false);
        }

        // Idempotent check-then-install. Omitting these delivers a UI-polish release that
        // renders in a fallback face, or binaries the machine cannot run.
        log.Report("Ensuring prerequisites (fonts, .NET 10, VC++ redistributable)...");
        new FontInstaller().Install(log);
        await new DotNetInstaller().EnsureInstalledAsync(log, ct, cfg.Interactive);
        await new VCRedistInstaller().EnsureInstalledAsync(null, ct);

        return new PreflightResult(true, pgDump, connectionString, null, false);
    }

    private string? ReadConnectionString()
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(ConfigPath));
            var raw = doc.RootElement.GetProperty("connectionStrings").GetProperty("storehub-db").GetString();
            return raw is null ? null : SecretProtector.Unprotect(StoreHubConfigReader.ConnectionStringKey, raw);
        }
        catch
        {
            return null;
        }
    }

    public Task<bool> StopServiceAsync(CancellationToken ct) =>
        _service.StopAsync(TimeSpan.FromSeconds(60), ct);

    public Task<BackupResult> BackupAsync(string pgDumpPath, string connectionString, CancellationToken ct) =>
        _backup.CreateAsync(pgDumpPath, connectionString, ct);

    public ConfigSnapshotHandle CaptureConfig()
    {
        var snapshot = ConfigSnapshot.Capture(ConfigPath);
        return new ConfigSnapshotHandle(snapshot.Restore);
    }

    public Task<bool> DeployAsync(CancellationToken ct) =>
        StoreHubPayload.ExtractAsync(config.StoreHubInstallPath, log, ct);

    public Task<MigrationRunResult> MigrateAsync(CancellationToken ct) =>
        MigrationRunner.RunAsync(config.StoreHubInstallPath, log, ct);

    public Task<bool> StartServiceAsync(CancellationToken ct) =>
        _service.StartAsync(TimeSpan.FromSeconds(60), ct);

    public Task<bool> HealthAsync(CancellationToken ct) =>
        HealthProbe.IsReadyAsync(config.HealthCheckPort, cancellationToken: ct);

    public string? ReadPosVersion() => PosAppVersion.Read(config.VelopackInstallPath);

    public async Task<bool> InstallPosAppAsync(CancellationToken ct) =>
        (await new VelopackLauncher().InstallAsync(config, null, ct)).Success;

    public void RestoreStoreHubTree(string stampDirectory) => _backup.RestoreStoreHubTree(stampDirectory);

    public Task WriteManifestAsync(CancellationToken ct) => InstallManifestWriter.WriteAsync(config, ct);
}
```

`ConfigSnapshot` is `internal sealed`; `WindowsUpgradeSteps` is in the same assembly, so this compiles. If `FontInstaller.Install` / `DotNetInstaller.EnsureInstalledAsync` / `VCRedistInstaller.EnsureInstalledAsync` have different signatures than shown, match the real ones as used in `FreshInstallOrchestrator` steps 0–1b.

- [ ] **Step 7: Run the tests to verify they pass**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests --filter FullyQualifiedName~UpgradeOrchestratorTests`
Expected: PASS, 21 tests (16 facts + 5 theory cases).

- [ ] **Step 8: Commit**

```bash
git add installer/IndyPOS.Bootstrapper/Upgrade/SimulatedFailure.cs installer/IndyPOS.Bootstrapper/Upgrade/UpgradeOrchestrator.cs installer/IndyPOS.Bootstrapper/Upgrade/WindowsUpgradeSteps.cs tests/IndyPOS.Bootstrapper.Tests/Upgrade/UpgradeOrchestratorTests.cs
git commit -m "feat(installer): add the in-place upgrade sequence with verified rollback"
```

---

## Task 12: Entry points — one router, and a wizard that refuses

Spec §9. Both entry points consume **one** detection result from a single router. If `Program.cs` and `SilentInstaller` each called the detector, the two forks would diverge.

The wizard must not be left undetected: a double-clicked `Setup.exe` on a live store otherwise reproduces both original failures, and that is the likely path on a hands-on store visit. But it does not gain an upgrade UI — it collects StoreId and StoreType up front (both already fixed on an upgrade) and its finish screen is built around bootstrap credentials that do not exist on an upgrade.

**Files:**
- Modify: `installer/IndyPOS.Bootstrapper/Silent/SilentArgs.cs`, `installer/IndyPOS.Bootstrapper/Silent/SilentInstaller.cs`, `installer/IndyPOS.Bootstrapper/Program.cs`, `installer/IndyPOS.Bootstrapper/UI/InstallationWizard.cs`
- Test: `tests/IndyPOS.Bootstrapper.Tests/Silent/SilentArgsTests.cs`

**Interfaces:**
- Consumes: `InstallModeDetector.Detect`, `WindowsInstallProbe`, `UpgradeOrchestrator`, `WindowsUpgradeSteps`, `SilentOutcomeMapper.ModeMarker`, `SimulatedFailure.Parse`
- Produces:
  - `SilentInstallOptions` gains `string? StoreId` (was `string`), `UpgradeStage? SimulateFailure`
  - `static string InstallationWizard.BuildExistingInstallMessage(DetectedInstall detected)`

- [ ] **Step 1: Write the failing tests**

Append to `tests/IndyPOS.Bootstrapper.Tests/Silent/SilentArgsTests.cs` (add `using IndyPOS.Bootstrapper.Upgrade;`):

```csharp
    [Fact]
    public void Parse_WithSilentAndNoStoreId_ShouldSucceedWithANullStoreId()
    {
        // On an upgrade the store id is adopted from the detected config. Requiring it
        // would force a comparison on every upgrade with no correct behaviour when the
        // detected value is null.
        var result = SilentArgs.Parse(["--silent"]);

        result.Status.Should().Be(ParseStatus.Silent);
        result.Options!.StoreId.Should().BeNull();
    }

    [Fact]
    public void Parse_WithSilentAndAStoreId_ShouldStillCarryIt()
    {
        var result = SilentArgs.Parse(["--silent", "--store-id", "Rungrat-001"]);

        result.Options!.StoreId.Should().Be("Rungrat-001");
    }

    [Fact]
    public void Parse_WithAnEmptyStoreIdValue_ShouldStillBeAUsageError()
    {
        SilentArgs.Parse(["--silent", "--store-id", "   "]).Status.Should().Be(ParseStatus.UsageError);
    }

    [Fact]
    public void Parse_WithSimulateFailure_ShouldCarryTheStage()
    {
        var result = SilentArgs.Parse(["--silent", "--simulate-failure=deploy"]);

        result.Options!.SimulateFailure.Should().Be(UpgradeStage.Deploy);
    }

    [Fact]
    public void Parse_WithAnUnknownSimulateFailureStage_ShouldBeAUsageError()
    {
        SilentArgs.Parse(["--silent", "--simulate-failure=nonsense"]).Status
            .Should().Be(ParseStatus.UsageError);
    }
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests --filter FullyQualifiedName~SilentArgsTests`
Expected: FAIL — `--silent` without `--store-id` currently returns a usage error, and `SimulateFailure` does not exist.

- [ ] **Step 3: Update SilentArgs**

In `installer/IndyPOS.Bootstrapper/Silent/SilentArgs.cs`:

Change the options record and add the new flag:

```csharp
using IndyPOS.Bootstrapper.Upgrade;

/// <param name="StoreId">
/// Null is legal: an upgrade adopts the detected store id. The router rejects a supplied
/// value that conflicts with the detected one — silently rewriting store identity would
/// orphan the store's sales history.
/// </param>
public sealed record SilentInstallOptions(
    string? StoreId,
    int TimeoutMinutes,
    StoreType StoreType = StoreType.GeneralHardware,
    UpgradeStage? SimulateFailure = null);
```

Inside `Parse`, add a local `UpgradeStage? simulateFailure = null;` and a new case:

```csharp
                case "--simulate-failure":
                    if (!TryReadValue(args, ref i, inlineValue, out var stageRaw)
                        || (simulateFailure = SimulatedFailure.Parse(stageRaw)) is null)
                        return ParseResult.Usage(
                            "--simulate-failure must name a stage (test hook).");
                    break;
```

Replace the trailing validation so an absent `--store-id` is allowed but a blank *value* is not. Track whether the flag appeared:

```csharp
        // A supplied-but-blank value is still a usage error; an absent flag is not.
        if (storeId is not null && string.IsNullOrWhiteSpace(storeId))
            return ParseResult.Usage("--store-id requires a non-empty value.");

        return ParseResult.Silent(
            new SilentInstallOptions(storeId?.Trim(), timeoutMinutes, storeType, simulateFailure));
```

Delete the old `storeId = storeId?.Trim();` / `IsNullOrWhiteSpace` rejection block at lines 69-71.

- [ ] **Step 4: Turn SilentInstaller into the router**

In `installer/IndyPOS.Bootstrapper/Silent/SilentInstaller.cs`, add `using IndyPOS.Bootstrapper.Upgrade;` and replace `Run`'s body from the config construction onwards:

```csharp
        var options = parse.Options!;

        // Detected first, with a placeholder store id: only computed paths are needed to
        // probe the machine, and StoreId is required on the record.
        var probeConfig = new InstallationConfig
        {
            StoreId = options.StoreId ?? "pending",
            StoreType = options.StoreType,
            Interactive = false
        };

        var detected = InstallModeDetector.Detect(
            new WindowsInstallProbe(probeConfig), probeConfig.StoreHubInstallPath);

        string logPath;
        TextWriter writer;
        try
        {
            (logPath, writer) = OpenLog(probeConfig);
        }
        catch (Exception ex)
        {
            return Emit(new InstallFailed(SecretScrubber.Scrub(ex.Message)), logger: null);
        }

        var logger = new SilentInstallLogger(writer);
        logger.WriteMarker(SilentOutcomeMapper.ModeMarker(detected.Mode));

        var outcome = detected.Mode switch
        {
            InstallMode.Unusable => new UnusableInstall(detected.Reason),
            InstallMode.Upgrade => RunUpgrade(detected, options, logger),
            _ => RunFreshInstall(BuildFreshConfig(options), options, logger)
        };

        var exit = Emit(outcome, logger);

        logger.Dispose();
        CopyLatest(probeConfig, logPath);
        return exit;
```

Rename the existing `RunInstall` to `RunFreshInstall`, changing only its first parameter from `InstallationConfig config` built inline to one passed in; its body is otherwise unchanged. Then add these two helpers:

```csharp
    // A fresh install still requires an explicit store id: there is nothing to adopt.
    private static InstallationConfig? BuildFreshConfig(SilentInstallOptions options) =>
        options.StoreId is null
            ? null
            : new InstallationConfig
            {
                StoreId = options.StoreId,
                StoreType = options.StoreType,
                Interactive = false
            };

    private static SilentOutcome RunUpgrade(
        DetectedInstall detected, SilentInstallOptions options, SilentInstallLogger logger)
    {
        // Rewriting store identity would orphan every sale recorded under the old id.
        if (options.StoreId is not null &&
            !string.Equals(options.StoreId, detected.StoreId, StringComparison.Ordinal))
        {
            return new UnusableInstall(
                $"--store-id '{options.StoreId}' does not match the installed store " +
                $"'{detected.StoreId}'. Re-run without --store-id to upgrade this store.");
        }

        var config = new InstallationConfig
        {
            StoreId = detected.StoreId!,
            StoreType = detected.StoreType!.Value,
            Interactive = false
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(options.TimeoutMinutes));

        try
        {
            var steps = new WindowsUpgradeSteps(config, logger);
            return new UpgradeOrchestrator(steps, options.SimulateFailure)
                .RunAsync(detected, config, logger, cts.Token)
                .GetAwaiter().GetResult();
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            logger.WriteLine("ERROR: upgrade timed out.");
            return new InstallTimedOut();
        }
        catch (Exception ex)
        {
            var message = SecretScrubber.Scrub(ex.Message);
            logger.WriteLine("ERROR: " + message);
            return new UpgradeFailed(message, false, false, false, null);
        }
    }
```

Change the `_ =>` arm to handle a missing store id on the fresh path:

```csharp
            _ => BuildFreshConfig(options) is { } fresh
                ? RunFreshInstall(fresh, options, logger)
                : new UsageErrorOutcome("--silent requires --store-id <ID> for a fresh install.")
```

`SilentInstallLogger` must satisfy both `IProgress<InstallationProgress>` (for the orchestrators) and `IProgress<string>` (for `WindowsUpgradeSteps`). Check its current declaration; if it only implements the former, add `IProgress<string>` with `void Report(string value) => WriteLine(value);`.

- [ ] **Step 5: Make the wizard detect and refuse**

In `installer/IndyPOS.Bootstrapper/UI/InstallationWizard.cs`, add `using IndyPOS.Bootstrapper.Upgrade;` and a static message builder:

```csharp
    /// <summary>
    /// The wizard deliberately gains no upgrade UI: it collects StoreId and StoreType up
    /// front (both already fixed on an upgrade) and its finish screen is built around
    /// bootstrap credentials that do not exist on one. But it must not silently proceed —
    /// a double-clicked Setup.exe on a live store reproduces both original failures.
    /// </summary>
    internal static string BuildExistingInstallMessage(DetectedInstall detected) =>
        $"IndyPOS {detected.InstalledVersion ?? "(unknown version)"} is already installed on " +
        $"this machine (store {detected.StoreId ?? "unknown"}).\n\n" +
        "The wizard only performs new installations. To upgrade in place, open an " +
        "administrator command prompt and run:\n\n" +
        "    IndyPOS-Setup.exe --silent\n\n" +
        "See docs\\operations\\upgrade-procedure.md.";
```

In `installer/IndyPOS.Bootstrapper/Program.cs`, run the same detector before showing the wizard:

```csharp
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

        // One detection result, one router. If the wizard and the silent path each called
        // the detector, the two forks would drift.
        var probeConfig = new InstallationConfig { StoreId = "pending", Interactive = true };
        var detected = InstallModeDetector.Detect(
            new WindowsInstallProbe(probeConfig), probeConfig.StoreHubInstallPath);

        if (detected.Mode != InstallMode.Fresh)
        {
            MessageBox.Show(
                detected.Mode == InstallMode.Upgrade
                    ? InstallationWizard.BuildExistingInstallMessage(detected)
                    : detected.Reason,
                "Existing Installation Detected",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return 0;
        }

        Application.Run(new InstallationWizard());
        return 0;
```

Add `using IndyPOS.Bootstrapper.Installers;` and `using IndyPOS.Bootstrapper.Upgrade;` to `Program.cs`.

- [ ] **Step 6: Run the full bootstrapper suite and build**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests`
Expected: PASS — every prior task's tests plus the 5 new `SilentArgsTests`.

Run: `dotnet build IndyPOS.sln -c Release`
Expected: 0 errors.

- [ ] **Step 7: Commit**

```bash
git add installer/IndyPOS.Bootstrapper/Silent/ installer/IndyPOS.Bootstrapper/Program.cs installer/IndyPOS.Bootstrapper/UI/InstallationWizard.cs tests/IndyPOS.Bootstrapper.Tests/Silent/SilentArgsTests.cs
git commit -m "feat(installer): route fresh, upgrade and unusable from one detection result"
```

---

## Task 13: VM harness — snapshot choice, expect-rollback, database assertions

Spec §10. The harness cannot run VM case 2 at all today: it hardcodes the snapshot name and **throws** whenever `RESULT != success` (`Reset-AndInstall.ps1:153`, `:269-272`), so a deliberate-rollback run fails by construction.

Case 2 must also assert against the **fresh timestamped log**, not `install-latest.log`, which can still hold a previous run's `RESULT=success`.

**Files:**
- Modify: `scripts/vm-testing/Reset-AndInstall.ps1`, `scripts/vm-testing/Test-IndyPOSInstallation.ps1`, `scripts/vm-testing/VMTestConfig.psd1`

**Interfaces:**
- Consumes: the markers from Task 10
- Produces: `Reset-AndInstall.ps1` parameters `-SnapshotName <string>`, `-ExpectRollback`, `-InstallerArgs <string[]>`; `Test-IndyPOSInstallation.ps1` parameter `-AssertDatabase`

**Everything written to the guest must be ASCII** — PowerShell 5.1 with a Windows-1252 codepage.

- [ ] **Step 1: Add the parameters**

In `scripts/vm-testing/Reset-AndInstall.ps1`, extend the `param()` block:

```powershell
    [string]$SnapshotName,
    [switch]$ExpectRollback,
    [string[]]$InstallerArgs,
```

Below `$Config = Import-PowerShellDataFile ...`, resolve the snapshot:

```powershell
# Case 1/2 run from a real store image; case 3 from the clean baseline.
if (-not $SnapshotName) { $SnapshotName = $Config.CleanSnapshotName }
```

Replace every `$Config.CleanSnapshotName` in `Test-Prerequisites` and `Restore-CleanVM` with `$SnapshotName`.

- [ ] **Step 2: Make the installer arguments configurable**

In `Invoke-SilentInstall`, replace the hardcoded argument list:

```powershell
    $arguments = if ($InstallerArgs) { $InstallerArgs } else { @('--silent', '--store-id', $Config.TestStoreId) }
    Write-Info "Running (in guest): $guestInstaller $($arguments -join ' ')"

    $job = Invoke-Command -VMName $Config.VMName -Credential $Credential -AsJob -ScriptBlock {
        param($exe, $argList)
        $p = Start-Process -FilePath $exe -ArgumentList $argList -Wait -PassThru
        [pscustomobject]@{ ExitCode = $p.ExitCode }
    } -ArgumentList $guestInstaller, $arguments
```

- [ ] **Step 3: Read the fresh log, not install-latest.log**

Still in `Invoke-SilentInstall`, replace the marker read. `install-latest.log` can hold a previous run's `RESULT=success`, which would make a deliberate-rollback run look like a pass:

```powershell
    # Newest install-<stamp>-<pid>.log, NOT install-latest.log: on a re-run the latter can
    # still carry the previous run's RESULT=success.
    $markers = Invoke-Command -VMName $Config.VMName -Credential $Credential -ScriptBlock {
        param($dir)
        $log = Get-ChildItem -Path $dir -Filter 'install-2*.log' -ErrorAction SilentlyContinue |
               Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($log) { Get-Content $log.FullName | Where-Object { $_ -like 'INDYPOS_MARKER *' } } else { @() }
    } -ArgumentList $logDir
```

- [ ] **Step 4: Add the expect-rollback gate**

Replace the gating block at the end of `Invoke-SilentInstall`:

```powershell
    if ($ExpectRollback) {
        # A deliberate mid-upgrade failure. Success here means the store came BACK, which
        # is the only claim the rollback design actually makes.
        $ok = ($marker['RESULT'] -eq 'failed') -and
              ($marker['ROLLED_BACK'] -eq 'true') -and
              ($marker['SERVICE_STARTED'] -eq 'true') -and
              ($marker['HEALTH'] -eq 'ok')
        if (-not $ok) {
            throw ("Expected a verified rollback but got RESULT=$($marker['RESULT']), " +
                   "ROLLED_BACK=$($marker['ROLLED_BACK']), SERVICE_STARTED=$($marker['SERVICE_STARTED']), " +
                   "HEALTH=$($marker['HEALTH']). See $latestLog in the guest.")
        }
        Write-OK 'Rollback verified: store is serving its previous version.'
        return
    }

    # Authoritative gating rule: exit code + RESULT + service.
    $failed = ($run.ExitCode -ne 0) -or ($marker['RESULT'] -ne 'success') -or ($marker['SERVICE_STARTED'] -eq 'false')
    if ($failed) {
        throw "Silent install failed (exit=$($run.ExitCode), RESULT=$($marker['RESULT']), SERVICE_STARTED=$($marker['SERVICE_STARTED'])). See $latestLog in the guest."
    }
    if ($marker['CRED_LOCKED'] -eq 'false') {
        Write-Warn2 "Credential file could not be ACL-locked (CRED_LOCKED=false)."
    }
    Write-OK "Silent install completed (RESULT=$($marker['RESULT']), MODE=$($marker['MODE']))."
```

Also skip the verifier on an expect-rollback run — it audits a *completed* install. In the `try` block of Main:

```powershell
    Invoke-SilentInstall -Credential $cred
    if ($ExpectRollback) {
        Write-OK 'Expect-rollback run complete; skipping the install verifier.'
        exit 0
    }
    $result = Invoke-Verifier -Credential $cred
```

- [ ] **Step 5: Script the database assertions**

Add to `scripts/vm-testing/Test-IndyPOSInstallation.ps1` a `-AssertDatabase` switch and this check block. Done interactively on 2026-07-25; this is the scripting of it:

```powershell
# Decrypt the DPAPI-sealed connection string in-guest, then drive psql. Machine scope,
# entropy = SHA256("IndyPOS:ConnectionStrings:storehub-db"). PowerShell 5.1 has no static
# SHA256.HashData, so use an instance.
function Get-StoreHubConnectionString {
    param([string]$AppSettingsPath)

    $raw = (Get-Content $AppSettingsPath -Raw | ConvertFrom-Json).connectionStrings.'storehub-db'
    if (-not $raw.StartsWith('DPAPI:')) { return $raw }

    Add-Type -AssemblyName System.Security
    $sha = [System.Security.Cryptography.SHA256]::Create()
    $entropy = $sha.ComputeHash([Text.Encoding]::UTF8.GetBytes('IndyPOS:ConnectionStrings:storehub-db'))
    $cipher = [Convert]::FromBase64String($raw.Substring(6))
    $plain = [System.Security.Cryptography.ProtectedData]::Unprotect(
        $cipher, $entropy, [System.Security.Cryptography.DataProtectionScope]::LocalMachine)

    return [Text.Encoding]::UTF8.GetString($plain)
}

function Invoke-StoreHubQuery {
    param([string]$ConnectionString, [string]$Sql, [string]$PsqlPath)

    $parts = @{}
    foreach ($kv in $ConnectionString.Split(';')) {
        if ($kv -match '^\s*([^=]+)=(.*)$') { $parts[$Matches[1].Trim()] = $Matches[2].Trim() }
    }

    $env:PGPASSWORD = $parts['Password']
    try {
        # SQL via stdin: native-argument quoting strips the double quotes psql needs.
        return $Sql | & $PsqlPath -h $parts['Host'] -p $parts['Port'] -U $parts['Username'] `
                                  -d $parts['Database'] -w -t -A -F '|'
    }
    finally { Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue }
}
```

Add the checks themselves, guarded by `-AssertDatabase`, asserting the spec §10 case-1 expectations against snapshot `Pre-Upgrade-2026-07-25`:

```powershell
if ($AssertDatabase) {
    $conn = Get-StoreHubConnectionString -AppSettingsPath 'C:\ProgramData\IndyPOS\v4\StoreHub\appsettings.json'
    $psql = 'C:\Program Files\PostgreSQL\18\bin\psql.exe'

    $rows = Invoke-StoreHubQuery -ConnectionString $conn -PsqlPath $psql `
        -Sql 'SELECT code, kind, is_enabled FROM payment_method ORDER BY code;'

    # The snapshot is a genuine PRE-reclassification store: PayLater kind=1, WelfareCard kind=1.
    Add-Check -Category 'Database' -Name 'PayLater reclassified to Special (kind=3)' `
              -Pass ([bool]($rows | Where-Object { $_ -like 'PayLater|3|*' }))

    Add-Check -Category 'Database' -Name 'WelfareCard is GovernmentCampaign (kind=2) and still enabled' `
              -Pass ([bool]($rows | Where-Object { $_ -eq 'WelfareCard|2|t' }))

    Add-Check -Category 'Database' -Name 'Cash and MoneyTransfer are Standard (kind=1)' `
              -Pass (@($rows | Where-Object { $_ -like 'Cash|1|*' -or $_ -like 'MoneyTransfer|1|*' }).Count -eq 2)

    $storeIds = Invoke-StoreHubQuery -ConnectionString $conn -PsqlPath $psql `
        -Sql 'SELECT DISTINCT store_id FROM payment_method;'

    # A second catalogue under STORE-<MachineName> is the exact orphaning that detection
    # rule 2's non-empty Store:Id check exists to prevent.
    Add-Check -Category 'Database' -Name 'Exactly one store id in payment_method' `
              -Pass (@($storeIds | Where-Object { $_ }).Count -eq 1) `
              -Detail ($storeIds -join ', ')
}
```

Match `Add-Check`'s real parameter names to the ones already used in the file.

- [ ] **Step 6: Verify the scripts parse and are ASCII**

```bash
pwsh -NoProfile -Command "\$null = [System.Management.Automation.Language.Parser]::ParseFile('scripts/vm-testing/Reset-AndInstall.ps1', [ref]\$null, [ref]\$e); if (\$e) { \$e; exit 1 }"
pwsh -NoProfile -Command "\$null = [System.Management.Automation.Language.Parser]::ParseFile('scripts/vm-testing/Test-IndyPOSInstallation.ps1', [ref]\$null, [ref]\$e); if (\$e) { \$e; exit 1 }"
```

Expected: no parse errors.

```bash
pwsh -NoProfile -Command "Get-ChildItem scripts/vm-testing/*.ps1 | ForEach-Object { \$b = [IO.File]::ReadAllBytes(\$_.FullName); if (\$b | Where-Object { \$_ -gt 127 }) { \$_.Name } }"
```

Expected: no output. Any listed file contains non-ASCII bytes and will not parse under the guest's Windows-1252 codepage — replace the offending characters.

- [ ] **Step 7: Commit**

```bash
git add scripts/vm-testing/
git commit -m "test(vm): support upgrade snapshots, expected rollbacks and database assertions"
```

---

## Task 14: Operator documentation

Spec §3 (`Unusable` recovery must be documented per rule), §4 (the `pg_restore` command), §5 (forward-only migrations as a release gate) and §6.

**Files:**
- Create: `docs/operations/upgrade-procedure.md`
- Modify: `docs/operations/update-procedure.md` (cross-link), `docs/operations/RUNBOOK.md` (index entry), `CLAUDE.md` (the forward-only migration rule)

- [ ] **Step 1: Write the upgrade procedure**

Create `docs/operations/upgrade-procedure.md` covering, in this order:

1. **When to use this** — an in-place upgrade of a store already running IndyPOS v4. Fresh installs use `docs/operations/store-installation-guide.md`.
2. **The command** — `IndyPOS-Setup.exe --silent` from an elevated prompt. `--store-id` is *not* needed and, if supplied, must match the installed store exactly. `--store-type` is needed only when the store predates 2026-07-18 and has no `Store:Type` key.
3. **Exit codes** — `0` success · `1` usage · `2` failed (read `ROLLED_BACK`) · `3` not elevated · `4` timeout · `5` unusable (read `REASON`) · `6` downgrade refused.
4. **Reading the result** — the markers in `C:\ProgramData\IndyPOS\v4\logs\install-latest.log`, with a table: `MODE`, `RESULT`, `FROM_VERSION`, `TO_VERSION`, `SERVICE_STARTED`, `HEALTH`, `BACKUP_DIR`, `BACKUP_LOCKED`, `POS_UPDATED`, `ROLLED_BACK`. Note explicitly that `POS_UPDATED=false` with `RESULT=success` means the POS app was already current — not a failure.
5. **`Unusable` recovery, one section per detection rule:**
   - *Rule 1 — a database from another major.* **Do not remove PostgreSQL; it holds the sales history.** Run the installer for the major that owns the existing install root (`C:\ProgramData\IndyPOS\v<N>`).
   - *Rule 2, connection string.* Restore `appsettings.json` from the newest `backups\<stamp>\StoreHub\`. A `DPAPI:` value sealed on another machine can never be decrypted here.
   - *Rule 2, `Store:Id`.* Set it to the store's real id — the one in `store_user.store_id` — before upgrading.
   - *Rule 2, `Store:Type`.* Re-run with `--store-type GeneralHardware|Minimart`.
   - *Rule 2, service ImagePath.* Re-register the service against `C:\ProgramData\IndyPOS\v4\StoreHub\IndyPOS.StoreHub.exe`.
   - *Rule 4.* A config with no manifest — inspect the install root before proceeding.

   **None of these sections may mention `cleanup-v4.ps1`.** That script drops `indypos_storehub` unconditionally unless `-SkipDatabase`.
6. **Restoring the database dump** — a human decision, never automatic:

```powershell
# Stop the service first, then restore into the existing database.
Stop-Service IndyPOS.StoreHub.v4
$env:PGPASSWORD = '<indypos_app password from the connection string>'
& 'C:\Program Files\PostgreSQL\18\bin\pg_restore.exe' `
    --host=127.0.0.1 --port=5432 --username=indypos_app `
    --dbname=indypos_storehub --clean --if-exists --no-owner --no-password `
    'C:\ProgramData\IndyPOS\v4\backups\<stamp>\storehub.dump'
Start-Service IndyPOS.StoreHub.v4
```

7. **Backups** — the last 2 stamps are kept under `C:\ProgramData\IndyPOS\v4\backups\`; each is roughly 130 MB. They are ACL-locked to Administrators + LocalSystem because the dump contains the full sales history and BCrypt admin hashes. `BACKUP_LOCKED=false` in the log means that failed — fix the ACL before leaving the store.
8. **Known limitations** — copy spec §11 items 1, 2, 5 and 8 in operator language: Velopack is per-user so a cashier's profile may lag; the POS app self-updates independently; one machine per run; a store missing `StoreConfiguration.json` will not have one created by an upgrade.

- [ ] **Step 2: Cross-link the existing docs**

Add to `docs/operations/update-procedure.md`, near the top: a line pointing at `upgrade-procedure.md` as the authoritative in-place upgrade path. Add `upgrade-procedure.md` to the document index in `docs/operations/RUNBOOK.md`.

- [ ] **Step 3: Record the forward-only migration release gate**

Add to `CLAUDE.md` under **Coding Standards**, a new subsection. This is a standing rule with teeth, not documentation of this epic:

```markdown
### Database Migrations — Forward-Only (release gate)

An upgrade rolls back binaries and config, **not schema**. Every migration in a release
must therefore be runnable against the *previous* release's binaries:

- Additive only. New columns nullable or with a default.
- No renames, no drops, no type narrowing.
- No new `NOT NULL` column without a default — the restored binaries' INSERT would fail
  and the till could not complete a sale.

A migration that breaks this makes the installer's rollback claim false.
```

- [ ] **Step 4: Verify the docs are ASCII-safe and links resolve**

```bash
pwsh -NoProfile -Command "\$b = [IO.File]::ReadAllBytes('docs/operations/upgrade-procedure.md'); if (\$b | Where-Object { \$_ -gt 127 }) { 'non-ascii present' } else { 'ascii clean' }"
```

Non-ASCII is acceptable in a Markdown doc (unlike guest scripts), but the embedded PowerShell block must be ASCII so it can be pasted into the guest.

Confirm every relative link resolves: `docs/operations/store-installation-guide.md`, `docs/operations/update-procedure.md`.

- [ ] **Step 5: Commit**

```bash
git add docs/operations/upgrade-procedure.md docs/operations/update-procedure.md docs/operations/RUNBOOK.md CLAUDE.md
git commit -m "docs(operations): document the upgrade procedure and unusable-install recovery"
```

---

## Final verification (after Task 14)

Not a task — the gate before the branch is offered for review.

- [ ] **Full build and test sweep**

```bash
dotnet build IndyPOS.sln -c Release
dotnet test tests/IndyPOS.Bootstrapper.Tests
dotnet test tests/IndyPOS.Domain.Tests
dotnet test tests/IndyPOS.Application.Tests
```

Expected: Release build 0 errors. Bootstrapper ~160 pass / 8 skip. Domain 8/8. Application 274/274. StoreHub integration (76/76) needs Docker and is unaffected by this branch — run it if Docker is up.

- [ ] **Confirm the refactor gate one last time**

```bash
git diff development -- installer/IndyPOS.Bootstrapper/Installers/FreshInstallOrchestrator.cs
```

The only changes may be the rename, the doc comment, and the delegations to `ServiceControl` / `StoreHubPayload` / `MigrationRunner` / `HealthProbe`. No reordering. No new conditionals.

- [ ] **Rebuild the installer**

```bash
pwsh -NoProfile -File scripts/publish.ps1
pwsh -NoProfile -File installer/build-installer.ps1
```

Note `scripts/publish.ps1` **deletes the whole `publish/` directory** on start — that is expected, but it means the current 190 MB `IndyPOS-Setup.exe` is gone until `build-installer.ps1` finishes.

- [ ] **VM case 1 — happy upgrade**

```bash
pwsh -File scripts/vm-testing/Reset-AndInstall.ps1 -SnapshotName 'Pre-Upgrade-2026-07-25' -InstallerArgs '--silent' -KeepRunning
```

Assert: `MODE=upgrade`; `RESULT=success`; `Store:Id` **and `Store:Type`** preserved; config still `DPAPI:`-protected; service Running; health 200; dump present, non-trivial and ACL-locked; and the payment-method reclassification (`PayLater kind=3`, `WelfareCard kind=2` **still enabled**, `Cash`/`MoneyTransfer kind=1`) via `-AssertDatabase`.

- [ ] **VM case 2 — forced mid-upgrade failure**

```bash
pwsh -File scripts/vm-testing/Reset-AndInstall.ps1 -SnapshotName 'Pre-Upgrade-2026-07-25' -InstallerArgs '--silent','--simulate-failure=deploy' -ExpectRollback
```

Assert: `ROLLED_BACK=true`, old version restored, real config back, service Running **and health ok**. Both of the original 2026-07-25 failures would have flunked this, so it is not optional.

- [ ] **VM case 3 — fresh-install regression**

```bash
pwsh -File scripts/vm-testing/Reset-AndInstall.ps1 -SnapshotName 'Clean-Windows-Ready'
```

Assert: 18/18, `MODE=fresh`. **This gates distribution, not merge** — the branch may land before it, but no installer goes to a store until this passes.

- [ ] **Park the VM**

```bash
pwsh -NoProfile -Command "Stop-VM -Name IndyPOS-Test -Force -TurnOff; Restore-VMSnapshot -VMName IndyPOS-Test -Name 'Pre-Upgrade-2026-07-25' -Confirm:\$false"
```

---

## Self-Review

**Spec coverage.** §1 goals → Tasks 3, 8, 11. §2 architecture → Tasks 4, 5, 6 (+ the frozen-path gate at 6 step 6 and again in final verification). §3 detection → Tasks 1, 3; `--store-id` optional → Task 12; `Unusable` recovery docs → Task 14. §4 sequence → Task 11, with preflight in 9 and backup in 8. §5 failure handling → Task 11 (`RollBackAsync`); forward-only migrations → Task 14 step 3. §6 backup artifacts → Task 8. §7 markers → Task 10; `MODE` emitted by the router in Task 12. §8 data-loss path → Task 2 **and** detection rule 1 in Task 3. §9 entry points → Task 12. §10 testing → per-task unit tests plus Task 13 and the final verification. §11 limitations → documented in Task 14 step 1 item 8. §12 → Task 7 (`sq.version`) and Task 11 (`POS_UPDATED` derivation).

**Two spec items deliberately deferred, and why:**
- §11 item 9, raising the `migrate` command timeout past Npgsql's 30 s. The spec says "consider"; no migration today comes close, and changing it touches StoreHub's runtime rather than the installer. It belongs with the first long DDL migration, and Task 14's release-gate section is where that decision will be reached for.
- §4's `--store-type`-writes-a-single-key option for a store missing `Store:Type`. Task 3 implements the other permitted branch — refuse as `Unusable` — which the spec explicitly allows ("Options when absent: require `--store-type` … or refuse"). Refusing is the smaller change and keeps the upgrade path from writing config, and Task 14 documents the `--store-type` re-run.

**Type consistency check.** `StoreHubConfigFacts` is constructed positionally as `(Exists, ConnectionStringUsable, StoreId, StoreType)` in Tasks 1 and 3 — same order both times. `MigrationRunResult(Success, AdminSeeded, ErrorMessage)` in Tasks 5 and 11 match. `BackupResult(Success, StampDirectory, Locked, ErrorMessage)` in Tasks 8 and 11 match. `PreflightResult(Ok, PgDumpPath, ConnectionString, FailureReason, IsDowngrade)` in Tasks 9 and 11 match. `UpgradeSucceeded` / `UpgradeFailed` field order in Task 10 matches every construction in Task 11. `SimulatedFailure.Parse` is defined in Task 11 and consumed in Task 12.

**One thing an implementer must verify rather than assume:** `DatabaseSetup.TryRestrictFilePermissions` takes a *file* path and uses `FileInfo.GetAccessControl`. Task 8 calls it on the stamp *directory*. If that throws at runtime, add the `DirectoryInfo`/`DirectorySecurity` sibling described in Task 8 step 4 — the ACL on the stamp directory is a real requirement, not a nicety, because `BackupsDirectory` inherits ProgramData's DACL granting `BUILTIN\Users` read.
