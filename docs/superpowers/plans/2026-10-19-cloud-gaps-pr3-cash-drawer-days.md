# Cloud Gaps PR 3 — The Daily Drawer Balance in the Cloud Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The cloud answers "did the cash count match the drawer (ลงตัวมั้ย) today, and how has it trended?" per
store per day, from rows it already has, with the same figures the till shows.

**Architecture:** StoreHub stamps each sale's store `BusinessDate` on `InvoiceCompleted` (from the store clock,
so the cloud never needs a time zone). The cloud stores it on `Invoices`. A Postgres view, `CashDrawerDays`,
derives cash kept, expected cash, the latest count and the difference per `(StoreId, BusinessDate)` with the
till's rules. The till's cash-kept rule becomes one public function both sides' tests use, so they cannot drift.

**Tech Stack:** C# .NET 10, EF Core 10 + Npgsql, xUnit + FluentAssertions + Moq/AutoFixture, Testcontainers
Postgres (`CloudPostgresFixture`, `EventPipeline`).

**Spec:** `docs/superpowers/specs/2026-10-19-cloud-paylater-and-drawer-balance-design.md` §2 goal A, §3 "The
day" and "The drawer balance", §5 tests. **PR 3 of 3:** PR 1 (#139) and PR 2 (#140) are merged.

## Already true (surveyed 2026-10-19, so no task)

- The till's formula: `CashDrawerCalculator.ExpectedCash(CashDrawerTotals)` = cash kept + debt repaid (drawer
  `DebtRepayments`) + floats − payouts; `Difference(counted, expected)` = counted − expected, null when uncounted.
- Cash kept, today a private `CashKept` in `GetCashDrawerSummaryQueryHandler`: per invoice, its Cash-method
  payments (case-insensitive) minus `SaleFigures.From(total, payments).ChangeGiven` (payments − total, never
  below 0, and 0 for a refund).
- `CashDrawerClock` (`ICashDrawerClock.Now()` → `CashDrawerInstant(Utc, BusinessDate)`) already decides the store
  day, with its Bangkok-midnight tests (`CashDrawerClockTests`).
- The cloud already mirrors every drawer row (D2): `CashPayouts`, `CashFloats`, `DebtRepayments` (with
  `IsDeleted`) and `CashCounts` (nine denomination counts, `CreatedAtUtc`), all keyed by `(StoreId, BusinessDate)`.

## Global Constraints

- **One source for the day: the store.** `InvoiceCompleted.BusinessDate` comes from `ICashDrawerClock`; the
  cloud copies it and never converts a time zone. A sale with no `BusinessDate` (dev, or before this field)
  **falls back to its UTC date** (spec §3).
- **The same rules as the till** (spec §3): cash kept = Σ per invoice of (Cash payments − change), change =
  max(Σ payments − total, 0) and 0 for a refund (total < 0); expected = cash kept + drawer debt repaid +
  floats − payouts, **non-deleted rows only**; counted = the **latest** count of the day (by `CreatedAtUtc`);
  difference = counted − expected, **null until counted**. ลงบัญชี repayments (`PayLaterRepayments`) are not
  drawer cash and are not in the view.
- **A day appears if anything happened on it**: a sale, a payout, a float, a drawer repayment or a count.
- **Derived, never stored** (cash spec §2): the view holds no copied figures; nothing is sent twice.
- **Readable by AI agents:** view `CashDrawerDays`, columns `StoreId`, `BusinessDate`, `CashSalesTotal`,
  `DebtRepaidTotal`, `CashFloatsTotal`, `PayoutsTotal`, `ExpectedCash`, `CountedCash`, `CashDifference`,
  `LatestCountAtUtc`; `COMMENT ON VIEW` and `COMMENT ON COLUMN` for every column; money `numeric(18,2)` baht.
- **Forward-only, cloud:** `AddInvoiceBusinessDate` (one nullable column + index) and `AddCashDrawerDaysView`
  (`CREATE VIEW` in `migrationBuilder.Sql`, `DROP VIEW` in `Down`). The view comment notes that a later
  migration altering a column the view reads must drop and recreate the view (Postgres refuses otherwise).
  No StoreHub migration in this PR (the field is in the event payload only).
