# Health-Check Convention Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Every health caller uses a route production has. Each service's `/health/ready` runs exactly one bounded `SELECT 1` against its own database, StoreHub never waits on the cloud, no probe leaks errors or hangs, the dead `StoreHubUpdateService` is gone, and the convention is written down.

**Architecture:** One helper in `IndyPOS.ServiceDefaults` (`AddDatabaseReadinessCheck`) registers Xabaril's Npgsql check with a 3-second Npgsql connect timeout, and `MapDefaultEndpoints` adds a 5-second request timeout and the Development-only JSON writer. The shared behaviour is tested once on a slim host in a new `IndyPOS.ServiceDefaults.Tests`, and each service gets wiring tests. The till's wizard moves its check into a testable `StoreHubConnectionCheck`. The installer's `HealthProbe` polls until a 60-second deadline. AppHost waits on real readiness and stops making StoreHub wait for CloudApi.

**Tech Stack:** .NET 10, ASP.NET Core health checks, `AspNetCore.HealthChecks.NpgSql` 9.0.0, `AspNetCore.HealthChecks.UI.Client` 9.0.0, Npgsql 10.0.0, Aspire.Hosting 9.3.0, xUnit, FluentAssertions 8 (solution) / 6 (installer), Moq, `TestPostgres` seam.

**Spec:** `docs/superpowers/specs/2026-10-06-health-check-convention-design.md` (revised 2026-10-07; approved by Pond).

## Prerequisites

- Branch `feat/health-check-convention`, rebased on `development` at or after `f437ac1` (it already holds the spec commits).
- **Docker running**, or `INDYPOS_TEST_POSTGRES` set (ONBOARDING.md). StoreHub, CloudApi and the new ServiceDefaults suites need a Postgres.
- Line numbers below are those at `f437ac1`. Find each block by the quoted text.

## Global Constraints

- [§2] "Production `/health/live` and `/health/ready` stay anonymous and say only `Healthy`/`Unhealthy`." Detailed JSON only on the Development-only `/health`.
- [§2] "StoreHub's `ready` checks **only its local database**. It never includes a cloud check, and StoreHub never waits on CloudApi, in production or in Aspire."
- [§3] "Route shapes do not change."
- [§3] The check's connection string sets **`Timeout=3`** and **`CommandTimeout=3`**. The registration timeout is 3 s and the request timeout is 5 s; both are backstops.
- [§4.1] Check name **`database`**, tag **`ready`**; a missing connection string → 500, accepted and not guarded.
- [§4.2] Both `AddNpgsqlDbContext` calls set `DisableHealthChecks = true`: exactly one database check per service.
- [§4.2] `app.UseRequestTimeouts()` is called **explicitly in each `Program.cs`**, not inside `MapDefaultEndpoints`.
- [§4.4] Rethrow `OperationCanceledException` when the caller's token is cancelled, before any broad catch (wizard check and installer probe).
- **Forward-only / no migration.** No schema changes in this plan.
- **Test style:**
  - names are `Subject_WhenScenario_DirectVerbOutcome`;
  - one behaviour per test;
  - negative cases first;
  - Arrange/Act/Assert separated by blank lines;
  - named constants for magic values;
  - FluentAssertions chains on separate lines with the dots aligned.
- **Comments must match behaviour**, with no planning labels in code.
- **Diagrams are pad-only:** keep each changed ASCII line's exact character width.
- **Commits:** conventional, at least one per task; the deletion in Task 1 is its own commit.

## Review Focus

These are the inputs the spec implies but no other task's tests exercise, most likely first. Each has its test in the task named:

1. **A production connection string that already sets its own `Timeout`** (e.g. `Timeout=15` written by the installer). Expected: the check still answers a hanging server within 5 s, because the helper overrides it for the check only. → Task 2 (`ReadyProbe_WithAConnectionStringThatSetsALongerTimeout_StillAnswersWithinFiveSeconds`).
2. **The wizard pointed at a non-default `StoreHub:BaseUrl`** (dev `:5012`, or a changed port). Expected: it probes `{BaseUrl}/health/ready`, never `localhost:5000`. → Task 5 (`CheckAsync_WithAConfiguredBaseUrl_ProbesItsReadyRoute`).
3. **The wizard closed while a check is in flight.** Expected: cancellation surfaces as `OperationCanceledException`, never as "timed out". → Task 5 (`CheckAsync_WithACancelledToken_ThrowsOperationCanceled`).
4. **An upgrade right after a reboot, with PostgreSQL still starting.** Expected: the probe keeps trying until 60 s, not 5 quick failures. → Task 6 (`IsReadyAsync_WhenNotReadyAtFirst_KeepsPollingUntilReady`).
5. **StoreHub started in Aspire with CloudApi stopped.** Expected: StoreHub still starts and turns healthy. → Task 7, by hand, recorded in the PR.

---

## File Structure

```
src/IndyPOS.ServiceDefaults/
  IndyPOS.ServiceDefaults.csproj          MODIFY  + NpgSql 9.0.0, UI.Client 9.0.0, Npgsql 10.0.0
  Extensions.cs                           MODIFY  timeout policy, writer, mapping
  DatabaseReadinessCheck.cs               CREATE  AddDatabaseReadinessCheck helper
src/IndyPOS.StoreHub/Program.cs           MODIFY  helper, DisableHealthChecks, UseRequestTimeouts, comments
src/IndyPOS.StoreHub/IndyPOS.StoreHub.csproj  MODIFY  drop HealthChecks.EntityFrameworkCore
src/IndyPOS.CloudApi/Program.cs           MODIFY  helper, DisableHealthChecks, delete MapGet, UseRequestTimeouts, partial Program
src/IndyPOS.AppHost/Program.cs            MODIFY  WithHttpHealthCheck, drop WaitFor(cloudApi)
src/IndyPOS.Application/Abstractions/StoreHub/
  StoreHubRoutes.cs                       CREATE  HealthReady constant
  IStoreHubConnectionCheck.cs             CREATE  interface + status + result
src/IndyPOS.Infrastructure/Services/StoreHub/
  StoreHubConnectionCheck.cs              CREATE  typed HttpClient check
  StoreHubHttpClient.cs                   MODIFY  IsHealthyAsync uses the constant
src/IndyPOS.Infrastructure/ConfigureServices.cs  MODIFY  register the check
src/IndyPOS.Windows.Forms/UI/Setup/FirstRunWizard.cs  MODIFY  use the check
src/IndyPOS.Windows.Forms/Services/StoreHubUpdateService.cs  DELETE
src/IndyPOS.Windows.Forms/ConfigureServices.cs  MODIFY  drop its registration
installer/IndyPOS.Bootstrapper/Installers/HealthProbe.cs  MODIFY  deadline, OCE, comment
tests/IndyPOS.ServiceDefaults.Tests/      CREATE  project + HealthTestHost + HealthEndpointTests
tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/HealthEndpointTests.cs  CREATE
tests/IndyPOS.CloudApi.IntegrationTests/HealthEndpointTests.cs            CREATE (time-boxed, Task 4)
tests/IndyPOS.Application.Tests/Integration/StoreHub/StoreHubConnectionCheckTests.cs  CREATE
tests/IndyPOS.Bootstrapper.Tests/Installers/HealthProbeTests.cs           CREATE
IndyPOS.sln                               MODIFY  add ServiceDefaults.Tests under Tests/Services
docs/... (Task 8)                         MODIFY
```

---

### Task 1: Delete the dead `StoreHubUpdateService`

