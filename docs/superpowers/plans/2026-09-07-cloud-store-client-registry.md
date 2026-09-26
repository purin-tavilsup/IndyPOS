# One Client Registry for Store-to-Cloud Auth — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make store-to-cloud OAuth work by giving each store exactly one client registry — OpenIddict's application store — instead of the two (a hashed secret in `CloudStoreConfig` *and* an empty `OpenIddictApplications` table) that make every real token request return `401 invalid_client`.

**Architecture:** OpenIddict's application store owns the client secret; `CloudStoreConfig` owns the store metadata and keeps the public `ClientId` as the join key. Registration writes both rows in one transaction behind a new `IStoreClientCredentialStore` seam. `TokenController` stops verifying the secret (OpenIddict now does it) but keeps its `IsActive` gate. The old `ClientSecretHash` column is dropped.

**Tech Stack:** C# .NET 10, ASP.NET Core, EF Core 10 (Npgsql), OpenIddict 6.x (client-credentials flow), xUnit + FluentAssertions + Moq + EF InMemory for tests.

**Spec:** `docs/superpowers/specs/2026-08-20-cloud-store-client-registry-design.md` — read it alongside this plan; the plan argues from it.

> **Shipped in PR #93 — this plan is the historical record, and three of its steps were wrong.**
> The shipped code fixed each; read the code, not these steps, if you are copying the pattern:
> 1. **Task 2's bare `BeginTransactionAsync`** is rejected by Npgsql's retrying execution strategy.
>    Shipped: the whole unit runs inside `Database.CreateExecutionStrategy().ExecuteAsync`
>    (`RegisterStoreHandler.cs:62-77`).
> 2. **A concurrent duplicate registration** passes the existence check and hits a unique violation
>    → `500`. Shipped: `DbUpdateException` with `UniqueViolation` maps to `409` (commit `04bc03f`).
> 3. **Task 1's EF Core InMemory 10** breaks CloudApi's EF 9 / OpenIddict stack with a
>    `MissingMethodException`. Shipped: `Microsoft.EntityFrameworkCore.InMemory` `9.0.6`
>    (`tests/IndyPOS.CloudApi.Tests/IndyPOS.CloudApi.Tests.csproj`).

## Global Constraints

