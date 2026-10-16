# Invoice History, Till Plan C2 — The Sales-History Tab and บิลวันนี้ Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A manager finds any bill from Reports → ประวัติการขาย (today, this month, this year, a date range, or by bill number) and reprints it. A cashier finds and reprints today's bills from a บิลวันนี้ button on the sale screen. Cashiers no longer see Reports, which StoreHub already refuses them.

**Architecture:** One code-built `InvoiceHistoryView` (top bar, bill list with "load more", and C1's `InvoiceDetailView` on the right) binds to plan B's `InvoiceHistoryViewModel`. Two hosts use it: the rebuilt `SalesHistoryReportPanel` (Reports tab, `CreateHistory`), and a new `TodaysBillsForm` dialog (sale screen, `CreateTodayOnly`). Each open gets a fresh ViewModel for whoever is logged in. The stubbed `IReportService` invoice members and their legacy DTOs go.

**Tech Stack:** .NET 10 WinForms, CommunityToolkit.Mvvm 8.4.2 (via `IndyPOS.Presentation`), Prism events, xUnit 2.9.3, Moq 4.20.72, FluentAssertions 8.8.0.

**Spec:** `docs/superpowers/specs/2026-09-27-invoice-history-v4-design.md` §7 (`InvoiceHistoryViewModel`, the rebuilt `SalesHistoryReportPanel`, cashier entry, "Removed"). Plan 3 is split into A — receipts (#127), B — ViewModels (#129), C1 — bill detail screen (#130), **C2 — this plan**.

## Global Constraints

- Screens "show ViewModel state and forward clicks only, with no logic in code-behind" (spec §7).
- `SalesHistoryReportPanel`, rebuilt: "a top bar with the period buttons, date range and bill-number box (Enter searches); the bill list on the left, with 'load more'; `InvoiceDetailView` on the right" (spec §7).
- Today-only mode: "no period picker, no totals" (spec §7).
- "a **บิลวันนี้** button on the sale screen, shown with `sales.reprint`, opens the same screen in today-only mode. Cashiers cannot open Reports." (spec §7)
- Removed: `GetInvoicesByPeriodAsync`, `GetInvoicesByDateRangeAsync`, `GetInvoiceProductsByInvoiceIdAsync`, `GetPaymentsByInvoiceIdAsync` (spec §7). `GetInvoiceInfoAsync` and `StubInvoiceInfo` are already gone (plan A); nothing to do for them. "The other stubs stay (§9)."
- Every button bound to a command goes through C1's `CommandButton` (it checks `CanExecute` before running).
- Bill number shown as `0000000000`; money `N2` (`MoneyFormatTests` pins it); times in store time (the ViewModel converts).
- Every control a test or the UI driver needs has a stable `Name` (it becomes the UIA AutomationId).
- Tests: `Subject_WhenScenario_DirectVerbOutcome`, negative first, one behaviour per test. **No test may open a modal dialog** (`ShowDialog`, `MessageForm`).

## Review Focus

1. **A manager's บิลวันนี้ stays today-only.** A yesterday's bill found by number there cannot be reprinted, even by a manager. Pinned in Task 2 (`CreateTodayOnly_ForAStoreManager_HoldsReprintToToday`).
2. **A late page from a ViewModel the view no longer follows.** It must add no rows. Pinned in Task 3 (`Bind_ToASecondViewModel_IgnoresTheFirstsBills`).
3. **Re-opening ประวัติการขาย after another user logs in.** The new user's rights apply, not the last one's. Pinned in Task 4 (`OpenAsync_AfterAnotherUserLogsIn_FollowsTheNewUser`).
4. **A payment code with no Thai name** (the names failed to load, or StoreHub's `Unknown`). The list shows the code, never a blank or a crash. Pinned in Task 1.
5. **A double-click on บิลวันนี้ while it loads.** One dialog, no "Form that is already visible" error. Pinned in Task 5.

---

## File Structure

| File | Responsibility |
|---|---|
| Modify `src/IndyPOS.Presentation/SalesHistory/InvoiceHistoryViewModel.cs` | Rows carry the payment method's display name |
| Modify `src/IndyPOS.Presentation/SalesHistory/SalesHistoryViewModelFactory.cs` | `CreateHistory`, `CreateTodayOnly` |
| Create `tests/IndyPOS.Windows.Forms.Tests/UI/Report/SalesHistoryFixture.cs` | Shared factory/user/feature builders for the till's sales-history tests |
| Create `src/IndyPOS.Windows.Forms/UI/Report/TillStyle.cs` | The till's font, colours, label and button, for views built in code |
| Create `src/IndyPOS.Windows.Forms/UI/Report/InvoiceHistoryView.cs` | Top bar, bill list, load more, detail; bound to `InvoiceHistoryViewModel` |
| Create `src/IndyPOS.Windows.Forms/Services/StoreFeaturesProviderExtensions.cs` | `GetLayoutAsync`: the store's layout, or the fallback |
| Replace `src/IndyPOS.Windows.Forms/UI/Report/SalesHistoryReportPanel.cs` (delete `.Designer.cs`, `.resx`) | Reports tab hosting `InvoiceHistoryView` |
| Create `src/IndyPOS.Windows.Forms/UI/Report/TodaysBillsForm.cs` | บิลวันนี้ dialog |
| Modify `SalePanel.cs`, `MainForm.cs`, `ConfigureServices.cs` (till) | บิลวันนี้ button; Reports only with `reports.view`; DI |
| Modify `IReportService.cs`, `StoreHubReportService.cs`, `LegacyDtos.cs` | Remove the invoice stubs and their DTOs |

**Branch:** `feat/sales-history-tab` from `development`.

---

### Task 1: The bill list shows payment-method names

`InvoiceSummaryDto.PrimaryPaymentMethod` is a code (`Cash`, `PayLater`, …; `ListSalesQueryHandler` sends `Unknown` for a bill with no payment). The list must show the Thai name, as the detail's payments grid does.

**Files:**
- Modify: `src/IndyPOS.Presentation/SalesHistory/InvoiceHistoryViewModel.cs`
- Test: `tests/IndyPOS.Presentation.Tests/SalesHistory/InvoiceHistoryViewModelTests.cs`

**Interfaces:**
- Consumes: `IPaymentMethodNames.GetAsync(CancellationToken)` (`IndyPOS.Application.Abstractions.StoreHub`). It never throws for a failed load; it returns an empty map and retries next time.
- Produces: `InvoiceHistoryViewModel(IStoreHubClient client, StoreClock clock, SalesHistoryAccess access, InvoiceDetailViewModel detail, IPaymentMethodNames paymentMethodNames)`. `InvoiceRow`'s 5th member is renamed `PrimaryPaymentMethod` → `PaymentMethodName`: `InvoiceRow(Guid Id, long InvoiceNumber, DateTime CreatedAt, decimal TotalAmount, string PaymentMethodName, int LineCount)`.

- [ ] **Step 1: Write the failing tests**

In `InvoiceHistoryViewModelTests`, replace the `History` helper and add a one-bill page helper and the names:

```csharp
    private InvoiceHistoryViewModel History(SalesHistoryAccess access)
    {
        var clock = new StoreClock(new FakeTimeProvider(NowUtc), Bangkok);
        var detail = new InvoiceDetailViewModel(_client.Object, Mock.Of<IReceiptPrinterService>(),
                                                new ReceiptDocumentFactory(Bangkok), clock, access);

        return new InvoiceHistoryViewModel(_client.Object, clock, access, detail, Names());
    }

    private static IPaymentMethodNames Names()
    {
        var names = new Mock<IPaymentMethodNames>();
        names.Setup(n => n.GetAsync(It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Dictionary<string, string> { ["Cash"] = "เงินสด" });

        return names.Object;
    }

    private static SalesPage PaidBy(string method) =>
        new([new InvoiceSummaryDto(Guid.NewGuid(), 1042, new DateTime(2026, 10, 15, 2, 0, 0, DateTimeKind.Utc), 100m, method, 1)],
            1, InvoiceHistoryViewModel.PageSize, HasMore: false);
```

Add the tests, beside the other `ShowToday` tests:

```csharp
    [Fact]
    public async Task ShowToday_WithAMethodThatHasNoName_ShowsItsCode()
    {
        _client.Setup(c => c.ListSalesAsync(Today, Today, 1, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()))
               .ReturnsAsync(PaidBy("Unknown"));
        var vm = History(Manager);

        await vm.ShowTodayCommand.ExecuteAsync(null);

        vm.Bills.Single().PaymentMethodName.Should()
                                           .Be("Unknown");
    }

    [Fact]
    public async Task ShowToday_WithAKnownMethod_ShowsItsName()
    {
        _client.Setup(c => c.ListSalesAsync(Today, Today, 1, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()))
               .ReturnsAsync(PaidBy("Cash"));
        var vm = History(Manager);

        await vm.ShowTodayCommand.ExecuteAsync(null);

        vm.Bills.Single().PaymentMethodName.Should()
                                           .Be("เงินสด");
    }
```

- [ ] **Step 2: Run them to see them fail**

Run: `dotnet test tests/IndyPOS.Presentation.Tests --filter "FullyQualifiedName~InvoiceHistoryViewModelTests"`
Expected: build FAILS — the constructor takes 4 arguments, and `InvoiceRow` has no `PaymentMethodName`.

- [ ] **Step 3: Implement**

In `InvoiceHistoryViewModel.cs`:

1. The record:

```csharp
/// <summary>A bill in the list, its time already store-local and its payment method by name.</summary>
public sealed record InvoiceRow(Guid Id, long InvoiceNumber, DateTime CreatedAt, decimal TotalAmount,
                                string PaymentMethodName, int LineCount);
```

2. A field `private readonly IPaymentMethodNames _paymentMethodNames;`, a fifth constructor parameter `IPaymentMethodNames paymentMethodNames`, assigned in the constructor.

3. In `LoadPageAsync`, between the `ListSalesAsync` call and the cancellation check:

```csharp
            var result = await _client.ListSalesAsync(range.From, range.To, page, PageSize, token);
            var names = await _paymentMethodNames.GetAsync(token);
            if (token.IsCancellationRequested)
                return;
```

and the loop becomes `Bills.Add(ToRow(bill, names));`.

4. `ToRow`:

```csharp
    // A code with no name (names not loaded yet, or StoreHub's "Unknown") is shown as the code, never blank.
    private InvoiceRow ToRow(InvoiceSummaryDto bill, IReadOnlyDictionary<string, string> names) =>
        new(bill.Id, bill.InvoiceNumber, _clock.ToStoreTime(bill.CreatedUtc), bill.TotalAmount,
            names.TryGetValue(bill.PrimaryPaymentMethod, out var name) ? name : bill.PrimaryPaymentMethod,
            bill.LineCount);
```

5. `SalesHistoryViewModelFactory` does not build this ViewModel yet (Task 2 does), so nothing else changes. `dotnet build src/IndyPOS.Presentation` must succeed.

- [ ] **Step 4: Run them to see them pass**

Run: `dotnet test tests/IndyPOS.Presentation.Tests`
Expected: PASS — the 2 new tests and every existing one.

- [ ] **Step 5: Commit**

```bash
git add src/IndyPOS.Presentation/SalesHistory/InvoiceHistoryViewModel.cs tests/IndyPOS.Presentation.Tests/SalesHistory/InvoiceHistoryViewModelTests.cs
git commit -m "feat(till): the bill list names each bill's payment method"
```

---

### Task 2: `CreateHistory` and `CreateTodayOnly`

**Files:**
- Modify: `src/IndyPOS.Presentation/SalesHistory/SalesHistoryViewModelFactory.cs`
- Create: `tests/IndyPOS.Windows.Forms.Tests/UI/Report/SalesHistoryFixture.cs`
- Modify: `tests/IndyPOS.Presentation.Tests/SalesHistory/SalesHistoryViewModelFactoryTests.cs`, `tests/IndyPOS.Windows.Forms.Tests/UI/Report/InvoiceDetailFormTests.cs`

**Interfaces:**
- Consumes: Task 1's `InvoiceHistoryViewModel` constructor.
- Produces:
  - `SalesHistoryViewModelFactory(IStoreHubClient client, IReceiptPrinterService printer, ReceiptDocumentFactory receipts, StoreClock clock, ICurrentUser currentUser, IPaymentMethodNames paymentMethodNames)` (the new parameter is last).
  - `InvoiceDetailViewModel CreateDetail()` (unchanged), `InvoiceHistoryViewModel CreateHistory()`, `InvoiceHistoryViewModel CreateTodayOnly()`; each throws `InvalidOperationException(NotLoggedIn)` with nobody logged in.
  - Test helper `SalesHistoryFixture` (Forms tests): `NowUtc`, `Today`, `Bangkok`, `Factory(IStoreHubClient, ICurrentUser)`, `LoggedIn(UserRole)`, `NobodyLoggedIn()`, `Features(StoreFeaturesDto)`, `Page(bool hasMore, params long[] numbers)`, `Find(Control, string)`.
- DI: none needed (`IPaymentMethodNames` is already registered by `AddStoreHubClientServices`).

- [ ] **Step 1: Write the failing tests**

In `SalesHistoryViewModelFactoryTests`, change the `Factory()` helper to pass names as the last argument:

```csharp
    private SalesHistoryViewModelFactory Factory() =>
        new(Mock.Of<IStoreHubClient>(), Mock.Of<IReceiptPrinterService>(), new ReceiptDocumentFactory(Bangkok),
            new StoreClock(new FakeTimeProvider(NowUtc), Bangkok), _currentUser.Object, Mock.Of<IPaymentMethodNames>());
```

and add:

```csharp
    [Fact]
    public void CreateHistory_WithNobodyLoggedIn_ThrowsNotLoggedIn()
    {
        var act = () => Factory().CreateHistory();

        act.Should()
           .Throw<InvalidOperationException>()
           .WithMessage(SalesHistoryViewModelFactory.NotLoggedIn);
    }

    [Fact]
    public void CreateTodayOnly_ForAStoreManager_HoldsReprintToToday()
    {
        _currentUser.SetupGet(c => c.User).Returns(User((int)UserRole.StoreManager));
        var history = Factory().CreateTodayOnly();

        history.Detail.Show(Bill(new DateTime(2026, 10, 14, 2, 0, 0, DateTimeKind.Utc)));

        history.Detail.ReprintCommand.CanExecute(null).Should()
                                                      .BeFalse();
    }

    [Fact]
    public void CreateTodayOnly_ForAStoreManager_IsTodayOnly()
    {
        _currentUser.SetupGet(c => c.User).Returns(User((int)UserRole.StoreManager));

        var history = Factory().CreateTodayOnly();

        history.IsTodayOnly.Should()
                           .BeTrue();
    }

    [Fact]
    public void CreateHistory_ForAStoreManager_CanPickAPeriod()
    {
        _currentUser.SetupGet(c => c.User).Returns(User((int)UserRole.StoreManager));

        var history = Factory().CreateHistory();

        history.ShowThisMonthCommand.CanExecute(null).Should()
                                                     .BeTrue();
    }
```

Create `tests/IndyPOS.Windows.Forms.Tests/UI/Report/SalesHistoryFixture.cs`. C1's `InvoiceDetailFormTests` built a factory by hand; Tasks 3–5 need it in three more test classes, so it moves here now:

```csharp
using System.Windows.Forms;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.Common.Receipts;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Presentation.SalesHistory;
using IndyPOS.Windows.Forms.Services;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace IndyPOS.Windows.Forms.Tests.UI.Report;

/// <summary>Builders for the till's sales-history tests. Now is 10:00 on 2026-10-15 in Bangkok.</summary>
internal static class SalesHistoryFixture
{
    public static readonly DateTimeOffset NowUtc = new(2026, 10, 15, 3, 0, 0, TimeSpan.Zero);
    public static readonly DateOnly Today = new(2026, 10, 15);
    public static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    public static SalesHistoryViewModelFactory Factory(IStoreHubClient client, ICurrentUser currentUser) => new(
        client, Mock.Of<IReceiptPrinterService>(), new ReceiptDocumentFactory(Bangkok),
        new StoreClock(new FakeTimeProvider(NowUtc), Bangkok), currentUser, Names());

    public static ICurrentUser LoggedIn(UserRole role)
    {
        var user = new Mock<ILoggedInUser>();
        user.SetupGet(u => u.FirstName).Returns("Somchai");
        user.SetupGet(u => u.LastName).Returns(role.ToString());
        user.SetupGet(u => u.RoleId).Returns((int)role);

        var current = new Mock<ICurrentUser>();
        current.SetupGet(c => c.User).Returns(user.Object);

        return current.Object;
    }

    public static ICurrentUser NobodyLoggedIn() => new Mock<ICurrentUser>().Object;

    public static IStoreFeaturesProvider Features(StoreFeaturesDto features)
    {
        var provider = new Mock<IStoreFeaturesProvider>();
        provider.Setup(p => p.GetAsync()).ReturnsAsync(features);

        return provider.Object;
    }

    // Every bill at 02:00 UTC = 09:00 Bangkok today.
    public static SalesPage Page(bool hasMore, params long[] numbers) =>
        new(numbers.Select(n => new InvoiceSummaryDto(Guid.NewGuid(), n, new DateTime(2026, 10, 15, 2, 0, 0, DateTimeKind.Utc),
                                                      100m, "Cash", 1)).ToList(),
            1, InvoiceHistoryViewModel.PageSize, hasMore);

    public static Control Find(Control root, string name) => root.Controls.Find(name, searchAllChildren: true).Single();

    private static IPaymentMethodNames Names()
    {
        var names = new Mock<IPaymentMethodNames>();
        names.Setup(n => n.GetAsync(It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Dictionary<string, string> { ["Cash"] = "เงินสด" });

        return names.Object;
    }
}
```

In `InvoiceDetailFormTests`, replace the hand-built factory in `Form()` with the fixture (the tests themselves do not change):

```csharp
    private InvoiceDetailForm Form() =>
        new(SalesHistoryFixture.Factory(_client.Object, SalesHistoryFixture.LoggedIn(UserRole.StoreManager)),
            SalesHistoryFixture.Features(MimyMart));
```

Delete its now-unused `_currentUser` field and `Bangkok` constant, and any `using` that becomes unused.

- [ ] **Step 2: Run them to see them fail**

Run: `dotnet test tests/IndyPOS.Presentation.Tests --filter "FullyQualifiedName~SalesHistoryViewModelFactoryTests"`
Expected: build FAILS — the constructor takes 5 arguments, and there is no `CreateHistory` / `CreateTodayOnly`.

- [ ] **Step 3: Implement**

`src/IndyPOS.Presentation/SalesHistory/SalesHistoryViewModelFactory.cs`:

```csharp
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Receipts;

namespace IndyPOS.Presentation.SalesHistory;

/// <summary>
/// The sales-history ViewModels for whoever is logged in when a screen opens. Screens are built once,
/// before any login, so they ask here each time they open rather than holding a ViewModel.
/// </summary>
public sealed class SalesHistoryViewModelFactory(
    IStoreHubClient client,
    IReceiptPrinterService printer,
    ReceiptDocumentFactory receipts,
    StoreClock clock,
    ICurrentUser currentUser,
    IPaymentMethodNames paymentMethodNames)
{
    public const string NotLoggedIn = "No user is logged in on this till.";

    public InvoiceDetailViewModel CreateDetail() => Detail(Access());

    public InvoiceHistoryViewModel CreateHistory() => History(Access());

    /// <summary>
    /// บิลวันนี้ on the sale screen: today only, whoever opens it (spec §7), so a manager's reprint from
    /// there keeps to today too.
    /// </summary>
    public InvoiceHistoryViewModel CreateTodayOnly() => History(Access() with { CanViewAnyDay = false });

    private InvoiceHistoryViewModel History(SalesHistoryAccess access) =>
        new(client, clock, access, Detail(access), paymentMethodNames);

    private InvoiceDetailViewModel Detail(SalesHistoryAccess access) => new(client, printer, receipts, clock, access);

    private SalesHistoryAccess Access() =>
        SalesHistoryAccess.For(currentUser.User ?? throw new InvalidOperationException(NotLoggedIn));
}
```

- [ ] **Step 4: Run them to see them pass**

Run: `dotnet test tests/IndyPOS.Presentation.Tests`. Expected: PASS (4 new).
Run: `dotnet test tests/IndyPOS.Windows.Forms.Tests --filter "FullyQualifiedName~InvoiceDetailFormTests|FullyQualifiedName~TillHostTests"`. Expected: PASS — the C1 form tests on the fixture, and the container still resolves the factory (with its new `IPaymentMethodNames` dependency).

- [ ] **Step 5: Commit**

```bash
git add src/IndyPOS.Presentation/SalesHistory/SalesHistoryViewModelFactory.cs tests/IndyPOS.Presentation.Tests/SalesHistory/SalesHistoryViewModelFactoryTests.cs tests/IndyPOS.Windows.Forms.Tests/UI/Report/SalesHistoryFixture.cs tests/IndyPOS.Windows.Forms.Tests/UI/Report/InvoiceDetailFormTests.cs
git commit -m "feat(till): the factory builds the bill list, in full or for today only"
```

---

### Task 3: `InvoiceHistoryView`

**Files:**
- Create: `src/IndyPOS.Windows.Forms/UI/Report/TillStyle.cs`
- Modify: `src/IndyPOS.Windows.Forms/UI/Report/InvoiceDetailView.cs`, `TillGrid.cs` (use `TillStyle`)
- Create: `src/IndyPOS.Windows.Forms/UI/Report/InvoiceHistoryView.cs`
- Test: `tests/IndyPOS.Windows.Forms.Tests/UI/Report/InvoiceHistoryViewTests.cs`

**Interfaces:**
- Consumes: Task 1's `InvoiceRow`, Task 2's factory and fixture, C1's `CommandButton`, `InvoiceDetailView`, `TillGrid`.
- Produces:
  - `public static class TillStyle` — `const string FontName`, `static Color Background`, `static Color Accent`, `static Label Label(string name, float size = 12F)`, `static ModernButton Button(string name, string text, Size size)`.
  - `public sealed class InvoiceHistoryView : UserControl` — `void Bind(InvoiceHistoryViewModel viewModel)`, `bool ShowProductTypeSplit { get; set; }` (forwarded to the detail), `bool IsTodayOnly { get; }`, `const string TodayOnlyTitle = "บิลวันนี้"`. Control names: `ShowTodayButton`, `ShowThisMonthButton`, `ShowThisYearButton`, `FromDatePicker`, `ToDatePicker`, `ShowRangeButton`, `BillNumberTextBox`, `SearchButton`, `PeriodLabel`, `HistoryErrorLabel`, `BillsGrid`, `LoadMoreButton`, and the detail's own (`InvoiceDetailView`, `ReprintButton`, …).

The list's error label is `HistoryErrorLabel` because the detail view already has an `ErrorLabel` inside the same control tree.

- [ ] **Step 1: Write the failing tests**

`tests/IndyPOS.Windows.Forms.Tests/UI/Report/InvoiceHistoryViewTests.cs`:

```csharp
using System.Reflection;
using System.Windows.Forms;
using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Presentation.SalesHistory;
using IndyPOS.Windows.Forms.UI.Report;
using Moq;
using Xunit;
using static IndyPOS.Windows.Forms.Tests.UI.Report.SalesHistoryFixture;

namespace IndyPOS.Windows.Forms.Tests.UI.Report;

public class InvoiceHistoryViewTests
{
    private readonly Mock<IStoreHubClient> _client = new();

    [Fact]
    public void Bind_ForATodayOnlyList_HidesThePeriodPicker()
    {
        using var view = new InvoiceHistoryView();

        view.Bind(Factory(_client.Object, LoggedIn(UserRole.Cashier)).CreateHistory());

        Find(view, "ShowThisMonthButton").Visible.Should()
                                                 .BeFalse();
    }

    [Fact]
    public async Task Bind_ToASecondViewModel_IgnoresTheFirstsBills()
    {
        ListsToday(Page(hasMore: false, 1042));
        using var view = new InvoiceHistoryView();
        var factory = Factory(_client.Object, LoggedIn(UserRole.StoreManager));
        var first = factory.CreateHistory();
        view.Bind(first);
        view.Bind(factory.CreateHistory());

        await first.ShowTodayCommand.ExecuteAsync(null);

        Bills(view).Rows.Count.Should()
                              .Be(0);
    }

    [Fact]
    public void Search_WithTextThatIsNotANumber_ShowsTheError()
    {
        using var view = new InvoiceHistoryView();
        view.Bind(Factory(_client.Object, LoggedIn(UserRole.StoreManager)).CreateHistory());
        Find(view, "BillNumberTextBox").Text = "abc";

        ((Button)Find(view, "SearchButton")).PerformClick();

        Find(view, "HistoryErrorLabel").Text.Should()
                                            .Be(InvoiceHistoryViewModel.NotANumber);
    }

    [Fact]
    public async Task ShowToday_AfterAnotherList_ReplacesTheRows()
    {
        _client.SetupSequence(c => c.ListSalesAsync(Today, Today, 1, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()))
               .ReturnsAsync(Page(hasMore: false, 1042, 1041))
               .ReturnsAsync(Page(hasMore: false, 7));
        using var view = new InvoiceHistoryView();
        var vm = Factory(_client.Object, LoggedIn(UserRole.StoreManager)).CreateHistory();
        view.Bind(vm);
        await vm.ShowTodayCommand.ExecuteAsync(null);

        await vm.ShowTodayCommand.ExecuteAsync(null);

        Bills(view).Rows.Count.Should()
                              .Be(1);
    }

    [Fact]
    public async Task ShowToday_WithBills_ListsEach()
    {
        ListsToday(Page(hasMore: false, 1042, 1041));
        using var view = new InvoiceHistoryView();
        var vm = Factory(_client.Object, LoggedIn(UserRole.StoreManager)).CreateHistory();
        view.Bind(vm);

        await vm.ShowTodayCommand.ExecuteAsync(null);

        Bills(view).Rows.Count.Should()
                              .Be(2);
    }

    [Fact]
    public async Task ShowToday_WithABill_ShowsItsNumberAsOnTheReceipt()
    {
        ListsToday(Page(hasMore: false, 1042));
        using var view = new InvoiceHistoryView();
        var vm = Factory(_client.Object, LoggedIn(UserRole.StoreManager)).CreateHistory();
        view.Bind(vm);

        await vm.ShowTodayCommand.ExecuteAsync(null);

        Bills(view).Rows[0].Cells[0].FormattedValue.Should()
                                                   .Be("0000001042");
    }

    [Fact]
    public async Task LoadMore_WithAnotherPage_AppendsIt()
    {
        ListsToday(Page(hasMore: true, 1042));
        _client.Setup(c => c.ListSalesAsync(Today, Today, 2, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()))
               .ReturnsAsync(Page(hasMore: false, 1041));
        using var view = new InvoiceHistoryView();
        var vm = Factory(_client.Object, LoggedIn(UserRole.StoreManager)).CreateHistory();
        view.Bind(vm);
        await vm.ShowTodayCommand.ExecuteAsync(null);

        ((Button)Find(view, "LoadMoreButton")).PerformClick();

        Bills(view).Rows.Count.Should()
                              .Be(2);
    }

    [Fact]
    public async Task ClickingABill_OpensIt()
    {
        var page = Page(hasMore: false, 1042);
        ListsToday(page);
        using var view = new InvoiceHistoryView();
        var vm = Factory(_client.Object, LoggedIn(UserRole.StoreManager)).CreateHistory();
        view.Bind(vm);
        await vm.ShowTodayCommand.ExecuteAsync(null);

        ClickRow(Bills(view), 0);

        _client.Verify(c => c.GetSaleAsync(page.Items[0].Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    private void ListsToday(SalesPage page) =>
        _client.Setup(c => c.ListSalesAsync(Today, Today, 1, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()))
               .ReturnsAsync(page);

    private static DataGridView Bills(Control view) => (DataGridView)Find(view, "BillsGrid");

    // DataGridView raises CellClick only from a real mouse; this is the same event, without a window.
    private static void ClickRow(DataGridView grid, int row) =>
        typeof(DataGridView).GetMethod("OnCellClick", BindingFlags.Instance | BindingFlags.NonPublic)!
                            .Invoke(grid, [new DataGridViewCellEventArgs(0, row)]);
}
```

If `FormattedValue` cannot be read without a window handle, assert on `Bills(view).Rows[0].Cells[0].Value` being `1042L` **and** `Bills(view).Columns[0].DefaultCellStyle.Format` being `"0000000000"` instead, and ledger it.

- [ ] **Step 2: Run them to see them fail**

Run: `dotnet test tests/IndyPOS.Windows.Forms.Tests --filter "FullyQualifiedName~InvoiceHistoryViewTests"`
Expected: build FAILS — `InvoiceHistoryView` does not exist.

- [ ] **Step 3: Extract `TillStyle` (refactor, stays green)**

`InvoiceDetailView` and `TillGrid` each hold the till's font name and colours, and the new view needs them and the same label and button: this is the second copy, so they move to one place.

`src/IndyPOS.Windows.Forms/UI/Report/TillStyle.cs`:

```csharp
using IndyPOS.Windows.Forms.UI.ModernUI;

namespace IndyPOS.Windows.Forms.UI.Report;

/// <summary>The till's dark look, as the designer-built screens have it, for views built in code.</summary>
public static class TillStyle
{
    public const string FontName = "FC Subject [Non-commercial] Reg";
    public static readonly Color Background = Color.FromArgb(38, 38, 38);
    public static readonly Color Accent = Color.FromArgb(50, 190, 166);

    public static Label Label(string name, float size = 12F) => new()
    {
        Name = name,
        AutoSize = false,
        Height = (int)(size * 2.2F),
        ForeColor = Color.Gainsboro,
        Font = new Font(FontName, size),
        TextAlign = ContentAlignment.MiddleLeft
    };

    public static ModernButton Button(string name, string text, Size size) => new()
    {
        Name = name,
        Text = text,
        Size = size,
        BackColor = Background,
        BackgroundColor = Background,
        BorderColor = Accent,
        BorderRadius = 5,
        BorderSize = 1,
        FlatStyle = FlatStyle.Flat,
        Font = new Font(FontName, 14F),
        ForeColor = Color.White,
        TextColor = Color.White
    };
}
```

In `InvoiceDetailView`: delete `FontName`, `Background`, `Accent` and `TextLabel`; every `TextLabel(` becomes `TillStyle.Label(`; `Background` becomes `TillStyle.Background`; the reprint button becomes:

```csharp
    private readonly ModernButton _reprint = TillStyle.Button("ReprintButton", "พิมพ์ซ้ำ", new Size(180, 60));
```

with `_reprint.Dock = DockStyle.Right;` added at the top of the constructor. In `TillGrid`: delete `FontName` and `Background`; use `TillStyle.FontName` and `TillStyle.Background`.

This refactor must stay green, but Step 1's test file does not compile yet. Move `InvoiceHistoryViewTests.cs` out of the test project for this one run (e.g. rename it to `.cs.pending`), then:

Run: `dotnet test tests/IndyPOS.Windows.Forms.Tests --filter "FullyQualifiedName~InvoiceDetailViewTests|FullyQualifiedName~InvoiceDetailFormTests"`
Expected: PASS (unchanged behaviour). Rename the test file back before Step 4.

- [ ] **Step 4: Implement `InvoiceHistoryView`**

`src/IndyPOS.Windows.Forms/UI/Report/InvoiceHistoryView.cs`:

```csharp
using System.Collections.Specialized;
using System.ComponentModel;
using IndyPOS.Presentation.SalesHistory;
using IndyPOS.Windows.Forms.UI.ModernUI;
using IndyPOS.Windows.Forms.UI.Mvvm;

namespace IndyPOS.Windows.Forms.UI.Report;

/// <summary>
/// The bill list (spec §7): period buttons, a date range and the bill-number box on top, the bills on
/// the left with "load more", and the chosen bill on the right. It shows
/// <see cref="InvoiceHistoryViewModel"/>'s state and forwards clicks; the ViewModel decides everything.
/// </summary>
public sealed class InvoiceHistoryView : UserControl
{
    public const string TodayOnlyTitle = "บิลวันนี้";

    private static readonly Size PeriodButtonSize = new(120, 50);

    private readonly ModernButton _today = TillStyle.Button("ShowTodayButton", "วันนี้", PeriodButtonSize);
    private readonly ModernButton _thisMonth = TillStyle.Button("ShowThisMonthButton", "เดือนนี้", PeriodButtonSize);
    private readonly ModernButton _thisYear = TillStyle.Button("ShowThisYearButton", "ปีนี้", PeriodButtonSize);
    private readonly DateTimePicker _from = DatePicker("FromDatePicker");
    private readonly DateTimePicker _to = DatePicker("ToDatePicker");
    private readonly ModernButton _range = TillStyle.Button("ShowRangeButton", "แสดง", PeriodButtonSize);
    private readonly TextBox _billNumber = new()
    {
        Name = "BillNumberTextBox",
        Width = 180,
        Margin = new Padding(20, 12, 3, 3),
        Font = new Font(TillStyle.FontName, 14F)
    };
    private readonly ModernButton _search = TillStyle.Button("SearchButton", "ค้นหาบิล", PeriodButtonSize);
    private readonly Label _period = TillStyle.Label("PeriodLabel", 14F);
    private readonly Label _error = TillStyle.Label("HistoryErrorLabel");
    private readonly DataGridView _bills = TillGrid.Create("BillsGrid");
    private readonly ModernButton _loadMore = TillStyle.Button("LoadMoreButton", "โหลดเพิ่ม", PeriodButtonSize);
    private readonly InvoiceDetailView _detail = new() { Dock = DockStyle.Fill };
    private readonly List<IDisposable> _commandBindings = [];

    private InvoiceHistoryViewModel? _viewModel;

    public InvoiceHistoryView()
    {
        Name = nameof(InvoiceHistoryView);
        BackColor = TillStyle.Background;
        _error.ForeColor = Color.Salmon;

        TillGrid.AddColumn(_bills, "เลขที่บิล", 140, "0000000000");
        TillGrid.AddColumn(_bills, "เวลา", 170, "dd/MM/yyyy HH:mm");
        TillGrid.AddColumn(_bills, "ยอดรวม", 120, "N2");
        TillGrid.AddColumn(_bills, "ชำระโดย", 130);
        TillGrid.AddColumn(_bills, "รายการ", 70);

        _bills.CellClick += OnBillClicked;
        _billNumber.TextChanged += OnBillNumberChanged;
        _billNumber.KeyDown += OnBillNumberKeyDown;

        Controls.Add(_detail);
        Controls.Add(BillList());
        Controls.Add(StatusRow());
        Controls.Add(TopBar());
    }

    public bool ShowProductTypeSplit
    {
        get => _detail.ShowProductTypeSplit;
        set => _detail.ShowProductTypeSplit = value;
    }

    public bool IsTodayOnly => _viewModel?.IsTodayOnly == true;

    /// <summary>Follows this ViewModel from now on, and stops following the previous one.</summary>
    public void Bind(InvoiceHistoryViewModel viewModel)
    {
        Unbind();

        _viewModel = viewModel;
        viewModel.PropertyChanged += OnViewModelChanged;
        viewModel.Bills.CollectionChanged += OnBillsChanged;
        _commandBindings.Add(CommandButton.Bind(_today, viewModel.ShowTodayCommand));
        _commandBindings.Add(CommandButton.Bind(_thisMonth, viewModel.ShowThisMonthCommand));
        _commandBindings.Add(CommandButton.Bind(_thisYear, viewModel.ShowThisYearCommand));
        _commandBindings.Add(CommandButton.Bind(_range, viewModel.ShowRangeCommand, SelectedRange));
        _commandBindings.Add(CommandButton.Bind(_search, viewModel.SearchCommand));
        _commandBindings.Add(CommandButton.Bind(_loadMore, viewModel.LoadMoreCommand));
        _detail.Bind(viewModel.Detail);

        foreach (var control in PeriodPicker())
            control.Visible = !viewModel.IsTodayOnly;

        _billNumber.Text = viewModel.SearchText;
        RefillBills();
        Render();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Unbind();

        base.Dispose(disposing);
    }

    private void Unbind()
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelChanged;
            _viewModel.Bills.CollectionChanged -= OnBillsChanged;
        }

        _commandBindings.ForEach(binding => binding.Dispose());
        _commandBindings.Clear();
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e) => Render();

    // The ViewModel only appends a page or clears the list for a new period.
    private void OnBillsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null)
        {
            foreach (InvoiceRow row in e.NewItems)
                AddBill(row);
        }
        else
        {
            RefillBills();
        }

        Render();
    }

    private void OnBillClicked(object? sender, DataGridViewCellEventArgs e)
    {
        if (_viewModel is null || e.RowIndex < 0 || _bills.Rows[e.RowIndex].Tag is not InvoiceRow row)
            return;

        CommandButton.TryExecute(_viewModel.OpenCommand, row);
    }

    private void OnBillNumberChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
            _viewModel.SearchText = _billNumber.Text;
    }

    private void OnBillNumberKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter || _viewModel is null)
            return;

        e.SuppressKeyPress = true;
        CommandButton.TryExecute(_viewModel.SearchCommand, null);
    }

    private void Render()
    {
        _period.Text = PeriodText();
        _error.Text = _viewModel?.ErrorMessage ?? string.Empty;
        UseWaitCursor = _viewModel?.IsBusy == true;
    }

    private string PeriodText()
    {
        if (_viewModel is null)
            return string.Empty;
        if (_viewModel.IsTodayOnly)
            return TodayOnlyTitle;
        if (_viewModel.From == default)
            return string.Empty;

        return _viewModel.From == _viewModel.To
            ? $"{_viewModel.From:dd/MM/yyyy}"
            : $"{_viewModel.From:dd/MM/yyyy} - {_viewModel.To:dd/MM/yyyy}";
    }

    private void RefillBills()
    {
        _bills.Rows.Clear();

        foreach (var row in _viewModel?.Bills ?? [])
            AddBill(row);
    }

    private void AddBill(InvoiceRow row)
    {
        var index = _bills.Rows.Add(row.InvoiceNumber, row.CreatedAt, row.TotalAmount, row.PaymentMethodName, row.LineCount);
        _bills.Rows[index].Tag = row;
    }

    private object? SelectedRange() =>
        new DateRange(DateOnly.FromDateTime(_from.Value), DateOnly.FromDateTime(_to.Value));

    private Control[] PeriodPicker() => [_thisMonth, _thisYear, _from, _to, _range];

    private FlowLayoutPanel TopBar()
    {
        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 70,
            WrapContents = false,
            BackColor = TillStyle.Background,
            Padding = new Padding(10, 8, 10, 0)
        };
        bar.Controls.AddRange([_today, _thisMonth, _thisYear, _from, _to, _range, _billNumber, _search]);

        return bar;
    }

    private Panel StatusRow()
    {
        var row = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = TillStyle.Background, Padding = new Padding(10, 0, 10, 0) };
        _period.Dock = DockStyle.Left;
        _period.Width = 420;
        _error.Dock = DockStyle.Fill;
        row.Controls.Add(_error);
        row.Controls.Add(_period);

        return row;
    }

    private Panel BillList()
    {
        var list = new Panel { Dock = DockStyle.Left, Width = 650, BackColor = TillStyle.Background, Padding = new Padding(10, 5, 5, 10) };
        _loadMore.Dock = DockStyle.Bottom;
        list.Controls.Add(_bills);
        list.Controls.Add(_loadMore);

        return list;
    }

    private static DateTimePicker DatePicker(string name) => new()
    {
        Name = name,
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "dd/MM/yyyy",
        Width = 150,
        Margin = new Padding(10, 12, 3, 3),
        Font = new Font(TillStyle.FontName, 14F),
        Value = DateTime.Today
    };
}
```

- [ ] **Step 5: Run them to see them pass**

Run: `dotnet test tests/IndyPOS.Windows.Forms.Tests`
Expected: PASS — the 8 new tests, C1's detail tests after the `TillStyle` refactor, and `MoneyFormatTests` (every money format here is `N2`).

- [ ] **Step 6: Commit**

```bash
git add src/IndyPOS.Windows.Forms/UI/Report/TillStyle.cs src/IndyPOS.Windows.Forms/UI/Report/TillGrid.cs src/IndyPOS.Windows.Forms/UI/Report/InvoiceDetailView.cs src/IndyPOS.Windows.Forms/UI/Report/InvoiceHistoryView.cs tests/IndyPOS.Windows.Forms.Tests/UI/Report/InvoiceHistoryViewTests.cs
git commit -m "feat(till): InvoiceHistoryView lists bills by period or number and shows the chosen one"
```

---

### Task 4: Reports → ประวัติการขาย, rebuilt

**Files:**
- Create: `src/IndyPOS.Windows.Forms/Services/StoreFeaturesProviderExtensions.cs`
- Replace: `src/IndyPOS.Windows.Forms/UI/Report/SalesHistoryReportPanel.cs`; delete `SalesHistoryReportPanel.Designer.cs` and `SalesHistoryReportPanel.resx`
- Modify: `src/IndyPOS.Windows.Forms/UI/Report/InvoiceDetailForm.cs` (use `GetLayoutAsync`)
- Modify: `tests/IndyPOS.Windows.Forms.Tests/UI/Report/ReportPanelsLayoutTests.cs` (the panel's new constructor)
- Test: `tests/IndyPOS.Windows.Forms.Tests/Services/StoreFeaturesProviderExtensionsTests.cs`, `tests/IndyPOS.Windows.Forms.Tests/UI/Report/SalesHistoryReportPanelTests.cs`

**Interfaces:**
- Consumes: Task 2's `CreateHistory()`, Task 3's `InvoiceHistoryView`.
- Produces:
  - `public static Task<TillLayout> GetLayoutAsync(this IStoreFeaturesProvider features, string screen)` in `IndyPOS.Windows.Forms.Services` — the store's layout, or `TillLayout.WhenFeaturesUnavailable` (logged) when the features cannot load.
  - `public sealed class SalesHistoryReportPanel : UserControl`, ctor `(SalesHistoryViewModelFactory factory, IStoreFeaturesProvider features)`; `Task OpenAsync()`, run every time the panel becomes visible. Its `Name` stays `SalesHistoryReportPanel` (`ReportsPanel.SwitchToPanel` compares names).

- [ ] **Step 1: Write the failing tests**

`tests/IndyPOS.Windows.Forms.Tests/Services/StoreFeaturesProviderExtensionsTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Common.Models;
using IndyPOS.Windows.Forms.Services;
using Moq;
using Xunit;

namespace IndyPOS.Windows.Forms.Tests.Services;

public class StoreFeaturesProviderExtensionsTests
{
    [Fact]
    public async Task GetLayoutAsync_WhenTheFeaturesCannotLoad_ReturnsTheFallback()
    {
        var features = new Mock<IStoreFeaturesProvider>();
        features.Setup(f => f.GetAsync()).ThrowsAsync(new HttpRequestException("StoreHub is down"));

        var layout = await features.Object.GetLayoutAsync("the test screen");

        layout.Should()
              .BeSameAs(TillLayout.WhenFeaturesUnavailable);
    }

    [Fact]
    public async Task GetLayoutAsync_AtAStoreWithOneProductType_HidesTheSplit()
    {
        var features = new Mock<IStoreFeaturesProvider>();
        features.Setup(f => f.GetAsync()).ReturnsAsync(new StoreFeaturesDto(PayLaterEnabled: false, MultipleProductTypesEnabled: false));

        var layout = await features.Object.GetLayoutAsync("the test screen");

        layout.ShowProductTypeSplit.Should()
                                   .BeFalse();
    }
}
```

`tests/IndyPOS.Windows.Forms.Tests/UI/Report/SalesHistoryReportPanelTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using IndyPOS.Presentation.SalesHistory;
using IndyPOS.Windows.Forms.UI.Report;
using Moq;
using Xunit;
using static IndyPOS.Windows.Forms.Tests.UI.Report.SalesHistoryFixture;

namespace IndyPOS.Windows.Forms.Tests.UI.Report;

public class SalesHistoryReportPanelTests
{
    private static readonly StoreFeaturesDto MimyMart = new(PayLaterEnabled: false, MultipleProductTypesEnabled: false);

    private readonly Mock<IStoreHubClient> _client = new();

    public SalesHistoryReportPanelTests() =>
        _client.Setup(c => c.ListSalesAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<int>(), It.IsAny<int>(),
                                            It.IsAny<CancellationToken>()))
               .ReturnsAsync(Page(hasMore: false));

    [Fact]
    public async Task OpenAsync_WithNobodyLoggedIn_DoesNotThrow()
    {
        using var panel = new SalesHistoryReportPanel(Factory(_client.Object, NobodyLoggedIn()), Features(MimyMart));

        var open = () => panel.OpenAsync();

        await open.Should()
                  .NotThrowAsync();
    }

    [Fact]
    public async Task OpenAsync_AfterAnotherUserLogsIn_FollowsTheNewUser()
    {
        var current = new SwitchableUser(LoggedIn(UserRole.Cashier));
        using var panel = new SalesHistoryReportPanel(Factory(_client.Object, current), Features(MimyMart));
        await panel.OpenAsync();
        current.Now = LoggedIn(UserRole.SystemAdmin);

        await panel.OpenAsync();

        History(panel).IsTodayOnly.Should()
                                  .BeFalse();
    }

    [Fact]
    public async Task OpenAsync_AtAStoreWithoutTheSplit_HidesIt()
    {
        using var panel = new SalesHistoryReportPanel(Factory(_client.Object, LoggedIn(UserRole.StoreManager)), Features(MimyMart));

        await panel.OpenAsync();

        History(panel).ShowProductTypeSplit.Should()
                                           .BeFalse();
    }

    [Fact]
    public async Task OpenAsync_WhenOpened_ListsToday()
    {
        using var panel = new SalesHistoryReportPanel(Factory(_client.Object, LoggedIn(UserRole.StoreManager)), Features(MimyMart));

        await panel.OpenAsync();

        _client.Verify(c => c.ListSalesAsync(Today, Today, 1, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()),
                       Times.Once);
    }

    private static InvoiceHistoryView History(SalesHistoryReportPanel panel) =>
        (InvoiceHistoryView)Find(panel, "InvoiceHistoryView");

    // Another user logs in between two visits: the factory reads whoever is current now.
    private sealed class SwitchableUser(ICurrentUser now) : ICurrentUser
    {
        public ICurrentUser Now { get; set; } = now;

        public ILoggedInUser? User => Now.User;
    }
}
```

In `ReportPanelsLayoutTests.ReportsPanel_WhenShownWhileFeaturesLoad_ShowsTheOverviewAtOnce`, the history panel's line becomes:

```csharp
            new SalesHistoryReportPanel(SalesHistoryFixture.Factory(Mock.Of<IStoreHubClient>(), SalesHistoryFixture.NobodyLoggedIn()),
                                        pending.Object),
```

(Remove the `IStoreConstants` using if it is no longer used in that file.)

- [ ] **Step 2: Run them to see them fail**

Run: `dotnet test tests/IndyPOS.Windows.Forms.Tests --filter "FullyQualifiedName~SalesHistoryReportPanelTests|FullyQualifiedName~StoreFeaturesProviderExtensionsTests"`
Expected: build FAILS — no `GetLayoutAsync`, no `OpenAsync`, and the old constructor takes `(IReportService, IStoreConstants, MessageForm)`.

- [ ] **Step 3: Implement the extension**

`src/IndyPOS.Windows.Forms/Services/StoreFeaturesProviderExtensions.cs`:

```csharp
using IndyPOS.Application.Common.Models;
using Serilog;

namespace IndyPOS.Windows.Forms.Services;

public static class StoreFeaturesProviderExtensions
{
    /// <summary>
    /// The store's till layout, or the designed layout when the features cannot be fetched: a screen still
    /// opens, and StoreHub still refuses whatever this store may not do.
    /// </summary>
    public static async Task<TillLayout> GetLayoutAsync(this IStoreFeaturesProvider features, string screen)
    {
        try
        {
            return TillLayout.For(await features.GetAsync());
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not load store features for {Screen}", screen);
            return TillLayout.WhenFeaturesUnavailable;
        }
    }
}
```

In `InvoiceDetailForm`, delete `LayoutAsync()` (and its `Serilog` / `Models` usings if now unused); `LoadAsync`'s layout line becomes:

```csharp
        _view.ShowProductTypeSplit = (await _features.GetLayoutAsync("the bill detail")).ShowProductTypeSplit;
```

The other screens' own copies of this try/catch (`MainForm`, `ReportsPanel`, `SalePanel`, …) are left alone: out of scope.

- [ ] **Step 4: Rebuild the panel**

Delete `SalesHistoryReportPanel.Designer.cs` and `SalesHistoryReportPanel.resx` (the designer grids and the hidden `PrintReceiptButton` go with them). Replace `SalesHistoryReportPanel.cs` with:

```csharp
using IndyPOS.Presentation.SalesHistory;
using IndyPOS.Windows.Forms.Services;
using Serilog;

namespace IndyPOS.Windows.Forms.UI.Report;

/// <summary>
/// Reports → ประวัติการขาย (spec §7). Every visit gets a fresh list for whoever is logged in now,
/// opened on today.
/// </summary>
public sealed class SalesHistoryReportPanel : UserControl
{
    private readonly SalesHistoryViewModelFactory _factory;
    private readonly IStoreFeaturesProvider _features;
    private readonly InvoiceHistoryView _view = new() { Dock = DockStyle.Fill };

    public SalesHistoryReportPanel(SalesHistoryViewModelFactory factory, IStoreFeaturesProvider features)
    {
        _factory = factory;
        _features = features;

        Name = nameof(SalesHistoryReportPanel);
        BackColor = TillStyle.Background;
        Controls.Add(_view);
        VisibleChanged += OnVisibleChanged;
    }

    public async Task OpenAsync()
    {
        InvoiceHistoryViewModel viewModel;
        try
        {
            viewModel = _factory.CreateHistory();
        }
        catch (InvalidOperationException ex)
        {
            // Only reachable through a stale event after a logout; the screen stays empty.
            Log.Warning(ex, "Sales history opened with nobody logged in");
            return;
        }

        _view.ShowProductTypeSplit = (await _features.GetLayoutAsync("sales history")).ShowProductTypeSplit;
        _view.Bind(viewModel);

        await viewModel.ShowTodayCommand.ExecuteAsync(null);
    }

    private async void OnVisibleChanged(object? sender, EventArgs e)
    {
        if (Visible)
            await OpenAsync();
    }
}
```

`ConfigureServices.cs` already registers `.AddSingleton<SalesHistoryReportPanel>()`; it now resolves the factory and features provider instead.

- [ ] **Step 5: Run the tests and the container check**

Run: `dotnet test tests/IndyPOS.Windows.Forms.Tests`
Expected: PASS — 6 new tests, `ReportPanelsLayoutTests` with the new constructor, the C1 form tests after the `GetLayoutAsync` refactor, and `TillHostTests` (DI resolves the rebuilt panel).

- [ ] **Step 6: Commit**

```bash
git add -A src/IndyPOS.Windows.Forms/Services/StoreFeaturesProviderExtensions.cs src/IndyPOS.Windows.Forms/UI/Report tests/IndyPOS.Windows.Forms.Tests/Services/StoreFeaturesProviderExtensionsTests.cs tests/IndyPOS.Windows.Forms.Tests/UI/Report
git commit -m "feat(till): Reports → ประวัติการขาย lists real bills and opens each one"
```

---

### Task 5: The บิลวันนี้ dialog

**Files:**
- Create: `src/IndyPOS.Windows.Forms/UI/Report/TodaysBillsForm.cs`
- Test: `tests/IndyPOS.Windows.Forms.Tests/UI/Report/TodaysBillsFormTests.cs`

**Interfaces:**
- Consumes: Task 2's `CreateTodayOnly()`, Task 3's `InvoiceHistoryView`, Task 4's `GetLayoutAsync`.
- Produces: `public sealed class TodaysBillsForm : Form`, ctor `(SalesHistoryViewModelFactory factory, IStoreFeaturesProvider features)`; `Task LoadAsync()` (binds a fresh today-only ViewModel and lists today, without showing); `Task ShowTodaysBillsAsync()` (`LoadAsync` then `ShowDialog`; a call while one is in flight returns at once).

- [ ] **Step 1: Write the failing tests**

`tests/IndyPOS.Windows.Forms.Tests/UI/Report/TodaysBillsFormTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Presentation.SalesHistory;
using IndyPOS.Windows.Forms.UI.Report;
using Moq;
using Xunit;
using static IndyPOS.Windows.Forms.Tests.UI.Report.SalesHistoryFixture;

namespace IndyPOS.Windows.Forms.Tests.UI.Report;

/// <summary>LoadAsync only, or a load that never finishes: ShowDialog would block the test run.</summary>
public class TodaysBillsFormTests
{
    private static readonly StoreFeaturesDto GeneralHardware = new(PayLaterEnabled: true, MultipleProductTypesEnabled: true);

    private readonly Mock<IStoreHubClient> _client = new();

    // A second press while the first list loads must not open a second dialog. The first load never
    // finishes, so no ShowDialog runs.
    [Fact]
    public void ShowTodaysBillsAsync_WhileTheListLoads_IgnoresTheSecondRequest()
    {
        _client.Setup(c => c.ListSalesAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<int>(), It.IsAny<int>(),
                                            It.IsAny<CancellationToken>()))
               .Returns(new TaskCompletionSource<SalesPage>().Task);
        using var form = Form(UserRole.Cashier);
        _ = form.ShowTodaysBillsAsync();

        _ = form.ShowTodaysBillsAsync();

        _client.Verify(c => c.ListSalesAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<int>(), It.IsAny<int>(),
                                             It.IsAny<CancellationToken>()),
                       Times.Once);
    }

    [Fact]
    public async Task LoadAsync_ForAStoreManager_IsTodayOnly()
    {
        ListsToday();
        using var form = Form(UserRole.StoreManager);

        await form.LoadAsync();

        ((InvoiceHistoryView)Find(form, "InvoiceHistoryView")).IsTodayOnly.Should()
                                                                          .BeTrue();
    }

    [Fact]
    public async Task LoadAsync_WhenOpened_ListsToday()
    {
        ListsToday();
        using var form = Form(UserRole.Cashier);

        await form.LoadAsync();

        _client.Verify(c => c.ListSalesAsync(Today, Today, 1, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()),
                       Times.Once);
    }

    private void ListsToday() =>
        _client.Setup(c => c.ListSalesAsync(Today, Today, 1, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()))
               .ReturnsAsync(Page(hasMore: false, 1042));

    private TodaysBillsForm Form(UserRole role) =>
        new(Factory(_client.Object, LoggedIn(role)), Features(GeneralHardware));
}
```

- [ ] **Step 2: Run them to see them fail**

Run: `dotnet test tests/IndyPOS.Windows.Forms.Tests --filter "FullyQualifiedName~TodaysBillsFormTests"`
Expected: build FAILS — `TodaysBillsForm` does not exist.

- [ ] **Step 3: Implement**

`src/IndyPOS.Windows.Forms/UI/Report/TodaysBillsForm.cs`:

```csharp
using IndyPOS.Presentation.SalesHistory;
using IndyPOS.Windows.Forms.Services;

namespace IndyPOS.Windows.Forms.UI.Report;

/// <summary>
/// บิลวันนี้ (spec §7): the sales-history screen in today-only mode, opened from the sale screen, so a
/// cashier, who cannot open Reports, can find and reprint a bill from today.
/// </summary>
public sealed class TodaysBillsForm : Form
{
    private readonly SalesHistoryViewModelFactory _factory;
    private readonly IStoreFeaturesProvider _features;
    private readonly InvoiceHistoryView _view = new() { Dock = DockStyle.Fill };
    private bool _opening;

    public TodaysBillsForm(SalesHistoryViewModelFactory factory, IStoreFeaturesProvider features)
    {
        _factory = factory;
        _features = features;

        Name = nameof(TodaysBillsForm);
        Text = InvoiceHistoryView.TodayOnlyTitle;
        Size = new Size(1500, 900);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = TillStyle.Background;
        Controls.Add(_view);
    }

    /// <summary>Binds a fresh today-only ViewModel and lists today, without showing the dialog.</summary>
    public async Task LoadAsync()
    {
        var viewModel = _factory.CreateTodayOnly();
        _view.ShowProductTypeSplit = (await _features.GetLayoutAsync("today's bills")).ShowProductTypeSplit;
        _view.Bind(viewModel);

        await viewModel.ShowTodayCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Lists today and shows the dialog. A press while another is still loading or shown is ignored: the
    /// dialog appears only once its list loads, so a second press would otherwise open it twice.
    /// </summary>
    public async Task ShowTodaysBillsAsync()
    {
        if (_opening)
            return;

        _opening = true;
        try
        {
            await LoadAsync();
            ShowDialog();
        }
        finally
        {
            _opening = false;
        }
    }
}
```

`TodaysBillsForm` is a singleton (Task 6), so `ShowDialog` must not dispose it: closing a modal form hides it, and the next `LoadAsync` re-binds.

- [ ] **Step 4: Run them to see them pass**

Run: `dotnet test tests/IndyPOS.Windows.Forms.Tests --filter "FullyQualifiedName~TodaysBillsFormTests"`
Expected: PASS, 3.

- [ ] **Step 5: Commit**

```bash
git add src/IndyPOS.Windows.Forms/UI/Report/TodaysBillsForm.cs tests/IndyPOS.Windows.Forms.Tests/UI/Report/TodaysBillsFormTests.cs
git commit -m "feat(till): TodaysBillsForm shows today's bills in today-only mode"
```

---

### Task 6: บิลวันนี้ on the sale screen; Reports only for `reports.view`

`SalePanel` and `MainForm` are designer forms with no unit tests (`[ExcludeFromCodeCoverage]`, and constructing them needs the whole till). The rules they apply are already pinned: `SalesHistoryAccess.For(user).CanReprint` (plan B, `SalesHistoryAccessTests`) and `RoleCapabilities` for `reports.view` (`RoleCapabilitiesTests`). This task's gate is `TillHostTests` (the container resolves `SalePanel` with its new dependency) and Task 7's live check.

**Files:**
- Modify: `src/IndyPOS.Windows.Forms/UI/Sale/SalePanel.cs`
- Modify: `src/IndyPOS.Windows.Forms/UI/MainForm.cs`
- Modify: `src/IndyPOS.Windows.Forms/ConfigureServices.cs`

**Interfaces:**
- Consumes: Task 5's `TodaysBillsForm.ShowTodaysBillsAsync()`.
- Produces: a `TodaysBillsButton` (UIA id) on the sale screen, left of ยกเลิกการขาย; `ReportsButton` hidden for a user without `reports.view`.

- [ ] **Step 1: Register the dialog**

In `src/IndyPOS.Windows.Forms/ConfigureServices.cs`, beside `.AddSingleton<InvoiceDetailForm>()`, add `.AddSingleton<TodaysBillsForm>()` to the same chain.

- [ ] **Step 2: The บิลวันนี้ button**

In `SalePanel.cs` (add `using IndyPOS.Presentation.SalesHistory;`, `using IndyPOS.Windows.Forms.UI.Report;`, `using IndyPOS.Windows.Forms.UI.ModernUI;` if missing):

1. A field `private readonly TodaysBillsForm _todaysBillsForm;` and a field for the button:

```csharp
    // Built in code, beside the designer's ยกเลิกการขาย, so the designer file stays untouched.
    private readonly ModernButton _todaysBillsButton = TillStyle.Button("TodaysBillsButton", InvoiceHistoryView.TodayOnlyTitle, Size.Empty);
```

2. A constructor parameter `TodaysBillsForm todaysBillsForm` (after `IStoreFeaturesProvider storeFeatures`), assigned to the field, then `AddTodaysBillsButton();` after `InitializeInvoiceDataView(); InitializePaymentDataView();`.

3. The methods:

```csharp
    // Left of ยกเลิกการขาย, the same size and row. BringToFront: panel8's title label docks over that row,
    // and a control added after it would sit behind it.
    private void AddTodaysBillsButton()
    {
        _todaysBillsButton.Size = CancelSaleInvoiceButton.Size;
        _todaysBillsButton.Font = CancelSaleInvoiceButton.Font;
        _todaysBillsButton.BorderRadius = CancelSaleInvoiceButton.BorderRadius;
        _todaysBillsButton.Location = new Point(CancelSaleInvoiceButton.Left - CancelSaleInvoiceButton.Width - 12,
                                                CancelSaleInvoiceButton.Top);
        _todaysBillsButton.Anchor = CancelSaleInvoiceButton.Anchor;
        _todaysBillsButton.Visible = false;
        _todaysBillsButton.Click += TodaysBillsButton_Click;
        panel8.Controls.Add(_todaysBillsButton);
        _todaysBillsButton.BringToFront();
    }

    // Shown with sales.reprint (spec §7); every role has it today, but the rule is the capability.
    private void OnUserLoggedIn(ILoggedInUser user) =>
        _todaysBillsButton.Visible = SalesHistoryAccess.For(user).CanReprint;

    private async void TodaysBillsButton_Click(object? sender, EventArgs e)
    {
        // The dialog shows its own errors; this catches only the unexpected.
        try
        {
            await _todaysBillsForm.ShowTodaysBillsAsync();
        }
        catch (Exception ex)
        {
            _messageForm.ShowDialog($"เปิดบิลวันนี้ไม่สำเร็จ: {ex.Message}", "Error");
        }
    }
```

4. In `SubscribeEvents`, add `_eventAggregator.GetEvent<UserLoggedInEvent>().Subscribe(OnUserLoggedIn);` (method group: Prism holds subscribers weakly, and DI keeps this singleton alive).

- [ ] **Step 3: Reports only with `reports.view`**

StoreHub refuses every report route without `reports.view` (`RequireAuthorization("CanViewReports")`), so today a cashier who opens Reports gets only errors. In `MainForm.cs` (add `using IndyPOS.Application.Common.Authorization;`):

```csharp
	private bool CanViewReports() =>
		_loggedInUser is not null && RoleCapabilities.HasCapability(_loggedInUser.RoleId, Capability.ReportsView);
```

- `ReportsButton_Click`: `if (!_isUserLoggedIn || !CanViewReports()) return;`
- `ApplyMenu(bool showAccountsReceivable)` → `ApplyMenu(bool showAccountsReceivable, bool showReports)`: set `ReportsButton.Visible = showReports;` beside the AccountsReceivable line, and pass both to `ShownMenuButtons`.
- `ShownMenuButtons(bool showAccountsReceivable, bool showReports)`:

```csharp
	private Control[] ShownMenuButtons(bool showAccountsReceivable, bool showReports) =>
		AllMenuButtons().Where(button => (button != AccountsReceivableButton || showAccountsReceivable)
										 && (button != ReportsButton || showReports))
						.ToArray();
```

- `ApplyStoreLayoutAsync`: both calls pass `CanViewReports()` as the second argument.

- [ ] **Step 4: Build and run the till's tests**

Run: `dotnet build src/IndyPOS.Windows.Forms -c Release` — Expected: success.
Run: `dotnet test tests/IndyPOS.Windows.Forms.Tests` — Expected: PASS, including `TillHostTests` (the container resolves `SalePanel` → `TodaysBillsForm` → the factory) and `MoneyFormatTests`.

- [ ] **Step 5: Commit**

```bash
git add src/IndyPOS.Windows.Forms/UI/Sale/SalePanel.cs src/IndyPOS.Windows.Forms/UI/MainForm.cs src/IndyPOS.Windows.Forms/ConfigureServices.cs
git commit -m "feat(till): บิลวันนี้ on the sale screen; Reports only for users who may view them"
```

---

### Task 7: Remove the invoice stubs; live check; docs; PR

**Files:**
- Modify: `src/IndyPOS.Application/Common/Interfaces/IReportService.cs`, `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubReportService.cs`, `src/IndyPOS.Application/Common/Models/LegacyDtos.cs`
- Modify: `CLAUDE.md`, `ONBOARDING.md` (counts)

- [ ] **Step 1: Remove the stubs (the compiler is the test)**

A removal has no failing test to write: the build fails if anything still calls what is removed. With Task 4 merged, nothing should.

1. `IReportService`: delete `GetInvoicesByPeriodAsync`, `GetInvoicesByDateRangeAsync`, `GetInvoiceProductsByInvoiceIdAsync`, `GetPaymentsByInvoiceIdAsync`. `GetPayLaterPaymentsByPeriodAsync` and the summaries stay (spec §9).
2. `StoreHubReportService`: delete the same four implementations. Keep the `// ===` comment block above the stubs that remain, reworded to name only what is left: `GetPayLaterPaymentsByPeriodAsync returns empty: cash flow still calls it, and slice 3 replaces it.`
3. `LegacyDtos.cs`: delete `InvoiceDto`, `InvoiceProductDto` (with its `GetTotal`) and `InvoicePaymentDto`. `PayLaterPaymentDto`, `UserDto` and `UserCredentialDto` stay.
4. `grep -rn "InvoiceDto\b\|InvoiceProductDto\|InvoicePaymentDto\|GetInvoicesByPeriodAsync\|GetInvoicesByDateRangeAsync\|GetInvoiceProductsByInvoiceIdAsync\|GetPaymentsByInvoiceIdAsync" src tests --include=*.cs` returns nothing (word-boundary `InvoiceDto\b` so `InvoiceDetailDto` does not match).

Run: `dotnet build` then `dotnet test tests/IndyPOS.Windows.Forms.Tests` and `dotnet test tests/IndyPOS.Application.Tests`. Expected: success, PASS.

```bash
git add src/IndyPOS.Application/Common/Interfaces/IReportService.cs src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubReportService.cs src/IndyPOS.Application/Common/Models/LegacyDtos.cs
git commit -m "refactor(till): remove the stubbed invoice report calls and their legacy DTOs"
```

- [ ] **Step 2: Live check (agent-driven, `testing-indypos` skill)**

Docker up; `up.sh GeneralHardware`. Tell Pond to keep hands off for ~10 minutes. Count `invoice_reprint` rows first.

1. **Cashier** (`cashier` / `cashier123`): the menu has **no รายงาน** button; the sale screen shows **บิลวันนี้** left of ยกเลิกการขาย, not overlapped.
2. Ring up and save a cash sale (`cash-sale.steps`). Press บิลวันนี้: the dialog lists today's bills, newest first, the new one on top with its number, store time, total and **เงินสด** (the name, not `Cash`). No เดือนนี้ / ปีนี้ / date range.
3. Click the new bill: the detail shows it; press พิมพ์ซ้ำ once: one new `<number>-copy-*.png`, `invoice_reprint` +1.
4. In the bill-number box type `abc` and press Enter: `กรุณาใส่เลขที่บิลเป็นตัวเลข`. Type the new bill's number and press Enter: the detail shows it.
5. Close the dialog, log out, log in as **manager**: รายงาน is back; Reports → ประวัติการขาย lists today; เดือนนี้ lists this month; the period label follows.
6. ลงบัญชี double-click (C1) still opens its bill.

Screenshot each step with the driver's `shot`; record bill numbers and PNG names. Any step that cannot be driven is recorded as such, with the test that covers it.

- [ ] **Step 3: Full run, docs, PR**

- `dotnet test --logger trx --results-directory <scratchpad>/trx-plan-c2`; every suite green; record per-suite counts.
- `CLAUDE.md` and `ONBOARDING.md`: measured counts (Presentation, Windows.Forms, total; Application if it moved), in the same style as C1's update.
- Commit `docs: measured counts after the sales-history tab`, push `feat/sales-history-tab`, open the PR (title `feat(till): sales history and บิลวันนี้ on real bills`) with the live-check evidence. No `--auto` until Codex has reviewed.

---

## Self-Review

**Spec coverage (§7):** History mode with Today / This Month / This Year / range — Task 3 (view) over plan B's ViewModel; today-only mode with no period picker — Tasks 2, 3; bill-number search, Enter searches — Task 3 (Enter live in Task 7); load more — Task 3; `SalesHistoryReportPanel` rebuilt with the list left and `InvoiceDetailView` right — Tasks 3, 4; บิลวันนี้ shown with `sales.reprint`, opening today-only — Tasks 5, 6; "Cashiers cannot open Reports" — Task 6; Removed stubs — Task 7 (`GetInvoiceInfoAsync` / `StubInvoiceInfo` already gone). Binding through `CommandButton` — Task 3.
**Beyond the spec, decided here:** payment-method names in the list (Task 1: the list would otherwise show raw codes); `TillStyle` and `GetLayoutAsync` extracted at their second copy (Tasks 3, 4); Reports hidden rather than only guarded (Task 6: a dead button is worse than none).
**Placeholders:** none. One step offers a stated fallback (`FormattedValue` without a handle).
**Type consistency:** `InvoiceRow(…, PaymentMethodName, …)`; `InvoiceHistoryViewModel(client, clock, access, detail, paymentMethodNames)`; `SalesHistoryViewModelFactory(client, printer, receipts, clock, currentUser, paymentMethodNames)` with `CreateDetail/CreateHistory/CreateTodayOnly`; `InvoiceHistoryView.Bind/ShowProductTypeSplit/IsTodayOnly/TodayOnlyTitle`; `GetLayoutAsync(features, screen)`; `SalesHistoryReportPanel(factory, features).OpenAsync`; `TodaysBillsForm(factory, features).LoadAsync/ShowTodaysBillsAsync` — same everywhere.
**Review Focus:** 1 → Task 2; 2 → Task 3; 3 → Task 4; 4 → Task 1; 5 → Task 5.