**Files:**
- Delete: `src/IndyPOS.Windows.Forms/Services/StoreHubUpdateService.cs` (it holds `IStoreHubUpdateService`, `StoreHubUpdateService` and `StoreHubUpdateProgress`)
- Modify: `src/IndyPOS.Windows.Forms/ConfigureServices.cs:56`

**Interfaces:** none produced; nothing consumes these types.

- [ ] **Step 1: Prove nothing uses it**

Run: `git grep -n "StoreHubUpdateService\|IStoreHubUpdateService\|StoreHubUpdateProgress" -- src tests`
Expected: only `Services/StoreHubUpdateService.cs` itself and `ConfigureServices.cs:56`. If anything else appears, stop and ask.

- [ ] **Step 2: Delete it and its registration**

```bash
git rm src/IndyPOS.Windows.Forms/Services/StoreHubUpdateService.cs
```

In `ConfigureServices.cs`, delete the line `services.AddSingleton<IStoreHubUpdateService, StoreHubUpdateService>();`. If the line directly above it is a comment that only describes this registration, delete that comment too.

- [ ] **Step 3: Build and test the till**

Run: `dotnet build src/IndyPOS.Windows.Forms` → 0 errors. Then `dotnet test tests/IndyPOS.Windows.Forms.Tests` → 50/50 pass.

- [ ] **Step 4: Commit**

```bash
git add -A src/IndyPOS.Windows.Forms
git commit -m "refactor(winforms): delete the unused StoreHubUpdateService" -m "Nothing resolved it; the till updates through Velopack. Wired up, it would have stopped StoreHub, downloaded a zip from the old ponggun/IndyPOS repository and overwritten StoreHub, around the installer's backup and rollback. It also probed /health, which production does not map."
```

---

### Task 2: The shared readiness check and probe mapping, tested on a slim host

**Files:**
- Modify: `src/IndyPOS.ServiceDefaults/IndyPOS.ServiceDefaults.csproj`, `src/IndyPOS.ServiceDefaults/Extensions.cs`
- Create: `src/IndyPOS.ServiceDefaults/DatabaseReadinessCheck.cs`
- Create: `tests/IndyPOS.ServiceDefaults.Tests/IndyPOS.ServiceDefaults.Tests.csproj`, `HealthTestHost.cs`, `HangingServer.cs`, `HealthEndpointTests.cs`
- Modify: `IndyPOS.sln`

**Interfaces:**
- Produces:
  - `IndyPOS.ServiceDefaults.DatabaseReadinessCheck.AddDatabaseReadinessCheck<TBuilder>(this TBuilder builder, string connectionName) where TBuilder : IHostApplicationBuilder`, which returns `builder`;
  - constants `DatabaseReadinessCheck.Name = "database"` and `Extensions.ReadyTag = "ready"`, `Extensions.LiveTag = "live"`, `Extensions.ProbeTimeoutPolicy = "health-probe"`.
- Consumed by Tasks 3 and 4.

- [ ] **Step 1: Create the test project**

`tests/IndyPOS.ServiceDefaults.Tests/IndyPOS.ServiceDefaults.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <PackageReference Include="FluentAssertions" Version="8.8.0" />
    <PackageReference Include="Microsoft.AspNetCore.TestHost" Version="10.0.5" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.3.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\IndyPOS.ServiceDefaults\IndyPOS.ServiceDefaults.csproj" />
    <ProjectReference Include="..\IndyPOS.Testing.Postgres\IndyPOS.Testing.Postgres.csproj" />
  </ItemGroup>

</Project>
```

Add it to the solution under the existing **Tests → Services** folder (GUID `{A1B2C3D4-6666-6666-6666-000000000063}`):

Run: `dotnet sln IndyPOS.sln add tests/IndyPOS.ServiceDefaults.Tests/IndyPOS.ServiceDefaults.Tests.csproj --solution-folder Tests/Services`
Then: `git diff IndyPOS.sln`. **Expected:** one new `Project(...)` entry, and a `NestedProjects` line mapping it to `{A1B2C3D4-6666-6666-6666-000000000063}`. If `dotnet sln` created new `Tests`/`Services` folders instead, delete those folder entries and point the `NestedProjects` line at `...0063` by hand.

`HangingServer.cs` (a TCP server that accepts connections and never answers):

```csharp
using System.Net;
using System.Net.Sockets;

namespace IndyPOS.ServiceDefaults.Tests;

/// <summary>
/// Accepts TCP connections and never answers, like a database host that has hung. Npgsql's connect
/// phase ignores cancellation, so only the connection string's own Timeout can end a check against it.
/// </summary>
internal sealed class HangingServer : IDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly List<Socket> _held = [];

    public HangingServer()
    {
        _listener.Start();
        _ = AcceptForeverAsync();
    }

    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    public string ConnectionString(string extra = "") =>
        $"Host=127.0.0.1;Port={Port};Username=probe;Password=probe;Database=probe;{extra}";

    public static string ClosedPortConnectionString()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return $"Host=127.0.0.1;Port={port};Username=probe;Password=probe;Database=probe";
    }

    private async Task AcceptForeverAsync()
    {
        try
        {
            while (true)
                _held.Add(await _listener.AcceptSocketAsync());
        }
        catch (ObjectDisposedException)
        {
        }
        catch (SocketException)
        {
        }
    }

    public void Dispose()
    {
        _listener.Stop();
        foreach (var socket in _held)
            socket.Dispose();
    }
}
```

`HealthTestHost.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

namespace IndyPOS.ServiceDefaults.Tests;

/// <summary>
/// The smallest app that wires health the way StoreHub and CloudApi do, so the shared behaviour is
/// tested once, without either service's own startup.
/// </summary>
internal sealed class HealthTestHost : IAsyncDisposable
{
    public const string ConnectionName = "test-db";

    private readonly WebApplication _app;

    private HealthTestHost(WebApplication app)
    {
        _app = app;
        Client = app.GetTestClient();
    }

    public HttpClient Client { get; }

    public static async Task<HealthTestHost> StartAsync(string connectionString, string environment = "Production")
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.WebHost.UseTestServer();
        builder.Configuration[$"ConnectionStrings:{ConnectionName}"] = connectionString;
        builder.AddServiceDefaults();
        builder.AddDatabaseReadinessCheck(ConnectionName);

        var app = builder.Build();
        app.UseRequestTimeouts();
        app.MapDefaultEndpoints();
        await app.StartAsync();

        return new HealthTestHost(app);
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.DisposeAsync();
    }
}
```

- [ ] **Step 2: Write the failing tests**

`HealthEndpointTests.cs`:

