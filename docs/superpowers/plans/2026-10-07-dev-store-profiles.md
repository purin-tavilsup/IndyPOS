# Dev Store Profiles Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** One command runs StoreHub and the till as GeneralHardware, MimyMart or MimyShop (or all three side by side, syncing to one CloudApi), each with its own database, products, payment methods and receipt header; and the key flows are tested once per store type in CI.

**Architecture:**
- **One catalogue.** A new `IndyPOS.StoreProfiles` project (`net10.0`, Domain only) holds the three profiles. It is read by:
  - the AppHost (`--store`);
  - StoreHub's Development seeder (products and initial stock);
  - CloudApi's Development seeder (cloud credentials);
  - the tests (one theory row per profile).
- **Development applies migrations instead of `EnsureCreated`** (StoreHub and CloudApi), on fresh database names.
- **StoreHub's secrets directory becomes configurable,** so a dev store never reads the installed store's secrets.

**Tech Stack:** .NET 10, Aspire.Hosting 9.3.0, EF Core + Npgsql, xUnit, FluentAssertions 8, `TestPostgres` seam.

**Spec:** `docs/superpowers/specs/2026-10-07-dev-store-profiles-design.md` (approved by Pond 2026-10-07).

## Prerequisites

- **PR #116 (health-check convention) is merged.** This plan builds on its AppHost (`WithHttpHealthCheck`, StoreHub with no `WaitFor(cloudApi)`) and its CloudApi `WebApplicationFactory` test host. Rebase `feat/dev-store-profiles` on `development` after #116 merges.
- **Docker running**, or `INDYPOS_TEST_POSTGRES` set.
- Line numbers are approximate. Find each block by the quoted text.

## Decisions this plan makes (flag them in the PR)

