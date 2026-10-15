# Invoice History, Till Plan C1 — The Bill Detail Screen Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A till user can open any bill from the ลงบัญชี (PayLater) screen, see it in full, and reprint it as a marked copy. A DEBUG till saves every receipt as a PNG instead of printing.

**Architecture:** WinForms screens bind to plan B's ViewModels through two small seams: `CommandButton` (a button follows `CanExecute` and only ever runs an executable command) and `SalesHistoryViewModelFactory` (builds ViewModels for whoever is logged in, read from a new `ICurrentUser`). `InvoiceDetailView` is a code-built `UserControl` with no logic of its own; `InvoiceDetailForm` hosts it as a dialog and replaces the stubbed `SaleHistoryByInvoiceIdForm`. The RELEASE print gate moves from a `[Conditional]` in one form into the composition root: RELEASE registers the thermal printer, DEBUG a `ReceiptImagePrinter` that writes PNGs.

**Tech Stack:** .NET 10 WinForms, CommunityToolkit.Mvvm 8.4.2 (via `IndyPOS.Presentation`), Prism events, xUnit 2.9.3, Moq 4.20.72, FluentAssertions 8.8.0.

**Spec:** `docs/superpowers/specs/2026-09-27-invoice-history-v4-design.md` §7 (screens, binding), §8 (print gate). Plan 3 is split into: A — receipts (#127), B — ViewModels (#129), **C1 — bill detail screen (this plan)**, C2 — sales-history tab, บิลวันนี้ button, stub removal (planned after C1 merges).

## Global Constraints

- Screens "show ViewModel state and forward clicks only, with no logic in code-behind" (spec §7).
- `InvoiceDetailView` (UserControl): a header with bill number, date/time, cashier and badges; lines and payments grids, then totals; a **พิมพ์ซ้ำ** button, and space kept for the void button (spec §7).
- `SaleHistoryByInvoiceIdForm` (opened from the PayLater panel) becomes a dialog hosting `InvoiceDetailView`, keyed by the debt's Guid `InvoiceId` (spec §7).
- Binding: WinForms on `INotifyPropertyChanged`; commands through a small Click → Execute helper with `Enabled` following `CanExecute` (spec §7). The helper **must check `CanExecute` before executing**: toolkit `Execute` does not, and a double-click would write two reprint records (plan B review).
- The `[Conditional("RELEASE")]` print gate's effect is kept: a DEBUG till never sends to a printer (spec §8, plan B note).
- Bill number shown as `0000000000` (as on the receipt); money `N2` (`MoneyFormatTests` pins it); times in store time (the ViewModel already converts).
- The Hardware / General split is shown only where `TillLayout.ShowProductTypeSplit` (GeneralHardware).
- Every control a test or the UI driver needs has a stable `Name` (it becomes the UIA AutomationId).
- Tests: `Subject_WhenScenario_DirectVerbOutcome`, negative first, one behaviour per test. **No test may open a modal dialog** (`ShowDialog`, `MessageForm`): a modal blocks the test run on the desktop.

## Review Focus

1. **A double-click on พิมพ์ซ้ำ.** One reprint record and one receipt, not two. Pinned in Task 1 (`TryExecute_WhileAnAsyncCommandRuns_DoesNotRunItAgain`).
2. **The PayLater bill of another day, opened by a cashier.** StoreHub answers 404 for a today-only caller; the dialog shows `ไม่พบบิล`, with no crash and no blank grid pretending to be a bill. Pinned in Task 4 and live in Task 6.
3. **Opening a second bill in the same dialog instance.** The view follows the new ViewModel only; the old one's late answer or `CanExecuteChanged` must not touch the screen. Pinned in Task 4 (`Bind_ToASecondViewModel_StopsFollowingTheFirst`).
4. **Nobody logged in when a screen asks for a ViewModel** (a stale event after logout). A clear `InvalidOperationException`, not a `NullReferenceException`. Pinned in Task 2.
5. **A DEBUG sale.** The sale's receipt is saved as a PNG named with its real bill number, proving plan A end to end. Pinned in Task 3 and live in Task 6.

---

## File Structure

| File | Responsibility |
|---|---|
| Create `src/IndyPOS.Windows.Forms/UI/Mvvm/CommandButton.cs` | Bind a button to an `ICommand`: `Enabled` follows `CanExecute`; a click runs the command only if it can execute |
| Create `src/IndyPOS.Application/Common/Interfaces/ICurrentUser.cs` | Who is logged in on this till, or null |
| Create `src/IndyPOS.Infrastructure/Services/EventCurrentUser.cs` | `ICurrentUser` from the login/logout events |
| Create `src/IndyPOS.Presentation/SalesHistory/SalesHistoryViewModelFactory.cs` | ViewModels for the logged-in user |
| Create `src/IndyPOS.Infrastructure/Services/ReceiptImagePrinter.cs` | DEBUG `IReceiptPrinterService`: a PNG per receipt |
| Create `src/IndyPOS.Infrastructure/Services/ReceiptHeaders.cs` | Store config → `ReceiptHeader`, shared by both printers |
| Create `src/IndyPOS.Windows.Forms/UI/Report/TillGrid.cs` | The till's dark grid style, in code |
| Create `src/IndyPOS.Windows.Forms/UI/Report/InvoiceDetailView.cs` | One bill, bound to `InvoiceDetailViewModel` |
| Create `src/IndyPOS.Windows.Forms/UI/Report/InvoiceDetailForm.cs` | Dialog hosting the view |
| Delete `src/IndyPOS.Windows.Forms/UI/Report/SaleHistoryByInvoiceIdForm.cs`, `.Designer.cs`, `.resx` | Replaced |
| Modify `ReceiptPrinterService.cs`, `PrintReceiptForm.cs`, `PayLaterPaymentPanel.cs`, both `ConfigureServices.cs`, `TillHost.cs`, `IndyPOS.Windows.Forms.csproj` | Wiring |

**Branch:** `feat/bill-detail-screen` from `development`.

---

### Task 1: `CommandButton`

**Files:**
- Create: `src/IndyPOS.Windows.Forms/UI/Mvvm/CommandButton.cs`
- Modify: `src/IndyPOS.Windows.Forms/IndyPOS.Windows.Forms.csproj` (reference `IndyPOS.Presentation`)
- Test: `tests/IndyPOS.Windows.Forms.Tests/UI/Mvvm/CommandButtonTests.cs`

**Interfaces:**
- Produces: `public static class CommandButton` in `IndyPOS.Windows.Forms.UI.Mvvm`:
  - `IDisposable Bind(Control button, ICommand command, Func<object?>? parameter = null)` — sets `Enabled` now and on every `CanExecuteChanged`; a `Click` calls `TryExecute`. Disposing unsubscribes both.
  - `bool TryExecute(ICommand command, object? parameter)` — runs the command only if `CanExecute(parameter)`; returns whether it ran.

- [ ] **Step 1: Reference Presentation from the till**

In `src/IndyPOS.Windows.Forms/IndyPOS.Windows.Forms.csproj`, beside the Application and Infrastructure references (line ~100):

```xml
    <ProjectReference Include="..\IndyPOS.Presentation\IndyPOS.Presentation.csproj" />
```

Run `dotnet build src/IndyPOS.Windows.Forms`. Expected: success (Presentation is net10.0, the till net10.0-windows).

- [ ] **Step 2: Write the failing tests**

`tests/IndyPOS.Windows.Forms.Tests/UI/Mvvm/CommandButtonTests.cs`:

```csharp
using System.Windows.Forms;
using CommunityToolkit.Mvvm.Input;
using FluentAssertions;
using IndyPOS.Windows.Forms.UI.Mvvm;
using Xunit;

namespace IndyPOS.Windows.Forms.Tests.UI.Mvvm;

/// <summary>
/// The toolkit's Execute does not check CanExecute, and a disabled button can still receive a queued
/// second click. Every button bound to a ViewModel goes through here, so a double-click on พิมพ์ซ้ำ
/// records one reprint, not two.
/// </summary>
public class CommandButtonTests
{
    [Fact]
    public void Bind_WhenTheCommandCannotExecute_DisablesTheButton()
    {
        using var button = new Button();

        using var binding = CommandButton.Bind(button, new RelayCommand(() => { }, () => false));

        button.Enabled.Should()
                      .BeFalse();
    }

    [Fact]
    public void TryExecute_WhenTheCommandCannotExecute_DoesNotRunIt()
    {
        var runs = 0;
        var command = new RelayCommand(() => runs++, () => false);

        CommandButton.TryExecute(command, null);

        runs.Should()
            .Be(0);
    }

    [Fact]
    public void TryExecute_WhileAnAsyncCommandRuns_DoesNotRunItAgain()
    {
        var runs = 0;
        var pending = new TaskCompletionSource();
        var command = new AsyncRelayCommand(() => { runs++; return pending.Task; });
        CommandButton.TryExecute(command, null);

        CommandButton.TryExecute(command, null);

        runs.Should()
            .Be(1);
    }

    [Fact]
    public void Dispose_AfterBind_StopsFollowingTheCommand()
    {
        using var button = new Button();
        var canExecute = true;
        var command = new RelayCommand(() => { }, () => canExecute);
        var binding = CommandButton.Bind(button, command);
        binding.Dispose();
        canExecute = false;

        command.NotifyCanExecuteChanged();

        button.Enabled.Should()
                      .BeTrue();
    }

    [Fact]
    public void Bind_WhenCanExecuteChanges_FollowsIt()
    {
        using var button = new Button();
        var canExecute = false;
        var command = new RelayCommand(() => { }, () => canExecute);
        using var binding = CommandButton.Bind(button, command);
        canExecute = true;

        command.NotifyCanExecuteChanged();

        button.Enabled.Should()
                      .BeTrue();
    }

    [Fact]
    public void TryExecute_WithAParameter_PassesIt()
    {
        object? received = null;
        var command = new RelayCommand<string>(value => received = value);

        CommandButton.TryExecute(command, "1042");

        received.Should()
                .Be("1042");
    }
}
```

- [ ] **Step 3: Run them to see them fail**

Run: `dotnet test tests/IndyPOS.Windows.Forms.Tests --filter "FullyQualifiedName~CommandButtonTests"`
Expected: build FAILS — `CommandButton` does not exist.

- [ ] **Step 4: Implement**

`src/IndyPOS.Windows.Forms/UI/Mvvm/CommandButton.cs`:

```csharp
using System.Windows.Input;

namespace IndyPOS.Windows.Forms.UI.Mvvm;

/// <summary>
/// Binds a button to a ViewModel command (spec §7's Click → Execute helper): Enabled follows
/// CanExecute, and a click runs the command only if it can execute now. The toolkit's Execute does
/// not check, so without this a double-click could run an async command twice.
/// </summary>
public static class CommandButton
{
    public static IDisposable Bind(Control button, ICommand command, Func<object?>? parameter = null)
    {
        object? Parameter() => parameter?.Invoke();
        void Refresh(object? sender, EventArgs e) => button.Enabled = command.CanExecute(Parameter());
        void Click(object? sender, EventArgs e) => TryExecute(command, Parameter());

        command.CanExecuteChanged += Refresh;
        button.Click += Click;
        Refresh(null, EventArgs.Empty);

        return new Unbinding(() =>
        {
            command.CanExecuteChanged -= Refresh;
            button.Click -= Click;
        });
    }

    public static bool TryExecute(ICommand command, object? parameter)
    {
        if (!command.CanExecute(parameter))
            return false;

        command.Execute(parameter);
        return true;
    }

    private sealed class Unbinding(Action unbind) : IDisposable
    {
        private Action? _unbind = unbind;

        public void Dispose()
        {
            _unbind?.Invoke();
            _unbind = null;
        }
    }
}
```

- [ ] **Step 5: Run them to see them pass**

Run: `dotnet test tests/IndyPOS.Windows.Forms.Tests --filter "FullyQualifiedName~CommandButtonTests"`
Expected: PASS, 6 tests.

- [ ] **Step 6: Commit**

```bash
git add src/IndyPOS.Windows.Forms/IndyPOS.Windows.Forms.csproj src/IndyPOS.Windows.Forms/UI/Mvvm tests/IndyPOS.Windows.Forms.Tests/UI/Mvvm
git commit -m "feat(till): CommandButton binds a button to a ViewModel command and never runs it while it cannot"
```

---

### Task 2: `ICurrentUser` and `SalesHistoryViewModelFactory`

Every till panel is a singleton built before anyone logs in, and five classes each keep their own copy of the logged-in user from the Prism events. The ViewModels need the user at the moment a screen opens, so this adds one shared source rather than a sixth copy. The existing five are left alone (out of scope).

**Files:**
- Create: `src/IndyPOS.Application/Common/Interfaces/ICurrentUser.cs`
- Create: `src/IndyPOS.Infrastructure/Services/EventCurrentUser.cs`
- Create: `src/IndyPOS.Presentation/SalesHistory/SalesHistoryViewModelFactory.cs`
- Modify: `src/IndyPOS.Infrastructure/ConfigureServices.cs` (`AddInfrastructureServices`, beside `IEventAggregator`)
- Modify: `src/IndyPOS.Windows.Forms/ConfigureServices.cs` (`AddUIServices`)
- Test: `tests/IndyPOS.Application.Tests/Infrastructure/EventCurrentUserTests.cs`, `tests/IndyPOS.Presentation.Tests/SalesHistory/SalesHistoryViewModelFactoryTests.cs`

**Interfaces:**
- Produces:
  - `public interface ICurrentUser { ILoggedInUser? User { get; } }` (`IndyPOS.Application.Common.Interfaces`)
  - `public sealed class EventCurrentUser(IEventAggregator) : ICurrentUser` (`IndyPOS.Infrastructure.Services`)
  - `public sealed class SalesHistoryViewModelFactory(IStoreHubClient client, IReceiptPrinterService printer, ReceiptDocumentFactory receipts, StoreClock clock, ICurrentUser currentUser)` with `InvoiceDetailViewModel CreateDetail()`; throws `InvalidOperationException(NotLoggedIn)` when `User` is null. C2 adds the history ViewModels.
- DI (till): `ICurrentUser` singleton; `TimeProvider` = `TimeProvider.System`; `StoreClock` singleton from `IStoreIdentityService.TimeZone`; `SalesHistoryViewModelFactory` singleton.

- [ ] **Step 1: Write the failing tests**

`tests/IndyPOS.Application.Tests/Infrastructure/EventCurrentUserTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Events;
using IndyPOS.Infrastructure.Services;
using Moq;
using Prism.Events;
using Xunit;

namespace IndyPOS.Application.Tests.Infrastructure;

public class EventCurrentUserTests
{
    private readonly EventAggregator _events = new();

    [Fact]
    public void User_BeforeAnyLogin_IsNull()
    {
        var current = new EventCurrentUser(_events);

        current.User.Should()
                    .BeNull();
    }

    [Fact]
    public void User_AfterLogout_IsNull()
    {
        var current = new EventCurrentUser(_events);
        _events.GetEvent<UserLoggedInEvent>().Publish(Mock.Of<ILoggedInUser>());

        _events.GetEvent<UserLoggedOutEvent>().Publish();

        current.User.Should()
                    .BeNull();
    }

    [Fact]
    public void User_AfterLogin_IsTheLoggedInUser()
    {
        var current = new EventCurrentUser(_events);
        var user = Mock.Of<ILoggedInUser>();

        _events.GetEvent<UserLoggedInEvent>().Publish(user);

        current.User.Should()
                    .BeSameAs(user);
    }
}
```

`Mock.Of` here: `IndyPOS.Application.Tests` references `IndyPOS.Mock`, whose namespace hijacks `Mock.Of` (plan A, Task 4 ruling). If the build reports `CS0234 'Of' does not exist in the namespace 'IndyPOS.Mock'`, write `new Mock<ILoggedInUser>().Object` instead and ledger it.

`tests/IndyPOS.Presentation.Tests/SalesHistory/SalesHistoryViewModelFactoryTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Receipts;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Presentation.SalesHistory;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;

namespace IndyPOS.Presentation.Tests.SalesHistory;

public class SalesHistoryViewModelFactoryTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 10, 15, 3, 0, 0, TimeSpan.Zero);
    private static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    private readonly Mock<ICurrentUser> _currentUser = new();

    [Fact]
    public void CreateDetail_WithNobodyLoggedIn_ThrowsNotLoggedIn()
    {
        var act = () => Factory().CreateDetail();

        act.Should()
           .Throw<InvalidOperationException>()
           .WithMessage(SalesHistoryViewModelFactory.NotLoggedIn);
    }

    [Fact]
    public void CreateDetail_ForACashier_HoldsReprintToToday()
    {
        _currentUser.SetupGet(c => c.User).Returns(User((int)UserRole.Cashier));
        var detail = Factory().CreateDetail();

        detail.Show(Bill(new DateTime(2026, 10, 14, 2, 0, 0, DateTimeKind.Utc)));

        detail.ReprintCommand.CanExecute(null).Should()
                                              .BeFalse();
    }

    [Fact]
    public void CreateDetail_ForAStoreManager_ReprintsAnyDay()
    {
        _currentUser.SetupGet(c => c.User).Returns(User((int)UserRole.StoreManager));
        var detail = Factory().CreateDetail();

        detail.Show(Bill(new DateTime(2026, 10, 14, 2, 0, 0, DateTimeKind.Utc)));

        detail.ReprintCommand.CanExecute(null).Should()
                                              .BeTrue();
    }

    private SalesHistoryViewModelFactory Factory() =>
        new(Mock.Of<IStoreHubClient>(), Mock.Of<IReceiptPrinterService>(), new ReceiptDocumentFactory(Bangkok),
            new StoreClock(new FakeTimeProvider(NowUtc), Bangkok), _currentUser.Object);

    private static ILoggedInUser User(int roleId)
    {
        var user = new Mock<ILoggedInUser>();
        user.SetupGet(u => u.FirstName).Returns("Malee");
        user.SetupGet(u => u.LastName).Returns("Cashier");
        user.SetupGet(u => u.RoleId).Returns(roleId);

        return user.Object;
    }

    private static InvoiceDetailDto Bill(DateTime createdUtc) => new(
        Guid.NewGuid(), 1042, "store-a", Guid.NewGuid(), "Malee Cashier", 100m, 100m, 0m, false, false, 0m,
        createdUtc, [], []);
}
```

- [ ] **Step 2: Run them to see them fail**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~EventCurrentUserTests"` and `dotnet test tests/IndyPOS.Presentation.Tests --filter "FullyQualifiedName~SalesHistoryViewModelFactoryTests"`
Expected: both builds FAIL — `ICurrentUser`, `EventCurrentUser`, `SalesHistoryViewModelFactory` do not exist.

- [ ] **Step 3: Implement**

`src/IndyPOS.Application/Common/Interfaces/ICurrentUser.cs`:

```csharp
namespace IndyPOS.Application.Common.Interfaces;

/// <summary>Who is logged in on this till right now; null between a logout and the next login.</summary>
public interface ICurrentUser
{
    ILoggedInUser? User { get; }
}
```

`src/IndyPOS.Infrastructure/Services/EventCurrentUser.cs`:

```csharp
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Events;
using Prism.Events;

namespace IndyPOS.Infrastructure.Services;

/// <summary>The logged-in user, kept from the same login/logout events every till panel listens to.</summary>
public sealed class EventCurrentUser : ICurrentUser
{
    public EventCurrentUser(IEventAggregator events)
    {
        // Method groups, not lambdas: Prism holds subscribers weakly, and this instance is what DI keeps alive.
        events.GetEvent<UserLoggedInEvent>().Subscribe(OnLoggedIn);
        events.GetEvent<UserLoggedOutEvent>().Subscribe(OnLoggedOut);
    }

    public ILoggedInUser? User { get; private set; }

    private void OnLoggedIn(ILoggedInUser user) => User = user;

    private void OnLoggedOut() => User = null;
}
```

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
    ICurrentUser currentUser)
{
    public const string NotLoggedIn = "No user is logged in on this till.";

    public InvoiceDetailViewModel CreateDetail() => new(client, printer, receipts, clock, Access());

    private SalesHistoryAccess Access() =>
        SalesHistoryAccess.For(currentUser.User ?? throw new InvalidOperationException(NotLoggedIn));
}
```

In `src/IndyPOS.Infrastructure/ConfigureServices.cs`, `AddInfrastructureServices`, directly after `.AddSingleton<IEventAggregator, EventAggregator>()` add `.AddSingleton<ICurrentUser, EventCurrentUser>()` to the same chain.

In `src/IndyPOS.Windows.Forms/ConfigureServices.cs`, `AddUIServices`, before `return services;` (add `using IndyPOS.Presentation.SalesHistory;`):

```csharp
		// Sales-history ViewModels, built per screen open for whoever is logged in.
		services.AddSingleton<TimeProvider>(TimeProvider.System)
				.AddSingleton(sp => new StoreClock(sp.GetRequiredService<TimeProvider>(),
												   sp.GetRequiredService<IStoreIdentityService>().TimeZone))
				.AddSingleton<SalesHistoryViewModelFactory>();