```csharp
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using FluentAssertions;
using IndyPOS.Testing.Postgres;
using Xunit;

namespace IndyPOS.ServiceDefaults.Tests;

public class HealthEndpointTests : IAsyncLifetime
{
    private const string Ready = "/health/ready";
    private const string Live = "/health/live";
    private const string Detailed = "/health";
    private static readonly TimeSpan ProbeBudget = TimeSpan.FromSeconds(5);

    private TestPostgres _postgres = null!;

    public async Task InitializeAsync() => _postgres = await TestPostgres.StartAsync();

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task ReadyProbe_WithAnUnreachableDatabase_ReturnsServiceUnavailable()
    {
        await using var host = await HealthTestHost.StartAsync(HangingServer.ClosedPortConnectionString());

        var response = await host.Client.GetAsync(Ready);

        response.StatusCode.Should()
                           .Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task ReadyProbe_WithAHangingDatabase_AnswersWithinFiveSeconds()
    {
        using var server = new HangingServer();
        await using var host = await HealthTestHost.StartAsync(server.ConnectionString());
        var clock = Stopwatch.StartNew();

        var response = await host.Client.GetAsync(Ready);

        clock.Elapsed.Should()
                     .BeLessThan(ProbeBudget, "Npgsql's default 15 s connect timeout must not reach the caller");
        response.StatusCode.Should()
                           .Be(HttpStatusCode.ServiceUnavailable);
    }

    // An installer-written string may carry its own, longer Timeout; the check must still be bounded.
    [Fact]
    public async Task ReadyProbe_WithAConnectionStringThatSetsALongerTimeout_StillAnswersWithinFiveSeconds()
    {
        using var server = new HangingServer();
        await using var host = await HealthTestHost.StartAsync(server.ConnectionString("Timeout=30"));
        var clock = Stopwatch.StartNew();

        await host.Client.GetAsync(Ready);

        clock.Elapsed.Should()
                     .BeLessThan(ProbeBudget);
    }

    [Fact]
    public async Task ReadyProbe_WithAFailingDatabase_DoesNotLeakTheError()
    {
        await using var host = await HealthTestHost.StartAsync(HangingServer.ClosedPortConnectionString());

        var body = await (await host.Client.GetAsync(Ready)).Content.ReadAsStringAsync();

        body.Should()
            .Be("Unhealthy");
    }

    [Fact]
    public async Task LiveProbe_WithAnUnreachableDatabase_ReturnsOk()
    {
        await using var host = await HealthTestHost.StartAsync(HangingServer.ClosedPortConnectionString());

        var response = await host.Client.GetAsync(Live);

        response.StatusCode.Should()
                           .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DetailedHealth_OutsideDevelopment_ReturnsNotFound()
    {
        await using var host = await HealthTestHost.StartAsync(_postgres.ConnectionString);

        var response = await host.Client.GetAsync(Detailed);

        response.StatusCode.Should()
                           .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReadyProbe_WithTheDatabaseUp_ReturnsOk()
    {
        await using var host = await HealthTestHost.StartAsync(_postgres.ConnectionString);

        var response = await host.Client.GetAsync(Ready);

        response.StatusCode.Should()
                           .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DetailedHealth_InDevelopment_ReportsTheDatabaseCheck()
    {
        await using var host = await HealthTestHost.StartAsync(_postgres.ConnectionString, "Development");

        using var json = JsonDocument.Parse(await host.Client.GetStringAsync(Detailed));

        json.RootElement.GetProperty("entries")
                        .TryGetProperty(DatabaseReadinessCheck.Name, out _)
                        .Should()
                        .BeTrue();
    }
}
```

- [ ] **Step 3: Run them and watch them fail**

Run: `dotnet test tests/IndyPOS.ServiceDefaults.Tests`
Expected: **build error** "`AddDatabaseReadinessCheck` / `DatabaseReadinessCheck` does not exist". That is the RED for a missing helper.

- [ ] **Step 4: Add the packages**

In `IndyPOS.ServiceDefaults.csproj`, inside the existing `<ItemGroup>`:

```xml
    <PackageReference Include="AspNetCore.HealthChecks.NpgSql" Version="9.0.0" />
    <PackageReference Include="AspNetCore.HealthChecks.UI.Client" Version="9.0.0" />
    <!-- Match the services' Npgsql; the health-check package alone would bring 8.0.3. -->
    <PackageReference Include="Npgsql" Version="10.0.0" />
```

- [ ] **Step 5: Write the helper**

`src/IndyPOS.ServiceDefaults/DatabaseReadinessCheck.cs`:

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace IndyPOS.ServiceDefaults;

/// <summary>
/// The one readiness check a service has: a <c>SELECT 1</c> against its own database. StoreHub's must
/// never include the cloud, so it can serve the till while the cloud is unreachable.
/// </summary>
public static class DatabaseReadinessCheck
{
    public const string Name = "database";

    private const int TimeoutSeconds = 3;

    public static TBuilder AddDatabaseReadinessCheck<TBuilder>(this TBuilder builder, string connectionName)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
                        .AddNpgSql(services => BoundedConnectionString(services, connectionName),
                                   name: Name,
                                   tags: [Extensions.ReadyTag],
                                   timeout: TimeSpan.FromSeconds(TimeoutSeconds));

        return builder;
    }

    // Read on the first check, then cached by the package: by then UnprotectSecrets (production),
    // Aspire (dev) or a test factory has put the real value in IConfiguration. Npgsql's connect phase
    // ignores cancellation, so only its own Timeout bounds a hanging server; it overrides whatever
    // the string carries, for this check only. A missing string throws and the probe answers 500.
    private static string BoundedConnectionString(IServiceProvider services, string connectionName)
    {
        var configured = services.GetRequiredService<IConfiguration>().GetConnectionString(connectionName)
                         ?? throw new InvalidOperationException($"Connection string '{connectionName}' is not configured.");

        return new NpgsqlConnectionStringBuilder(configured)
        {
            Timeout = TimeoutSeconds,
            CommandTimeout = TimeoutSeconds
        }.ConnectionString;
    }
}
```

- [ ] **Step 6: Update `Extensions.cs`**

Add the usings `using HealthChecks.UI.Client;` and `using Microsoft.AspNetCore.Http;`.

Add constants at the top of the class:

```csharp
    public const string LiveTag = "live";
    public const string ReadyTag = "ready";
    public const string ProbeTimeoutPolicy = "health-probe";
```

In `AddServiceDefaults`, after `builder.AddDefaultHealthChecks();`:

```csharp
        builder.Services.AddRequestTimeouts(options =>
            options.AddPolicy(ProbeTimeoutPolicy, TimeSpan.FromSeconds(5)));
```

In `AddDefaultHealthChecks`, `["live"]` → `[LiveTag]`.

Replace the body of `MapDefaultEndpoints` (keep the method signature):

```csharp
        // Kubernetes-style probes, tag-filtered so each runs only what its meaning needs: "live" = the
        // process is up (cheap, no database), "ready" = its own database answers. Terse in every
        // environment: anonymous callers learn only Healthy or Unhealthy. The timeout is a backstop;
        // each service must call app.UseRequestTimeouts() for it to apply.
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(LiveTag)
        }).WithRequestTimeout(ProbeTimeoutPolicy);

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(ReadyTag)
        }).WithRequestTimeout(ProbeTimeoutPolicy);

        // Every check with full detail (status, duration, error text). Development only: production
        // must not hand implementation details to anonymous callers.
        if (app.Environment.IsDevelopment())
        {
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });
        }

        return app;
```

- [ ] **Step 7: Run the tests and watch them pass**

Run: `dotnet test tests/IndyPOS.ServiceDefaults.Tests`
Expected: 8/8 PASS. The two hanging-server tests take about 3 s each.

If the hanging tests take about 15 s, the `Timeout` override is not reaching Npgsql. Check that the factory overload is the one used.

- [ ] **Step 8: Commit**

```bash
git add src/IndyPOS.ServiceDefaults tests/IndyPOS.ServiceDefaults.Tests IndyPOS.sln
git commit -m "feat(health): one bounded database readiness check, terse probes with a request timeout"
```

---

### Task 3: StoreHub on the shared check

**Files:**
- Modify: `src/IndyPOS.StoreHub/Program.cs` (lines 75-82, 232-233, 261-263)
- Modify: `src/IndyPOS.StoreHub/IndyPOS.StoreHub.csproj:23`
- Create: `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/HealthEndpointTests.cs`

**Interfaces:** consumes Task 2's `AddDatabaseReadinessCheck`, `DatabaseReadinessCheck.Name`, `Extensions.LiveTag`.

- [ ] **Step 1: Write the failing tests**

```csharp
using System.Net;
using FluentAssertions;
using IndyPOS.ServiceDefaults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

