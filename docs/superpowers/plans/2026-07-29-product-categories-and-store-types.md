# Data-driven product categories + store types — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the two-value `ProductCategory` enum with a store-scoped `product_category` catalogue so each of the three stores carries its own real category set, and add `MimyShop` as a store type.

**Architecture:** Mirror Epic M's `payment_method` catalogue exactly — same composite key, same repository shape, same idempotent seeder, same endpoint shape. A `Kind` column (`GeneralGoods`/`Hardware`/`Service`) carries the product-type gate that is currently a string comparison against an enum name.

**Tech Stack:** .NET 10, EF Core (Npgsql), Nokpirab mediator (handlers registered individually via `AddTransient`), xUnit + FluentAssertions, Testcontainers PostgreSQL for integration tests.

**Spec:** `docs/superpowers/specs/2026-07-29-product-categories-and-store-types-design.md`

## Global Constraints

- **Composite primary key `(StoreId, Code)`** — no `Guid Id`. This deliberately follows `PaymentMethod` (`PaymentMethodConfiguration` line 11: `builder.HasKey(e => new { e.StoreId, e.Code })`) rather than CLAUDE.md's general "all new entities use `Guid Id`" rule. Catalogue tables are keyed by their natural key in this codebase. **This corrects §3 of the spec**, which listed a `Guid Id`.
- **Table and column names are snake_case** (`product_category`, `display_name`) — see `PaymentMethodConfiguration`.
- **Enum backing values are persisted** via `HasConversion<int>()`. Never reuse or renumber a value.
- **`StoreType` value `3` is retired, not reused.** `CoffeeShop = 3` is deleted; `MimyShop = 4`.
- **Every repository method filters by `IStoreIdentityService.StoreId`** and `AddAsync` overwrites the caller's `StoreId`. Never trust a caller's store id.
- **Seeders are idempotent and non-fatal**: insert only an absent `Code`, log, never throw into startup.
- Thai display names are UTF-8 string literals in C# source. C# source files are UTF-8; this is safe (`PaymentMethodSeeder` already does it).
- Migration tool changes are **out of scope** — that is Epic 2. This plan only unblocks it.

---

## File Structure

**Domain**
- Create `src/IndyPOS.Domain/Enums/ProductCategoryKind.cs` — the kind enum.
- Create `src/IndyPOS.Domain/Entities/Core/ProductCategory.cs` — the catalogue entity.
- Create `src/IndyPOS.Domain/ValueObjects/ProductCategoryPolicy.cs` — which kinds a store type may use. Pure.
- Delete `src/IndyPOS.Domain/Entities/ProductCategory.cs` — dead code, verified zero references.
- Modify `src/IndyPOS.Domain/Enums/StoreType.cs`, `src/IndyPOS.Domain/ValueObjects/StoreTypeFeatures.cs`.

**Application**
- Create `src/IndyPOS.Application/Common/Constants/ProductCategoryCodes.cs` — the stable codes.
- Create `src/IndyPOS.Application/Abstractions/StoreHub/Repositories/IProductCategoryRepository.cs`.
- Create `src/IndyPOS.Application/Common/Exceptions/UnknownProductCategoryException.cs` — maps to 400.
- Create `src/IndyPOS.Application/UseCases/StoreHub/ProductCategories/ProductCategoryDto.cs`, `GetProductCategoriesQuery.cs`, `GetProductCategoriesQueryHandler.cs`.
- Delete `src/IndyPOS.Application/Common/Enums/ProductCategories.cs`.
- Modify the two product handlers.

**Infrastructure**
- Create `.../Persistence/StoreHub/Configurations/ProductCategoryConfiguration.cs`.
- Create `.../Persistence/StoreHub/Repositories/ProductCategoryRepository.cs`.
- Create `.../Persistence/StoreHub/Seeders/ProductCategorySeeder.cs` + its seed tables.
- Modify `StoreHubDbContext.cs`, `StoreHubDbContextExtensions.cs`, `ConfigureServices.cs`, `HardcodedStoreConstants.cs`, `StoreHubInventoryProductService.cs`, `GetLegacySalesSummaryQueryHandler.cs`, `StoreHubHttpClient.cs`.
- EF migration `AddProductCategoryTable`.

**StoreHub** — modify `Program.cs` (DI + endpoint + seeding call).

**WinForms** — modify the three inventory forms.

**Installer** — modify `SilentArgs.cs` usage text; the wizard picker.

---

## Task 1: ProductCategoryKind + the store-type policy

Pure Domain, no infrastructure. Establishes the vocabulary every later task uses.

**Files:**
- Create: `src/IndyPOS.Domain/Enums/ProductCategoryKind.cs`
- Create: `src/IndyPOS.Domain/ValueObjects/ProductCategoryPolicy.cs`
- Test: `tests/IndyPOS.Domain.Tests/ValueObjects/ProductCategoryPolicyTests.cs`

**Interfaces:**
- Consumes: `StoreTypeFeatures` (`IndyPOS.Domain.ValueObjects`), `StoreType` (`IndyPOS.Domain.Enums`).
- Produces:
  - `enum ProductCategoryKind { GeneralGoods = 1, Hardware = 2, Service = 3 }`
  - `static class ProductCategoryPolicy` with `static bool IsUsable(ProductCategoryKind kind, StoreTypeFeatures features)`

- [ ] **Step 1: Write the failing test**

Create `tests/IndyPOS.Domain.Tests/ValueObjects/ProductCategoryPolicyTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Domain.Enums;
using IndyPOS.Domain.ValueObjects;

namespace IndyPOS.Domain.Tests.ValueObjects;

public class ProductCategoryPolicyTests
{
    [Fact]
    public void IsUsable_WithGeneralGoods_ShouldAlwaysBeTrue()
    {
        // Every store sells general goods; this is the one kind with no gate.
        foreach (var storeType in Enum.GetValues<StoreType>())
        {
            ProductCategoryPolicy
                .IsUsable(ProductCategoryKind.GeneralGoods, StoreTypeFeatures.For(storeType))
                .Should().BeTrue($"{storeType} must be able to sell general goods");
        }
    }

    [Fact]
    public void IsUsable_WithHardwareOnAGeneralOnlyStore_ShouldBeFalse()
    {
        var features = StoreTypeFeatures.For(StoreType.Minimart);

        ProductCategoryPolicy.IsUsable(ProductCategoryKind.Hardware, features).Should().BeFalse();
    }

    [Fact]
    public void IsUsable_WithHardwareOnAMultiTypeStore_ShouldBeTrue()
    {
        var features = StoreTypeFeatures.For(StoreType.GeneralHardware);

        ProductCategoryPolicy.IsUsable(ProductCategoryKind.Hardware, features).Should().BeTrue();
    }

    [Fact]
    public void IsUsable_WithServiceOnAGeneralOnlyStore_ShouldBeFalse()
    {
        // Services are a non-general kind, so the same gate applies. MimyShop seeds a
        // Services category but cannot use it until the feature exists - see the spec's
        // "Known gap" section. Failing closed is the safe direction.
        var features = StoreTypeFeatures.For(StoreType.Minimart);

        ProductCategoryPolicy.IsUsable(ProductCategoryKind.Service, features).Should().BeFalse();
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests/IndyPOS.Domain.Tests --filter FullyQualifiedName~ProductCategoryPolicyTests`
Expected: FAIL — `ProductCategoryKind` and `ProductCategoryPolicy` do not exist (CS0246).

- [ ] **Step 3: Write the enum**

Create `src/IndyPOS.Domain/Enums/ProductCategoryKind.cs`:

```csharp
namespace IndyPOS.Domain.Enums;

/// <summary>
/// Classifies a catalogue product category. Carries the product-type gate that used to be a
/// string comparison against an enum name.
/// <para>Backing values are persisted (see ProductCategoryConfiguration), so never reuse one.</para>
/// </summary>
public enum ProductCategoryKind
{
    /// <summary>Everyday retail goods. Every store type may sell these.</summary>
    GeneralGoods = 1,

    /// <summary>Building materials and tools. GeneralHardware only.</summary>
    Hardware = 2,

    /// <summary>
    /// A service rather than stock (e.g. MimyShop's บริการ). Recorded so the category has a
    /// home; non-stock BEHAVIOUR is not implemented and needs Product.IsTrackable.
    /// </summary>
    Service = 3
}
```

- [ ] **Step 4: Write the policy**

Create `src/IndyPOS.Domain/ValueObjects/ProductCategoryPolicy.cs`:

```csharp
using IndyPOS.Domain.Enums;

namespace IndyPOS.Domain.ValueObjects;

/// <summary>
/// Whether a store may use a category of a given kind. The single place this rule lives, so the
/// create handler, the update handler and the UI cannot drift apart.
/// </summary>
public static class ProductCategoryPolicy
{
    public static bool IsUsable(ProductCategoryKind kind, StoreTypeFeatures features) =>
        kind == ProductCategoryKind.GeneralGoods || features.MultipleProductTypesEnabled;
}
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `dotnet test tests/IndyPOS.Domain.Tests --filter FullyQualifiedName~ProductCategoryPolicyTests`
Expected: PASS, 4 tests.

- [ ] **Step 6: Commit**

```bash
git add src/IndyPOS.Domain/Enums/ProductCategoryKind.cs src/IndyPOS.Domain/ValueObjects/ProductCategoryPolicy.cs tests/IndyPOS.Domain.Tests/ValueObjects/ProductCategoryPolicyTests.cs
git commit -m "feat(domain): add ProductCategoryKind and the store-type usability policy"
```

---

## Task 2: Add MimyShop, remove CoffeeShop

`CoffeeShop` has 9 code references. Removing it and adding `MimyShop` together keeps the enum compiling in one step.

**Files:**
- Modify: `src/IndyPOS.Domain/Enums/StoreType.cs`
- Modify: `src/IndyPOS.Domain/ValueObjects/StoreTypeFeatures.cs`
- Modify: `tests/IndyPOS.Domain.Tests/ValueObjects/StoreTypeFeaturesTests.cs:20`
- Modify: `tests/IndyPOS.Domain.Tests/ValueObjects/PaymentMethodPolicyTests.cs:29`
- Modify: `tests/IndyPOS.Mock/MockStoreIdentityService.cs:41-45`
- Modify: `installer/IndyPOS.Bootstrapper/Silent/SilentArgs.cs:71`
- Modify: `tests/IndyPOS.Bootstrapper.Tests/Silent/SilentArgsTests.cs:40`

**Interfaces:**
- Consumes: nothing new.
- Produces: `StoreType.MimyShop = 4`; `MockStoreIdentityService.MimyShop()` replacing `.CoffeeShop()`.

- [ ] **Step 1: Write the failing test**

In `tests/IndyPOS.Domain.Tests/ValueObjects/StoreTypeFeaturesTests.cs`, replace the `[InlineData(StoreType.CoffeeShop)]` line (line 20) with `[InlineData(StoreType.MimyShop)]`, then append this test to the class:

```csharp
    [Fact]
    public void For_WithMimyShop_ShouldMatchMinimart()
    {
        // MimyShop differs from a minimart only in its seeded category set today. Modelling it
        // as a real store type is deliberate (services and reporting will diverge), so this
        // test pins that the flags are intentionally identical rather than accidentally copied.
        StoreTypeFeatures.For(StoreType.MimyShop)
            .Should().BeEquivalentTo(StoreTypeFeatures.For(StoreType.Minimart));
    }

    [Fact]
    public void StoreType_ShouldNotDefineCoffeeShop()
    {
        // Removed 2026-07-29: coffee shops need a dedicated app. Value 3 stays reserved so a
        // future type cannot silently inherit persisted CoffeeShop rows.
        Enum.GetNames<StoreType>().Should().NotContain("CoffeeShop");
        Enum.IsDefined(typeof(StoreType), 3).Should().BeFalse();
    }
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests/IndyPOS.Domain.Tests --filter FullyQualifiedName~StoreTypeFeaturesTests`
Expected: FAIL to compile — `StoreType.MimyShop` does not exist.

- [ ] **Step 3: Update the enum**

Replace the body of `src/IndyPOS.Domain/Enums/StoreType.cs`:

```csharp
namespace IndyPOS.Domain.Enums;

/// <summary>
/// Type of store, determines available features.
/// Immutable after installation.
/// </summary>
public enum StoreType
{
    /// <summary>
    /// Full features: PayLater, multiple product types, all payment methods.
    /// </summary>
    GeneralHardware = 1,

    /// <summary>
    /// Limited features: No PayLater, general products only.
    /// </summary>
    Minimart = 2,

    // 3 was CoffeeShop, removed 2026-07-29 - its products and services differ enough to
    // deserve a dedicated app. Do NOT reuse the value: any store row still carrying 3 must
    // fail to bind rather than silently become a different type.

    /// <summary>
    /// Gift/lifestyle shop. Same feature flags as <see cref="Minimart"/> today; it exists as its
    /// own type because its product categories differ, and services plus reporting are expected
    /// to diverge. Payment methods are expected to CONVERGE with Minimart, so do not add a
    /// payment flag here.
    /// </summary>
    MimyShop = 4
}
```

- [ ] **Step 4: Update the feature map**

In `src/IndyPOS.Domain/ValueObjects/StoreTypeFeatures.cs`, replace the `StoreType.CoffeeShop => ...` arm with:

```csharp
        StoreType.MimyShop => new StoreTypeFeatures
        {
            PayLaterEnabled = false,
            MultipleProductTypesEnabled = false
        },
```

- [ ] **Step 5: Update the remaining CoffeeShop references**

- `tests/IndyPOS.Domain.Tests/ValueObjects/PaymentMethodPolicyTests.cs:29` — change `[InlineData(StoreType.CoffeeShop)]` to `[InlineData(StoreType.MimyShop)]`.
- `tests/IndyPOS.Mock/MockStoreIdentityService.cs` — rename the factory:

```csharp
    /// <summary>
    /// Creates a mock configured for a MimyShop store (PayLater disabled).
    /// </summary>
    public static MockStoreIdentityService MimyShop() => new()
    {
        StoreType = StoreType.MimyShop
    };
```

- `installer/IndyPOS.Bootstrapper/Silent/SilentArgs.cs:71` — change the usage message to:

```csharp
                        return ParseResult.Usage(
                            "--store-type must be one of: GeneralHardware, Minimart, MimyShop.");
```

- `tests/IndyPOS.Bootstrapper.Tests/Silent/SilentArgsTests.cs:40` — change `[InlineData("CoffeeShop", StoreType.CoffeeShop)]` to `[InlineData("MimyShop", StoreType.MimyShop)]`.

- [ ] **Step 6: Confirm no persisted CoffeeShop value exists**

The spec requires checking before deletion. Run:

```bash
grep -rn "CoffeeShop" --include=*.json --include=*.ps1 --include=*.psd1 . | grep -v node_modules
```

Expected: no hits in `appsettings*.json`, installer scripts, or VM test config. Documentation hits are fine and are updated in Task 11. If a real config hit appears, STOP and report it — a live store carrying `Type=CoffeeShop` would fail to bind after this change.

- [ ] **Step 7: Run the affected suites**

Run:
```bash
dotnet test tests/IndyPOS.Domain.Tests
dotnet test tests/IndyPOS.Bootstrapper.Tests --filter FullyQualifiedName~SilentArgsTests
dotnet build IndyPOS.sln -c Debug
```
Expected: Domain tests pass, SilentArgs tests pass, solution builds (the mock rename may surface call sites — fix any `MockStoreIdentityService.CoffeeShop()` callers to `.MimyShop()`).

- [ ] **Step 8: Commit**

```bash
git add src/IndyPOS.Domain tests/IndyPOS.Domain.Tests tests/IndyPOS.Mock installer/IndyPOS.Bootstrapper/Silent/SilentArgs.cs tests/IndyPOS.Bootstrapper.Tests/Silent/SilentArgsTests.cs
git commit -m "feat(domain): add MimyShop store type and remove CoffeeShop"
```

---

## Task 3: The ProductCategory entity, EF mapping and migration

**Files:**
- Create: `src/IndyPOS.Domain/Entities/Core/ProductCategory.cs`
- Delete: `src/IndyPOS.Domain/Entities/ProductCategory.cs`
- Create: `src/IndyPOS.Infrastructure/Persistence/StoreHub/Configurations/ProductCategoryConfiguration.cs`
- Modify: `src/IndyPOS.Infrastructure/Persistence/StoreHub/StoreHubDbContext.cs:25` (add the DbSet after `PaymentMethods`)
- Test: `tests/IndyPOS.StoreHub.IntegrationTests/ProductCategoryPersistenceTests.cs`

**Interfaces:**
- Consumes: `ProductCategoryKind` (Task 1).
- Produces:
  - `IndyPOS.Domain.Entities.Core.ProductCategory` with `string StoreId, string Code, string DisplayName, ProductCategoryKind Kind, bool IsEnabled, int DisplayOrder, DateTime CreatedUtc, DateTime LastModifiedUtc`
  - `StoreHubDbContext.ProductCategories`

- [ ] **Step 1: Confirm the legacy entity is dead before deleting it**

Run:

```bash
grep -rn "Entities\.ProductCategory\|new ProductCategory(" --include=*.cs src/ tests/ | grep -v "Entities/Core" | grep -v Designer
```

Expected: only `src/IndyPOS.Domain/Entities/ProductCategory.cs` itself. If anything else appears, STOP: it must be migrated to the new entity or the file kept and renamed `LegacyProductCategory` instead of deleted.

- [ ] **Step 2: Write the failing test**

Create `tests/IndyPOS.StoreHub.IntegrationTests/ProductCategoryPersistenceTests.cs`. Follow the fixture pattern used by the existing payment-method integration tests in this project (`IntegrationTestBase` provisions the schema via `EnsureCreatedAsync`):

```csharp
using FluentAssertions;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.StoreHub.IntegrationTests;

public class ProductCategoryPersistenceTests : IntegrationTestBase
{
    [Fact]
    public async Task ProductCategory_ShouldRoundTripThroughPostgres()
    {
        var now = DateTime.UtcNow;

        DbContext.ProductCategories.Add(new ProductCategory
        {
            StoreId = "STORE-A",
            Code = "PlumbingMaterials",
            DisplayName = "วัสดุและอุปกรณ์ระบบประปา",
            Kind = ProductCategoryKind.Hardware,
            IsEnabled = true,
            DisplayOrder = 14,
            CreatedUtc = now,
            LastModifiedUtc = now
        });
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        var loaded = await DbContext.ProductCategories.AsNoTracking()
            .SingleAsync(c => c.StoreId == "STORE-A" && c.Code == "PlumbingMaterials");

        loaded.Kind.Should().Be(ProductCategoryKind.Hardware);
        loaded.DisplayName.Should().Be("วัสดุและอุปกรณ์ระบบประปา", "Thai labels must survive the round trip");
        loaded.DisplayOrder.Should().Be(14);
    }

