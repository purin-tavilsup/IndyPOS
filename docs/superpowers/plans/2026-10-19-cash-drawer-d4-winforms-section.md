# Cash Drawer D4 — WinForms ลิ้นชักเก็บเงิน Section Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The cashier runs the whole cash drawer from its own main-menu section, ลิ้นชักเก็บเงิน: every
payout, float, debt repayment and count is saved to StoreHub at once, the figures come from
StoreHub, and the v3 panel with its JSON / CSV / Google Drive files is gone.

**Architecture:** Task 1 finishes the ViewModel side (today's ลงบัญชี list, and the per-store rules D3
left to the screen). Tasks 2–3 build code-only WinForms views bound by hand, exactly as the
sales-history tab does (`PropertyChanged` → `Render()`, `CommandButton.Bind`, `TillGrid`, no
designer, no `DataBindings`). Task 4 puts the section in `MainForm` behind `cash.manage` and takes
the old panel out of Reports. Task 5 deletes the v3 code. Task 6 proves it on real dev stores.

**Tech Stack:** C# .NET 10 WinForms, CommunityToolkit.Mvvm, xUnit + Moq + FluentAssertions,
glimpse-drive + the `testing-indypos` skill for the live run.

**Spec:** `docs/superpowers/specs/2026-09-20-cash-payout-float-persistence-design.md` §2 (retire both
v3 files), §8 (section, thin UI, MVVM); `docs/superpowers/specs/2026-10-07-per-store-ui-design.md`
§2 / §4.3 (per store). **PR D4 of 4**; D1 #132, D2 #135, D3 #136 merged.

## Global Constraints

- **The UI is thin** (spec §8): no totals, no formula, no "which count is latest", no validation
  beyond what the ViewModels already do. Every figure on screen is a ViewModel property.
- **Binding pattern = the sales-history tab's** (`UI/Report/InvoiceHistoryView.cs`,
  `SalesHistoryReportPanel.cs`): views built in code; `vm.PropertyChanged += … Render()`;
  `CommandButton.Bind(button, command)` kept in a `List<IDisposable>` and disposed in `Unbind()`;
  `TillGrid.Create/AddColumn(…, "N2")`; text boxes write to the VM on `TextChanged`; **Enter is read
  on `KeyUp`** (the scanner PreMessageFilter swallows Enter `KeyDown`). A fresh ViewModel per open,
  from `CashDrawerViewModelFactory.Create(features)`, on `VisibleChanged`.
- **Money shows as `"N2"`** (th-TH ICU prints three decimals for bare `N`; `MoneyFormatTests` guards it).
- **Per store** (spec 2026-10-07): payout category picker only when `ShowPayoutCategory`;
  general/hardware split rows only when `ShowProductTypeSplit`; the debt-repayment section and the
  ลงบัญชี list and totals only when `ShowPayLater`; one non-cash line per `Summary.NonCashPayments` entry.
- **Section gate:** the menu button shows for `cash.manage` (Cashier, StoreManager, SystemAdmin) via
  `RoleCapabilities.HasCapability(roleId, Capability.CashManage)`, like `CanViewReports()`. A day
  picker shows only when `CanPickDay`.
- **Stable control `Name`s** (they are the UI Automation ids the live run drives): `CashDrawerButton`,
  `PayoutsGrid`, `PayoutAmountBox`, `PayoutTextBox`, `PayoutCategoryBox`, `AddPayoutButton`,
  `EditPayoutButton`, `DeletePayoutButton`, same pattern for `Float*` and `DebtRepayment*`,
  `BankNote1000CountBox` … `Coin1CountBox`, `TypedTotalLabel`, `SaveCountButton`, `ResetCountButton`,
  `CountHistoryGrid`, `ExpectedCashLabel`, `CountedCashLabel`, `CashDifferenceLabel`,
  `CashDrawerErrorLabel`, `RefreshCashDrawerButton`, `CashDrawerDayPicker`, `TodaysPayLaterGrid`.