[Collection("Integration")]
public class HealthEndpointTests : IntegrationTestBase
{
    private const string SelfCheck = "self";

    public HealthEndpointTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    // Aspire's EF integration would add a second, untagged database check; it must be switched off.
    [Fact]
    public void HealthChecks_WithTheAppBuilt_RegisterOneDatabaseCheck()
    {
        var names = Factory.Services.GetRequiredService<IOptions<HealthCheckServiceOptions>>()
                                    .Value.Registrations
                                    .Select(r => r.Name);

        names.Should()
             .BeEquivalentTo([SelfCheck, DatabaseReadinessCheck.Name]);
    }

    [Fact]
    public void RouteTable_WithTheAppBuilt_MapsTheReadyProbeOnce()
    {
        var readyRoutes = Factory.Services.GetRequiredService<EndpointDataSource>()
                                          .Endpoints
                                          .OfType<RouteEndpoint>()
                                          .Count(e => e.RoutePattern.RawText == "/health/ready");

        readyRoutes.Should()
                   .Be(1);
    }

    [Fact]
    public async Task ReadyProbe_WithTheDatabaseUp_ReturnsOk()
    {
        var response = await Client.GetAsync("/health/ready");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.OK);
    }
}
```

Add a `ProjectReference` to `..\..\src\IndyPOS.ServiceDefaults\IndyPOS.ServiceDefaults.csproj` to the test project only if `IndyPOS.ServiceDefaults` is not already visible through StoreHub's reference. Build first to see.

- [ ] **Step 2: Run them and watch them fail**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~Endpoints.HealthEndpointTests"`
Expected: `HealthChecks_WithTheAppBuilt_RegisterOneDatabaseCheck` FAILS. The names today are `self`, `storehub-db` and `StoreHubDbContext` (Aspire's), not `self, database`. The other two PASS: they pin behaviour that must survive.

- [ ] **Step 3: Switch StoreHub to the shared check**

Replace lines 75-82:

```csharp
// Add PostgreSQL with EF Core via Aspire
// Connection name must match AppHost: postgres.AddDatabase("storehub-db")
builder.AddNpgsqlDbContext<StoreHubDbContext>("storehub-db");

// Readiness check: surfaces DB connectivity at /health/ready. Liveness ("self"
// check tagged "live") comes from ServiceDefaults.AddDefaultHealthChecks.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<StoreHubDbContext>("storehub-db", tags: ["ready"]);
```

with:

```csharp
// Add PostgreSQL with EF Core via Aspire
// Connection name must match AppHost: postgres.AddDatabase("storehub-db"). Aspire's own database
// check is off: readiness is the one bounded check below.
builder.AddNpgsqlDbContext<StoreHubDbContext>("storehub-db",
    settings => settings.DisableHealthChecks = true);

// Readiness = this store's own database only, never the cloud: the till must keep selling while the
// cloud is unreachable. Liveness ("self", tag "live") comes from ServiceDefaults.
builder.AddDatabaseReadinessCheck("storehub-db");
```

Then:
- Replace `// Map default endpoints (health, alive)` and `app.MapDefaultEndpoints();` (lines 232-233) with:

  ```csharp
  // Health probes (/health/live, /health/ready); their request timeout needs the middleware.
  app.UseRequestTimeouts();
  app.MapDefaultEndpoints();
  ```
- In the comment at lines 261-263, `backed by the DbContextCheck\n// registered above.` becomes `backed by the database readiness check\n// registered above.`
- In `IndyPOS.StoreHub.csproj`, delete the line `<PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore" Version="10.0.3" />`. Aspire still brings that package in on its own.

- [ ] **Step 4: Run StoreHub's whole suite**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests`
Expected: all PASS, including the 3 new tests.

- [ ] **Step 5: Commit**

```bash
git add src/IndyPOS.StoreHub tests/IndyPOS.StoreHub.IntegrationTests
git commit -m "feat(storehub): readiness is one bounded check of its own database"
```

---

### Task 4: CloudApi on the shared check (closes I0-C)

**Files:**
- Modify: `src/IndyPOS.CloudApi/Program.cs` (lines 35-37, 129-130, 198-211, and the end of the file)
- Create: `tests/IndyPOS.CloudApi.IntegrationTests/HealthEndpointTests.cs` (time-boxed, Step 1)
- Possibly modify: `tests/IndyPOS.CloudApi.IntegrationTests/IndyPOS.CloudApi.IntegrationTests.csproj`

**Interfaces:** consumes Task 2's helper and constants.

- [ ] **Step 1: Time-boxed attempt at a host for the real `Program` (about half a day, at most)**

Add `public partial class Program;` as the last line of `src/IndyPOS.CloudApi/Program.cs`, the same as StoreHub's. Add `<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.5" />` to the test project. Then write:

```csharp
using FluentAssertions;
using IndyPOS.ServiceDefaults;
using IndyPOS.Testing.Postgres;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Xunit;

namespace IndyPOS.CloudApi.IntegrationTests;

/// <summary>
/// CloudApi's health wiring on its real Program, in Development: OpenIddict and the production-safety
/// check need no certificates there, and EnsureCreated runs on this fixture's own database.
/// </summary>
public class HealthEndpointTests : IAsyncLifetime
{
    private const string SelfCheck = "self";

    private TestPostgres _postgres = null!;
    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        _postgres = await TestPostgres.StartAsync();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:cloud-db", _postgres.ConnectionString);
        });
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    // I0-C: MapDefaultEndpoints and a hand-rolled MapGet both mapped this route.
    [Fact]
    public void RouteTable_WithTheAppBuilt_MapsTheReadyProbeOnce()
    {
        var readyRoutes = _factory.Services.GetRequiredService<EndpointDataSource>()
                                           .Endpoints
                                           .OfType<RouteEndpoint>()
                                           .Count(e => e.RoutePattern.RawText == "/health/ready");

        readyRoutes.Should()
                   .Be(1);
    }

    [Fact]
    public void HealthChecks_WithTheAppBuilt_RegisterOneDatabaseCheck()
    {
        var names = _factory.Services.GetRequiredService<IOptions<HealthCheckServiceOptions>>()
                                     .Value.Registrations
                                     .Select(r => r.Name);

        names.Should()
             .BeEquivalentTo([SelfCheck, DatabaseReadinessCheck.Name]);
    }
}
```

Run: `dotnet test tests/IndyPOS.CloudApi.IntegrationTests --filter "FullyQualifiedName~HealthEndpointTests"`
- **Expected RED:** `RouteTable_…_MapsTheReadyProbeOnce` fails with **2**, and `HealthChecks_…_RegisterOneDatabaseCheck` fails, listing `CloudDbContext`.
- **If the host cannot start** (OpenIddict, the hosted `EventProcessor` against an empty database, or anything else) and half a day is not enough to fix it with test-only configuration, take the **fallback**:
  - delete this test file and the `Mvc.Testing` reference, but keep `public partial class Program;` (harmless);
  - ledger the ruling;
  - in Step 4, record a manual probe of the running container, `curl -i http://localhost:8080/health/ready` before and after, in the PR body.
  - The shared behaviour is already covered by Task 2.

- [ ] **Step 2: Switch CloudApi to the shared check**

- Lines 35-37: `builder.AddNpgsqlDbContext<CloudDbContext>("cloud-db");` becomes:

  ```csharp
  // Aspire's own database check is off: readiness is the one bounded check below.
  builder.AddNpgsqlDbContext<CloudDbContext>("cloud-db",
      settings => settings.DisableHealthChecks = true);
  builder.AddDatabaseReadinessCheck("cloud-db");
  ```
- Lines 129-130: `// Map default endpoints (health, alive)` + `app.MapDefaultEndpoints();` become:

  ```csharp
  // Health probes (/health/live, /health/ready); their request timeout needs the middleware.
  app.UseRequestTimeouts();
  app.MapDefaultEndpoints();
  ```
- **Delete** lines 198-211: the comment `// Health/ready endpoint with database check` and the whole `app.MapGet("/health/ready", …);` block.

- [ ] **Step 3: Run CloudApi's suites**

Run: `dotnet test tests/IndyPOS.CloudApi.IntegrationTests` and `dotnet test tests/IndyPOS.CloudApi.Tests`
Expected: all PASS (with the 2 new tests if Step 1 succeeded).

- [ ] **Step 4: Commit**

```bash
git add src/IndyPOS.CloudApi tests/IndyPOS.CloudApi.IntegrationTests
git commit -m "fix(cloudapi): /health/ready is mapped once and checks its database (I0-C)" -m "The hand-rolled MapGet duplicated MapDefaultEndpoints' route and returned ex.Message to anonymous callers. Readiness is now the shared bounded check; failure answers 503 with 'Unhealthy' instead of 500 JSON."
```

---

### Task 5: The wizard's connection check

**Files:**
- Create: `src/IndyPOS.Application/Abstractions/StoreHub/StoreHubRoutes.cs`, `src/IndyPOS.Application/Abstractions/StoreHub/IStoreHubConnectionCheck.cs`
- Create: `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubConnectionCheck.cs`
- Modify: `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubHttpClient.cs:248`, `src/IndyPOS.Infrastructure/ConfigureServices.cs` (in `AddStoreHubClientServices`, after line 196)
- Modify: `src/IndyPOS.Windows.Forms/UI/Setup/FirstRunWizard.cs` (constructor at :46, field at :16, `TestConnectionButton_Click` at :325-361)
- Create: `tests/IndyPOS.Application.Tests/Integration/StoreHub/StoreHubConnectionCheckTests.cs`

**Interfaces:**
- Produces:
  - `StoreHubRoutes.HealthReady` (`const string`);
  - `IStoreHubConnectionCheck.CheckAsync(CancellationToken) : Task<StoreHubConnectionResult>`;
  - `enum StoreHubConnectionStatus { Healthy, NotReady, Unexpected, Unreachable, TimedOut }`;
  - `sealed record StoreHubConnectionResult(StoreHubConnectionStatus Status, int? StatusCode = null)`;
  - `StoreHubConnectionCheck(HttpClient)` with `public static readonly TimeSpan Timeout`.

- [ ] **Step 1: The contract types**

`StoreHubRoutes.cs`:

```csharp
namespace IndyPOS.Application.Abstractions.StoreHub;

/// <summary>StoreHub paths the till calls from more than one place.</summary>
public static class StoreHubRoutes
{
    /// <summary>Readiness: StoreHub is up and its own database answers. Mapped in every environment.</summary>
    public const string HealthReady = "/health/ready";
}
```

`IStoreHubConnectionCheck.cs`:

```csharp
namespace IndyPOS.Application.Abstractions.StoreHub;

/// <summary>Asks StoreHub whether it is ready, and says why not, for the first-run wizard.</summary>
public interface IStoreHubConnectionCheck
{
    Task<StoreHubConnectionResult> CheckAsync(CancellationToken cancellationToken = default);
}

public enum StoreHubConnectionStatus
{
    Healthy,
    NotReady,
    Unexpected,
    Unreachable,
    TimedOut
}

public sealed record StoreHubConnectionResult(StoreHubConnectionStatus Status, int? StatusCode = null);
```

- [ ] **Step 2: Write the failing tests**

```csharp
using System.Net;
using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Infrastructure.Services.StoreHub;
using Xunit;

namespace IndyPOS.Application.Tests.Integration.StoreHub;

public class StoreHubConnectionCheckTests
{
    private const string ConfiguredBaseUrl = "http://localhost:5012";
    private static readonly TimeSpan ShortTimeout = TimeSpan.FromMilliseconds(50);

    [Fact]
    public async Task CheckAsync_WithAConfiguredBaseUrl_ProbesItsReadyRoute()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var check = CheckWith(handler);

        await check.CheckAsync();

        handler.LastRequestUri.Should()
                              .Be(new Uri(ConfiguredBaseUrl + StoreHubRoutes.HealthReady));
    }

    [Fact]
    public async Task CheckAsync_WithACancelledToken_ThrowsOperationCanceled()
    {
        var check = CheckWith(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        var act = () => check.CheckAsync(cancelled.Token);

        await act.Should()
                 .ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task CheckAsync_WhenStoreHubIsNotListening_ReturnsUnreachable()
    {
        var check = CheckWith(new StubHandler(_ => throw new HttpRequestException("refused")));

        var result = await check.CheckAsync();

        result.Status.Should()
                     .Be(StoreHubConnectionStatus.Unreachable);
    }

    [Fact]
    public async Task CheckAsync_WhenStoreHubDoesNotAnswerInTime_ReturnsTimedOut()
    {
        var check = CheckWith(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK), delay: TimeSpan.FromSeconds(5)),
                              ShortTimeout);

        var result = await check.CheckAsync();

        result.Status.Should()
                     .Be(StoreHubConnectionStatus.TimedOut);
    }

    [Fact]
    public async Task CheckAsync_WhenTheDatabaseIsNotReady_ReturnsNotReady()
    {
        var check = CheckWith(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        var result = await check.CheckAsync();

        result.Status.Should()
                     .Be(StoreHubConnectionStatus.NotReady);
    }

    [Fact]
    public async Task CheckAsync_WithAnUnexpectedStatus_ReturnsUnexpectedWithTheCode()
    {
        var check = CheckWith(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));

        var result = await check.CheckAsync();

        result.Should()
              .Be(new StoreHubConnectionResult(StoreHubConnectionStatus.Unexpected, (int)HttpStatusCode.NotFound));
    }

    [Fact]
    public async Task CheckAsync_WhenStoreHubIsReady_ReturnsHealthy()
    {
        var check = CheckWith(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        var result = await check.CheckAsync();

        result.Status.Should()
                     .Be(StoreHubConnectionStatus.Healthy);
    }

    private static StoreHubConnectionCheck CheckWith(HttpMessageHandler handler, TimeSpan? timeout = null) =>
        new(new HttpClient(handler)
        {
            BaseAddress = new Uri(ConfiguredBaseUrl),
            Timeout = timeout ?? StoreHubConnectionCheck.Timeout
        });

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond, TimeSpan? delay = null)
        : HttpMessageHandler
    {
        public Uri? LastRequestUri { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastRequestUri = request.RequestUri;
            if (delay is { } wait)
                await Task.Delay(wait, cancellationToken);
            return respond(request);
        }
    }
}
```

- [ ] **Step 3: Run them and watch them fail**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~StoreHubConnectionCheckTests"`
Expected: build error, `StoreHubConnectionCheck` does not exist.

- [ ] **Step 4: Implement the check**

`StoreHubConnectionCheck.cs`:

```csharp
using System.Net;
using IndyPOS.Application.Abstractions.StoreHub;

namespace IndyPOS.Infrastructure.Services.StoreHub;

/// <summary>
/// One readiness probe of the configured StoreHub, classified for a person: up, up without its
/// database, refused, too slow, or something unexpected.
/// </summary>
public sealed class StoreHubConnectionCheck(HttpClient httpClient) : IStoreHubConnectionCheck
{
    /// <summary>Short on purpose: a person is waiting on the button.</summary>
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public async Task<StoreHubConnectionResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClient.GetAsync(StoreHubRoutes.HealthReady, cancellationToken);
            return Classify(response.StatusCode);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException)
        {
            return new StoreHubConnectionResult(StoreHubConnectionStatus.TimedOut);
        }
        catch (HttpRequestException)
        {
            return new StoreHubConnectionResult(StoreHubConnectionStatus.Unreachable);
        }
    }

    private static StoreHubConnectionResult Classify(HttpStatusCode code) => code switch
    {
        >= HttpStatusCode.OK and < HttpStatusCode.MultipleChoices => new(StoreHubConnectionStatus.Healthy),
        HttpStatusCode.ServiceUnavailable => new(StoreHubConnectionStatus.NotReady, (int)code),
        _ => new(StoreHubConnectionStatus.Unexpected, (int)code)
    };
}
```

In `ConfigureServices.cs` `AddStoreHubClientServices`, after `services.AddSingleton<IStoreHubClient>(…);`:

```csharp
		// The first-run wizard's "Test connection": the configured StoreHub, a short timeout.
		services.AddHttpClient<IStoreHubConnectionCheck, StoreHubConnectionCheck>(client =>
		{
			client.BaseAddress = new Uri(storeHubOptions.BaseUrl);
			client.Timeout = StoreHubConnectionCheck.Timeout;
		});
```

This file is **tab-indented**. Add `using IndyPOS.Application.Abstractions.StoreHub;` if it is missing.

In `StoreHubHttpClient.IsHealthyAsync`, `"/health/ready"` → `StoreHubRoutes.HealthReady`.

- [ ] **Step 5: Use it in the wizard**

- Add a field `private readonly IStoreHubConnectionCheck _connectionCheck;`.
- Constructor: `public FirstRunWizard(IStoreConfigurationService storeConfigurationService, IStoreHubConnectionCheck connectionCheck)`, assigning `_connectionCheck = connectionCheck;` beside the existing assignment.
- Replace the `try { … } catch (HttpRequestException) { … } catch (TaskCanceledException) { … }` part of `TestConnectionButton_Click` (keep the opening lines and the `finally`) with:

```csharp
        try
        {
            var result = await _connectionCheck.CheckAsync();
            (_connectionStatusLabel.Text, _connectionStatusLabel.ForeColor) = Describe(result);
        }
        finally
        {
            _testConnectionButton.Enabled = true;
        }
    }

    private static (string Text, Color Colour) Describe(StoreHubConnectionResult result) => result.Status switch
    {
        StoreHubConnectionStatus.Healthy => ("✓ StoreHub is running and healthy!", Color.LightGreen),
        StoreHubConnectionStatus.NotReady => ("⚠ StoreHub is running but its database is not ready.", Color.Orange),
        StoreHubConnectionStatus.Unexpected => ($"⚠ StoreHub responded with status: {result.StatusCode}", Color.Orange),
        StoreHubConnectionStatus.TimedOut => ("✗ Connection timed out.", Color.Salmon),
        _ => ("✗ Could not connect to StoreHub.\nMake sure the service is running.", Color.Salmon)
    };
```

Add `using IndyPOS.Application.Abstractions.StoreHub;`.

- [ ] **Step 6: Run the tests and check the till builds**

Run:
- `dotnet test tests/IndyPOS.Application.Tests` → all pass, including 7 new tests;
- `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~ConfigureServicesTests"` → pass (the till's container still builds);
- `dotnet build src/IndyPOS.Windows.Forms` → 0 errors.

Run: `git grep -n "localhost:5000/health\|\"/health\"" -- src` → nothing.

- [ ] **Step 7: Commit**

```bash
git add src/IndyPOS.Application src/IndyPOS.Infrastructure src/IndyPOS.Windows.Forms tests/IndyPOS.Application.Tests
git commit -m "fix(winforms): the wizard's Test connection probes /health/ready on the configured StoreHub" -m "It called localhost:5000/health, which production does not map (404 on every installed till), and ignored StoreHub:BaseUrl. The check now lives in a testable StoreHubConnectionCheck that tells 'up', 'database not ready', 'refused', 'timed out' and 'unexpected' apart, and rethrows cancellation."
```

---

### Task 6: The installer's probe polls until a deadline

**Files:**
- Modify: `installer/IndyPOS.Bootstrapper/Installers/HealthProbe.cs`
- Create: `tests/IndyPOS.Bootstrapper.Tests/Installers/HealthProbeTests.cs`

**Interfaces:**
- `public static Task<bool> IsReadyAsync(int port, CancellationToken cancellationToken = default)`. The `attempts` parameter is removed; both callers pass only `port` and `cancellationToken:`.
- `internal static Task<bool> IsReadyAsync(Uri readyUrl, HttpMessageHandler handler, TimeProvider clock, Func<TimeSpan, CancellationToken, Task> delay, CancellationToken cancellationToken)`.

- [ ] **Step 1: Write the failing tests** (FluentAssertions **6** in this project)

```csharp
using System.Net;
using FluentAssertions;
using IndyPOS.Bootstrapper.Installers;

namespace IndyPOS.Bootstrapper.Tests.Installers;

public class HealthProbeTests
{
    private static readonly Uri ReadyUrl = new("http://localhost:5000/health/ready");
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(60);

    [Fact]
    public async Task IsReadyAsync_WithACancelledToken_ThrowsOperationCanceled()
    {
        var clock = new ManualClock();
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        var act = () => HealthProbe.IsReadyAsync(ReadyUrl, new Responder(HttpStatusCode.OK), clock, clock.Advance, cancelled.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task IsReadyAsync_WhenNeverReady_GivesUpAtTheDeadline()
    {
        var clock = new ManualClock();
        var responder = new Responder(HttpStatusCode.ServiceUnavailable);

        var ready = await HealthProbe.IsReadyAsync(ReadyUrl, responder, clock, clock.Advance, CancellationToken.None);

        ready.Should().BeFalse();
        clock.Elapsed.Should().BeCloseTo(Deadline, TimeSpan.FromSeconds(2));
    }

    // PostgreSQL can take most of a minute after a reboot; failing fast must not end the wait early.
    [Fact]
    public async Task IsReadyAsync_WhenNotReadyAtFirst_KeepsPollingUntilReady()
    {
        var clock = new ManualClock();
        var responder = new Responder(HttpStatusCode.ServiceUnavailable, readyAfter: 20);

        var ready = await HealthProbe.IsReadyAsync(ReadyUrl, responder, clock, clock.Advance, CancellationToken.None);

        ready.Should().BeTrue();
    }

    [Fact]
    public async Task IsReadyAsync_WhenReady_ReturnsOnTheFirstTry()
    {
        var clock = new ManualClock();
        var responder = new Responder(HttpStatusCode.OK);

        var ready = await HealthProbe.IsReadyAsync(ReadyUrl, responder, clock, clock.Advance, CancellationToken.None);

        ready.Should().BeTrue();
        responder.Calls.Should().Be(1);
    }

    private sealed class ManualClock : TimeProvider
    {
        private readonly DateTimeOffset _start = new(2026, 10, 7, 0, 0, 0, TimeSpan.Zero);
        private DateTimeOffset _now = new(2026, 10, 7, 0, 0, 0, TimeSpan.Zero);

        public TimeSpan Elapsed => _now - _start;

        public override DateTimeOffset GetUtcNow() => _now;

        public Task Advance(TimeSpan by, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _now += by;
            return Task.CompletedTask;
        }
    }

    private sealed class Responder(HttpStatusCode status, int readyAfter = int.MaxValue) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            var code = Calls > readyAfter ? HttpStatusCode.OK : status;
            return Task.FromResult(new HttpResponseMessage(code));
        }
    }
}
```

- [ ] **Step 2: Run them and watch them fail**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests --filter "FullyQualifiedName~HealthProbeTests"`
Expected: build error, no internal overload exists.

- [ ] **Step 3: Rewrite `HealthProbe`**

Keep the class doc comment. Replace the method:

```csharp
    // Long enough for PostgreSQL to come up after a reboot. Bounded by time, not attempts: StoreHub's
    // readiness check fails within about 3 seconds, so a fixed number of attempts would end the wait
    // too soon and roll an upgrade back while the database is still starting.
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan RetryGap = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan AttemptTimeout = TimeSpan.FromSeconds(10);

    public static Task<bool> IsReadyAsync(int port, CancellationToken cancellationToken = default) =>
        // /health/ready is StoreHub's readiness probe (its own database). /health is mapped in
        // Development only, so an installed StoreHub answers it with 404.
        IsReadyAsync(new Uri($"http://localhost:{port}/health/ready"), new HttpClientHandler(),
                     TimeProvider.System, (gap, ct) => Task.Delay(gap, ct), cancellationToken);

    internal static async Task<bool> IsReadyAsync(
        Uri readyUrl,
        HttpMessageHandler handler,
        TimeProvider clock,
        Func<TimeSpan, CancellationToken, Task> delay,
        CancellationToken cancellationToken)
    {
        using var client = new HttpClient(handler) { Timeout = AttemptTimeout };
        var giveUpAt = clock.GetUtcNow() + Deadline;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await AnswersReadyAsync(client, readyUrl, cancellationToken))
                return true;

            if (clock.GetUtcNow() + RetryGap > giveUpAt)
                return false;

            await delay(RetryGap, cancellationToken);
        }
    }

    private static async Task<bool> AnswersReadyAsync(HttpClient client, Uri readyUrl, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.GetAsync(readyUrl, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return false;
        }
    }