1. **The cloud client id is `store_{StoreId}`, not the bare `StoreId`** (spec §4.3 said "client id = the profile's StoreId"). `RegisterStoreHandler` already uses `store_{StoreId}`. The dev seeder must follow it, or the cloud would hold two conventions. It is exposed as `StoreProfile.CloudClientId`.
2. **StoreHub's secrets directory becomes configurable (`Secrets:Directory`).** This is not in the spec.
   - `SecureCloudTokenOptions` reads the DPAPI secret store **before** config.
   - The store lives in `%ProgramData%\IndyPOS\Secrets`, with `LocalMachine` scope. That is the same folder an installed StoreHub uses.
   - So a dev StoreHub on a machine with an installed till (Pond's) would pick up the installed store's real cloud secret. Three dev stores would all read one secret.
   - The default is unchanged. The AppHost gives each dev store its own directory.
3. **AppHost StoreHub resources skip launch profiles and declare an explicit `http` endpoint** on the profile's `DevPort`, with `ASPNETCORE_ENVIRONMENT=Development` set. Three instances of one project would otherwise all claim the launch profile's `https` port 7150.
4. **The "no profile matches" fallback is the default profile.** Every `StoreType` has a profile (a test pins it), so the spec's generic 5-product fallback can never be reached. `StoreProfiles.ForType` falls back to `Default` instead.

## Global Constraints

- [§2] **Development and tests only:** no production behaviour change. Production still migrates through the installer's `migrate` step and registers stores through `/admin/stores/register`.
- [§2] **StoreHub never waits for CloudApi** (offline-first).
- [§3] **MimyShop's service barcodes are exactly `2002500000014` (จัดส่ง) and `2002500000021` (เอกสาร).**
- [§3] **GeneralHardware keeps port 5012** and is the default store.
- [§4.2] **The developer's real `C:\ProgramData\IndyPOS\Config\StoreConfiguration.json` is never read or written** by the AppHost.
- [§4.3] **The dev cloud seeder runs only in Development.**
- [§6] **Test expectations are written in the tests, never read from the code under test.**
- **Test style:**
  - names are `Subject_WhenScenario_DirectVerbOutcome`;
  - negative cases first;
  - Arrange/Act/Assert separated by blank lines;
  - FluentAssertions chains on separate lines with the dots aligned.
- **Commits:**
  - conventional, at least one per task;
  - end each message with the session's `Claude-Session:` line.

## Review Focus

These are the inputs the spec implies but no other task's tests exercise, most likely first. Each has its test in the task named:

1. **A machine with an installed StoreHub** (secrets in `%ProgramData%\IndyPOS\Secrets`). Expected: a dev store reads only its own secrets directory. → Task 2 (`AddStoreHubServices_WithASecretsDirectory_StoresSecretsThere`), plus a manual check in Task 6.
2. **`--store` typed loosely** (`mimymart`, `" MimyShop "`, `ALL`). Expected: it matches without regard to case or surrounding spaces. An unknown key fails with the list of valid keys. → Task 1 (`Resolve_*` tests).
3. **Aspire restarted on an existing per-store database.** Expected: re-seeding adds no duplicate products and **no second initial-stock movement**. → Task 3 (`DevSeed_RunTwice_AddsInitialStockOnce`).
4. **Selling a MimyShop service product** (non-trackable). Expected: the sale completes and moves no stock. → Task 4 (`ServiceSale_OnMimyShop_MovesNoStock`).
5. **The cloud dev seeder run twice, or outside Development.** Expected: each store is registered once, and nothing at all outside Development. → Task 5.

---

## File Structure

```
src/IndyPOS.StoreProfiles/
  IndyPOS.StoreProfiles.csproj      CREATE  net10.0, references IndyPOS.Domain
  StoreProfile.cs                   CREATE  StoreProfile + StoreProfileProduct records
  StoreProfiles.cs                  CREATE  catalogue: All, Default, Find, ForType, Resolve, DevCloudClientSecret
src/IndyPOS.Infrastructure/
  IndyPOS.Infrastructure.csproj     MODIFY  + ProjectReference StoreProfiles
  ConfigureServices.cs              MODIFY  Secrets:Directory
  Persistence/StoreHub/Seeders/DevelopmentDataSeeder.cs  MODIFY  profile products + initial stock
src/IndyPOS.StoreHub/Program.cs     MODIFY  Development: migrate, not EnsureCreated
src/IndyPOS.CloudApi/
  IndyPOS.CloudApi.csproj           MODIFY  + ProjectReference StoreProfiles
  Infrastructure/Auth/DevStoreRegistration.cs  CREATE
  Program.cs                        MODIFY  Development: migrate + DevStoreRegistration
src/IndyPOS.AppHost/
  IndyPOS.AppHost.csproj            MODIFY  + ProjectReference StoreProfiles (IsAspireProjectResource=false)
  Program.cs                        MODIFY  --store, per-profile resources
  DevStoreFiles.cs                  CREATE  writes StoreConfiguration.json, secrets dir per profile
tests/IndyPOS.Application.Tests/StoreProfiles/StoreProfilesTests.cs  CREATE
tests/IndyPOS.Application.Tests/ConfigureServicesSecretsTests.cs    CREATE
tests/IndyPOS.StoreHub.IntegrationTests/
  StoreHubWebApplicationFactory.cs  MODIFY  identity hook; TestStoreIdentityService parameters
  StoreProfiles/StoreProfileHosts.cs            CREATE  collection fixture, one host per profile
  StoreProfiles/DevSeedTests.cs                 CREATE
  StoreProfiles/StoreTypeFlowTests.cs           CREATE
tests/IndyPOS.CloudApi.IntegrationTests/DevStoreRegistrationTests.cs  CREATE
IndyPOS.sln                         MODIFY  add StoreProfiles under Core
ONBOARDING.md, CLAUDE.md            MODIFY
```

---

### Task 1: The store-profile catalogue

**Files:**
- Create: `src/IndyPOS.StoreProfiles/{IndyPOS.StoreProfiles.csproj, StoreProfile.cs, StoreProfiles.cs}`
- Create: `tests/IndyPOS.Application.Tests/StoreProfiles/StoreProfilesTests.cs`
- Modify: `tests/IndyPOS.Application.Tests/IndyPOS.Application.Tests.csproj` (ProjectReference), `IndyPOS.sln`

**Interfaces (Produces):**
- `record StoreProfile(string Key, StoreType Type, string StoreId, string Name, string FullName, string AddressLine1, string AddressLine2, string Phone, int Code, int DevPort, IReadOnlyList<StoreProfileProduct> Products)`, with `string CloudClientId => $"store_{StoreId}"` and `string DatabaseName => $"storehub-{Key.ToLowerInvariant()}"`.
- `record StoreProfileProduct(string Barcode, string Name, string Category, decimal UnitPrice, int InitialStock, bool IsTrackable = true)`.
- `static class StoreProfiles` with:
  - `All` (`IReadOnlyList<StoreProfile>`);
  - `Default`;
  - `Find(string? key)` → `StoreProfile?`;
  - `ForType(StoreType)` → `StoreProfile`, falling back to `Default`;
  - `Resolve(string? selection)` → `IReadOnlyList<StoreProfile>`, which throws `ArgumentException`;
  - `const string AllKey = "all"`;
  - `const string DevCloudClientSecret`.

- [ ] **Step 1: Project**

`src/IndyPOS.StoreProfiles/IndyPOS.StoreProfiles.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\IndyPOS.Domain\IndyPOS.Domain.csproj" />
  </ItemGroup>

</Project>
```

Add it to the solution under the existing **Core** folder (`{A1B2C3D4-1111-1111-1111-000000000001}`):

`dotnet sln IndyPOS.sln add src/IndyPOS.StoreProfiles/IndyPOS.StoreProfiles.csproj --solution-folder Core`

Then check `git diff IndyPOS.sln` nests it under `...0001`. Add `<ProjectReference Include="..\..\src\IndyPOS.StoreProfiles\IndyPOS.StoreProfiles.csproj" />` to `IndyPOS.Application.Tests.csproj`.

- [ ] **Step 2: Write the failing tests**

```csharp
using FluentAssertions;
using IndyPOS.Domain.Enums;
using IndyPOS.Infrastructure.Persistence.StoreHub.Seeders;
using Xunit;
using Profiles = IndyPOS.StoreProfiles.StoreProfiles;

namespace IndyPOS.Application.Tests.StoreProfiles;

public class StoreProfilesTests
{
    private const string UnknownKey = "NoSuchStore";

    [Fact]
    public void Resolve_WithAnUnknownKey_ThrowsListingTheValidKeys()
    {
        var act = () => Profiles.Resolve(UnknownKey);

        act.Should()
           .Throw<ArgumentException>()
           .WithMessage("*GeneralHardware*MimyMart*MimyShop*all*");
    }

    [Fact]
    public void Find_WithAnUnknownKey_ReturnsNull()
    {
        Profiles.Find(UnknownKey).Should()
                                 .BeNull();
    }

    [Theory]
    [InlineData("mimymart")]
    [InlineData(" MimyMart ")]
    [InlineData("MIMYMART")]
    public void Resolve_WithAKeyInAnyCase_ReturnsThatStore(string selection)
    {
        Profiles.Resolve(selection).Select(p => p.Key).Should()
                                                      .Equal("MimyMart");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Resolve_WithNoSelection_ReturnsGeneralHardware(string? selection)
    {
        Profiles.Resolve(selection).Select(p => p.Key).Should()
                                                      .Equal("GeneralHardware");
    }

    [Fact]
    public void Resolve_WithAll_ReturnsEveryStore()
    {
        Profiles.Resolve("ALL").Select(p => p.Key).Should()
                                                  .Equal("GeneralHardware", "MimyMart", "MimyShop");
    }

    [Fact]
    public void All_ForEachStoreType_HasExactlyOneProfile()
    {
        Profiles.All.Select(p => p.Type).Should()
                                         .BeEquivalentTo(Enum.GetValues<StoreType>());
    }

    [Fact]
    public void All_WithTheirDevPorts_UsesEachPortOnceAndKeeps5012ForGeneralHardware()
    {
        Profiles.All.Select(p => p.DevPort).Should()
                                            .OnlyHaveUniqueItems();
        Profiles.Default.DevPort.Should()
                                .Be(5012);
    }

    [Fact]
    public void MimyShop_ServiceProducts_UseTheRealBarcodesAndMoveNoStock()
    {
        var services = Profiles.Find("MimyShop")!.Products.Where(p => !p.IsTrackable);

        services.Select(p => p.Barcode).Should()
                                       .BeEquivalentTo(["2002500000014", "2002500000021"]);
    }

    // A product filed under a code the store type does not seed is invisible to every picker.
    [Theory]
    [InlineData("GeneralHardware")]
    [InlineData("MimyMart")]
    [InlineData("MimyShop")]
    public void Products_ForEachStore_UseOnlyCategoriesThatStoreTypeSeeds(string key)
    {
        var profile = Profiles.Find(key)!;

        profile.Products.Select(p => p.Category).Should()
                                                .BeSubsetOf(ProductCategorySeeder.CodesFor(profile.Type));
    }
}
```

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~StoreProfilesTests"`
Expected: build error, `StoreProfiles` does not exist.

- [ ] **Step 3: Implement**

`StoreProfile.cs`:

```csharp
using IndyPOS.Domain.Enums;

namespace IndyPOS.StoreProfiles;

/// <summary>
/// One store a developer can run or test as. Development and tests only: production stores are
/// configured by the installer, never by a profile.
/// </summary>
public sealed record StoreProfile(
    string Key,
    StoreType Type,
    string StoreId,
    string Name,
    string FullName,
    string AddressLine1,
    string AddressLine2,
    string Phone,
    int Code,
    int DevPort,
    IReadOnlyList<StoreProfileProduct> Products)
{
    /// <summary>The cloud client id; CloudApi names every store's client this way.</summary>
    public string CloudClientId => $"store_{StoreId}";

    public string DatabaseName => $"storehub-{Key.ToLowerInvariant()}";
}

/// <summary>A seed product. A non-trackable one (a service) is sold without moving stock.</summary>
public sealed record StoreProfileProduct(
    string Barcode,
    string Name,
    string Category,
    decimal UnitPrice,
    int InitialStock,
    bool IsTrackable = true);
```

`StoreProfiles.cs`. Category codes are written as strings, because this project cannot reference Application. The Task 1 test checks them against `ProductCategorySeeder.CodesFor`.

```csharp
using IndyPOS.Domain.Enums;

namespace IndyPOS.StoreProfiles;

public static class StoreProfiles
{
    public const string AllKey = "all";

    /// <summary>
    /// The secret every dev store uses to sign in to the dev cloud. Registered only by CloudApi's
    /// Development seeder; production stores get a random one from /admin/stores/register.
    /// </summary>
    public const string DevCloudClientSecret = "dev-store-secret-not-for-production";

    public static StoreProfile GeneralHardware { get; } = new(
        "GeneralHardware", StoreType.GeneralHardware, "DEV-GENERALHARDWARE",
        "ร้านวัสดุ (Dev)", "ร้านวัสดุก่อสร้าง ทดสอบ", "123 ถนนทดสอบ", "เมือง 10000", "000-000-0001",
        Code: 1, DevPort: 5012,
        Products:
        [
            new("8850100000011", "ปูนซีเมนต์ 50 กก.", "ConstructionMaterials", 145m, 40),
            new("8850100000028", "ท่อ PVC 1/2 นิ้ว 4 ม.", "PlumbingMaterials", 65m, 60),
            new("8850100000035", "สายไฟ VAF 2x1.5 (เมตร)", "ElectricalMaterials", 18m, 300),
            new("8850100000042", "ตะปู 2 นิ้ว (กก.)", "GeneralMaterials", 55m, 50),
            new("8850100000059", "ปุ๋ยยูเรีย 50 กก.", "Agriculture", 890m, 20),
            new("8850000000001", "น้ำดื่ม 600ml", "Beverages", 7m, 100)
        ]);

    public static StoreProfile MimyMart { get; } = new(
        "MimyMart", StoreType.Minimart, "DEV-MIMYMART",
        "มินิมาร์ท (Dev)", "มินิมาร์ท ทดสอบ", "456 ถนนทดสอบ", "เมือง 10000", "000-000-0002",
        Code: 2, DevPort: 5013,
        Products:
        [
            new("8850000000001", "น้ำดื่ม 600ml", "Beverages", 7m, 120),
            new("8850000000002", "โค้ก 325ml", "Beverages", 15m, 80),
            new("8850000000003", "มาม่าหมูสับ", "Food", 6m, 200),
            new("8850000000004", "ขนมปังปี๊บ", "Snacks", 20m, 30),
            new("8850000000005", "นมจืด 200ml", "Beverages", 12m, 60),
            new("8850200000016", "ผงซักฟอก 800 ก.", "Household", 45m, 25)
        ]);

    public static StoreProfile MimyShop { get; } = new(
        "MimyShop", StoreType.MimyShop, "DEV-MIMYSHOP",
        "มิมี่ช็อป (Dev)", "มิมี่ช็อป ทดสอบ", "789 ถนนทดสอบ", "เมือง 10000", "000-000-0003",
        Code: 3, DevPort: 5014,
        Products:
        [
            new("2002500000014", "จัดส่ง", "Services", 30m, 0, IsTrackable: false),
            new("2002500000021", "เอกสาร", "Services", 10m, 0, IsTrackable: false),
            new("8850300000013", "สมุดปกอ่อน", "BooksAndNotebooks", 20m, 50),
            new("8850300000020", "ปากกาลูกลื่น", "Stationery", 10m, 100),
            new("8850300000037", "ตุ๊กตาหมี", "Toys", 159m, 12),
            new("8850300000044", "สายชาร์จ USB-C", "MobileAccessories", 99m, 30)
        ]);

    public static IReadOnlyList<StoreProfile> All { get; } = [GeneralHardware, MimyMart, MimyShop];

    public static StoreProfile Default => GeneralHardware;

    public static StoreProfile? Find(string? key) =>
        All.FirstOrDefault(p => string.Equals(p.Key, key?.Trim(), StringComparison.OrdinalIgnoreCase));

    public static StoreProfile ForType(StoreType type) => All.FirstOrDefault(p => p.Type == type) ?? Default;

    /// <summary>The stores a dev run starts: none chosen = the default, "all" = every store.</summary>
    public static IReadOnlyList<StoreProfile> Resolve(string? selection)
    {
        if (string.IsNullOrWhiteSpace(selection))
            return [Default];

        if (string.Equals(selection.Trim(), AllKey, StringComparison.OrdinalIgnoreCase))
            return All;

        return Find(selection) is { } profile
            ? [profile]
            : throw new ArgumentException(
                $"Unknown store '{selection.Trim()}'. Use one of: {string.Join(", ", All.Select(p => p.Key))}, {AllKey}.",
                nameof(selection));
    }
}
```

- [ ] **Step 4: Run and pass**

Run the same filter → 15 PASS (12 tests, three of them theories).

- [ ] **Step 5: Commit**

`git commit -m "feat(dev): store-profile catalogue for GeneralHardware, MimyMart and MimyShop"`, with the session line.

---

### Task 2: StoreHub's secrets directory is configurable

**Files:**
- Modify: `src/IndyPOS.Infrastructure/ConfigureServices.cs` (`AddStoreHubServices`, lines 86-94)
- Create: `tests/IndyPOS.Application.Tests/ConfigureServicesSecretsTests.cs`

**Interfaces (Produces):** config key `Secrets:Directory`, defaulting to `%ProgramData%\IndyPOS\Secrets`.

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IndyPOS.Application.Tests;

// A dev store must never read the installed store's secrets in %ProgramData%\IndyPOS\Secrets.
public class ConfigureServicesSecretsTests
{
    private const string SecretKey = "CloudApi:ClientSecret";

    [Fact]
    public async Task AddStoreHubServices_WithASecretsDirectory_StoresSecretsThere()
    {
        var directory = Directory.CreateTempSubdirectory("indypos-secrets-").FullName;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Secrets:Directory"] = directory })
            .Build();
        var services = new ServiceCollection().AddLogging()
                                              .AddStoreHubServices(configuration);
        await using var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<ISecretStorage>().SetSecretAsync(SecretKey, "value");

        Directory.EnumerateFiles(directory).Should()
                                           .NotBeEmpty();
    }
}
```

Find `ISecretStorage`'s namespace with `git grep -n "interface ISecretStorage" -- src` and fix the `using` to match. If `AddStoreHubServices` needs registrations the test does not give, add only what the error names (e.g. `IConfiguration`).

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~ConfigureServicesSecretsTests"` → FAIL. The file goes to ProgramData, and the temp directory stays empty.

- [ ] **Step 2: Implement**

Replace:

```csharp
		// Stores encrypted secrets in %ProgramData%\IndyPOS\Secrets
		var secretsDirectory = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
			"IndyPOS", "Secrets");
```

with:

```csharp
		// Stores encrypted secrets in %ProgramData%\IndyPOS\Secrets. Secrets:Directory overrides it so a
		// dev store never reads an installed StoreHub's secrets on the same machine (they are read
		// before config).
		var secretsDirectory = configuration["Secrets:Directory"]
			?? Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
				"IndyPOS", "Secrets");
```

The file is tab-indented. `configuration` is the method's existing parameter; check its name.

- [ ] **Step 3: Pass, then commit**

Run the filter → PASS. Run `dotnet test tests/IndyPOS.Application.Tests` → all pass.

`git commit -m "feat(storehub): Secrets:Directory overrides where StoreHub keeps its DPAPI secrets"`, with the session line.

---

### Task 3: Profile products seeded in Development; Development migrates

**Files:**
- Modify: `src/IndyPOS.Infrastructure/IndyPOS.Infrastructure.csproj` (+ ProjectReference to StoreProfiles)
- Modify: `src/IndyPOS.Infrastructure/Persistence/StoreHub/Seeders/DevelopmentDataSeeder.cs` (constructor + `SeedProductsAsync`)
- Modify: `src/IndyPOS.StoreHub/Program.cs` (the `if (app.Environment.IsDevelopment())` block, about line 206)
- Modify: `src/IndyPOS.Infrastructure/Persistence/StoreHub/StoreHubDbContextExtensions.cs` (doc comment of `MigrateStoreHubDatabaseAsync`)
- Modify: `tests/IndyPOS.StoreHub.IntegrationTests/StoreHubWebApplicationFactory.cs`
- Create: `tests/IndyPOS.StoreHub.IntegrationTests/StoreProfiles/StoreProfileHosts.cs`, `DevSeedTests.cs`

**Interfaces:**
- Produces:
  - `StoreHubWebApplicationFactory.CreateStoreIdentity()` (`protected virtual`);
  - `TestStoreIdentityService(string storeId = "test-store", StoreType type = StoreType.GeneralHardware)`;
  - `StoreProfileHosts` (collection `"StoreProfiles"`), with:
    - `Task<HttpClient> SignedInAsync(string key, string username = "cashier", string password = "cashier123")`;
    - `StoreHubDbContext DbFor(string key)` (a new scope's context);
    - `IServiceProvider ServicesFor(string key)`.
- Consumed by Task 4.

- [ ] **Step 1: Make the test identity configurable**

In `StoreHubWebApplicationFactory.cs`, replace `services.AddSingleton<IStoreIdentityService>(new TestStoreIdentityService());` with `services.AddSingleton(CreateStoreIdentity());`, and add to the class:

```csharp
    /// <summary>The store the host runs as. Override to boot StoreHub as a dev store profile.</summary>
    protected virtual IStoreIdentityService CreateStoreIdentity() => new TestStoreIdentityService();
```

Make `TestStoreIdentityService` take its values:

```csharp
internal class TestStoreIdentityService(string storeId = TestStoreIdentityService.TestStoreId,
                                        StoreType type = StoreType.GeneralHardware) : IStoreIdentityService
{
    public const string TestStoreId = "test-store";

    public string StoreId => storeId;
    public string StoreName => "Test Store";
    public StoreType StoreType => type;
    public StoreTypeFeatures Features => StoreTypeFeatures.For(type);
    public TimeZoneInfo TimeZone => TimeZoneInfo.Local;
    [Obsolete("Use StoreId (UUID) for identification.")]
    public int StoreCode => 1;
    public void EnsureConfigured() { }
}
```

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests` → still all pass (default values are unchanged).

- [ ] **Step 2: The per-profile hosts fixture**

`StoreProfiles/StoreProfileHosts.cs`:

```csharp
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.Auth;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using IndyPOS.Infrastructure.Persistence.StoreHub.Seeders;
using IndyPOS.StoreProfiles;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Profiles = IndyPOS.StoreProfiles.StoreProfiles;

namespace IndyPOS.StoreHub.IntegrationTests.StoreProfiles;

[CollectionDefinition(Name)]
public sealed class StoreProfilesCollection : ICollectionFixture<StoreProfileHosts>
{
    public const string Name = "StoreProfiles";
}

/// <summary>
/// One StoreHub per dev store profile, each on its own database and seeded the way Development seeds
/// it, built once and shared by every per-store test.
/// </summary>
public sealed class StoreProfileHosts : IAsyncLifetime
{
    private readonly Dictionary<string, ProfileFactory> _hosts = new(StringComparer.OrdinalIgnoreCase);

    public async Task InitializeAsync()
    {
        foreach (var profile in Profiles.All)
        {
            var factory = new ProfileFactory(profile);
            await factory.InitializeAsync();
            await SeedLikeDevelopmentAsync(factory.Services);
            _hosts[profile.Key] = factory;
        }
    }

    public async Task DisposeAsync()
    {
        foreach (var factory in _hosts.Values)
            await factory.DisposeAsync();
    }

    public IServiceProvider ServicesFor(string key) => _hosts[key].Services;

    public async Task<HttpClient> SignedInAsync(string key, string username = "cashier", string password = "cashier123")
    {
        var client = _hosts[key].CreateClient();
        var login = await client.PostAsJsonAsync("/auth/login", new { username, password });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<LoginResponse>())!.Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    // Mirrors Program.cs's Development block, minus the migrate (the tests' schema comes from
    // EnsureCreated on a fresh database, like the rest of this suite).
    internal static async Task SeedLikeDevelopmentAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<StoreHubDbContext>().Database.EnsureCreatedAsync();
        await scope.ServiceProvider.GetRequiredService<PaymentMethodSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<ProductCategorySeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>().SeedAsync();
    }

    private sealed class ProfileFactory(StoreProfile profile) : StoreHubWebApplicationFactory
    {
        protected override IStoreIdentityService CreateStoreIdentity() =>
            new TestStoreIdentityService(profile.StoreId, profile.Type);
    }
}
```

`LoginResponse`'s token property name and namespace: copy them from `IntegrationTestBase.AuthenticateAsAsync`.

- [ ] **Step 3: Write the failing tests**

`StoreProfiles/DevSeedTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Profiles = IndyPOS.StoreProfiles.StoreProfiles;

namespace IndyPOS.StoreHub.IntegrationTests.StoreProfiles;

[Collection(StoreProfilesCollection.Name)]
public class DevSeedTests(StoreProfileHosts hosts)
{
    private const string InitialStock = "InitialStock";

    // Aspire restarts reuse the store's database: re-seeding must not double the opening stock.
    [Fact]
    public async Task DevSeed_RunTwice_AddsInitialStockOnce()
    {
        var services = hosts.ServicesFor("MimyMart");

        await StoreProfileHosts.SeedLikeDevelopmentAsync(services);

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        var openingRows = await db.Set<IndyPOS.Domain.Entities.Core.InventoryMovement>()
                                  .CountAsync(m => m.Reason == InitialStock);
        openingRows.Should()
                   .Be(Profiles.MimyMart.Products.Count(p => p.IsTrackable && p.InitialStock > 0));
    }

    [Theory]
    [InlineData("GeneralHardware")]
    [InlineData("MimyMart")]
    [InlineData("MimyShop")]
    public async Task DevSeed_ForEachStore_SeedsThatStoresProducts(string key)
    {
        await using var scope = hosts.ServicesFor(key).CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();

        var barcodes = await db.Products.Select(p => p.Barcode).ToListAsync();

        barcodes.Should()
                .BeEquivalentTo(Profiles.Find(key)!.Products.Select(p => p.Barcode));
    }
}
```

`Products` is the `DbSet` name, and `InventoryMovement` sits under `IndyPOS.Domain.Entities.Core`. Check both against `StoreHubDbContext`.

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~StoreProfiles"`
Expected:
- the seed tests FAIL: today's 5 generic products for every store, and no `InitialStock` rows;
- `RunTwice` FAILS with 0.

- [ ] **Step 4: Seed the profile's products with their opening stock**

In `DevelopmentDataSeeder`:
- inject `IInventoryMovementRepository inventoryMovements` (add the field and constructor parameter);
- replace the whole body of `SeedProductsAsync` with:

```csharp
    private async Task SeedProductsAsync(CancellationToken cancellationToken)
    {
        var storeId = _storeIdentity.StoreId;
        var profile = IndyPOS.StoreProfiles.StoreProfiles.ForType(_storeIdentity.StoreType);

        foreach (var seed in profile.Products)
        {
            var existing = await _productRepository.GetByBarcodeAsync(seed.Barcode, cancellationToken);
            if (existing is not null)
            {
                await RefreshAsync(existing, seed, cancellationToken);
                continue;
            }

            var product = new Product
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
                Barcode = seed.Barcode,
                Name = seed.Name,
                Description = seed.Name,
                Category = seed.Category,
                UnitPrice = seed.UnitPrice,
                IsTrackable = seed.IsTrackable,
                IsActive = true,
                CreatedUtc = DateTime.UtcNow,
                LastModifiedUtc = DateTime.UtcNow
            };
            await _productRepository.AddAsync(product, cancellationToken);
            await AddOpeningStockAsync(product, seed, cancellationToken);
            _logger.LogInformation("Created dev product for {Store}: {Name} ({Barcode})", profile.Key, seed.Name, seed.Barcode);
        }
    }

    // Opening stock only when the product is first created, so a re-seed never doubles it.
    private async Task AddOpeningStockAsync(Product product, IndyPOS.StoreProfiles.StoreProfileProduct seed,
                                            CancellationToken cancellationToken)
    {
        if (!seed.IsTrackable || seed.InitialStock <= 0)
            return;

        await _inventoryMovements.AddAsync(new InventoryMovement
        {
            Id = Guid.NewGuid(),
            StoreId = product.StoreId,
            ProductId = product.Id,
            QuantityDelta = seed.InitialStock,
            Reason = "InitialStock",
            CreatedUtc = DateTime.UtcNow
        }, cancellationToken);
    }

    private async Task RefreshAsync(Product existing, IndyPOS.StoreProfiles.StoreProfileProduct seed,
                                    CancellationToken cancellationToken)
    {
        if (existing.Name == seed.Name && existing.Category == seed.Category &&
            existing.UnitPrice == seed.UnitPrice && existing.IsTrackable == seed.IsTrackable)
            return;

        existing.Name = seed.Name;
        existing.Description = seed.Name;
        existing.Category = seed.Category;
        existing.UnitPrice = seed.UnitPrice;
        existing.IsTrackable = seed.IsTrackable;
        await _productRepository.UpdateAsync(existing, cancellationToken);
    }
```

Copy the `InventoryMovement` property names (`CreatedUtc` etc.) from the entity. Delete the now-unused `ProductCategoryCodes` using if the build warns. Add the ProjectReference to StoreProfiles in `IndyPOS.Infrastructure.csproj`.

- [ ] **Step 5: Development migrates instead of `EnsureCreated`**

In `src/IndyPOS.StoreHub/Program.cs`, inside `if (app.Environment.IsDevelopment())`, replace `await app.EnsureStoreHubDatabaseCreatedAsync();` with:

```csharp
    // Migrations, not EnsureCreated: EnsureCreated never adds a column to an existing database, so a
    // dev database went stale after every schema change and StoreHub crashed while seeding.
    await app.MigrateStoreHubDatabaseAsync();
```

- Update `MigrateStoreHubDatabaseAsync`'s doc comment: "(dev uses EnsureCreated for speed)" becomes "(Development applies them too, on start)".
- Check for other callers: `git grep -n "EnsureStoreHubDatabaseCreatedAsync" -- src tests`. If nothing calls it any more, delete the method.

- [ ] **Step 6: Pass, then commit**

Run:
- `dotnet test tests/IndyPOS.StoreHub.IntegrationTests` → all pass (the 4 new ones included);
- `dotnet test tests/IndyPOS.Application.Tests` → all pass;
- `dotnet build src/IndyPOS.StoreHub` → 0 errors.

`git commit -m "feat(storehub): Development seeds the store profile's products and stock, and migrates its schema"`, with the session line.

---

### Task 4: The same flows, once per store

**Files:**
- Create: `tests/IndyPOS.StoreHub.IntegrationTests/StoreProfiles/StoreTypeFlowTests.cs`

**Interfaces:** consumes Task 3's `StoreProfileHosts`.

- [ ] **Step 1: Write the tests** (these pin today's behaviour per store; they must pass on first run, and each is checked able to fail in Step 2)

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.StoreHub.PaymentMethods;
using IndyPOS.Application.UseCases.StoreHub.ProductCategories;
using IndyPOS.Application.UseCases.StoreHub.Products;
using IndyPOS.Application.UseCases.StoreHub.Sales;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Profiles = IndyPOS.StoreProfiles.StoreProfiles;

namespace IndyPOS.StoreHub.IntegrationTests.StoreProfiles;

/// <summary>
/// The flows that differ by store type, once per dev store. Every expected value is written here,
/// never read from the code under test, so a policy or seed change that drops one fails.
/// </summary>
[Collection(StoreProfilesCollection.Name)]
public class StoreTypeFlowTests(StoreProfileHosts hosts)
{
    private const string PayLater = "PayLater";
    private const string Cash = "Cash";

    public static TheoryData<string, string[]> OfferedMethods => new()
    {
        { "GeneralHardware", ["Cash", "MoneyTransfer", "WelfareCard", "PayLater"] },
        { "MimyMart", ["Cash", "MoneyTransfer", "WelfareCard"] },
        { "MimyShop", ["Cash", "MoneyTransfer", "WelfareCard"] }
    };

    public static TheoryData<string, string[]> Categories => new()
    {
        { "GeneralHardware", ["Miscellaneous", "Beverages", "Snacks", "AlcoholicBeverages", "Food", "Stationery",
                              "Household", "ElectricalAppliances", "Toys", "Medicine", "Agriculture",
                              "GeneralMaterials", "MaterialsAndEquipment", "PlumbingMaterials",
                              "ElectricalMaterials", "ConstructionMaterials"] },
        { "MimyMart", ["Miscellaneous", "Beverages", "Snacks", "AlcoholicBeverages", "Food", "Stationery",
                       "Household", "ElectricalAppliances", "Toys", "Medicine", "Agriculture"] },
        { "MimyShop", ["Gifts", "Toys", "Stationery", "BooksAndNotebooks", "Cosmetics", "Jewellery", "Bags",
                       "Fashion", "Household", "Kitchenware", "SnacksAndBeverages", "MobileAccessories",
                       "Electronics", "PartySupplies", "SeasonalGoods", "Services", "Miscellaneous"] }
    };

    [Theory]
    [InlineData("MimyMart")]
    [InlineData("MimyShop")]
    public async Task PayLaterSale_WithAStoreWithoutPayLater_ReturnsBadRequest(string key)
    {
        var client = await hosts.SignedInAsync(key);

        var response = await client.PostAsJsonAsync("/sales", await SaleAsync(key, PayLater, note: "ลูกค้าทดสอบ"));

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
        (await ErrorOfAsync(response)).Should()
                                      .Be("ช่องทางชำระเงิน 'PayLater' ใช้กับร้านนี้ไม่ได้");
    }

    [Fact]
    public async Task PayLaterSale_OnGeneralHardware_CreatesTheDebt()
    {
        var client = await hosts.SignedInAsync("GeneralHardware");

        var response = await client.PostAsJsonAsync("/sales", await SaleAsync("GeneralHardware", PayLater, note: "ลูกค้าทดสอบ"));

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Created);
    }

    [Theory]
    [MemberData(nameof(OfferedMethods))]
    public async Task OfferedPaymentMethods_ForEachStore_MatchTheStoresSet(string key, string[] expected)
    {
        var client = await hosts.SignedInAsync(key);

        var methods = await client.GetFromJsonAsync<List<PaymentMethodDto>>("/payment-methods");

        methods!.Select(m => m.Code).Should()
                                    .Equal(expected);
    }

    [Theory]
    [MemberData(nameof(Categories))]
    public async Task ProductCategories_ForEachStore_MatchTheStoreType(string key, string[] expected)
    {
        var client = await hosts.SignedInAsync(key);

        var categories = await client.GetFromJsonAsync<List<ProductCategoryDto>>("/product-categories");

        categories!.Select(c => c.Code).Should()
                                       .BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("GeneralHardware", true, true)]
    [InlineData("MimyMart", false, false)]
    [InlineData("MimyShop", false, false)]
    public async Task StoreFeatures_ForEachStore_ReportTheTypesFeatures(string key, bool payLater, bool multipleTypes)
    {
        var client = await hosts.SignedInAsync(key);

        var features = await client.GetFromJsonAsync<StoreFeaturesDto>("/store/features");

        features.Should()
                .Be(new StoreFeaturesDto(payLater, multipleTypes));
    }

    [Theory]
    [InlineData("GeneralHardware")]
    [InlineData("MimyMart")]
    [InlineData("MimyShop")]
    public async Task CashSale_ForEachStore_Completes(string key)
    {
        var client = await hosts.SignedInAsync(key);

        var response = await client.PostAsJsonAsync("/sales", await SaleAsync(key, Cash));

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Created);
    }

    // A service (จัดส่ง) is sold without moving stock: there is none to move.
    [Fact]
    public async Task ServiceSale_OnMimyShop_MovesNoStock()
    {
        var client = await hosts.SignedInAsync("MimyShop");
        var delivery = await ProductAsync("MimyShop", "2002500000014");
        var before = await MovementsOfAsync("MimyShop", delivery.Id);

        var response = await client.PostAsJsonAsync("/sales", new CompleteSaleRequest(
            Lines: [new SaleLineRequest(delivery.Id, Quantity: 1, UnitPrice: delivery.UnitPrice)],
            Payments: [new SalePaymentRequest(Cash, delivery.UnitPrice)]));

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Created);
        (await MovementsOfAsync("MimyShop", delivery.Id)).Should()
                                                         .Be(before);
    }

    private async Task<CompleteSaleRequest> SaleAsync(string key, string method, string? note = null)
    {
        var trackable = Profiles.Find(key)!.Products.First(p => p.IsTrackable);
        var product = await ProductAsync(key, trackable.Barcode);
        return new CompleteSaleRequest(
            Lines: [new SaleLineRequest(product.Id, Quantity: 1, UnitPrice: product.UnitPrice)],
            Payments: [new SalePaymentRequest(method, product.UnitPrice, note)]);
    }

    private async Task<IndyPOS.Domain.Entities.Core.Product> ProductAsync(string key, string barcode)
    {
        await using var scope = hosts.ServicesFor(key).CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<StoreHubDbContext>().Products
                          .AsNoTracking().SingleAsync(p => p.Barcode == barcode);
    }

    private async Task<int> MovementsOfAsync(string key, Guid productId)
    {
        await using var scope = hosts.ServicesFor(key).CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<StoreHubDbContext>()
                          .Set<IndyPOS.Domain.Entities.Core.InventoryMovement>()
                          .CountAsync(m => m.ProductId == productId);
    }

    private static async Task<string?> ErrorOfAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString();
}
```

- [ ] **Step 2: Run, then prove they can fail**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~StoreTypeFlowTests"` → **16 PASS**. They pin today's behaviour.

