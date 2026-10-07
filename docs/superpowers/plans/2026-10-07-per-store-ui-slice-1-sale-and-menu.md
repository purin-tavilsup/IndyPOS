# Per-Store UI, Slice 1 (Sale Panel and Menu) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Each store's till shows its own quick buttons and menu:
- MimyShop gets the จัดส่ง / เอกสาร service buttons (the go-live blocker);
- the ฮาร์ดแวร์ button stays GeneralHardware-only;
- รายการลงบัญชี appears only where PayLater exists.

**Architecture:**
- `StoreTypeFeatures` (Domain) gains `ServiceProductsEnabled`. `GET /store/features` returns it, additively.
- A small pure `TillLayout` (Application) turns the features into "which buttons and menu items". The forms only apply it.
- The menu closes the gap a hidden button would leave, through a testable `MenuLayout` helper.

**Tech Stack:** .NET 10, ASP.NET Core minimal APIs, WinForms, xUnit, FluentAssertions 8, the #117 per-store test hosts.

**Spec:** `docs/superpowers/specs/2026-10-07-per-store-ui-design.md` §3 and §4.1 (approved).

## Prerequisites

- PR #117 merged. This branch (`feat/per-store-ui`) is stacked on it; rebase onto `development` after it merges.
- **Never merge #117 with `--delete-branch` while a PR from this branch is open:** it would close the child PR for good. Merge #117 first, then rebase, then open this PR.
- Docker running, or `INDYPOS_TEST_POSTGRES` set.

## Decisions this plan makes (flag them in the PR)

1. **The service buttons are text buttons** ("จัดส่ง", "เอกสาร"), not MimyShop v3's 80×80 icons.
   - Adding images means hand-editing `Resources.Designer.cs` (see memory "WinForms .resx designer codegen") for a UI that the Avalonia port will redraw.
   - Text is clear at the till.
2. **The service buttons take the ฮาร์ดแวร์ button's slot**, side by side, each half its width. No store type has both, and a Domain test pins that, so they never overlap.
3. **`TillLayout` lives in `IndyPOS.Application`.** There is no Presentation project yet, and creating one is out of scope. It is a pure function of `StoreFeaturesDto`, so it moves unchanged when a Presentation project exists.
4. **When the features cannot be fetched,** the service buttons stay hidden (they default to off) and the existing one-time warning shows. A till that cannot reach its StoreHub cannot complete a sale anyway.

## Global Constraints

- [Spec §1] Only switch existing pieces on or off; no new features.
- [Spec §3] Exactly one new flag, `ServiceProductsEnabled`: MimyShop only. Barcodes `2002500000014` (จัดส่ง) and `2002500000021` (เอกสาร).
- [Spec §6] Additive only:
  - an older till ignores the new field;
  - a missing field reads as `false`;
  - no migration.
- **Test style:**
  - names are `Subject_WhenScenario_DirectVerbOutcome`;
  - negative cases first;
  - Arrange/Act/Assert separated by blank lines;
  - FluentAssertions chains on separate lines with the dots aligned.
- **Commits:**
  - conventional, at least one per task;
  - end each message with the session's `Claude-Session:` line.

## Review Focus

1. **A MimyShop till whose catalogue lacks a service product** (e.g. a migrated store). Expected: the existing dialog says `ไม่พบรหัสสินค้า … ในระบบ`, never a crash. → Task 4, checked by hand (the dialog already does this).
2. **An older StoreHub without the new field.** Expected: `ServiceProductsEnabled` reads `false`, and no crash. → Task 2 (`StoreFeaturesDto_WithoutTheNewField_ReadsServiceProductsAsOff`).
3. **Hiding รายการลงบัญชี leaves a hole in the menu.** Expected: the buttons below move up. → Task 5 (`Collapse_WithAHiddenButton_MovesTheButtonsBelowUp`).
4. **Logging out and back in.** Expected: the menu is re-applied without moving buttons twice. → Task 5 (`Collapse_RunTwice_DoesNotMoveButtonsTwice`).
5. **A future store type with both hardware and services.** Expected: a test fails before the UI overlaps. → Task 1 (`For_EachStoreType_NeverEnablesHardwareAndServicesTogether`).