```

The `ManualClock` does not advance during a request. `IsReadyAsync_WhenNeverReady_GivesUpAtTheDeadline` therefore counts only the gaps, about 30 tries × 2 s. That is the point: the deadline is measured on the clock, not by the number of tries.

- [ ] **Step 4: Run the installer suite**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests`
Expected: all pass. Total 231 + 4 = **235** (227 pass, 8 skipped).

Run: `git grep -n "attempts:" -- installer` → nothing; no caller passed it.

- [ ] **Step 5: Commit**

```bash
git add installer tests/IndyPOS.Bootstrapper.Tests
git commit -m "fix(installer): the health probe waits up to 60 s for readiness and honours cancellation"
```

---

### Task 7: AppHost waits on real readiness; StoreHub stops waiting for the cloud

**Files:**
- Modify: `src/IndyPOS.AppHost/Program.cs` (lines 14-24)

- [ ] **Step 1: Edit**

```csharp
// Cloud API (central cloud service) - must be defined first for service discovery
var cloudApi = builder.AddProject<Projects.IndyPOS_CloudApi>("cloud-api")
                      .WithReference(cloudDb)
                      .WithHttpHealthCheck("/health/ready", endpointName: "http")
                      .WaitFor(postgres);

// StoreHub API (local store service). It references CloudApi for sync but never waits for it:
// offline-first, the till must be able to sell while the cloud is down.
var storeHub = builder.AddProject<Projects.IndyPOS_StoreHub>("storehub-api")
                      .WithReference(storeHubDb)
                      .WithReference(cloudApi)
                      .WithHttpHealthCheck("/health/ready", endpointName: "http")
                      .WaitFor(postgres);
```