- **Never `ShowDialog`/`MessageBox` from these views**: errors go to the error labels. (A modal in a
  test hangs the run — memory "WinForms tests can pop modal dialogs".)
- **Tests:** `Subject_WhenScenario_DirectVerbOutcome`; negative first; a test that leaves a command
  running completes its TCS in `finally`; panels are opened with `OpenAsync()` directly.
- **Live verification is required** (memory "Autonomous UI testing skills"): drive the real till
  with the `testing-indypos` skill, not Pond's clicks.

## Review Focus

1. **A cashier at a MimyMart** sees no category picker, no debt-repayment section and no ลงบัญชี list.
   → Task 3 `Open_ForAMinimart_HidesTheDebtRepayments`, Task 2 `Bind_WithoutCategory_HidesThePicker`.
2. **A refused save** shows StoreHub's Thai reason on screen and keeps the typed amount in the box.
   → Task 2 `Add_WhenStoreHubRefuses_ShowsTheReasonUnderTheList`.
3. **Opening the section twice** (log out, log in as someone else) binds a fresh ViewModel and drops
   the old one. → Task 3 `Open_Twice_UsesTheNewViewModel`.
4. **A day picked by a manager** shows that day and disables every add/edit/delete/save button.
   → Task 3 `ShowDay_ForYesterday_DisablesSaving`.
5. **The menu with nine buttons** fits the control panel without overlap. → Task 4
   `ApplyMenu_WithEveryButton_StacksWithoutOverlap`.

---

### Task 1: ViewModel — today's ลงบัญชี and the per-store rules

**Files:** `src/IndyPOS.Presentation/CashDrawer/CashDrawerViewModel.cs`, `CashEntriesViewModel.cs`,
`CashCountViewModel.cs`; StoreHub's `CashCountDto` and the counts query/add handlers
(`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Counts/`); tests in
`tests/IndyPOS.Presentation.Tests/CashDrawer/` and the existing count handler tests.

**Who counted (spec §3: the history shows time, cashier, total):** the till cannot list users, so
StoreHub names them. `CashCountDto` gains a trailing `string? CountedByName` ("First Last" from
`StoreUsers`; null when the user row is gone) — additive, no migration; the D2 cloud mirror ignores
it (its payload is the domain row). `CashCountRow` gains `string? CountedByName`. Test:
`HandleAsync_ForACount_NamesWhoCounted`, `LoadAsync_WithACount_ShowsWhoCountedByName`.