```

- [ ] **Step 4: Run them to see them pass, and the till's container**

Run the two filtered commands from Step 2. Expected: PASS, 3 and 3.
Run: `dotnet test tests/IndyPOS.Windows.Forms.Tests --filter "FullyQualifiedName~TillHostTests"`. Expected: PASS — the Development host validates every registration, so it proves the factory and its five dependencies resolve.

- [ ] **Step 5: Commit**

```bash
git add src/IndyPOS.Application/Common/Interfaces/ICurrentUser.cs src/IndyPOS.Infrastructure src/IndyPOS.Presentation src/IndyPOS.Windows.Forms/ConfigureServices.cs tests/IndyPOS.Application.Tests/Infrastructure/EventCurrentUserTests.cs tests/IndyPOS.Presentation.Tests/SalesHistory/SalesHistoryViewModelFactoryTests.cs
git commit -m "feat(till): ViewModels are built for whoever is logged in when a screen opens"
```

---

### Task 3: One print gate, and DEBUG receipts as PNGs

Today the gate is `[Conditional("RELEASE")]` on `PrintReceiptForm.PrintReceipt`, so a DEBUG sale prints nothing and leaves no trace. Plan B's `InvoiceDetailViewModel` calls `IReceiptPrinterService` directly, outside any form. The gate therefore moves into the composition root, the one place both paths pass through: RELEASE registers the thermal printer, DEBUG a printer that saves the receipt as a PNG. That also gives a developer, and the UI driver, the exact receipt a sale or reprint would print.

**Files:**
- Create: `src/IndyPOS.Infrastructure/Services/ReceiptHeaders.cs`
- Create: `src/IndyPOS.Infrastructure/Services/ReceiptImagePrinter.cs`
- Modify: `src/IndyPOS.Infrastructure/Services/ReceiptPrinterService.cs` (`GetStoreConfiguration` uses `ReceiptHeaders.From`)
- Modify: `src/IndyPOS.Windows.Forms/TillHost.cs` (`AddServices`)
- Modify: `src/IndyPOS.Windows.Forms/UI/Sale/PrintReceiptForm.cs` (drop `[Conditional("RELEASE")]`)
- Test: `tests/IndyPOS.Application.Tests/Common/Receipts/ReceiptImagePrinterTests.cs`

**Interfaces:**
- Produces:
  - `public static class ReceiptHeaders { static ReceiptHeader From(StoreConfiguration config) }`
  - `public sealed class ReceiptImagePrinter : IReceiptPrinterService` with ctors `(IStoreConfigurationService, ILogger<ReceiptImagePrinter>)` (DI; saves under `DefaultDirectory`) and `(IStoreConfigurationService, ILogger<ReceiptImagePrinter>, string directory)` (tests), `static string DefaultDirectory`, and `string? LastSavedPath`.

- [ ] **Step 1: Write the failing tests**

`tests/IndyPOS.Application.Tests/Common/Receipts/ReceiptImagePrinterTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.Common.Receipts;
using IndyPOS.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.Common.Receipts;

