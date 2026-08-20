# CloudApi Containerization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Package `IndyPOS.CloudApi` as a Docker image with a compose stack that runs it locally and
deploys it to a DigitalOcean Droplet, so Epic I tasks I1–I3 deploy a proven artefact.

**Architecture:** Four sequential PRs. A dependency bump, then a fail-closed startup guard on the
shared JWT secret in both hosts, then a schema path for CloudApi (an initial EF migration plus a
`migrate` verb, mirroring StoreHub's installer-invoked pattern), then the packaging itself — a
multi-stage Dockerfile plus a local and a production compose file under `deploy/cloud/`.

**Tech Stack:** .NET 10, ASP.NET Core minimal APIs, EF Core 9.0.6 (CloudApi's resolved version, via
`Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` 9.2.0), OpenIddict 6.4, PostgreSQL 16, Docker +
Compose, xUnit + FluentAssertions.

**Spec:** `docs/superpowers/specs/2026-08-20-cloudapi-containerization-design.md`

## Global Constraints

- **Target framework is `net10.0`** for every project touched. Do not retarget anything.
- **Test framework is xUnit + FluentAssertions 8.8.0** — `[Fact]` / `[Theory]` + `[InlineData]`.
  There is no MSTest here; `[DataRow]` does not exist in this repo.
- **Test naming:** `Method_Condition_ShouldExpectedBehavior`. Arrange/Act/Assert separated by blank
  lines. No `#region`.
- **Existing test file convention** in `IndyPOS.Application.Tests` puts the file-scoped `namespace`
  declaration **first**, then `using` directives. Match it.
- **Suite baselines that must hold** (measured 2026-08-19, Docker up, real store databases present):
  solution **637 total / 636 pass / 1 skip**; StoreHub integration suite **104**; installer, which
  `IndyPOS.sln` excludes, **231 total / 223 pass / 8 skip**.
- **Forward-only migration gate:** additive only; new columns nullable or defaulted; no renames,
  drops, or type narrowing. See `docs/operations/upgrade-procedure.md`.
- **`.claude/` is gitignored** and this repo is public. Never stage anything under it. Never commit a
  `.env`, a real connection string, a real secret, or a store's file paths.
- **Pushing requires** `gh auth switch --user purin-tavilsup`, and switching back to `purin-mimica`
  afterwards.
- **Conventional commits.** Small, focused commits — one logical unit each.
- Branch from `development` for each PR. `development` is the main branch.

---

## File Structure

**PR 0 — `chore/bump-nokpirab-1.1.0`**
- Modify: `src/IndyPOS.Application/IndyPOS.Application.csproj` — the single `Nokpirab` version pin.

**PR 1 — `fix/reject-default-jwt-secret`**
- Modify: `src/IndyPOS.Application/Common/Models/LocalTokenOptions.cs` — name the default literal,
  expose a predicate for "still the default".
- Create: `src/IndyPOS.Application/Common/Models/LocalTokenOptionsValidator.cs` — the throw. Pure,
  no hosting dependency, so it is unit-testable and both hosts share one message.
- Create: `tests/IndyPOS.Application.Tests/Models/LocalTokenOptionsTests.cs`
- Create: `tests/IndyPOS.Application.Tests/Models/LocalTokenOptionsValidatorTests.cs`
- Modify: `src/IndyPOS.CloudApi/Program.cs:61-63` — call the validator.
- Modify: `src/IndyPOS.StoreHub/Program.cs:130-132` — call the validator.
- Modify: `tests/IndyPOS.StoreHub.IntegrationTests/StoreHubWebApplicationFactory.cs:33-35` — supply a
  per-run secret, because the factory hosts as `Testing`.

**PR 2 — `feat/cloudapi-migrate-verb`**
- Create: `src/IndyPOS.CloudApi/Infrastructure/CloudDbContextDesignTimeFactory.cs` — so `dotnet ef`
  never executes `Program.cs`.
- Create: `src/IndyPOS.CloudApi/Infrastructure/CloudDbContextExtensions.cs` — `MigrateCloudDatabaseAsync`,
  mirroring `StoreHubDbContextExtensions`.
- Create: `src/IndyPOS.CloudApi/Infrastructure/Migrations/*` — generated, never hand-written.
- Modify: `src/IndyPOS.CloudApi/IndyPOS.CloudApi.csproj` — add `Microsoft.EntityFrameworkCore.Design`.
- Modify: `src/IndyPOS.CloudApi/Infrastructure/CloudDbContext.cs` — only if the Task 4 probe says so.
- Modify: `src/IndyPOS.CloudApi/Program.cs:106-112` — the `migrate` branch.

**PR 3 — `feat/cloudapi-docker`**
- Create: `src/IndyPOS.CloudApi/Dockerfile` — build context is the repo root.
- Create: `.dockerignore` (repo root).
- Create: `deploy/cloud/compose.yaml` — local full stack.
- Create: `deploy/cloud/compose.prod.yaml` — Droplet deployment unit.
- Create: `deploy/cloud/.env.example` — committed; contains no real secret.
- Create: `docs/operations/cloud-deployment.md`.
- Modify: `.gitignore` — ignore `deploy/cloud/.env`.

---

## Task 1: Bump Nokpirab to 1.1.0 (PR 0)

`Nokpirab` supplies `ICommandHandler` / `IQueryHandler`, which every use case, both API hosts and the
WinForms client are written against. A breaking change surfaces solution-wide, so this lands alone.

**Files:**
- Modify: `src/IndyPOS.Application/IndyPOS.Application.csproj:23`

**Interfaces:**
- Consumes: nothing.
- Produces: nothing new. Later tasks rely only on `ICommandHandler<TCommand, TResponse>` and
  `IQueryHandler<TQuery, TResponse>` continuing to resolve, which is what this task verifies.

- [ ] **Step 1: Create the branch**

```bash
git checkout development
git pull
git checkout -b chore/bump-nokpirab-1.1.0
```

- [ ] **Step 2: Capture the baseline BEFORE changing anything**

Docker must be running and the real store databases present, or the numbers will not be comparable.

```bash
dotnet test 2>&1 | tail -30
```

Expected: `637 total, 636 passed, 1 skipped`. Write the actual numbers down — if the baseline does
not match, stop and report it rather than proceeding; a pre-existing failure must not be attributed
to this bump.

- [ ] **Step 3: Bump the version**

In `src/IndyPOS.Application/IndyPOS.Application.csproj`, change:

```xml
<PackageReference Include="Nokpirab" Version="1.0.0" />
```

to:

```xml
<PackageReference Include="Nokpirab" Version="1.1.0" />
```

- [ ] **Step 4: Restore and build**

```bash
dotnet build 2>&1 | tail -20
```

Expected: build succeeds, 0 errors. If it fails with missing members on `ICommandHandler` /
`IQueryHandler`, 1.1.0 is a breaking change: fix the call sites in this same PR and note what changed
in the commit body. Do not roll the version back without saying so.

- [ ] **Step 5: Run the full suite and compare**

```bash
dotnet test 2>&1 | tail -30
```

Expected: the same numbers as Step 2 — `637 total, 636 passed, 1 skipped`. Any delta is a finding to
report, not a number to accept.

- [ ] **Step 6: Run the installer suite, which `dotnet test` never touches**

```bash
dotnet test tests/IndyPOS.Bootstrapper.Tests 2>&1 | tail -15
```

Expected: `231 total, 223 passed, 8 skipped`, exit 0.

- [ ] **Step 7: Commit**

```bash
git add src/IndyPOS.Application/IndyPOS.Application.csproj
git commit -m "chore: bump Nokpirab to 1.1.0

Nokpirab supplies the ICommandHandler/IQueryHandler abstractions every
use case and both API hosts compile against, so the bump lands on its
own PR where its blast radius is legible.

Verified against the full suite (637 total, 636 pass, 1 skip) and the
installer suite (231 total, 223 pass, 8 skip), both unchanged."
```

---

## Task 2: The default-secret predicate (PR 1)

**Files:**
- Modify: `src/IndyPOS.Application/Common/Models/LocalTokenOptions.cs`
- Test: `tests/IndyPOS.Application.Tests/Models/LocalTokenOptionsTests.cs` (create)

**Interfaces:**
- Consumes: `LocalTokenOptions` (existing: `SecretKey`, `Issuer`, `Audience`, `ExpiryHours`,
  `const string SectionName = "LocalToken"`).
- Produces:
  - `public const string BuiltInDefaultSecretKey` — the literal that the property initialiser also
    uses, so the two cannot drift apart.
  - `public bool UsesBuiltInDefaultSecretKey { get; }` — `true` when the key is the built-in default
    **or** is null/blank. Task 3's validator consumes exactly this.

- [ ] **Step 1: Create the branch**

```bash
git checkout development
git pull
git checkout -b fix/reject-default-jwt-secret
```

- [ ] **Step 2: Write the failing test**

Create `tests/IndyPOS.Application.Tests/Models/LocalTokenOptionsTests.cs`:

```csharp
namespace IndyPOS.Application.Tests.Models;

using FluentAssertions;
using IndyPOS.Application.Common.Models;
using Xunit;

public class LocalTokenOptionsTests
{
    [Fact]
    public void UsesBuiltInDefaultSecretKey_WithUntouchedOptions_ShouldReturnTrue()
    {
        var options = new LocalTokenOptions();

        options.UsesBuiltInDefaultSecretKey
               .Should()
               .BeTrue("a freshly constructed instance still carries the repo's published key");
    }

    [Fact]
    public void UsesBuiltInDefaultSecretKey_WithConfiguredKey_ShouldReturnFalse()
    {
        var options = new LocalTokenOptions
        {
            SecretKey = "eEq0T1u+PzvR7mQ3nS9xW2yB5cF8hJ1kL4oN6pA0dG=="
        };

        options.UsesBuiltInDefaultSecretKey
               .Should()
               .BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UsesBuiltInDefaultSecretKey_WithMissingKey_ShouldReturnTrue(string? secretKey)
    {
        var options = new LocalTokenOptions { SecretKey = secretKey! };

        options.UsesBuiltInDefaultSecretKey
               .Should()
               .BeTrue("an unset key is no safer than the default one");
    }

    [Fact]
    public void BuiltInDefaultSecretKey_ShouldBeTheValueThePropertyInitialiserUses()
    {
        new LocalTokenOptions().SecretKey
                               .Should()
                               .Be(LocalTokenOptions.BuiltInDefaultSecretKey);
    }
}
```

- [ ] **Step 3: Run the test to verify it fails**

```bash
dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~LocalTokenOptionsTests"
```

Expected: **compile error** — `UsesBuiltInDefaultSecretKey` and `BuiltInDefaultSecretKey` do not
exist yet. A compile failure is a valid red for a new member.

- [ ] **Step 4: Write the minimal implementation**

In `src/IndyPOS.Application/Common/Models/LocalTokenOptions.cs`, replace the `SecretKey` member with:

```csharp
    /// <summary>
    /// The key this class falls back to when no LocalToken section is configured. It is published in
    /// a public repository, so it is a placeholder for local development only — never a usable
    /// secret. <see cref="LocalTokenOptionsValidator"/> refuses to start a host that still has it.
    /// </summary>
    public const string BuiltInDefaultSecretKey = "IndyPOS-StoreHub-Local-Auth-Secret-Key-2026";

    /// <summary>
    /// Secret key for signing tokens. Must be at least 32 characters.
    /// </summary>
    public string SecretKey { get; set; } = BuiltInDefaultSecretKey;

    /// <summary>
    /// True when this instance carries no usable secret — either the built-in default or nothing at
    /// all. Both cases mean "the operator has not supplied a key".
    /// </summary>
    public bool UsesBuiltInDefaultSecretKey =>
        string.IsNullOrWhiteSpace(SecretKey) || SecretKey == BuiltInDefaultSecretKey;
```

- [ ] **Step 5: Run the test to verify it passes**

```bash
dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~LocalTokenOptionsTests"
```

Expected: **6 passed, 0 failed** — three `[Fact]` cases plus the three `[InlineData]` rows.

- [ ] **Step 6: Commit**

```bash
git add src/IndyPOS.Application/Common/Models/LocalTokenOptions.cs \
        tests/IndyPOS.Application.Tests/Models/LocalTokenOptionsTests.cs
git commit -m "feat: let LocalTokenOptions report an unconfigured secret

Names the fallback literal as a constant so the property initialiser and
the check cannot drift, and treats a blank key the same as the default --
both mean no key was supplied."
```

---

## Task 3: Refuse the default secret in both hosts (PR 1)

**Files:**
- Create: `src/IndyPOS.Application/Common/Models/LocalTokenOptionsValidator.cs`
- Test: `tests/IndyPOS.Application.Tests/Models/LocalTokenOptionsValidatorTests.cs` (create)
- Modify: `src/IndyPOS.CloudApi/Program.cs:61-63`
- Modify: `src/IndyPOS.StoreHub/Program.cs:130-132`
- Modify: `tests/IndyPOS.StoreHub.IntegrationTests/StoreHubWebApplicationFactory.cs:33-35`

**Interfaces:**
- Consumes: `LocalTokenOptions.UsesBuiltInDefaultSecretKey` from Task 2.
- Produces: `LocalTokenOptionsValidator.EnsureProductionSafe(LocalTokenOptions options, bool isDevelopment)`
  — returns `void`, throws `InvalidOperationException`. Takes a `bool` rather than an
  `IHostEnvironment` deliberately: `IndyPOS.Application` must not take a hosting dependency, and a
  bool is trivially testable.

- [ ] **Step 1: Write the failing test**

Create `tests/IndyPOS.Application.Tests/Models/LocalTokenOptionsValidatorTests.cs`:

```csharp
namespace IndyPOS.Application.Tests.Models;

using System;
using FluentAssertions;
using IndyPOS.Application.Common.Models;
using Xunit;

public class LocalTokenOptionsValidatorTests
{
    private static LocalTokenOptions ConfiguredOptions() => new()
    {
        SecretKey = "eEq0T1u+PzvR7mQ3nS9xW2yB5cF8hJ1kL4oN6pA0dG=="
    };

    [Fact]
    public void EnsureProductionSafe_WithDefaultKeyOutsideDevelopment_ShouldThrow()
    {
        var act = () => LocalTokenOptionsValidator.EnsureProductionSafe(
            new LocalTokenOptions(),
            isDevelopment: false);

        act.Should()
           .Throw<InvalidOperationException>()
           .WithMessage("*LocalToken__SecretKey*");
    }

    [Fact]
    public void EnsureProductionSafe_WithDefaultKeyInDevelopment_ShouldNotThrow()
    {
        var act = () => LocalTokenOptionsValidator.EnsureProductionSafe(
            new LocalTokenOptions(),
            isDevelopment: true);

        act.Should()
           .NotThrow("the Aspire dev loop must keep starting without configuration");
    }

    [Fact]
    public void EnsureProductionSafe_WithConfiguredKeyOutsideDevelopment_ShouldNotThrow()
    {
        var act = () => LocalTokenOptionsValidator.EnsureProductionSafe(
            ConfiguredOptions(),
            isDevelopment: false);

        act.Should()
           .NotThrow();
    }

    [Fact]
    public void EnsureProductionSafe_WithBlankKeyOutsideDevelopment_ShouldThrow()
    {
        var act = () => LocalTokenOptionsValidator.EnsureProductionSafe(
            new LocalTokenOptions { SecretKey = "  " },
            isDevelopment: false);

        act.Should()
           .Throw<InvalidOperationException>();
    }

    [Fact]
    public void EnsureProductionSafe_WithNullOptions_ShouldThrowArgumentNullException()
    {
        var act = () => LocalTokenOptionsValidator.EnsureProductionSafe(null!, isDevelopment: false);

        act.Should()
           .Throw<ArgumentNullException>();
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

```bash
dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~LocalTokenOptionsValidatorTests"
```

Expected: compile error — `LocalTokenOptionsValidator` does not exist.

- [ ] **Step 3: Write the minimal implementation**

Create `src/IndyPOS.Application/Common/Models/LocalTokenOptionsValidator.cs`:

```csharp
namespace IndyPOS.Application.Common.Models;

/// <summary>
/// Startup guard for the shared JWT signing key. Both API hosts fall back to
/// <see cref="LocalTokenOptions.BuiltInDefaultSecretKey"/> when no LocalToken section is configured,
/// and that literal is published in a public repository — so outside development, running on it
/// would let anyone mint a token for the capability-protected admin endpoints.
/// </summary>
public static class LocalTokenOptionsValidator
{
    /// <summary>
    /// Throws unless the host has been given a real signing key. Development is the only exemption:
    /// every other environment name — Production, Testing, Staging, or a typo — must supply one, so
    /// a mistyped ASPNETCORE_ENVIRONMENT cannot silently disable the guard.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// The key is missing or is still the built-in default, and this is not development.
    /// </exception>
    public static void EnsureProductionSafe(LocalTokenOptions options, bool isDevelopment)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (isDevelopment || !options.UsesBuiltInDefaultSecretKey)
        {
            return;
        }

        throw new InvalidOperationException(
            $"{LocalTokenOptions.SectionName}:SecretKey is not configured, or is still the built-in " +
            "default that ships in this public repository. It signs the tokens that guard the admin " +
            "endpoints, so the host will not start outside Development without a real one. Set the " +
            $"environment variable {LocalTokenOptions.SectionName}__SecretKey, or the " +
            $"{LocalTokenOptions.SectionName}:SecretKey entry in appsettings.json — on an installed " +
            "till the installer's DatabaseSetup step writes a DPAPI-protected key there.");
    }
}
```

- [ ] **Step 4: Run the test to verify it passes**

```bash
dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~LocalTokenOptionsValidatorTests"
```

Expected: 5 passed, 0 failed.

- [ ] **Step 5: Commit the validator**

```bash
git add src/IndyPOS.Application/Common/Models/LocalTokenOptionsValidator.cs \
        tests/IndyPOS.Application.Tests/Models/LocalTokenOptionsValidatorTests.cs