- **Additive event field:** `public DateOnly? BusinessDate { get; init; }` on `InvoiceCompletedEvent`;
  `SchemaVersion` stays 1 (old cloud binaries ignore the extra key).
- **Test naming** `Subject_WhenScenario_DirectVerbOutcome` with `When`/`With`, never `Should`; one behaviour per
  test; negative first; named constants; AAA blank lines; dots vertically aligned. Docker running.

## Review Focus

1. **A sale just after Bangkok midnight** (00:30 Bangkok = 17:30 UTC the day before) must count on its Bangkok
   day, not its UTC date. → Task 3 `CashDrawerDays_WithASaleJustAfterBangkokMidnight_CountsItOnItsBusinessDate`.
2. **A refund** keeps no change and its negative cash comes off the day. → Task 3 `CashDrawerDays_WithARefund_TakesItsCashOff`.
3. **A deleted payout, float or repayment** must drop out of expected cash. → Task 3
   `CashDrawerDays_WithADeletedPayout_LeavesItOut`.
4. **A day with no sales but a float** must still show. → Task 3 `CashDrawerDays_WithOnlyAFloat_ShowsTheDay`.
5. **Another store's rows on the same day** must not mix in. → Task 3 `CashDrawerDays_WithAnotherStoresRows_KeepsThemApart`.

---

## File Structure

```
src/IndyPOS.Application/UseCases/
  Cloud/Sync/Events/InvoiceCompletedEvent.cs                     MODIFY  + BusinessDate
  StoreHub/Sales/Complete/CompleteSaleCommandHandler.cs          MODIFY  ICashDrawerClock replaces DateTime.UtcNow
  StoreHub/CashDrawer/Summary/CashDrawerCalculator.cs            MODIFY  + CashKept (moved from the summary handler)
src/IndyPOS.Infrastructure/QueryHandlers/CashDrawer/GetCashDrawerSummaryQueryHandler.cs   MODIFY  uses CashKept
src/IndyPOS.CloudApi/Domain/CloudInvoice.cs                      MODIFY  + BusinessDate
src/IndyPOS.CloudApi/Infrastructure/CloudDbContext.cs, EventProcessor.cs                 MODIFY
src/IndyPOS.CloudApi/Infrastructure/Migrations/<ts>_AddInvoiceBusinessDate.cs, <ts>_AddCashDrawerDaysView.cs  CREATE
tests/IndyPOS.Application.Tests/…/CompleteSaleCommandHandlerTests.cs, CompleteSaleInvoiceNumberTests.cs,
  UseCases/StoreHub/CashDrawer/Summary/CashDrawerCalculatorTests.cs   MODIFY
tests/IndyPOS.CloudApi.IntegrationTests/EventPipeline.cs, EventProcessorTests.cs          MODIFY
tests/IndyPOS.CloudApi.IntegrationTests/CashDrawerDaysTests.cs   CREATE
docs/operations/upgrade-procedure.md, CLAUDE.md, ONBOARDING.md, the spec §3   MODIFY
```

---

### Task 1: A sale carries its store day

**Files:** modify `InvoiceCompletedEvent.cs`, `CompleteSaleCommandHandler.cs`; tests in
`CompleteSaleCommandHandlerTests.cs` (and fix the hand-built handler in `CompleteSaleInvoiceNumberTests.cs`).

**Interfaces — Produces:** `InvoiceCompletedEvent.BusinessDate` (`DateOnly?`); `CompleteSaleCommandHandler` gains
an `ICashDrawerClock clock` constructor parameter (last), and every instant the handler stamps (invoice, lines,
payments, debt, movements, outbox) is `clock.Now().Utc`, read once.

- [ ] **Step 1: Failing tests** (Frozen `Mock<ICashDrawerClock>` returning `new CashDrawerInstant(HalfPastMidnightUtc,
  BangkokDay)` with `HalfPastMidnightUtc = 2026-10-19 17:30 UTC`, `BangkokDay = 2026-10-20`; capture the outbox
  list as the existing tests do):
  - `HandleAsync_WithASale_PutsTheStoresBusinessDateOnTheEvent` — payload `BusinessDate == BangkokDay`
    (deserialize `InvoiceCompletedEvent`; not the UTC date 2026-10-19).
  - `HandleAsync_WithASale_StampsTheClocksInstant` — the saved invoice's `CreatedUtc == HalfPastMidnightUtc`.
