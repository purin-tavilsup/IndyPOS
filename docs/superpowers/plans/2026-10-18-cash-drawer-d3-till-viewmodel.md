# Cash Drawer D3 — Till Client + CashDrawerViewModel Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The till can read and change the day's cash drawer through StoreHub, and a
`CashDrawerViewModel` holds the whole ลิ้นชักเก็บเงิน screen's logic (lists, count, totals, errors,
what is enabled), so D4 only binds WinForms controls to it and the Avalonia port reuses it as is.

**Architecture:** Task 1 adds the `/cash` routes to `IStoreHubClient` / `StoreHubHttpClient`.
`IndyPOS.Presentation/CashDrawer/` then gets three ViewModels: one `CashEntriesViewModel` used three
times (payouts, floats, debt repayments) through a small per-list adapter, a `CashCountViewModel`
(the nine counts and the day's count history), and the screen's `CashDrawerViewModel`, which owns the
day, the summary and the per-store flags, and reloads everything after any confirmed save. A
factory builds it for the logged-in user, as `SalesHistoryViewModelFactory` does.

**Tech Stack:** C# .NET 10, CommunityToolkit.Mvvm 8.4, xUnit 2.9 + Moq + FluentAssertions 8,
`FakeTimeProvider`.

**Spec:** `docs/superpowers/specs/2026-09-20-cash-payout-float-persistence-design.md` §3 (cash count:
append-only, latest count used), §5 edge rules, §8 (MVVM, `CashDrawerViewModel`); and
`docs/superpowers/specs/2026-10-07-per-store-ui-design.md` §2 / §4.3 (per-store gating, expected cash).
**PR D3 of 4**: D1 #132, D2 #135 merged; D4 = the WinForms section, moving the panel out of Reports.

## Global Constraints

- **The ViewModel does no money maths** (spec §8): expected cash, counted cash, difference and all
  totals come from `GET /cash/summary`. The **one** exception is the live total of the nine counts
  while typing (v3 shows it before saving); it uses a shared Domain helper, the same one
  `CashCount.CountedTotal` uses, so the two cannot disagree.
- **No optimistic UI** (spec §5): nothing is added to, changed in or removed from a list until
  StoreHub confirms; then the whole screen reloads (summary + lists + counts). On any failure the
  lists stay as they were, **the typed input stays**, and `ErrorMessage` shows Thai text.
- **One call per action**, never batched.
- **Editing only when the summary says `IsEditable`** (today). Commands' `CanExecute` is false
  otherwise; the server's 409 is still the guard.
- **Other days:** only a user with `reports.view` may pick another day (StoreHub returns 403
  otherwise). Everyone with `cash.manage` opens today.
- **Per store** (spec 2026-10-07): the payout category selector and the general/hardware split show
  only when `StoreFeaturesDto.MultipleProductTypesEnabled`; the debt-repayment list and the PayLater
  totals only when `PayLaterEnabled`. When features cannot be read, use `TillLayout.WhenFeaturesUnavailable`'s rule
  (show everything). The non-cash lines are the summary's `NonCashPayments`, as given.
- **Payout category** defaults to `PayoutCategory.General`; edits send it explicitly (`EditCashPayoutRequest` requires it).
- **Input shape only:** an amount the till cannot parse as a number (InvariantCulture, up to 2
  decimals) is rejected with `InvalidAmount` and **no call is made**; every other rule (> 0, max,
  name required) is StoreHub's, and its Thai message is shown as is.
- **Dates on the wire** are InvariantCulture `yyyy-MM-dd` (a Thai till would send Buddhist years).
- **Thai messages** are `public const string` on each ViewModel, formatted with `ex.Message` as the
  sales-history VMs do; catch `StoreHubException` only, never `Exception`, and never swallow the
  caller's cancellation.
- **Not in D3:** today's ลงบัญชี list (no `/cash` route returns it; D4 decides how to show it), the
  WinForms panel, the removal of the v3 JSON/CSV code.
- **Test naming:** `Subject_WhenScenario_DirectVerbOutcome`, never `Should`; one behaviour per test;
  negative first (more than half); named constants; AAA blank lines; dots vertically aligned. A test
  that leaves a command running must complete its `TaskCompletionSource` in `finally` (xunit hang).

## Review Focus

1. **A save that fails keeps what the cashier typed** and does not touch the list. → Task 2
   `AddCommand_WhenStoreHubRefuses_KeepsTheTypedAmount`, `…_LeavesTheListUnchanged`.
2. **A screen opened before midnight** whose summary later says not editable must disable editing.
   → Task 4 `Refresh_WhenTheDayIsNoLongerEditable_DisablesAdding`.
3. **A slow reload overtaken by a newer one** must not overwrite the newer answer. → Task 4
   `ShowDay_WhenAnOlderLoadFinishesLast_KeepsTheNewerDay`.
4. **An amount typed as `"1,000"` or `"abc"`** — thousands separators are accepted, letters refused
   without a call. → Task 2 `AddCommand_WithThousandsSeparators_SendsTheAmount`, `AddCommand_WithLetters_MakesNoCall`.
5. **A cashier (no `reports.view`)** cannot pick yesterday. → Task 4 `ShowDay_ForACashierPickingYesterday_StaysOnToday`.

---

## File Structure

```
src/IndyPOS.Application/Abstractions/StoreHub/IStoreHubClient.cs          MODIFY  17 cash members
src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubHttpClient.cs         MODIFY  their implementation
tests/IndyPOS.Application.Tests/Integration/StoreHub/StoreHubHttpClientCashTests.cs  CREATE
src/IndyPOS.Domain/ValueObjects/CashDenominations.cs                       CREATE  Total(nine counts)
src/IndyPOS.Domain/Entities/Core/CashCount.cs                              MODIFY  CountedTotal uses it
src/IndyPOS.Presentation/CashDrawer/
  ICashEntryApi.cs, CashEntryApis.cs      CREATE  per-list adapter over IStoreHubClient (3 impls)
  CashEntriesViewModel.cs                 CREATE  one list: rows, inputs, add/edit/delete
  CashCountViewModel.cs                   CREATE  nine counts, live total, save, reset, history
  CashDrawerAccess.cs                     CREATE  who may open / view other days
  CashDrawerViewModel.cs                  CREATE  day, summary, flags, sections, reload
  CashDrawerViewModelFactory.cs           CREATE
src/IndyPOS.Windows.Forms/ConfigureServices.cs                             MODIFY  register the factory
tests/IndyPOS.Presentation.Tests/CashDrawer/*.cs                           CREATE
CLAUDE.md, ONBOARDING.md                                                    MODIFY  counts
```

---

### Task 1: The till's StoreHub client speaks /cash

**Files:** `IStoreHubClient.cs`, `StoreHubHttpClient.cs`; test `StoreHubHttpClientCashTests.cs`
(copy the mock-handler helpers from `StoreHubHttpClientTests`).

**Interfaces — Produces** (all with a trailing `CancellationToken cancellationToken = default`;
types from `UseCases.StoreHub.CashDrawer.*`):
```csharp
Task<CashDrawerSummaryDto> GetCashSummaryAsync(DateOnly? businessDate, …);
Task<IReadOnlyList<CashPayoutDto>> GetCashPayoutsAsync(DateOnly? businessDate, …);
Task<CashPayoutDto> AddCashPayoutAsync(AddCashPayoutRequest request, …);
Task<CashPayoutDto> EditCashPayoutAsync(Guid id, EditCashPayoutRequest request, …);
Task DeleteCashPayoutAsync(Guid id, …);
// the same four for floats (CashFloatDto, AddCashFloatRequest, EditCashFloatRequest)
// and debt repayments (DebtRepaymentDto, AddDebtRepaymentRequest, EditDebtRepaymentRequest)
Task<IReadOnlyList<CashCountDto>> GetCashCountsAsync(DateOnly? businessDate, …);
Task<CashCountDto> AddCashCountAsync(AddCashCountRequest request, …);
```
Routes: `/cash/summary`, `/cash/payouts`, `/cash/floats`, `/cash/debt-repayments`, `/cash/counts`;
`?businessDate=yyyy-MM-dd` only when a date is given; `PUT|DELETE /cash/<list>/{id}`. Errors go
through the existing `EnsureSuccessfulResponseAsync` (Thai `{ error }` → `StoreHubClientException`).

- [ ] **Step 1: Write the failing tests** (negative first):
  - `AddCashPayoutAsync_WhenTheDayIsClosed_ThrowsTheServersReason` — 409 `{ "error": "แก้ไขได้เฉพาะรายการของวันนี้เท่านั้น" }` → `StoreHubClientException` with that message.
  - `GetCashSummaryAsync_WithoutLoggingIn_ThrowsNotAuthenticated`.
  - `GetCashSummaryAsync_OnAThaiTill_SendsAGregorianDate` (`RunUnderThaiCultureAsync`; query `businessDate=2026-10-18`).
  - `GetCashSummaryAsync_WithoutADate_SendsNoBusinessDate` — path `/cash/summary`, no query.
  - `DeleteCashFloatAsync_WhenStoreHubAnswersNoContent_Completes` — 204, no body.
  - `EditDebtRepaymentAsync_SendsAPutToTheEntry` — method PUT, path `/cash/debt-repayments/{id}`.
  - `AddCashPayoutAsync_WithAHardwarePayout_SendsTheCategoryByName` — body contains `"category":"Hardware"`.
  - `AddCashCountAsync_PostsToCounts` — POST `/cash/counts`, returns the 201 body's `CountedTotal`.
- [ ] **Step 2: Run, expect compile FAIL.** `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~StoreHubHttpClientCashTests"`
- [ ] **Step 3: Implement** with the existing helpers (`SendAuthenticatedAsync<T>`, the no-body
  overload for DELETE, `IsoDate`). One private `CashQuery(string path, DateOnly? day)` builds the
  URL. XML doc on each interface member: one line, the route.
- [ ] **Step 4: Run, expect PASS.** Same command.
- [ ] **Step 5: Commit** — `feat(till): the StoreHub client speaks /cash`

### Task 2: One list section for payouts, floats and debt repayments

**Files:** `ICashEntryApi.cs`, `CashEntryApis.cs`, `CashEntriesViewModel.cs`; tests
`CashEntriesViewModelTests.cs` (payouts for most cases; one test each that floats and repayments
call their own routes).

**Interfaces — Produces:**
```csharp
public sealed record CashEntryRow(Guid Id, DateTime StoreTime, string? Text, PayoutCategory? Category, decimal Amount);

public interface ICashEntryApi   // one per list; maps rows and inputs to that list's requests
{
    bool HasCategory { get; }    // payouts only
    Task<IReadOnlyList<CashEntryRow>> ListAsync(DateOnly day, CancellationToken ct);
    Task AddAsync(CashEntryInput input, CancellationToken ct);
    Task EditAsync(Guid id, CashEntryInput input, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
public sealed record CashEntryInput(decimal Amount, string? Text, PayoutCategory Category);
// CashEntryApis.Payouts(client, clock) / .Floats(client, clock) / .DebtRepayments(client, clock)
//   Text = Description (payouts, floats) or CustomerName (repayments); StoreTime via StoreClock.

public sealed partial class CashEntriesViewModel : ObservableObject
{
    public CashEntriesViewModel(ICashEntryApi api, Func<Task> reloadScreen);
    public ObservableCollection<CashEntryRow> Rows { get; }
    [ObservableProperty] string _amountText; [ObservableProperty] string? _text;
    [ObservableProperty] PayoutCategory _category = PayoutCategory.General;
    [ObservableProperty] CashEntryRow? _selectedRow;   // selecting loads its values into the inputs
    [ObservableProperty] bool _isEditable; [ObservableProperty] bool _isBusy; [ObservableProperty] string? _errorMessage;
    public bool HasCategory => api.HasCategory;
    IAsyncRelayCommand AddCommand;     // CanExecute: IsEditable && !IsBusy
    IAsyncRelayCommand EditCommand;    // + SelectedRow != null
    IAsyncRelayCommand DeleteCommand;  // + SelectedRow != null
    Task LoadAsync(DateOnly day, CancellationToken ct);   // the parent calls it; replaces Rows
    public const string InvalidAmount = "จำนวนเงินไม่ถูกต้อง";
    public const string SaveFailed = "บันทึกไม่สำเร็จ: {0}";
    public const string DeleteFailed = "ลบไม่สำเร็จ: {0}";
}
```
After a confirmed add or edit: clear the inputs (`AmountText = ""`, `Text = null`, `Category =
General`, `SelectedRow = null`), then `await reloadScreen()`. After a confirmed delete: `SelectedRow = null`, reload.

- [ ] **Step 1: Write the failing tests** (negative first):
  `AddCommand_WhenStoreHubRefuses_KeepsTheTypedAmount`, `AddCommand_WhenStoreHubRefuses_LeavesTheListUnchanged`,
  `AddCommand_WhenStoreHubRefuses_ShowsItsReason` (`string.Format(SaveFailed, msg)`),
  `AddCommand_WithLetters_MakesNoCall`, `AddCommand_WithLetters_ShowsInvalidAmount`,
  `AddCommand_WhenNotEditable_CannotExecute`, `DeleteCommand_WithNothingSelected_CannotExecute`,
  `DeleteCommand_WhenStoreHubRefuses_KeepsTheRow`, `EditCommand_WhenStoreHubRefuses_KeepsTheTypedText`,
  `AddCommand_WithThousandsSeparators_SendsTheAmount` ("1,000.50" → 1000.50m),
  `AddCommand_WithoutChoosingACategory_SendsGeneral`, `AddCommand_WhenSaved_ReloadsTheScreen`,
  `AddCommand_WhenSaved_ClearsTheInputs`, `SelectedRow_WhenSet_LoadsItsAmountIntoTheInput`,
  `EditCommand_ForAPayout_SendsItsCategory`, `DeleteCommand_WhenDeleted_ReloadsTheScreen`,
  `LoadAsync_ForFloats_AsksTheFloatsRoute`, `LoadAsync_ForDebtRepayments_ShowsTheCustomerName`.
  Use a real `CashEntryApis.Payouts(...)` over a mocked `IStoreHubClient` (not a mocked adapter),
  so the adapter is tested too; `reloadScreen` is a counting lambda.
- [ ] **Step 2: Run, expect compile FAIL.** `dotnet test tests/IndyPOS.Presentation.Tests --filter "FullyQualifiedName~CashEntriesViewModelTests"`
- [ ] **Step 3: Implement.** Parse `AmountText` with `decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)`.
  Commands use `[NotifyCanExecuteChangedFor]` on `IsEditable`, `IsBusy`, `SelectedRow`.
- [ ] **Step 4: Run, expect PASS.** Same command.
- [ ] **Step 5: Commit** — `feat(till): one cash-entry list ViewModel for payouts, floats and repayments`

### Task 3: The cash count section

**Files:** `src/IndyPOS.Domain/ValueObjects/CashDenominations.cs`, `CashCount.cs`,
`CashCountViewModel.cs`; tests `CashCountViewModelTests.cs`, and one Domain test.

**Interfaces — Produces:**
```csharp
public static class CashDenominations   // Domain
{
    public static decimal Total(int bank1000, int bank500, int bank100, int bank50, int bank20,
                                int coin10, int coin5, int coin2, int coin1);
}
public sealed partial class CashCountViewModel : ObservableObject
{
    public CashCountViewModel(IStoreHubClient client, StoreClock clock, Func<Task> reloadScreen);
    [ObservableProperty] int _bankNote1000Count … _coin1Count;   // nine, each [NotifyPropertyChangedFor(nameof(TypedTotal))]
    public decimal TypedTotal { get; }                            // CashDenominations.Total(...)
    public ObservableCollection<CashCountRow> History { get; }    // newest first, from GET /cash/counts
    [ObservableProperty] bool _isEditable; [ObservableProperty] bool _isBusy; [ObservableProperty] string? _errorMessage;
    IAsyncRelayCommand SaveCommand;   // CanExecute: IsEditable && !IsBusy
    IRelayCommand ResetCommand;       // sets the nine inputs to 0 only; never touches saved rows (spec §3)
    Task LoadAsync(DateOnly day, CancellationToken ct);
    public const string SaveFailed = "บันทึกการนับเงินไม่สำเร็จ: {0}";
}
public sealed record CashCountRow(DateTime StoreTime, decimal CountedTotal);
```
- [ ] **Step 1: Write the failing tests** (negative first): `SaveCommand_WhenStoreHubRefuses_KeepsTheCounts`,
  `SaveCommand_WhenStoreHubRefuses_ShowsItsReason`, `SaveCommand_WhenNotEditable_CannotExecute`,
  `ResetCommand_AfterASave_DoesNotCallStoreHub`, `ResetCommand_WithCountsTyped_ZeroesThem`,
  `TypedTotal_WithOneOfEach_ReturnsTheSumOfTheirValues` (1888m), `SaveCommand_WhenSaved_SendsTheNineCounts`,
  `SaveCommand_WhenSaved_ReloadsTheScreen`, `LoadAsync_WithTwoCounts_ListsTheNewestFirst`;
  Domain: `Total_WithOneOfEach_Returns1888`.
- [ ] **Step 2: Run, expect compile FAIL.** `dotnet test tests/IndyPOS.Presentation.Tests --filter "FullyQualifiedName~CashCountViewModelTests"`
- [ ] **Step 3: Implement**; `CashCount.CountedTotal` now calls `CashDenominations.Total` (its
  existing tests must stay green). After a confirmed save the inputs are **kept** (the cashier sees
  what was saved; Reset clears them) and the screen reloads.
- [ ] **Step 4: Run, expect PASS** — Presentation and Domain suites.
- [ ] **Step 5: Commit** — `feat(till): the cash count ViewModel, with a live total from the shared denominations`

### Task 4: The screen — day, summary, per-store flags, reload

**Files:** `CashDrawerAccess.cs`, `CashDrawerViewModel.cs`, `CashDrawerViewModelFactory.cs`,
`src/IndyPOS.Windows.Forms/ConfigureServices.cs`; tests `CashDrawerViewModelTests.cs`,
`CashDrawerViewModelFactoryTests.cs`.

**Interfaces — Consumes:** Tasks 1–3. **Produces:**
```csharp
public sealed record CashDrawerAccess(bool CanOpen, bool CanViewAnyDay)
{ public static CashDrawerAccess For(ILoggedInUser user); }   // cash.manage / reports.view via RoleCapabilities

public sealed partial class CashDrawerViewModel : ObservableObject
{
    public CashDrawerViewModel(IStoreHubClient client, StoreClock clock, CashDrawerAccess access, StoreFeaturesDto? features);
    public CashEntriesViewModel Payouts { get; } , Floats { get; } , DebtRepayments { get; }
    public CashCountViewModel Count { get; }
    [ObservableProperty] DateOnly _businessDate;            // starts at clock.Today
    [ObservableProperty] CashDrawerSummaryDto? _summary;
    public bool IsEditable => Summary?.IsEditable ?? false; // pushed into the four sections on each load
    public bool ShowProductTypeSplit { get; }               // MultipleProductTypesEnabled; null features -> true
    public bool ShowPayLater { get; }                       // PayLaterEnabled; null features -> true
    public bool CanPickDay => access.CanViewAnyDay;
    [ObservableProperty] bool _isBusy; [ObservableProperty] string? _errorMessage;
    IAsyncRelayCommand RefreshCommand;                      // reloads BusinessDate
    IAsyncRelayCommand<DateOnly> ShowDayCommand;            // other days only if CanViewAnyDay
    public const string LoadFailed = "โหลดลิ้นชักเก็บเงินไม่สำเร็จ: {0}";
}
public sealed class CashDrawerViewModelFactory(IStoreHubClient client, StoreClock clock, ICurrentUser currentUser)
{ public CashDrawerViewModel Create(StoreFeaturesDto? features); public const string NotLoggedIn = "No user is logged in on this till."; }
```
One load = summary, the three lists and the counts for `BusinessDate`, through one restartable
`CancellationTokenSource` (or a load version, as `InvoiceHistoryViewModel`), so an older answer
never lands over a newer one. The sections get `reloadScreen = () => RefreshCommand.ExecuteAsync(null)`.

- [ ] **Step 1: Write the failing tests** (negative first): `Refresh_WhenStoreHubFails_ShowsItsReason`,
  `Refresh_WhenTheDayIsNoLongerEditable_DisablesAdding` (summary `IsEditable = false` →
  `Payouts.AddCommand.CanExecute(null)` false), `ShowDay_ForACashierPickingYesterday_StaysOnToday`
  (no call for yesterday), `ShowDay_WhenAnOlderLoadFinishesLast_KeepsTheNewerDay` (TCS),
  `Create_WithNobodyLoggedIn_ThrowsNotLoggedIn`, `ShowPayLater_WithoutFeatures_ReturnsTrue`,
  `ShowPayLater_ForAStoreWithoutPayLater_ReturnsFalse`, `ShowProductTypeSplit_ForAMinimart_ReturnsFalse`,
  `Refresh_WhenLoaded_ShowsTheSummarysExpectedCash`, `Refresh_WhenLoaded_LoadsAllThreeLists`,
  `Refresh_WhenLoaded_LoadsTheCountHistory`, `ShowDay_ForAManagerPickingYesterday_LoadsYesterday`,
  `PayoutsAdd_WhenSaved_ReloadsTheSummary` (a section save reaches the parent's reload).
- [ ] **Step 2: Run, expect compile FAIL.** `dotnet test tests/IndyPOS.Presentation.Tests --filter "FullyQualifiedName~CashDrawer"`
- [ ] **Step 3: Implement**, and register `CashDrawerViewModelFactory` as a singleton next to
  `SalesHistoryViewModelFactory` in `src/IndyPOS.Windows.Forms/ConfigureServices.cs` (no UI uses it
  until D4; a Windows.Forms test that resolves it from the container proves the registration:
  `ConfigureServices_WhenBuilt_ResolvesTheCashDrawerFactory`, following the file's existing DI tests if any).
- [ ] **Step 4: Run the Presentation, Domain, Application and Windows.Forms suites, expect PASS.**
- [ ] **Step 5: Run the full solution with TRX, nothing else running**; update CLAUDE.md and
  ONBOARDING.md counts (Presentation, Application, Domain, Windows.Forms; no container suite changes).
  Run: `dotnet test --logger trx --results-directory <scratchpad>/trx`
- [ ] **Step 6: Commit** — `feat(till): the cash drawer screen ViewModel` and `docs: counts after the cash drawer ViewModel`;
  PR, Codex, then merge once its findings are handled and CI is green (check PR head == local HEAD).