git commit -m "feat: add a fail-closed guard for the shared JWT signing key

Development is the only exemption. Guarding on IsProduction() alone would
let a mistyped ASPNETCORE_ENVIRONMENT serve admin auth with a key that is
committed to a public repo."
```

- [ ] **Step 6: Wire the guard into CloudApi**

In `src/IndyPOS.CloudApi/Program.cs`, immediately after the existing binding at lines 61-62 and
**before** `builder.Services.AddAuthentication().AddJwtBearer("StoreHubJwt", …)`:

```csharp
var localTokenOptions = builder.Configuration.GetSection(LocalTokenOptions.SectionName).Get<LocalTokenOptions>()
    ?? new LocalTokenOptions();

LocalTokenOptionsValidator.EnsureProductionSafe(
    localTokenOptions,
    builder.Environment.IsDevelopment());
```

`LocalTokenOptions` is already imported via `using IndyPOS.Application.Common.Models;` — the
validator lives in the same namespace, so no new using is needed.

- [ ] **Step 7: Wire the guard into StoreHub**

In `src/IndyPOS.StoreHub/Program.cs`, immediately after the existing binding at lines 130-131 and
**before** `builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)`:

```csharp
var tokenOptions = builder.Configuration.GetSection(LocalTokenOptions.SectionName).Get<LocalTokenOptions>()
    ?? new LocalTokenOptions();

