# Data-Driven Payment Methods + Real Store-Type Gating — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the hardcoded `PaymentType` enum with a data-driven `payment_method` catalog (so new government campaigns need no redeploy), gate offerable methods by store type (PayLater = GeneralHardware only), and make `StoreType` actually persisted per store.

**Architecture:** A pure Domain rule (`PaymentMethodPolicy`) decides which catalog rows are offerable for a `StoreType`. An Application catalog service composes the repository + policy and exposes admin operations. StoreHub serves an offerable-methods query + capability-gated admin endpoints. WinForms renders payment buttons from whatever the offerable endpoint returns (store-type filtering stays server-side). The installer writes `Store:Type` so gating is real.

**Tech Stack:** C# .NET 10 (`net10.0-windows`), EF Core + PostgreSQL (Npgsql), Nokpirab mediator (`ICommandHandler<TCmd,TResp>` / `IQueryHandler<TQuery,TResp>`), ASP.NET Core minimal APIs with capability-based authorization, WinForms (constructor DI), xUnit + FluentAssertions (+ AutoFixture/AutoMoq in Application.Tests). Spec: `docs/superpowers/specs/2026-07-18-data-driven-payment-methods-design.md`.

## Global Constraints

- Target framework `net10.0-windows`; `Nullable` enable; `ImplicitUsings` enable.
- EF entities map to **snake_case** tables/columns; PKs default `HasDefaultValueSql("gen_random_uuid()")`; entity configs are `IEntityTypeConfiguration<T>` in `src/IndyPOS.Infrastructure/Persistence/StoreHub/Configurations/` (auto-discovered via `ApplyConfigurationsFromAssembly`).
- **No EF `HasData` seeding** — seed at runtime via an idempotent seeder in `src/IndyPOS.Infrastructure/Persistence/StoreHub/Seeders/`, invoked from StoreHub `Program.cs` on the `migrate` path, mirroring `InitialAdminSeeder`.
- Mediator handlers are registered **individually** in `src/IndyPOS.StoreHub/Program.cs` via `builder.Services.AddTransient<ICommandHandler<Cmd,Resp>, Handler>()` (do NOT rely on assembly scanning for StoreHub).
- Endpoint authorization is **capability-based**: add a `Capability` constant + `RoleCapabilities` mapping + a named `AddPolicy(...)` policy, then `.RequireAuthorization("<PolicyName>")`. There is no role-name gating in this codebase.
- Repositories are `Scoped`, take `StoreHubDbContext` + `IStoreIdentityService`, and filter every query by `_storeIdentity.StoreId`. `IStoreIdentityService` is `Singleton`.
- Stable payment `Code` values (PascalCase, match legacy enum names): `Cash`, `MoneyTransfer`, `WelfareCard`, `PayLater`, `M33WeLove`, `FiftyFifty`, `WeWin`.
- PayLater's "GeneralHardware only" is a **code invariant** in `PaymentMethodPolicy`, independent of any data row.
- Commit after every task. Do not convert existing classes to records or vice versa.
- Domain namespaces: entities in `IndyPOS.Domain.Entities.Core`, enums in `IndyPOS.Domain.Enums`, value objects/rules in `IndyPOS.Domain.ValueObjects`. Beware the legacy `IndyPOS.Domain.Entities.Invoice` (int key) — the Core one is `IndyPOS.Domain.Entities.Core.Invoice`.

---

## File Structure

**Domain (`src/IndyPOS.Domain/`)**
- `Entities/Core/PaymentMethod.cs` — catalog row entity (NEW)
- `Enums/PaymentMethodKind.cs` — `Permanent | GovernmentCampaign` (NEW)
- `ValueObjects/PaymentMethodPolicy.cs` — pure `Offerable(methods, storeType)` rule + PayLater invariant (NEW)

**Application (`src/IndyPOS.Application/`)**
- `Abstractions/StoreHub/Repositories/IPaymentMethodRepository.cs` (NEW)
- `Abstractions/StoreHub/IStoreHubClient.cs` — add offerable + admin methods (MODIFY)
- `UseCases/StoreHub/PaymentMethods/` — `PaymentMethodDto`, `GetOfferablePaymentMethodsQuery(+Handler)`, `AddCampaignPaymentMethodCommand(+Handler)`, `TogglePaymentMethodCommand(+Handler)`, `EditPaymentMethodDisplayCommand(+Handler)` (NEW)
- `Common/Authorization/` — add `Capability.ManagePaymentMethods` + `RoleCapabilities` mapping (MODIFY, exact file found in Task 7)
- `UseCases/StoreHub/Sales/Complete/CompleteSaleCommandHandler.cs` — generalize enforcement (MODIFY)
- `Common/Constants/PaymentMethodCodes.cs` — stable code constants (NEW)

**Infrastructure (`src/IndyPOS.Infrastructure/`)**
- `Persistence/StoreHub/Configurations/PaymentMethodConfiguration.cs` (NEW)
- `Persistence/StoreHub/Repositories/PaymentMethodRepository.cs` (NEW)
- `Persistence/StoreHub/Seeders/PaymentMethodSeeder.cs` (NEW)
- `Persistence/StoreHub/Migrations/<timestamp>_AddPaymentMethodTable.cs` (NEW, generated)
- `Services/StoreHub/StoreHubHttpClient.cs` — implement new client methods (MODIFY)
- `ConfigureServices.cs` — register repo + seeder (MODIFY)

**StoreHub (`src/IndyPOS.StoreHub/Program.cs`)** — GET offerable + admin endpoints, handler + policy registration (MODIFY)

**WinForms (`src/IndyPOS.Windows.Forms/`)**
- `UI/Payment/AcceptPaymentForm.cs` — dynamic catalog-driven buttons (MODIFY)
- `UI/Settings/PaymentMethodsSettingsPanel.cs` — admin management screen (NEW)
- `ISaleService` + impl — `AddPayment(string methodCode, decimal, string)` overload (MODIFY)

**Installer (`installer/IndyPOS.Bootstrapper/`)**
- `Installers/InstallationConfig.cs` — `StoreType` property (MODIFY)
- `UI/InstallationWizard.cs` — store-type picker (MODIFY)
- `Installers/DatabaseSetup.cs` — write `Store:Type` (MODIFY)

**Tests**
- `tests/IndyPOS.Domain.Tests/` — NEW project (`PaymentMethodPolicyTests`, `StoreTypeFeaturesTests`)
- `tests/IndyPOS.Application.Tests/StoreHub/PaymentMethods/` — service + command/query + CompleteSale tests

---

## Phasing (for checkpointing)

- **Phase A — backend foundation (Tasks 1–5):** domain rule, persistence, seed, catalog service, server enforcement. Independently valuable (server enforces even before the UI is dynamic).
- **Phase B — API + client (Tasks 6–8):** offerable + admin endpoints, WinForms HTTP client methods.
- **Phase C — UI + installer (Tasks 9–12):** dynamic payment UI, admin screen, StoreType picker, data migration.

---

### Task 1: Domain — `PaymentMethod` entity, `PaymentMethodKind`, `PaymentMethodPolicy` (+ new Domain.Tests project)

**Files:**
- Create: `tests/IndyPOS.Domain.Tests/IndyPOS.Domain.Tests.csproj`
- Create: `tests/IndyPOS.Domain.Tests/ValueObjects/PaymentMethodPolicyTests.cs`
- Create: `src/IndyPOS.Domain/Enums/PaymentMethodKind.cs`
- Create: `src/IndyPOS.Domain/Entities/Core/PaymentMethod.cs`
- Create: `src/IndyPOS.Domain/ValueObjects/PaymentMethodPolicy.cs`

**Interfaces:**
- Produces:
  - `enum PaymentMethodKind { Permanent = 1, GovernmentCampaign = 2 }`
  - `class PaymentMethod { string Code; string DisplayName; PaymentMethodKind Kind; bool IsEnabled; int DisplayOrder; DateTime? ValidFrom; DateTime? ValidTo; string StoreId; DateTime CreatedUtc; DateTime LastModifiedUtc; }`
  - `static class PaymentMethodPolicy { IReadOnlyList<PaymentMethod> Offerable(IEnumerable<PaymentMethod> methods, StoreType storeType); bool IsOfferable(PaymentMethod method, StoreType storeType); }`

- [ ] **Step 1: Create the Domain.Tests project file**