Both projects expose an `http` endpoint in each launch profile (StoreHub `:5012`, CloudApi `:5180`). The till's existing `.WaitFor(storeHub)` stays.

- [ ] **Step 2: Build**

Run: `dotnet build src/IndyPOS.AppHost` → 0 errors.

- [ ] **Step 3: Verify by hand** (record in the PR body)

Run: `dotnet run --project src/IndyPOS.AppHost --launch-profile https`. Then:
1. On the dashboard (`https://localhost:17222`), `storehub-api` and `cloud-api` turn **Healthy** only after their databases answer.
2. Stop `cloud-api` from the dashboard, then restart `storehub-api`. It starts and turns Healthy with the cloud down.
3. Start `winforms-app`. It starts only once StoreHub is Healthy.

- [ ] **Step 4: Commit**

```bash
git add src/IndyPOS.AppHost
git commit -m "fix(apphost): wait on real readiness; StoreHub no longer waits for CloudApi"
```

---

### Task 8: Docs

**Files:**
- `docs/architecture/api-conventions.md`, `ONBOARDING.md` (Trap 5, lines 198-210)
- `docs/operations/{smoke-test.ps1:152, update-procedure.md:144-151, troubleshooting-guide.md:18, RUNBOOK.md:47, pilot-checklist.md:84,96, post-deployment-monitoring.md:185, cloud-deployment.md:143-170, store-installation-guide.md:598}`
- `src/IndyPOS.Windows.Forms/appsettings.README.md:166`
- `docs/diagrams/architecture-overview.md:254,315`