**Mutation check.** These tests are written after the code, so prove they can fail:
1. Temporarily change `PaymentMethodPolicy.IsOfferable`'s `storeType != StoreType.GeneralHardware` to `storeType == StoreType.MimyShop`. Re-run, and expect the MimyMart PayLater row and its offered-methods row to FAIL.
2. Revert, and confirm `git diff` shows no change to the policy file.

Record the check in the ledger.

- [ ] **Step 3: Commit**

`git commit -m "test(storehub): the store-type flows, once per dev store"`, with the session line.

---

### Task 5: CloudApi in Development: migrations and dev store credentials

**Files:**
- Modify: `src/IndyPOS.CloudApi/IndyPOS.CloudApi.csproj` (+ ProjectReference StoreProfiles), `src/IndyPOS.CloudApi/Program.cs` (Development block, about line 117)
- Create: `src/IndyPOS.CloudApi/Infrastructure/Auth/DevStoreRegistration.cs`
- Create: `tests/IndyPOS.CloudApi.IntegrationTests/DevStoreRegistrationTests.cs`

**Interfaces (Produces):** `DevStoreRegistration.RegisterAsync(IServiceProvider services, IHostEnvironment environment, CancellationToken)` returns `Task<int>`, the number of stores registered.

- [ ] **Step 1: Write the failing tests**