LocalTokenOptionsValidator.EnsureProductionSafe(
    tokenOptions,
    builder.Environment.IsDevelopment());
```

This sits **after** `builder.Configuration.UnprotectSecrets(...)` at line 58, so the DPAPI-protected
value from an installed till has already been decrypted and the guard sees the real key.

- [ ] **Step 8: Give the integration-test host a real key**

`StoreHubWebApplicationFactory` hosts as `Testing`, so the guard applies to it. In
`tests/IndyPOS.StoreHub.IntegrationTests/StoreHubWebApplicationFactory.cs`, inside
`ConfigureWebHost`, next to the existing `UseSetting` call:

```csharp
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment to Testing and provide connection string
        builder.UseSetting("ConnectionStrings:storehub-db", _postgresContainer.GetConnectionString());

        // The host refuses to start outside Development on the built-in default signing key.
        // Tests obtain their tokens from the login endpoint, which signs server-side, so a random
        // per-run key is invisible to them.
        builder.UseSetting(
            "LocalToken:SecretKey",
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)));

        builder.UseEnvironment("Testing");
```

Add the usings the file does not yet have:

```csharp
using System;
using System.Security.Cryptography;
```

- [ ] **Step 9: Verify the guard did not break the integration suite**

```bash
dotnet test tests/IndyPOS.StoreHub.IntegrationTests 2>&1 | tail -15
```

Expected: **104 total**, same pass/skip split as the baseline. If tests fail with the guard's
message, the fixture change is not being applied — check that `UseSetting` runs before the host
builds, not inside `ConfigureTestServices`.

- [ ] **Step 10: Verify by running, not only by testing**

Start StoreHub outside Development with no key and read the failure:

```bash
ASPNETCORE_ENVIRONMENT=Production dotnet run --project src/IndyPOS.StoreHub --no-launch-profile
```

Expected: it exits with the `InvalidOperationException` naming `LocalToken__SecretKey`. Record the
actual message. Then confirm the Aspire dev loop is untouched:

```bash
dotnet run --project src/IndyPOS.AppHost --launch-profile https
```

Expected: `storehub-api` and `cloud-api` both reach Running. Stop it afterwards.

- [ ] **Step 11: Run the full suite**

```bash
dotnet test 2>&1 | tail -30
```

Expected: `637 total, 636 passed, 1 skipped` — the two new test files add rows, so confirm the total
grew by exactly the number of new test cases and that failures are still 0. Record the new total; it
becomes the baseline for PR 2.

- [ ] **Step 12: Commit the wiring**

```bash
git add src/IndyPOS.CloudApi/Program.cs \
        src/IndyPOS.StoreHub/Program.cs \
        tests/IndyPOS.StoreHub.IntegrationTests/StoreHubWebApplicationFactory.cs