- [ ] **Step 2: FAIL** — `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~CompleteSale"`
  (compile error on `BusinessDate`, then the instant assertion).
- [ ] **Step 3: Implement.** XML doc on the field: "The store's day the sale belongs to (its time zone, midnight
  boundary), decided by StoreHub so the cloud never converts a time. Null for an event queued before this field."
  `CompleteSaleInvoiceNumberTests.Handler()` passes a `Mock<ICashDrawerClock>`.
- [ ] **Step 4: PASS** — `dotnet test tests/IndyPOS.Application.Tests`, then `dotnet build`.
  **Step 5: Commit** — `feat(storehub): a completed sale carries the store's business date`

### Task 2: The cloud keeps the sale's day

**Files:** modify `CloudInvoice.cs`, `CloudDbContext.cs`, `EventProcessor.cs`, `EventPipeline.cs`,
`EventProcessorTests.cs`; create the migration.

**Interfaces — Produces:** `CloudInvoice.BusinessDate` (`DateOnly?`), column `BusinessDate date NULL`, index
`(StoreId, BusinessDate)`; `EventPipeline.AddInvoiceCompletedAsync(..., DateOnly? businessDate = null,
decimal totalAmount = 35m, IReadOnlyList<PaymentSnapshot>? payments = null, DateTime? createdAtUtc = null)` —
omitting `businessDate` sends no key at all (as an old till did), like `invoiceNumber`.

- [ ] **Step 1: Failing tests:** `ProcessPendingEventsAsync_WithABusinessDate_StoresIt` and
  `ProcessPendingEventsAsync_WithoutABusinessDate_StoresNull` (`FindInvoiceAsync(...).BusinessDate`).
- [ ] **Step 2: FAIL** (`--filter "FullyQualifiedName~EventProcessorTests"`). **Step 3: Implement**; comment:
  "The store's day the sale belongs to (Asia/Bangkok calendar date), as StoreHub decided it. NULL for a sale
  synced before this field or bulk-migrated from v3; CashDrawerDays then uses its UTC date."
  Migration: `dotnet ef migrations add AddInvoiceBusinessDate --project src/IndyPOS.CloudApi` — read it: one
  `AddColumn` (nullable) and one `CreateIndex`, nothing else.
- [ ] **Step 4: PASS** — `dotnet test tests/IndyPOS.CloudApi.IntegrationTests`. **Step 5: Commit** —
  `feat(cloud): invoices keep the store's business date`

### Task 3: `CashDrawerDays`, the till's figures per store per day

**Files:** modify `CashDrawerCalculator.cs`, `GetCashDrawerSummaryQueryHandler.cs`, `CashDrawerCalculatorTests.cs`;
create the view migration and `CashDrawerDaysTests.cs`.