**Produces:**
```csharp
public sealed record PayLaterRow(DateTime StoreTime, string Description, decimal Amount);
// CashDrawerViewModel
public ObservableCollection<PayLaterRow> TodaysPayLater { get; }   // the day's new ลงบัญชี, oldest first
public bool ShowPayoutCategory => ShowProductTypeSplit;
// CashEntriesViewModel: deselecting a row (SelectedRow = null) clears the inputs
```
Source: `IStoreHubClient.GetPayLaterAsync(includeCompleted: true, searchTerm: null)` (any logged-in
user may call it; `/reports/pay-later` refuses cashiers and is grouped per customer), filtered to
`StoreClock.BusinessDateOf(CreatedUtc) == day`; fetched with the rest of the day only when
`ShowPayLater`, and shown together with it (D3's whole-day-or-nothing rule). The **DebtRepayments**
section stays not editable when `!ShowPayLater` (`SetEditable`).

- [ ] **Step 1: Failing tests:** `Refresh_ForAStoreWithoutPayLater_DoesNotAskForPayLater`,
  `Refresh_ForAStoreWithoutPayLater_KeepsDebtRepaymentsReadOnly`,
  `Refresh_WithAPayLaterFromYesterday_LeavesItOut`, `Refresh_WithACompletedPayLaterFromToday_ListsIt`
  (paid off the same day still happened today), `Refresh_WithTwoPayLaterToday_ListsThemOldestFirst`,
  `ShowPayoutCategory_ForAMinimart_ReturnsFalse`, `SelectedRow_WhenCleared_ClearsTheInputs`.
- [ ] **Step 2: Run, expect FAIL.** `dotnet test tests/IndyPOS.Presentation.Tests --filter "FullyQualifiedName~CashDrawer"`
- [ ] **Step 3: Implement.** **Step 4: PASS.** **Step 5: Commit** — `feat(till): the cash drawer lists today's ลงบัญชี and owns its per-store rules`

### Task 2: The list and count views

**Files:** `src/IndyPOS.Windows.Forms/UI/CashDrawer/CashEntriesView.cs` (one view, used three times,
with a `namePrefix` such as `"Payout"` for its control names and a Thai title),
`CashCountView.cs`; tests `tests/IndyPOS.Windows.Forms.Tests/UI/CashDrawer/CashEntriesViewTests.cs`,
`CashCountViewTests.cs`, fixture `CashDrawerFixture.cs` (modelled on `SalesHistoryFixture`).

**Produces:** `CashEntriesView(string namePrefix, string title)` with `Bind(CashEntriesViewModel)`,
`Unbind()`; `CashCountView` with `Bind(CashCountViewModel)`, `Unbind()`. Grid columns: เวลา (HH:mm),
รายการ / ชื่อลูกค้า, หมวด (payouts, only when the category shows), จำนวนเงิน (N2). Count view: nine
labelled boxes (1000 … 1), the live `TypedTotal` (N2), Save, Reset, and the history grid (เวลา,
ผู้นับ = `CountedByName`, blank when null, ยอดนับ N2).

- [ ] **Step 1: Failing tests** (negative first): `Add_WhenStoreHubRefuses_ShowsTheReasonUnderTheList`,
  `Add_WhenStoreHubRefuses_KeepsTheAmountInTheBox`, `Bind_WhenNotEditable_DisablesAdd`,
  `Bind_WithoutCategory_HidesThePicker`, `Delete_WithNoRowSelected_IsDisabled`,
  `CountBox_WithLetters_KeepsTheTypedTotalAtZero`, `Bind_WithTwoRows_FillsTheGrid`,
  `CountBox_WhenTyped_UpdatesTheTypedTotal`, `ClickingARow_PutsItsAmountInTheBox`.
- [ ] **Step 2: Run, expect FAIL.** `dotnet test tests/IndyPOS.Windows.Forms.Tests --filter "FullyQualifiedName~CashDrawer"`
- [ ] **Step 3: Implement** with `TillStyle`/`TillGrid`/`CommandButton`. **Step 4: PASS.**
- [ ] **Step 5: Commit** — `feat(till): cash-entry and cash-count views bound to their ViewModels`

### Task 3: The ลิ้นชักเก็บเงิน panel

**Files:** `src/IndyPOS.Windows.Forms/UI/CashDrawer/CashDrawerPanel.cs`; tests `CashDrawerPanelTests.cs`.

**Produces:** `CashDrawerPanel(CashDrawerViewModelFactory factory, IStoreFeaturesProvider features)`,
`public Task OpenAsync()` (called from `VisibleChanged`): create the VM (`InvalidOperationException`
when nobody is logged in → show nothing), read features, bind the three `CashEntriesView`s, the
`CashCountView` and the summary block, then `RefreshCommand`. The summary block (with
`MoneyRowView`): ยอดขาย เบ็ดเตล็ด / ฮาร์ดแวร์ (split only), [รายรับ] เงินสด, one line per non-cash method,
ลงบัญชี เบ็ดเตล็ด / ฮาร์ดแวร์ (PayLater only), รายจ่าย, เงินทอน, ลูกค้าชำระหนี้ (PayLater only),
เงินสด [คำนวณ], เงินสด [นับ], ผลต่าง with พอดี / เกิน / ขาด. The ผลต่าง word is a ViewModel property
(`DifferenceText`) added here with its own Presentation test, so the screen does no comparison.

- [ ] **Step 1: Failing tests:** `Open_WithNobodyLoggedIn_LeavesTheScreenEmpty`,
  `Open_ForAMinimart_HidesTheDebtRepayments`, `Open_ForAMinimart_HidesTheProductTypeSplit`,
  `ShowDay_ForYesterday_DisablesSaving`, `Open_Twice_UsesTheNewViewModel`,
  `Open_WhenLoaded_ShowsTheExpectedCash`, `Open_WithTwoNonCashMethods_ShowsTwoLines`,
  `Open_ForACashier_HidesTheDayPicker`; Presentation: `DifferenceText_WhenShort_ReturnsKhad` (ขาด),
  `…_WhenOver_ReturnsKoen` (เกิน), `…_WhenEqual_ReturnsPhodi` (พอดี), `…_WhenNotCounted_ReturnsEmpty`.
- [ ] **Step 2: FAIL. Step 3: Implement. Step 4: PASS.**
- [ ] **Step 5: Commit** — `feat(till): the ลิ้นชักเก็บเงิน panel`

### Task 4: Its own menu section; out of Reports

**Files:** `src/IndyPOS.Windows.Forms/Enums/Panels.cs` (`SubPanel.CashDrawer`; drop
`ReportSubPanel.CashFlowCalculator`), `MainForm.cs` / `.Designer.cs` (a ninth menu button
`CashDrawerButton` with `Properties.Resources.Money_50`, gated by `CanManageCash()`; **nine slots of
equal height filling the 972 px control panel** instead of eight), `ReportsPanel.cs` / `.Designer.cs`
(remove the นับเงินสด button and its field, case and tab), `ConfigureServices.cs`
(`CashDrawerPanel` singleton). Tests: existing MainForm / menu-layout tests extended,
`ReportPanelsLayoutTests` updated.

- [ ] **Step 1: Failing tests:** `ApplyMenu_ForAUserWithoutCashManage_HidesTheCashDrawerButton`
  (a role without it, if any; else assert the gate through `RoleCapabilities`),
  `ApplyMenu_WithEveryButton_StacksWithoutOverlap`, `ReportsPanel_WhenBuilt_HasNoCashFlowTab`.
- [ ] **Step 2: FAIL. Step 3: Implement. Step 4: PASS** — Windows.Forms suite.
- [ ] **Step 5: Commit** — `feat(till): ลิ้นชักเก็บเงิน gets its own menu section, out of Reports`

### Task 5: Delete the v3 cash-flow code

Delete `CashFlowCalculatorPanel.cs` / `.Designer.cs` / `.resx`, `Application/Common/Models/CashFlowData.cs`,
`Change.cs`, `Payout.cs`, `PayLaterPayment.cs`, `tests/IndyPOS.Application.Tests/Models/CashFlowDataTests.cs`,
`ICsvService` / `CsvService` and its registration, and the `CsvHelper.NS` package. Keep `IJsonService`
(store configuration uses it). Remove `IReportService.GetPayLaterPaymentsByPeriodAsync` and its
implementation **only if** nothing else calls it (grep first). Spec §9's `Change → CashFloat` rename
is then moot: record that in the spec.

- [ ] **Step 1:** `grep` each type and member; delete; `dotnet build` → 0 errors.
- [ ] **Step 2:** run Application, Windows.Forms and Infrastructure-using suites → PASS.
- [ ] **Step 3: Commit** — `refactor(till): delete the v3 cash-flow panel and its JSON/CSV/Drive files`

### Task 6: Prove it on the real till, then counts and PR

- [ ] **Step 1: Live run** with the `testing-indypos` skill (load it): `up.sh GeneralHardware`, log in
  as `cashier`, open `CashDrawerButton`, add a Hardware payout of 50, a float of 1,000, a debt
  repayment of 300 (ลุงสมชาย), type a count and save; read `ExpectedCashLabel` before and after; check
  the rows in Postgres (`db.sh`). Then `up.sh MimyMart`: the debt section, category picker and
  ลงบัญชี list are absent. Screenshot each (glimpse) and record what was read in the PR.
- [ ] **Step 2:** full solution with TRX, nothing else running; update CLAUDE.md / ONBOARDING.md counts.
- [ ] **Step 3:** commit `docs: counts after the cash drawer section`; PR; Codex; merge once findings
  are handled and CI is green (PR head == local HEAD).