git commit -m "fix: refuse to start on the default JWT secret outside development

Both hosts fell back to a signing key committed to this public repo
whenever no LocalToken section was configured -- which is the normal case
for CloudApi, since it ships no appsettings.json at all. That key is the
only guard on the capability-protected admin endpoints.

The integration-test factory hosts as Testing rather than Development, so
it now supplies a random per-run key. Tests take their tokens from the
login endpoint, which signs server-side, so the value is invisible to
them."
```

---

## Task 4: Probe the OpenIddict token flow (PR 2)

Defect I0-D: `modelBuilder.UseOpenIddict()` is called nowhere, and `CloudDbContext.OnModelCreating`
never calls `base.OnModelCreating`, so OpenIddict's EF entities are not in the model — while
`AddCore().UseEntityFrameworkCore().UseDbContext<CloudDbContext>()` registers its EF stores against
that context and nothing calls `DisableTokenStorage()`.

**The migration generated in Task 5 depends on how this is resolved, so it is settled first, by
running the flow.** Produce evidence, then choose.

**Files:**
- Modify (one of two ways, decided by this task's evidence):
  `src/IndyPOS.CloudApi/Infrastructure/CloudDbContext.cs` **or**
  `src/IndyPOS.CloudApi/Infrastructure/Auth/OpenIddictExtensions.cs`

**Interfaces:**
- Consumes: nothing from earlier tasks.
- Produces: a decision recorded in the commit body, and whichever one-line change it implies. Task 5
  generates the migration *after* this, so the model is final when the snapshot is taken.

- [ ] **Step 1: Create the branch**

```bash
git checkout development
git pull                       # PR 1 must be merged first
git checkout -b feat/cloudapi-migrate-verb
```

- [ ] **Step 2: Bring up a database and CloudApi in Development**

```bash
docker run -d --name probe-pg -e POSTGRES_PASSWORD=probe -e POSTGRES_DB=cloud_probe \
  -p 5434:5432 postgres:16-alpine
```

Then run CloudApi against it (Development, so `EnsureCreatedAsync` builds the schema):

```bash
ConnectionStrings__cloud-db="Host=localhost;Port=5434;Database=cloud_probe;Username=postgres;Password=probe" \
ASPNETCORE_ENVIRONMENT=Development \
dotnet run --project src/IndyPOS.CloudApi --no-launch-profile
```

- [ ] **Step 3: Record which tables EnsureCreated actually made**

```bash
docker exec probe-pg psql -U postgres -d cloud_probe -c "\dt"
```

Expected, per the defect: the nine application tables and **no** `OpenIddict*` tables. Record the
actual list — this is the evidence that the model omits them.

- [ ] **Step 4: Drive the token endpoint and record the failure mode**

`TokenController.Exchange` looks the client up in `CloudStoreConfig` by `ClientId` and verifies the
secret with `BCrypt.Net.BCrypt.Verify(clientSecret, storeConfig.ClientSecretHash)`
(`TokenController.cs:82-84`), so the row needs a real BCrypt hash. Neither `psql` nor the system
Python can produce one; use the same library the app uses, in a throwaway console app:

```bash
SCRATCH="$(mktemp -d)" && cd "$SCRATCH"
dotnet new console -o . --force >/dev/null
dotnet add package BCrypt.Net-Next >/dev/null
printf 'Console.WriteLine(BCrypt.Net.BCrypt.HashPassword(args[0]));\n' > Program.cs
dotnet run -- probe-secret
```

Copy the printed hash. Then insert the row — EF maps `CloudStoreConfig` with no `ToTable`, so the
table is `StoreConfigs` and the columns keep their PascalCase property names, which means every
identifier must be double-quoted in `psql`:

```bash
docker exec probe-pg psql -U postgres -d cloud_probe -c \
  "insert into \"StoreConfigs\" (\"StoreId\", \"StoreName\", \"StoreFullName\", \"LastModifiedAtUtc\", \"ClientId\", \"ClientSecretHash\", \"IsActive\")
   values ('probe-store', 'Probe', 'Probe Store', now(), 'probe-store', '<paste the BCrypt hash>', true);"
```

Then request a token (CloudApi's http profile listens on 5180):

```bash
curl -i -X POST http://localhost:5180/oauth/token \
  -d grant_type=client_credentials \
  -d client_id=probe-store \
  -d client_secret=probe-secret
