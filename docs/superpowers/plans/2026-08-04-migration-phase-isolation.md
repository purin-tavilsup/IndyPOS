# Migration Phase Isolation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A phase-level failure in the migrator records a diagnosis and lets the remaining phases run, instead of throwing out of `MigrateAllAsync` and silently discarding an entire in-memory migration.

**Architecture:** Three layers, in dependency order. `MigrationResult` gains a phase-failure list, a three-state `Outcome`, and a per-phase cap on error strings. `SqliteMigrationService` routes its four phase calls through a `RunPhaseAsync` wrapper and gates `SaveChangesAsync` on there being no phase failure — so atomicity is preserved deliberately rather than by an escaping exception. `Program.cs` then reports the three outcomes distinctly, because "completed with errors" applied to a run that wrote nothing would tell the operator their store is mostly migrated.

**Tech Stack:** C# .NET 10, xUnit 2.9.3, FluentAssertions 8.3.0, Dapper, System.Data.SQLite, Testcontainers.PostgreSql (Docker required for Task 2 and 3 tests).

**Spec:** `docs/superpowers/specs/2026-08-04-migration-phase-isolation-design.md`
**Defect:** 12 in `.planning/indypos-overhaul/PLAN.md`
**Branch:** `fix/migration-phase-isolation`, off `development` at `2901d1b` (PR #61 merged).

## Global Constraints

- **Atomicity must not change.** One `SaveChangesAsync` for the whole run. A phase failure persists **nothing**. Never make a phase commit independently — a store with products and half its sales history that reports success is worse than the crash being replaced.
- **`IsSuccess` must account for phase failures.** `Program.cs:163` is `context.ExitCode = result.IsSuccess ? 0 : 1`. If `IsSuccess` ignores phase failures, a migration that wrote nothing exits **0** and announces success.
- **`Failed` counts stay exact.** Only human-readable error *strings* are capped. No number in the report may become an approximation.
- **`OperationCanceledException` is rethrown**, never recorded as a phase failure. A cancelled run is an operator decision, not a schema defect.
- **Existing tests are the regression net.** All 75 in `tests/IndyPOS.MigrationTool.Tests` must stay green. Do not weaken an existing assertion to accommodate a change.
- **Pinning tests exist in this suite** and assert today's *wrong* behaviour on purpose. Do not "fix" one because it looks inverted — read its comment.
- Docker must be running for Task 2 and Task 3 tests. Task 1's tests are pure units and need neither Docker nor the real store databases.

---

## File Structure

| File | Responsibility | Task |
|---|---|---|
| `src/IndyPOS.MigrationTool/MigrationResult.cs` (modify) | The result contract: phase failures, `Outcome`, bounded error strings | 1 |
| `tests/IndyPOS.MigrationTool.Tests/MigrationResultTests.cs` (create) | Pure unit tests for that contract | 1 |
| `src/IndyPOS.MigrationTool/Services/SqliteMigrationService.cs` (modify) | `RunPhaseAsync`, the four wrapped calls, the save gate, `AddError` call sites | 2, 3 |
| `tests/IndyPOS.MigrationTool.Tests/MigrationPhaseIsolationTests.cs` (create) | Integration tests driving real column drift | 2, 3 |
| `src/IndyPOS.MigrationTool/Program.cs` (modify) | Three distinct operator-facing outcomes | 3 |

---

## Task 1: The result contract

**Files:**
- Modify: `src/IndyPOS.MigrationTool/MigrationResult.cs`
- Create: `tests/IndyPOS.MigrationTool.Tests/MigrationResultTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces, all used by Tasks 2 and 3:
  - `sealed record MigrationPhaseFailure(string Phase, string Message)`
  - `enum MigrationOutcome { Success, CompletedWithErrors, Aborted }`
  - `MigrationResult.PhaseFailures` → `List<MigrationPhaseFailure>`
  - `MigrationResult.Outcome` → `MigrationOutcome`
  - `MigrationResult.IsSuccess` → `bool` (now `Outcome == MigrationOutcome.Success`)
  - `MigrationResult.Errors` → `IReadOnlyList<string>` (was `List<string>`)
  - `void MigrationResult.AddError(string phase, string message)`
  - `void MigrationResult.AddPhaseFailure(string phase, string message)`

- [ ] **Step 1: Write the failing tests**

Create `tests/IndyPOS.MigrationTool.Tests/MigrationResultTests.cs`:

```csharp
namespace IndyPOS.MigrationTool.Tests;

/// <summary>
/// Pure unit tests. No Docker, no database, no real store data.
/// </summary>
public class MigrationResultTests
{
    [Fact]
    public void Outcome_WithAPhaseFailure_IsAbortedAndIsSuccessIsFalse()
    {
        // THE load-bearing test of defect 12's fix. Program.cs derives its exit code from IsSuccess.
        // Before this fix a phase failure threw, so the operator got a red message and exit 1. Now it
        // returns normally -- so if IsSuccess ignored PhaseFailures, a migration that wrote NOTHING
        // would exit 0 and announce success. That would be worse than the defect being fixed.
        var result = new MigrationResult();
        result.Users.Migrated = 12;

        result.AddPhaseFailure("PayLater", "SQL logic error: no such column: PaidAmount");

        result.Outcome.Should().Be(MigrationOutcome.Aborted);
        result.IsSuccess.Should().BeFalse("nothing was persisted, so the exit code must be non-zero");
        result.PhaseFailures.Should().ContainSingle()
              .Which.Should().BeEquivalentTo(
                  new MigrationPhaseFailure("PayLater", "SQL logic error: no such column: PaidAmount"));
    }

    [Fact]
    public void Outcome_WithOnlyRowFailures_IsCompletedWithErrors()
    {
        // Distinct from Aborted: these rows were refused but the run still committed, so data WAS
        // written. Conflating the two would tell an operator their store is mostly migrated when
        // nothing at all was written.
        var result = new MigrationResult();
        result.Invoices.Migrated = 500;
        result.Payments.Failed = 1;

        result.Outcome.Should().Be(MigrationOutcome.CompletedWithErrors);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Outcome_WithNothingFailed_IsSuccess()
    {
        var result = new MigrationResult();
        result.Users.Migrated = 3;
        result.Products.Skipped = 2;

        result.Outcome.Should().Be(MigrationOutcome.Success);
        result.IsSuccess.Should().BeTrue("skipped rows are not failures");
    }

    [Fact]
    public void AddError_PastTheCap_BoundsTheStringsAndSaysHowManyWereDropped()
    {
        // An upstream phase failure makes every downstream row miss its lookup: up to 325,780
        // near-identical strings on real GeneralHardware data, burying the one line that explains
        // the cause.
        var result = new MigrationResult();

        for (var i = 1; i <= 250; i++)
        {
            result.AddError("Invoices", $"Invoice {i}: product not found");
        }

        result.Errors.Should().HaveCount(101, "100 real errors plus one suppression note");
        result.Errors.Last().Should().Be("Invoices: 150 further error(s) suppressed.");
    }

    [Fact]
    public void AddError_CapsEachPhaseIndependently()
    {
        var result = new MigrationResult();

        for (var i = 1; i <= 150; i++)
        {
            result.AddError("Invoices", $"Invoice {i}: product not found");
            result.AddError("PayLater", $"PayLater {i}: no migrated payment");
        }

        result.Errors.Should().HaveCount(202, "each phase gets its own 100 plus its own note");
        result.Errors.Should().Contain("Invoices: 50 further error(s) suppressed.");
        result.Errors.Should().Contain("PayLater: 50 further error(s) suppressed.");
    }

    [Fact]
    public void AddError_UnderTheCap_KeepsEveryMessageVerbatim()
    {
        var result = new MigrationResult();

        result.AddError("PayLater", "PayLater 999: no migrated payment for legacy PaymentId 999");

        result.Errors.Should().ContainSingle()
              .Which.Should().Be("PayLater 999: no migrated payment for legacy PaymentId 999");
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/IndyPOS.MigrationTool.Tests --filter "MigrationResultTests"`
Expected: **compile errors** — `MigrationOutcome`, `AddPhaseFailure`, `AddError` and `PhaseFailures` do not exist yet. A compile failure is the correct "red" here.

- [ ] **Step 3: Implement the contract**

In `src/IndyPOS.MigrationTool/MigrationResult.cs`, add above `public class MigrationResult`:

```csharp
/// <summary>
/// A phase that failed as a whole -- its query threw, so none of its rows were examined. Distinct
/// from a per-row failure: a phase failure means NOTHING is persisted for the entire run.
/// </summary>
public sealed record MigrationPhaseFailure(string Phase, string Message);

/// <summary>
/// The three outcomes an operator must be able to tell apart. Collapsing <see cref="Aborted"/> into
/// <see cref="CompletedWithErrors"/> would report a run that wrote nothing as a partial success.
/// </summary>
public enum MigrationOutcome
{
    /// <summary>Everything migrated, and it was committed.</summary>
    Success,

    /// <summary>Committed, but individual rows were refused. Data WAS written.</summary>
    CompletedWithErrors,

    /// <summary>A phase failed wholesale, so NOTHING was written.</summary>
    Aborted
}
```

Then replace the `Errors` and `IsSuccess` members of `MigrationResult` with:

```csharp
    /// <summary>Per phase, because a cascade from one failure must not bury every other phase.</summary>
    private const int MaxErrorsPerPhase = 100;

    private readonly List<string> _errors = [];
    private readonly Dictionary<string, int> _errorCountByPhase = [];
    private readonly Dictionary<string, int> _suppressionNoteIndexByPhase = [];

    /// <summary>Read-only so every write goes through <see cref="AddError"/> and stays bounded.</summary>
    public IReadOnlyList<string> Errors => _errors;

    /// <summary>Phases that failed as a whole. Non-empty means nothing was persisted.</summary>
    public List<MigrationPhaseFailure> PhaseFailures { get; } = [];

    public MigrationOutcome Outcome =>
        PhaseFailures.Count > 0 ? MigrationOutcome.Aborted :
        AnyRowFailed ? MigrationOutcome.CompletedWithErrors :
        MigrationOutcome.Success;

    /// <remarks>
    /// Defect 12: this MUST count phase failures. Program.cs turns it into the process exit code, so
    /// a phase failure that left IsSuccess true would exit 0 on a run that wrote nothing.
    /// </remarks>
    public bool IsSuccess => Outcome == MigrationOutcome.Success;

    private bool AnyRowFailed => Users.Failed > 0 || Products.Failed > 0 ||
                                 Invoices.Failed > 0 || Payments.Failed > 0 ||
                                 PayLater.Failed > 0;

    /// <summary>
    /// Records a per-row error, capped per phase. The <see cref="EntityMigrationResult.Failed"/>
    /// counts stay exact -- only these strings are capped, because an upstream phase failure makes
    /// every downstream row miss its lookup (up to 325,780 on real data).
    /// </summary>
    public void AddError(string phase, string message)
    {
        var alreadyRecorded = _errorCountByPhase.GetValueOrDefault(phase);
        _errorCountByPhase[phase] = alreadyRecorded + 1;

        if (alreadyRecorded < MaxErrorsPerPhase)
        {
            _errors.Add(message);
            return;
        }

        var note = $"{phase}: {alreadyRecorded + 1 - MaxErrorsPerPhase} further error(s) suppressed.";

        if (_suppressionNoteIndexByPhase.TryGetValue(phase, out var index))
        {
            _errors[index] = note;
            return;
        }

        _errors.Add(note);
        _suppressionNoteIndexByPhase[phase] = _errors.Count - 1;
    }

    public void AddPhaseFailure(string phase, string message) =>
        PhaseFailures.Add(new MigrationPhaseFailure(phase, message));
```

Leave `Users`, `Products`, `Invoices`, `Payments`, `PayLater`, `TotalMigrated` and the four ID maps exactly as they are.

- [ ] **Step 4: Fix the now-broken writers so the solution compiles**

`Errors` is read-only now, so the six `_result.Errors.Add(...)` calls in
`src/IndyPOS.MigrationTool/Services/SqliteMigrationService.cs` will not compile. Convert each to
`AddError` with its phase name (this is the call-site migration the cap needs):

| Line (approx) | Was | Becomes |
|---|---|---|
| 127 | `_result.Errors.Add($"User {user.UserId}: {ex.Message}");` | `_result.AddError("Users", $"User {user.UserId}: {ex.Message}");` |
| 205 | `_result.Errors.Add($"Product {product.Barcode}: {ex.Message}");` | `_result.AddError("Products", $"Product {product.Barcode}: {ex.Message}");` |
| 301 | `_result.Errors.Add(` (payment-method refusal) | `_result.AddError("Invoices",` — keep the message text byte-identical |
| 338 | `_result.Errors.Add($"Invoice {invoice.InvoiceId}: {ex.Message}");` | `_result.AddError("Invoices", $"Invoice {invoice.InvoiceId}: {ex.Message}");` |
| 390 | `_result.Errors.Add(` (PayLater map miss) | `_result.AddError("PayLater",` — keep the message text byte-identical |
| 425 | `_result.Errors.Add($"PayLater {payLater.PaymentId}: {ex.Message}");` | `_result.AddError("PayLater", $"PayLater {payLater.PaymentId}: {ex.Message}");` |

⚠️ **Do not reword any message.** Existing tests assert on their content — e.g.
`result.Errors.Should().ContainSingle().Which.Should().Contain("999")` and
`.Contain("PaymentTypeId 6")`. Only the call changes, never the string.

`MigrationVerifier.cs` also writes to a `result.Errors`, but that is `VerificationResult` — a
different class. Do **not** touch it.

- [ ] **Step 5: Run the new tests and the whole suite**

Run: `dotnet test tests/IndyPOS.MigrationTool.Tests --filter "MigrationResultTests"`
Expected: **6 passed.**

Run: `dotnet test tests/IndyPOS.MigrationTool.Tests`
Expected: **80 passed, 1 skipped, 81 total, 0 failed.** (The branch point is 74 passed + 1 skipped =
75; these 6 new tests are all passing additions.) State the numbers you actually observe. If any
pre-existing test now fails, STOP — a message was reworded, or `IsSuccess` changed meaning for a case
that is not a phase failure.

- [ ] **Step 6: Commit**

```bash
git add src/IndyPOS.MigrationTool/MigrationResult.cs \
        src/IndyPOS.MigrationTool/Services/SqliteMigrationService.cs \
        tests/IndyPOS.MigrationTool.Tests/MigrationResultTests.cs
git commit -m "feat(migration): add phase failures and bounded errors to MigrationResult

Defect 12, part 1 of 3. The result type could not express 'a whole phase
failed', because such a failure threw out of MigrateAllAsync and no result
was ever returned.

Adds PhaseFailures, a three-state Outcome, and AddError with a per-phase cap
of 100 strings. Failed COUNTS stay exact -- only the strings are capped,
because an upstream phase failure makes every downstream row miss its lookup
(up to 325,780 near-identical messages on real data, burying the cause).

IsSuccess now derives from Outcome, so it counts phase failures. That line is
load-bearing: Program.cs turns IsSuccess into the process exit code, so
without it a run that wrote nothing would exit 0 and report success.

Errors becomes IReadOnlyList so every write goes through the cap; the six
call sites move to AddError with their phase names and byte-identical
message text."
```

---

## Task 2: Phase isolation and the save gate

**Files:**
- Modify: `src/IndyPOS.MigrationTool/Services/SqliteMigrationService.cs:26-62`
- Create: `tests/IndyPOS.MigrationTool.Tests/MigrationPhaseIsolationTests.cs`

**Interfaces:**
- Consumes: everything Task 1 produced. Also `MigrationScenario.RunAsync(store, postgres, dryRun)` from `PayLaterMigrationTests.cs`, `LegacyStoreDatabase.CreateAsync(LegacyStoreShape)`, `LegacyStoreDataBuilder`, `PostgresFixture`.
- Produces: `private Task RunPhaseAsync(string phase, Func<Task> migratePhase)`. Nothing outside the class uses it.

- [ ] **Step 1: Write the failing tests**

Create `tests/IndyPOS.MigrationTool.Tests/MigrationPhaseIsolationTests.cs`:

```csharp
using Dapper;
using IndyPOS.MigrationTool.Tests.Fixtures;
using IndyPOS.MigrationTool.Tests.Tools;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.MigrationTool.Tests;

/// <summary>
/// Defect 12. A phase-level throw used to escape MigrateAllAsync entirely, and because
/// SaveChangesAsync runs after the last phase, an entire in-memory migration was discarded with only
/// a stack trace to show for it.
///
/// The drift is induced the way it will really happen -- a real artefact schema with a column
/// removed -- because that is exactly what defects 2 and 3 were.
/// </summary>
[Collection("Postgres")]
public class MigrationPhaseIsolationTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;

    public MigrationPhaseIsolationTests(PostgresFixture postgres) => _postgres = postgres;

    public Task InitializeAsync() => _postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// A healthy credit sale: user, product, invoice, line, type-2 payment, PayLater extension.
    /// Every phase has something to do, so "an earlier phase succeeded" is a real claim.
    /// </summary>
    private static async Task SeedOneOfEverythingAsync(LegacyStoreDatabase store)
    {
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddProductAsync(
            productId: 10, barcode: "8850001000010", description: "Cement 50kg",
            unitPrice: 700m, quantityInStock: 20, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");
        await builder.AddInvoiceAsync(1, userId: 1, total: 700m, dateCreated: "2024-03-15 14:30:00");
        await builder.AddInvoiceLineAsync(
            invoiceProductId: 1, invoiceId: 1, productId: 10, barcode: "8850001000010",
            description: "Cement 50kg", quantity: 1, unitPrice: 700m, originalUnitPrice: 700m);
        await builder.AddPaymentAsync(
            paymentId: 500, invoiceId: 1, paymentTypeId: 2, amount: 700m,
            dateCreated: "2024-03-15 14:30:00", note: "Somchai");
        await builder.AddPayLaterAsync(
            paymentId: 500, invoiceId: 1, description: "Somchai", payLaterAmount: 700m,
            paidAmount: 0m, isCompleted: false, dateCreated: "2024-03-15 14:30:00");
    }

    /// <summary>
    /// Drops a column the migrator's SELECT names by hand, which is what column drift looks like.
    /// PaidAmount is a plain NUMERIC with no index, no UNIQUE and no PK role, so SQLite permits it.
    /// </summary>
    private static Task DropPayLaterPaidAmountAsync(LegacyStoreDatabase store) =>
        store.Connection.ExecuteAsync("ALTER TABLE PayLater DROP COLUMN PaidAmount;");

    [Fact]
    public async Task MigrateAllAsync_WhenAPhaseFails_DoesNotThrowAndNamesThePhase()
    {
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedOneOfEverythingAsync(store);
        await DropPayLaterPaidAmountAsync(store);

        var result = await MigrationScenario.RunAsync(store, _postgres);

        result.PhaseFailures.Should().ContainSingle(
            "the PayLater SELECT names PaidAmount, so it throws -- and that must be caught");
        var failure = result.PhaseFailures.Single();
        failure.Phase.Should().Be("PayLater");
        failure.Message.Should().Contain("PaidAmount",
            "the operator needs the actual cause, not just the phase name");
    }

    [Fact]
    public async Task MigrateAllAsync_WhenAPhaseFails_ReportsFailureSoTheExitCodeIsNonZero()
    {
        // Program.cs:163 is `context.ExitCode = result.IsSuccess ? 0 : 1`. Before this fix the phase
        // threw and was caught there, so the operator got exit 1. Now it returns normally -- so
        // IsSuccess is the ONLY thing standing between a store that migrated nothing and a green
        // "success" on the console.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedOneOfEverythingAsync(store);
        await DropPayLaterPaidAmountAsync(store);

        var result = await MigrationScenario.RunAsync(store, _postgres);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(MigrationOutcome.Aborted);
    }

    [Fact]
    public async Task MigrateAllAsync_WhenAPhaseFails_PersistsNothingFromEarlierPhases()
    {
        // The property that keeps defect 12 merely expensive rather than catastrophic. Users,
        // products and invoices all migrated successfully IN MEMORY before PayLater threw. None of
        // it may reach PostgreSQL: a store holding products and part of its sales history, reported
        // as a success, is worse than no migration at all.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedOneOfEverythingAsync(store);
        await DropPayLaterPaidAmountAsync(store);

        var result = await MigrationScenario.RunAsync(store, _postgres);

        result.Users.Migrated.Should().Be(1, "the Users phase itself succeeded");
        result.Products.Migrated.Should().Be(1);
        result.Invoices.Migrated.Should().Be(1);

        await using var db = _postgres.CreateDbContext();
        (await db.StoreUsers.CountAsync()).Should().Be(0, "no phase may be committed when another failed");
        (await db.Products.CountAsync()).Should().Be(0);
        (await db.Invoices.CountAsync()).Should().Be(0);
        (await db.Payments.CountAsync()).Should().Be(0);
        (await db.PayLaters.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task MigrateAllAsync_WhenAnEarlyPhaseFails_StillAttemptsTheLaterOnes()
    {
        // The whole point of isolating the diagnosis: one attempt reports every schema problem.
        // Re-running against a real shop costs a visit with the till switched off, so learning about
        // one broken phase per run is expensive.
        // Manufacturer is named explicitly in the Products SELECT and is a plain nullable TEXT with
        // no index, so dropping it makes that phase -- and only that phase -- throw.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedOneOfEverythingAsync(store);
        await store.Connection.ExecuteAsync("ALTER TABLE InventoryProduct DROP COLUMN Manufacturer;");

        var result = await MigrationScenario.RunAsync(store, _postgres);

        result.PhaseFailures.Should().ContainSingle().Which.Phase.Should().Be("Products");
        result.Users.Migrated.Should().Be(1, "the phase before the failure still ran");
        result.Invoices.Migrated.Should().Be(1, "the phase AFTER the failure still ran");
        result.PayLater.Migrated.Should().Be(1, "and so did the last one");
        result.IsSuccess.Should().BeFalse("a phase still failed, so nothing was written");
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/IndyPOS.MigrationTool.Tests --filter "MigrationPhaseIsolationTests"`
Expected: **4 failed.** The first three fail because the SQLite exception escapes `MigrateAllAsync`
(the test reports the raised `SQLiteException`, not an assertion failure) — which is precisely defect
12. Confirm the failure message mentions `PaidAmount` or `Manufacturer`; if instead you see
`ALTER TABLE ... DROP COLUMN` rejected by SQLite, apply the spec's §5 fallback (drop and recreate the
table from the artefact DDL without that column) before continuing.

- [ ] **Step 3: Implement the wrapper and the save gate**

In `src/IndyPOS.MigrationTool/Services/SqliteMigrationService.cs`, replace the four phase calls and
the save block (currently lines 47-56) with:

```csharp
        // Order matters: Users → Products → Invoices (with lines/payments) → PayLater
        // Defect 12: each phase is isolated so ONE run reports every schema problem. Re-running
        // against a real shop costs a visit with the till switched off.
        await RunPhaseAsync("Users", () => MigrateUsersAsync(sqliteConnection, context, ct));
        await RunPhaseAsync("Products", () => MigrateProductsAsync(sqliteConnection, context, ct));
        await RunPhaseAsync("Invoices", () => MigrateInvoicesAsync(sqliteConnection, context, ct));
        await RunPhaseAsync("PayLater", () => MigratePayLaterAsync(sqliteConnection, context, ct));

        // Isolating the diagnosis must NOT isolate the transaction. If any phase failed the run is
        // not trustworthy, so nothing is written -- the same outcome as before, reached deliberately
        // instead of by an exception escaping past this line. A half-migrated store that reported
        // success would be far worse than the crash this replaces.
        if (!_options.DryRun && _result.PhaseFailures.Count == 0)
        {
            await context.SaveChangesAsync(ct);
        }
```

Then add this private method next to the phase methods:

```csharp
    /// <summary>
    /// Runs one migration phase, turning a phase-level throw into a recorded failure so the remaining
    /// phases still run and report their own state.
    /// </summary>
    /// <remarks>
    /// Nothing is swallowed: a recorded failure makes <see cref="MigrationResult.IsSuccess"/> false,
    /// which is the process exit code, prints an ABORTED banner, and suppresses the save.
    /// </remarks>
    private async Task RunPhaseAsync(string phase, Func<Task> migratePhase)
    {
        try
        {
            await migratePhase();
        }
        catch (OperationCanceledException)
        {
            // An operator cancelling is not a defect in the store's schema.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Migration phase {Phase} failed. Nothing will be persisted for this run.", phase);
            _result.AddPhaseFailure(phase, ex.Message);
        }
    }
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/IndyPOS.MigrationTool.Tests --filter "MigrationPhaseIsolationTests"`
Expected: **4 passed.**

- [ ] **Step 5: Prove the tests are not vacuous**

Two probes, each reverted immediately afterwards. Confirm `git diff -- src/` is empty before moving on.

1. Make `RunPhaseAsync` rethrow: add `throw;` as the last line of the `catch (Exception ex)` block.
   Run the class: the first three tests **must fail** with the escaping `SQLiteException`. Revert.
2. Make the save gate ignore phase failures: change the condition to `if (!_options.DryRun)`.
   Run the class: `MigrateAllAsync_WhenAPhaseFails_PersistsNothingFromEarlierPhases` **must fail**,
   reporting non-zero counts — i.e. the half-migrated store the Global Constraints forbid. Revert.

If either probe leaves the tests green, the test is not testing what it claims. Stop and report.

- [ ] **Step 6: Run the whole suite**

Run: `dotnet test tests/IndyPOS.MigrationTool.Tests`
Expected: **85 total** (81 after Task 1, plus 4), 1 skipped, 0 failed. In particular
`MigrateAllAsync_InDryRun_ShouldWriteNothing` and the defect 2/3 completion tests must still pass,
proving a healthy run is unaffected.

- [ ] **Step 7: Commit**

```bash
git add src/IndyPOS.MigrationTool/Services/SqliteMigrationService.cs \
        tests/IndyPOS.MigrationTool.Tests/MigrationPhaseIsolationTests.cs
git commit -m "fix(migration): isolate phase failures without breaking atomicity

Defect 12, part 2 of 3. The four phase calls sat in no try, and
SaveChangesAsync runs after the last of them, so a schema-level throw escaped
MigrateAllAsync and discarded an entire in-memory migration. That is what
defects 2 and 3 did to every real store; fixing their column names left the
amplifier in place for the next drift.

Each phase now runs through RunPhaseAsync, which records a phase failure
instead of throwing, so all four are attempted and ONE run reports every
schema problem. Re-running against a real shop costs a visit with the till
switched off.

Atomicity is deliberately preserved: the save is gated on there being no
phase failure, so nothing is written -- the same outcome as before, reached
on purpose rather than by an escaping exception. Letting phases commit
independently was considered and rejected in the spec: a store holding
products and half its sales history, reporting success, is the same class of
harm as defect 10.

OperationCanceledException is rethrown; a cancelled run is an operator
decision, not a schema defect.

Tests induce the drift the way it will really happen -- ALTER TABLE DROP
COLUMN on a real artefact schema -- and both directions are proven
non-vacuous: making RunPhaseAsync rethrow fails three of them, and ungating
the save fails the no-partial-write test."
```

---

## Task 3: Tell the operator the truth

**Files:**
- Modify: `src/IndyPOS.MigrationTool/Program.cs:301-316`
- Modify: `tests/IndyPOS.MigrationTool.Tests/MigrationPhaseIsolationTests.cs` (add one test)

**Interfaces:**
- Consumes: `MigrationResult.Outcome`, `MigrationOutcome`, `MigrationResult.PhaseFailures` from Task 1; the wrapper from Task 2.
- Produces: nothing.

- [ ] **Step 1: Write the failing test**

The console rendering itself is `AnsiConsole` output and not worth a capture harness — Task 1 already
unit-tests the `Outcome` decision that drives it. What is untested is that a real cascade stays
bounded while the counts stay exact. Append to `MigrationPhaseIsolationTests.cs`:

```csharp
    [Fact]
    public async Task MigrateAllAsync_WithMoreRowErrorsThanTheCap_BoundsTheStringsButNotTheCounts()
    {
        // Drives the cap through the real service. This run has NO phase failure -- it reaches the cap
        // with ordinary per-row refusals, which is the cheapest way to exercise it. The cascade the cap
        // exists for (a failed Products phase making every invoice line miss its lookup, up to 325,780
        // near-identical strings on real data) is the same code path with a bigger multiplier.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddProductAsync(
            productId: 10, barcode: "8850001000010", description: "Cement 50kg",
            unitPrice: 1m, quantityInStock: 5, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");

        // 150 invoices, each paid by a legacy type with no catalogue code, so each records one
        // per-row error in the Invoices phase -- 150 > the cap of 100.
        for (var i = 1; i <= 150; i++)
        {
            await builder.AddInvoiceAsync(i, userId: 1, total: 1m, dateCreated: "2024-03-15 14:30:00");
            await builder.AddPaymentAsync(
                paymentId: 500 + i, invoiceId: i, paymentTypeId: 6, amount: 1m,
                dateCreated: "2024-03-15 14:30:00");
        }

        var result = await MigrationScenario.RunAsync(store, _postgres);

        result.Payments.Failed.Should().Be(150, "the COUNT must stay exact");
        result.Errors.Should().HaveCount(101, "100 strings plus one suppression note");
        result.Errors.Last().Should().Be("Invoices: 50 further error(s) suppressed.");
    }
```

- [ ] **Step 2: Run it**

Run: `dotnet test tests/IndyPOS.MigrationTool.Tests --filter "BoundsTheStringsButNotTheCounts"`
Expected: **PASS** — Task 1 already implemented the cap and moved the call sites. This test exists to
prove the cap works through the real service rather than only in a unit test. If it fails with 150
errors instead of 101, a call site at line 301 or 338 was left on the old `Errors.Add`.

- [ ] **Step 3: Split the console output into three outcomes**

In `src/IndyPOS.MigrationTool/Program.cs`, replace the `if (result.IsSuccess) { ... } else { ... }`
block at lines 301-316 with:

```csharp
    switch (result.Outcome)
    {
        case MigrationOutcome.Success:
            AnsiConsole.MarkupLine("\n[green]✓ Migration completed successfully![/]");
            break;

        case MigrationOutcome.Aborted:
            // Distinct from "completed with errors" on purpose: NOTHING was written. Saying
            // "completed" here would tell the operator their store is mostly migrated.
            AnsiConsole.MarkupLine("\n[red]✗ Migration ABORTED - nothing was written.[/]");
            AnsiConsole.MarkupLine("[red]  The whole run was discarded because a phase failed:[/]");
            foreach (var failure in result.PhaseFailures)
            {
                AnsiConsole.MarkupLine($"  [red]•[/] [bold]{failure.Phase}[/]: {failure.Message}");
            }
            AnsiConsole.MarkupLine(
                "[grey]  Fix the cause and re-run. The database is untouched.[/]");
            break;

        default:
            AnsiConsole.MarkupLine("\n[red]✗ Migration completed with errors[/]");
            foreach (var error in result.Errors.Take(10))
            {
                AnsiConsole.MarkupLine($"  [red]•[/] {error}");
            }
            if (result.Errors.Count > 10)
            {
                AnsiConsole.MarkupLine($"  [grey]... and {result.Errors.Count - 10} more errors[/]");
            }
            break;
    }
```

`Program.cs:163` (`context.ExitCode = result.IsSuccess ? 0 : 1`) needs **no change** — Task 1 made
`IsSuccess` account for phase failures, so an aborted run already exits 1.

- [ ] **Step 4: Verify the whole solution builds and passes**

Run: `dotnet build`
Expected: 0 errors.

Run: `dotnet test tests/IndyPOS.MigrationTool.Tests`
Expected: **86 total**, 1 skipped, 0 failed.

Run: `dotnet test IndyPOS.sln`
Expected: all six suites green. Record the totals.

- [ ] **Step 5: Update the defect table**

In `.planning/indypos-overhaul/PLAN.md`, change defect 12's Status cell from
`❌ **new 2026-08-04** — not pinned; needs its own fix spec` to `✅ Fixed <sha of Task 2's commit>`.

Also update the "RESOLVED 2026-08-04" banner note above the table: it currently credits `926a245`
and `cf61005` with fixing the crash. Add that the *structural* cause — a phase throw discarding the
whole run — was fixed separately as defect 12, with this plan's sha.

- [ ] **Step 6: Commit**

```bash
git add src/IndyPOS.MigrationTool/Program.cs \
        tests/IndyPOS.MigrationTool.Tests/MigrationPhaseIsolationTests.cs \
        .planning/indypos-overhaul/PLAN.md
git commit -m "feat(migration): report an aborted run distinctly from a partial one

Defect 12, part 3 of 3. DisplayResults had two branches, so a run that wrote
NOTHING was announced as 'Migration completed with errors' -- which invites
the operator to believe the store is mostly migrated and go looking for the
few bad rows.

Three outcomes now: completed, completed-with-errors (data WAS written), and
ABORTED - nothing was written, listing each failed phase and its cause and
stating that the database is untouched.

The exit code needed no change: IsSuccess already counts phase failures.

Adds the cascade test that proves the per-phase cap works through the real
service -- 150 refused payments produce an exact Failed count of 150 but only
101 strings -- and marks defect 12 fixed in PLAN.md."
```

---

## Self-Review

**Spec coverage.** §1 defect → Task 2. §2 atomicity constraint → Global Constraints + Task 2 Step 3
save gate + Task 2 Step 5 probe 2 + test 3. §3.1 phase isolation → Task 2. §3.2 save gate and the
`IsSuccess` warning → Task 1 (`Outcome`/`IsSuccess`) and Task 2, pinned by Task 1's first test and
Task 2's second. §3.3 bounded errors → Task 1 Steps 3-4, Task 3 Step 1. §3.4 console → Task 3 Step 3.
§4 out of scope → nothing in any task touches `Database.MigrateAsync`, per-row logic beyond the
`AddError` call sites, or resumability. §5 tests → Tasks 1-3, all five spec tests present
(spec test 5 appears as Task 1's cap tests plus Task 3's end-to-end cascade test). §6 risks → each
has a named mitigation: exit code (Task 1 test 1, Task 2 test 2), nothing swallowed (Task 2 Step 3
`<remarks>`), cancellation (Task 2 Step 3), cap hiding errors (Task 1 Step 3 suppression note).

**Placeholder scan.** No TBD/TODO. Every code step contains the actual code. The one conditional
instruction (Task 2 Step 2's SQLite fallback) names the exact alternative and the trigger for it.

**Type consistency.** `AddError(string phase, string message)`, `AddPhaseFailure(string phase, string message)`,
`MigrationPhaseFailure(string Phase, string Message)`, `MigrationOutcome { Success, CompletedWithErrors, Aborted }`,
`PhaseFailures`, `Outcome`, `IsSuccess`, `Errors` as `IReadOnlyList<string>`, and
`RunPhaseAsync(string phase, Func<Task> migratePhase)` are spelled identically in every task that
uses them. Phase name strings — `"Users"`, `"Products"`, `"Invoices"`, `"PayLater"` — match between
Task 1 Step 4's call-site table, Task 2 Step 3's wrapper calls, and every assertion.

**Count arithmetic.** 75 at branch point → 81 after Task 1 (+6) → 85 after Task 2 (+4) → 86 after
Task 3 (+1). One of these is the always-skipped extractor, so expect "N−1 passed, 1 skipped".