Use the existing `CloudPostgresFixture` (read `tests/IndyPOS.CloudApi.IntegrationTests/CloudPostgresFixture.cs` for how it gives a `CloudDbContext` and a service provider). Copy the arrange part from `RegisterStoreTransactionTests.cs`, which already builds the services `RegisterStoreHandler` needs, including `IStoreClientCredentialStore`.

```csharp
    [Fact]
    public async Task DevStoreRegistration_OutsideDevelopment_RegistersNothing()
    {
        var services = BuildServices();   // as RegisterStoreTransactionTests does

        var registered = await DevStoreRegistration.RegisterAsync(services, Environment("Production"), CancellationToken.None);

        registered.Should()
                  .Be(0);
    }

    [Fact]
    public async Task DevStoreRegistration_RunTwice_RegistersEachStoreOnce()
    {
        var services = BuildServices();
        await DevStoreRegistration.RegisterAsync(services, Environment("Development"), CancellationToken.None);

        var second = await DevStoreRegistration.RegisterAsync(services, Environment("Development"), CancellationToken.None);

        second.Should()
              .Be(0);
        (await StoreIdsAsync(services)).Should()
                                       .BeEquivalentTo(IndyPOS.StoreProfiles.StoreProfiles.All.Select(p => p.StoreId));
    }

    private static IHostEnvironment Environment(string name) =>
        new HostingEnvironment { EnvironmentName = name };
```