```

Record the HTTP status, the response body, **and the server-side exception if one is logged**. That
exception — or its absence — is the whole point of this task.

- [ ] **Step 5: Choose the fix from the evidence and write it down**

Two candidates, and the evidence decides:

- If the flow fails inside OpenIddict's stores, or tokens must be revocable (Epic I's sync design
  wants a store to lose access when deregistered), add to `CloudDbContext.OnModelCreating`, as its
  **last** statement:

  ```csharp
      base.OnModelCreating(modelBuilder);
      modelBuilder.UseOpenIddict();
  ```

  with `using OpenIddict.EntityFrameworkCore.Models;` if the compiler asks for it.

- If the flow needs no persistence — stateless access tokens, 15-minute lifetime, no revocation —
  add to the `AddServer` block in `OpenIddictExtensions.cs`, beside the existing
  `options.DisableAccessTokenEncryption();`:

  ```csharp
                options.DisableTokenStorage();
  ```

  and note that revocation is then impossible until it is revisited.

Do not pick by preference. Pick by what Step 4 printed, and put that output in the commit body.

- [ ] **Step 6: Re-run the probe to confirm the fix**

Drop the database, restart CloudApi so `EnsureCreatedAsync` rebuilds, and repeat Step 4.

```bash
docker exec probe-pg psql -U postgres -c "drop database cloud_probe;"
docker exec probe-pg psql -U postgres -c "create database cloud_probe;"
```

Expected: `POST /oauth/token` returns `200` with an `access_token`. If the chosen fix was
`UseOpenIddict()`, `\dt` now also lists the `OpenIddict*` tables.

- [ ] **Step 7: Tear down and commit**

```bash
docker rm -f probe-pg
git add -A src/IndyPOS.CloudApi/
git commit -m "fix: make OpenIddict's token flow work against CloudDbContext

UseOpenIddict() was called nowhere and OnModelCreating never chained to
base, so OpenIddict's EF entities were absent from the model while its EF
stores were registered against that same context.

Evidence from driving POST /oauth/token against a real Postgres, before
and after, is below -- the fix was chosen from it rather than from
preference:

<paste the recorded status, body and server exception here>"
```

---

## Task 5: Initial migration and the `migrate` verb (PR 2)

**Files:**
- Modify: `src/IndyPOS.CloudApi/IndyPOS.CloudApi.csproj`
- Create: `src/IndyPOS.CloudApi/Infrastructure/CloudDbContextDesignTimeFactory.cs`
- Create: `src/IndyPOS.CloudApi/Infrastructure/CloudDbContextExtensions.cs`
- Create: `src/IndyPOS.CloudApi/Infrastructure/Migrations/` (generated)
- Modify: `src/IndyPOS.CloudApi/Program.cs:106-112`

**Interfaces:**
- Consumes: the final model from Task 4; `CloudDbContext(DbContextOptions<CloudDbContext> options)`.
- Produces: `CloudDbContextExtensions.MigrateCloudDatabaseAsync(this IHost app)` returning `Task` —
  the compose one-shot in Task 7 invokes it through the `migrate` argument.

- [ ] **Step 1: Confirm the EF tool version can handle this project**

CloudApi resolves **EF Core 9.0.6** (via `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` 9.2.0). The
tool must be at least that version.

```bash
dotnet ef --version
```

Expected: `9.0.8` or newer. If it reports older than 9.0.6, or the command is missing:

```bash
dotnet tool update --global dotnet-ef
```

- [ ] **Step 2: Add the design-time package**

In `src/IndyPOS.CloudApi/IndyPOS.CloudApi.csproj`, inside the existing `PackageReference` group:

```xml
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.6">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
```

`PrivateAssets` keeps the design-time tooling out of the published image.

- [ ] **Step 3: Add the design-time factory**

Without this, `dotnet ef` executes `Program.cs`, where Aspire's `AddNpgsqlDbContext` demands a real
`cloud-db` connection string and Task 3's guard demands a signing key. Neither exists when generating
a migration.

Create `src/IndyPOS.CloudApi/Infrastructure/CloudDbContextDesignTimeFactory.cs`:

```csharp
namespace IndyPOS.CloudApi.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

/// <summary>
/// Lets `dotnet ef` build the model without executing Program.cs, whose Aspire component demands a
/// real "cloud-db" connection string and whose startup guard demands a configured signing key.
/// The connection string below is never connected to — generating a migration needs only the
/// provider, so that Npgsql decides the column types.
/// </summary>
public sealed class CloudDbContextDesignTimeFactory : IDesignTimeDbContextFactory<CloudDbContext>
{
    public CloudDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CloudDbContext>()
            .UseNpgsql("Host=design-time-only;Database=indypos_cloud;Username=none;Password=none")
            .Options;

        return new CloudDbContext(options);
    }
}
```

- [ ] **Step 4: Add the migrate extension**

Create `src/IndyPOS.CloudApi/Infrastructure/CloudDbContextExtensions.cs`, mirroring
`src/IndyPOS.Infrastructure/Persistence/StoreHub/StoreHubDbContextExtensions.cs`:

```csharp
namespace IndyPOS.CloudApi.Infrastructure;

using Microsoft.EntityFrameworkCore;

public static class CloudDbContextExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations. This is the production path for provisioning the schema;
    /// development uses EnsureCreated for speed. Kept off the normal start path so the compose
    /// one-shot either succeeds or fails before the API is allowed to start.
    /// </summary>
    public static async Task MigrateCloudDatabaseAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CloudDbContext>();
        await db.Database.MigrateAsync();
    }
}
```

If the compiler cannot see `IHost`, `CreateScope` or `GetRequiredService`, add:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
```

- [ ] **Step 5: Generate the migration — do not hand-write it**

```bash
dotnet ef migrations add InitialCloudSchema \
  --project src/IndyPOS.CloudApi \
  --output-dir Infrastructure/Migrations
```

Expected: three files under `src/IndyPOS.CloudApi/Infrastructure/Migrations/`. Read the generated
`Up()` and confirm it creates the nine application tables — and, if Task 4 chose `UseOpenIddict()`,
the four `OpenIddict*` tables too.

- [ ] **Step 6: Verify the migration applies to an empty database**

```bash
docker run -d --name mig-pg -e POSTGRES_PASSWORD=mig -e POSTGRES_DB=cloud_mig \
  -p 5435:5432 postgres:16-alpine
```

```bash
ConnectionStrings__cloud-db="Host=localhost;Port=5435;Database=cloud_mig;Username=postgres;Password=mig" \
dotnet ef database update --project src/IndyPOS.CloudApi
```

```bash
docker exec mig-pg psql -U postgres -d cloud_mig -c "\dt"
```

Expected: every table from Step 5's `Up()`, plus `__EFMigrationsHistory`.

- [ ] **Step 7: Add the `migrate` verb**

In `src/IndyPOS.CloudApi/Program.cs`, replace the Development-only block at lines 106-112:

```csharp
// Provision the database. Dev uses EnsureCreated for speed; production applies EF migrations, and
// only when invoked explicitly as "migrate" so schema work never sits on the normal start path.
// The compose one-shot runs this and must exit 0 before the API container is allowed to start.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CloudDbContext>();
    await db.Database.EnsureCreatedAsync();
}
else if (Array.Exists(args, a => string.Equals(a, "migrate", StringComparison.OrdinalIgnoreCase)))
{
    await app.MigrateCloudDatabaseAsync();
    return;
}
```

Note the asymmetry, which is deliberate and matches StoreHub: in Development, `migrate` is ignored in
favour of `EnsureCreated`. The compose stack runs as Production, so it takes the migration path.

- [ ] **Step 8: Verify the verb end to end against a fresh database**

```bash
docker exec mig-pg psql -U postgres -c "drop database cloud_mig;"
docker exec mig-pg psql -U postgres -c "create database cloud_mig;"
```

```bash
ConnectionStrings__cloud-db="Host=localhost;Port=5435;Database=cloud_mig;Username=postgres;Password=mig" \
ASPNETCORE_ENVIRONMENT=Production \
LocalToken__SecretKey="$(openssl rand -base64 64 | tr -d '\n')" \
dotnet run --project src/IndyPOS.CloudApi --no-launch-profile -- migrate
```

Expected: the process applies the migration and **exits 0 without starting a web server** — no
"Now listening on" line. Confirm with `\dt` again, then tear down:

```bash
docker rm -f mig-pg
```

- [ ] **Step 9: Run the full suite**

```bash
dotnet test 2>&1 | tail -30
```

Expected: unchanged from Task 3 Step 11. Nothing here touches a test, so a delta means something
unintended moved.

- [ ] **Step 10: Commit**

```bash
git add src/IndyPOS.CloudApi/
git commit -m "feat: give CloudApi a production schema path

Adds the initial CloudDbContext migration and a \"migrate\" verb, shaped
like StoreHub's: schema work stays off the normal start path, so the
compose one-shot either succeeds or fails before the API starts.

A design-time factory keeps dotnet ef from executing Program.cs, where
Aspire's component wants a real connection string and the startup guard
wants a signing key -- neither of which exists when generating a
migration.

Verified by applying the migration to an empty Postgres and by running
the verb against a fresh database: it exits 0 without starting a server."
```

---

## Task 6: The Dockerfile (PR 3)

**Files:**
- Create: `src/IndyPOS.CloudApi/Dockerfile`
- Create: `.dockerignore` (repo root)

**Interfaces:**
- Consumes: `MigrateCloudDatabaseAsync` via the `migrate` argument from Task 5.
- Produces: one Dockerfile whose entrypoint is `dotnet IndyPOS.CloudApi.dll`, listening on **8080**,
  with `HEALTHCHECK` on `/health/live`, and which accepts `migrate` as a command argument. Task 7
  builds it under two tags from this same file — `indypos-cloudapi:local` for the local stack and
  `indypos-cloudapi:prod` for the Droplet — and depends on that port and command shape.

- [ ] **Step 1: Create the branch**

```bash
git checkout development
git pull                       # PR 2 must be merged first
git checkout -b feat/cloudapi-docker
```

- [ ] **Step 2: Write `.dockerignore` first**

Without it the build context includes the `bin/Debug/net10.0` trees, the git history and the
gitignored `.claude/` directory. Create `.dockerignore` at the repo root:

```gitignore
**/bin/
**/obj/
**/.vs/
**/*.user
.git/
.github/
.claude/
.planning/
publish/
tests/
installer/
docs/
fonts/
deploy/cloud/.env
**/*.md
```

- [ ] **Step 3: Write the Dockerfile**

Create `src/IndyPOS.CloudApi/Dockerfile`. The project graph is CloudApi → {Application → Domain,
ServiceDefaults}, so exactly four csproj files go in the restore layer.

```dockerfile
# Build context is the REPOSITORY ROOT, not this directory:
#   docker build -f src/IndyPOS.CloudApi/Dockerfile -t indypos-cloudapi:local .
# CloudApi references Application (-> Domain) and ServiceDefaults, and inherits
# Directory.Build.props, none of which are reachable from src/IndyPOS.CloudApi/.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore layer: project files only, so editing a .cs file does not re-restore.
COPY Directory.Build.props ./
COPY src/IndyPOS.CloudApi/IndyPOS.CloudApi.csproj src/IndyPOS.CloudApi/
COPY src/IndyPOS.Application/IndyPOS.Application.csproj src/IndyPOS.Application/
COPY src/IndyPOS.Domain/IndyPOS.Domain.csproj src/IndyPOS.Domain/
COPY src/IndyPOS.ServiceDefaults/IndyPOS.ServiceDefaults.csproj src/IndyPOS.ServiceDefaults/
RUN dotnet restore src/IndyPOS.CloudApi/IndyPOS.CloudApi.csproj

COPY src/IndyPOS.CloudApi/ src/IndyPOS.CloudApi/
COPY src/IndyPOS.Application/ src/IndyPOS.Application/
COPY src/IndyPOS.Domain/ src/IndyPOS.Domain/
COPY src/IndyPOS.ServiceDefaults/ src/IndyPOS.ServiceDefaults/
RUN dotnet publish src/IndyPOS.CloudApi/IndyPOS.CloudApi.csproj \
      -c Release -o /app/publish --no-restore

# Debian, not Alpine: this system converts timestamps to and from Asia/Bangkok and handles Thai
# product names, and the Alpine images ship neither ICU nor tzdata without extra packages.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# curl is the probe binary for HEALTHCHECK and for compose's service_healthy condition. The aspnet
# image ships neither curl nor wget, which is also why the chiseled variants are unusable here.
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish ./

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Non-root. The aspnet images define APP_UID for exactly this.
USER $APP_UID

# /health/live, not /health/ready: it is the cheap probe by design, and it is mapped exactly once
# (see defect I0-C in the spec).
HEALTHCHECK --interval=15s --timeout=3s --start-period=20s --retries=3 \
  CMD curl -fsS http://localhost:8080/health/live || exit 1

# Entrypoint without the command, so "migrate" can be passed as an argument by the compose one-shot
# rather than needing a second image.
ENTRYPOINT ["dotnet", "IndyPOS.CloudApi.dll"]
```

- [ ] **Step 4: Build the image**

```bash
docker build -f src/IndyPOS.CloudApi/Dockerfile -t indypos-cloudapi:local .
```

Expected: succeeds. This is also the first proof that `Nokpirab` restores inside the container from
nuget.org, with no private feed and no credentials.

- [ ] **Step 5: Confirm the image runs as non-root and has the probe binary**

```bash
docker run --rm --entrypoint sh indypos-cloudapi:local -c "id -u; curl --version | head -1"
```

Expected: a non-zero uid, and a curl version line.