public sealed class ReceiptImagePrinterTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "indypos-receipt-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void PrintReceipt_WithACopy_MarksTheFileAsACopy()
    {
        var printer = Printer();

        printer.PrintReceipt(Receipt() with { Reprint = new ReceiptReprint(new DateTime(2026, 10, 16, 9, 5, 0), "Somchai") });

        Path.GetFileName(printer.LastSavedPath).Should()
                                               .StartWith("0000001042-copy");
    }

    [Fact]
    public void PrintReceipt_WithASale_SavesAPngNamedByTheBillNumber()
    {
        var printer = Printer();

        printer.PrintReceipt(Receipt());

        Path.GetFileName(printer.LastSavedPath).Should()
                                               .MatchRegex(@"^0000001042-\d{14}\.png$");
    }

    [Fact]
    public void PrintReceipt_WithASale_WritesTheFile()
    {
        var printer = Printer();

        printer.PrintReceipt(Receipt());

        File.Exists(printer.LastSavedPath).Should()
                                          .BeTrue();
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private ReceiptImagePrinter Printer()
    {
        var config = new Mock<IStoreConfigurationService>();
        config.Setup(c => c.Get()).Returns(new StoreConfiguration { StoreName = "Test Store" });

        return new ReceiptImagePrinter(config.Object, NullLogger<ReceiptImagePrinter>.Instance, _directory);
    }

    private static ReceiptDocument Receipt() => new(
        1042, new DateTime(2026, 10, 15, 14, 30, 0), "Malee Cashier",
        [new ReceiptItem("Nails 1kg", null, 2, 50m, 100m)],
        [new ReceiptPayment("เงินสด", 100m, null)],
        100m, 100m, 0m, false);
}
```

Before running: open `src/IndyPOS.Application/Common/Models/StoreConfiguration.cs` and confirm `StoreName` is settable and the class has a parameterless constructor; if it uses `required` members, set them in the object initializer.

- [ ] **Step 2: Run them to see them fail**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~ReceiptImagePrinterTests"`
Expected: build FAILS — `ReceiptImagePrinter` does not exist.