`StoreIdsAsync` reads `CloudDbContext.StoreConfigs.Select(s => s.StoreId)`. Use `Microsoft.Extensions.Hosting.Internal.HostingEnvironment`.

Run: `dotnet test tests/IndyPOS.CloudApi.IntegrationTests --filter "FullyQualifiedName~DevStoreRegistrationTests"` → build error.

- [ ] **Step 2: Implement**

```csharp
using IndyPOS.Application.Abstractions.Cloud.Auth;
using IndyPOS.Domain.Entities.Cloud;   // CloudStoreConfig — check its namespace
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Profiles = IndyPOS.StoreProfiles.StoreProfiles;

namespace IndyPOS.CloudApi.Infrastructure.Auth;

/// <summary>
/// Development only: registers each dev store profile as a cloud client with the fixed dev secret, so
/// dev StoreHubs can sync. Production registers stores only through /admin/stores/register, which
/// hands out a random secret.
/// </summary>
public static class DevStoreRegistration
{
    public static async Task<int> RegisterAsync(IServiceProvider services, IHostEnvironment environment,
                                                CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
            return 0;

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CloudDbContext>();
        var credentials = scope.ServiceProvider.GetRequiredService<IStoreClientCredentialStore>();
        var registered = 0;

        foreach (var profile in Profiles.All)
        {
            if (await db.StoreConfigs.AnyAsync(s => s.StoreId == profile.StoreId, cancellationToken))
                continue;

            db.StoreConfigs.Add(new CloudStoreConfig
            {
                StoreId = profile.StoreId,
                StoreName = profile.Name,
                StoreFullName = profile.FullName,
                ClientId = profile.CloudClientId,
                IsActive = true,
                LastModifiedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);
            await credentials.CreateAsync(profile.CloudClientId, Profiles.DevCloudClientSecret, profile.Name, cancellationToken);
            registered++;
        }

        return registered;
    }
}
```