    [Fact]
    public async Task ProductCategory_ShouldAllowTheSameCodeInDifferentStores()
    {
        // The whole point of store scoping: MimyShop's Toys and GeneralHardware's Toys are
        // different rows, and a global unique constraint would reject the second store.
        var now = DateTime.UtcNow;

        foreach (var storeId in new[] { "STORE-B", "STORE-C" })
        {
            DbContext.ProductCategories.Add(new ProductCategory
            {
                StoreId = storeId, Code = "Toys", DisplayName = "ของเล่น",
                Kind = ProductCategoryKind.GeneralGoods, IsEnabled = true, DisplayOrder = 1,
                CreatedUtc = now, LastModifiedUtc = now
            });
        }

        var act = async () => await DbContext.SaveChangesAsync();

        await act.Should().NotThrowAsync();
    }
}
```

- [ ] **Step 3: Run the test to verify it fails**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter FullyQualifiedName~ProductCategoryPersistenceTests`
Expected: FAIL to compile — `ProductCategories` is not a member of `StoreHubDbContext`. (Docker must be running.)

- [ ] **Step 4: Create the entity and delete the dead one**

Create `src/IndyPOS.Domain/Entities/Core/ProductCategory.cs`:

```csharp
using IndyPOS.Domain.Enums;

namespace IndyPOS.Domain.Entities.Core;

/// <summary>
/// A product category available in this store. Rows are data (not a hardcoded enum) because the
/// three store types carry genuinely different category sets, and legacy category ids collide
/// across them — id 10 is เบ็ดเตล็ด (misc) in GeneralHardware and ของขวัญ (gifts) in MimyShop.
/// <para>Code is the stable key stored on <see cref="Product.Category"/> and used in reports.
/// Codes are shared across stores where the meaning genuinely matches, which is what makes
/// cross-store reporting a plain GROUP BY.</para>
/// </summary>
public class ProductCategory
{
    public string StoreId { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public ProductCategoryKind Kind { get; set; }
    public bool IsEnabled { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }
}
```

Delete `src/IndyPOS.Domain/Entities/ProductCategory.cs` (confirmed dead in Step 1).

- [ ] **Step 5: Add the EF configuration and DbSet**

Create `src/IndyPOS.Infrastructure/Persistence/StoreHub/Configurations/ProductCategoryConfiguration.cs`:

```csharp
using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Configurations;

public class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("product_category");
        builder.HasKey(e => new { e.StoreId, e.Code });

        builder.Property(e => e.StoreId).HasColumnName("store_id").HasMaxLength(50).IsRequired();
        builder.Property(e => e.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Kind).HasColumnName("kind").HasConversion<int>().IsRequired();
        builder.Property(e => e.IsEnabled).HasColumnName("is_enabled").IsRequired();
        builder.Property(e => e.DisplayOrder).HasColumnName("display_order").IsRequired();
        builder.Property(e => e.CreatedUtc).HasColumnName("created_utc").IsRequired();
        builder.Property(e => e.LastModifiedUtc).HasColumnName("last_modified_utc").IsRequired();
    }
}
```

In `src/IndyPOS.Infrastructure/Persistence/StoreHub/StoreHubDbContext.cs`, add after the `PaymentMethods` line:

```csharp
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
```

- [ ] **Step 6: Run the test to verify it passes**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter FullyQualifiedName~ProductCategoryPersistenceTests`
Expected: PASS, 2 tests.

- [ ] **Step 7: Create the EF migration**

Run:

```bash
dotnet ef migrations add AddProductCategoryTable \
  --project src/IndyPOS.Infrastructure \
  --startup-project src/IndyPOS.StoreHub \
  --context StoreHubDbContext
```

Open the generated migration and confirm it contains **only** `CreateTable("product_category", ...)` with the composite primary key. There must be **no** data migration — no store runs v4, so there is no `Product.Category` data to reclassify (spec §6). If the migration contains anything else, something drifted; STOP and report.

- [ ] **Step 8: Verify the migration applies to a fresh database**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests`
Expected: the whole suite passes (76 pre-existing + 2 new = 78).

- [ ] **Step 9: Commit**

```bash
git add src/IndyPOS.Domain/Entities src/IndyPOS.Infrastructure/Persistence/StoreHub tests/IndyPOS.StoreHub.IntegrationTests/ProductCategoryPersistenceTests.cs
git commit -m "feat(storehub): add the store-scoped product_category table"
```

---

## Task 4: The repository

**Files:**
- Create: `src/IndyPOS.Application/Abstractions/StoreHub/Repositories/IProductCategoryRepository.cs`
- Create: `src/IndyPOS.Infrastructure/Persistence/StoreHub/Repositories/ProductCategoryRepository.cs`
- Modify: `src/IndyPOS.Infrastructure/ConfigureServices.cs:72` (register beside `IPaymentMethodRepository`)
- Test: `tests/IndyPOS.StoreHub.IntegrationTests/ProductCategoryRepositoryTests.cs`

**Interfaces:**
- Consumes: `ProductCategory` (Task 3), `IStoreIdentityService`.
- Produces: `IProductCategoryRepository` with
  `Task<IReadOnlyList<ProductCategory>> GetAllAsync(CancellationToken)`,
  `Task<ProductCategory?> GetByCodeAsync(string code, CancellationToken)`,
  `Task AddAsync(ProductCategory category, CancellationToken)`

- [ ] **Step 1: Write the failing test**

Create `tests/IndyPOS.StoreHub.IntegrationTests/ProductCategoryRepositoryTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;

namespace IndyPOS.StoreHub.IntegrationTests;

public class ProductCategoryRepositoryTests : IntegrationTestBase
{
    private ProductCategoryRepository CreateRepository(string storeId) =>
        new(DbContext, new MockStoreIdentityService { StoreId = storeId });

    private static ProductCategory Category(string code, int order, ProductCategoryKind kind) => new()
    {
        Code = code,
        DisplayName = code,
        Kind = kind,
        IsEnabled = true,
        DisplayOrder = order,
        CreatedUtc = DateTime.UtcNow,
        LastModifiedUtc = DateTime.UtcNow
    };

    [Fact]
    public async Task GetAllAsync_ShouldReturnOnlyThisStoreOrderedByDisplayOrder()
    {
        var mine = CreateRepository("STORE-MINE");
        await mine.AddAsync(Category("Toys", 2, ProductCategoryKind.GeneralGoods));
        await mine.AddAsync(Category("Gifts", 1, ProductCategoryKind.GeneralGoods));

        var theirs = CreateRepository("STORE-THEIRS");
        await theirs.AddAsync(Category("PlumbingMaterials", 1, ProductCategoryKind.Hardware));

        var result = await mine.GetAllAsync();

        result.Select(c => c.Code).Should().Equal("Gifts", "Toys");
    }

    [Fact]
    public async Task AddAsync_ShouldOverwriteACallerSuppliedStoreId()
    {
        // Never trust the caller's store id - the same guard PaymentMethodRepository applies.
        var repository = CreateRepository("STORE-REAL");
        var category = Category("Toys", 1, ProductCategoryKind.GeneralGoods);
        category.StoreId = "STORE-SOMEONE-ELSE";

        await repository.AddAsync(category);

        (await repository.GetByCodeAsync("Toys")).Should().NotBeNull();
    }

    [Fact]
    public async Task GetByCodeAsync_ForAnotherStoresCode_ShouldReturnNull()
    {
        await CreateRepository("STORE-A").AddAsync(Category("Gifts", 1, ProductCategoryKind.GeneralGoods));

        (await CreateRepository("STORE-B").GetByCodeAsync("Gifts")).Should().BeNull();
    }
}
```

If `MockStoreIdentityService` is not already referenced by this test project, add a project reference to `tests/IndyPOS.Mock` — the payment-method integration tests use the same mock, so follow whatever they do.

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter FullyQualifiedName~ProductCategoryRepositoryTests`
Expected: FAIL to compile — `ProductCategoryRepository` does not exist.

- [ ] **Step 3: Write the interface**

Create `src/IndyPOS.Application/Abstractions/StoreHub/Repositories/IProductCategoryRepository.cs`:

```csharp
using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Application.Abstractions.StoreHub.Repositories;

public interface IProductCategoryRepository
{
    Task<IReadOnlyList<ProductCategory>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProductCategory?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task AddAsync(ProductCategory category, CancellationToken cancellationToken = default);
}
```

There is deliberately no `UpdateAsync`: the spec rules out an admin editing screen (categories are stable, unlike churning government campaigns). Add it when there is a reason.

- [ ] **Step 4: Write the implementation**

Create `src/IndyPOS.Infrastructure/Persistence/StoreHub/Repositories/ProductCategoryRepository.cs`:

```csharp
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;

public class ProductCategoryRepository : IProductCategoryRepository
{
    private readonly StoreHubDbContext _dbContext;
    private readonly IStoreIdentityService _storeIdentity;

    public ProductCategoryRepository(StoreHubDbContext dbContext, IStoreIdentityService storeIdentity)
    {
        _dbContext = dbContext;
        _storeIdentity = storeIdentity;
    }