- [ ] **Step 6: Commit**

```bash
git add .dockerignore src/IndyPOS.CloudApi/Dockerfile
git commit -m "build: add a Dockerfile for CloudApi

Multi-stage, non-root, listening on 8080, no in-container HTTPS. Build
context is the repo root because CloudApi's project graph reaches
Application, Domain and ServiceDefaults and inherits
Directory.Build.props.

Debian rather than Alpine: the system converts timestamps to Asia/Bangkok
and handles Thai text, and the Alpine images carry neither ICU nor tzdata
by default. curl is installed so HEALTHCHECK has a probe binary, which
also rules out the chiseled variants."
```

---

## Task 7: The compose stacks and the runbook (PR 3)

**Files:**
- Create: `deploy/cloud/compose.yaml`
- Create: `deploy/cloud/compose.prod.yaml`
- Create: `deploy/cloud/.env.example`
- Create: `docs/operations/cloud-deployment.md`
- Modify: `.gitignore`

**Interfaces:**
- Consumes: the `indypos-cloudapi:local` image, port 8080 and the `migrate` command from Task 6.
- Produces: the deployment unit I1–I3 use. No further tasks depend on it.

- [ ] **Step 1: Ignore the real env file before writing anything that could hold a secret**

Append to `.gitignore`:

```gitignore
# Cloud deployment secrets — .env.example is the committed template
deploy/cloud/.env
```

Verify:

```bash
mkdir -p deploy/cloud && touch deploy/cloud/.env && git check-ignore -v deploy/cloud/.env
```

Expected: a line naming the rule. If it prints nothing, the file is **not** ignored — stop and fix it.

- [ ] **Step 2: Write the committed template**

Create `deploy/cloud/.env.example`. Every value here is a placeholder; none is a real secret.

```dotenv
# Copy to .env and fill in real values:  cp .env.example .env
# .env is gitignored. This repository is public — never commit a real secret.

# Production even for the local stack: it is the path that actually ships, so it is the path worth
# verifying. The cost is no Scalar/OpenAPI UI locally.
ASPNETCORE_ENVIRONMENT=Production

# Signs the tokens guarding /admin/*. The host refuses to start on the built-in default.
#   openssl rand -base64 64
LocalToken__SecretKey=replace-me-not-a-real-secret-0000000000000000000000000000

# Base64 of a PEM RSA private key, 2048 bits or more. Generate with scripts/generate-rsa-key.ps1.
# Left empty, OpenIddict falls back to ephemeral dev certificates and every restart invalidates
# every issued token — acceptable locally, never in production.
INDYPOS_RSA_SIGNING_KEY=

# The managed PostgreSQL cluster. Note the hyphen in the key: it must match
# AddNpgsqlDbContext<CloudDbContext>("cloud-db"). compose.yaml overrides this for the local stack.
ConnectionStrings__cloud-db=
```

- [ ] **Step 3: Write the local stack**

Create `deploy/cloud/compose.yaml`. It lives here rather than at the repo root because Compose
prefers `compose.yaml` over `docker-compose.yml`, and a root file would silently shadow the existing
StoreHub dev stack.

```yaml
# Local full stack. Run from this directory:
#   docker compose up --build
# Postgres is published on 5433 because the StoreHub dev stack at the repo root already holds 5432.
name: indypos-cloud

x-cloudapi: &cloudapi
  build:
    context: ../..
    dockerfile: src/IndyPOS.CloudApi/Dockerfile
  image: indypos-cloudapi:local
  env_file: .env.example
  environment:
    ConnectionStrings__cloud-db: "Host=postgres;Port=5432;Database=indypos_cloud;Username=cloud_app;Password=cloud_dev_password"

services:
  postgres:
    image: postgres:16-alpine
    container_name: indypos-cloud-postgres
    restart: unless-stopped
    environment:
      POSTGRES_USER: cloud_app
      POSTGRES_PASSWORD: cloud_dev_password
      POSTGRES_DB: indypos_cloud
    ports:
      - "5433:5432"
    volumes:
      - cloud_postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U cloud_app -d indypos_cloud"]
      interval: 10s
      timeout: 5s
      retries: 5

  # One-shot. Applies EF migrations and exits; the API is not allowed to start until it exits 0.
  cloud-api-migrate:
    <<: *cloudapi
    container_name: indypos-cloud-api-migrate
    command: ["migrate"]
    restart: "no"
    depends_on:
      postgres:
        condition: service_healthy

  cloud-api:
    <<: *cloudapi
    container_name: indypos-cloud-api
    restart: unless-stopped
    ports:
      - "8080:8080"
    depends_on:
      cloud-api-migrate:
        condition: service_completed_successfully

volumes:
  cloud_postgres_data:
```

- [ ] **Step 4: Bring the stack up**

```bash
cd deploy/cloud && docker compose up --build
```

Expected, in order: postgres becomes healthy → `cloud-api-migrate` applies the migration and exits 0
→ `cloud-api` starts and reports healthy. If the API starts before the migration finishes, the
`depends_on` condition is wrong.

- [ ] **Step 5: Check the endpoints, and record the two open observations**

```bash
curl -i http://localhost:8080/
curl -i http://localhost:8080/health/live
curl -i http://localhost:8080/health/ready
curl -i http://localhost:8080/sync/status
docker inspect --format '{{.State.Health.Status}}' indypos-cloud-api
```

Expected: `/` returns `IndyPOS Cloud API`; `/health/live` returns 200; `/sync/status` returns the
counters; the container reports `healthy`.

**Record, do not assume** — these are the spec's two open observations:
1. What `GET /health/ready` actually does. Two mappings share that route
   (`ServiceDefaults/Extensions.cs:86` and `CloudApi/Program.cs:183`), so an
   `AmbiguousMatchException` is plausible but unverified.
2. Whether any `ready`-tagged check exists behind it — `AddDefaultHealthChecks` registers only
   `self`, tagged `live`.

Write both findings into the PR description. If `/health/ready` throws, that is a new defect to
report, **not** something to fix inside this packaging PR.

- [ ] **Step 6: Prove the token flow works inside the container**

Generate a real RSA key into `.env` so this exercises the production key path rather than ephemeral
certificates:

```bash
pwsh -File ../../scripts/generate-rsa-key.ps1
```

Read that script's output for where it writes the key and in what form, put it in `deploy/cloud/.env`
as `INDYPOS_RSA_SIGNING_KEY`, then `docker compose up -d --force-recreate cloud-api` and repeat the
client-credentials exchange from Task 4 Step 4 against `http://localhost:8080/oauth/token`.

Expected: `200` with an `access_token`. Then restart the container and confirm a previously issued
token is still accepted — that is what distinguishes a real key from an ephemeral certificate.