Match `RegisterStoreHandler`'s field names exactly (`StoreConfigs`, `CloudStoreConfig`).

In `Program.cs`, replace the Development block:

```csharp
if (app.Environment.IsDevelopment())
{
    // Migrations, not EnsureCreated: EnsureCreated never updates an existing database.
    await app.MigrateCloudDatabaseAsync();
    await DevStoreRegistration.RegisterAsync(app.Services, app.Environment, CancellationToken.None);
}
```

Update the comment above it ("Dev uses EnsureCreated for speed") to say Development migrates too.

The CloudApi `HealthEndpointTests` from #116 boot CloudApi in Development on a fresh `TestPostgres` database. With this change they migrate and register the dev stores, which is fine on a fresh database. Run them.

- [ ] **Step 3: Pass, then commit**

Run: `dotnet test tests/IndyPOS.CloudApi.IntegrationTests` and `dotnet test tests/IndyPOS.CloudApi.Tests` → all pass.

`git commit -m "feat(cloudapi): Development migrates and registers the dev stores as cloud clients"`, with the session line.

---

### Task 6: AppHost: `--store`, a database and a till per store

**Files:**
- Modify: `src/IndyPOS.AppHost/IndyPOS.AppHost.csproj`
- Modify: `src/IndyPOS.AppHost/Program.cs`
- Create: `src/IndyPOS.AppHost/DevStoreFiles.cs`