    public async Task<IReadOnlyList<ProductCategory>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var storeId = _storeIdentity.StoreId;
        return await _dbContext.ProductCategories.AsNoTracking()
            .Where(c => c.StoreId == storeId)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductCategory?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var storeId = _storeIdentity.StoreId;
        return await _dbContext.ProductCategories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.StoreId == storeId && c.Code == code, cancellationToken);
    }

    public async Task AddAsync(ProductCategory category, CancellationToken cancellationToken = default)
    {
        // Force the current store's identity — never trust the caller's StoreId.
        category.StoreId = _storeIdentity.StoreId;

        _dbContext.ProductCategories.Add(category);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 5: Register it**

In `src/IndyPOS.Infrastructure/ConfigureServices.cs`, beside the `IPaymentMethodRepository` registration (line 72), add:

```csharp
		        .AddScoped<IProductCategoryRepository, ProductCategoryRepository>()
```

- [ ] **Step 6: Run the test to verify it passes**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter FullyQualifiedName~ProductCategoryRepositoryTests`
Expected: PASS, 3 tests.

- [ ] **Step 7: Commit**

```bash
git add src/IndyPOS.Application/Abstractions src/IndyPOS.Infrastructure tests/IndyPOS.StoreHub.IntegrationTests/ProductCategoryRepositoryTests.cs
git commit -m "feat(storehub): add the product category repository"
```

---

## Task 5: The category codes and per-store-type seed tables

Data only, so it is worth its own task and its own review: these codes are the contract Epic 2's migration maps into.

**Files:**
- Create: `src/IndyPOS.Application/Common/Constants/ProductCategoryCodes.cs`
- Test: `tests/IndyPOS.Application.Tests/Common/Constants/ProductCategoryCodesTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `static class ProductCategoryCodes` with one `const string` per code (names equal values).

- [ ] **Step 1: Write the failing test**

Create `tests/IndyPOS.Application.Tests/Common/Constants/ProductCategoryCodesTests.cs`:

```csharp
using System.Reflection;
using FluentAssertions;
using IndyPOS.Application.Common.Constants;

namespace IndyPOS.Application.Tests.Common.Constants;

public class ProductCategoryCodesTests
{
    private static IReadOnlyList<FieldInfo> Constants() =>
        typeof(ProductCategoryCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f is { IsLiteral: true, IsInitOnly: false })
            .ToList();

    [Fact]
    public void EveryConstant_ShouldHaveAValueEqualToItsName()
    {
        // The code IS the identifier. A mismatch means a rename silently changed stored data.
        foreach (var field in Constants())
        {
            field.GetValue(null).Should().Be(field.Name);
        }
    }

    [Fact]
    public void Codes_ShouldBeUnique()
    {
        var values = Constants().Select(f => (string)f.GetValue(null)!).ToList();

        values.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Codes_ShouldCoverEveryCategoryTheThreeStoresUse()
    {
        // 16 GeneralHardware + 17 MimyShop, minus the 4 codes shared between them
        // (Toys, Stationery, Household, Miscellaneous). MimyMart's 10 are a subset of
        // GeneralHardware's. See the spec's section 4 tables.
        Constants().Should().HaveCount(29);
    }

    [Fact]
    public void Codes_ShouldIncludeTheServicesCategory()
    {
        ProductCategoryCodes.Services.Should().Be("Services");
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter FullyQualifiedName~ProductCategoryCodesTests`
Expected: FAIL — `ProductCategoryCodes` does not exist.

- [ ] **Step 3: Write the constants**

Create `src/IndyPOS.Application/Common/Constants/ProductCategoryCodes.cs`:

```csharp
namespace IndyPOS.Application.Common.Constants;

/// <summary>
/// Stable product-category codes. Stored on <c>Product.Category</c> and mapped to by the
/// SQLite migration, so a value here is a data contract — rename nothing without a migration.
/// <para>Codes are shared across stores where the meaning genuinely matches (Toys, Stationery,
/// Household, Miscellaneous), which is what makes cross-store reporting a plain GROUP BY.</para>
/// </summary>
public static class ProductCategoryCodes
{
    // --- General goods, present in GeneralHardware and MimyMart ---
    public const string Miscellaneous = "Miscellaneous";              // เบ็ดเตล็ด
    public const string Beverages = "Beverages";                      // เครื่องดื่ม
    public const string Snacks = "Snacks";                            // ขนม
    public const string AlcoholicBeverages = "AlcoholicBeverages";    // เครื่องดื่มแอลกอฮอล์
    public const string Food = "Food";                                // อาหาร
    public const string Stationery = "Stationery";                    // เครื่องเขียน
    public const string Household = "Household";                      // ของใช้ในบ้าน
    public const string ElectricalAppliances = "ElectricalAppliances";// เครื่องใช้ไฟฟ้า
    public const string Toys = "Toys";                                // ของเล่น
    public const string Medicine = "Medicine";                        // ยา

    // --- GeneralHardware only ---
    public const string Agriculture = "Agriculture";                          // การเกษตร
    public const string GeneralMaterials = "GeneralMaterials";                // วัสดุและอุปกรณ์ทั่วไป
    public const string MaterialsAndEquipment = "MaterialsAndEquipment";      // วัสดุและอุปกรณ์
    public const string PlumbingMaterials = "PlumbingMaterials";              // วัสดุและอุปกรณ์ระบบประปา
    public const string ElectricalMaterials = "ElectricalMaterials";          // วัสดุและอุปกรณ์ระบบไฟฟ้า
    public const string ConstructionMaterials = "ConstructionMaterials";      // วัสดุก่อสร้างและอุปกรณ์การช่าง

    // --- MimyShop only ---
    public const string Gifts = "Gifts";                              // ของขวัญ
    public const string BooksAndNotebooks = "BooksAndNotebooks";      // หนังสือและสมุด
    public const string Cosmetics = "Cosmetics";                      // เครื่องสำอาง
    public const string Jewellery = "Jewellery";                      // เครื่องประดับ
    public const string Bags = "Bags";                                // กระเป๋า
    public const string Fashion = "Fashion";                          // แฟชั่น
    public const string Kitchenware = "Kitchenware";                  // เครื่องครัว
    public const string SnacksAndBeverages = "SnacksAndBeverages";    // ขนมและเครื่องดื่ม
    public const string MobileAccessories = "MobileAccessories";      // อุปกรณ์มือถือ
    public const string Electronics = "Electronics";                  // อุปกรณ์อิเล็กทรอนิกส์
    public const string PartySupplies = "PartySupplies";              // อุปกรณ์งานปาร์ตี้
    public const string SeasonalGoods = "SeasonalGoods";              // สินค้าตามเทศกาล
    public const string Services = "Services";                        // บริการ
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter FullyQualifiedName~ProductCategoryCodesTests`
Expected: PASS, 4 tests. If the count assertion fails, recount against the spec's §4 tables rather than editing the number to match.

- [ ] **Step 5: Commit**

```bash
git add src/IndyPOS.Application/Common/Constants/ProductCategoryCodes.cs tests/IndyPOS.Application.Tests/Common/Constants/ProductCategoryCodesTests.cs
git commit -m "feat(application): add stable product category codes"
```

---

## Task 6: The per-store-type seeder

The task that catches an id or name mix-up between MimyMart and MimyShop.

**Files:**
- Create: `src/IndyPOS.Infrastructure/Persistence/StoreHub/Seeders/ProductCategorySeeder.cs`
- Modify: `src/IndyPOS.Infrastructure/ConfigureServices.cs:165` (register beside `PaymentMethodSeeder`)
- Modify: `src/IndyPOS.Infrastructure/Persistence/StoreHub/StoreHubDbContextExtensions.cs` (add `SeedProductCategoriesAsync`)
- Modify: `src/IndyPOS.StoreHub/Program.cs` (call it wherever `SeedPaymentMethodsAsync` is called)
- Test: `tests/IndyPOS.StoreHub.IntegrationTests/ProductCategorySeederTests.cs`

**Interfaces:**
- Consumes: `IProductCategoryRepository` (Task 4), `ProductCategoryCodes` (Task 5), `ProductCategoryKind` (Task 1), `IStoreIdentityService`.
- Produces: `ProductCategorySeeder` with `Task SeedAsync(CancellationToken)`; `IHost.SeedProductCategoriesAsync()`.

- [ ] **Step 1: Write the failing test**

Create `tests/IndyPOS.StoreHub.IntegrationTests/ProductCategorySeederTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Common.Constants;
using IndyPOS.Domain.Enums;
using IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;
using IndyPOS.Infrastructure.Persistence.StoreHub.Seeders;
using Microsoft.Extensions.Logging.Abstractions;

namespace IndyPOS.StoreHub.IntegrationTests;

public class ProductCategorySeederTests : IntegrationTestBase
{
    private async Task<IReadOnlyList<string>> SeedAndReadCodesAsync(string storeId, StoreType storeType)
    {
        var identity = new MockStoreIdentityService { StoreId = storeId, StoreType = storeType };
        var repository = new ProductCategoryRepository(DbContext, identity);

        await new ProductCategorySeeder(repository, identity,
            NullLogger<ProductCategorySeeder>.Instance).SeedAsync();

        DbContext.ChangeTracker.Clear();
        return (await repository.GetAllAsync()).Select(c => c.Code).ToList();
    }

    [Fact]
    public async Task SeedAsync_ForGeneralHardware_ShouldSeedSixteenIncludingFiveHardware()
    {
        var identity = new MockStoreIdentityService
        {
            StoreId = "STORE-GH", StoreType = StoreType.GeneralHardware
        };
        var repository = new ProductCategoryRepository(DbContext, identity);

        await new ProductCategorySeeder(repository, identity,
            NullLogger<ProductCategorySeeder>.Instance).SeedAsync();
        DbContext.ChangeTracker.Clear();

        var seeded = await repository.GetAllAsync();

        seeded.Should().HaveCount(16);
        seeded.Count(c => c.Kind == ProductCategoryKind.Hardware).Should().Be(5);
        seeded.Should().Contain(c => c.Code == ProductCategoryCodes.PlumbingMaterials);
    }

    [Fact]
    public async Task SeedAsync_ForMinimart_ShouldSeedTenAndNoHardware()
    {
        var codes = await SeedAndReadCodesAsync("STORE-MM", StoreType.Minimart);

        codes.Should().HaveCount(10);
        codes.Should().NotContain(ProductCategoryCodes.PlumbingMaterials);
    }

    [Fact]
    public async Task SeedAsync_ForMinimart_ShouldNotSeedTheLeftoverAgricultureCategory()
    {
        // MimyMart's category table was copied from GeneralHardware and การเกษตร came along as a
        // leftover: 0 products and 0 invoice lines in the real database. Seeding it would put a
        // category a minimart never sells in its picker.
        var codes = await SeedAndReadCodesAsync("STORE-MM2", StoreType.Minimart);

        codes.Should().NotContain(ProductCategoryCodes.Agriculture);
    }

    [Fact]
    public async Task SeedAsync_ForMimyShop_ShouldSeedSeventeenIncludingServices()
    {
        // All 17 are seeded even though 12 have no products yet: MimyShop is a new store still
        // adding inventory, so its unused categories are a plan, not detritus.
        var codes = await SeedAndReadCodesAsync("STORE-MS", StoreType.MimyShop);

        codes.Should().HaveCount(17);
        codes.Should().Contain(ProductCategoryCodes.Services);
        codes.Should().Contain(ProductCategoryCodes.Gifts);
    }

    [Fact]
    public async Task SeedAsync_ForMimyShop_ShouldNotSeedMinimartOnlyGroceryCategories()
    {
        // The mix-up this test exists to catch: MimyShop and MimyMart share id ranges with
        // completely different meanings, so a copy-paste between their seed tables is easy.
        var codes = await SeedAndReadCodesAsync("STORE-MS2", StoreType.MimyShop);

        codes.Should().NotContain(ProductCategoryCodes.Beverages);
        codes.Should().NotContain(ProductCategoryCodes.AlcoholicBeverages);
        codes.Should().NotContain(ProductCategoryCodes.Medicine);
    }

    [Fact]
    public async Task SeedAsync_RunTwice_ShouldNotDuplicate()
    {
        var identity = new MockStoreIdentityService
        {
            StoreId = "STORE-TWICE", StoreType = StoreType.MimyShop
        };
        var repository = new ProductCategoryRepository(DbContext, identity);
        var seeder = new ProductCategorySeeder(repository, identity,
            NullLogger<ProductCategorySeeder>.Instance);

        await seeder.SeedAsync();
        DbContext.ChangeTracker.Clear();
        await seeder.SeedAsync();
        DbContext.ChangeTracker.Clear();

        (await repository.GetAllAsync()).Should().HaveCount(17);
    }

    [Fact]
    public async Task SeedAsync_ShouldGiveEachStoreItsOwnRows()
    {
        await SeedAndReadCodesAsync("STORE-A", StoreType.MimyShop);
        var second = await SeedAndReadCodesAsync("STORE-B", StoreType.Minimart);

        second.Should().HaveCount(10, "STORE-B must not see STORE-A's rows");
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter FullyQualifiedName~ProductCategorySeederTests`
Expected: FAIL to compile — `ProductCategorySeeder` does not exist.

- [ ] **Step 3: Write the seeder**

Create `src/IndyPOS.Infrastructure/Persistence/StoreHub/Seeders/ProductCategorySeeder.cs`:

```csharp
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Constants;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Seeders;

/// <summary>
/// Seeds this store's product categories. Idempotent — only inserts a Code that is absent.
/// <para>Unlike <see cref="PaymentMethodSeeder"/>, the set depends on the STORE TYPE: the three
/// stores carry genuinely different catalogues, and legacy category ids collide across them
/// (id 10 is เบ็ดเตล็ด in GeneralHardware and ของขวัญ in MimyShop), so there is no shared default.</para>
/// </summary>
public class ProductCategorySeeder
{
    private readonly IProductCategoryRepository _repository;
    private readonly IStoreIdentityService _storeIdentity;
    private readonly ILogger<ProductCategorySeeder> _logger;

    public ProductCategorySeeder(IProductCategoryRepository repository, IStoreIdentityService storeIdentity,
        ILogger<ProductCategorySeeder> logger)
    {
        _repository = repository;
        _storeIdentity = storeIdentity;
        _logger = logger;
    }

    private sealed record Seed(string Code, string DisplayName, ProductCategoryKind Kind, int Order);

    // Shared by GeneralHardware and Minimart, in legacy id order 10-19.
    private static readonly Seed[] GroceryCommon =
    [
        new(ProductCategoryCodes.Miscellaneous, "เบ็ดเตล็ด", ProductCategoryKind.GeneralGoods, 1),
        new(ProductCategoryCodes.Beverages, "เครื่องดื่ม", ProductCategoryKind.GeneralGoods, 2),
        new(ProductCategoryCodes.Snacks, "ขนม", ProductCategoryKind.GeneralGoods, 3),
        new(ProductCategoryCodes.AlcoholicBeverages, "เครื่องดื่มแอลกอฮอล์", ProductCategoryKind.GeneralGoods, 4),
        new(ProductCategoryCodes.Food, "อาหาร", ProductCategoryKind.GeneralGoods, 5),
        new(ProductCategoryCodes.Stationery, "เครื่องเขียน", ProductCategoryKind.GeneralGoods, 6),
        new(ProductCategoryCodes.Household, "ของใช้ในบ้าน", ProductCategoryKind.GeneralGoods, 7),
        new(ProductCategoryCodes.ElectricalAppliances, "เครื่องใช้ไฟฟ้า", ProductCategoryKind.GeneralGoods, 8),
        new(ProductCategoryCodes.Toys, "ของเล่น", ProductCategoryKind.GeneralGoods, 9),
        new(ProductCategoryCodes.Medicine, "ยา", ProductCategoryKind.GeneralGoods, 10)
    ];

    // GeneralHardware = the grocery set, plus การเกษตร (legacy 20) and the five วัสดุ* ranges (50-54).
    private static readonly Seed[] GeneralHardwareSeeds =
    [
        .. GroceryCommon,
        new(ProductCategoryCodes.Agriculture, "การเกษตร", ProductCategoryKind.GeneralGoods, 11),
        new(ProductCategoryCodes.GeneralMaterials, "วัสดุและอุปกรณ์ทั่วไป", ProductCategoryKind.Hardware, 12),
        new(ProductCategoryCodes.MaterialsAndEquipment, "วัสดุและอุปกรณ์", ProductCategoryKind.Hardware, 13),
        new(ProductCategoryCodes.PlumbingMaterials, "วัสดุและอุปกรณ์ระบบประปา", ProductCategoryKind.Hardware, 14),
        new(ProductCategoryCodes.ElectricalMaterials, "วัสดุและอุปกรณ์ระบบไฟฟ้า", ProductCategoryKind.Hardware, 15),
        new(ProductCategoryCodes.ConstructionMaterials, "วัสดุก่อสร้างและอุปกรณ์การช่าง", ProductCategoryKind.Hardware, 16)
    ];

    // Minimart = the grocery set only. การเกษตร is deliberately absent: it was copied from
    // GeneralHardware and has 0 products and 0 invoice lines in the real MimyMart database.
    private static readonly Seed[] MinimartSeeds = [.. GroceryCommon];

    // MimyShop reuses legacy ids 10-26 with entirely different meanings. All 17 are seeded even
    // though 12 currently have no products: it is a new store still adding inventory.
    private static readonly Seed[] MimyShopSeeds =
    [
        new(ProductCategoryCodes.Gifts, "ของขวัญ", ProductCategoryKind.GeneralGoods, 1),
        new(ProductCategoryCodes.Toys, "ของเล่น", ProductCategoryKind.GeneralGoods, 2),
        new(ProductCategoryCodes.Stationery, "เครื่องเขียน", ProductCategoryKind.GeneralGoods, 3),
        new(ProductCategoryCodes.BooksAndNotebooks, "หนังสือและสมุด", ProductCategoryKind.GeneralGoods, 4),
        new(ProductCategoryCodes.Cosmetics, "เครื่องสำอาง", ProductCategoryKind.GeneralGoods, 5),
        new(ProductCategoryCodes.Jewellery, "เครื่องประดับ", ProductCategoryKind.GeneralGoods, 6),
        new(ProductCategoryCodes.Bags, "กระเป๋า", ProductCategoryKind.GeneralGoods, 7),
        new(ProductCategoryCodes.Fashion, "แฟชั่น", ProductCategoryKind.GeneralGoods, 8),
        new(ProductCategoryCodes.Household, "ของใช้ในบ้าน", ProductCategoryKind.GeneralGoods, 9),
        new(ProductCategoryCodes.Kitchenware, "เครื่องครัว", ProductCategoryKind.GeneralGoods, 10),
        new(ProductCategoryCodes.SnacksAndBeverages, "ขนมและเครื่องดื่ม", ProductCategoryKind.GeneralGoods, 11),
        new(ProductCategoryCodes.MobileAccessories, "อุปกรณ์มือถือ", ProductCategoryKind.GeneralGoods, 12),
        new(ProductCategoryCodes.Electronics, "อุปกรณ์อิเล็กทรอนิกส์", ProductCategoryKind.GeneralGoods, 13),
        new(ProductCategoryCodes.PartySupplies, "อุปกรณ์งานปาร์ตี้", ProductCategoryKind.GeneralGoods, 14),
        new(ProductCategoryCodes.SeasonalGoods, "สินค้าตามเทศกาล", ProductCategoryKind.GeneralGoods, 15),
        new(ProductCategoryCodes.Services, "บริการ", ProductCategoryKind.Service, 16),
        new(ProductCategoryCodes.Miscellaneous, "เบ็ดเตล็ด", ProductCategoryKind.GeneralGoods, 17)
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var storeType = _storeIdentity.StoreType;
        var seeds = SeedsFor(storeType);
        var now = DateTime.UtcNow;
        var seededCount = 0;

        foreach (var seed in seeds)
        {
            if (await _repository.GetByCodeAsync(seed.Code, cancellationToken) is not null) continue;

            await _repository.AddAsync(new ProductCategory
            {
                Code = seed.Code,
                DisplayName = seed.DisplayName,
                Kind = seed.Kind,
                IsEnabled = true,
                DisplayOrder = seed.Order,
                StoreId = _storeIdentity.StoreId,
                CreatedUtc = now,
                LastModifiedUtc = now
            }, cancellationToken);
            seededCount++;
        }

        _logger.LogInformation(
            "Product category seeding complete for {StoreType}: {SeededCount} of {TotalCount} inserted",
            storeType, seededCount, seeds.Length);
    }

    private static Seed[] SeedsFor(StoreType storeType) => storeType switch
    {
        StoreType.GeneralHardware => GeneralHardwareSeeds,
        StoreType.Minimart => MinimartSeeds,
        StoreType.MimyShop => MimyShopSeeds,
        // A new store type with no seed table must be a loud failure, not an empty catalogue:
        // an empty catalogue means no product can be created at all.
        _ => throw new ArgumentOutOfRangeException(nameof(storeType), storeType,
            "No product-category seed set is defined for this store type.")
    };
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter FullyQualifiedName~ProductCategorySeederTests`
Expected: PASS, 7 tests.

- [ ] **Step 5: Register and wire the seeder**

In `src/IndyPOS.Infrastructure/ConfigureServices.cs`, beside `services.AddScoped<PaymentMethodSeeder>();` (line 165):

```csharp
		services.AddScoped<ProductCategorySeeder>();
```

In `src/IndyPOS.Infrastructure/Persistence/StoreHub/StoreHubDbContextExtensions.cs`, after `SeedPaymentMethodsAsync`:

```csharp
    /// <summary>
    /// Seeds this store's product categories, chosen by store type.
    /// Idempotent — safe to run on every start.
    /// </summary>
    public static async Task SeedProductCategoriesAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<ProductCategorySeeder>();
        await seeder.SeedAsync();
    }
```

In `src/IndyPOS.StoreHub/Program.cs`, find every call to `SeedPaymentMethodsAsync()` and add immediately after each:

```csharp
await app.SeedProductCategoriesAsync();
```

Both the `migrate` CLI path and normal startup must seed, exactly as payment methods do.

- [ ] **Step 6: Verify the wiring compiles and nothing regressed**

Run:
```bash
dotnet build IndyPOS.sln -c Debug
dotnet test tests/IndyPOS.StoreHub.IntegrationTests
```
Expected: builds clean; suite passes (78 from Task 3 + 3 from Task 4 + 7 = 88).

- [ ] **Step 7: Commit**

```bash
git add src/IndyPOS.Infrastructure src/IndyPOS.StoreHub/Program.cs tests/IndyPOS.StoreHub.IntegrationTests/ProductCategorySeederTests.cs
git commit -m "feat(storehub): seed product categories per store type"
```

---

## Task 7: GET /product-categories

**Files:**
- Create: `src/IndyPOS.Application/UseCases/StoreHub/ProductCategories/ProductCategoryDto.cs`
- Create: `src/IndyPOS.Application/UseCases/StoreHub/ProductCategories/GetProductCategoriesQuery.cs`
- Create: `src/IndyPOS.Application/UseCases/StoreHub/ProductCategories/GetProductCategoriesQueryHandler.cs`
- Modify: `src/IndyPOS.StoreHub/Program.cs` (register the handler near line 103; map the endpoint near line 332)
- Test: `tests/IndyPOS.Application.Tests/UseCases/StoreHub/ProductCategories/GetProductCategoriesQueryHandlerTests.cs`

**Interfaces:**
- Consumes: `IProductCategoryRepository` (Task 4), `ProductCategoryKind` (Task 1).
- Produces:
  - `record ProductCategoryDto(string Code, string DisplayName, ProductCategoryKind Kind, bool IsEnabled, int DisplayOrder)`
  - `record GetProductCategoriesQuery()`
  - `GetProductCategoriesQueryHandler : IQueryHandler<GetProductCategoriesQuery, IReadOnlyList<ProductCategoryDto>>`

- [ ] **Step 1: Write the failing test**

Create `tests/IndyPOS.Application.Tests/UseCases/StoreHub/ProductCategories/GetProductCategoriesQueryHandlerTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.UseCases.StoreHub.ProductCategories;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using Moq;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.ProductCategories;

public class GetProductCategoriesQueryHandlerTests
{
    private static ProductCategory Row(string code, int order, bool enabled = true) => new()
    {
        StoreId = "STORE-A",
        Code = code,
        DisplayName = code,
        Kind = ProductCategoryKind.GeneralGoods,
        IsEnabled = enabled,
        DisplayOrder = order,
        CreatedUtc = DateTime.UtcNow,
        LastModifiedUtc = DateTime.UtcNow
    };

    [Fact]
    public async Task HandleAsync_ShouldReturnDisabledCategoriesToo()
    {
        // The POS filters for pickers; existing products referencing a disabled category must
        // still render their label, so the endpoint returns everything with its IsEnabled flag.
        var repository = new Mock<IProductCategoryRepository>();
        repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync([Row("Gifts", 1), Row("Retired", 2, enabled: false)]);

        var result = await new GetProductCategoriesQueryHandler(repository.Object)
            .HandleAsync(new GetProductCategoriesQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Single(c => c.Code == "Retired").IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldPreserveTheRepositoryOrdering()
    {
        var repository = new Mock<IProductCategoryRepository>();
        repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync([Row("First", 1), Row("Second", 2)]);

        var result = await new GetProductCategoriesQueryHandler(repository.Object)
            .HandleAsync(new GetProductCategoriesQuery(), CancellationToken.None);

        result.Select(c => c.Code).Should().Equal("First", "Second");
    }
}
```

If this test project does not already use Moq, mirror whatever mocking approach the existing payment-method handler tests use instead.

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter FullyQualifiedName~GetProductCategoriesQueryHandlerTests`
Expected: FAIL to compile — the query, DTO and handler do not exist.

- [ ] **Step 3: Write the DTO, query and handler**

Create `src/IndyPOS.Application/UseCases/StoreHub/ProductCategories/ProductCategoryDto.cs`:

```csharp
using IndyPOS.Domain.Enums;

namespace IndyPOS.Application.UseCases.StoreHub.ProductCategories;

public record ProductCategoryDto(
    string Code, string DisplayName, ProductCategoryKind Kind, bool IsEnabled, int DisplayOrder);
```

Create `src/IndyPOS.Application/UseCases/StoreHub/ProductCategories/GetProductCategoriesQuery.cs`:

```csharp
namespace IndyPOS.Application.UseCases.StoreHub.ProductCategories;

/// <summary>Every category for this store, enabled or not, in display order.</summary>
public record GetProductCategoriesQuery();
```

Create `src/IndyPOS.Application/UseCases/StoreHub/ProductCategories/GetProductCategoriesQueryHandler.cs`:

```csharp
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;

namespace IndyPOS.Application.UseCases.StoreHub.ProductCategories;

public class GetProductCategoriesQueryHandler
    : IQueryHandler<GetProductCategoriesQuery, IReadOnlyList<ProductCategoryDto>>
{
    private readonly IProductCategoryRepository _repository;

    public GetProductCategoriesQueryHandler(IProductCategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ProductCategoryDto>> HandleAsync(
        GetProductCategoriesQuery query, CancellationToken cancellationToken = default)
    {
        var categories = await _repository.GetAllAsync(cancellationToken);

        return categories
            .Select(c => new ProductCategoryDto(c.Code, c.DisplayName, c.Kind, c.IsEnabled, c.DisplayOrder))
            .ToList();
    }
}
```

Match `IQueryHandler`'s exact signature as used by `GetOfferablePaymentMethodsQueryHandler` — if the interface names the method differently or omits the default token, follow that file.

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter FullyQualifiedName~GetProductCategoriesQueryHandlerTests`
Expected: PASS, 2 tests.

- [ ] **Step 5: Register the handler and map the endpoint**

In `src/IndyPOS.StoreHub/Program.cs`, after the payment-method handler registrations (line 107):

```csharp
builder.Services.AddTransient<IQueryHandler<GetProductCategoriesQuery, IReadOnlyList<ProductCategoryDto>>, GetProductCategoriesQueryHandler>();
```

After the `/payment-methods` endpoint (line ~339):

```csharp
// Product categories for this store (the POS renders pickers from this)
app.MapGet("/product-categories", async (
    IQueryHandler<GetProductCategoriesQuery, IReadOnlyList<ProductCategoryDto>> handler,
    CancellationToken cancellationToken) =>
{
    var categories = await handler.HandleAsync(new GetProductCategoriesQuery(), cancellationToken);
    return Results.Ok(categories);
}).RequireAuthorization("CanReadProducts");
```

Add `using IndyPOS.Application.UseCases.StoreHub.ProductCategories;` to `Program.cs` if the usings are explicit rather than global.

- [ ] **Step 6: Verify the endpoint end to end**

Run: `dotnet build IndyPOS.sln -c Debug && dotnet test tests/IndyPOS.StoreHub.IntegrationTests`
Expected: builds clean, suite still passes.

- [ ] **Step 7: Commit**

```bash
git add src/IndyPOS.Application/UseCases/StoreHub/ProductCategories src/IndyPOS.StoreHub/Program.cs tests/IndyPOS.Application.Tests/UseCases/StoreHub/ProductCategories
git commit -m "feat(storehub): expose GET /product-categories"
```

---

## Task 8: Gate product create/update on the category's Kind

Replaces the string comparison in both handlers. This is the behavioural heart of the epic.

**Files:**
- Create: `src/IndyPOS.Application/Common/Exceptions/UnknownProductCategoryException.cs`
- Modify: `src/IndyPOS.Application/UseCases/StoreHub/Products/Create/CreateProductCommandHandler.cs:45-52`
- Modify: `src/IndyPOS.Application/UseCases/StoreHub/Products/Update/UpdateProductCommandHandler.cs:52-59`
- Modify: `src/IndyPOS.StoreHub/Program.cs:405-419` (POST /products) and the PUT arm at ~422-445
- Test: `tests/IndyPOS.Application.Tests/UseCases/StoreHub/Products/CreateProductCategoryGateTests.cs`

**Interfaces:**
- Consumes: `IProductCategoryRepository` (Task 4), `ProductCategoryPolicy` (Task 1).
- Produces: `UnknownProductCategoryException`; both handlers gain an `IProductCategoryRepository` constructor parameter.

- [ ] **Step 1: Write the failing test**

Create `tests/IndyPOS.Application.Tests/UseCases/StoreHub/Products/CreateProductCategoryGateTests.cs`. Copy the arrangement style (mocked `IProductRepository`, `MockStoreIdentityService`) from the existing product-type-restriction tests in this project so the constructor arguments match:

```csharp
using FluentAssertions;
using IndyPOS.Application.Common.Constants;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.Products;

public class CreateProductCategoryGateTests
{
    private static ProductCategory Category(string code, ProductCategoryKind kind) => new()
    {
        StoreId = "STORE-A", Code = code, DisplayName = code, Kind = kind,
        IsEnabled = true, DisplayOrder = 1,
        CreatedUtc = DateTime.UtcNow, LastModifiedUtc = DateTime.UtcNow
    };

    [Fact]
    public async Task HandleAsync_WithAnUnknownCategoryCode_ShouldThrowUnknownProductCategory()
    {
        // A code the catalogue does not define is a malformed request (400), not a conflict.
        // Accepting it would put a product in a category nothing can resolve - the failure mode
        // that made 'Other' payments unreportable.
        var handler = BuildHandler(StoreType.GeneralHardware, category: null);

        var act = async () => await handler.HandleAsync(
            CommandWithCategory("NoSuchCategory"), CancellationToken.None);

        await act.Should().ThrowAsync<UnknownProductCategoryException>();
    }

    [Fact]
    public async Task HandleAsync_WithAHardwareCategoryOnAGeneralOnlyStore_ShouldThrowInvalidOperation()
    {
        // Existing product-type restriction behaviour, now driven by Kind rather than by
        // comparing the category string to an enum name. Maps to 409.
        var handler = BuildHandler(StoreType.Minimart,
            Category(ProductCategoryCodes.PlumbingMaterials, ProductCategoryKind.Hardware));

        var act = async () => await handler.HandleAsync(
            CommandWithCategory(ProductCategoryCodes.PlumbingMaterials), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*not available*");
    }

    [Fact]
    public async Task HandleAsync_WithAHardwareCategoryOnAMultiTypeStore_ShouldSucceed()
    {
        var handler = BuildHandler(StoreType.GeneralHardware,
            Category(ProductCategoryCodes.PlumbingMaterials, ProductCategoryKind.Hardware));

        var act = async () => await handler.HandleAsync(
            CommandWithCategory(ProductCategoryCodes.PlumbingMaterials), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task HandleAsync_WithAGeneralGoodsCategoryOnAGeneralOnlyStore_ShouldSucceed()
    {
        var handler = BuildHandler(StoreType.Minimart,
            Category(ProductCategoryCodes.Beverages, ProductCategoryKind.GeneralGoods));

        var act = async () => await handler.HandleAsync(
            CommandWithCategory(ProductCategoryCodes.Beverages), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
```

Write the `BuildHandler(StoreType, ProductCategory?)` and `CommandWithCategory(string)` helpers to match `CreateProductCommandHandler`'s real constructor and `CreateProductCommand`'s real shape — read both files first. `BuildHandler` must stub `IProductCategoryRepository.GetByCodeAsync` to return the supplied category (or null) and `IProductRepository.ExistsByBarcodeAsync` to return false.

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter FullyQualifiedName~CreateProductCategoryGateTests`
Expected: FAIL — `UnknownProductCategoryException` does not exist and the handler takes no category repository.

- [ ] **Step 3: Add the exception**

Create `src/IndyPOS.Application/Common/Exceptions/UnknownProductCategoryException.cs`:

```csharp
namespace IndyPOS.Application.Common.Exceptions;

/// <summary>
/// The requested category code is not in this store's catalogue. A malformed request (400), not a
/// conflict: accepting it would file a product under a category nothing can resolve.
/// </summary>
public class UnknownProductCategoryException : Exception
{
    public UnknownProductCategoryException(string message) : base(message) { }
}
```

- [ ] **Step 4: Replace the gate in CreateProductCommandHandler**

Add `IProductCategoryRepository _categoryRepository` as a constructor-injected field, then replace lines 44-52 (the `isHardware` block) with:

```csharp
        // Store-type gating driven by the category's Kind. The category must exist: an unknown
        // code would file the product under something no report or picker can resolve.
        var category = await _categoryRepository.GetByCodeAsync(command.Category, cancellationToken)
            ?? throw new UnknownProductCategoryException(
                $"Product category '{command.Category}' is not in this store's catalogue.");

        if (!ProductCategoryPolicy.IsUsable(category.Kind, _storeIdentityService.Features))
        {
            _logger.LogWarning(
                "Product creation rejected: Kind={Kind}, StoreType={StoreType}, Barcode={Barcode}",
                category.Kind, _storeIdentityService.StoreType, command.Barcode);
            throw new InvalidOperationException(
                $"{category.DisplayName} products are not available for {_storeIdentityService.StoreType} stores.");
        }
```

Add the usings for `IndyPOS.Application.Common.Exceptions` and `IndyPOS.Domain.ValueObjects`, and remove the now-unused `IndyPOS.Application.Common.Enums` using if nothing else in the file needs it.

- [ ] **Step 5: Apply the identical change to UpdateProductCommandHandler**

Make the same substitution at `UpdateProductCommandHandler.cs:52`. The message and logging use `command.Barcode` if present on the update command; if not, use the product id. Do not paraphrase — keep the two handlers' logic identical so they cannot drift.

- [ ] **Step 6: Map the exception to 400**

In `src/IndyPOS.StoreHub/Program.cs`, in **both** the POST `/products` and PUT `/products/{id:guid}` handlers, add a catch arm **before** the existing `InvalidOperationException` arm (order matters — a more specific exception must be caught first):

```csharp
    catch (UnknownProductCategoryException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
```

- [ ] **Step 7: Run the tests to verify they pass**

Run:
```bash
dotnet test tests/IndyPOS.Application.Tests
dotnet test tests/IndyPOS.StoreHub.IntegrationTests
```
Expected: both pass. Pre-existing product-type-restriction tests may need their arrangement updated to stub the new repository — update them rather than deleting them; they assert real behaviour.

- [ ] **Step 8: Commit**

```bash
git add src/IndyPOS.Application src/IndyPOS.StoreHub/Program.cs tests/IndyPOS.Application.Tests
git commit -m "feat(storehub): gate products on the category Kind instead of a string comparison"
```

---

## Task 9: The client method and the WinForms pickers

**Files:**
- Modify: `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubHttpClient.cs` (after the payment-method catalog block, ~line 305)
- Modify: the `IStoreHubClient` interface the client implements (find it via the existing `GetOfferablePaymentMethodsAsync` declaration)
- Modify: `src/IndyPOS.Windows.Forms/UI/Inventory/AddNewInventoryProductForm.cs:118-134`
- Modify: `src/IndyPOS.Windows.Forms/UI/Inventory/AddNewInventoryProductWithCustomBarcodeForm.cs:106-120`
- Modify: `src/IndyPOS.Windows.Forms/UI/Inventory/UpdateInventoryProductForm.cs:94-108`

**Interfaces:**
- Consumes: `ProductCategoryDto` (Task 7).
- Produces: `Task<IReadOnlyList<ProductCategoryDto>> GetProductCategoriesAsync(CancellationToken)` on the StoreHub client.

- [ ] **Step 1: Add the client method**

In `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubHttpClient.cs`, after the payment-method catalog methods:

```csharp
    // ========================
    // Product category catalog
    // ========================

    public Task<IReadOnlyList<ProductCategoryDto>> GetProductCategoriesAsync(
        CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync<IReadOnlyList<ProductCategoryDto>>(
            HttpMethod.Get, "/product-categories", content: null, cancellationToken);
```

Declare it on the same interface that declares `GetOfferablePaymentMethodsAsync`.

- [ ] **Step 2: Replace the combo population in all three forms**

In each of the three forms, replace the body of `PopulateProductCategoryComboBoxAsync` with:

```csharp
	private async Task PopulateProductCategoryComboBoxAsync()
	{
		CategoryComboBox.Items.Clear();

		IReadOnlyList<ProductCategoryDto> categories;
		bool multipleTypes = true;
		try
		{
			categories = await _storeHubClient.GetProductCategoriesAsync();
			multipleTypes = (await _storeHubClient.GetStoreFeaturesAsync()).MultipleProductTypesEnabled;
		}
		catch
		{
			// The server still guards creation, so a fetch failure must not block data entry.
			// Leaving the list empty would do exactly that, so surface nothing and let the
			// operator retry rather than silently offering a wrong set.
			return;
		}

		foreach (var category in categories.Where(c => c.IsEnabled))
		{
			// Hide kinds this store may not use; the server rejects them anyway.
			if (!multipleTypes && category.Kind != ProductCategoryKind.GeneralGoods)
				continue;

			CategoryComboBox.Items.Add(category.DisplayName);
		}
	}
```

Each form stores the selected **DisplayName** in the combo today. The value sent to the server must be the **Code**, so add a field mapping display name back to code, populated in the same loop:

```csharp
	private readonly Dictionary<string, string> _categoryCodeByDisplayName = new();
```

Add `_categoryCodeByDisplayName[category.DisplayName] = category.Code;` inside the loop, clear it alongside `CategoryComboBox.Items.Clear()`, and where the form currently derives a category value for the save call, look the code up from the selected text. **Read each form's save path before editing** — they differ, and sending a DisplayName where a Code is expected would make every product creation fail with `UnknownProductCategoryException`.

- [ ] **Step 3: Remove the hardcoded dictionary and enum**

- Delete `src/IndyPOS.Application/Common/Enums/ProductCategories.cs`.
- In `src/IndyPOS.Infrastructure/Constants/HardcodedStoreConstants.cs`, delete the `ProductCategories` dictionary, its initialisation, and the `IReadOnlyDictionary<int, string> ProductCategories` property — plus the member on whatever interface declares it.
- In `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubInventoryProductService.cs`, remove `MapCategoryName` (~line 250) and the two `(int)ProductCategory.GeneralGoods` fallbacks (~lines 256, 262). Read the surrounding methods and replace the category handling with the string `Code` carried straight through; this service predates the catalogue and should no longer translate ids.
- In `src/IndyPOS.Infrastructure/QueryHandlers/Reports/GetLegacySalesSummaryQueryHandler.cs:135`, replace the `nameof(ProductCategory.Hardware)` comparison. This handler reports on **historical** rows, so it must classify by looking the category code up in the catalogue and testing `Kind == ProductCategoryKind.Hardware`. Inject `IProductCategoryRepository`, fetch once with `GetAllAsync`, and build a `HashSet<string>` of hardware codes rather than querying per row.

- [ ] **Step 4: Build and run everything**

Run:
```bash
dotnet build IndyPOS.sln -c Release
dotnet test tests/IndyPOS.Domain.Tests
dotnet test tests/IndyPOS.Application.Tests
dotnet test tests/IndyPOS.StoreHub.IntegrationTests
dotnet test tests/IndyPOS.Windows.Forms.Tests
```
Expected: Release build 0 errors; all suites pass. Compile errors are the main signal here — every remaining reference to the deleted enum surfaces now.

- [ ] **Step 5: Commit**

```bash
git add src/IndyPOS.Infrastructure src/IndyPOS.Windows.Forms src/IndyPOS.Application
git commit -m "feat(pos): render category pickers from the catalogue and drop the hardcoded enum"
```

---

## Task 10: Installer store-type support and documentation

**Files:**
- Modify: `installer/IndyPOS.Bootstrapper/UI/InstallationWizard.cs` (the store-type picker)
- Modify: `docs/operations/store-installation-guide.md`
- Modify: `docs/operations/upgrade-procedure.md` (the `--store-type` recovery section lists valid values)
- Modify: `CLAUDE.md` if it enumerates store types
- Test: `tests/IndyPOS.Bootstrapper.Tests/Silent/SilentArgsTests.cs` (already updated in Task 2)

**Interfaces:**
- Consumes: `StoreType.MimyShop` (Task 2).
- Produces: no new code interfaces.

- [ ] **Step 1: Verify the parser already accepts MimyShop**

`SilentArgs.TryParseStoreTypeName` matches against `Enum.GetNames<StoreType>()`, so `--store-type MimyShop` works the moment the enum value exists. Confirm:

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests --filter FullyQualifiedName~SilentArgsTests`
Expected: PASS, including the `[InlineData("MimyShop", StoreType.MimyShop)]` case added in Task 2.

- [ ] **Step 2: Add MimyShop to the wizard picker**

Find the store-type combo in `installer/IndyPOS.Bootstrapper/UI/InstallationWizard.cs` (search for `_storeTypeComboBox`). If it is populated from `Enum.GetNames<StoreType>()`, nothing is needed — verify and note it. If the values are hardcoded, add `MimyShop` and remove `CoffeeShop`.

- [ ] **Step 3: Update the documentation**

Replace every enumeration of supported store types with **GeneralHardware, Minimart, MimyShop**, and remove CoffeeShop:

```bash
grep -rn "CoffeeShop" docs/ CLAUDE.md .claude/STATUS.md
```

In `docs/operations/upgrade-procedure.md`, the `Store:Type is missing` recovery section names valid values — update it to `GeneralHardware`, `Minimart`, or `MimyShop`.

- [ ] **Step 4: Verify**

Run: `dotnet build IndyPOS.sln -c Release && dotnet test tests/IndyPOS.Bootstrapper.Tests`
Expected: 0 errors; suite passes.

- [ ] **Step 5: Commit**

```bash
git add installer docs CLAUDE.md .claude/STATUS.md
git commit -m "feat(installer): support the MimyShop store type and drop CoffeeShop from docs"
```

---

## Task 11: Full verification sweep

Not a code task — the gate before this branch is offered for review.

- [ ] **Step 1: Build and test everything**

```bash
dotnet build IndyPOS.sln -c Release
dotnet test tests/IndyPOS.Domain.Tests
dotnet test tests/IndyPOS.Application.Tests
dotnet test tests/IndyPOS.StoreHub.IntegrationTests
dotnet test tests/IndyPOS.Bootstrapper.Tests
dotnet test tests/IndyPOS.Windows.Forms.Tests
dotnet test tests/IndyPOS.Vault.Tests
```

Expected: Release 0 errors, and these counts:

| Suite | Before | New | After |
|---|---|---|---|
| Domain | 8 | 6 (4 policy + 2 store type) | 14 |
| Application | 274 | 10 (4 codes + 2 query + 4 gate) | 284 |
| StoreHub integration | 76 | 12 (2 persistence + 3 repository + 7 seeder) | 88 |
| Bootstrapper | 223 / 8 skip | 0 | 223 / 8 skip |

No suite regresses. Pre-existing product-type-restriction tests may need their arrangement
updated for the new constructor parameter — that is expected, not a regression.

- [ ] **Step 2: Confirm the enum and dictionary are really gone**

```bash
grep -rn "ProductCategory\.GeneralGoods\|ProductCategory\.Hardware\|ProductCategories\[" --include=*.cs src/ tests/ | grep -v Designer
```

Expected: no hits. Any remaining hit means a call site still classifies by the old enum.

- [ ] **Step 3: Confirm CoffeeShop is gone from code**

```bash
grep -rn "CoffeeShop" --include=*.cs --include=*.json --include=*.ps1 src/ tests/ installer/ scripts/
```

Expected: no hits. Historical mentions in `docs/superpowers/plans/` and `.superpowers/sdd/progress.md` are records of past decisions — leave them.

- [ ] **Step 4: Verify a fresh install seeds the right catalogue for each store type**

For each of `GeneralHardware`, `Minimart`, `MimyShop`, run StoreHub's `migrate` against a scratch database with `Store:Type` set accordingly, then:

```sql
SELECT code, kind, is_enabled, display_order FROM product_category ORDER BY display_order;
```

Expected row counts: 16, 10, 17. Confirm no `Hardware` kind appears for Minimart or MimyShop, and that MimyShop has exactly one `Service`.

- [ ] **Step 5: Hand off to Epic 2**

The migration hardening epic maps legacy category ids to these codes per store. Confirm `ProductCategoryCodes` covers every id in the three real databases:

```bash
python -c "
import sqlite3, os
base = r'.planning/indypos-overhaul/sqlite_database'
for s in ['GeneralHardware','MimyMart','MimyShop']:
    c = sqlite3.connect(os.path.join(base, s, 'Store.db'))
    print(s, sorted(r[0] for r in c.execute('SELECT Id FROM ProductCategory')))
"
```

Expected: GeneralHardware `[10..20, 50..54]`, MimyMart `[10..20]`, MimyShop `[10..26]`. Every id except MimyMart's 20 must have a code in Task 5's table. Record any mismatch in the Epic 2 spec rather than fixing it here.

---

## Self-Review

**Spec coverage:**

| Spec section | Task |
|---|---|
| §3 data model (table, columns, `Kind`) | 1, 3 |
| §3 remove enum + `HardcodedStoreConstants.ProductCategories` | 9 |
| §3 legacy entity rename | 3 — **changed to deletion**: grep proved zero references, so deleting is simpler than renaming. Step 1 stops and reverts to a rename if that proves wrong |
| §4 seed data, all three store types | 5, 6 |
| §4 MimyMart drops `Agriculture`; MimyShop keeps empties | 6 (two dedicated tests) |
| §4 no orphaned category ids | 11 step 5 |
| §5 `MimyShop = 4`, `CoffeeShop` removed, 3 reserved | 2 |
| §5 installer `--store-type` + wizard | 10 |
| §6 seeder per store type, in `migrate` | 6 |
| §6 `GET /product-categories` | 7 |
| §6 no data migration needed | 3 step 7 asserts the migration is create-only |
| §7 unknown code → 400 | 8 |
| §7 wrong kind → 409 | 8 |
| §7 disabled category hidden for new, rendered for existing | 7 (endpoint returns disabled), 9 (picker filters) |
| §8 `IsTrackable` gap | Documented in Task 1's enum comment; deliberately not implemented |
| §9 testing per layer | 1, 4, 6, 7, 8 |

**Deviations from the spec, both deliberate:** composite key instead of `Guid Id` (stated in Global Constraints, matches `PaymentMethod`); legacy entity deleted rather than renamed (evidence-gated in Task 3 step 1).

**Placeholder scan:** no TBD/TODO. Three places tell the implementer to read a file before editing rather than showing final code — the three WinForms save paths (Task 9 step 2), `GetLegacySalesSummaryQueryHandler` (Task 9 step 3), and the `BuildHandler` helper (Task 8 step 1). These are genuinely file-specific and the surrounding code was not read during planning; each states exactly what to look for and what breaks if it is got wrong.

**Type consistency:** `ProductCategoryKind` (1) is used identically in 3, 5, 6, 7, 8, 9. `IProductCategoryRepository`'s three methods (4) are consumed as declared in 6, 7, 8, 9. `ProductCategoryDto`'s five fields (7) match the client and picker usage (9). `ProductCategoryPolicy.IsUsable(kind, features)` (1) is called with that argument order in 8. `ProductCategoryCodes` members (5) are referenced in 6 and 8 by the same names.
