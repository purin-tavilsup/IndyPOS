# Product-Type Restriction by Store Type — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Enforce `StoreTypeFeatures.MultipleProductTypesEnabled` so a Minimart store deals in General Goods only — hide the Hardware entry points in the WinForms UI and reject Hardware product creation server-side; GeneralHardware is unchanged.

**Architecture:** The server is the source of truth for store-type gating. A new `GET /store/features` endpoint exposes the current store's `StoreTypeFeatures` (computed from the singleton `IStoreIdentityService`). WinForms (a separate process) fetches those flags over HTTP and hides Hardware in the sale screen + product-create category picker. `CreateProductCommandHandler` independently rejects a Hardware product on a general-only store (the real boundary).

**Tech Stack:** C# .NET 10 (`net10.0-windows`), ASP.NET Core minimal APIs, Nokpirab mediator, WinForms (constructor DI), EF Core/PostgreSQL, xUnit + FluentAssertions (+ Moq/AutoFixture in Application.Tests). Spec: `docs/superpowers/specs/2026-07-19-product-type-restriction-design.md`.

## Global Constraints

- Target framework `net10.0-windows`; `Nullable` enable; `ImplicitUsings` enable.
- Supported store types: **GeneralHardware** (General Goods + Hardware) and **Minimart** (General Goods only). `StoreType.CoffeeShop` stays in the enum but is untargeted (behaves like Minimart via `StoreTypeFeatures.For`).
- Product categories are the fixed enum `IndyPOS.Application.Common.Enums.ProductCategory { GeneralGoods = 10, Hardware = 50 }`. The category is stored/transported as the **name string** (`nameof`, e.g. `"Hardware"`, `"GeneralGoods"`) — confirmed by `StoreHubInventoryProductService.GetCategoryName` (maps int→name via `IStoreConstants.ProductCategories`) and `DevelopmentDataSeeder` (`nameof(ProductCategory.GeneralGoods)`).
- `StoreTypeFeatures` (`IndyPOS.Domain.ValueObjects`) already exists: `GeneralHardware` → both flags true; `Minimart`/`CoffeeShop` → both false. **No Domain change to the type.**
- `IStoreIdentityService` is a registered **singleton** exposing `.StoreType` and `.Features` (a `StoreTypeFeatures`), server-side only.
- WinForms authenticated HTTP goes through `StoreHubHttpClient.SendAuthenticatedAsync<T>(HttpMethod, url, content, ct)`.
- Commit after every task. Do not convert existing classes to records or vice versa.

---

## File Structure

**Application (`src/IndyPOS.Application/`)**
- `Common/Models/StoreFeaturesDto.cs` — transport DTO for store feature flags (NEW).
- `Abstractions/StoreHub/IStoreHubClient.cs` — add `GetStoreFeaturesAsync` (MODIFY).
- `UseCases/StoreHub/Products/Create/CreateProductCommandHandler.cs` — add the general-only Hardware guard (MODIFY).

**StoreHub (`src/IndyPOS.StoreHub/Program.cs`)** — add `GET /store/features` endpoint (MODIFY).

**Infrastructure (`src/IndyPOS.Infrastructure/`)**
- `Services/StoreHub/StoreHubHttpClient.cs` — implement `GetStoreFeaturesAsync` (MODIFY).

**WinForms (`src/IndyPOS.Windows.Forms/`)**
- `UI/Sale/SalePanel.cs` — hide `AddHardwareProductButton` when general-only (MODIFY).
- `UI/Inventory/AddNewInventoryProductForm.cs` — filter Hardware from the category combo when general-only (MODIFY).
- `UI/Inventory/AddNewInventoryProductWithCustomBarcodeForm.cs` — same filter (MODIFY).

