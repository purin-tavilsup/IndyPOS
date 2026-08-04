# Migration phase isolation — design

**Date:** 2026-08-04
**Defect:** 12 (`.planning/indypos-overhaul/PLAN.md`)
**Depends on:** PR #61 (`feat/epic2-migration-coverage`). The tests need `LegacyStoreDatabase` and
`LegacyStoreDataBuilder`, which land in that PR, so this work **stacks on that branch** and its PR
must merge after #61.

---

## 1. The defect

`MigrateAllAsync` calls four phases and then saves once:

```csharp
await MigrateUsersAsync(...);      // line 48
await MigrateProductsAsync(...);
await MigrateInvoicesAsync(...);
await MigratePayLaterAsync(...);   // line 51

if (!_options.DryRun)
{
    await context.SaveChangesAsync(ct);   // line 55
}
```

None of the four calls is wrapped in a `try`. Inside each phase, the per-row loop has a `try`, but the
`SELECT` that feeds it does not — nor does the `sqlite_master` probe. So a schema-level problem throws
out of the phase, out of `MigrateAllAsync`, and past line 55.

The tool therefore does the entire migration **in memory** and then discards all of it.

This is exactly what defects 2 and 3 did on every real store: `no such column: PayLaterId` on
GeneralHardware, `no such table: PayLater` on MimyMart and MimyShop. Those two are fixed, but **only
at the symptom**. The structure that turned a single wrong column name into "no store can migrate at
all" is untouched, and it will do the same thing to the next drift — a store whose `PayLater` lacks
`PaidAmount`, a renamed `InvoiceProduct` column, anything.

### Why this is worth fixing on its own

The failure is silent in the way that matters: the operator sees a stack trace, not a diagnosis, and
learns nothing about the *other* three phases. Fixing one drift, re-running, and hitting the next is
an expensive loop when each cycle is a scheduled visit to a working shop whose till is offline.

---

## 2. What must NOT change

**The migration is atomic today, and it stays atomic.** All four phases stage into one `DbContext`
and a single `SaveChangesAsync` commits them in one transaction. A phase failure means nothing
persists.

That property is the reason this defect is merely expensive rather than catastrophic, and the obvious
reading of "isolate the phases so one failure doesn't kill the run" would destroy it. If phases were
allowed to commit independently, a store could end up with products and half its sales history — and
report success. Under-counted revenue that reconciles against nothing is the same class of harm as
defect 10's double-count, and the third instance of this pattern in this epic:

| Defect | The tempting fix | What it would actually cause |
|---|---|---|
| 2 | Correct the column names | Silent ฿836k revenue inflation (defect 10 becomes reachable) |
| 6 | Recompute the discount from the line | A plausible number derived from dead data |
| **12** | **Let each phase commit on its own** | **A half-migrated store reporting success** |

So: **isolate the diagnosis, not the transaction.**

---

## 3. Design

### 3.1 Phase isolation

Each phase call goes through one wrapper:

```csharp
await RunPhaseAsync("Users",    () => MigrateUsersAsync(sqlite, context, ct));
await RunPhaseAsync("Products", () => MigrateProductsAsync(sqlite, context, ct));
await RunPhaseAsync("Invoices", () => MigrateInvoicesAsync(sqlite, context, ct));
await RunPhaseAsync("PayLater", () => MigratePayLaterAsync(sqlite, context, ct));
```

`RunPhaseAsync` catches any exception escaping the phase, logs it, and appends a
`MigrationPhaseFailure(phase, message)` to the result. It does not rethrow — with one exception:
`OperationCanceledException` is rethrown, because a cancelled run is an operator decision, not a
defect in the store's schema, and recording it as a phase failure would misreport it.

**All four phases are always attempted**, even after one fails. That is the point: one run reports
every schema problem, instead of one per round trip. A phase whose upstream dependency failed will
produce useless per-row misses — §3.3 keeps that from drowning the signal.

### 3.2 The save gate

```csharp
if (!_options.DryRun && _result.PhaseFailures.Count == 0)
{
    await context.SaveChangesAsync(ct);
}
```

Nothing is written when any phase failed — the same outcome as today, reached deliberately instead of
by an escaping exception.

`MigrationResult` gains:

```csharp
public List<MigrationPhaseFailure> PhaseFailures { get; } = [];

public bool IsSuccess => PhaseFailures.Count == 0 &&
                         Users.Failed == 0 && Products.Failed == 0 &&
                         Invoices.Failed == 0 && Payments.Failed == 0 &&
                         PayLater.Failed == 0;
```

> ⚠️ **`IsSuccess` including `PhaseFailures` is the load-bearing line of this whole change.**
> `Program.cs:163` is `context.ExitCode = result.IsSuccess ? 0 : 1`. Today a phase failure throws and
> is caught at `Program.cs:165`, so the operator gets a red message and a non-zero exit. After this
> change it returns normally — so if `IsSuccess` ignored `PhaseFailures`, a migration that wrote
> **nothing** would exit **0** and announce success. That would be a far worse defect than the one
> being fixed. It gets its own test (§5, test 2).

