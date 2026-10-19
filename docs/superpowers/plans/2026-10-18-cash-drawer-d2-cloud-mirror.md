# Cash Drawer D2 — Cloud Mirror Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The cloud keeps a queryable, self-describing copy of every cash payout, cash float, debt
repayment and cash count, built from the four events StoreHub already sends.

**Architecture:** Four mirror tables in `CloudDbContext`, each table and column carrying a Postgres
comment. One new static class, `CashDrawerMirror`, turns an inbox event into an upsert by `Id`
(insert if new; update only if the incoming `LastModifiedUtc` is newer; counts insert-only).
`EventProcessor` adds the four types to its allow-list and its switch. Nothing changes on StoreHub.

**Tech Stack:** C# .NET 10, EF Core 10 + Npgsql (cloud), xUnit + FluentAssertions, Testcontainers
Postgres (`CloudPostgresFixture`).

**Spec:** `docs/superpowers/specs/2026-09-20-cash-payout-float-persistence-design.md` §7 (cloud side)
and §2 (the cloud copy is the off-site audit record, replacing the Drive CSV). **PR D2 of 4**: D1
(expected cash, #132) is merged; D3 = till client + `CashDrawerViewModel`; D4 = WinForms section.

## Already true (surveyed 2026-10-18, so no task)

- StoreHub writes `CashPayoutChanged` / `CashFloatChanged` / `DebtRepaymentChanged` /
  `CashCountChanged` in the same save as the row (`CashDrawerOutbox`, `Application/UseCases/StoreHub/CashDrawer/Common`).
  Payload = the **domain entity** serialized with default `System.Text.Json` (PascalCase;
  `Category` as `"General"`/`"Hardware"`; `BusinessDate` as `"yyyy-MM-dd"`; a count also carries a
  computed `CountedTotal`). **No `EventId` in the payload** — the inbox row's `EventId` is the key.
- The cloud **no longer drops** unknown types: `GetUnprocessedAsync(HandledEventTypes, …)` fetches
  only handled types, so today's cash events are waiting in `SyncedEvents`, and will be processed
  once this ships. Spec §7's "deploy the cloud first" note is therefore obsolete.
- The `BeginTransactionAsync` bug is fixed: handlers do one plain `SaveChangesAsync` with their
  `ProcessedEvent` row; a thrown handler gets the inbox retry backoff.

## Global Constraints

- **Upsert by `Id`; stale events are ignored:** "if the incoming `LastModifiedUtc` is not newer than
  the stored row's, skip it" (spec §7). Not newer = older **or equal**. A skipped event is still
  marked processed (a `ProcessedEvent` row), so it is not retried.
- **`CashCount` is insert-only** but goes through the same path: an `Id` already stored is skipped.
- **A row never changes store.** An event whose `Id` is stored under a different `StoreId` throws
  (so it is retried and, after 10 attempts, logged as an Error) — same stance as `InvoiceReprinted`.
- **`ProcessedEvent.EventId = syncedEvent.EventId`** (payloads have no event id), saved in the same
  `SaveChangesAsync` as the mirror row. Deserialize with default options into the **Domain entity
  types** (`CashPayout`, `CashFloat`, `DebtRepayment`, `CashCount`) — the exact types StoreHub
  serializes, so the contract cannot drift.
- **Readable by AI agents** (spec §7): table names `CashPayouts`, `CashFloats`, `DebtRepayments`,
  `CashCounts`; enums as strings (`Category` is a `string`, max 20); money `decimal` baht; cash day
  `BusinessDate` (`date`); instants follow the **cloud** suffix `…AtUtc` (`CreatedAtUtc`,
  `LastModifiedAtUtc`, `DeletedAtUtc`, `SyncedAtUtc`), as `CloudInvoiceReprint` does. **Every table
  and every column has a `HasComment`.** `StoreId` `HasMaxLength(50)`. No `CountedTotal` column
  (spec §3: totals are derived, never stored).
- **User ids, decided:** `CreatedByUserId` / `LastModifiedByUserId` are mirrored **as StoreHub sends
  them** (`StoreUser.Id`). The survey found they equal cloud `Users.Id` **only for users migrated
  from v3**; a cloud-created user gets a new store-side id (`UserSyncService.MapToStoreUser`), and
  `Invoices.UserId` already has the same gap. The column comment says so plainly. Joining to
  `Users` for every user is a follow-up for all mirrors at once, not this PR.
- **Times are UTC on write:** every mirrored `DateTime` passes through `DateTime.SpecifyKind(x, DateTimeKind.Utc)`
  (Npgsql refuses an unspecified kind for `timestamptz`).
- **One additive cloud migration** `AddCashDrawerMirror` (four new tables, nothing else) — passes the
  cloud forward-only gate (`docs/operations/upgrade-procedure.md` cloud recipe).
- **Test naming:** `Subject_WhenScenario_DirectVerbOutcome`, never `Should`; one behaviour per test;
  negative first (more than half); named constants; AAA blank lines; dots vertically aligned.
- **Docker must be running** for every task (all tests are on Postgres).

## Review Focus

1. **An edit arriving before its add** (out-of-order delivery) must end with the edit's values, not
   the add's. → Task 2 `ProcessPendingEventsAsync_WithAnEditArrivingBeforeItsAdd_KeepsTheEdit`.
2. **A delete followed by a stale retry of the earlier edit** must leave the row deleted. → Task 2
   `ProcessPendingEventsAsync_WithAStaleEditAfterADelete_KeepsTheRowDeleted`.
3. **Another store's event reusing an existing row's `Id`** must not overwrite it. → Task 2
   `ProcessPendingEventsAsync_WithARowIdFromAnotherStore_LeavesTheRowUnchanged`.
4. **A payload whose times carry no `Z`** (unspecified kind) must still mirror. → Task 2
   `ProcessPendingEventsAsync_WithTimesWithoutAZone_StoresThemAsUtc`.
5. **The same count delivered twice under two inbox ids** must leave one row. → Task 3
   `ProcessPendingEventsAsync_WithTheSameCountTwice_StoresOneRow`.

---

## File Structure

```
src/IndyPOS.CloudApi/Domain/
  CloudCashDrawer.cs                CREATE  ICloudCashRow + CloudCashPayout/CloudCashFloat/
                                            CloudDebtRepayment/CloudCashCount
src/IndyPOS.CloudApi/Infrastructure/
  CloudDbContext.cs                 MODIFY  4 DbSets + ConfigureCashDrawer(modelBuilder)
  CashDrawerMirror.cs               CREATE  event -> upsert (one public method per event type)
  EventProcessor.cs                 MODIFY  HandledEventTypes + switch cases
  Migrations/<ts>_AddCashDrawerMirror.cs  CREATE (dotnet ef)
tests/IndyPOS.CloudApi.IntegrationTests/
  EventPipeline.cs                  CREATE  Pipeline moved out of EventProcessorTests (pure move)
  EventProcessorTests.cs            MODIFY  uses EventPipeline; EventTypeWithoutAHandler repointed
  CashDrawerMirrorSchemaTests.cs    CREATE
  CashDrawerMirrorTests.cs          CREATE
docs/operations/upgrade-procedure.md, CLAUDE.md, ONBOARDING.md,
docs/superpowers/specs/2026-09-20-cash-payout-float-persistence-design.md   MODIFY
```

---

### Task 1: Mirror tables that explain themselves

**Files:**
- Create: `src/IndyPOS.CloudApi/Domain/CloudCashDrawer.cs`
- Modify: `src/IndyPOS.CloudApi/Infrastructure/CloudDbContext.cs`
- Create: `src/IndyPOS.CloudApi/Infrastructure/Migrations/<timestamp>_AddCashDrawerMirror.cs` (generated)
- Test: `tests/IndyPOS.CloudApi.IntegrationTests/CashDrawerMirrorSchemaTests.cs`

**Interfaces:**
- Produces:
  ```csharp
  public interface ICloudCashRow { Guid Id { get; } string StoreId { get; } DateTime LastModifiedAtUtc { get; } }
  public class CloudCashPayout : ICloudCashRow   // Id, StoreId, Category (string), Amount, Description?,
                                                  // BusinessDate (DateOnly), CreatedAtUtc, LastModifiedAtUtc,
                                                  // CreatedByUserId, LastModifiedByUserId?, IsDeleted, DeletedAtUtc?, SyncedAtUtc
  public class CloudCashFloat : ICloudCashRow     // as payout, without Category
  public class CloudDebtRepayment : ICloudCashRow // as float, CustomerName (string) instead of Description
  public class CloudCashCount : ICloudCashRow     // Id, StoreId, BusinessDate, the nine *Count ints,
                                                  // CreatedAtUtc, LastModifiedAtUtc, CreatedByUserId, SyncedAtUtc
  ```
  `CloudDbContext.CashPayouts`, `.CashFloats`, `.DebtRepayments`, `.CashCounts`.

- [ ] **Step 1: Write the failing tests** in `CashDrawerMirrorSchemaTests` (`IClassFixture<CloudPostgresFixture>`,
  migrate a fresh database, then read `pg_catalog`). Named constant `CashTables = ["CashPayouts", "CashFloats", "DebtRepayments", "CashCounts"]`.
  - `CashDrawerMirror_ForEveryColumn_HasAComment` — `[Theory]` over `CashTables`: no row from
    `information_schema.columns c where table_name = @t and col_description(('"'||@t||'"')::regclass, c.ordinal_position) is null`.
  - `CashDrawerMirror_ForEveryTable_HasAComment` — `obj_description(('"'||@t||'"')::regclass, 'pg_class')` is not null or empty.
  - `CashDrawerMirror_ForCashCounts_HasNoCountedTotalColumn` — no `CountedTotal` column.
  - `CashDrawerMirror_ForCashPayouts_StoresCategoryAsText` — `data_type` of `Category` is `character varying`.

- [ ] **Step 2: Run, expect FAIL** (tables do not exist).
  Run: `dotnet test tests/IndyPOS.CloudApi.IntegrationTests --filter "FullyQualifiedName~CashDrawerMirrorSchemaTests"`

- [ ] **Step 3: Add the entities and `ConfigureCashDrawer(ModelBuilder)`**, called from
  `OnModelCreating`. Keys `Id` (no value generation: `ValueGeneratedNever`); index
  `(StoreId, BusinessDate)` on all four, plus `(StoreId, BusinessDate, CreatedAtUtc)` on counts;
  `Amount` `HasPrecision(18, 2)` (match the existing cloud money columns — check `CloudPayment`).
  Comment wording carries the rules an agent needs:
  - payouts table: "Money taken out of a store's cash drawer (รายจ่าย), one row per entry, mirrored from StoreHub. Rows with IsDeleted are cancelled entries kept for audit; leave them out of totals."
  - floats: "Money put into the drawer as change float (เงินทอน) …" same deleted rule.
  - repayments: "Cash a customer paid to settle PayLater debt (ลูกค้าชำระหนี้) … Not linked to a PayLater row."
  - counts: "Every cash count of a store's drawer (insert-only). The latest row per StoreId+BusinessDate is the count used for the cash difference; earlier rows are audit history."
  - `Category`: "General (เบ็ดเตล็ด) or Hardware; which goods the payout was for."
  - `CreatedByUserId` / `LastModifiedByUserId`: "StoreHub user id (StoreUser.Id). Equals Users.Id only for users migrated from v3."
  - `BusinessDate`: "The store's cash day (Asia/Bangkok calendar date) the entry belongs to."
  - `SyncedAtUtc`: "When the latest version of this row reached the cloud, UTC."
  - each `*Count`: "Number of <n>-baht banknotes/coins counted."

- [ ] **Step 4: Generate the migration.**
  Run: `dotnet ef migrations add AddCashDrawerMirror --project src/IndyPOS.CloudApi`
  Expected: one migration with four `CreateTable` calls and their indexes, nothing else (read it).

- [ ] **Step 5: Run the schema tests, expect PASS.** Same command as Step 2.

- [ ] **Step 6: Commit** — `feat(cloud): mirror tables for the cash drawer, with a comment on every column`

### Task 2: Payouts, floats and repayments upsert by Id

**Files:**
- Create: `tests/IndyPOS.CloudApi.IntegrationTests/EventPipeline.cs`
- Modify: `tests/IndyPOS.CloudApi.IntegrationTests/EventProcessorTests.cs`
- Create: `src/IndyPOS.CloudApi/Infrastructure/CashDrawerMirror.cs`
- Modify: `src/IndyPOS.CloudApi/Infrastructure/EventProcessor.cs`
- Test: `tests/IndyPOS.CloudApi.IntegrationTests/CashDrawerMirrorTests.cs`

**Interfaces:**
- Consumes: Task 1 entities and DbSets; `CashDrawerOutbox.Changed(CashDrawerEntry, DateTime)` (Application) to build payloads in tests.
- Produces:
  ```csharp
  internal static class CashDrawerMirror
  {
      public static Task ProcessPayoutAsync(SyncedEventEntity syncedEvent, CloudDbContext db, CancellationToken ct);
      public static Task ProcessFloatAsync(SyncedEventEntity syncedEvent, CloudDbContext db, CancellationToken ct);
      public static Task ProcessDebtRepaymentAsync(SyncedEventEntity syncedEvent, CloudDbContext db, CancellationToken ct);
  }
  ```
  `EventPipeline` (test helper, `internal sealed`): everything the nested `Pipeline` had, plus
  `Task AddCashEventAsync(OutboxEvent outbox)` (inbox row from `outbox.Type` / `outbox.PayloadJson`,
  new `EventId`) and `Task<T?> FindAsync<T>(Guid id) where T : class`.

- [ ] **Step 1: Move `Pipeline` to `EventPipeline.cs`** (pure move + rename; it takes the fixture
  in `CreateAsync`). Run `dotnet test tests/IndyPOS.CloudApi.IntegrationTests --filter "FullyQualifiedName~EventProcessorTests"` — same count, all pass. Commit `refactor(cloud-tests): the event pipeline harness gets its own file`.

- [ ] **Step 2: Write the failing tests** in `CashDrawerMirrorTests`. Build entities like StoreHub
  does (`StoreId = "1"`, the harness's store), then `pipeline.AddCashEventAsync(CashDrawerOutbox.Changed(entry, now))`,
  `PollAsync()`, assert with `FindAsync`. Named constants: `PayoutAmount = 120m`, `EditedAmount = 150m`,
  `OtherStoreId = "2"`, `T0 = 2026-10-18 03:00 UTC`, `T1 = T0 + 1 min`, `T2 = T0 + 2 min`. Negative first:
  - `…_WithAStaleEdit_KeepsTheNewerAmount` — add at T0 (120), edit at T2 (150), then an edit at T1 (999) → Amount 150.
  - `…_WithAnEditAtTheSameInstant_KeepsTheStoredAmount` — stored at T1 (150), incoming at T1 (999) → 150.
  - `…_WithAStaleEditAfterADelete_KeepsTheRowDeleted` — delete at T2, then edit at T1 → `IsDeleted` true.
  - `…_WithAnEditArrivingBeforeItsAdd_KeepsTheEdit` — edit at T1 (150) polled first, add at T0 (120) → 150.
  - `…_WithARowIdFromAnotherStore_LeavesTheRowUnchanged` — stored under "1"; same Id, `StoreId = OtherStoreId`, newer → Amount unchanged.
  - `…_WithARowIdFromAnotherStore_LeavesTheEventUnprocessed` — `CountUnprocessedAsync()` 1.
  - `…_WithAStaleEdit_MarksItProcessed` — `CountUnprocessedAsync()` 0.
  - `…_WithTimesWithoutAZone_StoresThemAsUtc` — payload JSON with `"CreatedUtc":"2026-10-18T03:00:00"` (hand-written, no `Z`) → row stored, `CreatedAtUtc.Kind` Utc and `== T0`.
  - `…_WithAnEmptyPayload_LeavesItUnprocessed` — payload `"null"` → unprocessed, `FailedAttemptsAsync()` 1.
  - `…_WithANewHardwarePayout_StoresItsCategoryAsText` — `Category == "Hardware"`.
  - `…_WithAnEdit_UpdatesWhoLastChangedIt` — `LastModifiedByUserId` = the editor.
  - `…_WithADelete_RecordsWhenItWasDeleted` — `IsDeleted` true, `DeletedAtUtc` = the delete time.
  - `…_WithANewFloat_StoresItsAmount` and `…_WithANewDebtRepayment_StoresTheCustomerName`.
  (`…` = `ProcessPendingEventsAsync`.)

- [ ] **Step 3: Run, expect FAIL** (events stay unprocessed; rows null).
  Run: `dotnet test tests/IndyPOS.CloudApi.IntegrationTests --filter "FullyQualifiedName~CashDrawerMirrorTests"`

- [ ] **Step 4: Implement `CashDrawerMirror`.** One private generic core:
  `private static async Task UpsertAsync<TCloud>(SyncedEventEntity syncedEvent, DbSet<TCloud> set, TCloud incoming, CloudDbContext db, CancellationToken ct) where TCloud : class, ICloudCashRow`
  — `FindAsync(incoming.Id)`; null → `Add`; different `StoreId` → throw `InvalidOperationException`
  naming the id and both stores; `incoming.LastModifiedAtUtc > existing.LastModifiedAtUtc` →
  `db.Entry(existing).CurrentValues.SetValues(incoming)`; else leave it. Then add
  `ProcessedEvent { EventId = syncedEvent.EventId, EventType = syncedEvent.EventType, StoreId = incoming.StoreId, ProcessedAtUtc = now }`
  and one `SaveChangesAsync`. Each public method deserializes into the Domain type
  (`?? throw` on null), maps to the cloud entity (`SpecifyKind` Utc on every time, `Category.ToString()`,
  `SyncedAtUtc = now`) and calls the core. Class summary says WHY upsert: these rows are mutable,
  unlike `InvoiceCompleted`, and the stale check stops a retry rolling a newer edit back.

- [ ] **Step 5: Wire `EventProcessor`:** add the three types to `HandledEventTypes` (use the
  `CashDrawerOutbox` constants) and three `case`s calling `CashDrawerMirror`.

- [ ] **Step 6: Run `CashDrawerMirrorTests` and `EventProcessorTests`, expect PASS.**
  Run: `dotnet test tests/IndyPOS.CloudApi.IntegrationTests`

- [ ] **Step 7: Commit** — `feat(cloud): mirror cash payouts, floats and debt repayments`

### Task 3: Cash counts, insert-only

**Files:**
- Modify: `src/IndyPOS.CloudApi/Infrastructure/CashDrawerMirror.cs`, `EventProcessor.cs`
- Modify: `tests/IndyPOS.CloudApi.IntegrationTests/EventProcessorTests.cs`
- Test: `tests/IndyPOS.CloudApi.IntegrationTests/CashDrawerMirrorTests.cs`

**Interfaces:**
- Consumes: `UpsertAsync` core (Task 2); `CashDrawerOutbox.CountAdded(CashCount, DateTime)`.
- Produces: `public static Task ProcessCashCountAsync(SyncedEventEntity syncedEvent, CloudDbContext db, CancellationToken ct);`

- [ ] **Step 1: Repoint `EventTypeWithoutAHandler`** in `EventProcessorTests` to `"StockAdjusted"`, with the
  comment "A type the cloud cannot handle yet (stock adjustments send no event today)". Those two tests must still pass — run `--filter "FullyQualifiedName~EventProcessorTests"`.

- [ ] **Step 2: Write the failing tests:**
  - `…_WithTheSameCountTwice_StoresOneRow` — same `CashCount` sent under two inbox ids → `CashCounts` count for that Id is 1.
  - `…_WithTheSameCountTwice_MarksBothProcessed` — unprocessed 0.
  - `…_WithACount_StoresEveryDenomination` — counts 1,2,3,4,5,6,7,8,9 → the nine columns match.
  - `…_WithTwoCountsOnOneDay_KeepsBoth` — two Ids, same `BusinessDate` → 2 rows.

- [ ] **Step 3: Run, expect FAIL.** `--filter "FullyQualifiedName~CashDrawerMirrorTests"`

- [ ] **Step 4: Implement `ProcessCashCountAsync`** through the same core
  (`LastModifiedAtUtc` from the payload's `LastModifiedUtc`, which StoreHub sets equal to `CreatedUtc`; a stored Id is never newer-than-itself, so it is skipped —
  insert-only falls out of the stale rule). Add the type to `HandledEventTypes` and the switch.

- [ ] **Step 5: Run the whole CloudApi integration suite, expect PASS.**
  Run: `dotnet test tests/IndyPOS.CloudApi.IntegrationTests`

- [ ] **Step 6: Commit** — `feat(cloud): mirror cash counts, insert-only`

### Task 4: Release gate, docs and counts

**Files:**
- Modify: `docs/operations/upgrade-procedure.md`, `CLAUDE.md`, `ONBOARDING.md`,
  `docs/superpowers/specs/2026-09-20-cash-payout-float-persistence-design.md`

- [ ] **Step 1: Run the cloud forward-only gate** (`upgrade-procedure.md` cloud recipe): update to
  `20261002102910_AddSyncedEventSourceStore`, snapshot, update to `AddCashDrawerMirror`, diff
  (expect only four new tables + indexes), then INSERT a `"SyncedEvents"` row and mark it processed
  using only the previous release's columns. Record the result in the doc's changelog table and add
  `AddCashDrawerMirror` to CLAUDE.md's verified-migrations sentence.

- [ ] **Step 2: Fix the spec's stale lines** (no behaviour change): §5 line "The cash formula is
  unchanged" → points at 2026-10-07 §4.3 (Cash kept, D1); §7 "Handled separately" → both items done
  (unknown types stay pending; no bare transaction), so deploy order no longer matters; §7 user
  ids → "verified 2026-10-18: equal only for migrated users; mirrored as sent".

- [ ] **Step 3: Run the full solution with TRX, nothing else running**, then update CLAUDE.md and
  ONBOARDING.md counts (CloudApi.IntegrationTests grows; its Docker-down split equals its total).
  Run: `dotnet test --logger trx --results-directory <scratchpad>/trx`

- [ ] **Step 4: Commit** — `docs: cloud cash mirror gate, spec notes and counts`; then PR to
  `development`, Codex review, `gh pr merge --auto --merge`.