- **OpenIddict is pinned to `6.*`** in `src/IndyPOS.CloudApi/IndyPOS.CloudApi.csproj`. The 6.x handler layout is assumed throughout. (The spec's out-of-scope note flags pinning the exact version in a separate PR — do **not** do it here.)
- **Tests are xUnit + FluentAssertions** (matching the repo). No MSTest, no `[DataRow]` — use `[Fact]`/`[Theory]` + `[InlineData]`.
- **The `ClientId` convention is `store_{StoreId}`** and the `RegisterStoreResponse` contract (`StoreId`, `ClientId`, `ClientSecret`, `Message`) is unchanged. `GenerateClientSecret()` stays; BCrypt hashing goes.
- **Client permissions are load-bearing.** Every OpenIddict application must carry: `Endpoints.Token`, `GrantTypes.ClientCredentials`, `Prefixes.Scope + "sync.write"`, `Prefixes.Scope + "master.read"`. Missing any → the request is rejected even though the client exists.
- **The `IsActive` check in `TokenController` MUST survive.** OpenIddict knows nothing about `IsActive`; deleting that check alongside the BCrypt block silently re-grants deactivated stores. A test pins it.
- **Registration is all-or-nothing** (one transaction). EF InMemory ignores transactions, so that guarantee is verified against real PostgreSQL (Task 5), not by a unit test.
- **The forward-only migration gate does not bind here** — CloudApi has never been released, so dropping `ClientSecretHash` breaks no rollback. Task 4 states this in the migration.
- **Task order keeps every task compiling:** add the seam → move registration onto it → move token issuance off the secret → *then* drop the column. Do not reorder.

---

### Task 1: The credential-store seam (interface + OpenIddict implementation + DI)

Adds the abstraction and its real implementation. The implementation's descriptor is unit-testable against a mocked `IOpenIddictApplicationManager`; the live round-trip is proved in Task 5. This task also **creates the new test project** because it holds the first xUnit test.

**Files:**
- Create: `src/IndyPOS.Application/Abstractions/Cloud/Auth/IStoreClientCredentialStore.cs`
- Create: `src/IndyPOS.CloudApi/Infrastructure/Auth/OpenIddictStoreClientCredentialStore.cs`
- Modify: `src/IndyPOS.CloudApi/Program.cs` (register the seam in DI, ~line 38)
- Create: `tests/IndyPOS.CloudApi.Tests/IndyPOS.CloudApi.Tests.csproj`
- Create: `tests/IndyPOS.CloudApi.Tests/Infrastructure/Auth/OpenIddictStoreClientCredentialStoreTests.cs`

**Interfaces:**
- Produces: `IStoreClientCredentialStore.CreateAsync(string clientId, string clientSecret, string displayName, CancellationToken cancellationToken = default) : Task` — consumed by Task 2's handler.
- Consumes: `IOpenIddictApplicationManager` (registered by the existing `AddOpenIddict().AddCore().UseEntityFrameworkCore().UseDbContext<CloudDbContext>()` in `OpenIddictExtensions.cs`).

- [ ] **Step 1: Create the test project and add it to the solution**

`tests/IndyPOS.CloudApi.Tests/IndyPOS.CloudApi.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FluentAssertions" Version="8.8.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.5" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.3.0" />
    <PackageReference Include="Moq" Version="4.20.72" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\IndyPOS.CloudApi\IndyPOS.CloudApi.csproj" />
    <ProjectReference Include="..\..\src\IndyPOS.Application\IndyPOS.Application.csproj" />
  </ItemGroup>

</Project>
```

Run: `dotnet sln IndyPOS.sln add tests/IndyPOS.CloudApi.Tests/IndyPOS.CloudApi.Tests.csproj`

- [ ] **Step 2: Write the failing test for the descriptor the seam builds**

`tests/IndyPOS.CloudApi.Tests/Infrastructure/Auth/OpenIddictStoreClientCredentialStoreTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.CloudApi.Infrastructure.Auth;
using Moq;
using OpenIddict.Abstractions;
using Xunit;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace IndyPOS.CloudApi.Tests.Infrastructure.Auth;

public class OpenIddictStoreClientCredentialStoreTests
{
    [Fact]
    public async Task CreateAsync_BuildsConfidentialClientWithTokenEndpointGrantAndScopes()
    {
        OpenIddictApplicationDescriptor? captured = null;
        var managerMock = new Mock<IOpenIddictApplicationManager>();
        managerMock
            .Setup(m => m.CreateAsync(It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()))
            .Callback<OpenIddictApplicationDescriptor, CancellationToken>((d, _) => captured = d)
            .Returns(ValueTask.FromResult<object>(new object()));

        var sut = new OpenIddictStoreClientCredentialStore(managerMock.Object);

        await sut.CreateAsync("store_store1", "the-secret", "Test Store");

        captured.Should().NotBeNull();
        captured!.ClientId.Should().Be("store_store1");
        captured.ClientSecret.Should().Be("the-secret");
        captured.DisplayName.Should().Be("Test Store");
        captured.ClientType.Should().Be(ClientTypes.Confidential);
        captured.Permissions.Should().Contain(new[]
        {
            Permissions.Endpoints.Token,
            Permissions.GrantTypes.ClientCredentials,
            Permissions.Prefixes.Scope + "sync.write",
            Permissions.Prefixes.Scope + "master.read"
        });
    }
}
```

> Note: `IOpenIddictApplicationManager.CreateAsync(descriptor, ct)` returns `ValueTask<object>` in OpenIddict 6.x. If the compiler disagrees on the return type, adjust the `.Returns(...)` accordingly — the assertions on `captured` are what matter.

- [ ] **Step 3: Run the test to verify it fails**

Run: `dotnet test tests/IndyPOS.CloudApi.Tests --filter "OpenIddictStoreClientCredentialStoreTests"`
Expected: FAIL — `OpenIddictStoreClientCredentialStore` / `IStoreClientCredentialStore` do not exist (compile error).

- [ ] **Step 4: Write the interface**

`src/IndyPOS.Application/Abstractions/Cloud/Auth/IStoreClientCredentialStore.cs`:

```csharp
namespace IndyPOS.Application.Abstractions.Cloud.Auth;

/// <summary>
/// Creates the OAuth2 client a store authenticates as. The store's public metadata lives in
/// CloudStoreConfig; the client secret lives here, owned by OpenIddict's application store.
/// Registration writes both in one transaction so neither can exist without the other.
/// </summary>
public interface IStoreClientCredentialStore
{
    Task CreateAsync(
        string clientId,
        string clientSecret,
        string displayName,
        CancellationToken cancellationToken = default);
}
```

- [ ] **Step 5: Write the implementation**

`src/IndyPOS.CloudApi/Infrastructure/Auth/OpenIddictStoreClientCredentialStore.cs`:

```csharp
using IndyPOS.Application.Abstractions.Cloud.Auth;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace IndyPOS.CloudApi.Infrastructure.Auth;

/// <summary>
/// Creates the OpenIddict application row a store authenticates as. The manager hashes the
/// secret; only the hash is persisted. The permission entries are load-bearing — without them
/// OpenIddict's endpoint/grant/scope validation handlers reject the request even though the
/// client exists.
/// </summary>
public sealed class OpenIddictStoreClientCredentialStore : IStoreClientCredentialStore
{
    private readonly IOpenIddictApplicationManager _applicationManager;

    public OpenIddictStoreClientCredentialStore(IOpenIddictApplicationManager applicationManager)
    {
        _applicationManager = applicationManager;
    }

    public async Task CreateAsync(
        string clientId,
        string clientSecret,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            DisplayName = displayName,
            ClientType = ClientTypes.Confidential,
            Permissions =
            {
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.ClientCredentials,
                Permissions.Prefixes.Scope + "sync.write",
                Permissions.Prefixes.Scope + "master.read"
            }
        };

        await _applicationManager.CreateAsync(descriptor, cancellationToken);
    }
}
```

- [ ] **Step 6: Register the seam in DI**

In `src/IndyPOS.CloudApi/Program.cs`, add near the other `AddScoped` calls (after line 38, `AddScoped<ICloudUserRepository, ...>`):

```csharp
builder.Services.AddScoped<IStoreClientCredentialStore, OpenIddictStoreClientCredentialStore>();
```

Add the using at the top: `using IndyPOS.Application.Abstractions.Cloud.Auth;`

- [ ] **Step 7: Run the test to verify it passes**

Run: `dotnet test tests/IndyPOS.CloudApi.Tests --filter "OpenIddictStoreClientCredentialStoreTests"`
Expected: PASS.

- [ ] **Step 8: Commit**

```bash
git add src/IndyPOS.Application/Abstractions/Cloud/Auth/IStoreClientCredentialStore.cs \
        src/IndyPOS.CloudApi/Infrastructure/Auth/OpenIddictStoreClientCredentialStore.cs \
        src/IndyPOS.CloudApi/Program.cs \
        tests/IndyPOS.CloudApi.Tests/ IndyPOS.sln
git commit -m "feat(cloudapi): add IStoreClientCredentialStore over OpenIddict"
```

---

### Task 2: Registration writes both rows in one transaction

`RegisterStoreHandler` stops hashing a secret into `CloudStoreConfig` and instead creates the OpenIddict client through the seam, inside a single transaction.

**Files:**
- Modify: `src/IndyPOS.CloudApi/Infrastructure/Auth/RegisterStoreHandler.cs`
- Create: `tests/IndyPOS.CloudApi.Tests/Infrastructure/Auth/RegisterStoreHandlerTests.cs`

**Interfaces:**
- Consumes: `IStoreClientCredentialStore.CreateAsync(...)` (Task 1); `CloudDbContext.StoreConfigs`; `RegisterStoreCommand`/`RegisterStoreResponse` (unchanged).
- Produces: no new public API — the handler's constructor gains an `IStoreClientCredentialStore` parameter.

- [ ] **Step 1: Write the failing tests**

`tests/IndyPOS.CloudApi.Tests/Infrastructure/Auth/RegisterStoreHandlerTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Abstractions.Cloud.Auth;
using IndyPOS.Application.UseCases.Cloud.Stores.RegisterStore;
using IndyPOS.CloudApi.Domain;
using IndyPOS.CloudApi.Infrastructure;
using IndyPOS.CloudApi.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IndyPOS.CloudApi.Tests.Infrastructure.Auth;

public class RegisterStoreHandlerTests
{
    private static CloudDbContext CreateInMemoryContext() =>
        new(new DbContextOptionsBuilder<CloudDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    [Fact]
    public async Task HandleAsync_ValidCommand_WritesConfigRowAndCreatesClientWithSameSecret()
    {
        await using var db = CreateInMemoryContext();
        string? secretHandedToClientStore = null;
        var credentialStore = new Mock<IStoreClientCredentialStore>();
        credentialStore
            .Setup(s => s.CreateAsync("store_store1", It.IsAny<string>(), "Test Store", It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, secret, _, _) => secretHandedToClientStore = secret)
            .Returns(Task.CompletedTask);

        var handler = new RegisterStoreHandler(db, credentialStore.Object, NullLogger<RegisterStoreHandler>.Instance);
        var command = new RegisterStoreCommand("store1", "Test Store", "Test Store Full");

        var response = await handler.HandleAsync(command);

        response.ClientId.Should().Be("store_store1");
        response.ClientSecret.Should().NotBeNullOrEmpty();
        response.ClientSecret.Should().Be(secretHandedToClientStore);

        var row = await db.StoreConfigs.SingleAsync();
        row.StoreId.Should().Be("store1");
        row.ClientId.Should().Be("store_store1");
        row.IsActive.Should().BeTrue();

        credentialStore.Verify(
            s => s.CreateAsync("store_store1", It.IsAny<string>(), "Test Store", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_DuplicateStoreId_ThrowsAndCreatesNoClient()
    {
        await using var db = CreateInMemoryContext();
        db.StoreConfigs.Add(new CloudStoreConfig { StoreId = "store1", ClientId = "store_store1", IsActive = true });
        await db.SaveChangesAsync();

        var credentialStore = new Mock<IStoreClientCredentialStore>();
        var handler = new RegisterStoreHandler(db, credentialStore.Object, NullLogger<RegisterStoreHandler>.Instance);

        var act = () => handler.HandleAsync(new RegisterStoreCommand("store1", "Test Store", "Test Store Full"));

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*store1*already exists*");
        credentialStore.Verify(
            s => s.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/IndyPOS.CloudApi.Tests --filter "RegisterStoreHandlerTests"`
Expected: FAIL — `RegisterStoreHandler`'s constructor has no `IStoreClientCredentialStore` parameter (compile error).

- [ ] **Step 3: Rewrite the handler**

Replace `src/IndyPOS.CloudApi/Infrastructure/Auth/RegisterStoreHandler.cs` with:

```csharp
using System.Security.Cryptography;
using IndyPOS.Application.Abstractions.Cloud.Auth;
using IndyPOS.Application.UseCases.Cloud.Stores.RegisterStore;
using IndyPOS.CloudApi.Domain;
using Microsoft.EntityFrameworkCore;
using Nokpirab;

namespace IndyPOS.CloudApi.Infrastructure.Auth;

/// <summary>
/// Registers a new store. Writes the CloudStoreConfig row and creates the OpenIddict client the
/// store authenticates as, in one transaction — a store with no credentials, or credentials with
/// no store, are both broken states a partial failure would leave behind.
/// </summary>
public class RegisterStoreHandler : ICommandHandler<RegisterStoreCommand, RegisterStoreResponse>
{
    private readonly CloudDbContext _dbContext;
    private readonly IStoreClientCredentialStore _credentialStore;
    private readonly ILogger<RegisterStoreHandler> _logger;

    public RegisterStoreHandler(
        CloudDbContext dbContext,
        IStoreClientCredentialStore credentialStore,
        ILogger<RegisterStoreHandler> logger)
    {
        _dbContext = dbContext;
        _credentialStore = credentialStore;
        _logger = logger;
    }

    public async Task<RegisterStoreResponse> HandleAsync(
        RegisterStoreCommand command,
        CancellationToken cancellationToken = default)
    {
        var existingStore = await _dbContext.StoreConfigs
            .FirstOrDefaultAsync(s => s.StoreId == command.StoreId, cancellationToken);

        if (existingStore is not null)
        {
            throw new InvalidOperationException($"Store with ID '{command.StoreId}' already exists.");
        }

        var clientId = $"store_{command.StoreId}";
        var clientSecret = GenerateClientSecret();

        var storeConfig = new CloudStoreConfig
        {
            StoreId = command.StoreId,
            StoreName = command.StoreName,
            StoreFullName = command.StoreFullName,
            AddressLine1 = command.AddressLine1,
            AddressLine2 = command.AddressLine2,
            PhoneNumber = command.PhoneNumber,
            PrinterName = command.PrinterName,
            ClientId = clientId,
            IsActive = true,
            LastModifiedAtUtc = DateTime.UtcNow
        };

        // All-or-nothing. EF InMemory ignores this transaction; the guarantee is verified against
        // real PostgreSQL (see the plan's end-to-end task), not by a unit test.
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        _dbContext.StoreConfigs.Add(storeConfig);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _credentialStore.CreateAsync(clientId, clientSecret, command.StoreName, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Registered store {StoreId} with ClientId {ClientId}",
            command.StoreId,
            clientId);

        return new RegisterStoreResponse(
            StoreId: command.StoreId,
            ClientId: clientId,
            ClientSecret: clientSecret,
            Message: "Store registered successfully. Save the ClientSecret securely - it won't be shown again!");
    }

    private static string GenerateClientSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}
```

(The `ClientSecretHash` assignment and the `using BCrypt.Net;` are gone. The column still exists on the entity until Task 4 — that is fine, the handler just no longer writes it.)

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/IndyPOS.CloudApi.Tests --filter "RegisterStoreHandlerTests"`
Expected: PASS (both facts).

- [ ] **Step 5: Commit**

```bash
git add src/IndyPOS.CloudApi/Infrastructure/Auth/RegisterStoreHandler.cs \
        tests/IndyPOS.CloudApi.Tests/Infrastructure/Auth/RegisterStoreHandlerTests.cs
git commit -m "feat(cloudapi): register store + OpenIddict client in one transaction"
```

---

### Task 3: Token issuance drops the secret check, keeps `IsActive`

`TokenController.Exchange` no longer verifies the secret (OpenIddict already did, before passthrough). It keeps the `StoreConfigs` lookup, the `IsActive` gate, the `LastAuthenticatedAtUtc` stamp, and the claims/`SignIn` block.

**Files:**
- Modify: `src/IndyPOS.CloudApi/Infrastructure/Auth/TokenController.cs` (remove the BCrypt block, lines ~82–96, and `using BCrypt.Net;`)
- Create: `tests/IndyPOS.CloudApi.Tests/Infrastructure/Auth/TokenControllerTests.cs`

**Interfaces:**
- Consumes: `CloudDbContext.StoreConfigs`; `HttpContext.GetOpenIddictServerRequest()`.
- Produces: no new public API.

- [ ] **Step 1: Write the failing tests (the `IsActive` regression pin + unknown client)**

`tests/IndyPOS.CloudApi.Tests/Infrastructure/Auth/TokenControllerTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.CloudApi.Domain;
using IndyPOS.CloudApi.Infrastructure;
using IndyPOS.CloudApi.Infrastructure.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using Xunit;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace IndyPOS.CloudApi.Tests.Infrastructure.Auth;

public class TokenControllerTests
{
    private static CloudDbContext CreateInMemoryContext() =>
        new(new DbContextOptionsBuilder<CloudDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static TokenController CreateController(CloudDbContext db, string clientId)
    {
        var request = new OpenIddictRequest
        {
            GrantType = GrantTypes.ClientCredentials,
            ClientId = clientId,
            ClientSecret = "irrelevant-openiddict-already-validated-it"
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set(new OpenIddictServerAspNetCoreFeature
        {
            Transaction = new OpenIddictServerTransaction { Request = request }
        });

        return new TokenController(db, NullLogger<TokenController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    [Fact]
    public async Task Exchange_InactiveStore_IsForbidden()
    {
        await using var db = CreateInMemoryContext();
        db.StoreConfigs.Add(new CloudStoreConfig
        {
            StoreId = "store1",
            StoreName = "Test Store",
            ClientId = "store_store1",
            IsActive = false
        });
        await db.SaveChangesAsync();

        var result = await CreateController(db, "store_store1").Exchange();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Exchange_UnknownClientId_IsForbidden()
    {
        await using var db = CreateInMemoryContext();

        var result = await CreateController(db, "store_does_not_exist").Exchange();

        result.Should().BeOfType<ForbidResult>();
    }
}
```

> The `OpenIddictServerTransaction` / `OpenIddictServerAspNetCoreFeature` stub is the one fiddly seam. If constructing the transaction is awkward in the pinned 6.x build, fall back to a `WebApplicationFactory<Program>` integration test for these two cases — but keep the assertion identical (inactive and unknown both `Forbid`).

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/IndyPOS.CloudApi.Tests --filter "TokenControllerTests"`
Expected: FAIL — with the current controller an inactive store still reaches the BCrypt block; confirm the test compiles and the `Exchange_InactiveStore_IsForbidden` assertion is what drives the change. (If the current combined `is null || !IsActive` already forbids inactive, this test passes today — that is acceptable: it is a **regression pin** that must stay green through Step 3's edit. Run it before and after and confirm it never goes red on the good code.)

- [ ] **Step 3: Remove the BCrypt block from the controller**

In `src/IndyPOS.CloudApi/Infrastructure/Auth/TokenController.cs`:
- Delete the `using BCrypt.Net;` line (line 2).
- Delete the entire secret-verification block (the `if (string.IsNullOrEmpty(storeConfig.ClientSecretHash) || !BCrypt.Net.BCrypt.Verify(...))` `if` and its `Forbid(...)` body, ~lines 82–96).
- Leave everything else untouched — crucially the `if (storeConfig is null || !storeConfig.IsActive)` block above it stays.

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/IndyPOS.CloudApi.Tests --filter "TokenControllerTests"`
Expected: PASS (both facts).

- [ ] **Step 5: Commit**

```bash
git add src/IndyPOS.CloudApi/Infrastructure/Auth/TokenController.cs \
        tests/IndyPOS.CloudApi.Tests/Infrastructure/Auth/TokenControllerTests.cs
git commit -m "feat(cloudapi): let OpenIddict verify the secret, keep the IsActive gate"
```

---

### Task 4: Drop the `ClientSecretHash` column

Now that nothing reads or writes it, remove the column from the entity, the model config, and the schema via a new migration.

**Files:**
- Modify: `src/IndyPOS.CloudApi/Domain/CloudStoreConfig.cs` (remove `ClientSecretHash`)
- Modify: `src/IndyPOS.CloudApi/Infrastructure/CloudDbContext.cs` (remove the `ClientSecretHash` property mapping, ~line 134)
- Create: `src/IndyPOS.CloudApi/Infrastructure/Migrations/<timestamp>_RemoveClientSecretHash.cs` (generated)

**Interfaces:**
- Produces: schema without `CloudStoreConfig.ClientSecretHash`. No code API change beyond the removed property.

- [ ] **Step 1: Remove the property from the entity**

In `src/IndyPOS.CloudApi/Domain/CloudStoreConfig.cs`, delete:

```csharp
    public string? ClientSecretHash { get; set; }
```

- [ ] **Step 2: Remove the model mapping**

In `src/IndyPOS.CloudApi/Infrastructure/CloudDbContext.cs`, delete this line from the `CloudStoreConfig` entity block:

```csharp
            entity.Property(e => e.ClientSecretHash).HasMaxLength(200);
```

- [ ] **Step 3: Verify the code builds without the property**

Run: `dotnet build src/IndyPOS.CloudApi`
Expected: SUCCESS — proves Tasks 2 and 3 removed every reference.

- [ ] **Step 4: Generate the migration**

Run:
```bash
dotnet ef migrations add RemoveClientSecretHash \
  --project src/IndyPOS.CloudApi \
  --context CloudDbContext \
  --output-dir Infrastructure/Migrations
```
Expected: creates `<timestamp>_RemoveClientSecretHash.cs` whose `Up` calls `migrationBuilder.DropColumn(name: "ClientSecretHash", table: "StoreConfigs")`. (The `CloudDbContextDesignTimeFactory` keeps `dotnet ef` from executing `Program.cs`.)

- [ ] **Step 5: Add the gate note to the migration**

At the top of the generated migration's `Up` method, add:

```csharp
        // Forward-only gate (docs/operations/upgrade-procedure.md) forbids drops so restored
        // binaries can still INSERT. It does NOT bind here: CloudApi has never been released, so
        // there are no previous binaries to roll back to. See
        // docs/superpowers/specs/2026-08-20-cloud-store-client-registry-design.md.
```

- [ ] **Step 6: Build to confirm the migration and snapshot compile**

Run: `dotnet build src/IndyPOS.CloudApi`
Expected: SUCCESS. `CloudDbContextModelSnapshot.cs` no longer lists `ClientSecretHash`.

- [ ] **Step 7: Commit**

```bash
git add src/IndyPOS.CloudApi/Domain/CloudStoreConfig.cs \
        src/IndyPOS.CloudApi/Infrastructure/CloudDbContext.cs \
        src/IndyPOS.CloudApi/Infrastructure/Migrations/
git commit -m "feat(cloudapi): drop the now-unused ClientSecretHash column"
```

---

### Task 5: End-to-end verification against real PostgreSQL

The InMemory tests cannot prove the transaction guarantee or the live OpenIddict round-trip. This task is the "verified by running" standard this epic uses, and its passing is what closes defect I0-E — the first time store-to-cloud auth actually works.

**Files:** none (verification only). Record the result in `.claude/STATUS.md` and the PR body.

- [ ] **Step 1: Run the full CloudApi.Tests suite**

Run: `dotnet test tests/IndyPOS.CloudApi.Tests`
Expected: all green (descriptor, registration ×2, token ×2).

- [ ] **Step 2: Start a real PostgreSQL**

Run: `docker run -d --name cloudapi-e2e-pg -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=cloud -p 5433:5432 postgres:16-alpine`

- [ ] **Step 3: Apply the schema with the migrate verb**

Run (PowerShell):
```powershell
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ConnectionStrings__cloud-db = "Host=localhost;Port=5433;Database=cloud;Username=postgres;Password=postgres"
dotnet run --project src/IndyPOS.CloudApi -- migrate
```
Expected: applies `InitialCloudSchema` then `RemoveClientSecretHash`; exits 0. Confirm the column is gone:
```bash
docker exec cloudapi-e2e-pg psql -U postgres -d cloud -c '\d "StoreConfigs"'
```
Expected: no `ClientSecretHash` row.

- [ ] **Step 4: Register a store, exchange for a token, confirm the token persisted**

Run the API (same env as Step 3, plus a `LocalToken` secret and the RSA signing key per `docs/operations/cloud-deployment.md`), then register through `POST /admin/stores/register` with a `SystemAdminOnly` StoreHubJwt (obtained per the runbook), capture the returned `ClientId`/`ClientSecret`, and:
```bash
curl -s -X POST http://localhost:8080/oauth/token \
  -d grant_type=client_credentials \
  -d client_id=<ClientId> \
  -d client_secret=<ClientSecret> \
  -d scope="sync.write master.read"
```
Expected: HTTP 200 with an `access_token` — **not** `401 invalid_client`. Then confirm persistence:
```bash
docker exec cloudapi-e2e-pg psql -U postgres -d cloud -c 'SELECT COUNT(*) FROM "OpenIddictTokens";'
```
Expected: `>= 1`.

- [ ] **Step 5: Tear down and record the result**

Run: `docker rm -f cloudapi-e2e-pg`
Record in `.claude/STATUS.md`: the 200 response, the `OpenIddictTokens` count, and that I0-E is closed. This is not committed (gitignored), but note the outcome in the PR body.

---

## Self-Review

**Spec coverage:**
- "OpenIddict's application store owns the secret; `CloudStoreConfig` owns the store" → Tasks 1–2 + Task 4 (column drop). ✅
- New `IStoreClientCredentialStore` in Application, impl over `IOpenIddictApplicationManager` → Task 1. ✅
- Load-bearing permissions (token endpoint, client-credentials grant, two scopes, confidential) → Task 1 impl + test. ✅
- `RevokeAsync` **cut** per Pond's decision (deferred to I4 with its caller) → interface has only `CreateAsync`. ✅ (Deliberate deviation from the spec's interface; recorded here.)
- Registration writes both rows in one transaction → Task 2. ✅
- `RegisterStoreResponse`, `ClientId` convention, `GenerateClientSecret` unchanged; BCrypt hashing removed → Task 2. ✅
- `TokenController` loses secret verify, keeps lookup + `IsActive` + `LastAuthenticatedAtUtc` + claims/`SignIn` → Task 3. ✅
- `IsActive` regression pin → Task 3 test. ✅
- Column dropped via new migration; forward-only gate note → Task 4. ✅
- New `tests/IndyPOS.CloudApi.Tests` project (xUnit + FluentAssertions) → Task 1. ✅
- InMemory-ignores-transactions caveat → noted in Task 2 handler + Global Constraints; real-Postgres proof → Task 5. ✅
- End-to-end register → exchange → confirm `OpenIddictTokens` INSERT → Task 5. ✅

**Placeholder scan:** No TBD/TODO; every code step carries full code; every run step names the command and expected output.

**Type consistency:** `IStoreClientCredentialStore.CreateAsync(clientId, clientSecret, displayName, ct)` is defined in Task 1 and consumed identically in Task 2's handler and mock. `RegisterStoreHandler(CloudDbContext, IStoreClientCredentialStore, ILogger<RegisterStoreHandler>)` matches between Task 2's code and both test call-sites. `ClientId` values (`store_store1`) consistent across Tasks 2–3.

**Out-of-scope guardrails (from the spec):** do not call/implement `RevokeAsync`; do not implement secret rotation; do not touch the TLS/ID2083 flag (PR #89); do not pin OpenIddict's exact version here.