- [ ] **Step 3: Implement**

`src/IndyPOS.Infrastructure/Services/ReceiptHeaders.cs`:

```csharp
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.Common.Receipts;

namespace IndyPOS.Infrastructure.Services;

/// <summary>The store lines every receipt starts with, from the till's store configuration.</summary>
public static class ReceiptHeaders
{
    public static ReceiptHeader From(StoreConfiguration config) => new(
        config.StoreName ?? string.Empty,
        config.StoreAddressLine1 ?? string.Empty,
        config.StoreAddressLine2 ?? string.Empty,
        config.StorePhoneNumber ?? string.Empty);
}
```

In `ReceiptPrinterService.GetStoreConfiguration`, replace the four-line `_header = new ReceiptHeader(...)` with `_header = ReceiptHeaders.From(config);`.

`src/IndyPOS.Infrastructure/Services/ReceiptImagePrinter.cs`:

```csharp
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.Versioning;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Receipts;
using Microsoft.Extensions.Logging;

namespace IndyPOS.Infrastructure.Services;

/// <summary>
/// The DEBUG till's printer: each receipt is saved as a PNG, drawn exactly as the thermal printer
/// would, instead of being sent to a printer the machine does not have. TillHost registers it in
/// DEBUG builds only, which is the till's whole print gate.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class ReceiptImagePrinter : IReceiptPrinterService
{
    private const int Width = 220;
    private const int TallestReceipt = 4000;
    private const int BottomMargin = 10;

    private readonly ReceiptHeader _header;
    private readonly ILogger<ReceiptImagePrinter> _logger;
    private readonly string _directory;

    public ReceiptImagePrinter(IStoreConfigurationService storeConfiguration, ILogger<ReceiptImagePrinter> logger)
        : this(storeConfiguration, logger, DefaultDirectory)
    {
    }

    public ReceiptImagePrinter(IStoreConfigurationService storeConfiguration, ILogger<ReceiptImagePrinter> logger,
                               string directory)
    {
        _header = ReceiptHeaders.From(storeConfiguration.Get());
        _logger = logger;
        _directory = directory;
    }

    public static string DefaultDirectory { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IndyPOS", "receipts");

    public string? LastSavedPath { get; private set; }

    public void PrintReceipt(ReceiptDocument document)
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, FileName(document));

        using var page = new Bitmap(Width, TallestReceipt);
        int bottom;
        using (var graphics = Graphics.FromImage(page))
        {
            graphics.Clear(Color.White);
            bottom = ReceiptRenderer.Draw(graphics, ReceiptLayout.Rows(_header, document, CultureInfo.CurrentCulture));
        }

        using var receipt = page.Clone(new Rectangle(0, 0, Width, Math.Min(bottom + BottomMargin, TallestReceipt)), page.PixelFormat);
        receipt.Save(path, ImageFormat.Png);

        LastSavedPath = path;
        _logger.LogInformation("DEBUG build: receipt {InvoiceNumber} saved as {Path}, not printed", document.InvoiceNumber, path);
    }

    // The time keeps two copies of one bill apart.
    private static string FileName(ReceiptDocument document) =>
        $"{document.InvoiceNumber.ToString("0000000000", CultureInfo.InvariantCulture)}{(document.IsCopy ? "-copy" : "")}" +
        $"-{DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)}.png";
}
```

In `src/IndyPOS.Windows.Forms/TillHost.cs`, `AddServices` becomes:

```csharp
	private static void AddServices(HostBuilderContext context, IServiceCollection services)
	{
		services.AddUIServices()
				.AddInfrastructureServices(context.Configuration)
				.AddStoreHubClientServices(context.Configuration); // Epic G: StoreHub integration

		AddReceiptPrinter(services);
	}

	// The till's one print gate: a DEBUG build saves receipts as PNGs rather than printing them. Registered
	// last so it replaces the thermal printer AddInfrastructureServices registered.
	private static void AddReceiptPrinter(IServiceCollection services)
	{
#if DEBUG
		services.AddSingleton<IReceiptPrinterService, ReceiptImagePrinter>();
#endif
	}
```

Add `using IndyPOS.Application.Common.Interfaces;` and `using IndyPOS.Infrastructure.Services;` to `TillHost.cs`. In `PrintReceiptForm.cs`, delete the `[Conditional("RELEASE")]` attribute line and the `using System.Diagnostics;` if nothing else uses it; the method itself stays.

- [ ] **Step 4: Run them to see them pass**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~ReceiptImagePrinterTests"`. Expected: PASS, 3.
Run: `dotnet test tests/IndyPOS.Windows.Forms.Tests --filter "FullyQualifiedName~TillHostTests"`. Expected: PASS (a Debug test run builds the Debug host, so it resolves `ReceiptImagePrinter`).
Run: `dotnet build src/IndyPOS.Windows.Forms -c Release`. Expected: success (the `#if` compiles away).

- [ ] **Step 5: Commit**

```bash
git add src/IndyPOS.Infrastructure/Services src/IndyPOS.Windows.Forms/TillHost.cs src/IndyPOS.Windows.Forms/UI/Sale/PrintReceiptForm.cs tests/IndyPOS.Application.Tests/Common/Receipts/ReceiptImagePrinterTests.cs
git commit -m "feat(till): one print gate in the composition root; a DEBUG till saves each receipt as a PNG"
```

---

### Task 4: `InvoiceDetailView`

**Files:**
- Create: `src/IndyPOS.Windows.Forms/UI/Report/TillGrid.cs`
- Create: `src/IndyPOS.Windows.Forms/UI/Report/InvoiceDetailView.cs`
- Test: `tests/IndyPOS.Windows.Forms.Tests/UI/Report/InvoiceDetailViewTests.cs`