Create `tests/IndyPOS.Domain.Tests/IndyPOS.Domain.Tests.csproj` (mirrors `IndyPOS.Application.Tests.csproj`, trimmed to Domain-only — no Moq/AutoFixture/EF/Infrastructure):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <IsPackable>false</IsPackable>
    <PlatformTarget>x64</PlatformTarget>
    <Platforms>x64</Platforms>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="FluentAssertions" Version="8.8.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.3.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\IndyPOS.Domain\IndyPOS.Domain.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Add the project to the solution**

Run: `dotnet sln add tests/IndyPOS.Domain.Tests/IndyPOS.Domain.Tests.csproj`
Expected: "Project ... added to the solution."

- [ ] **Step 3: Write the failing policy test**

Create `tests/IndyPOS.Domain.Tests/ValueObjects/PaymentMethodPolicyTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using IndyPOS.Domain.ValueObjects;
using Xunit;

namespace IndyPOS.Domain.Tests.ValueObjects;

public class PaymentMethodPolicyTests
{
    private static PaymentMethod Method(string code, bool enabled = true, int order = 0) => new()
    {
        Code = code, DisplayName = code, Kind = PaymentMethodKind.Permanent,
        IsEnabled = enabled, DisplayOrder = order, StoreId = "s"
    };

    [Fact]
    public void Offerable_ForGeneralHardware_ShouldIncludePayLater()
    {
        var methods = new[] { Method("Cash", order: 1), Method("PayLater", order: 2) };

        var result = PaymentMethodPolicy.Offerable(methods, StoreType.GeneralHardware);

        result.Select(m => m.Code).Should().ContainInOrder("Cash", "PayLater");
    }

    [Theory]
    [InlineData(StoreType.Minimart)]
    [InlineData(StoreType.CoffeeShop)]
    public void Offerable_ForNonGeneralHardware_ShouldExcludePayLaterEvenWhenEnabled(StoreType storeType)
    {
        var methods = new[] { Method("Cash"), Method("PayLater", enabled: true) };

        var result = PaymentMethodPolicy.Offerable(methods, storeType);

        result.Select(m => m.Code).Should().NotContain("PayLater");
        result.Select(m => m.Code).Should().Contain("Cash");
    }

    [Fact]
    public void Offerable_ShouldExcludeDisabledMethods()
    {
        var methods = new[] { Method("Cash"), Method("M33WeLove", enabled: false) };

        var result = PaymentMethodPolicy.Offerable(methods, StoreType.GeneralHardware);

        result.Select(m => m.Code).Should().NotContain("M33WeLove");
    }

    [Fact]
    public void Offerable_ShouldOrderByDisplayOrder()
    {
        var methods = new[] { Method("MoneyTransfer", order: 3), Method("Cash", order: 1) };

        var result = PaymentMethodPolicy.Offerable(methods, StoreType.GeneralHardware);

        result.Select(m => m.Code).Should().ContainInOrder("Cash", "MoneyTransfer");
    }
}
```

- [ ] **Step 4: Run the test to verify it fails**

Run: `dotnet test tests/IndyPOS.Domain.Tests/IndyPOS.Domain.Tests.csproj`
Expected: FAIL — `PaymentMethod`, `PaymentMethodKind`, `PaymentMethodPolicy` do not exist (compile error).

- [ ] **Step 5: Create the enum**

Create `src/IndyPOS.Domain/Enums/PaymentMethodKind.cs`:

```csharp
namespace IndyPOS.Domain.Enums;

public enum PaymentMethodKind
{
    Permanent = 1,
    GovernmentCampaign = 2
}
```

- [ ] **Step 6: Create the entity**

Create `src/IndyPOS.Domain/Entities/Core/PaymentMethod.cs`:

```csharp
using IndyPOS.Domain.Enums;

namespace IndyPOS.Domain.Entities.Core;

/// <summary>
/// A payment method available in the POS. Rows are data (not a hardcoded enum)
/// so government-campaign methods can be added/retired without a redeploy.
/// Code is the stable key stored on payments and used in reports.
/// </summary>
public class PaymentMethod
{
    public string Code { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public PaymentMethodKind Kind { get; set; }
    public bool IsEnabled { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime? ValidFrom { get; set; }   // informational only — not enforced
    public DateTime? ValidTo { get; set; }      // informational only — not enforced
    public string StoreId { get; set; } = default!;
    public DateTime CreatedUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }
}
```

- [ ] **Step 7: Create the policy (with the PayLater code invariant)**

Create `src/IndyPOS.Domain/ValueObjects/PaymentMethodPolicy.cs`:

```csharp
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using IndyPOS.Domain.Common.Constants; // PaymentMethodCodes (Task 4 adds it in Application; see note)

namespace IndyPOS.Domain.ValueObjects;

/// <summary>
/// Pure rule (no I/O) deciding which catalog methods are offerable for a store
/// type. PayLater is GeneralHardware-only as a hard code invariant, independent
/// of the row's IsEnabled — so a hand-edited catalog cannot enable it elsewhere.
/// </summary>
public static class PaymentMethodPolicy
{
    private const string PayLaterCode = "PayLater";

    public static IReadOnlyList<PaymentMethod> Offerable(
        IEnumerable<PaymentMethod> methods, StoreType storeType) =>
        methods
            .Where(m => IsOfferable(m, storeType))
            .OrderBy(m => m.DisplayOrder)
            .ToList();

    public static bool IsOfferable(PaymentMethod method, StoreType storeType)
    {
        if (!method.IsEnabled) return false;
        if (IsPayLater(method) && storeType != StoreType.GeneralHardware) return false;
        return true;
    }

    private static bool IsPayLater(PaymentMethod method) =>
        string.Equals(method.Code, PayLaterCode, StringComparison.OrdinalIgnoreCase);
}
```

> Note: `PaymentMethodPolicy` uses a local `PayLaterCode` const (Domain must not depend on Application). Do NOT add the `using IndyPOS.Domain.Common.Constants;` line — it was shown to flag that the shared code constants (Task 4) live in Application, not Domain. Keep the Domain policy self-contained with its local const.

- [ ] **Step 8: Fix the policy file (remove the stray using)**

Edit `src/IndyPOS.Domain/ValueObjects/PaymentMethodPolicy.cs`: delete the line `using IndyPOS.Domain.Common.Constants;`. The policy is self-contained via its local `PayLaterCode`.

- [ ] **Step 9: Run the tests to verify they pass**

Run: `dotnet test tests/IndyPOS.Domain.Tests/IndyPOS.Domain.Tests.csproj`
Expected: PASS (4 tests).

- [ ] **Step 10: Commit**

```bash
git add src/IndyPOS.Domain/Enums/PaymentMethodKind.cs src/IndyPOS.Domain/Entities/Core/PaymentMethod.cs src/IndyPOS.Domain/ValueObjects/PaymentMethodPolicy.cs tests/IndyPOS.Domain.Tests/
git commit -m "feat(domain): payment method catalog entity + offerable policy (PayLater invariant)"
```

---

### Task 2: Persistence — repository + EF config + migration

**Files:**
- Create: `src/IndyPOS.Application/Abstractions/StoreHub/Repositories/IPaymentMethodRepository.cs`
- Create: `src/IndyPOS.Infrastructure/Persistence/StoreHub/Configurations/PaymentMethodConfiguration.cs`
- Create: `src/IndyPOS.Infrastructure/Persistence/StoreHub/Repositories/PaymentMethodRepository.cs`
- Modify: `src/IndyPOS.Infrastructure/Persistence/StoreHub/StoreHubDbContext.cs` (add `DbSet<PaymentMethod>`)
- Modify: `src/IndyPOS.Infrastructure/ConfigureServices.cs` (register repo)
- Generated: `src/IndyPOS.Infrastructure/Persistence/StoreHub/Migrations/<timestamp>_AddPaymentMethodTable.cs`

**Interfaces:**
- Consumes: `PaymentMethod` (Task 1).
- Produces:
  - `interface IPaymentMethodRepository { Task<IReadOnlyList<PaymentMethod>> GetAllAsync(CancellationToken); Task<PaymentMethod?> GetByCodeAsync(string code, CancellationToken); Task AddAsync(PaymentMethod method, CancellationToken); Task UpdateAsync(PaymentMethod method, CancellationToken); }`

- [ ] **Step 1: Write the failing repository test (EF InMemory)**