### 3.3 Bounded error strings

`MigrationResult.Errors` is unbounded today. When a phase fails, every downstream row misses its
lookup: on real GeneralHardware data a failed Products phase means up to **325,780** near-identical
strings (~32 MB), burying the one line that explains the cause.

Add to `MigrationResult`:

```csharp
public void AddError(string phase, string message)
```

It appends up to `MaxErrorsPerPhase` (100) strings per phase; on the first one past the cap it appends
a single `"<Phase>: N further errors suppressed"` note, updated as more arrive.

**`Failed` counts stay exact.** Only the human-readable strings are capped, so no number in the report
becomes an approximation. The existing `_result.Errors.Add(...)` call sites move to `AddError`.

### 3.4 Console output

`DisplayResults` currently has two branches, success and `"✗ Migration completed with errors"`. After
this change that message would be shown for a run that wrote nothing — inviting the operator to
believe the store is mostly migrated. Three branches:

| Condition | Message |
|---|---|
| `PhaseFailures.Count > 0` | `✗ Migration ABORTED — nothing was written` + each phase failure |
| `!IsSuccess` | `✗ Migration completed with errors` (unchanged; data **was** written) |
| otherwise | `✓ Migration completed successfully!` |

The middle branch keeps its current meaning: per-row failures with a successful commit.

---

## 4. Out of scope

- **`context.Database.MigrateAsync`** (line 42). Failing to reach PostgreSQL is a genuine exception,
  already caught in `Program.cs`. Wrapping it would convert an infrastructure failure into a
  data-shaped one.
- **Per-row handling inside the four phases.** Untouched apart from the `AddError` call sites.
- **Resumability.** The ID maps are in-memory, so a partial migration cannot be resumed. Out of scope,
  and §2 is the reason it is not needed.
- **Defects 4, 5, 6, 7, 8, 11, 13.** Each has its own pinning test and its own fix.

---

## 5. Tests

Named for the defect, in `tests/IndyPOS.MigrationTool.Tests`.

The drift is induced the way it will actually happen — a real artefact schema with a column removed —
rather than by a mock:

```csharp
await store.Connection.ExecuteAsync("ALTER TABLE PayLater DROP COLUMN PaidAmount;");
```

`DROP COLUMN` needs SQLite 3.35+. If the bundled engine in `System.Data.SQLite` 1.0.119 rejects it,
the fallback is to `DROP TABLE PayLater` and recreate it without that column from the artefact's own
DDL — same observable drift, no reliance on a newer engine. Confirm which applies in step 1 of the
plan rather than assuming.

| # | Test | Asserts |
|---|---|---|
| 1 | `MigrateAllAsync_WhenAPhaseFails_DoesNotThrowAndNamesThePhase` | Returns rather than throws; `PhaseFailures` contains one entry naming `PayLater` and the SQLite message |
| 2 | `MigrateAllAsync_WhenAPhaseFails_ReportsFailureSoTheExitCodeIsNonZero` | **`IsSuccess` is false.** The §3.2 warning, pinned |
| 3 | `MigrateAllAsync_WhenAPhaseFails_PersistsNothingFromEarlierPhases` | Users and Products succeeded in memory, yet `StoreUsers`, `Products`, `Invoices` are all **empty** — atomicity held |
| 4 | `MigrateAllAsync_WhenAnEarlyPhaseFails_StillAttemptsTheLaterOnes` | Drift is induced in the **Products** phase; the run still reaches PayLater, evidenced by `PhaseFailures` naming Products while `result.PayLater.Total` shows its rows were examined. Proves one run surfaces problems in more than one phase |
| 5 | `AddError_PastTheCap_BoundsTheStringsButNotTheCounts` | Error strings stop at the cap with a suppression note; `Failed` stays exact |

Non-vacuity: each of tests 1-3 must fail if `RunPhaseAsync` rethrows, and test 2 must fail if
`IsSuccess` omits `PhaseFailures`. Verified by temporarily reverting each, per the harness convention.

The existing 75 tests are the regression net — in particular
`MigrateAllAsync_InDryRun_ShouldWriteNothing` and the defect 2/3 completion tests, which must stay
green, proving a healthy run is unaffected.

---

## 6. Risks

| Risk | Mitigation |
|---|---|
| A phase failure exits 0 and reports success | §3.2 warning; test 2 exists solely for this |
| Swallowing a bug that should crash loudly | Nothing is swallowed: the run returns `IsSuccess == false`, exits 1, prints ABORTED, and persists nothing. The exception's message is carried into `PhaseFailures` |
| `catch (Exception)` hiding a `CancellationToken` cancellation | Rethrow `OperationCanceledException` — a cancelled run is not a phase failure |
| The cap hiding a real error | Only strings are capped, never counts; the suppression note states how many were dropped |
