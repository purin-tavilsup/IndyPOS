# Cash Drawer D1 — Expected Cash Counts Only Cash — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `GET /cash/summary` works out expected cash from **Cash-method payments only**
(`+ debt repaid + floats − payouts`), and returns one line per non-cash method so the till can show
them without doing any maths.

**Architecture:** The pure formula in `CashDrawerCalculator` drops its transfer/welfare subtraction
and takes the Cash-method total instead. `GetCashDrawerSummaryQueryHandler` reads the day's
`Payments` and the store's `PaymentMethods` catalogue itself, so it no longer needs the legacy
payments summary, and reuses `PaymentMethodTotals.Build` (from per-store UI slice 2) for the
per-method lines. No migration, no new route.

**Tech Stack:** C# .NET 10, EF Core 10 (InMemory for unit tests, Testcontainers Postgres for
integration), xUnit + FluentAssertions + Moq.

**Spec:** `docs/superpowers/specs/2026-10-07-per-store-ui-design.md` §2 related rules and §4.3
(expected cash), plus `docs/superpowers/specs/2026-09-20-cash-payout-float-persistence-design.md` §5
(the summary query does all the maths). This is **PR D1 of 4** for the cash drawer on the till:
D2 = cloud mirror (cash plan 2), D3 = till client + `CashDrawerViewModel`, D4 = WinForms
ลิ้นชักเก็บเงิน section (cash plan 3 + slice 3 UI). Each later plan is written after the previous PR merges.

## Global Constraints

- **Branch:** `fix/cash-expected-cash-only` from `development` (`845cb0a` or later).
- **Expected cash = Cash-method payments + debt repaid + floats − payouts** (spec §4.3). "Count the
  Cash method directly, so no future campaign can break it": no list of non-cash codes to subtract.
- **The Cash code matches case-insensitively** (`PaymentMethodCodes.Cash`, `OrdinalIgnoreCase`): a
  sale stores the caller's spelling, so `"cash"` is Cash (same rule as `PaymentMethodTotals`).
- **Non-cash lines = `PaymentMethodTotals.Build(catalogue, payments)` minus `Cash` and `PayLater`.**
  PayLater keeps its own general/hardware totals and debt lists (spec §2: ลงบัญชี is
  `PayLaterEnabled`-gated, not a non-cash tender line). A method shows when enabled or used that day.
- **The day's payments** = `Payments` whose `CreatedUtc` falls in
  `ReportDateRange.ToUtcRange(day, day, storeIdentity.TimeZone)` — the window the legacy payments
  summary already uses for this screen.
- **DTO change, decided:** `CashDrawerSummaryDto` **drops** `CashSalesTotal`, `MoneyTransferTotal` and
  `WelfareCardTotal` and **adds** `CashPaymentsTotal` and `NonCashPayments`. Not additive, on purpose:
  no till reads `/cash` yet (`IStoreHubClient` has no cash method; verified 2026-10-16), and
  `CashSalesTotal` meant "every non-PayLater sale", the very bug being fixed — keeping it invites the
  wrong number back. Renames are cheap until Phase B (REST memory).
- **The v3 panel is untouched.** `CashFlowData.CalculateExpectedCash` keeps the old formula until
  D4 replaces the panel; do not fix it here.
- **Test naming:** `Subject_WhenScenario_DirectVerbOutcome`, never `Should`; one behaviour per test;
  negative/edge first (more than half); named constants; AAA blank lines; dots vertically aligned.
- **Docker must be running** for Task 3.

## Review Focus

1. **A payment stored as `"cash"` (lower case)** must still count as cash. → Task 2
   `HandleAsync_WithLowerCaseCashCode_CountsItAsCash`.
2. **A Cash payment at 23:59 Bangkok yesterday** must not land in today's drawer. → Task 2
   `HandleAsync_WithCashPaidJustBeforeMidnight_ExcludesItFromToday`.
3. **A คนละครึ่ง (`FiftyFifty`) sale** must not change expected cash — the reported bug. → Task 2
   `HandleAsync_WithFiftyFiftyPayment_DoesNotChangeExpectedCash`, and Task 3 end to end on MimyShop.
4. **A PayLater sale** must be neither cash nor a non-cash line. → Task 2
   `HandleAsync_WithPayLaterPayment_ExcludesItFromNonCashPayments` and
   `HandleAsync_WithPayLaterPayment_DoesNotChangeExpectedCash`.
5. **A code no catalogue row names** (an old `"Card"`) must not count as cash, and still shows as a
   non-cash line under its own code. → Task 2 `HandleAsync_WithUncataloguedCode_ListsItAsNonCash`.