Create `tests/IndyPOS.Application.Tests/StoreHub/PaymentMethods/PaymentMethodRepositoryTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;
using IndyPOS.Mock;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.PaymentMethods;

public class PaymentMethodRepositoryTests
{
    private static StoreHubDbContext NewContext() =>
        new(new DbContextOptionsBuilder<StoreHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task GetAllAsync_ShouldReturnOnlyThisStoresMethods()
    {
        await using var ctx = NewContext();
        ctx.Set<PaymentMethod>().AddRange(
            new PaymentMethod { Code = "Cash", DisplayName = "Cash", Kind = PaymentMethodKind.Permanent, IsEnabled = true, StoreId = "store-A" },
            new PaymentMethod { Code = "Cash", DisplayName = "Cash", Kind = PaymentMethodKind.Permanent, IsEnabled = true, StoreId = "store-B" });
        await ctx.SaveChangesAsync();
        var identity = new MockStoreIdentityService { StoreId = "store-A" };
        var sut = new PaymentMethodRepository(ctx, identity);

        var result = await sut.GetAllAsync(default);

        result.Should().OnlyContain(m => m.StoreId == "store-A");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/IndyPOS.Application.Tests/IndyPOS.Application.Tests.csproj --filter "FullyQualifiedName~PaymentMethodRepositoryTests"`
Expected: FAIL — `IPaymentMethodRepository`/`PaymentMethodRepository` do not exist.

- [ ] **Step 3: Create the repository interface**

Create `src/IndyPOS.Application/Abstractions/StoreHub/Repositories/IPaymentMethodRepository.cs`:

```csharp
using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Application.Abstractions.StoreHub.Repositories;

public interface IPaymentMethodRepository
{
    Task<IReadOnlyList<PaymentMethod>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PaymentMethod?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task AddAsync(PaymentMethod method, CancellationToken cancellationToken = default);
    Task UpdateAsync(PaymentMethod method, CancellationToken cancellationToken = default);
}
```

> Confirm the exact namespace of the other repository interfaces (the explore found `IndyPOS.Application.Abstractions.StoreHub.Repositories`). Match it exactly.

- [ ] **Step 4: Add the DbSet**

In `src/IndyPOS.Infrastructure/Persistence/StoreHub/StoreHubDbContext.cs`, add alongside the other DbSets:

```csharp
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
```

- [ ] **Step 5: Create the EF configuration**

Create `src/IndyPOS.Infrastructure/Persistence/StoreHub/Configurations/PaymentMethodConfiguration.cs` (mirrors `PaymentConfiguration` style; composite key `(StoreId, Code)` since Code is unique per store):

```csharp
using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Configurations;

public class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        builder.ToTable("payment_method");
        builder.HasKey(e => new { e.StoreId, e.Code });

        builder.Property(e => e.StoreId).HasColumnName("store_id").HasMaxLength(50).IsRequired();
        builder.Property(e => e.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Kind).HasColumnName("kind").HasConversion<int>().IsRequired();
        builder.Property(e => e.IsEnabled).HasColumnName("is_enabled").IsRequired();
        builder.Property(e => e.DisplayOrder).HasColumnName("display_order").IsRequired();
        builder.Property(e => e.ValidFrom).HasColumnName("valid_from");
        builder.Property(e => e.ValidTo).HasColumnName("valid_to");
        builder.Property(e => e.CreatedUtc).HasColumnName("created_utc").IsRequired();
        builder.Property(e => e.LastModifiedUtc).HasColumnName("last_modified_utc").IsRequired();
    }
}
```

- [ ] **Step 6: Create the repository implementation**

Create `src/IndyPOS.Infrastructure/Persistence/StoreHub/Repositories/PaymentMethodRepository.cs`:

```csharp
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;

public class PaymentMethodRepository : IPaymentMethodRepository
{
    private readonly StoreHubDbContext _dbContext;
    private readonly IStoreIdentityService _storeIdentity;

    public PaymentMethodRepository(StoreHubDbContext dbContext, IStoreIdentityService storeIdentity)
    {
        _dbContext = dbContext;
        _storeIdentity = storeIdentity;
    }

    public async Task<IReadOnlyList<PaymentMethod>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var storeId = _storeIdentity.StoreId;
        return await _dbContext.PaymentMethods.AsNoTracking()
            .Where(m => m.StoreId == storeId)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<PaymentMethod?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var storeId = _storeIdentity.StoreId;
        return await _dbContext.PaymentMethods
            .FirstOrDefaultAsync(m => m.StoreId == storeId && m.Code == code, cancellationToken);
    }

    public async Task AddAsync(PaymentMethod method, CancellationToken cancellationToken = default)
    {
        _dbContext.PaymentMethods.Add(method);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PaymentMethod method, CancellationToken cancellationToken = default)
    {
        _dbContext.PaymentMethods.Update(method);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
```

> Confirm the namespace of `IStoreIdentityService` (`IndyPOS.Application.Common.Interfaces`) and match the `using` exactly.

- [ ] **Step 7: Run the repository test to verify it passes**

Run: `dotnet test tests/IndyPOS.Application.Tests/IndyPOS.Application.Tests.csproj --filter "FullyQualifiedName~PaymentMethodRepositoryTests"`
Expected: PASS.

- [ ] **Step 8: Register the repository in DI**

In `src/IndyPOS.Infrastructure/ConfigureServices.cs`, inside `AddStoreHubServices`, add to the scoped chain:

```csharp
            .AddScoped<IPaymentMethodRepository, PaymentMethodRepository>()
```

- [ ] **Step 9: Generate the migration**

Run (from repo root; adjust the startup/context args to match how other StoreHub migrations were generated — check the existing migration headers if unsure):
```bash
dotnet ef migrations add AddPaymentMethodTable --project src/IndyPOS.Infrastructure --startup-project src/IndyPOS.StoreHub --context StoreHubDbContext --output-dir Persistence/StoreHub/Migrations
```
Expected: a new `<timestamp>_AddPaymentMethodTable.cs` + snapshot update creating the `payment_method` table with the columns/keys from Step 5.

- [ ] **Step 10: Build to verify the migration compiles**

Run: `dotnet build src/IndyPOS.Infrastructure/IndyPOS.Infrastructure.csproj -c Release`
Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 11: Commit**

```bash
git add src/IndyPOS.Application/Abstractions/StoreHub/Repositories/IPaymentMethodRepository.cs src/IndyPOS.Infrastructure/Persistence/StoreHub/ src/IndyPOS.Infrastructure/ConfigureServices.cs tests/IndyPOS.Application.Tests/StoreHub/PaymentMethods/PaymentMethodRepositoryTests.cs
git commit -m "feat(infra): payment_method table, EF config, repository + migration"
```

---

### Task 3: Seed the catalog (idempotent runtime seeder)

**Files:**
- Create: `src/IndyPOS.Application/Common/Constants/PaymentMethodCodes.cs`
- Create: `src/IndyPOS.Infrastructure/Persistence/StoreHub/Seeders/PaymentMethodSeeder.cs`
- Modify: `src/IndyPOS.StoreHub/Program.cs` (invoke the seeder on the `migrate` path, mirroring `SeedInitialAdminAsync`)
- Modify: `src/IndyPOS.Infrastructure/ConfigureServices.cs` (register the seeder if the existing seeders are DI-registered — mirror `InitialAdminSeeder`)

**Interfaces:**
- Consumes: `IPaymentMethodRepository`, `IStoreIdentityService`, `PaymentMethod`, `PaymentMethodKind`.
- Produces: `PaymentMethodCodes` (const strings), `PaymentMethodSeeder.SeedAsync(CancellationToken)`.

- [ ] **Step 1: Create the code constants**

Create `src/IndyPOS.Application/Common/Constants/PaymentMethodCodes.cs`:

```csharp
namespace IndyPOS.Application.Common.Constants;

/// <summary>Stable payment-method Codes. Stored on payments + used in reports.</summary>
public static class PaymentMethodCodes
{
    public const string Cash = "Cash";
    public const string MoneyTransfer = "MoneyTransfer";
    public const string WelfareCard = "WelfareCard";
    public const string PayLater = "PayLater";
    public const string M33WeLove = "M33WeLove";
    public const string FiftyFifty = "FiftyFifty";
    public const string WeWin = "WeWin";
}
```

- [ ] **Step 2: Write the failing seeder test**