**Interfaces:** consumes `StoreProfiles.Resolve`, `StoreProfile.*`, `StoreProfiles.DevCloudClientSecret`.

- [ ] **Step 1: Reference the catalogue (as a library, not a resource)**

```xml
    <ProjectReference Include="..\IndyPOS.StoreProfiles\IndyPOS.StoreProfiles.csproj" IsAspireProjectResource="false" />
```

- [ ] **Step 2: `DevStoreFiles.cs`**

```csharp
using System.Text.Json;
using IndyPOS.StoreProfiles;

/// <summary>
/// Per-store files the till and StoreHub need in dev, written under the AppHost's obj folder so the
/// developer's real C:\ProgramData\IndyPOS files are never read or touched.
/// </summary>
internal static class DevStoreFiles
{
    public static string StoreConfigurationPath(string appHostDirectory, StoreProfile profile)
    {
        var path = Path.Combine(StoreDirectory(appHostDirectory, profile), "StoreConfiguration.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            StoreFullName = profile.FullName,
            StoreName = profile.Name,
            StoreAddressLine1 = profile.AddressLine1,
            StoreAddressLine2 = profile.AddressLine2,
            StorePhoneNumber = profile.Phone,
            PrinterName = "XP-58",
            BarcodeScannerDeviceName = "",
            SerialPortName = "COM1",
            profile.Code
        }, new JsonSerializerOptions { WriteIndented = true }));
        return path;
    }

    public static string SecretsDirectory(string appHostDirectory, StoreProfile profile) =>
        Directory.CreateDirectory(Path.Combine(StoreDirectory(appHostDirectory, profile), "secrets")).FullName;

    private static string StoreDirectory(string appHostDirectory, StoreProfile profile) =>
        Directory.CreateDirectory(Path.Combine(appHostDirectory, "obj", "dev-stores", profile.Key)).FullName;
}
```

Check the property names against `src/IndyPOS.Application/Common/Models/StoreConfiguration.cs`, and match them exactly.

- [ ] **Step 3: Rewrite `Program.cs`**

Starting from #116's version, replace everything after `var cloudApi = …;` up to `builder.Build().Run();` with:

```csharp
// Dev stores: --store <GeneralHardware|MimyMart|MimyShop|all>; none = GeneralHardware.
// Each store gets its own database, StoreHub and till. See IndyPOS.StoreProfiles.
var stores = StoreProfiles.Resolve(builder.Configuration["store"]);
var single = stores.Count == 1;

foreach (var profile in stores)
{
    var database = postgres.AddDatabase(profile.DatabaseName);

    // No launch profile: three instances of one project would all claim its https port.
    var storeHub = builder.AddProject<Projects.IndyPOS_StoreHub>(single ? "storehub-api" : $"storehub-{profile.Key.ToLowerInvariant()}",
                                                                 launchProfileName: null)
                          .WithHttpEndpoint(port: profile.DevPort, name: "http")
                          .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
                          .WithEnvironment("Store__Type", profile.Type.ToString())
                          .WithEnvironment("Store__Id", profile.StoreId)
                          .WithEnvironment("Store__Name", profile.Name)
                          .WithEnvironment("Store__Code", profile.Code.ToString())
                          .WithEnvironment("Secrets__Directory", DevStoreFiles.SecretsDirectory(builder.AppHostDirectory, profile))
                          .WithEnvironment("CloudApi__ClientId", profile.CloudClientId)
                          .WithEnvironment("CloudApi__ClientSecret", StoreProfiles.DevCloudClientSecret)
                          .WithReference(database, "storehub-db")
                          .WithReference(cloudApi)
                          .WithHttpHealthCheck("/health/ready", endpointName: "http")
                          .WaitFor(postgres);

    builder.AddProject<Projects.IndyPOS_Windows_Forms>(single ? "winforms-app" : $"till-{profile.Key.ToLowerInvariant()}")
           .WithReference(storeHub)
           .WithEnvironment("StoreHub__BaseUrl", $"http://localhost:{profile.DevPort}")
           .WithEnvironment("Store__ConfigPath", DevStoreFiles.StoreConfigurationPath(builder.AppHostDirectory, profile))
           .WaitFor(storeHub)
           .WithExplicitStart();
}
```

