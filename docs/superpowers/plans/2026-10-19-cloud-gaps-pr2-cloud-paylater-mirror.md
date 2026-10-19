# Cloud Gaps PR 2 — Cloud Mirror of ลงบัญชี Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The cloud keeps a queryable, self-describing copy of every ลงบัญชี debt and every repayment taken
on the ลงบัญชี screen, built from the `PayLaterChanged` and `PayLaterRepaid` events StoreHub sends since PR 1.

**Architecture:** Two mirror tables in `CloudDbContext` (`PayLaters`, `PayLaterRepayments`), configured in a
new `PayLaterMirrorModel` with a comment on every table and column. A new static `PayLaterMirror` turns each
event into a row: a debt is an upsert by `Id` where the newest **or equal** `LastModifiedAtUtc` wins; a
repayment is insert-only and waits (throws, so it is retried) until its debt has arrived. `EventProcessor`
adds both types to its allow-list and switch. Nothing changes on StoreHub.

**Tech Stack:** C# .NET 10, EF Core 10 + Npgsql (cloud), xUnit + FluentAssertions, Testcontainers
Postgres (`CloudPostgresFixture`, `EventPipeline`).

**Spec:** `docs/superpowers/specs/2026-10-19-cloud-paylater-and-drawer-balance-design.md` §3 ("Sync events",
"Cloud mirror", "v3 debts"). **PR 2 of 3:** PR 1 (#139, StoreHub events) is merged; PR 3 = `BusinessDate`
on `InvoiceCompleted` + the `CashDrawerDays` view.

## Already true (surveyed 2026-10-19, so no task)

- StoreHub writes both events through `PayLaterOutbox` (`Application/UseCases/StoreHub/PayLater/PayLaterOutbox.cs`).
  Payloads are the records `PayLaterChangedPayload(Id, StoreId, PaymentId, InvoiceId, Description,
  PayLaterAmount, PaidAmount, IsCompleted, LegacyPaymentId, CreatedUtc, LastModifiedUtc)` and
  `PayLaterRepaidPayload(Id, StoreId, PayLaterId, Amount, BusinessDate, CreatedUtc, CreatedByUserId)`,
  default `System.Text.Json`. **No event id in the payload**: the inbox row's `EventId` is the key.
- Unknown types wait in `SyncedEvents` (`GetUnprocessedAsync(HandledEventTypes, …)`), so every event PR 1
  has queued is processed once this ships, in any deploy order.
- A thrown handler gets the inbox retry backoff; `EventPipeline.ElapseRetryDelaysAsync()` skips the wait in tests.

## Global Constraints

- **A debt is an upsert by `Id`; newest or equal wins:** apply the incoming snapshot when
  `incoming.LastModifiedAtUtc >= stored.LastModifiedAtUtc` (spec §3: "`>=`, not `>`", so a re-migrated
  v3 debt, which keeps its id but gets a new invoice and payment, points at the new ones). Older → skipped.
- **A repayment is insert-only**, once per `Id`: a stored `Id` is skipped, whatever the payload says.
- **A repayment waits for its debt:** no `PayLaters` row with that `PayLaterId` **and** that `StoreId` →
  throw `InvalidOperationException` (event stays unprocessed and is retried), as `InvoiceReprinted` does.
- **A row never changes store:** a debt `Id` stored under another `StoreId` → throw (same as D2).
- **A skipped event is still marked processed:** `ProcessedEvent { EventId = syncedEvent.EventId, … }` in
  the same `SaveChangesAsync` as the row.
- **Deserialize into the PR 1 payload records**, so the contract cannot drift.
- **No foreign key from `PayLaters` to `Invoices`/`Payments`:** a v3 debt's invoice reaches the cloud only
  through the best-effort bulk upload, and re-migration changes those ids. `PayLaterRepayments.PayLaterId`
  **does** reference `PayLaters.Id` (`ON DELETE RESTRICT`), since a repayment is only inserted after its debt.
- **Readable by AI agents:** table names `PayLaters`, `PayLaterRepayments`; money `decimal(18,2)` baht;
  instants `…AtUtc` (`CreatedAtUtc`, `LastModifiedAtUtc`, `SyncedAtUtc`); `BusinessDate` `date`;
  `StoreId` `HasMaxLength(50)`; the customer column is `CustomerName` (StoreHub's `Description`).
  **Every table and every column has a `HasComment`.** Derived figures are stored generated columns,
  never sent: `OwedAmount = PayLaterAmount - PaidAmount` and `IsFromV3 = LegacyPaymentId IS NOT NULL`.
- **Not the drawer's `DebtRepayments`:** both new table comments say so, so an agent never adds the two.
- **User ids join `Users.Id`** (`CreatedByUserId`, as D2's comment words it). A debt has no user column
  (the sale's invoice has one).
- **Times are UTC on write** (`DateTime.SpecifyKind(x, DateTimeKind.Utc)`).
- **One additive cloud migration** `AddPayLaterMirror`, passing the cloud forward-only gate.
- **Test naming** `Subject_WhenScenario_DirectVerbOutcome` with `When`/`With`, never `Should`; one behaviour
  per test; negative first; named constants; AAA blank lines; dots vertically aligned. Docker running.

## Review Focus

1. **A stale debt snapshot after a newer one** (retry, out-of-order) must not roll `PaidAmount` back. →
   Task 2 `ProcessPendingEventsAsync_WithAStaleDebtSnapshot_KeepsTheNewerPaidAmount`.
2. **A repayment whose debt is another store's** must not be stored for either store. → Task 3
   `ProcessPendingEventsAsync_WithARepaymentForAnotherStoresDebt_LeavesItUnprocessed`.
3. **The same repayment delivered twice** (two inbox ids) must be one row. → Task 3
   `ProcessPendingEventsAsync_WithTheSameRepaymentTwice_StoresOneRow`.
4. **A v3 debt later repaid on v4** must stay marked from v3. → Task 2
   `ProcessPendingEventsAsync_WithAV3DebtRepaidOnV4_KeepsItMarkedFromV3`.
5. **A re-migrated v3 debt** (same id, same timestamp, new invoice) must point at the new invoice. → Task 2
   `ProcessPendingEventsAsync_WithAnEqualTimestamp_TakesTheNewInvoiceId`.

---

## File Structure

```
src/IndyPOS.CloudApi/Domain/
  CloudPayLater.cs                    CREATE  CloudPayLater + CloudPayLaterRepayment
src/IndyPOS.CloudApi/Infrastructure/
  PayLaterMirrorModel.cs              CREATE  table/column config + comments
  CloudDbContext.cs                   MODIFY  2 DbSets + PayLaterMirrorModel.Configure(modelBuilder)
  PayLaterMirror.cs                   CREATE  ProcessChangedAsync / ProcessRepaidAsync
  EventProcessor.cs                   MODIFY  HandledEventTypes + 2 switch cases
  Migrations/<ts>_AddPayLaterMirror.cs  CREATE (dotnet ef)
tests/IndyPOS.CloudApi.IntegrationTests/
  PayLaterMirrorSchemaTests.cs        CREATE
  PayLaterMirrorTests.cs              CREATE
  EventPipeline.cs                    MODIFY  AddCashEventAsync → AddOutboxEventAsync (rename only)
docs/operations/upgrade-procedure.md, CLAUDE.md, ONBOARDING.md   MODIFY
```

`PayLaterMirror` has its own small upsert rather than reusing `CashDrawerMirror.UpsertAsync`: the tie rule
differs (`>=` against `>`), and a shared core with a flag would change D2's code for no D2 benefit.

---

### Task 1: Mirror tables that explain themselves

**Files:** Create `src/IndyPOS.CloudApi/Domain/CloudPayLater.cs`, `src/IndyPOS.CloudApi/Infrastructure/PayLaterMirrorModel.cs`,
the migration (generated); modify `CloudDbContext.cs`; test `PayLaterMirrorSchemaTests.cs`.

**Interfaces — Produces:**
```csharp
public class CloudPayLater          // Id, StoreId, InvoiceId, PaymentId, CustomerName, PayLaterAmount, PaidAmount,
                                    // OwedAmount (generated), IsCompleted, LegacyPaymentId (int?), IsFromV3 (generated),
                                    // CreatedAtUtc, LastModifiedAtUtc, SyncedAtUtc
public class CloudPayLaterRepayment // Id, StoreId, PayLaterId, Amount, BusinessDate (DateOnly), CreatedAtUtc,
                                    // CreatedByUserId, SyncedAtUtc
// CloudDbContext.PayLaters, CloudDbContext.PayLaterRepayments
```

- [ ] **Step 1: Failing tests** in `PayLaterMirrorSchemaTests` (copy `CashDrawerMirrorSchemaTests`' fixture and
  queries; `PayLaterTables = { "PayLaters", "PayLaterRepayments" }`):
  `PayLaterMirror_ForEveryColumn_HasAComment` and `PayLaterMirror_ForEveryTable_HasAComment` (`[Theory]`),
  `PayLaterMirror_WithAPartPaidDebt_ComputesOwedAmount` (insert 700 / 299 through the context, read back
  `OwedAmount` 401), `PayLaterMirror_WithALegacyPaymentId_ComputesIsFromV3` (true), and
  `PayLaterMirror_WithoutALegacyPaymentId_ComputesIsFromV3False`.
- [ ] **Step 2: FAIL** — `dotnet test tests/IndyPOS.CloudApi.IntegrationTests --filter "FullyQualifiedName~PayLaterMirrorSchemaTests"`
  (tables do not exist).
- [ ] **Step 3: Implement.** Keys `ValueGeneratedNever`; generated columns via
  `HasComputedColumnSql("\"PayLaterAmount\" - \"PaidAmount\"", stored: true)` and
  `HasComputedColumnSql("\"LegacyPaymentId\" IS NOT NULL", stored: true)`; indexes `(StoreId, IsCompleted)` on
  debts, `(StoreId, BusinessDate)` and `PayLaterId` on repayments; FK repayment → debt `Restrict`.
  Comment wording carries the rules:
  - `PayLaters`: "Every ลงบัญชี (PayLater) debt of a store, one row per credit sale, mirrored from StoreHub with its latest totals. v3 debts are included (IsFromV3). Not the cash drawer's DebtRepayments."
  - `PayLaterRepayments`: "Every repayment taken on a store's ลงบัญชี screen (insert-only audit), with the cashier. Sum per PayLaterId for v4 repayments; v3 debts carry no repayment history, only PaidAmount. Not the cash drawer's DebtRepayments, which record cash entering the drawer."
  - `InvoiceId` / `PaymentId`: "The credit sale (Invoices.Id) / its PayLater payment. A v3 debt's invoice may be missing from Invoices if its bulk upload was skipped."
  - `CustomerName`: "Who owes, as the cashier typed it at the sale."
  - `OwedAmount`: "PayLaterAmount − PaidAmount, computed by the database. Negative if overpaid."
  - `LegacyPaymentId`: "The v3 PaymentId of a debt migrated from v3; null for a v4 debt."
  - `BusinessDate`: "The store's day (Asia/Bangkok calendar date) the repayment was taken."
  - `CreatedByUserId`: D2's wording ("The user, as Users.Id. A store's own seeded admin has no Users row.").
- [ ] **Step 4: Migration** — `dotnet ef migrations add AddPayLaterMirror --project src/IndyPOS.CloudApi`;
  read it: two `CreateTable`s and their indexes, nothing else.
- [ ] **Step 5: PASS** (Step 2's command). **Step 6: Commit** — `feat(cloud): mirror tables for ลงบัญชี, with a comment on every column`

### Task 2: A debt is an upsert, newest or equal wins

**Files:** Create `src/IndyPOS.CloudApi/Infrastructure/PayLaterMirror.cs`; modify `EventProcessor.cs`,
`EventPipeline.cs` (rename `AddCashEventAsync` → `AddOutboxEventAsync`, updating `CashDrawerMirrorTests`);
test `PayLaterMirrorTests.cs`.

**Interfaces:** Consumes Task 1; `PayLaterOutbox.Changed(PayLater, string, DateTime)`. Produces
`internal static class PayLaterMirror { public static Task ProcessChangedAsync(SyncedEventEntity syncedEvent, CloudDbContext db, CancellationToken ct); }`.

- [ ] **Step 1: Rename** the harness method; run `--filter "FullyQualifiedName~CashDrawerMirrorTests"`, all pass.
- [ ] **Step 2: Failing tests.** Build a `PayLater` entity (`StoreId` "1" = the harness store, via
  `PayLaterOutbox.Changed(debt, "1", now)`), `AddOutboxEventAsync`, `PollAsync`, `FindAsync<CloudPayLater>`.
  Constants `DebtAmount = 700m`, `PartPaid = 299m`, `OtherStoreId = "2"`, `V3PaymentId = 4242`, `T0` =
  2026-10-19 03:00 UTC, `T1 = T0 + 1 min`, `T2 = T0 + 2 min`. Negative first (`…` = `ProcessPendingEventsAsync`):
  - `…_WithAStaleDebtSnapshot_KeepsTheNewerPaidAmount` — T0 paid 0, T2 paid 299, then T1 paid 100 → 299.
  - `…_WithAStaleDebtSnapshot_MarksItProcessed` — `CountUnprocessedAsync()` 0.
  - `…_WithADebtIdFromAnotherStore_LeavesTheDebtUnchanged` — same Id, `StoreId` "2", newer → PaidAmount unchanged.
  - `…_WithADebtIdFromAnotherStore_LeavesTheEventUnprocessed` — 1 unprocessed.
  - `…_WithAnEmptyDebtPayload_LeavesItUnprocessed` — payload `"null"` → unprocessed, `FailedAttemptsAsync()` 1.
  - `…_WithAnEqualTimestamp_TakesTheNewInvoiceId` — two snapshots at T1, second with a new `InvoiceId` → the new one.
  - `…_WithANewDebt_StoresTheCustomerName` — `CustomerName` = the `Description`.
  - `…_WithADebtWhoseInvoiceIsNotInTheCloud_StoresIt` — no `Invoices` row (a v3 store whose bulk upload was
    skipped, spec §5) → the debt row exists.
  - `…_WithANewerSnapshot_CompletesTheDebt` — T2 paid 700 completed → `IsCompleted` true, `OwedAmount` 0.
  - `…_WithAV3Debt_MarksItFromV3` — `LegacyPaymentId` 4242 → `IsFromV3` true.
  - `…_WithAV3DebtRepaidOnV4_KeepsItMarkedFromV3` — v3 snapshot at T0, then T1 with paid 299 and the same
    `LegacyPaymentId` (as StoreHub sends it) → `IsFromV3` true, `PaidAmount` 299.
- [ ] **Step 3: FAIL** — `--filter "FullyQualifiedName~PayLaterMirrorTests"` (events stay unprocessed).
- [ ] **Step 4: Implement `ProcessChangedAsync`** per Global Constraints (find by Id → add / throw on another
  store / `SetValues` when `>=` / skip; then `ProcessedEvent` + one save). Add `PayLaterOutbox.PayLaterChanged`
  to `HandledEventTypes` and the switch. Class summary says WHY `>=`: re-migration keeps the debt id but
  not its invoice id, with the same timestamp (spec §3).
- [ ] **Step 5: PASS** — `dotnet test tests/IndyPOS.CloudApi.IntegrationTests`. **Step 6: Commit** —
  `feat(cloud): mirror ลงบัญชี debts, the newest or equal snapshot wins`

### Task 3: A repayment is insert-only and waits for its debt

**Files:** modify `PayLaterMirror.cs`, `EventProcessor.cs`; test `PayLaterMirrorTests.cs`.

**Interfaces:** Consumes Task 2; `PayLaterOutbox.Repaid(PayLaterRepayment, DateTime)`. Produces
`public static Task ProcessRepaidAsync(SyncedEventEntity syncedEvent, CloudDbContext db, CancellationToken ct);`.

- [ ] **Step 1: Failing tests** (constants `RepaidAmount = 100m`, `Cashier` a fixed Guid, `Today` 2026-10-19):
  - `…_WithARepaymentBeforeItsDebt_LeavesItUnprocessed` — no debt yet → 1 unprocessed, no row.
  - `…_WithARepaymentBeforeItsDebt_StoresItOnceTheDebtArrives` — then the debt event, `ElapseRetryDelaysAsync`,
    `PollAsync` → the row exists.
  - `…_WithARepaymentForAnotherStoresDebt_LeavesItUnprocessed` — debt stored under "1"; repayment with
    `StoreId` "2" naming it → unprocessed, no row.
  - `…_WithTheSameRepaymentTwice_StoresOneRow` and `…_WithTheSameRepaymentTwice_MarksBothProcessed`.
  - `…_WithARepayment_RecordsTheCashier` (`CreatedByUserId`) and `…_WithARepayment_StoresItsBusinessDate`.
- [ ] **Step 2: FAIL.** **Step 3: Implement** per Global Constraints; add `PayLaterOutbox.PayLaterRepaid` to
  `HandledEventTypes` and the switch. The throw's message names the repayment, the debt and the store, and
  says it is retried (as the reprint one does).
- [ ] **Step 4: PASS** — `dotnet test tests/IndyPOS.CloudApi.IntegrationTests`. **Step 5: Commit** —
  `feat(cloud): mirror ลงบัญชี repayments, once each, after their debt`

### Task 4: Release gate, docs, counts, PR

- [ ] **Cloud forward-only gate** (`upgrade-procedure.md` cloud recipe, bash `env` form): update to
  `20261019051410_AddCashDrawerMirror`, snapshot columns + `pg_indexes`, update to `AddPayLaterMirror`, diff
  (expect only the two new tables, their indexes and the FK), then INSERT a `"SyncedEvents"` row and mark it
  processed naming only the previous release's columns. Record the result and changelog row; add
  `AddPayLaterMirror` to CLAUDE.md's verified-migrations sentence.
- [ ] **Full solution with TRX**, nothing else running; update CLAUDE.md / ONBOARDING.md counts
  (CloudApi.IntegrationTests grows; its Docker-down split equals its total).
- [ ] **Commit** `docs: …`; PR to `development`; Codex; merge when findings are handled and CI is green
  (PR head == local HEAD).