**Interfaces:**
- Consumes: Task 1's `CommandButton`; plan B's `InvoiceDetailViewModel` (`Bill`, `IssuedAt`, `HardwareTotal`, `GeneralTotal`, `IsBusy`, `ErrorMessage`, `ReprintCommand`, `PropertyChanged`).
- Produces:
  - `public static class TillGrid { static DataGridView Create(string name); static void AddColumn(DataGridView grid, string header, int width, string? format = null) }`
  - `public sealed class InvoiceDetailView : UserControl` with `void Bind(InvoiceDetailViewModel viewModel)` and `bool ShowProductTypeSplit { get; set; }` (default true). Control names: `BillNumberLabel`, `IssuedAtLabel`, `CashierLabel`, `BadgesLabel`, `LinesGrid`, `PaymentsGrid`, `TotalLabel`, `ReceivedLabel`, `ChangeLabel`, `SplitLabel`, `ErrorLabel`, `ReprintButton`.

- [ ] **Step 1: Write the failing tests**

`tests/IndyPOS.Windows.Forms.Tests/UI/Report/InvoiceDetailViewTests.cs`:

```csharp
using System.Windows.Forms;
using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Receipts;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Domain.Enums;
using IndyPOS.Presentation.SalesHistory;
using IndyPOS.Windows.Forms.UI.Report;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;

namespace IndyPOS.Windows.Forms.Tests.UI.Report;

public class InvoiceDetailViewTests
{
    // Now is 10:00 on 2026-10-15 in Bangkok; the bills below are 09:00 today and 09:00 yesterday.
    private static readonly DateTimeOffset NowUtc = new(2026, 10, 15, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateTime TodayUtc = new(2026, 10, 15, 2, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime YesterdayUtc = new(2026, 10, 14, 2, 0, 0, DateTimeKind.Utc);
    private static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    private static readonly SalesHistoryAccess Cashier = new("Malee Cashier", CanViewAnyDay: false, CanReprint: true);
    private static readonly SalesHistoryAccess Manager = new("Somchai Manager", CanViewAnyDay: true, CanReprint: true);

    private readonly Mock<IStoreHubClient> _client = new();

    [Fact]
    public void Bind_ForATodayOnlyUserOnYesterdaysBill_DisablesReprint()
    {
        using var view = new InvoiceDetailView();
        var vm = Detail(Cashier);
        view.Bind(vm);

        vm.Show(Bill(YesterdayUtc));

        Find(view, "ReprintButton").Enabled.Should()
                                           .BeFalse();
    }

    [Fact]
    public async Task Bind_WhenTheBillIsNotFound_ShowsTheError()
    {
        _client.Setup(c => c.GetSaleAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((InvoiceDetailDto?)null);
        using var view = new InvoiceDetailView();
        var vm = Detail(Cashier);
        view.Bind(vm);

        await vm.LoadAsync(Guid.NewGuid());

        Find(view, "ErrorLabel").Text.Should()
                                     .Be(InvoiceDetailViewModel.BillNotFound);
    }

    [Fact]
    public void Bind_WithNoProductTypeSplit_HidesTheSplit()
    {
        using var view = new InvoiceDetailView { ShowProductTypeSplit = false };
        var vm = Detail(Manager);
        view.Bind(vm);

        vm.Show(Bill(TodayUtc));

        Find(view, "SplitLabel").Visible.Should()
                                        .BeFalse();
    }

    [Fact]
    public void Bind_ToASecondViewModel_StopsFollowingTheFirst()
    {
        using var view = new InvoiceDetailView();
        var first = Detail(Manager);
        view.Bind(first);
        view.Bind(Detail(Manager));

        first.Show(Bill(TodayUtc) with { InvoiceNumber = 7 });

        Find(view, "BillNumberLabel").Text.Should()
                                          .NotContain("0000000007");
    }

    [Fact]
    public void Bind_WithABill_ShowsItsNumberAsOnTheReceipt()
    {
        using var view = new InvoiceDetailView();
        var vm = Detail(Manager);
        view.Bind(vm);

        vm.Show(Bill(TodayUtc));

        Find(view, "BillNumberLabel").Text.Should()
                                          .Contain("0000001042");
    }

    [Fact]
    public void Bind_WithABill_ListsEachLine()
    {
        using var view = new InvoiceDetailView();
        var vm = Detail(Manager);
        view.Bind(vm);

        vm.Show(Bill(TodayUtc));

        ((DataGridView)Find(view, "LinesGrid")).Rows.Count.Should()
                                                          .Be(2);
    }

    [Fact]
    public void Bind_WithARefund_ShowsTheRefundBadge()
    {
        using var view = new InvoiceDetailView();
        var vm = Detail(Manager);
        view.Bind(vm);

        vm.Show(Bill(TodayUtc) with { IsRefund = true });

        Find(view, "BadgesLabel").Text.Should()
                                      .Contain(InvoiceDetailView.RefundBadge);
    }

    [Fact]
    public void Bind_ForAManagerOnYesterdaysBill_EnablesReprint()
    {
        using var view = new InvoiceDetailView();
        var vm = Detail(Manager);
        view.Bind(vm);

        vm.Show(Bill(YesterdayUtc));

        Find(view, "ReprintButton").Enabled.Should()
                                           .BeTrue();
    }

    private InvoiceDetailViewModel Detail(SalesHistoryAccess access) =>
        new(_client.Object, Mock.Of<IReceiptPrinterService>(), new ReceiptDocumentFactory(Bangkok),
            new StoreClock(new FakeTimeProvider(NowUtc), Bangkok), access);

    private static InvoiceDetailDto Bill(DateTime createdUtc) => new(
        Guid.NewGuid(), 1042, "store-a", Guid.NewGuid(), "Malee Cashier", 350m, 400m, 50m, false, false, 0m, createdUtc,
        [
            new InvoiceLineDto(Guid.NewGuid(), Guid.NewGuid(), "Hammer", null, null, ProductCategoryKind.Hardware, 1, 250m, 250m),
            new InvoiceLineDto(Guid.NewGuid(), Guid.NewGuid(), "Rice", null, null, ProductCategoryKind.GeneralGoods, 1, 100m, 100m)
        ],
        [new PaymentDto(Guid.NewGuid(), "Cash", "เงินสด", 400m, null)]);

    private static Control Find(Control root, string name) => root.Controls.Find(name, searchAllChildren: true).Single();
}
```

`Mock.Of` resolves to Moq here: `IndyPOS.Windows.Forms.Tests` does not reference `IndyPOS.Mock` (check its csproj; if it does, use `new Mock<T>().Object` and ledger it). `Microsoft.Extensions.TimeProvider.Testing` may not be referenced by this test project: if `FakeTimeProvider` does not resolve, add `<PackageReference Include="Microsoft.Extensions.TimeProvider.Testing" Version="10.10.0" />` to `IndyPOS.Windows.Forms.Tests.csproj`.

- [ ] **Step 2: Run them to see them fail**

Run: `dotnet test tests/IndyPOS.Windows.Forms.Tests --filter "FullyQualifiedName~InvoiceDetailViewTests"`
Expected: build FAILS — `InvoiceDetailView` does not exist.

- [ ] **Step 3: Implement `TillGrid`**

`src/IndyPOS.Windows.Forms/UI/Report/TillGrid.cs`:

```csharp
namespace IndyPOS.Windows.Forms.UI.Report;

/// <summary>The till's dark, read-only grid, as the designer-built report grids style it, for views built in code.</summary>
public static class TillGrid
{
    private const string FontName = "FC Subject [Non-commercial] Reg";
    private static readonly Color Background = Color.FromArgb(38, 38, 38);
    private static readonly Color HeaderBackground = Color.FromArgb(48, 48, 48);

    public static DataGridView Create(string name)
    {
        var grid = new DataGridView
        {
            Name = name,
            Dock = DockStyle.Fill,
            ReadOnly = true,
            MultiSelect = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Background,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
            ColumnHeadersHeight = 50,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            EnableHeadersVisualStyles = false,
            GridColor = Color.DimGray
        };

        grid.ColumnHeadersDefaultCellStyle.BackColor = HeaderBackground;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.Gainsboro;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font(FontName, 14F);
        grid.DefaultCellStyle.BackColor = Background;
        grid.DefaultCellStyle.ForeColor = Color.Gainsboro;
        grid.DefaultCellStyle.Font = new Font(FontName, 12F);
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(70, 70, 70);
        grid.DefaultCellStyle.SelectionForeColor = Color.Gainsboro;
        grid.RowTemplate.Height = 35;

        return grid;
    }

    public static void AddColumn(DataGridView grid, string header, int width, string? format = null)
    {
        var column = grid.Columns[grid.Columns.Add(header, header)];
        column.Width = width;
        column.SortMode = DataGridViewColumnSortMode.NotSortable;

        if (format is not null)
            column.DefaultCellStyle.Format = format;
    }
}
```

- [ ] **Step 4: Implement `InvoiceDetailView`**

`src/IndyPOS.Windows.Forms/UI/Report/InvoiceDetailView.cs`:

```csharp
using System.ComponentModel;
using IndyPOS.Presentation.SalesHistory;
using IndyPOS.Windows.Forms.UI.ModernUI;
using IndyPOS.Windows.Forms.UI.Mvvm;

namespace IndyPOS.Windows.Forms.UI.Report;

/// <summary>
/// One bill (spec §7). It shows <see cref="InvoiceDetailViewModel"/>'s state and forwards พิมพ์ซ้ำ;
/// everything else lives in the ViewModel. Built in code, not the designer, so the layout is reviewable.
/// The void feature adds its button beside พิมพ์ซ้ำ.
/// </summary>
public sealed class InvoiceDetailView : UserControl
{
    public const string RefundBadge = "คืนสินค้า";
    public const string PayLaterBadge = "ลงบัญชี";

    private const string FontName = "FC Subject [Non-commercial] Reg";
    private static readonly Color Background = Color.FromArgb(38, 38, 38);
    private static readonly Color Accent = Color.FromArgb(50, 190, 166);

    private readonly Label _billNumber = TextLabel("BillNumberLabel", 18F);
    private readonly Label _issuedAt = TextLabel("IssuedAtLabel");
    private readonly Label _cashier = TextLabel("CashierLabel");
    private readonly Label _badges = TextLabel("BadgesLabel");
    private readonly DataGridView _lines = TillGrid.Create("LinesGrid");
    private readonly DataGridView _payments = TillGrid.Create("PaymentsGrid");
    private readonly Label _total = TextLabel("TotalLabel");
    private readonly Label _received = TextLabel("ReceivedLabel");
    private readonly Label _change = TextLabel("ChangeLabel");
    private readonly Label _split = TextLabel("SplitLabel");
    private readonly Label _error = TextLabel("ErrorLabel");
    private readonly ModernButton _reprint = new()
    {
        Name = "ReprintButton",
        Text = "พิมพ์ซ้ำ",
        Size = new Size(180, 60),
        Dock = DockStyle.Right,
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

    private InvoiceDetailViewModel? _viewModel;
    private IDisposable? _reprintBinding;
    private bool _showProductTypeSplit = true;

    public InvoiceDetailView()
    {
        Name = nameof(InvoiceDetailView);
        BackColor = Background;
        _error.ForeColor = Color.Salmon;

        TillGrid.AddColumn(_lines, "สินค้า", 320);
        TillGrid.AddColumn(_lines, "จำนวน", 90);
        TillGrid.AddColumn(_lines, "ราคาต่อหน่วย", 140, "N2");
        TillGrid.AddColumn(_lines, "ราคารวม", 140, "N2");
        TillGrid.AddColumn(_lines, "Note", 200);
        TillGrid.AddColumn(_payments, "การชำระเงิน", 320);
        TillGrid.AddColumn(_payments, "จำนวนเงิน", 160, "N2");
        TillGrid.AddColumn(_payments, "Note", 200);

        Controls.Add(_lines);
        Controls.Add(Stack(DockStyle.Bottom, 170, _payments));
        Controls.Add(Totals());
        Controls.Add(Stack(DockStyle.Top, 130, _billNumber, _issuedAt, _cashier, _badges));
    }

    /// <summary>Only GeneralHardware splits its bills (TillLayout.ShowProductTypeSplit).</summary>
    public bool ShowProductTypeSplit
    {
        get => _showProductTypeSplit;
        set
        {
            _showProductTypeSplit = value;
            Render();
        }
    }

    /// <summary>Follows this ViewModel from now on, and stops following the previous one.</summary>
    public void Bind(InvoiceDetailViewModel viewModel)
    {
        if (_viewModel is not null)
            _viewModel.PropertyChanged -= OnViewModelChanged;
        _reprintBinding?.Dispose();

        _viewModel = viewModel;
        _viewModel.PropertyChanged += OnViewModelChanged;
        _reprintBinding = CommandButton.Bind(_reprint, viewModel.ReprintCommand);
        Render();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_viewModel is not null)
                _viewModel.PropertyChanged -= OnViewModelChanged;
            _reprintBinding?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e) => Render();

    private void Render()
    {
        var bill = _viewModel?.Bill;

        _billNumber.Text = bill is null ? string.Empty : $"บิลเลขที่ {bill.InvoiceNumber:0000000000}";
        _issuedAt.Text = _viewModel?.IssuedAt is { } issuedAt ? $"{issuedAt:dd/MM/yyyy HH:mm}" : string.Empty;
        _cashier.Text = bill is null ? string.Empty : $"แคชเชียร์: {bill.CashierName ?? "-"}";
        _badges.Text = bill is null ? string.Empty : Badges(bill.IsRefund, bill.HasPayLater, bill.PayLaterAmount);
        _total.Text = bill is null ? string.Empty : $"ราคารวม {bill.TotalAmount:N2}";
        _received.Text = bill is null ? string.Empty : $"เงินรวม {bill.AmountReceived:N2}";
        _change.Text = bill is null ? string.Empty : $"เงินทอน {bill.ChangeGiven:N2}";
        _split.Visible = ShowProductTypeSplit && bill is not null;
        _split.Text = $"เบ็ดเตล็ด {_viewModel?.GeneralTotal:N2} · ฮาร์ดแวร์ {_viewModel?.HardwareTotal:N2}";
        _error.Text = _viewModel?.ErrorMessage ?? string.Empty;
        UseWaitCursor = _viewModel?.IsBusy == true;

        FillGrids();
    }

    private void FillGrids()
    {
        var bill = _viewModel?.Bill;
        _lines.Rows.Clear();
        _payments.Rows.Clear();

        if (bill is null)
            return;

        foreach (var line in bill.Lines)
            _lines.Rows.Add(line.ProductName, line.Quantity, line.UnitPrice, line.LineTotal, line.Note);

        foreach (var payment in bill.Payments)
            _payments.Rows.Add(payment.MethodDisplayName, payment.Amount, payment.Note);
    }

    private static string Badges(bool isRefund, bool hasPayLater, decimal payLaterAmount)
    {
        var badges = new List<string>();
        if (isRefund)
            badges.Add(RefundBadge);
        if (hasPayLater)
            badges.Add($"{PayLaterBadge} {payLaterAmount:N2}");

        return string.Join(" · ", badges);
    }

    private Panel Totals()
    {
        var figures = Stack(DockStyle.Fill, 0, _total, _received, _change, _split, _error);
        var totals = new Panel { Dock = DockStyle.Bottom, Height = 170, BackColor = Background, Padding = new Padding(10) };
        totals.Controls.Add(figures);
        totals.Controls.Add(_reprint);

        return totals;
    }

    // Children dock to the top in the order given (WinForms docks the last-added first, hence the reverse).
    private static Panel Stack(DockStyle dock, int height, params Control[] children)
    {
        var panel = new Panel { Dock = dock, BackColor = Background, Padding = new Padding(10, 5, 10, 5) };
        if (height > 0)
            panel.Height = height;

        // Enumerable.Reverse, not children.Reverse(): on an array, C# 14 binds the in-place span Reverse.
        foreach (var child in Enumerable.Reverse(children))
        {
            child.Dock = child is DataGridView ? DockStyle.Fill : DockStyle.Top;
            panel.Controls.Add(child);
        }

        return panel;
    }

    private static Label TextLabel(string name, float size = 12F) => new()
    {
        Name = name,
        AutoSize = false,
        Height = (int)(size * 2.2F),
        ForeColor = Color.Gainsboro,
        Font = new Font(FontName, size),
        TextAlign = ContentAlignment.MiddleLeft
    };
}
```