Create `tests/IndyPOS.Application.Tests/StoreHub/PaymentMethods/PaymentMethodSeederTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;
using IndyPOS.Infrastructure.Persistence.StoreHub.Seeders;
using IndyPOS.Mock;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.PaymentMethods;

public class PaymentMethodSeederTests
{
    private static StoreHubDbContext NewContext() =>
        new(new DbContextOptionsBuilder<StoreHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task SeedAsync_OnEmptyCatalog_ShouldSeedSevenMethodsWithDeadCampaignsDisabled()
    {
        await using var ctx = NewContext();
        var repo = new PaymentMethodRepository(ctx, new MockStoreIdentityService { StoreId = "store-A" });
        var sut = new PaymentMethodSeeder(repo, new MockStoreIdentityService { StoreId = "store-A" },
            NullLogger<PaymentMethodSeeder>.Instance);

        await sut.SeedAsync(default);

        var all = await repo.GetAllAsync();
        all.Should().HaveCount(7);
        all.Single(m => m.Code == "PayLater").IsEnabled.Should().BeTrue();
        all.Single(m => m.Code == "M33WeLove").IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task SeedAsync_RunTwice_ShouldBeIdempotent()
    {
        await using var ctx = NewContext();
        var repo = new PaymentMethodRepository(ctx, new MockStoreIdentityService { StoreId = "store-A" });
        var sut = new PaymentMethodSeeder(repo, new MockStoreIdentityService { StoreId = "store-A" },
            NullLogger<PaymentMethodSeeder>.Instance);

        await sut.SeedAsync(default);
        await sut.SeedAsync(default);

        (await repo.GetAllAsync()).Should().HaveCount(7);
    }
}
```

- [ ] **Step 3: Run test to verify it fails**

Run: `dotnet test tests/IndyPOS.Application.Tests/IndyPOS.Application.Tests.csproj --filter "FullyQualifiedName~PaymentMethodSeederTests"`
Expected: FAIL — `PaymentMethodSeeder` does not exist.

- [ ] **Step 4: Implement the seeder**