- [ ] **Step 1: `api-conventions.md` gets a "Health checks" section**

Add it before "Renaming or removing a route after go-live":

```markdown
## Health checks

| Route | Environments | Answers |
|---|---|---|
| `/health/live` | all | `Healthy` 200 — the process is up; never touches the database |
| `/health/ready` | all | `Healthy` 200 or `Unhealthy` 503 — one `SELECT 1` against the service's **own** database |
| `/health` | Development only | every check, with detail (JSON) |

- **Callers use `/health/ready`, never `/health`.** `/health` does not exist on an installed till.
- **Production probes are terse.** They are anonymous, so they say only Healthy or Unhealthy.
- **StoreHub's readiness never involves the cloud**, and nothing makes StoreHub wait for CloudApi.
- **A probe is bounded.**
  - The check's connection string sets Npgsql `Timeout=3`, the only thing that can end a hanging
    connect. A hanging database answers 503 in about 3 seconds.
  - A 5-second request timeout (504 if it ever fires) is a backstop.
- **One registration helper:** `AddDatabaseReadinessCheck(connectionName)` in `ServiceDefaults`. Aspire's
  own database check is switched off, so there is exactly one.
- Defect I0-C, CloudApi mapping `/health/ready` twice, was resolved by this convention (2026-10).
```

- [ ] **Step 2: Every `/health` caller in docs and scripts**