- [ ] **Step 5: Run them to see them pass**

Run: `dotnet test tests/IndyPOS.Windows.Forms.Tests --filter "FullyQualifiedName~InvoiceDetailViewTests"`
Expected: PASS, 8 tests. `MoneyFormatTests` must stay green too (every money format above is `N2`): run `dotnet test tests/IndyPOS.Windows.Forms.Tests`.

- [ ] **Step 6: Commit**

```bash
git add src/IndyPOS.Windows.Forms/UI/Report/TillGrid.cs src/IndyPOS.Windows.Forms/UI/Report/InvoiceDetailView.cs tests/IndyPOS.Windows.Forms.Tests/UI/Report/InvoiceDetailViewTests.cs tests/IndyPOS.Windows.Forms.Tests/IndyPOS.Windows.Forms.Tests.csproj
git commit -m "feat(till): InvoiceDetailView shows one bill and forwards พิมพ์ซ้ำ to its ViewModel"
```

---

### Task 5: `InvoiceDetailForm`, opened from the ลงบัญชี screen

`PayLaterPaymentPanel` injects `SaleHistoryByInvoiceIdForm` but never calls it; double-clicking a debt shows "(Invoice detail view coming soon)". That form's data calls are all stubs.

**Files:**
- Create: `src/IndyPOS.Windows.Forms/UI/Report/InvoiceDetailForm.cs`
- Delete: `src/IndyPOS.Windows.Forms/UI/Report/SaleHistoryByInvoiceIdForm.cs`, `SaleHistoryByInvoiceIdForm.Designer.cs`, `SaleHistoryByInvoiceIdForm.resx`
- Modify: `src/IndyPOS.Windows.Forms/UI/PayLater/PayLaterPaymentPanel.cs` (constructor field + `PayLaterPaymentsDataView_DoubleClick`, line ~340)
- Modify: `src/IndyPOS.Windows.Forms/ConfigureServices.cs` (`SaleHistoryByInvoiceIdForm` → `InvoiceDetailForm`)
- Test: `tests/IndyPOS.Windows.Forms.Tests/UI/Report/InvoiceDetailFormTests.cs`

**Interfaces:**
- Consumes: Task 2's `SalesHistoryViewModelFactory.CreateDetail()`; Task 4's `InvoiceDetailView`; `IStoreFeaturesProvider.GetAsync()` and `TillLayout.For` / `WhenFeaturesUnavailable`.
- Produces: `public sealed class InvoiceDetailForm : Form`, ctor `(SalesHistoryViewModelFactory factory, IStoreFeaturesProvider features)`; `Task LoadAsync(Guid saleId)` (binds a fresh ViewModel and loads, without showing); `Task ShowBillAsync(Guid saleId)` (`LoadAsync` then `ShowDialog`). The detail view is named `InvoiceDetailView`.

- [ ] **Step 1: Write the failing tests**

`tests/IndyPOS.Windows.Forms.Tests/UI/Report/InvoiceDetailFormTests.cs`:

```csharp
using System.Windows.Forms;
using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.Common.Receipts;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Presentation.SalesHistory;
using IndyPOS.Windows.Forms.Services;
using IndyPOS.Windows.Forms.UI.Report;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;

namespace IndyPOS.Windows.Forms.Tests.UI.Report;

/// <summary>LoadAsync only: ShowDialog would block the test run on a modal window.</summary>
public class InvoiceDetailFormTests
{
    private static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
    private static readonly StoreFeaturesDto MimyMart = new(PayLaterEnabled: false, MultipleProductTypesEnabled: false);

    private readonly Mock<IStoreHubClient> _client = new();
    private readonly Mock<ICurrentUser> _currentUser = new();

    [Fact]
    public async Task LoadAsync_WhenStoreHubAnswersNotFound_ShowsBillNotFound()
    {
        _client.Setup(c => c.GetSaleAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((InvoiceDetailDto?)null);
        using var form = Form();

        await form.LoadAsync(Guid.NewGuid());

        Find(form, "ErrorLabel").Text.Should()
                                     .Be(InvoiceDetailViewModel.BillNotFound);
    }

    [Fact]
    public async Task LoadAsync_AtAStoreWithoutTheSplit_HidesIt()
    {
        var bill = Bill();
        _client.Setup(c => c.GetSaleAsync(bill.Id, It.IsAny<CancellationToken>())).ReturnsAsync(bill);
        using var form = Form();

        await form.LoadAsync(bill.Id);

        // Not SplitLabel.Visible: every child of an unshown Form reports Visible == false, so that could not fail.
        ((InvoiceDetailView)Find(form, "InvoiceDetailView")).ShowProductTypeSplit.Should()
                                                                                  .BeFalse();
    }

    [Fact]
    public async Task LoadAsync_WithABill_ShowsIt()
    {
        var bill = Bill();
        _client.Setup(c => c.GetSaleAsync(bill.Id, It.IsAny<CancellationToken>())).ReturnsAsync(bill);
        using var form = Form();

        await form.LoadAsync(bill.Id);

        Find(form, "BillNumberLabel").Text.Should()
                                          .Contain("0000001042");
    }

    private InvoiceDetailForm Form()
    {
        var user = new Mock<ILoggedInUser>();
        user.SetupGet(u => u.FirstName).Returns("Somchai");
        user.SetupGet(u => u.LastName).Returns("Manager");
        user.SetupGet(u => u.RoleId).Returns((int)UserRole.StoreManager);
        _currentUser.SetupGet(c => c.User).Returns(user.Object);

        var features = new Mock<IStoreFeaturesProvider>();
        features.Setup(f => f.GetAsync()).ReturnsAsync(MimyMart);

        var factory = new SalesHistoryViewModelFactory(
            _client.Object, Mock.Of<IReceiptPrinterService>(), new ReceiptDocumentFactory(Bangkok),
            new StoreClock(new FakeTimeProvider(new DateTimeOffset(2026, 10, 15, 3, 0, 0, TimeSpan.Zero)), Bangkok),
            _currentUser.Object);

        return new InvoiceDetailForm(factory, features.Object);
    }

    private static InvoiceDetailDto Bill() => new(
        Guid.NewGuid(), 1042, "store-a", Guid.NewGuid(), "Malee Cashier", 100m, 100m, 0m, false, false, 0m,
        new DateTime(2026, 10, 15, 2, 0, 0, DateTimeKind.Utc), [], []);

    private static Control Find(Control root, string name) => root.Controls.Find(name, searchAllChildren: true).Single();
}
```

Check `StoreFeaturesDto`'s constructor in `ReportPanelsLayoutTests` (it is called there with named arguments `PayLaterEnabled`, `MultipleProductTypesEnabled`, optional `ServiceProductsEnabled`) and its namespace (`IndyPOS.Application.Common.Models` per that file's usings).

- [ ] **Step 2: Run them to see them fail**

Run: `dotnet test tests/IndyPOS.Windows.Forms.Tests --filter "FullyQualifiedName~InvoiceDetailFormTests"`
Expected: build FAILS — `InvoiceDetailForm` does not exist.

- [ ] **Step 3: Implement the form**

`src/IndyPOS.Windows.Forms/UI/Report/InvoiceDetailForm.cs`:

```csharp
using IndyPOS.Application.Common.Models;
using IndyPOS.Presentation.SalesHistory;
using IndyPOS.Windows.Forms.Services;
using Serilog;

namespace IndyPOS.Windows.Forms.UI.Report;

/// <summary>
/// One bill in a dialog (spec §7: the old SaleHistoryByInvoiceIdForm), opened by the bill's Guid, e.g. from a
/// ลงบัญชี debt. Each open gets a fresh ViewModel for whoever is logged in now.
/// </summary>
public sealed class InvoiceDetailForm : Form
{
    private readonly SalesHistoryViewModelFactory _factory;
    private readonly IStoreFeaturesProvider _features;
    private readonly InvoiceDetailView _view = new() { Dock = DockStyle.Fill };

    public InvoiceDetailForm(SalesHistoryViewModelFactory factory, IStoreFeaturesProvider features)
    {
        _factory = factory;
        _features = features;

        Name = nameof(InvoiceDetailForm);
        Text = "รายละเอียดบิล";
        Size = new Size(1000, 850);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(38, 38, 38);
        Controls.Add(_view);
    }

    /// <summary>Binds a fresh ViewModel and loads the bill, without showing the dialog.</summary>
    public async Task LoadAsync(Guid saleId)
    {
        var viewModel = _factory.CreateDetail();
        _view.ShowProductTypeSplit = (await LayoutAsync()).ShowProductTypeSplit;
        _view.Bind(viewModel);

        await viewModel.LoadAsync(saleId);
    }

    public async Task ShowBillAsync(Guid saleId)
    {
        await LoadAsync(saleId);
        ShowDialog();
    }

    private async Task<TillLayout> LayoutAsync()
    {
        try
        {
            return TillLayout.For(await _features.GetAsync());
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not load store features for the bill detail");
            return TillLayout.WhenFeaturesUnavailable;
        }
    }
}
```

`InvoiceDetailForm` is a singleton (registered in place of `SaleHistoryByInvoiceIdForm`), so `ShowDialog` must not dispose it: closing a modal form hides it, and the next `LoadAsync` re-binds.

- [ ] **Step 4: Swap it in**

1. Delete the three `SaleHistoryByInvoiceIdForm` files.
2. In `src/IndyPOS.Windows.Forms/ConfigureServices.cs`, replace `.AddSingleton<SaleHistoryByInvoiceIdForm>()` with `.AddSingleton<InvoiceDetailForm>()`.
3. In `PayLaterPaymentPanel.cs`, replace the `SaleHistoryByInvoiceIdForm` field and constructor parameter (lines ~13 and ~29) with `InvoiceDetailForm` (`_invoiceDetailForm`), and replace `PayLaterPaymentsDataView_DoubleClick` with:

```csharp
    private async void PayLaterPaymentsDataView_DoubleClick(object sender, EventArgs e)
    {
        var invoiceId = GetInvoiceIdFromSelectedRow();
        if (invoiceId is null)
            return;

        // The dialog shows its own errors (ไม่พบบิล, StoreHub's reason); this catches only the unexpected.
        try
        {
            await _invoiceDetailForm.ShowBillAsync(invoiceId.Value);
        }
        catch (Exception ex)
        {
            _messageForm.ShowDialog($"เปิดบิลไม่สำเร็จ: {ex.Message}", "Error");
        }
    }
```

4. `grep -rn "SaleHistoryByInvoiceIdForm" src tests` must return nothing.

- [ ] **Step 5: Run the tests and the container check**

Run: `dotnet test tests/IndyPOS.Windows.Forms.Tests`
Expected: PASS — the 3 new tests, `TillHostTests` (DI resolves `InvoiceDetailForm` and the panel), `MoneyFormatTests`, and the rest.

- [ ] **Step 6: Commit**

```bash
git add -A src/IndyPOS.Windows.Forms tests/IndyPOS.Windows.Forms.Tests
git commit -m "feat(till): double-clicking a ลงบัญชี debt opens its bill, with reprint"
```

---

### Task 6: Live check, docs, PR

Verified on the real till, driven by the agent (load the `testing-indypos` skill; it drives the till through UI Automation and reads Postgres). Pond's hands are not needed except to keep off the mouse while the driver runs.

- [ ] **Step 1: Run the store with ลงบัญชี**

`dotnet run --project src/IndyPOS.AppHost -- --store GeneralHardware` (Docker up), wait for the till, log in as `manager` / `123` with the skill's login flow.

- [ ] **Step 2: A sale's receipt (plan A, end to end)**

Ring up one product and save the sale (the skill's `cash-sale` flow). Expected: a new `%LOCALAPPDATA%\IndyPOS\receipts\<number>-<time>.png` whose number equals the new `invoice.invoice_number` in Postgres (`scripts/db.sh`). Look at the PNG (glimpse/Read): real bill number, store time, `02:30 PM`-style time, two-decimal money, the cashier's name.

- [ ] **Step 3: A ลงบัญชี bill, reprinted**

Make a ลงบัญชี sale (or use a seeded debt), open ลงบัญชี, double-click the debt. Expected: `InvoiceDetailForm` shows the bill number, lines and payments, the hardware/general split, and an enabled พิมพ์ซ้ำ. Click it once. Expected: one new `<number>-copy-*.png` with the สำเนา / COPY banner and the manager as reprinter; exactly one new `invoice_reprint` row.

- [ ] **Step 4: The cashier and an old bill**

If a debt from a previous day exists (seeded data), log in as `cashier` / `123` and open it. Expected: `ไม่พบบิล`, no crash. If none exists, record that this case is covered only by `LoadAsync_WhenStoreHubAnswersNotFound_ShowsBillNotFound`.

- [ ] **Step 5: Full run, docs, PR**

- `dotnet test --logger trx --results-directory <scratchpad>/trx-plan-c1`; every suite green; record counts.
- `CLAUDE.md` and `ONBOARDING.md`: measured counts (Application, Presentation, Windows.Forms, total); and one line under ONBOARDING's run section: "A DEBUG till does not print: each receipt is saved as a PNG under `%LOCALAPPDATA%\IndyPOS\receipts`."
- Commit `docs: measured counts after the bill detail screen`, push `feat/bill-detail-screen`, open the PR (title `feat(till): open any ลงบัญชี bill and reprint it`), with the live-check evidence (PNG names, the reprint row). Wait for Codex before `--auto`.

---

## Self-Review

**Spec coverage (§7, §8):** InvoiceDetailView header/lines/payments/totals/พิมพ์ซ้ำ with room for void — Task 4. SaleHistoryByInvoiceIdForm → dialog hosting the view, keyed by the debt's Guid — Task 5. Binding via INotifyPropertyChanged and a Click → Execute helper that follows CanExecute — Tasks 1, 4. Print gate kept — Task 3 (moved, not dropped). Hardware/General split per store — Tasks 4, 5.
**Left for C2:** the rebuilt sales-history tab (list, periods, search, load more), the บิลวันนี้ button and today-only screen, the history ViewModel factory methods, removing `GetInvoicesByPeriodAsync`, `GetInvoicesByDateRangeAsync`, `GetInvoiceProductsByInvoiceIdAsync`, `GetPaymentsByInvoiceIdAsync` and the panel's hidden print button.
**Placeholders:** none. Two steps ask the executor to confirm a constructor shape (`StoreConfiguration`, `StoreFeaturesDto`) before relying on it, with the fallback stated.
**Type consistency:** `CommandButton.Bind(Control, ICommand, Func<object?>?)` / `TryExecute(ICommand, object?)`; `ICurrentUser.User`; `SalesHistoryViewModelFactory(client, printer, receipts, clock, currentUser).CreateDetail()`; `ReceiptImagePrinter(config, logger[, directory])`, `LastSavedPath`; `ReceiptHeaders.From(StoreConfiguration)`; `InvoiceDetailView.Bind`, `ShowProductTypeSplit`; `InvoiceDetailForm(factory, features).LoadAsync / ShowBillAsync` — same everywhere.
**Review Focus:** 1 → Task 1; 2 → Tasks 5 and 6; 3 → Task 4; 4 → Task 2; 5 → Tasks 3 and 6.