Create `src/IndyPOS.Infrastructure/Persistence/StoreHub/Seeders/PaymentMethodSeeder.cs` (mirror `InitialAdminSeeder`'s ctor-injection + idempotent check-then-insert; `DateTime.UtcNow` is fine here):

```csharp
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Constants;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Seeders;

/// <summary>Seeds the known payment methods for this store. Idempotent — only
/// inserts a Code that is absent, so re-runs (and future new campaigns) are safe.</summary>
public class PaymentMethodSeeder
{
    private readonly IPaymentMethodRepository _repository;
    private readonly IStoreIdentityService _storeIdentity;
    private readonly ILogger<PaymentMethodSeeder> _logger;

    public PaymentMethodSeeder(IPaymentMethodRepository repository, IStoreIdentityService storeIdentity,
        ILogger<PaymentMethodSeeder> logger)
    {
        _repository = repository;
        _storeIdentity = storeIdentity;
        _logger = logger;
    }

    private sealed record Seed(string Code, string DisplayName, PaymentMethodKind Kind, bool Enabled, int Order);

    private static readonly Seed[] Defaults =
    [
        new(PaymentMethodCodes.Cash, "เงินสด", PaymentMethodKind.Permanent, true, 1),
        new(PaymentMethodCodes.MoneyTransfer, "เงินโอน", PaymentMethodKind.Permanent, true, 2),
        new(PaymentMethodCodes.WelfareCard, "บัตรสวัสดิการแห่งรัฐ", PaymentMethodKind.Permanent, true, 3),
        new(PaymentMethodCodes.PayLater, "เงินเชื่อ", PaymentMethodKind.Permanent, true, 4),
        new(PaymentMethodCodes.M33WeLove, "ม33เรารักกัน", PaymentMethodKind.GovernmentCampaign, false, 5),
        new(PaymentMethodCodes.FiftyFifty, "คนละครึ่ง", PaymentMethodKind.GovernmentCampaign, false, 6),
        new(PaymentMethodCodes.WeWin, "เราชนะ", PaymentMethodKind.GovernmentCampaign, false, 7),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var storeId = _storeIdentity.StoreId;
        var now = DateTime.UtcNow;
        var seededCount = 0;

        foreach (var seed in Defaults)
        {
            if (await _repository.GetByCodeAsync(seed.Code, cancellationToken) is not null) continue;

            await _repository.AddAsync(new PaymentMethod
            {
                Code = seed.Code,
                DisplayName = seed.DisplayName,
                Kind = seed.Kind,
                IsEnabled = seed.Enabled,
                DisplayOrder = seed.Order,
                StoreId = storeId,
                CreatedUtc = now,
                LastModifiedUtc = now
            }, cancellationToken);
            seededCount++;
        }

        _logger.LogInformation("Payment method seeding complete: {Count} inserted.", seededCount);
    }
}
```

- [ ] **Step 5: Run the seeder tests to verify they pass**

Run: `dotnet test tests/IndyPOS.Application.Tests/IndyPOS.Application.Tests.csproj --filter "FullyQualifiedName~PaymentMethodSeederTests"`
Expected: PASS (2 tests).

- [ ] **Step 6: Wire the seeder into StoreHub startup**

In `src/IndyPOS.StoreHub/Program.cs`, find where `SeedInitialAdminAsync()` (or the equivalent seed step) runs on the `migrate` path and add a `PaymentMethodSeeder.SeedAsync()` invocation immediately after, following the exact same resolution pattern the file uses for `InitialAdminSeeder` (register `PaymentMethodSeeder` in DI if the existing seeders are registered, then resolve + call within the same scope). Mirror the existing `app.SeedInitialAdminAsync()` extension or inline block verbatim in style.

- [ ] **Step 7: Build to verify**

Run: `dotnet build src/IndyPOS.StoreHub/IndyPOS.StoreHub.csproj -c Release`
Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 8: Commit**

```bash
git add src/IndyPOS.Application/Common/Constants/PaymentMethodCodes.cs src/IndyPOS.Infrastructure/Persistence/StoreHub/Seeders/PaymentMethodSeeder.cs src/IndyPOS.StoreHub/Program.cs src/IndyPOS.Infrastructure/ConfigureServices.cs tests/IndyPOS.Application.Tests/StoreHub/PaymentMethods/PaymentMethodSeederTests.cs
git commit -m "feat(infra): seed payment method catalog (dead campaigns disabled, idempotent)"
```

---

### Task 4: Application — catalog service + query + admin commands

**Files:**
- Create: `src/IndyPOS.Application/UseCases/StoreHub/PaymentMethods/PaymentMethodDto.cs`
- Create: `src/IndyPOS.Application/UseCases/StoreHub/PaymentMethods/IPaymentMethodCatalogService.cs` + `PaymentMethodCatalogService.cs`
- Create: `.../GetOfferablePaymentMethodsQuery.cs` (+ handler), `.../AddCampaignPaymentMethodCommand.cs` (+ handler), `.../TogglePaymentMethodCommand.cs` (+ handler), `.../EditPaymentMethodDisplayCommand.cs` (+ handler)
- Modify: `src/IndyPOS.Infrastructure/ConfigureServices.cs` (register the service)

**Interfaces:**
- Consumes: `IPaymentMethodRepository`, `IStoreIdentityService`, `PaymentMethodPolicy`, `PaymentMethod`, `PaymentMethodKind`, Nokpirab `ICommand<T>`/`ICommandHandler<,>`/`IQuery<T>`/`IQueryHandler<,>` (confirm exact query interface names from an existing query in the codebase).
- Produces:
  - `record PaymentMethodDto(string Code, string DisplayName, PaymentMethodKind Kind, bool IsEnabled, int DisplayOrder)`
  - `interface IPaymentMethodCatalogService { Task<IReadOnlyList<PaymentMethod>> GetOfferableAsync(CancellationToken); Task<IReadOnlyList<PaymentMethod>> GetAllAsync(CancellationToken); Task AddCampaignAsync(string code, string displayName, int displayOrder, CancellationToken); Task SetEnabledAsync(string code, bool enabled, CancellationToken); Task UpdateDisplayAsync(string code, string displayName, int displayOrder, CancellationToken); }`

- [ ] **Step 1: Write the failing catalog-service test**

Create `tests/IndyPOS.Application.Tests/StoreHub/PaymentMethods/PaymentMethodCatalogServiceTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.UseCases.StoreHub.PaymentMethods;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using IndyPOS.Mock;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.PaymentMethods;

public class PaymentMethodCatalogServiceTests
{
    private static PaymentMethod M(string code, bool enabled = true, int order = 0) => new()
    { Code = code, DisplayName = code, Kind = PaymentMethodKind.Permanent, IsEnabled = enabled, DisplayOrder = order, StoreId = "s" };

    [Fact]
    public async Task GetOfferableAsync_OnMinimart_ShouldApplyPolicyAndExcludePayLater()
    {
        var repo = new Mock<IPaymentMethodRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentMethod> { M("Cash", order: 1), M("PayLater", order: 2) });
        var sut = new PaymentMethodCatalogService(repo.Object, new MockStoreIdentityService { StoreType = StoreType.Minimart });

        var result = await sut.GetOfferableAsync(default);

        result.Select(m => m.Code).Should().Contain("Cash").And.NotContain("PayLater");
    }

    [Fact]
    public async Task AddCampaignAsync_ShouldPersistGovernmentCampaignEnabled()
    {
        var repo = new Mock<IPaymentMethodRepository>();
        repo.Setup(r => r.GetByCodeAsync("SomeNew2027", It.IsAny<CancellationToken>())).ReturnsAsync((PaymentMethod?)null);
        var sut = new PaymentMethodCatalogService(repo.Object, new MockStoreIdentityService());

        await sut.AddCampaignAsync("SomeNew2027", "New Campaign", 8, default);

        repo.Verify(r => r.AddAsync(It.Is<PaymentMethod>(m =>
            m.Code == "SomeNew2027" && m.Kind == PaymentMethodKind.GovernmentCampaign && m.IsEnabled), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddCampaignAsync_DuplicateCode_ShouldThrow()
    {
        var repo = new Mock<IPaymentMethodRepository>();
        repo.Setup(r => r.GetByCodeAsync("Cash", It.IsAny<CancellationToken>())).ReturnsAsync(M("Cash"));
        var sut = new PaymentMethodCatalogService(repo.Object, new MockStoreIdentityService());

        var act = () => sut.AddCampaignAsync("Cash", "dup", 9, default);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/IndyPOS.Application.Tests/IndyPOS.Application.Tests.csproj --filter "FullyQualifiedName~PaymentMethodCatalogServiceTests"`
Expected: FAIL — service does not exist.

- [ ] **Step 3: Create the DTO + service interface**

Create `src/IndyPOS.Application/UseCases/StoreHub/PaymentMethods/PaymentMethodDto.cs`:

```csharp
using IndyPOS.Domain.Enums;

namespace IndyPOS.Application.UseCases.StoreHub.PaymentMethods;

public record PaymentMethodDto(string Code, string DisplayName, PaymentMethodKind Kind, bool IsEnabled, int DisplayOrder);
```

Create `src/IndyPOS.Application/UseCases/StoreHub/PaymentMethods/IPaymentMethodCatalogService.cs`:

```csharp
using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Application.UseCases.StoreHub.PaymentMethods;

public interface IPaymentMethodCatalogService
{
    Task<IReadOnlyList<PaymentMethod>> GetOfferableAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentMethod>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddCampaignAsync(string code, string displayName, int displayOrder, CancellationToken cancellationToken = default);
    Task SetEnabledAsync(string code, bool enabled, CancellationToken cancellationToken = default);
    Task UpdateDisplayAsync(string code, string displayName, int displayOrder, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 4: Implement the service**

Create `src/IndyPOS.Application/UseCases/StoreHub/PaymentMethods/PaymentMethodCatalogService.cs`:

```csharp
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using IndyPOS.Domain.ValueObjects;

namespace IndyPOS.Application.UseCases.StoreHub.PaymentMethods;

public class PaymentMethodCatalogService : IPaymentMethodCatalogService
{
    private readonly IPaymentMethodRepository _repository;
    private readonly IStoreIdentityService _storeIdentity;

    public PaymentMethodCatalogService(IPaymentMethodRepository repository, IStoreIdentityService storeIdentity)
    {
        _repository = repository;
        _storeIdentity = storeIdentity;
    }

    public async Task<IReadOnlyList<PaymentMethod>> GetOfferableAsync(CancellationToken cancellationToken = default)
    {
        var all = await _repository.GetAllAsync(cancellationToken);
        return PaymentMethodPolicy.Offerable(all, _storeIdentity.StoreType);
    }

    public Task<IReadOnlyList<PaymentMethod>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);

    public async Task AddCampaignAsync(string code, string displayName, int displayOrder, CancellationToken cancellationToken = default)
    {
        if (await _repository.GetByCodeAsync(code, cancellationToken) is not null)
            throw new InvalidOperationException($"Payment method '{code}' already exists.");

        var now = DateTime.UtcNow;
        await _repository.AddAsync(new PaymentMethod
        {
            Code = code, DisplayName = displayName, Kind = PaymentMethodKind.GovernmentCampaign,
            IsEnabled = true, DisplayOrder = displayOrder, StoreId = _storeIdentity.StoreId,
            CreatedUtc = now, LastModifiedUtc = now
        }, cancellationToken);
    }

    public async Task SetEnabledAsync(string code, bool enabled, CancellationToken cancellationToken = default)
    {
        var method = await Require(code, cancellationToken);
        method.IsEnabled = enabled;
        method.LastModifiedUtc = DateTime.UtcNow;
        await _repository.UpdateAsync(method, cancellationToken);
    }

    public async Task UpdateDisplayAsync(string code, string displayName, int displayOrder, CancellationToken cancellationToken = default)
    {
        var method = await Require(code, cancellationToken);
        method.DisplayName = displayName;
        method.DisplayOrder = displayOrder;
        method.LastModifiedUtc = DateTime.UtcNow;
        await _repository.UpdateAsync(method, cancellationToken);
    }

    private async Task<PaymentMethod> Require(string code, CancellationToken cancellationToken) =>
        await _repository.GetByCodeAsync(code, cancellationToken)
        ?? throw new InvalidOperationException($"Payment method '{code}' not found.");
}
```

- [ ] **Step 5: Run the service tests to verify they pass**

Run: `dotnet test tests/IndyPOS.Application.Tests/IndyPOS.Application.Tests.csproj --filter "FullyQualifiedName~PaymentMethodCatalogServiceTests"`
Expected: PASS (3 tests).

- [ ] **Step 6: Add the Nokpirab query + command wrappers**

Inspect an existing query (e.g. `GetProductsQuery` + its handler) to copy the exact `IQuery<T>`/`IQueryHandler<TQuery,TResp>` and `ICommand<T>`/`ICommandHandler<TCmd,TResp>` shapes. Then create thin wrappers in `src/IndyPOS.Application/UseCases/StoreHub/PaymentMethods/`, each delegating to `IPaymentMethodCatalogService`:

- `GetOfferablePaymentMethodsQuery() : IQuery<IReadOnlyList<PaymentMethodDto>>` + handler → `service.GetOfferableAsync()` mapped to `PaymentMethodDto`.
- `AddCampaignPaymentMethodCommand(string Code, string DisplayName, int DisplayOrder) : ICommand<Unit-or-equivalent>` + handler → `service.AddCampaignAsync(...)`.
- `TogglePaymentMethodCommand(string Code, bool Enabled) : ICommand<...>` + handler → `service.SetEnabledAsync(...)`.
- `EditPaymentMethodDisplayCommand(string Code, string DisplayName, int DisplayOrder) : ICommand<...>` + handler → `service.UpdateDisplayAsync(...)`.

(Match the exact command/handler return-type convention the codebase uses — the explore showed handlers return a response type, e.g. `CompleteSaleResponse`; use a minimal response or the codebase's `Unit` equivalent if one exists.)

- [ ] **Step 7: Register the service in DI**

In `src/IndyPOS.Infrastructure/ConfigureServices.cs` (`AddStoreHubServices`), add:

```csharp
            .AddScoped<IPaymentMethodCatalogService, PaymentMethodCatalogService>()