---

## File Structure

```
src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Summary/
  CashDrawerCalculator.cs        MODIFY  CashDrawerTotals loses transfer/welfare, gains CashPaymentsTotal
  CashDrawerSummaryDto.cs        MODIFY  -CashSalesTotal -MoneyTransferTotal -WelfareCardTotal
                                         +CashPaymentsTotal +NonCashPayments
src/IndyPOS.Infrastructure/QueryHandlers/CashDrawer/
  GetCashDrawerSummaryQueryHandler.cs  MODIFY  reads Payments + catalogue; drops legacy payments handler
tests/IndyPOS.Application.Tests/UseCases/StoreHub/CashDrawer/Summary/
  CashDrawerCalculatorTests.cs         MODIFY
  GetCashDrawerSummaryQueryHandlerTests.cs  MODIFY
tests/IndyPOS.StoreHub.IntegrationTests/StoreProfiles/
  CashSummaryPerStoreTests.cs          CREATE
CLAUDE.md, ONBOARDING.md               MODIFY  measured test counts
```

---

### Task 1: The formula counts only cash

**Files:**
- Modify: `src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Summary/CashDrawerCalculator.cs`
- Test: `tests/IndyPOS.Application.Tests/UseCases/StoreHub/CashDrawer/Summary/CashDrawerCalculatorTests.cs`

**Interfaces:**
- Produces: `public readonly record struct CashDrawerTotals(decimal CashPaymentsTotal, decimal DebtRepaymentsTotal, decimal CashFloatsTotal, decimal PayoutsTotal);`
  `CashDrawerCalculator.ExpectedCash(CashDrawerTotals)` and `Difference(decimal?, decimal)` keep their signatures.

- [ ] **Step 1: Update the tests.** Change the `Day` fixture to the new four-field struct
  (`CashPaymentsTotal: 10_000m, DebtRepaymentsTotal: 300m, CashFloatsTotal: 1_000m, PayoutsTotal: 650m`).
  Replace `ExpectedCash_WithATypicalDay_ReturnsTodaysFormula` with
  `ExpectedCash_WithATypicalDay_ReturnsCashPlusRepaidPlusFloatsMinusPayouts`, asserting
  `.Be(10_000m + 300m + 1_000m - 650m)`. Fix `ExpectedCash_WithPayoutsAboveTakings_ReturnsNegative` to
  `new CashDrawerTotals(0m, 0m, 0m, 50m)`. The class summary must no longer say "unchanged from v3".

- [ ] **Step 2: Run, expect a compile failure** (the struct still has six fields).
  Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~CashDrawerCalculatorTests"`

- [ ] **Step 3: Change `CashDrawerTotals` and `ExpectedCash`** to the four-field formula. Summary
  comment says WHY: only the Cash method goes into the drawer; v3 subtracted transfer and welfare
  from all takings, so every other campaign (คนละครึ่ง, ม.33, เราชนะ) inflated expected cash. Keep the build green and the
  behaviour unchanged by switching the handler's call site to `new CashDrawerTotals(sales.InvoiceTotalWithoutPayLaterPayments - payments.MoneyTransferTotal - payments.WelfareCardTotal, repaymentsTotal, floatsTotal, payoutsTotal)`
  Task 2 replaces that first argument.

- [ ] **Step 4: Run the calculator and summary tests, expect PASS.**
  Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~CashDrawer"`

- [ ] **Step 5: Commit** — `refactor(cash): the drawer formula takes the cash total, not takings minus two methods`

### Task 2: The summary reads Cash-method payments and lists non-cash methods

**Files:**
- Modify: `src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Summary/CashDrawerSummaryDto.cs`
- Modify: `src/IndyPOS.Infrastructure/QueryHandlers/CashDrawer/GetCashDrawerSummaryQueryHandler.cs`
- Test: `tests/IndyPOS.Application.Tests/UseCases/StoreHub/CashDrawer/Summary/GetCashDrawerSummaryQueryHandlerTests.cs`

**Interfaces:**
- Consumes: `CashDrawerTotals` (Task 1); `PaymentMethodTotals.Build(IReadOnlyList<PaymentMethod>, IReadOnlyList<Payment>)`
  and `PaymentMethodTotalDto(string Code, string DisplayName, decimal Total)` (existing);
  `ReportDateRange.ToUtcRange` (existing).