**Interfaces — Produces:** `public static decimal CashKept(decimal totalAmount, IReadOnlyList<SalePayment> payments)`
on `CashDrawerCalculator` (the handler's private rule, moved; the handler calls it); view `CashDrawerDays`.

- [ ] **Step 1: Move `CashKept`** with failing unit tests first in `CashDrawerCalculatorTests`:
  `CashKept_WithChange_SubtractsIt` (total 80, Cash 100 → 80), `CashKept_WithAMixedBill_SubtractsTheChangeFromCash`
  (total 100, MoneyTransfer 60 + Cash 50 → 40), `CashKept_WithARefund_KeepsNoChange` (total −100, Cash −100 → −100),
  `CashKept_WithALowerCaseCashCode_CountsIt`. FAIL (no method) → move → PASS; the D1 summary tests stay green.
- [ ] **Step 2: Failing view tests** in `CashDrawerDaysTests` (fixture + `EventPipeline`; feed sales with
  `AddInvoiceCompletedAsync` and drawer rows with `AddOutboxEventAsync(CashDrawerOutbox.…)`, `PollAsync`, then read
  the view with Npgsql). Constants `Store = "1"`, `OtherStore = "2"`, `Day = 2026-10-19`. Negative first:
  - `CashDrawerDays_WithNoCount_HasNoDifference` — `CountedCash` and `CashDifference` null.
  - `CashDrawerDays_WithARefund_TakesItsCashOff` — a 100 sale and a −100 refund → `CashSalesTotal` 0.
  - `CashDrawerDays_WithADeletedPayout_LeavesItOut` — `PayoutsTotal` 0.
  - `CashDrawerDays_WithAPayLaterSale_KeepsNoCash` — `CashSalesTotal` 0.
  - `CashDrawerDays_WithAnotherStoresRows_KeepsThemApart` — store "1" figures ignore store "2"'s sale.
  - `CashDrawerDays_WithASaleJustAfterBangkokMidnight_CountsItOnItsBusinessDate` — `CreatedAtUtc` 2026-10-19
    17:30 UTC, `BusinessDate` 2026-10-20 → a row for 2026-10-20 and none for 2026-10-19.
  - `CashDrawerDays_WithASaleWithoutABusinessDate_FallsBackToItsUtcDate`.
  - `CashDrawerDays_WithOnlyAFloat_ShowsTheDay` — `CashSalesTotal` 0, `CashFloatsTotal` the float.
  - `CashDrawerDays_WithTwoCounts_UsesTheLatest` — counted = the later count's total; `LatestCountAtUtc` its time.
  - `CashDrawerDays_WithACountShort_ShowsANegativeDifference`.
  - `CashDrawerDays_ForADay_MatchesTheTillsExpectedCash` — **the parity test**: one day with a change sale, a mixed
    bill, a refund, a PayLater sale, a drawer repayment, a float, a payout, a deleted payout and two counts. The
    expected figures are computed in the test **with the till's own functions** (`CashDrawerCalculator.CashKept`,
    `ExpectedCash`, `Difference`, `CashCount.CountedTotal`); assert `ExpectedCash` and `CashDifference` equal them.
  - `CashDrawerDays_ForEveryColumn_HasAComment` and `CashDrawerDays_ForTheView_HasAComment`.
- [ ] **Step 3: FAIL** (`--filter "FullyQualifiedName~CashDrawerDaysTests"`: the view does not exist).
- [ ] **Step 4: Implement the view** in `dotnet ef migrations add AddCashDrawerDaysView --project src/IndyPOS.CloudApi`
  (empty model diff; write `Up`/`Down` by hand) per Global Constraints. The SQL is the builder's; one CTE per
  source is the obvious shape. View comment: "One row per store per day that had a sale or drawer entry: the
  till's expected cash, the latest count and the difference (ขาด < 0, เกิน > 0, พอดี = 0). Derived live from
  Invoices, Payments, CashPayouts, CashFloats, DebtRepayments and CashCounts with the till's rules; never edited."
  `CashDifference` comment names the three Thai words; `CountedCash` says "null until the day is counted".
- [ ] **Step 5: PASS** — `dotnet test tests/IndyPOS.CloudApi.IntegrationTests` and `dotnet test tests/IndyPOS.Application.Tests`.
  **Step 6: Commit** — `feat(cloud): CashDrawerDays, the till's drawer balance per store per day`

### Task 4: Release gate, docs, counts, PR

- [ ] **Cloud forward-only gate** for both migrations (from `20261019134046_AddPayLaterMirror`): diff columns,
  `pg_indexes` and views; then, naming only the previous release's columns, INSERT an `Invoices` row (its
  `BusinessDate` takes NULL and the view still reads it) and a `SyncedEvents` row marked processed. Record it;
  add both migrations to CLAUDE.md's verified sentence.
- [ ] **Spec §3:** note the view's name and that it is live (no refresh), and that v3 bulk-migrated sales use
  their UTC date (store hours 07:00–20:00 Bangkok fall on the same UTC date).
- [ ] **Full solution with TRX**; update CLAUDE.md / ONBOARDING.md counts.
- [ ] **Commit** `docs: …`; PR to `development`; Codex; merge when findings are handled and CI is green
  (PR head == local HEAD).