---

### Task 1: Domain: `ServiceProductsEnabled` and the service barcodes

**Files:**
- Modify: `src/IndyPOS.Domain/ValueObjects/StoreTypeFeatures.cs`
- Create: `src/IndyPOS.Domain/ValueObjects/ServiceProductBarcodes.cs`
- Modify: `src/IndyPOS.StoreProfiles/StoreProfiles.cs` (MimyShop's two service products use the constants)
- Create: `tests/IndyPOS.Domain.Tests/ValueObjects/StoreTypeFeaturesTests.cs` (or extend the existing file if one exists: `git grep -l StoreTypeFeatures -- tests/IndyPOS.Domain.Tests`)

**Interfaces (Produces):**
- `StoreTypeFeatures.ServiceProductsEnabled` (`bool`, init);
- `ServiceProductBarcodes.Delivery = "2002500000014"`, `ServiceProductBarcodes.Documents = "2002500000021"`, `ServiceProductBarcodes.All`.

- [ ] **Step 1: Failing tests**

```csharp
using FluentAssertions;
using IndyPOS.Domain.Enums;
using IndyPOS.Domain.ValueObjects;
using Xunit;

namespace IndyPOS.Domain.Tests.ValueObjects;

public class StoreTypeFeaturesServiceTests
{
    [Theory]
    [InlineData(StoreType.GeneralHardware, false)]
    [InlineData(StoreType.Minimart, false)]
    [InlineData(StoreType.MimyShop, true)]
    public void For_EachStoreType_EnablesServiceProductsOnlyForMimyShop(StoreType type, bool expected)
    {
        StoreTypeFeatures.For(type).ServiceProductsEnabled.Should()
                                                          .Be(expected);
    }

    // The sale panel puts the service buttons in the hardware button's slot.
    [Fact]
    public void For_EachStoreType_NeverEnablesHardwareAndServicesTogether()
    {
        var both = Enum.GetValues<StoreType>()
                       .Select(StoreTypeFeatures.For)
                       .Where(f => f.ServiceProductsEnabled && f.MultipleProductTypesEnabled);

        both.Should()
            .BeEmpty();
    }

    [Fact]
    public void ServiceProductBarcodes_All_AreMimyShopsRealBarcodes()
    {
        ServiceProductBarcodes.All.Should()
                                  .Equal("2002500000014", "2002500000021");
    }
}
```

Run: `dotnet test tests/IndyPOS.Domain.Tests --filter "FullyQualifiedName~StoreTypeFeaturesServiceTests"` → build error.

- [ ] **Step 2: Implement**

Add to `StoreTypeFeatures`:

```csharp
    /// <summary>The store sells services (จัดส่ง, เอกสาร) from quick buttons on the sale panel.</summary>
    public bool ServiceProductsEnabled { get; init; }
```

Set `ServiceProductsEnabled = true` in the `StoreType.MimyShop` branch only. The other branches leave it `false`.

`ServiceProductBarcodes.cs`:

```csharp
namespace IndyPOS.Domain.ValueObjects;

/// <summary>
/// The service products sold from the sale panel's quick buttons. The barcodes are MimyShop's real ones
/// (its v3 till pinned them), and every MimyShop till and migration must keep them verbatim.
/// </summary>
public static class ServiceProductBarcodes
{
    public const string Delivery = "2002500000014";   // จัดส่ง
    public const string Documents = "2002500000021";  // เอกสาร

    public static IReadOnlyList<string> All { get; } = [Delivery, Documents];
}
```

In `StoreProfiles.MimyShop`, the two service products' barcodes become `ServiceProductBarcodes.Delivery` / `.Documents`. Add the `using IndyPOS.Domain.ValueObjects;`.

- [ ] **Step 3: Pass, then commit**

Run: `dotnet test tests/IndyPOS.Domain.Tests` and `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~StoreProfiles"` → all pass.

Commit: `feat(domain): MimyShop sells services from quick buttons (ServiceProductsEnabled)`.

---

### Task 2: StoreHub: `/store/features` reports the new flag

**Files:**
- Modify: `src/IndyPOS.Application/Common/Models/StoreFeaturesDto.cs`
- Modify: `src/IndyPOS.StoreHub/Endpoints/Catalogue/CatalogueEndpoints.cs:36`
- Modify: `tests/IndyPOS.StoreHub.IntegrationTests/StoreProfiles/StoreTypeFlowTests.cs` (`StoreFeatures_ForEachStore_ReportTheTypesFeatures`)
- Create: `tests/IndyPOS.Application.Tests/Common/StoreFeaturesDtoTests.cs`

**Interfaces (Produces):** `record StoreFeaturesDto(bool PayLaterEnabled, bool MultipleProductTypesEnabled, bool ServiceProductsEnabled = false)`.

- [ ] **Step 1: Failing tests**
  - In `StoreTypeFlowTests`, the features theory gains a third value. Its rows become `("GeneralHardware", true, true, false)`, `("MimyMart", false, false, false)`, `("MimyShop", false, false, true)`, and it asserts `new StoreFeaturesDto(payLater, multipleTypes, services)`.
  - `StoreFeaturesDtoTests`:

```csharp
using System.Text.Json;
using FluentAssertions;
using IndyPOS.Application.Common.Models;
using Xunit;

namespace IndyPOS.Application.Tests.Common;

// A till newer than its StoreHub (StoreHub rolled back) must read a missing flag as off, not crash.
public class StoreFeaturesDtoTests
{
    [Fact]
    public void StoreFeaturesDto_WithoutTheNewField_ReadsServiceProductsAsOff()
    {
        const string olderStoreHub = """{"payLaterEnabled":true,"multipleProductTypesEnabled":true}""";

        var features = JsonSerializer.Deserialize<StoreFeaturesDto>(olderStoreHub, JsonSerializerOptions.Web);

        features!.ServiceProductsEnabled.Should()
                                        .BeFalse();
    }
}
```

Run both. Expected: build error (the DTO has two parameters), then, once it compiles, the MimyShop row fails.

- [ ] **Step 2: Implement**

```csharp
public record StoreFeaturesDto(bool PayLaterEnabled, bool MultipleProductTypesEnabled, bool ServiceProductsEnabled = false);
```

Endpoint: `new StoreFeaturesDto(f.PayLaterEnabled, f.MultipleProductTypesEnabled, f.ServiceProductsEnabled)`.

- [ ] **Step 3: Pass, then commit**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~StoreProfiles|FullyQualifiedName~StoreFeatures"` and `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~StoreFeaturesDtoTests"` → pass.

Commit: `feat(storehub): /store/features reports ServiceProductsEnabled`.

---

### Task 3: `TillLayout`: which buttons and menu items a store has

**Files:**
- Create: `src/IndyPOS.Application/Common/Models/TillLayout.cs`
- Create: `tests/IndyPOS.Application.Tests/Common/TillLayoutTests.cs`

**Interfaces (Produces):** `sealed record TillLayout(bool ShowHardwareButton, bool ShowServiceButtons, bool ShowAccountsReceivableMenu)` with `static TillLayout For(StoreFeaturesDto features)`.

- [ ] **Step 1: Failing tests**

```csharp
using FluentAssertions;
using IndyPOS.Application.Common.Models;
using Xunit;

namespace IndyPOS.Application.Tests.Common;

public class TillLayoutTests
{
    [Fact]
    public void For_WithMimyShopsFeatures_ShowsServiceButtonsOnly()
    {
        var layout = TillLayout.For(new StoreFeaturesDto(PayLaterEnabled: false, MultipleProductTypesEnabled: false,
                                                         ServiceProductsEnabled: true));

        layout.Should()
              .Be(new TillLayout(ShowHardwareButton: false, ShowServiceButtons: true, ShowAccountsReceivableMenu: false));
    }

    [Fact]
    public void For_WithMimyMartsFeatures_ShowsNoExtraButtonOrMenu()
    {
        var layout = TillLayout.For(new StoreFeaturesDto(false, false, false));

        layout.Should()
              .Be(new TillLayout(false, false, false));
    }

    [Fact]
    public void For_WithGeneralHardwaresFeatures_ShowsTheHardwareButtonAndTheLedger()
    {
        var layout = TillLayout.For(new StoreFeaturesDto(true, true, false));

        layout.Should()
              .Be(new TillLayout(ShowHardwareButton: true, ShowServiceButtons: false, ShowAccountsReceivableMenu: true));
    }
}
```

Run → build error.

- [ ] **Step 2: Implement**

```csharp
namespace IndyPOS.Application.Common.Models;

/// <summary>
/// Which of the till's optional pieces this store has, decided from its store-type features alone. The
/// forms only apply it (WinForms today, Avalonia later).
/// </summary>
public sealed record TillLayout(bool ShowHardwareButton, bool ShowServiceButtons, bool ShowAccountsReceivableMenu)
{
    public static TillLayout For(StoreFeaturesDto features) => new(
        ShowHardwareButton: features.MultipleProductTypesEnabled,
        ShowServiceButtons: features.ServiceProductsEnabled,
        ShowAccountsReceivableMenu: features.PayLaterEnabled);
}
```

- [ ] **Step 3: Pass, then commit**

Commit: `feat(app): TillLayout decides the till's optional buttons and menu per store`.

---

### Task 4: Sale panel: the service buttons

**Files:**
- Modify: `src/IndyPOS.Windows.Forms/UI/Sale/SalePanel.Designer.cs` (two buttons in `panel3`)
- Modify: `src/IndyPOS.Windows.Forms/UI/Sale/SalePanel.cs` (click handlers, `EnsureStoreFeaturesAppliedAsync`)

- [ ] **Step 1: Two buttons in the designer**
  - Add `DeliveryServiceButton` and `DocumentServiceButton`: the same `ModernButton` type and styling as `AddHardwareProductButton` (copy its property block, `Designer.cs` around line 410-430).
  - Positions: `Location = new Point(9, 164)`, `Size = new Size(120, 80)`, `Text = "จัดส่ง"`; and `Location = new Point(136, 164)`, `Size = new Size(120, 80)`, `Text = "เอกสาร"`.
  - Both `Visible = false`. Add both to `panel3.Controls`. Wire `Click += DeliveryServiceButton_Click` / `DocumentServiceButton_Click`. Declare both fields at the bottom with the other controls.
- [ ] **Step 2: Handlers and the layout**

In `SalePanel.cs`:

```csharp
    private async void DeliveryServiceButton_Click(object sender, EventArgs e)
    {
        await _addInvoiceProductForm.ShowDialog(ServiceProductBarcodes.Delivery);
    }

    private async void DocumentServiceButton_Click(object sender, EventArgs e)
    {
        await _addInvoiceProductForm.ShowDialog(ServiceProductBarcodes.Documents);
    }
```

In `EnsureStoreFeaturesAppliedAsync`, replace `AddHardwareProductButton.Visible = features.MultipleProductTypesEnabled;` with:

```csharp
            var layout = TillLayout.For(features);
            AddHardwareProductButton.Visible = layout.ShowHardwareButton;
            DeliveryServiceButton.Visible = layout.ShowServiceButtons;
            DocumentServiceButton.Visible = layout.ShowServiceButtons;
```

Update the failure comment: "Leave the buttons as designed: hardware visible (the server still blocks creating Hardware), services hidden." Add the `using`s for `IndyPOS.Domain.ValueObjects` and `IndyPOS.Application.Common.Models`.

`AddInvoiceProductForm.ShowDialog` already shows `ไม่พบรหัสสินค้า … ในระบบ` when the product is missing (Review Focus 1). No change there.

- [ ] **Step 3: Build and the WinForms suite**

Run: `dotnet build src/IndyPOS.Windows.Forms` → 0 errors; `dotnet test tests/IndyPOS.Windows.Forms.Tests` → all pass.

- [ ] **Step 4: Commit**

Commit: `feat(winforms): MimyShop's จัดส่ง and เอกสาร service buttons on the sale panel`.

---

### Task 5: Menu: รายการลงบัญชี only where PayLater exists, without a gap

**Files:**
- Create: `src/IndyPOS.Windows.Forms/UI/MenuLayout.cs`
- Create: `tests/IndyPOS.Windows.Forms.Tests/UI/MenuLayoutTests.cs`
- Modify: `src/IndyPOS.Windows.Forms/UI/MainForm.cs` (constructor takes `IStoreHubClient`; `OnUserLoggedIn` applies the layout)

**Interfaces (Produces):** `static class MenuLayout { void Collapse(IReadOnlyList<Control> buttonsTopToBottom, int top, int pitch); }`. It places the visible buttons from `top` down, one `pitch` apart.

- [ ] **Step 1: Failing tests**

```csharp
using System.Windows.Forms;
using FluentAssertions;
using IndyPOS.Windows.Forms.UI;
using Xunit;

namespace IndyPOS.Windows.Forms.Tests.UI;

public class MenuLayoutTests
{
    private const int Top = 3;
    private const int Pitch = 121;

    [Fact]
    public void Collapse_WithAHiddenButton_MovesTheButtonsBelowUp()
    {
        var buttons = Buttons(4);
        buttons[1].Visible = false;

        MenuLayout.Collapse(buttons, Top, Pitch);

        buttons.Where(b => b.Visible).Select(b => b.Top).Should()
                                                         .Equal(Top, Top + Pitch, Top + 2 * Pitch);
    }

    // Applied again on every login.
    [Fact]
    public void Collapse_RunTwice_DoesNotMoveButtonsTwice()
    {
        var buttons = Buttons(4);
        buttons[1].Visible = false;
        MenuLayout.Collapse(buttons, Top, Pitch);

        MenuLayout.Collapse(buttons, Top, Pitch);

        buttons[3].Top.Should()
                      .Be(Top + 2 * Pitch);
    }

    [Fact]
    public void Collapse_WithEveryButtonVisible_KeepsTheDesignedPositions()
    {
        var buttons = Buttons(3);

        MenuLayout.Collapse(buttons, Top, Pitch);

        buttons.Select(b => b.Top).Should()
                                  .Equal(Top, Top + Pitch, Top + 2 * Pitch);
    }

    private static List<Control> Buttons(int count) =>
        Enumerable.Range(0, count).Select(i => (Control)new Button { Top = Top + i * Pitch }).ToList();
}
```

The `Visible` getter of an unparented control reads its own state flag, so a plain `Button` works without a form. If `Visible` reads `false` on a button with no parent, test `b.Visible` through a helper `IsShown(Control c) => c.Tag is not "hidden"`, and have `MenuLayout` hide buttons by setting `Visible` and `Tag` together; record that as a ruling.

Run → build error.

- [ ] **Step 2: `MenuLayout`**

```csharp
using System.Windows.Forms;

namespace IndyPOS.Windows.Forms.UI;

/// <summary>Stacks the visible menu buttons with no gap where a hidden one was.</summary>
public static class MenuLayout
{
    public static void Collapse(IReadOnlyList<Control> buttonsTopToBottom, int top, int pitch)
    {
        var next = top;
        foreach (var button in buttonsTopToBottom.Where(b => b.Visible))
        {
            button.Top = next;
            next += pitch;
        }
    }
}
```

- [ ] **Step 3: `MainForm` applies it on login**
  - Add `IStoreHubClient storeHubClient` as the last constructor parameter and store it. DI resolves it; `ConfigureServicesTests` (#114) proves the container still builds.
  - Add:

```csharp
    // The menu buttons in their designed order. Top and pitch are the designer's (3 and 115 + 6).
    private Control[] MenuButtonsTopToBottom =>
        [SaleButton, InventoryButton, UsersButton, ReportsButton, AccountsReceivableButton,
         SettingsButton, LogInButton, CloseApplicationButton];

    private const int MenuTop = 3;
    private const int MenuPitch = 121;

    private async Task ApplyStoreLayoutAsync()
    {
        try
        {
            var layout = TillLayout.For(await _storeHubClient.GetStoreFeaturesAsync());
            AccountsReceivableButton.Visible = layout.ShowAccountsReceivableMenu;
        }
        catch (Exception ex)
        {
            // Keep the designed menu: the ledger's own screen still needs StoreHub to do anything.
            Log.Warning(ex, "Could not load store features for the menu");
        }

        MenuLayout.Collapse(MenuButtonsTopToBottom, MenuTop, MenuPitch);
    }
```

  - Call `await ApplyStoreLayoutAsync();` at the end of `OnUserLoggedIn`. Make it `async void`, since it is an event handler, and keep the rest of its body as it is.
  - Check that MainForm already has a `Log` (Serilog). If not, use the logger the form already uses, or `Serilog.Log`.
  - Check the pitch: Sale 3, Inventory 124, so the pitch is 121. ✓

- [ ] **Step 4: Run, then commit**

Run:
- `dotnet test tests/IndyPOS.Windows.Forms.Tests` → all pass;
- `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~ConfigureServicesTests"` → pass;
- `dotnet build src/IndyPOS.Windows.Forms` → 0 errors.

Commit: `feat(winforms): รายการลงบัญชี only for stores with PayLater, with no gap in the menu`.

---

### Task 6: By hand, docs and counts

- [ ] **Step 1: Each store, by hand** (record in the PR)

For each of `dotnet run --project src/IndyPOS.AppHost -- --store MimyShop`, `MimyMart` and `GeneralHardware`:
1. Start the till (`winforms-app`) and log in as `cashier`.
2. Check:
   - **MimyShop:** the sale panel shows **จัดส่ง and เอกสาร** in the ฮาร์ดแวร์ slot, and no ฮาร์ดแวร์ button. Pressing จัดส่ง opens the price dialog; enter ฿30 and add it. The menu has **no** รายการลงบัญชี, and the buttons below it have moved up.
   - **MimyMart:** no extra buttons, and no รายการลงบัญชี.
   - **GeneralHardware:** ฮาร์ดแวร์ is shown, no service buttons, and รายการลงบัญชี is shown.
3. Log out and back in: the menu is unchanged (no double shift).

- [ ] **Step 2: Docs**
  - **ONBOARDING "Running as a store":** one sentence on what each store's sale panel and menu show.
  - **CLAUDE.md:** nothing beyond the counts.

- [ ] **Step 3: Counts**

Run the whole solution with TRX and sum it with `-LiteralPath`. Expected: Domain +4, Application +4, Windows.Forms +3, and StoreHub unchanged (a theory row was edited, not added). Write the measured numbers into CLAUDE.md and ONBOARDING.

Commit: `docs: per-store sale panel and menu; measured counts`.

---

## Self-Review

**Spec coverage:**
- §3 new flag, barcodes and the `/store/features` field: Tasks 1-2.
- §4.1:
  - ฮาर์ดแวร์ button: Task 4, via `TillLayout`;
  - service buttons and their behaviour: Task 4;
  - a missing product gets the Thai message: Task 4 note (existing behaviour);
  - รายการลงบัญชี only with PayLater: Task 5;
  - the decision in a pure class, not the form: Task 3.
- §5 tests: Domain (Task 1), StoreHub per store (Task 2), `TillLayout` (Task 3), menu layout (Task 5), by hand (Task 6).
- §6 additive and no migration: Task 2's old-StoreHub test.
- Slices 2 and 3 are not in this plan, by design.

**Placeholders:** none. Each "check X" names the exact file or value to read.

**Type consistency:** `StoreTypeFeatures.ServiceProductsEnabled`, `ServiceProductBarcodes.{Delivery,Documents,All}`, `StoreFeaturesDto(..., ServiceProductsEnabled = false)`, `TillLayout(ShowHardwareButton, ShowServiceButtons, ShowAccountsReceivableMenu)` and `MenuLayout.Collapse(IReadOnlyList<Control>, int, int)` are used the same way in every task.