- Produces, on `CashDrawerSummaryDto`:
  - `public required decimal CashPaymentsTotal { get; init; }` — doc: "Takings paid with the Cash method; the only sales money in the drawer."
  - `public required IReadOnlyList<PaymentMethodTotalDto> NonCashPayments { get; init; }` — doc: "One row per non-cash method (not Cash, not PayLater) enabled or used that day, in display order. Shown, never added to expected cash."
  - removed: `CashSalesTotal`, `MoneyTransferTotal`, `WelfareCardTotal`.
- Handler constructor becomes `(IQueryHandler<GetLegacySalesSummaryQuery, SalesSummary> salesSummary, StoreHubDbContext db, ICashCountRepository counts, ICashDrawerClock clock, IStoreIdentityService storeIdentity)`.
  DI in `CashDrawerServiceCollectionExtensions` registers by type, so it needs no change.

- [ ] **Step 1: Rework the test helper and write the failing tests.** `HandlerFor` drops the payments
  mock; the sales mock keeps returning `InvoiceTotalWithoutPayLaterPayments = 5_000m` so tests prove
  the handler no longer reads it. Add a helper `SeedPaymentAsync(CashDrawerTestContext c, string method, decimal amount, DateTime? createdUtc = null)`
  (an `Invoice` with one `Payment`, default time `TenAmBangkok`) and `SeedMethodAsync(c, code, displayName, isEnabled, displayOrder)`
  for `PaymentMethods` rows (`StoreId = c.StoreIdentity.StoreId`). Named constants:
  `CashPaid = 700m`, `FiftyFiftyPaid = 250m`, `JustBeforeMidnightYesterday = new DateTime(2026, 9, 25, 16, 59, 0, DateTimeKind.Utc)` (23:59 Bangkok).
  Tests (negative first):
  - `HandleAsync_WithFiftyFiftyPayment_DoesNotChangeExpectedCash` — Cash 700 + FiftyFifty 250 → `ExpectedCash.Be(CashPaid)`.
  - `HandleAsync_WithPayLaterPayment_DoesNotChangeExpectedCash` — PayLater 400 only → `ExpectedCash.Be(0m)`.
  - `HandleAsync_WithPayLaterPayment_ExcludesItFromNonCashPayments` — PayLater catalogued and enabled → `NonCashPayments.Should().NotContain(p => p.Code == "PayLater")`.
  - `HandleAsync_WithCashPaidJustBeforeMidnight_ExcludesItFromToday` — Cash 700 at `JustBeforeMidnightYesterday` → `CashPaymentsTotal.Be(0m)`.
  - `HandleAsync_WithLowerCaseCashCode_CountsItAsCash` — method `"cash"` 700 → `CashPaymentsTotal.Be(CashPaid)`.
  - `HandleAsync_WithUncataloguedCode_ListsItAsNonCash` — `"Card"` 90 → `NonCashPayments` contains `("Card", "Card", 90m)` and `CashPaymentsTotal.Be(0m)` is a separate test `HandleAsync_WithUncataloguedCode_DoesNotCountItAsCash`.
  - `HandleAsync_WithNoPaymentsToday_ReturnsEnabledNonCashMethodsAtZero` — catalogue Cash, MoneyTransfer (enabled), WeWin (disabled) → codes `.Equal("MoneyTransfer")`, total 0.
  - `HandleAsync_WithCashPayment_AddsItToExpectedCash` — Cash 700 → `ExpectedCash.Be(CashPaid)`.
  - `HandleAsync_WithFiftyFiftyPayment_ListsItAsNonCash` — FiftyFifty catalogued enabled → row `("FiftyFifty", "คนละครึ่ง", 250m)`.
  Update existing expected-cash tests that assumed `CashSales` (5,000) came from the sales mock:
  `WithPayouts_SubtractsThemFromExpectedCash` → `.Be(-100m)`, `WithCashFloat_…` → `.Be(1_000m)`,
  `WithDebtRepayment_…` → `.Be(300m)`, `WithTwoCounts_DiffsAgainstOnlyTheLatestCount` → `.Be(5_000m - 0m)`.
  Delete `HandleAsync_WithPastDate_AsksForThatDaysPayments` (the dependency is gone); add
  `HandleAsync_WithPastDate_CountsThatDaysCash` — Cash 700 seeded on `Yesterday` 10:00 Bangkok,
  query `Yesterday` → `CashPaymentsTotal.Be(CashPaid)`.

- [ ] **Step 2: Run, expect failures** (compile errors on the DTO members first).
  Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~GetCashDrawerSummaryQueryHandlerTests"`