In each file below, `…/health` becomes `…/health/ready`. Where the text shows an expected body, it becomes the plain text `Healthy`.
- `docs/operations/smoke-test.ps1:152`: `-Url "$StoreHubUrl/health"` → `-Url "$StoreHubUrl/health/ready"`.
- `docs/operations/update-procedure.md:144`: the URL changes, and the checkpoint at `:151`, which claims `{"status":"Healthy"}`, becomes "returns `Healthy`".
- `docs/operations/troubleshooting-guide.md:18`, `RUNBOOK.md:47`, `pilot-checklist.md:84`: the URL changes. `pilot-checklist.md:96` becomes `- [ ] \`/health/ready\` returns 200 OK`.
- `docs/operations/post-deployment-monitoring.md:185`: `"/health",` → `"/health/ready",`.
- `src/IndyPOS.Windows.Forms/appsettings.README.md:166`: `curl http://<BaseUrl>/health` → `curl http://<BaseUrl>/health/ready`.
- `docs/operations/store-installation-guide.md:598`: `192.168.1.100:5000` → `localhost:5000`. StoreHub listens on localhost only (`DatabaseSetup.cs:448`). Add one sentence saying so.
- `docs/operations/cloud-deployment.md:143-170`: rewrite the passage that describes I0-C and the JSON body as live. CloudApi now maps `/health/ready` once; it answers `Healthy`/`Unhealthy` with 200/503; the container `HEALTHCHECK` stays on `/health/live`.
- `docs/diagrams/architecture-overview.md:254` and `:315`: `GET /health` → `GET /health/ready`. **Pad-only:** remove 6 spaces after it so the line keeps its exact character width. Check with:

```bash
PYTHONIOENCODING=utf-8 python -c "import subprocess,io;o=subprocess.run(['git','show','HEAD:docs/diagrams/architecture-overview.md'],capture_output=True).stdout.decode().replace('\r','').split('\n');n=io.open('docs/diagrams/architecture-overview.md',encoding='utf-8').read().replace('\r','').split('\n');[print(i+1,len(a),len(b)) for i,(a,b) in enumerate(zip(o,n)) if a!=b and len(a)!=len(b)]"
```

Expected: no output.

- [ ] **Step 3: ONBOARDING Trap 5**

Keep the table. After its last sentence, add: "Since 2026-10 every caller in the code, scripts and runbooks uses `/health/ready`; the first-run wizard's 'Test connection' used to probe `/health` and always reported a 404." Make sure no sentence in Trap 5 still advises `/health` for a real install.

Run: `git grep -nE "localhost:[0-9]+/health([^/]|$)|/health\"" -- docs src scripts ONBOARDING.md ':!docs/superpowers' ':!.planning'` → nothing, except lines that explicitly describe `/health` as Development-only.

- [ ] **Step 4: Commit**

```bash
git add docs ONBOARDING.md src/IndyPOS.Windows.Forms/appsettings.README.md
git commit -m "docs: health-check convention, and every runbook probes /health/ready"
```

---

### Task 9: Measure the counts

**Files:** `CLAUDE.md` (Docker-down block, the "Solution suites total" paragraph, the installer line), `ONBOARDING.md` (Trap 1, Expected counts)

- [ ] **Step 1: Run everything with TRX**

Run the solution with `--logger trx --results-directory <dir>` (as in earlier plans) and sum each `.trx` with `Get-ChildItem -LiteralPath`. **Not** with `-Path`: a `[1]` in a file name is a wildcard. Then run `dotnet test tests/IndyPOS.Bootstrapper.Tests`.

Cross-check:

| Suite | Baseline (2026-10-07) | Added | Expected |
|---|---|---|---|
| Application | 577 | +7 (Task 5) | 584 |
| Windows.Forms | 50 (login fix, not yet in the docs) | — | 50 |
| StoreHub.IntegrationTests | 289 | +3 (Task 3) | 292 |
| CloudApi.IntegrationTests | 49 | +2 (Task 4, if its host worked) | 51 or 49 |
| **ServiceDefaults.Tests (new)** | — | +8 (Task 2) | 8 |
| Domain 56, Vault 17, CloudApi 6, MigrationTool 145 | — | — | unchanged |
| **Total** | 1189 | +20 | **1209** (1208 pass, 1 skip), or 1207 with the fallback |
| Installer (outside the solution) | 231 | +4 (Task 6) | 235 (227 pass, 8 skip) |

- [ ] **Step 2: Write the measured numbers**

In both files, write:
- the new totals, with today's date, keeping the previous total in the history list;
- the per-suite numbers;
- the new suite;
- the installer's 235.

The new suite needs a Postgres, so the **Docker-down** figure grows: StoreHub's derived count + 3, CloudApi's + 2 (if added), and ServiceDefaults + 8. Recompute it, keep it labelled derived, and add one sentence naming this change.

The no-store-DB total is the measured total − 20, labelled derived until CI reports it.

- [ ] **Step 3: Commit**

```bash
git add CLAUDE.md ONBOARDING.md
git commit -m "docs: measured test counts after the health-check convention"
```

- [ ] **Step 4: Before release (Pond, at the VM)**

Run an **upgrade smoke, 4.x → this build**, on `IndyPOS-Test`: the installer's probe must pass, and a reboot followed by an upgrade must not roll back while PostgreSQL starts. Record it in the PR body. This is not a code step.

---

## Self-Review

**Spec coverage:**
- §1.1 wizard: Task 5.
- §1.2 / §4.6 delete the update service: Task 1.
- §1.3 I0-C: Task 4.
- §1.4 tests: Tasks 2-6.
- §1.5 AppHost: Task 7.
- §1.6 docs: Task 8.
- §2 terse production and dev-only JSON: Task 2 (`DetailedHealth_*`). Offline-first: Task 3's comment and Task 7. Packages: Task 2.
- §3 contract and timeout layers: Task 2 (hanging + longer-timeout tests). The CloudApi body change: Task 4's commit and Task 8. Caller table: Tasks 1, 5, 6, 7, 8.
- §4.1 helper: Task 2. §4.2 each service: Tasks 3-4. §4.3 AppHost: Task 7. §4.4 wizard: Task 5. §4.5 probe: Task 6.
- §5 error handling: Task 2 (no-leak, hang, unreachable, live-without-DB). The missing-string 500 is documented in code (Task 2, Step 5).
- §6 version skew: no code; the routes are unchanged.
- §7 tests: Tasks 2-6, plus Task 7 by hand. CloudApi's fallback is Task 4, Step 1.
- §8 docs: Task 8. §9 success: Tasks 1-9, plus the VM smoke (Task 9, Step 4).

**Placeholders:** none. Task 4's fallback is a defined branch with its own steps, not a TBD.

**Type consistency:**
- `DatabaseReadinessCheck.Name`, `Extensions.ReadyTag`/`LiveTag`/`ProbeTimeoutPolicy` are defined in Task 2 and used in Tasks 3-4.
- `StoreHubRoutes.HealthReady`, `IStoreHubConnectionCheck`, `StoreHubConnectionStatus`, `StoreHubConnectionResult` and `StoreHubConnectionCheck.Timeout` are defined and used in Task 5.
- `HealthProbe.IsReadyAsync(Uri, HttpMessageHandler, TimeProvider, Func<TimeSpan, CancellationToken, Task>, CancellationToken)` is defined and used in Task 6.

**Known risk:** Task 4, Step 1, is the only step with a real chance of not working as written. It is time-boxed, and its fallback keeps the convention fully covered by Task 2.