Keep the existing `postgres` and `cloudApi` definitions from #116. Drop the old `storeHubDb` and the old single `storeHub`/WinForms block.

- `WithReference(database, "storehub-db")` keeps StoreHub's connection-string name. Check the overload exists in Aspire 9.3 (`WithReference(IResourceBuilder<IResourceWithConnectionString>, string connectionName)`). If it does not, use `.WithEnvironment("ConnectionStrings__storehub-db", database)`.
- CloudApi's database name changes to `cloud` (spec §5): `postgres.AddDatabase("cloud-db", databaseName: "cloud")` keeps the resource and connection name. Check that this overload exists; if not, rename the resource and update CloudApi's `AddNpgsqlDbContext("cloud-db")` and `AddDatabaseReadinessCheck("cloud-db")`.

Run: `dotnet build src/IndyPOS.AppHost` → 0 errors.

- [ ] **Step 4: Verify by hand** (record in the PR)

1. `dotnet run --project src/IndyPOS.AppHost`:
   - the dashboard shows `storehub-api` (GeneralHardware) Healthy on 5012;
   - start `winforms-app`: it shows ลงบัญชี and the hardware products;
   - the receipt header says "ร้านวัสดุ (Dev)".
2. `-- --store MimyShop`:
   - the till shows the MimyShop products, including จัดส่ง and เอกสาร;
   - there is no ลงบัญชี;
   - selling จัดส่ง moves no stock.
3. `-- --store all`:
   - three StoreHubs are Healthy on 5012, 5013 and 5014;
   - ring up one sale on each till;
   - CloudApi's inbox (`/sync/status`, signed in per store, or the `cloud` database) shows each store's sale under its own `SourceStoreId`.
4. `-- --store Nope`:
   - the AppHost stops with "Unknown store 'Nope'. Use one of: …".
5. With the installed StoreHub service running:
   - `dir src\IndyPOS.AppHost\obj\dev-stores\GeneralHardware\secrets` is the dev store's;
   - `%ProgramData%\IndyPOS\Secrets` is unchanged (compare its file times before and after).

- [ ] **Step 5: Commit**

`git commit -m "feat(apphost): --store runs one or all dev stores, each with its own database and till"`, with the session line.

---

### Task 7: Docs

- [ ] **Step 1: ONBOARDING gets a "Running as a store" section** (after the Aspire section)

```markdown
### Running as a store

| Command | Starts |
|---|---|
| `dotnet run --project src/IndyPOS.AppHost` | GeneralHardware (ลงบัญชี, hardware products), StoreHub on :5012 |
| `dotnet run --project src/IndyPOS.AppHost -- --store MimyMart` | MimyMart (no ลงบัญชี, groceries) |
| `dotnet run --project src/IndyPOS.AppHost -- --store MimyShop` | MimyShop (service products จัดส่ง / เอกสาร) |
| `dotnet run --project src/IndyPOS.AppHost -- --store all` | all three, on :5012 / :5013 / :5014, syncing to one CloudApi |

- **Each store has its own database** (`storehub-generalhardware`, …), seeded from `IndyPOS.StoreProfiles`, and
  applies migrations on start. The old `storehub-db` and `cloud-db` databases are no longer used; drop them
  if you like.
- **Dev stores sync** with a fixed dev secret that CloudApi registers in Development only.
- **The till's receipt header** comes from a generated file under `src/IndyPOS.AppHost/obj/dev-stores/`.
  Your `C:\ProgramData\IndyPOS\Config\StoreConfiguration.json` is not used.
```

- [ ] **Step 2: CLAUDE.md**

- **Quick Commands:** add the `--store` lines.
- **"Store Configuration (Required for Debug)":** first sentence becomes "Needed only when running the till without Aspire; the AppHost generates one per store."
- **Project Structure:** add `IndyPOS.StoreProfiles/   # Dev store profiles (AppHost, dev seeding, tests)`.

- [ ] **Step 3: Commit**

`git commit -m "docs: running as a dev store"`, with the session line.

---

### Task 8: Measure the counts

Run the whole solution with `--logger trx`, and sum the `.trx` files with `-LiteralPath`. Expected growth over the post-#116 baseline (1209):

| Suite | Added |
|---|---|
| Application | +15 (Task 1) +1 (Task 2) |
| StoreHub.IntegrationTests | +4 (Task 3) +16 (Task 4) |
| CloudApi.IntegrationTests | +2 (Task 5) |
| **Total** | **+38 → 1247** |

Write the measured numbers into CLAUDE.md and ONBOARDING (Expected counts, the Docker-down derivation, which gains StoreHub +20 and CloudApi +2). Commit: `docs: measured test counts after the dev store profiles`.

---

## Self-Review

**Spec coverage:**
- §3 catalogue: Task 1.
- §4.1 `--store`: Tasks 1 (`Resolve`) and 6.
- §4.2 per-profile database, StoreHub and till; the generated `StoreConfiguration`; ProgramData untouched: Task 6.
- §4.3 dev cloud credentials: Task 5, plus Task 6 passing them.
- §5 Development migrations: Task 3 (StoreHub) and Task 5 (CloudApi). Profile seeding: Task 3.
- §6 tests:
  - catalogue: Task 1;
  - per-profile host and flows: Tasks 3-4;
  - CloudApi seeder: Task 5;
  - AppHost by hand: Task 6;
  - counts: Task 8.
- §7 docs: Task 7.
- **Additions beyond the spec:** decisions 1-4 above, plus `Secrets:Directory` (Task 2).

**Placeholders:** none. The steps that say "check the name against X" name the exact file to read. They are about matching existing identifiers, not open design.

**Type consistency:** `StoreProfile`, `StoreProfileProduct`, `StoreProfiles.{All, Default, Find, ForType, Resolve, AllKey, DevCloudClientSecret}`, `StoreProfileHosts.{SignedInAsync, ServicesFor, SeedLikeDevelopmentAsync}` and `DevStoreRegistration.RegisterAsync` are used with the same signatures in every task.

**Known risks:**
- Task 6's Aspire overloads (`WithReference(…, connectionName)`, `AddDatabase(…, databaseName:)`, `launchProfileName: null`). Each has a named fallback.
- Task 4's tests pass on first run by design; the mutation check proves they can fail.