- [ ] **Step 3: Change the DTO and the handler.** The handler loads the day's payments
  (`AsNoTracking`, the Global-Constraints window) and the catalogue
  (`db.PaymentMethods.AsNoTracking()`), then:
  `cashTotal` = sum where `Method` equals `PaymentMethodCodes.Cash` (`OrdinalIgnoreCase`);
  `NonCashPayments` = `PaymentMethodTotals.Build(catalogue, payments)` without codes Cash and PayLater
  (`OrdinalIgnoreCase`); expected cash from `new CashDrawerTotals(cashTotal, repaymentsTotal, floatsTotal, payoutsTotal)`.
  Keep the method ≤20 lines by extracting `private async Task<(decimal Cash, IReadOnlyList<PaymentMethodTotalDto> NonCash)> PaymentsAsync(DateOnly day, CancellationToken ct)`.
  Update the class summary: the drawer holds only Cash-method takings (spec 2026-10-07 §4.3).

- [ ] **Step 4: Run the whole Application suite, expect PASS** (nothing else uses the removed members).
  Run: `dotnet test tests/IndyPOS.Application.Tests`

- [ ] **Step 5: Build the solution** — `dotnet build` — expect 0 errors (StoreHub integration
  tests reference `CashDrawerSummaryDto`; `PayLaterGeneralProductsTotal` etc. are unchanged).

- [ ] **Step 6: Commit** — `fix(cash): expected cash counts only Cash-method payments`

### Task 3: Prove it on a real MimyShop StoreHub

**Files:**
- Create: `tests/IndyPOS.StoreHub.IntegrationTests/StoreProfiles/CashSummaryPerStoreTests.cs`
- Modify: `CLAUDE.md`, `ONBOARDING.md` (counts)

**Interfaces:**
- Consumes: `StoreProfileHosts.SignedInAsync(key)` (cashier holds `cash.manage`), `ServicesFor(key)`,
  `CompleteSaleRequest` / `SaleLineRequest` / `SalePaymentRequest`, `ServiceProductBarcodes.Delivery`;
  the DTO members from Task 2.

`[Collection(StoreProfilesCollection.Name)]`, primary-constructor `(StoreProfileHosts hosts)`, same
before/after style as `SalesSummaryPerStoreTests` (shared databases — never assert absolute totals).
A private `SellAsync(key, barcode, price, method)` posts a real sale; `SummaryAsync(key)` reads
`/cash/summary` as the cashier.

- [ ] **Step 1: Write the tests.**
  - `CashSummary_ForMimyShopAfterAFiftyFiftySale_KeepsExpectedCash` — sell Delivery 30 paid `FiftyFifty`;
    `after.ExpectedCash.Be(before.ExpectedCash)`.
  - `CashSummary_ForMimyShopAfterAFiftyFiftySale_AddsItToItsNonCashLine` — the `FiftyFifty` row's
    total grows by 30.
  - `CashSummary_ForMimyShopAfterACashSale_AddsItToExpectedCash` — sell Delivery 30 paid `Cash`;
    `after.ExpectedCash.Be(before.ExpectedCash + 30m)`.
  - `CashSummary_ForMimyMart_ListsOnlyMoneyTransferAsNonCash` — codes `.Equal("MoneyTransfer")`.
  - `CashSummary_ForGeneralHardware_HasNoPayLaterNonCashLine` — no `PayLater` code.

- [ ] **Step 2: Run against the fix, expect PASS** (Docker running).
  Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~CashSummaryPerStoreTests"`

- [ ] **Step 3: RED check.** In the handler, temporarily pass `sales.InvoiceTotalWithoutPayLaterPayments`
  (all non-PayLater takings, which is what v3 started from; the payments summary that v3 subtracted
  is no longer injected, and it never held คนละครึ่ง anyway) as the first `CashDrawerTotals` argument. Run the FiftyFifty test and
  see it fail with expected cash 30 higher. Then restore the fix. Record the failure line in the PR body.

- [ ] **Step 4: Run the full solution with TRX**, measure the counts, update CLAUDE.md and
  ONBOARDING.md per-suite numbers (Application and StoreHub change; StoreHub's Docker-down split
  +5, DERIVED). Run: `dotnet test --logger trx --results-directory <scratchpad>/trx`

- [ ] **Step 5: Commit** — tests: `test(storehub): expected cash ignores a คนละครึ่ง sale on MimyShop`;
  docs: `docs: counts after the expected-cash fix`.

- [ ] **Step 6: PR** to `development`, then wait for Codex review before `gh pr merge --auto --merge`.