**Tests**
- `tests/IndyPOS.Domain.Tests/ValueObjects/StoreTypeFeaturesTests.cs` — mapping test (NEW).
- `tests/IndyPOS.Application.Tests/StoreHub/Products/CreateProductCommandHandlerTests.cs` — guard tests (NEW or extend).
- `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/StoreFeaturesEndpointTests.cs` — endpoint test (NEW, Docker-gated).

---

### Task 1: `StoreFeaturesDto` + `GET /store/features` endpoint

**Files:**
- Create: `src/IndyPOS.Application/Common/Models/StoreFeaturesDto.cs`
- Modify: `src/IndyPOS.StoreHub/Program.cs`
- Create: `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/StoreFeaturesEndpointTests.cs`

**Interfaces:**
- Produces: `record StoreFeaturesDto(bool PayLaterEnabled, bool MultipleProductTypesEnabled)` — consumed by Task 3 (client) and this endpoint.

- [ ] **Step 1: Create the DTO**

Create `src/IndyPOS.Application/Common/Models/StoreFeaturesDto.cs`:

```csharp
namespace IndyPOS.Application.Common.Models;

/// <summary>
/// Store feature flags exposed to clients (WinForms) so store-type gating can be
/// applied in the UI. Mirrors <c>IndyPOS.Domain.ValueObjects.StoreTypeFeatures</c>.
/// </summary>
public record StoreFeaturesDto(bool PayLaterEnabled, bool MultipleProductTypesEnabled);
```

- [ ] **Step 2: Write the failing integration test**

Create `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/StoreFeaturesEndpointTests.cs`. The test host's `TestStoreIdentityService` is `StoreType.GeneralHardware` (`StoreHubWebApplicationFactory.cs:87`), so the endpoint must return both flags `true`. Follow the existing endpoint-test style (`[Collection("Integration")]`, `IntegrationTestBase`, `AuthenticateAsCashierAsync`):

```csharp
using System.Net;
using System.Net.Http.Json;
using IndyPOS.Application.Common.Models;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

[Collection("Integration")]
public class StoreFeaturesEndpointTests : IntegrationTestBase
{
    public StoreFeaturesEndpointTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetStoreFeatures_ForGeneralHardwareHost_ReturnsBothFlagsTrue()
    {
        await AuthenticateAsCashierAsync();

        var resp = await Client.GetAsync("/store/features");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<StoreFeaturesDto>(JsonOptions);
        Assert.NotNull(dto);
        Assert.True(dto!.PayLaterEnabled);
        Assert.True(dto.MultipleProductTypesEnabled);
    }

    [Fact]
    public async Task GetStoreFeatures_Unauthenticated_IsRejected()
    {
        ClearAuthentication();

        var resp = await Client.GetAsync("/store/features");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}
```

- [ ] **Step 3: Run the test to verify it fails**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests/IndyPOS.StoreHub.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~StoreFeaturesEndpointTests"`
Expected: FAIL — endpoint not mapped → the OK test gets 404 (or the unauth test may pass incidentally). If Docker/Testcontainers is unavailable, the failure is the Docker error — note it and verify the endpoint by the build + the unit/domain tests instead; this test is validated in CI/VM.

- [ ] **Step 4: Map the endpoint**

In `src/IndyPOS.StoreHub/Program.cs`, near the other `app.MapGet(...)` endpoints (e.g. after `/payment-methods`), add a plain inline endpoint. `IStoreIdentityService` is a DI singleton with `.Features`:

```csharp
app.MapGet("/store/features", (IStoreIdentityService storeIdentity) =>
{
    var f = storeIdentity.Features;
    return Results.Ok(new StoreFeaturesDto(f.PayLaterEnabled, f.MultipleProductTypesEnabled));
}).RequireAuthorization();
```