```

- [ ] **Step 8: Build + run the PaymentMethods tests**

Run: `dotnet build -c Release` then `dotnet test tests/IndyPOS.Application.Tests/IndyPOS.Application.Tests.csproj --filter "FullyQualifiedName~PaymentMethods"`
Expected: build 0 errors; tests pass.

- [ ] **Step 9: Commit**

```bash
git add src/IndyPOS.Application/UseCases/StoreHub/PaymentMethods/ src/IndyPOS.Infrastructure/ConfigureServices.cs tests/IndyPOS.Application.Tests/StoreHub/PaymentMethods/PaymentMethodCatalogServiceTests.cs
git commit -m "feat(application): payment method catalog service + query/command handlers"
```

---

### Task 5: Generalize `CompleteSale` server enforcement

**Files:**
- Modify: `src/IndyPOS.Application/UseCases/StoreHub/Sales/Complete/CompleteSaleCommandHandler.cs`
- Test: `tests/IndyPOS.Application.Tests/StoreHub/Sales/Commands/CompleteSaleCommandHandlerTests.cs` (add cases)

**Interfaces:**
- Consumes: `IPaymentMethodCatalogService.GetOfferableAsync`.

- [ ] **Step 1: Write failing tests for the generalized rule**

Add to `CompleteSaleCommandHandlerTests.cs` (follow the file's `[Theory]/[CustomAutoData]` + `[Frozen] Mock<...>` style; add a `[Frozen] Mock<IPaymentMethodCatalogService>` parameter and set up its `GetOfferableAsync` to return a specific offerable set):

```csharp
    [Theory]
    [CustomAutoData]
    public async Task HandleAsync_WhenPaymentMethodNotOfferable_ShouldReject(
        [Frozen] Mock<IPaymentMethodCatalogService> catalog,
        [Frozen] Mock<ISaleRepository> saleRepository,
        CompleteSaleCommandHandler sut)
    {
        catalog.Setup(c => c.GetOfferableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentMethod> { new() { Code = "Cash", IsEnabled = true, StoreId = "s" } });
        var command = /* build a CompleteSaleCommand whose Payments include Method = "PayLater" */;

        var act = () => sut.HandleAsync(command);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Theory]
    [CustomAutoData]
    public async Task HandleAsync_WhenAllPaymentMethodsOfferable_ShouldComplete(
        [Frozen] Mock<IPaymentMethodCatalogService> catalog,
        [Frozen] Mock<ISaleRepository> saleRepository,
        CompleteSaleCommandHandler sut)
    {
        catalog.Setup(c => c.GetOfferableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentMethod> { new() { Code = "Cash", IsEnabled = true, StoreId = "s" } });
        var command = /* build a CompleteSaleCommand whose Payments all use Method = "Cash" */;

        var result = await sut.HandleAsync(command);

        result.Should().NotBeNull();
    }
```

(Fill the two `/* build ... */` command literals using the same construction the existing passing tests use — reuse their helper/arrange block verbatim, swapping the payment `Method` values.)

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test tests/IndyPOS.Application.Tests/IndyPOS.Application.Tests.csproj --filter "FullyQualifiedName~CompleteSaleCommandHandlerTests"`
Expected: FAIL — handler does not yet depend on `IPaymentMethodCatalogService` / does not perform the generalized check (compile error on the new ctor param, or assertion failure).

- [ ] **Step 3: Inject the catalog service + replace the PayLater-only block**

In `CompleteSaleCommandHandler.cs`:
- Add `IPaymentMethodCatalogService catalog` to the constructor (store in a field `_catalog`).
- Replace the existing PayLater block (the `hasPayLater` / `Features.PayLaterEnabled` throw) with:

```csharp
        var offerable = await _catalog.GetOfferableAsync(cancellationToken);
        var offerableCodes = offerable.Select(m => m.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rejected = command.Payments.FirstOrDefault(p => !offerableCodes.Contains(p.Method));
        if (rejected is not null)
        {
            _logger.LogWarning("Payment method rejected: Method={Method}, StoreType={StoreType}, UserId={UserId}",
                rejected.Method, _storeIdentity.StoreType, command.UserId);
            throw new InvalidOperationException(
                $"Payment method '{rejected.Method}' is not available for {_storeIdentity.StoreType} stores.");
        }
```

- [ ] **Step 4: Register the new dependency (if StoreHub registers this handler explicitly)**

The handler is DI-constructed; `IPaymentMethodCatalogService` is already registered (Task 4). No StoreHub `Program.cs` change needed beyond the existing `AddTransient<ICommandHandler<CompleteSaleCommand,...>, CompleteSaleCommandHandler>()`. Confirm the build resolves it.

- [ ] **Step 5: Run tests to verify pass**

Run: `dotnet test tests/IndyPOS.Application.Tests/IndyPOS.Application.Tests.csproj --filter "FullyQualifiedName~CompleteSaleCommandHandlerTests"`
Expected: PASS (existing 4 + 2 new).

- [ ] **Step 6: Commit**

```bash
git add src/IndyPOS.Application/UseCases/StoreHub/Sales/Complete/CompleteSaleCommandHandler.cs tests/IndyPOS.Application.Tests/StoreHub/Sales/Commands/CompleteSaleCommandHandlerTests.cs
git commit -m "feat(application): enforce sale payment methods via offerable catalog (drops PayLater-only check)"
```

---

### Task 6: StoreHub — `GET /payment-methods` endpoint

**Files:**
- Modify: `src/IndyPOS.StoreHub/Program.cs`

**Interfaces:**
- Consumes: `IQueryHandler<GetOfferablePaymentMethodsQuery, IReadOnlyList<PaymentMethodDto>>` (Task 4).

- [ ] **Step 1: Register the query handler**

In `src/IndyPOS.StoreHub/Program.cs`, alongside the other `AddTransient<...>` handler registrations, add:

```csharp
builder.Services.AddTransient<IQueryHandler<GetOfferablePaymentMethodsQuery, IReadOnlyList<PaymentMethodDto>>, GetOfferablePaymentMethodsQueryHandler>();
```

- [ ] **Step 2: Map the endpoint**

Add near the `/products` GET (reuse the `CanReadProducts` capability — reading payment methods is a POS read, same audience):

```csharp
app.MapGet("/payment-methods", async (
    IQueryHandler<GetOfferablePaymentMethodsQuery, IReadOnlyList<PaymentMethodDto>> handler,
    CancellationToken cancellationToken) =>
{
    var methods = await handler.HandleAsync(new GetOfferablePaymentMethodsQuery(), cancellationToken);
    return Results.Ok(methods);
}).RequireAuthorization("CanReadProducts");
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build src/IndyPOS.StoreHub/IndyPOS.StoreHub.csproj -c Release`
Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 4: Commit**

```bash
git add src/IndyPOS.StoreHub/Program.cs
git commit -m "feat(storehub): GET /payment-methods returns offerable methods for this store"
```

---

### Task 7: StoreHub — admin endpoints + `CanManagePaymentMethods` capability