- [ ] **Step 7: Confirm the guard is live in the container**

```bash
docker compose run --rm -e LocalToken__SecretKey="IndyPOS-StoreHub-Local-Auth-Secret-Key-2026" cloud-api
```

Expected: the container exits with the guard's `InvalidOperationException`. This is the containerized
proof of Task 3.

- [ ] **Step 8: Tear down and commit the local stack**

```bash
docker compose down -v
cd ../..
git add .gitignore deploy/cloud/.env.example deploy/cloud/compose.yaml
git commit -m "build: add a local compose stack for CloudApi

Postgres -> migrate one-shot -> API, gated on
service_completed_successfully so the API never starts against an
unmigrated database. Runs as Production deliberately: that is the path
that ships, so it is the path worth verifying locally.

Lives under deploy/cloud/ because Compose prefers compose.yaml over
docker-compose.yml, so a root file would silently shadow the existing
StoreHub dev stack."
```

- [ ] **Step 9: Write the production stack**

Create `deploy/cloud/compose.prod.yaml`. No database service — that is the managed cluster.

```yaml
# Droplet deployment unit. On the box:
#   docker compose -f compose.prod.yaml up -d --build
# Requires a .env alongside this file (see .env.example). Never commit that file.
name: indypos-cloud

x-cloudapi: &cloudapi
  build:
    context: ../..
    dockerfile: src/IndyPOS.CloudApi/Dockerfile
  image: indypos-cloudapi:prod
  env_file: .env
  logging:
    driver: json-file
    options:
      # The Droplet has a 50 GB disk. An uncapped container log is a slow outage.
      max-size: "10m"
      max-file: "5"

services:
  # One-shot. Must exit 0 before the API is allowed to start.
  cloud-api-migrate:
    <<: *cloudapi
    container_name: indypos-cloud-api-migrate
    command: ["migrate"]
    restart: "no"

  cloud-api:
    <<: *cloudapi
    container_name: indypos-cloud-api
    restart: unless-stopped
    # TLS TERMINATOR SEAM -----------------------------------------------------------------
    # Bound to loopback on purpose: nothing here terminates TLS, and no domain is registered
    # yet. The infrastructure guide's firewall opens 443 publicly and 22 to trusted IPs only.
    # A terminator must: listen on 443 with a certificate for the API's domain, forward to
    # this container's 8080, and set X-Forwarded-Proto / X-Forwarded-For. When a domain
    # exists, add a Caddy service on an internal network and drop this ports mapping.
    # -------------------------------------------------------------------------------------
    ports:
      - "127.0.0.1:8080:8080"
    depends_on:
      cloud-api-migrate:
        condition: service_completed_successfully
```

- [ ] **Step 10: Verify the production file is valid and resolves as intended**

`docker compose config` renders the merged file without starting anything.

```bash
cd deploy/cloud
cp .env.example .env
docker compose -f compose.prod.yaml config
```

Expected: valid YAML output; `cloud-api` published on `127.0.0.1:8080`; both services carrying the
`env_file` values; no `postgres` service. Then remove the throwaway env file:

```bash
rm .env
```

- [ ] **Step 11: Write the runbook**

Create `docs/operations/cloud-deployment.md`. Keep it to what these files actually do — the full
guide is Epic I task I8.

```markdown
# CloudApi deployment

Covers the container and the two compose stacks in `deploy/cloud/`. Provisioning the Droplet and the
managed database is Epic I tasks I1–I3; the full operations guide is task I8.

## Run it locally

```bash
cd deploy/cloud
docker compose up --build
```

Brings up PostgreSQL on 5433 (5432 belongs to the StoreHub dev stack at the repo root), applies EF
migrations in a one-shot container, then starts the API on <http://localhost:8080>.

The stack runs as `Production` on purpose — that is the path that ships. There is no Scalar/OpenAPI
UI as a result; for that, use the Aspire dev loop instead.

## Run it on the Droplet

1. Copy the repository to the box, or build the image elsewhere and push it to a registry.
2. `cp deploy/cloud/.env.example deploy/cloud/.env` and fill in every value.
   - `ConnectionStrings__cloud-db` — the managed cluster's string. The hyphen matters.
   - `LocalToken__SecretKey` — `openssl rand -base64 64`. The API refuses to start without it.
   - `INDYPOS_RSA_SIGNING_KEY` — from `scripts/generate-rsa-key.ps1`. Without it, every restart
     invalidates every issued token.
3. `cd deploy/cloud && docker compose -f compose.prod.yaml up -d --build`

The migrate one-shot runs first and must exit 0; the API will not start otherwise.

## Health

- `/health/live` — process up. This is what the container's `HEALTHCHECK` probes.
- `/health/ready` — currently mapped twice (`ServiceDefaults` and `Program.cs`) and has no
  `ready`-tagged check behind it. Do not rely on it until that is resolved.

## TLS

Nothing in these files terminates TLS, and no domain is registered yet, so `compose.prod.yaml` binds
the API to `127.0.0.1:8080`. See the marked seam in that file for what a terminator must do.

## Schema changes

Forward-only, same gate as StoreHub: additive, nullable or defaulted, no renames or drops. See
`docs/operations/upgrade-procedure.md`. Generate migrations with:

```bash
dotnet ef migrations add <Name> --project src/IndyPOS.CloudApi --output-dir Infrastructure/Migrations
```

Never hand-write one.
```

- [ ] **Step 12: Run the full suite one last time**

```bash
cd ../.. && dotnet test 2>&1 | tail -30
```

Expected: unchanged from Task 5. PR 3 touches no C#, so any delta is unrelated and worth reporting.

- [ ] **Step 13: Confirm no secret is staged**

```bash
git status --short
git diff --cached --stat
```

Expected: no `.env`, no real connection string, no real key. Then:

```bash
git add deploy/cloud/compose.prod.yaml docs/operations/cloud-deployment.md
git commit -m "build: add the Droplet compose stack and a deployment runbook

CloudApi plus the migrate one-shot only -- the database is the managed
cluster. Bound to loopback with a marked TLS-terminator seam, because no
domain is registered yet and nothing here terminates TLS. Container logs
are capped: the Droplet has a 50 GB disk.

The runbook covers only what these files do; the full cloud operations
guide remains Epic I task I8."
```

---

## Wrap-up

- [ ] **Update `.claude/STATUS.md`** — Epic I task I0 done, I1–I3 unblocked, plus any new defect the
  Task 7 Step 5 observations turned up. That file is gitignored; never stage it.
- [ ] **Update `CLAUDE.md`'s test counts** if Task 3 changed the solution total.
- [ ] **Report the four recorded findings** in the PR descriptions: the Task 4 probe output, the
  `/health/ready` behaviour, whether a `ready`-tagged check exists, and the observed token lifetime
  across a container restart.