Add the usings if the file lacks them: `using IndyPOS.Application.Common.Models;` and `using IndyPOS.Application.Common.Interfaces;` (for `IStoreFeaturesDto`/`IStoreIdentityService` — check the existing usings first and only add what's missing).

- [ ] **Step 5: Build to verify**

Run: `dotnet build src/IndyPOS.StoreHub/IndyPOS.StoreHub.csproj -c Release`
Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 6: Run the integration test to verify it passes (if Docker available)**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests/IndyPOS.StoreHub.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~StoreFeaturesEndpointTests"`
Expected: PASS (2 tests) when Docker is running; otherwise Docker-gated (note it).

- [ ] **Step 7: Commit**

```bash
git add src/IndyPOS.Application/Common/Models/StoreFeaturesDto.cs src/IndyPOS.StoreHub/Program.cs tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/StoreFeaturesEndpointTests.cs
git commit -m "feat(storehub): GET /store/features exposes store-type feature flags"
```

---

### Task 2: Server guard — reject Hardware product on a general-only store

**Files:**
- Modify: `src/IndyPOS.Application/UseCases/StoreHub/Products/Create/CreateProductCommandHandler.cs`
- Create: `tests/IndyPOS.Application.Tests/StoreHub/Products/CreateProductCommandHandlerTests.cs` (if absent; otherwise extend the existing file)

**Interfaces:**
- Consumes: `IStoreIdentityService.Features.MultipleProductTypesEnabled`; `ProductCategory` (`IndyPOS.Application.Common.Enums`); `CreateProductCommand.Category` (string name).

- [ ] **Step 1: Write the failing tests**

Create `tests/IndyPOS.Application.Tests/StoreHub/Products/CreateProductCommandHandlerTests.cs`. Use the existing Application.Tests conventions (`[Theory]`/`[CustomAutoData]` + `[Frozen] Mock<...>` if that's the file's style; otherwise plain xUnit + Moq). `MockStoreIdentityService` (in `IndyPOS.Mock`) has a settable `StoreType` and derives `.Features`. The handler needs `IProductRepository`, `IInventoryMovementRepository`, `IStoreIdentityService`, `ILogger<>`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.UseCases.StoreHub.Products;
using IndyPOS.Application.UseCases.StoreHub.Products.Create;
using IndyPOS.Domain.Enums;
using IndyPOS.Mock;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Products;

public class CreateProductCommandHandlerTests
{
    private static CreateProductCommandHandler NewSut(Mock<IProductRepository> products, StoreType storeType) =>
        new(products.Object,
            Mock.Of<IInventoryMovementRepository>(),
            new MockStoreIdentityService { StoreType = storeType },
            NullLogger<CreateProductCommandHandler>.Instance);

    private static CreateProductCommand Command(string category) => new()
    {
        Barcode = "8850000000099", Name = "Test", Category = category, UnitPrice = 10m
    };

    [Fact]
    public async Task HandleAsync_HardwareCategory_OnGeneralOnlyStore_ShouldThrow()
    {
        var products = new Mock<IProductRepository>();
        products.Setup(r => r.ExistsByBarcodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = NewSut(products, StoreType.Minimart);

        var act = () => sut.HandleAsync(Command(nameof(ProductCategory.Hardware)));

        await act.Should().ThrowAsync<InvalidOperationException>();
        products.Verify(r => r.AddAsync(It.IsAny<Domain.Entities.Core.Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_HardwareCategory_OnGeneralHardwareStore_ShouldSucceed()
    {
        var products = new Mock<IProductRepository>();
        products.Setup(r => r.ExistsByBarcodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = NewSut(products, StoreType.GeneralHardware);

        var result = await sut.HandleAsync(Command(nameof(ProductCategory.Hardware)));

        result.Should().NotBeNull();
        products.Verify(r => r.AddAsync(It.IsAny<Domain.Entities.Core.Product>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_GeneralGoodsCategory_OnGeneralOnlyStore_ShouldSucceed()
    {
        var products = new Mock<IProductRepository>();
        products.Setup(r => r.ExistsByBarcodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = NewSut(products, StoreType.Minimart);

        var result = await sut.HandleAsync(Command(nameof(ProductCategory.GeneralGoods)));

        result.Should().NotBeNull();
        products.Verify(r => r.AddAsync(It.IsAny<Domain.Entities.Core.Product>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

> Confirm `Product.ToDto()` doesn't NRE on the minimal product built in the handler (it maps simple fields). If the existing test file already covers CreateProduct, add these three `[Fact]`s to it instead of creating a duplicate class, matching its style.

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/IndyPOS.Application.Tests/IndyPOS.Application.Tests.csproj --filter "FullyQualifiedName~CreateProductCommandHandlerTests"`
Expected: FAIL — the Hardware-on-Minimart test does not throw yet (no guard), so it proceeds to `AddAsync` and the `Times.Never` verify fails (or the throw assertion fails).

- [ ] **Step 3: Add the guard**

In `CreateProductCommandHandler.HandleAsync`, immediately after the barcode-uniqueness check and before building the `Product`, add:

```csharp
        // Store-type gating: a general-only store (e.g. Minimart) may not carry Hardware products.
        var isHardware = string.Equals(command.Category, nameof(ProductCategory.Hardware), StringComparison.OrdinalIgnoreCase);
        if (isHardware && !_storeIdentityService.Features.MultipleProductTypesEnabled)
        {
            _logger.LogWarning("Hardware product creation rejected: StoreType={StoreType}, Barcode={Barcode}",
                _storeIdentityService.StoreType, command.Barcode);
            throw new InvalidOperationException(
                $"Hardware products are not available for {_storeIdentityService.StoreType} stores.");
        }
```

Add `using IndyPOS.Application.Common.Enums;` to the handler if it's not already imported.

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/IndyPOS.Application.Tests/IndyPOS.Application.Tests.csproj --filter "FullyQualifiedName~CreateProductCommandHandlerTests"`
Expected: PASS (3 tests).

- [ ] **Step 5: Confirm the `/products` POST surfaces the exception**

Read the `/products` POST endpoint in `src/IndyPOS.StoreHub/Program.cs` (~line 398). `CreateProductCommandHandler` already throws `InvalidOperationException` for the duplicate-barcode case, so the endpoint's existing handling of that exception now also covers this guard — no endpoint change needed. Verify by inspection that an `InvalidOperationException` from the handler yields a 4xx (not an unhandled 500). If the endpoint has no try/catch and relies on middleware, confirm the middleware maps it; if it explicitly catches the duplicate case only, note it as an observation (the client surfaces the message regardless via `StoreHubClientException`).

- [ ] **Step 6: Build + commit**

Run: `dotnet build -c Release` → 0 errors.
```bash
git add src/IndyPOS.Application/UseCases/StoreHub/Products/Create/CreateProductCommandHandler.cs tests/IndyPOS.Application.Tests/StoreHub/Products/CreateProductCommandHandlerTests.cs
git commit -m "feat(products): reject Hardware product creation on general-only stores"
```

---

### Task 3: WinForms client — `GetStoreFeaturesAsync`

**Files:**
- Modify: `src/IndyPOS.Application/Abstractions/StoreHub/IStoreHubClient.cs`
- Modify: `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubHttpClient.cs`

**Interfaces:**
- Consumes: `StoreFeaturesDto` (Task 1).
- Produces: `IStoreHubClient.GetStoreFeaturesAsync(CancellationToken = default) : Task<StoreFeaturesDto>`.

- [ ] **Step 1: Add the interface member**

In `src/IndyPOS.Application/Abstractions/StoreHub/IStoreHubClient.cs`, add (with an XML-doc comment matching the file's style; add `using IndyPOS.Application.Common.Models;` if absent):

```csharp
    /// <summary>
    /// Get the current store's feature flags (store-type gating).
    /// Requires authentication.
    /// </summary>
    Task<StoreFeaturesDto> GetStoreFeaturesAsync(CancellationToken cancellationToken = default);
```

- [ ] **Step 2: Implement in `StoreHubHttpClient`**

In `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubHttpClient.cs`, add near the other authenticated GETs (e.g. `GetOfferablePaymentMethodsAsync`), reusing the generic authenticated-GET helper:

```csharp
    public Task<StoreFeaturesDto> GetStoreFeaturesAsync(CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync<StoreFeaturesDto>(HttpMethod.Get, "/store/features", content: null, cancellationToken);
```

Add `using IndyPOS.Application.Common.Models;` if absent.

- [ ] **Step 3: Build to verify**

Run: `dotnet build -c Release`
Expected: `Build succeeded. 0 Error(s)`. (Confirm `StoreHubHttpClient` is the only `IStoreHubClient` implementer — a build error would reveal any other implementer needing the new member. As of Task 8 of the payment-methods epic it was the sole implementer.)

- [ ] **Step 4: Commit**

```bash
git add src/IndyPOS.Application/Abstractions/StoreHub/IStoreHubClient.cs src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubHttpClient.cs
git commit -m "feat(client): StoreHub client method for store feature flags"
```

---

### Task 4: WinForms — hide the Add Hardware button on general-only stores

**Files:**
- Modify: `src/IndyPOS.Windows.Forms/UI/Sale/SalePanel.cs`

**Interfaces:**
- Consumes: `IStoreHubClient.GetStoreFeaturesAsync` (Task 3). `SalePanel` already holds `_storeHubClient` (injected for `EnsurePaymentMethodNamesLoadedAsync`) and has `AddHardwareProductButton` (a designer control).

- [ ] **Step 1: Add a one-time feature-gating apply method**

In `src/IndyPOS.Windows.Forms/UI/Sale/SalePanel.cs`, add a field and an idempotent apply method (mirror the existing `EnsurePaymentMethodNamesLoadedAsync` pattern — try/catch to `MessageForm`, guard so it runs once):

```csharp
    private bool _storeFeaturesApplied;

    private async Task EnsureStoreFeaturesAppliedAsync()
    {
        if (_storeFeaturesApplied) return;

        try
        {
            var features = await _storeHubClient.GetStoreFeaturesAsync();
            AddHardwareProductButton.Visible = features.MultipleProductTypesEnabled;
            _storeFeaturesApplied = true;
        }
        catch (Exception ex)
        {
            _messageForm.ShowDialog($"ไม่สามารถโหลดการตั้งค่าร้านค้าได้ Error: {ex.Message}", "ข้อผิดพลาด");
            // Leave the button as designed (visible) on failure — server guard still blocks Hardware creation.
        }
    }
```

> Confirm the exact field names in `SalePanel` for the client (`_storeHubClient`) and message form (`_messageForm`) and match them. If the client field has a different name, use it.

- [ ] **Step 2: Call it when the panel becomes active**

`SalePanel` is shown via `MainForm.SwitchToPanel`. Add a `VisibleChanged` handler (or reuse an existing show/activate seam if present) that applies the gating on first display. In the `SalePanel` constructor (after `InitializeComponent()` / where events are wired in `SubscribeEvents`), subscribe:

```csharp
        VisibleChanged += async (_, _) => { if (Visible) await EnsureStoreFeaturesAppliedAsync(); };
```

> If `SalePanel` already has a `Load`/`VisibleChanged` handler, add the `await EnsureStoreFeaturesAppliedAsync();` call there instead of adding a second handler.

- [ ] **Step 3: Build to verify**

Run: `dotnet build src/IndyPOS.Windows.Forms/IndyPOS.Windows.Forms.csproj -c Release`
Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 4: Commit**

```bash
git add src/IndyPOS.Windows.Forms/UI/Sale/SalePanel.cs
git commit -m "feat(winforms): hide Add Hardware button on general-only stores"
```

---

### Task 5: WinForms — drop Hardware from the product-create category picker

**Files:**
- Modify: `src/IndyPOS.Windows.Forms/UI/Inventory/AddNewInventoryProductForm.cs`
- Modify: `src/IndyPOS.Windows.Forms/UI/Inventory/AddNewInventoryProductWithCustomBarcodeForm.cs`

**Interfaces:**
- Consumes: `IStoreHubClient.GetStoreFeaturesAsync` (Task 3); `ProductCategory` (`IndyPOS.Application.Common.Enums`). Both forms populate a `CategoryComboBox` from `_productCategoryDictionary` (`IStoreConstants.ProductCategories`, `{10:"GeneralGoods", 50:"Hardware"}`) in a `PopulateProductCategoryComboBox`-style method.

- [ ] **Step 1: Inject the client (if not already present)**

Check each form's constructor. `AddNewInventoryProductForm(IStoreConstants storeConstants, ...)` — add an `IStoreHubClient storeHubClient` constructor parameter and store it in a field, if the form doesn't already have one. These forms are DI-constructed (registered in `IndyPOS.Windows.Forms/ConfigureServices.cs`); adding a ctor param is resolved automatically. Do the same for `AddNewInventoryProductWithCustomBarcodeForm`.

- [ ] **Step 2: Filter Hardware from the category combo when general-only**

Locate the method that populates the category combo (in `AddNewInventoryProductForm` it clears `CategoryComboBox.Items` and loops `_productCategoryDictionary` adding `item.Value`). Make it async-aware or gate before populating. Fetch features and exclude the Hardware entry when general-only. Change the populate to:

```csharp
    private async Task PopulateProductCategoryComboBoxAsync()
    {
        bool multipleTypes = true;
        try { multipleTypes = (await _storeHubClient.GetStoreFeaturesAsync()).MultipleProductTypesEnabled; }
        catch { /* on failure, fall back to showing all categories; server still guards creation */ }

        CategoryComboBox.Items.Clear();
        foreach (var item in _productCategoryDictionary)
        {
            if (!multipleTypes && item.Key == (int)ProductCategory.Hardware)
                continue; // Hardware hidden on general-only stores
            CategoryComboBox.Items.Add(item.Value);
        }
    }
```

Add `using IndyPOS.Application.Common.Enums;` if absent. Update the caller (the `ShowDialog`/reset path that previously called the synchronous populate) to `await PopulateProductCategoryComboBoxAsync();`. If the form's `ShowDialog` is currently synchronous (`void`), change it to `async Task ShowDialog(...)` and update its caller(s) in `InventoryPanel` to `await` it — follow the async-dialog pattern already used by `AddInvoiceProductForm.ShowDialog`/`AcceptPaymentForm.ShowDialog`. Keep the existing validation that rejects an unselected/invalid category unchanged (it already rejects anything not in `_productCategoryDictionary.Values`, and now Hardware simply isn't offered).

> Apply the same change to `AddNewInventoryProductWithCustomBarcodeForm` (it has the equivalent category-combo populate).

- [ ] **Step 3: Build to verify**

Run: `dotnet build src/IndyPOS.Windows.Forms/IndyPOS.Windows.Forms.csproj -c Release`
Expected: `Build succeeded. 0 Error(s)`. Fix any caller that broke from the sync→async `ShowDialog` change (the compiler will point them out).

- [ ] **Step 4: Commit**

```bash
git add src/IndyPOS.Windows.Forms/UI/Inventory/AddNewInventoryProductForm.cs src/IndyPOS.Windows.Forms/UI/Inventory/AddNewInventoryProductWithCustomBarcodeForm.cs
git commit -m "feat(winforms): hide Hardware category in product create on general-only stores"
```

---

### Task 6: Domain mapping test + full verification

**Files:**
- Create: `tests/IndyPOS.Domain.Tests/ValueObjects/StoreTypeFeaturesTests.cs`

- [ ] **Step 1: Write the `StoreTypeFeatures.For` mapping test**

No dedicated test exists today. Create `tests/IndyPOS.Domain.Tests/ValueObjects/StoreTypeFeaturesTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Domain.Enums;
using IndyPOS.Domain.ValueObjects;
using Xunit;

namespace IndyPOS.Domain.Tests.ValueObjects;

public class StoreTypeFeaturesTests
{
    [Fact]
    public void For_GeneralHardware_EnablesBothFeatures()
    {
        var f = StoreTypeFeatures.For(StoreType.GeneralHardware);
        f.PayLaterEnabled.Should().BeTrue();
        f.MultipleProductTypesEnabled.Should().BeTrue();
    }

    [Theory]
    [InlineData(StoreType.Minimart)]
    [InlineData(StoreType.CoffeeShop)]
    public void For_GeneralOnlyStoreTypes_DisableBothFeatures(StoreType storeType)
    {
        var f = StoreTypeFeatures.For(storeType);
        f.PayLaterEnabled.Should().BeFalse();
        f.MultipleProductTypesEnabled.Should().BeFalse();
    }
}
```

- [ ] **Step 2: Run the Domain test**

Run: `dotnet test tests/IndyPOS.Domain.Tests/IndyPOS.Domain.Tests.csproj`
Expected: PASS (existing + 3 new cases).

- [ ] **Step 3: Full build + affected suites**

Run: `dotnet build -c Release` → 0 errors.
Run: `dotnet test tests/IndyPOS.Domain.Tests/IndyPOS.Domain.Tests.csproj` → pass.
Run: `dotnet test tests/IndyPOS.Application.Tests/IndyPOS.Application.Tests.csproj` → pass.
Run (if Docker up): `dotnet test tests/IndyPOS.StoreHub.IntegrationTests/IndyPOS.StoreHub.IntegrationTests.csproj` → pass (incl. `StoreFeaturesEndpointTests`).

- [ ] **Step 4: Commit**

```bash
git add tests/IndyPOS.Domain.Tests/ValueObjects/StoreTypeFeaturesTests.cs
git commit -m "test(domain): StoreTypeFeatures.For mapping per store type"
```

- [ ] **Step 5: VM validation note (manual, post-merge)**

The WinForms gating (hidden Add Hardware button; Hardware absent from the product-create category picker) has no automated harness. Validate live on a **Minimart** VM install (`IndyPOS-Setup.exe --silent --store-id <ID> --store-type Minimart`) alongside the payment-methods smoke: confirm the sale screen shows no Add Hardware button, the product-create category combo offers only General Goods, and a direct Hardware create is rejected. A GeneralHardware install still shows both. Record in the SDD ledger.

---

## Notes for the implementer

- The server guard (Task 2) is the real boundary; the WinForms hiding (Tasks 4–5) is UX. Both consume the same flag but independently (server via `IStoreIdentityService`, WinForms via `GET /store/features`).
- On a features-fetch failure, the WinForms code falls back to showing everything — the server guard still prevents a bad Hardware create, so this fails safe.
- Do NOT change `StoreTypeFeatures`, `ProductCategory`, or remove `StoreType.CoffeeShop`.

## Self-Review (completed by author)

- **Spec coverage:** store-features endpoint (T1), server guard (T2), WinForms client (T3), sale-screen hide (T4), product-create category filter (T5), Domain mapping test + verification + VM note (T6). All spec §Design points + §Testing map to a task.
- **Placeholder scan:** the few "confirm the exact field name / caller" notes are genuine implementation-time lookups against real files (named exactly), not vague hand-waves. No TBD/TODO.
- **Type consistency:** `StoreFeaturesDto(bool PayLaterEnabled, bool MultipleProductTypesEnabled)`, `GetStoreFeaturesAsync`, `ProductCategory.Hardware`/`nameof`, `MultipleProductTypesEnabled`, `AddHardwareProductButton` used consistently across tasks.