**Files:**
- Modify: `src/IndyPOS.Application/Common/Authorization/` — the `Capability` constants + `RoleCapabilities` mapping (exact files: locate `Capability` + `RoleCapabilities` from `CapabilityAuthorizationHandler`'s references)
- Modify: `src/IndyPOS.StoreHub/Program.cs` — policy + admin endpoints + handler registration

**Interfaces:**
- Consumes: the Task-4 admin command handlers.

- [ ] **Step 1: Add the capability**

Locate the `Capability` definition (referenced by `RoleCapabilities.HasCapability(roleId, capability)` and the existing policies like `CanManageProducts`). Add a `ManagePaymentMethods` capability constant, and grant it to the SystemAdmin role in the `RoleCapabilities` mapping (mirror how `CanManageProducts`/`ManageProducts` is granted). Match the existing naming exactly.

- [ ] **Step 2: Add the named policy**

In `src/IndyPOS.StoreHub/Program.cs`, in the `AddAuthorizationBuilder()...AddPolicy(...)` block, add (mirroring `CanManageProducts`):

```csharp
    .AddPolicy("CanManagePaymentMethods", policy =>
        policy.RequireAuthenticatedUser().AddRequirements(new CapabilityRequirement(Capability.ManagePaymentMethods)))
```

- [ ] **Step 3: Register the admin command handlers**

```csharp
builder.Services.AddTransient<ICommandHandler<AddCampaignPaymentMethodCommand, /*Resp*/>, AddCampaignPaymentMethodCommandHandler>();
builder.Services.AddTransient<ICommandHandler<TogglePaymentMethodCommand, /*Resp*/>, TogglePaymentMethodCommandHandler>();
builder.Services.AddTransient<ICommandHandler<EditPaymentMethodDisplayCommand, /*Resp*/>, EditPaymentMethodDisplayCommandHandler>();
```
(Use the actual response types from Task 4.)

- [ ] **Step 4: Map the admin endpoints**

```csharp
app.MapPost("/admin/payment-methods", async (
    ICommandHandler<AddCampaignPaymentMethodCommand, /*Resp*/> handler,
    AddCampaignPaymentMethodRequest request, CancellationToken ct) =>
{
    await handler.HandleAsync(new AddCampaignPaymentMethodCommand(request.Code, request.DisplayName, request.DisplayOrder), ct);
    return Results.Ok();
}).RequireAuthorization("CanManagePaymentMethods");

app.MapPatch("/admin/payment-methods/{code}", async (
    ICommandHandler<TogglePaymentMethodCommand, /*Resp*/> toggleHandler,
    ICommandHandler<EditPaymentMethodDisplayCommand, /*Resp*/> editHandler,
    string code, UpdatePaymentMethodRequest request, CancellationToken ct) =>
{
    if (request.IsEnabled is bool enabled)
        await toggleHandler.HandleAsync(new TogglePaymentMethodCommand(code, enabled), ct);
    if (request.DisplayName is not null)
        await editHandler.HandleAsync(new EditPaymentMethodDisplayCommand(code, request.DisplayName, request.DisplayOrder ?? 0), ct);
    return Results.Ok();
}).RequireAuthorization("CanManagePaymentMethods");
```
Define the request records (`AddCampaignPaymentMethodRequest(string Code, string DisplayName, int DisplayOrder)`, `UpdatePaymentMethodRequest(bool? IsEnabled, string? DisplayName, int? DisplayOrder)`) near the file's other request DTOs. Return `409` on the duplicate-Code `InvalidOperationException` via the existing error-handling convention (mirror how other endpoints surface domain exceptions).

- [ ] **Step 5: Build to verify**

Run: `dotnet build src/IndyPOS.StoreHub/IndyPOS.StoreHub.csproj -c Release`
Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 6: Commit**

```bash
git add src/IndyPOS.Application/Common/Authorization/ src/IndyPOS.StoreHub/Program.cs
git commit -m "feat(storehub): capability-gated admin endpoints for payment methods"
```

---

### Task 8: WinForms HTTP client — offerable + admin methods

**Files:**
- Modify: `src/IndyPOS.Application/Abstractions/StoreHub/IStoreHubClient.cs`
- Modify: `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubHttpClient.cs`

**Interfaces:**
- Produces on `IStoreHubClient`: `Task<IReadOnlyList<PaymentMethodDto>> GetOfferablePaymentMethodsAsync(CancellationToken)`, `Task<IReadOnlyList<PaymentMethodDto>> GetAllPaymentMethodsAsync(CancellationToken)`, `Task AddCampaignPaymentMethodAsync(string code, string displayName, int displayOrder, CancellationToken)`, `Task SetPaymentMethodEnabledAsync(string code, bool enabled, CancellationToken)`, `Task UpdatePaymentMethodDisplayAsync(string code, string displayName, int displayOrder, CancellationToken)`.

- [ ] **Step 1: Add the interface members**

Add the five signatures above to `IStoreHubClient` (there is no admin GET-all endpoint yet for offerable-only; the admin screen needs all rows — add a `GET /admin/payment-methods` in Task 7 if the admin screen must show disabled rows. If so, add that endpoint + `CanManagePaymentMethods` gate there too, and `GetAllPaymentMethodsAsync` calls it).

> Adjustment to Task 7 if not already done: also `app.MapGet("/admin/payment-methods", ... GetAllPaymentMethodsQuery ...).RequireAuthorization("CanManagePaymentMethods")` returning ALL rows (enabled + disabled) for the admin screen. Add the `GetAllPaymentMethodsQuery` handler in Task 4's set.

- [ ] **Step 2: Implement in `StoreHubHttpClient`**

Mirror the existing `GetProductsAsync` (authenticated GET) and a POST helper. Example:

```csharp
    public Task<IReadOnlyList<PaymentMethodDto>> GetOfferablePaymentMethodsAsync(CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync<IReadOnlyList<PaymentMethodDto>>(HttpMethod.Get, "/payment-methods", content: null, cancellationToken);

    public Task<IReadOnlyList<PaymentMethodDto>> GetAllPaymentMethodsAsync(CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync<IReadOnlyList<PaymentMethodDto>>(HttpMethod.Get, "/admin/payment-methods", content: null, cancellationToken);
```
Implement the three admin mutators via the existing authenticated POST/PATCH helper pattern (`PostAsJsonAsync`/a PATCH equivalent) against `/admin/payment-methods` and `/admin/payment-methods/{code}`.

- [ ] **Step 3: Build to verify**

Run: `dotnet build -c Release`
Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 4: Commit**

```bash
git add src/IndyPOS.Application/Abstractions/StoreHub/IStoreHubClient.cs src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubHttpClient.cs
git commit -m "feat(client): StoreHub client methods for payment-method catalog"
```

---

### Task 9: WinForms — catalog-driven payment buttons in `AcceptPaymentForm`

**Files:**
- Modify: `src/IndyPOS.Windows.Forms/UI/Payment/AcceptPaymentForm.cs`
- Modify: `src/IndyPOS.Application/Common/Interfaces/ISaleService.cs` + its impl (add a `string`-code `AddPayment` overload)

**Interfaces:**
- Consumes: `IStoreHubClient.GetOfferablePaymentMethodsAsync`, `PaymentMethodDto`.
- Produces: `ISaleService.AddPayment(string methodCode, decimal paymentAmount, string note)`.

- [ ] **Step 1: Add the string-code `AddPayment` overload**

In `ISaleService`, add:

```csharp
    void AddPayment(string methodCode, decimal paymentAmount, string note);
```
Implement it in the concrete `SaleService` to record the payment with `Method = methodCode` (mirror the existing `AddPayment(PaymentType, ...)` impl, but store the code string directly instead of mapping the enum). Keep the enum overload temporarily for any other callers; mark it `[Obsolete("Use AddPayment(string methodCode, ...)")]`.

- [ ] **Step 2: Inject the client + load offerable methods on open**

In `AcceptPaymentForm`, add an `IStoreHubClient` constructor dependency (it's already DI-constructed). On form load, call `GetOfferablePaymentMethodsAsync()` and render one `Button` per returned `PaymentMethodDto`, ordered by `DisplayOrder`, `Text = DisplayName`, `Tag = Code`. Remove the hardcoded per-`PaymentType` buttons + their designer `Click` handlers. Each dynamic button's click calls `ChangePaymentType(code)` (a new string-based version).

- [ ] **Step 3: Replace `ChangePaymentType(PaymentType)` with a code-based version**

```csharp
    private string _selectedMethodCode = "";
    private void ChangePaymentType(string methodCode)
    {
        _selectedMethodCode = methodCode;
        _isPaymentTypeSelected = true;
        PaymentTypeLabel.Text = /* the DisplayName for methodCode from the loaded list */;
        var isRefundInvoice = _saleService.IsRefundInvoice();
        var isPayLater = string.Equals(methodCode, "PayLater", StringComparison.OrdinalIgnoreCase);
        AcceptPaymentButton.Visible = !isPayLater && !isRefundInvoice;
        AcceptPayLaterPaymentButton.Visible = isPayLater && !isRefundInvoice;
    }
```
And in the accept handlers, call `_saleService.AddPayment(_selectedMethodCode, amount, note)`.

- [ ] **Step 4: Build to verify (WinForms)**

Run: `dotnet build src/IndyPOS.Windows.Forms/IndyPOS.Windows.Forms.csproj -c Release`
Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 5: Add/adjust WinForms tests if present**

If `tests/IndyPOS.Windows.Forms.Tests` covers `AcceptPaymentForm`/`SaleService`, update them to the string-code `AddPayment`. Run: `dotnet test tests/IndyPOS.Windows.Forms.Tests/IndyPOS.Windows.Forms.Tests.csproj`. Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/IndyPOS.Windows.Forms/UI/Payment/AcceptPaymentForm.cs src/IndyPOS.Application/Common/Interfaces/ISaleService.cs src/IndyPOS.Application/**/SaleService*.cs
git commit -m "feat(winforms): render payment buttons from offerable catalog"
```

---

### Task 10: WinForms — `PaymentMethodsSettingsPanel` admin screen

**Files:**
- Create: `src/IndyPOS.Windows.Forms/UI/Settings/PaymentMethodsSettingsPanel.cs` (+ designer if the project uses designer files)
- Modify: the settings host (wherever settings panels are registered/shown) to add the new panel.

**Interfaces:**
- Consumes: `IStoreHubClient.GetAllPaymentMethodsAsync`, `SetPaymentMethodEnabledAsync`, `AddCampaignPaymentMethodAsync`, `UpdatePaymentMethodDisplayAsync`.

- [ ] **Step 1: Build the panel**

Create a panel (mirror an existing settings panel's structure/DI, e.g. `UsersPanel`) with:
- A grid/list of all methods (Code, DisplayName, Kind, IsEnabled, DisplayOrder) via `GetAllPaymentMethodsAsync`.
- An enable/disable toggle per row → `SetPaymentMethodEnabledAsync(code, enabled)`.
- An "Add campaign" form (Code, DisplayName, DisplayOrder) → `AddCampaignPaymentMethodAsync(...)`.
- Editable DisplayName/DisplayOrder → `UpdatePaymentMethodDisplayAsync(...)`.
- Code/Kind read-only; PayLater's store-type restriction is not represented here (it's a server invariant).

- [ ] **Step 2: Register/route the panel**

Add it to the settings navigation the same way other panels are registered (follow the existing pattern in the settings host form).

- [ ] **Step 3: Build to verify**

Run: `dotnet build src/IndyPOS.Windows.Forms/IndyPOS.Windows.Forms.csproj -c Release`
Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 4: Commit**

```bash
git add src/IndyPOS.Windows.Forms/UI/Settings/PaymentMethodsSettingsPanel.cs src/IndyPOS.Windows.Forms/**/*Settings*.cs
git commit -m "feat(winforms): admin screen to manage payment methods (toggle/add/edit)"
```

---

### Task 11: Installer — StoreType picker writes `Store:Type`

**Files:**
- Modify: `installer/IndyPOS.Bootstrapper/Installers/InstallationConfig.cs`
- Modify: `installer/IndyPOS.Bootstrapper/UI/InstallationWizard.cs`
- Modify: `installer/IndyPOS.Bootstrapper/Installers/DatabaseSetup.cs`
- Test: `tests/IndyPOS.Bootstrapper.Tests/Installers/DatabaseSetupStoreTypeTests.cs` (if `BuildStoreHubConfigJson` is testable in isolation) OR an `InstallationConfig` default test.

**Interfaces:**
- Produces: `InstallationConfig.StoreType` (`IndyPOS.Domain.Enums.StoreType`).

- [ ] **Step 1: Add `StoreType` to `InstallationConfig`**

```csharp
    public IndyPOS.Domain.Enums.StoreType StoreType { get; init; } = IndyPOS.Domain.Enums.StoreType.GeneralHardware;
```
(Confirm the bootstrapper already references `IndyPOS.Domain`; if not, add the project reference.)

- [ ] **Step 2: Add the wizard picker**

In `InstallationWizard.CreateConfigPanel()`, mirror the Store ID `TextBox` block to add a `ComboBox` labeled "Store Type:" bound to `Enum.GetValues<StoreType>()`, defaulting to `GeneralHardware`. Declare a `private ComboBox _storeTypeComboBox = null!;` field. In `StartButton_Click`, read the selected value and include it in the `new InstallationConfig { StoreId = ..., StoreType = (StoreType)_storeTypeComboBox.SelectedItem }`.

- [ ] **Step 3: Write `Store:Type` in `DatabaseSetup`**

In `DatabaseSetup.BuildStoreHubConfigJson`, change the `Store` anonymous object:

```csharp
                Store = new
                {
                    Id = config.StoreId,
                    Type = config.StoreType.ToString()
                },
```
(`StoreIdentityOptions.Type` binds the enum from its string name — `ToString()` is correct.)

- [ ] **Step 4: Write the failing test (StoreType default + serialization)**

If `BuildStoreHubConfigJson` can be exercised, assert the produced JSON contains `"type": "Minimart"` for a `Minimart` config. Otherwise, assert `new InstallationConfig { StoreId = "x" }.StoreType == StoreType.GeneralHardware`. Run it to fail first (before Step 1–3 if ordering allows), then pass.

- [ ] **Step 5: Build the bootstrapper**

Run: `dotnet build installer/IndyPOS.Bootstrapper/IndyPOS.Bootstrapper.csproj -c Release`
Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 6: Commit**

```bash
git add installer/IndyPOS.Bootstrapper/ tests/IndyPOS.Bootstrapper.Tests/
git commit -m "feat(installer): store-type picker writes Store:Type (fixes silent-GeneralHardware default)"
```

---

### Task 12: Data migration — map existing payments + full verification

**Files:**
- Create: `src/IndyPOS.Infrastructure/Persistence/StoreHub/Migrations/<timestamp>_MapLegacyPaymentValuesToCodes.cs` (only if existing stored values differ from the Codes)
- No other source changes — verification-heavy.

- [ ] **Step 1: Confirm current stored payment values**

Inspect how `SaleService.AddPayment(PaymentType, ...)` currently persists `Payment.Method` (string). Determine whether existing rows already store `"Cash"`/`"PayLater"`/etc. (matching Codes) or store the enum's int/other text.
Run (against a dev/copy DB, read-only): `SELECT DISTINCT method FROM payment;`
Record the distinct values.

- [ ] **Step 2: If values already match Codes → no data migration needed**

If distinct values are already `Cash`, `PayLater`, `MoneyTransfer`, `WelfareCard`, etc., document that no mapping migration is required and skip to Step 4.

- [ ] **Step 3: If values differ → write a data migration**

Generate an empty migration and write `Up` SQL mapping each legacy value → its `Code` (e.g. `UPDATE payment SET method = 'MoneyTransfer' WHERE method = '5';`). `Down` reverses it. Keep it idempotent-safe.
```bash
dotnet ef migrations add MapLegacyPaymentValuesToCodes --project src/IndyPOS.Infrastructure --startup-project src/IndyPOS.StoreHub --context StoreHubDbContext --output-dir Persistence/StoreHub/Migrations
```

- [ ] **Step 4: Full solution build + all affected test suites**

Run: `dotnet build -c Release` (0 errors)
Run: `dotnet test tests/IndyPOS.Domain.Tests/IndyPOS.Domain.Tests.csproj` (all pass)
Run: `dotnet test tests/IndyPOS.Application.Tests/IndyPOS.Application.Tests.csproj` (all pass)
Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests/IndyPOS.Bootstrapper.Tests.csproj` (all pass)

- [ ] **Step 5: Manual/dev end-to-end smoke (Aspire)**

Run StoreHub via Aspire (`dotnet run --project src/IndyPOS.AppHost --launch-profile http`), confirm: fresh DB seeds 7 methods; `GET /payment-methods` on a GeneralHardware store returns Cash/MoneyTransfer/WelfareCard/PayLater (enabled), excludes disabled campaigns; a Minimart store (set `Store:Type=Minimart` in config) excludes PayLater. Complete a sale with Cash (accepted) and attempt PayLater on Minimart (rejected).

- [ ] **Step 6: Commit**

```bash
git add src/IndyPOS.Infrastructure/Persistence/StoreHub/Migrations/
git commit -m "chore(payments): map legacy payment values to catalog codes + verify"
```

---

## Notes for the implementer

- The Domain layer must not reference Application — `PaymentMethodPolicy` keeps its own `PayLaterCode` const (Task 1 Step 8).
- StoreHub registers Nokpirab handlers **individually** — every new handler needs an `AddTransient<...>` line (Tasks 4/6/7).
- Endpoint auth is capability-based — new admin endpoints need a `Capability` + `RoleCapabilities` grant + named policy, never a role-name check (Task 7).
- WinForms and StoreHub are separate processes; the POS learns store-type gating only through `GET /payment-methods` (Task 9) — do not try to share `IStoreIdentityService` across them.
- `DateTime.UtcNow` is fine in this application code (unlike workflow scripts).

## Self-Review (completed by author)

- **Spec coverage:** catalog model (T1–T3), retire enum + Code storage (T3 constants, T9 string overload, T12 migration), offerable rule single-source (T1 policy + T4 service, consumed by T6 + T5), PayLater invariant (T1), server enforcement (T5), catalog-driven UI (T9), admin toggle/add/edit screen (T7 endpoints + T10 UI), make-StoreType-real (T11), Domain.Tests (T1), tests (throughout). All spec §1–§14 requirements map to a task.
- **Placeholder scan:** the two intentional "fill from existing arrange block" spots (T5 Step 1 command literals) and the "confirm stored values" (T12 Step 1) are genuine implementation-time lookups against real code, not vague hand-waves — each names exactly what to read and where. The `/*Resp*/` markers in T7 are deliberate: the exact command response type is defined in T4 against the codebase's convention and must be used verbatim.
- **Type consistency:** `PaymentMethod`, `PaymentMethodKind`, `PaymentMethodPolicy.Offerable`, `IPaymentMethodRepository` (GetAllAsync/GetByCodeAsync/AddAsync/UpdateAsync), `IPaymentMethodCatalogService` (GetOfferableAsync/GetAllAsync/AddCampaignAsync/SetEnabledAsync/UpdateDisplayAsync), `PaymentMethodDto`, and `PaymentMethodCodes` names are used consistently across tasks.
