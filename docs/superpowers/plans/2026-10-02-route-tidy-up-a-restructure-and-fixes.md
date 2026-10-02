# Route Tidy-Up A — Restructure and Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** StoreHub's `Program.cs` holds wiring only, with every route in one file per area. Four bugs are fixed: a stock movement records its user, the sale body loses its dead `UserId`, a report date gives a Thai 400 instead of a 500, and the cloud's `/sync/status` needs a store token. Every path, verb, policy, status code and body stays the same.

**Architecture:** Routes move from `src/IndyPOS.StoreHub/Program.cs` into `Endpoints/<Area>/<Area>Endpoints.cs` extension methods, following the `Endpoints/Cash` and `Endpoints/Sales` pattern. Each route sits in its own small private method. The move is proved two ways: the existing tests pass unedited, and a new route-table pin checks every moved route's verb, path and policy. The fixes then land one task each, RED first. One nullable column is added (`inventory_movement.created_by_user_id`). The `SalesQueryRules` date range moves into a shared `DateRangeRule`, which the `/reports` routes also use. The cloud inbox gains a nullable `SyncedEvents.SourceStoreId` (CloudApi migration `AddSyncedEventSourceStore`), stamped at ingest from the token. The cloud status route moves into its own file and counts by that column.

**Tech Stack:** C# .NET 10, ASP.NET Core minimal APIs, EF Core + Npgsql, Nokpirab CQRS, xUnit + FluentAssertions 8 + Moq/AutoFixture, Testcontainers PostgreSQL 16, and `Microsoft.AspNetCore.TestHost`, new to `IndyPOS.CloudApi.IntegrationTests`.

**Spec:** `docs/superpowers/specs/2026-10-02-route-tidy-up-design.md`. This plan covers §3 (PR A) only. PR B (§4, the renames) is a separate plan.

## Prerequisites

- Branch from `development` at or after `f9970ca`, the commit that holds the spec.
- **Docker is running.** `IndyPOS.StoreHub.IntegrationTests` and `IndyPOS.CloudApi.IntegrationTests` start a real Postgres container. Without Docker they fail in under a second, which looks like a regression but is not (CLAUDE.md, Trap 1).
- **Line numbers** below are those at `f9970ca`. Each move task deletes lines from `Program.cs`, so later blocks shift. Find each block by the first and last lines quoted, not by the number.

## Global Constraints

- [§3.1] "**Paths, verbs, policies, status codes and response bodies stay byte-for-byte the same.** The existing endpoint tests are the proof: they pass with no edits to URLs or asserted bodies." Tasks 1-6 edit no existing test; Task 1 only adds one. Tasks 7-11 edit only the tests they name.
- [§3.1] "**`Program.cs` keeps** service registration, auth policies, middleware and the `Map…Endpoints()` calls only."
- [§3.1] "**Shared helpers move:** `RequireUserIdFilter` and `ClaimsPrincipalExtensions` (`GetRequiredUserId`, `HasCapability`) go from `Endpoints/Cash/` to `Endpoints/Common/`."
- [§3.2.1] "Add a **nullable** column `inventory_movement.created_by_user_id` (migration `AddInventoryMovementUser`)." "A token without a usable user id gets **401**." "Older rows stay `NULL`."
- [§3.2.2] "A body that still sends `userId` is accepted."
- [§3.2.3] "(2000-01-01..2099-12-31, to ≥ from) into one shared rule. Both `/sales` and `/reports` use it, so the two cannot drift." "Every report route that takes dates answers with a Thai `{ error }` and **400**."
- [§3.2.4] "It will require the same store token as `/sync/events` and count only that token's store. No token gives **401**." "The StoreHub `/sync/status` … is unchanged."
- **Forward-only migrations (CLAUDE.md release gate):** additive only. A new column is nullable. No renames, no drops.
- **Never a bare `BeginTransactionAsync`** on `StoreHubDbContext`. No task here needs a transaction. `SaleRepository.CompleteSaleAsync` is not touched.
- **Offline-first is priority #1.** No StoreHub write may wait on the network. The only cloud change is a read route on the cloud itself.
- **Error bodies** are Thai `{ "error": "..." }`. A policy-level 401/403 has no body. Existing non-Thai bodies (`"Delta must not be zero."`, the bare string on `PUT /products/{id}`) are frozen by §3.1 and stay as they are.
- **Test style:**
  - names follow `Subject_WhenScenario_DirectVerbOutcome` (`When`/`With`, a direct verb, never `Should`);
  - one behaviour per test;
  - negative cases first, more than 50% failure or edge cases;
  - Arrange/Act/Assert separated by blank lines;
  - named constants for boundary values;
  - FluentAssertions chains on separate lines, with the dots aligned.
- **Shared integration DB:** the StoreHub test database is **not** reset between tests. Assert only on rows the test created, found by the product id or username the test made.
- **Code style:** file-scoped namespaces, fluent dots aligned, `ConfigureServices.cs` is tab-indented (no task touches it).
- **Comments must match behaviour.** A comment the move or a fix makes wrong is a defect. Each task names the comments it corrects. No planning labels (`PR B`, `Task 3`) go into code comments.
- **Commits:** conventional commits, one or more per task. Never push.
- **[§3.2.4, Pond 2026-10-02] The cloud inbox is scoped by a new column:** nullable `SyncedEvents.SourceStoreId`, set at ingest from the token's `store_id`, with a `HasComment`. Rows ingested before the release stay `NULL` and are counted for no store. The `int StoreId` column and `HttpCloudSyncClient.ParseStoreId` are **not** changed (spec §7).
- **Cloud tables explain themselves:** every new cloud column carries a `HasComment` for dashboards and MCP.

## Review Focus

These are the five inputs the spec implies but its test list does not exercise, most likely first. Each has a test in the task that owns the code:

1. **A moved route silently loses its policy or verb.** Most moved routes have no HTTP test at all: `/payment-methods`, all three `/admin/payment-methods` routes, `/product-categories`, `/version`, `/` and `/pay-later/{id}`. Expected: every route keeps its exact verb, path and policy. → Task 1 (`RouteTable_WithTheAppBuilt_ContainsEveryMovedRouteWithItsPolicy`), re-run in every move task.
2. **An authenticated token with no user id on `adjust-quantity`.** It must not get a 200 with a `NULL` user, or a 500. Expected: 401, and **no movement is written**. → Task 7 (`AdjustQuantity_WithATokenWithoutAUserId_WritesNoMovement`).
3. **A report date just outside the bounds, which does not crash today (`1999-12-31`).** Expected: the same Thai 400 as the dates that do crash, not a silent 200. The Thai body is checked too. → Task 10 (`DatedReport_WithADateBeforeTheEarliest_ReturnsBadRequest`, `DatedReport_WithToBeforeFrom_ExplainsInThai`).
4. **A cloud inbox row ingested before this release** (`SourceStoreId` NULL), even one whose payload names the caller's store. Expected: counted for no store, never guessed from the payload. → Task 11 (`SyncStatus_WithALegacyInboxRowWithoutASourceStore_DoesNotCountIt`).
5. **A cloud token that authenticates but carries no `store_id`.** Expected: 403, never every store's counts. → Task 11 (`SyncStatus_WithATokenWithoutAStore_ReturnsForbidden`).

---

## File Structure

```
src/IndyPOS.StoreHub/
  Program.cs                                   MOD  wiring only (Tasks 1-6)
  Endpoints/Common/ClaimsPrincipalExtensions.cs  MOVED from Endpoints/Cash (Task 1)
  Endpoints/Common/RequireUserIdFilter.cs        MOVED from Endpoints/Cash (Task 1)
  Endpoints/Cash/*.cs                          MOD  using Endpoints.Common; CashEndpoints doc (Tasks 1, 6)
  Endpoints/Sales/SaleQueryEndpoints.cs        MOD  using (Task 1)
  Endpoints/Sales/SaleReprintEndpoints.cs      MOD  using (Task 1)
  Endpoints/Sales/SalesEndpoints.cs            MOD  using; maps completion; doc (Tasks 1, 5)
  Endpoints/Sales/SaleCompletionEndpoints.cs   NEW  POST /sales/complete (Task 5)
  Endpoints/SystemInfo/SystemInfoEndpoints.cs  NEW  /, /version (Task 2)
  Endpoints/Auth/AuthEndpoints.cs              NEW  /auth/* (Task 2)
  Endpoints/Products/ProductsEndpoints.cs      NEW  /products* (Task 3; Task 7 edits adjust-quantity)
  Endpoints/PaymentMethods/PaymentMethodsEndpoints.cs  NEW  /payment-methods, /admin/payment-methods* (Task 4)
  Endpoints/Catalogue/CatalogueEndpoints.cs    NEW  /product-categories, /store/features (Task 4)
  Endpoints/Sync/SyncEndpoints.cs              NEW  /sync/status (Task 5)
  Endpoints/Reports/ReportsEndpoints.cs        NEW  /reports/* (Task 6; Task 10 adds the date rule)
  Endpoints/PayLater/PayLaterEndpoints.cs      NEW  /pay-later* (Task 6)

src/IndyPOS.Domain/Entities/Core/InventoryMovement.cs                          MOD  CreatedByUserId (Task 7)
src/IndyPOS.Infrastructure/Persistence/StoreHub/Configurations/InventoryMovementConfiguration.cs  MOD (Task 7)
src/IndyPOS.Infrastructure/Persistence/StoreHub/Migrations/<ts>_AddInventoryMovementUser.cs       NEW (dotnet ef, Task 7)
src/IndyPOS.Application/UseCases/StoreHub/Products/AdjustQuantity/AdjustProductQuantityCommand.cs        MOD (Task 7)
src/IndyPOS.Application/UseCases/StoreHub/Products/AdjustQuantity/AdjustProductQuantityCommandHandler.cs MOD (Task 7)
src/IndyPOS.Application/UseCases/StoreHub/Sales/Complete/CompleteSaleCommandHandler.cs                   MOD (Task 8)
src/IndyPOS.Application/UseCases/StoreHub/Sales/CompleteSaleRequest.cs         MOD  drop UserId (Task 9)
src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubSaleService.cs            MOD  drop UserId (Task 9)
src/IndyPOS.Application/Common/Validation/DateRangeRule.cs                     NEW  (Task 10)
src/IndyPOS.Application/UseCases/StoreHub/Sales/History/SalesQueryRules.cs     MOD  delegates (Task 10)

src/IndyPOS.Application/Abstractions/Cloud/Repositories/ISyncedEventRepository.cs  MOD  SourceStoreId (Task 11)
src/IndyPOS.Application/UseCases/Cloud/Sync/IngestEvents/IngestEventsCommandHandler.cs  MOD  stamps it (Task 11)
src/IndyPOS.CloudApi/Infrastructure/CloudDbContext.cs          MOD  column, comment, index (Task 11)
src/IndyPOS.CloudApi/Infrastructure/Migrations/<ts>_AddSyncedEventSourceStore.cs  NEW (dotnet ef, Task 11)
src/IndyPOS.CloudApi/Program.cs                                MOD  app.MapSyncStatus() (Task 11)
src/IndyPOS.CloudApi/Endpoints/SyncStatusEndpoints.cs          NEW  (Task 11)
src/IndyPOS.CloudApi/Infrastructure/StoreSyncStatusQuery.cs    NEW  (Task 11)

tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/RouteTableTests.cs   NEW  (Task 1)
tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/ProductsEndpointTests.cs  MOD (Task 7)
tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/SalesEndpointTests.cs     MOD (Task 9)
tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/PayLaterSaleEndpointTests.cs  MOD (Task 9)
tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/SalesHistoryEndpointsTests.cs MOD (Task 9)
tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/ReportsEndpointTests.cs   MOD (Task 10)
tests/IndyPOS.Application.Tests/StoreHub/Sales/Commands/CompleteSaleCommandHandlerTests.cs  MOD (Task 8)
tests/IndyPOS.Application.Tests/Integration/StoreHub/StoreHubE2ETests.cs        MOD (Task 9)
tests/IndyPOS.Application.Tests/Integration/StoreHub/StoreHubHttpClientTests.cs MOD (Task 9)
tests/IndyPOS.Application.Tests/Common/Validation/DateRangeRuleTests.cs         NEW (Task 10)
tests/IndyPOS.Application.Tests/UseCases/StoreHub/Sales/SalesQueryRulesTests.cs MOD line 58 (Task 10)
tests/IndyPOS.Application.Tests/UseCases/Cloud/Sync/IngestEventsCommandHandlerTests.cs  MOD (Task 11)
tests/IndyPOS.CloudApi.IntegrationTests/IndyPOS.CloudApi.IntegrationTests.csproj  MOD (Task 11)
tests/IndyPOS.CloudApi.IntegrationTests/SyncStatusTestHost.cs                   NEW (Task 11)
tests/IndyPOS.CloudApi.IntegrationTests/SyncStatusEndpointTests.cs              NEW (Task 11)

.bruno/StoreHub/sales/complete-sale.bru, complete-sale-example.bru   MOD  no userId (Task 9)
docs/development/getting-started.md, docs/diagrams/flows.md          MOD  no userId (Task 9)
docs/operations/upgrade-procedure.md                                 MOD  gate results + cloud variant (Task 12)
CLAUDE.md, ONBOARDING.md                                             MOD  measured counts (Task 13)
```

**Why `SystemInfo`, not `System`:** the spec names the area `System`. But a namespace `IndyPOS.StoreHub.Endpoints.System` would shadow the root `System` namespace for every sibling namespace under `IndyPOS.StoreHub.Endpoints`. Any `System.Xxx` written in code there would then bind to the wrong place. The folder and namespace are therefore `SystemInfo`.

**Why one private method per route:** CLAUDE.md caps a function at 20 lines. One `Map…Endpoints` method that holds every lambda would run to 80+ lines. Each route gets its own private `Map<Route>` method, and the public extension calls them in order. Lambda bodies are copied unchanged. A few are longer than 20 lines today, such as `PUT /products/{id}`. They stay as they are, because §3.1 freezes them.

---

### Task 1: Pin the route table, then move the shared helpers to `Endpoints/Common`

**Files:**
- Create: `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/RouteTableTests.cs`
- Move: `src/IndyPOS.StoreHub/Endpoints/Cash/ClaimsPrincipalExtensions.cs` → `src/IndyPOS.StoreHub/Endpoints/Common/ClaimsPrincipalExtensions.cs`
- Move: `src/IndyPOS.StoreHub/Endpoints/Cash/RequireUserIdFilter.cs` → `src/IndyPOS.StoreHub/Endpoints/Common/RequireUserIdFilter.cs`
- Modify: `src/IndyPOS.StoreHub/Endpoints/Cash/{CashCountEndpoints,CashEndpoints,CashFloatEndpoints,CashPayoutEndpoints,DebtRepaymentEndpoints,TodayOnlyBusinessDateFilter}.cs` (usings)
- Modify: `src/IndyPOS.StoreHub/Endpoints/Sales/{SaleQueryEndpoints,SaleReprintEndpoints,SalesEndpoints}.cs` (usings)
- Modify: `src/IndyPOS.StoreHub/Program.cs:35-36` (usings)

**Interfaces:**
- Consumes: `IntegrationTestBase` (`Factory`, the `[Collection("Integration")]` fixture).
- Produces: namespace `IndyPOS.StoreHub.Endpoints.Common` with `internal sealed class RequireUserIdFilter : IEndpointFilter` and `internal static class ClaimsPrincipalExtensions` (`Guid? FindUserId(this ClaimsPrincipal)`, `Guid GetRequiredUserId(this ClaimsPrincipal)`, `bool HasCapability(this ClaimsPrincipal, string)`). Tasks 5 and 7 use them. The test `RouteTable_WithTheAppBuilt_ContainsEveryMovedRouteWithItsPolicy` is re-run by Tasks 2-6.

- [ ] **Step 1: Write the route-table pin**

Create `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/RouteTableTests.cs`:

```csharp
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

/// <summary>
/// Pins the verb, path and policy of every route that Program.cs mapped inline before the routes moved
/// into Endpoints/. Most of these routes have no HTTP test of their own, so this test is what catches
/// a move that drops a RequireAuthorization or changes a verb. Endpoint filters are not endpoint
/// metadata, so RequireUserIdFilter is proved by behaviour tests instead
/// (CompleteSale_WithATokenWithoutAUserId_ReturnsUnauthorized).
/// </summary>
[Collection("Integration")]
public class RouteTableTests : IntegrationTestBase
{
    private const string Anonymous = "(anonymous)";
    private const string AnyAuthenticatedUser = "(authenticated)";

    private static readonly string[] MovedRoutes =
    [
        $"GET / {Anonymous}",
        $"GET /version {Anonymous}",
        $"POST /auth/login {Anonymous}",
        $"GET /auth/me {AnyAuthenticatedUser}",
        $"POST /auth/change-password {AnyAuthenticatedUser}",
        "GET /products CanReadProducts",
        "GET /products/stock CanReadProducts",
        "POST /products CanManageProducts",
        "PUT /products/{id:guid} CanManageProducts",
        "DELETE /products/{id:guid} CanManageProducts",
        "POST /products/{id:guid}/adjust-quantity CanAdjustInventory",
        "POST /products/next-barcode CanManageProducts",
        "GET /payment-methods CanReadProducts",
        "GET /admin/payment-methods CanManagePaymentMethods",
        "POST /admin/payment-methods CanManagePaymentMethods",
        "PATCH /admin/payment-methods/{code} CanManagePaymentMethods",
        "GET /product-categories CanReadProducts",
        $"GET /store/features {AnyAuthenticatedUser}",
        "POST /sales/complete CanCompleteSales",
        "GET /sync/status CanViewSyncStatus",
        "GET /reports/sales-summary CanViewReports",
        "GET /reports/pay-later CanViewReports",
        "GET /reports/product-sales CanViewReports",
        "GET /reports/legacy/sales-summary CanViewReports",
        "GET /reports/legacy/payments-summary CanViewReports",
        $"GET /pay-later {AnyAuthenticatedUser}",
        $"GET /pay-later/{{id:guid}} {AnyAuthenticatedUser}",
        $"POST /pay-later/{{id:guid}}/record-payment {AnyAuthenticatedUser}"
    ];

    public RouteTableTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public void RouteTable_WithTheAppBuilt_ContainsEveryMovedRouteWithItsPolicy()
    {
        var table = Factory.Services.GetRequiredService<EndpointDataSource>()
                                    .Endpoints
                                    .OfType<RouteEndpoint>()
                                    .Select(Describe)
                                    .ToList();

        table.Should()
             .Contain(MovedRoutes);
    }

    private static string Describe(RouteEndpoint endpoint)
    {
        var methods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? [];
        var policies = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
                                        .Select(a => a.Policy ?? AnyAuthenticatedUser)
                                        .ToList();
        var policy = policies.Count == 0 ? Anonymous : string.Join(",", policies);

        return $"{string.Join(",", methods)} {endpoint.RoutePattern.RawText} {policy}";
    }
}
```

The two `/pay-later/{{id:guid}}` rows are interpolated strings, so the braces are doubled there. In the plain strings they stay single.

- [ ] **Step 2: Run the pin against today's `Program.cs`**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~RouteTableTests"`
Expected: PASS. It describes today's mapping.

- [ ] **Step 3: Prove the pin can fail, then revert**

In `src/IndyPOS.StoreHub/Program.cs:361`, temporarily change `}).RequireAuthorization("CanReadProducts");` (the end of `/products/stock`) to `}).RequireAuthorization();`. Run Step 2's command.
Expected: FAIL. The message lists `"GET /products/stock CanReadProducts"` as missing.
Revert the change (`git checkout -- src/IndyPOS.StoreHub/Program.cs`), and re-run to confirm it passes again.

- [ ] **Step 4: Commit the pin**

```bash
git add tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/RouteTableTests.cs
git commit -m "test(storehub): pin the verb, path and policy of every Program.cs route"
```

- [ ] **Step 5: Move the two helper files**

```bash
mkdir -p src/IndyPOS.StoreHub/Endpoints/Common
git mv src/IndyPOS.StoreHub/Endpoints/Cash/ClaimsPrincipalExtensions.cs src/IndyPOS.StoreHub/Endpoints/Common/ClaimsPrincipalExtensions.cs
git mv src/IndyPOS.StoreHub/Endpoints/Cash/RequireUserIdFilter.cs src/IndyPOS.StoreHub/Endpoints/Common/RequireUserIdFilter.cs
```

In `Endpoints/Common/ClaimsPrincipalExtensions.cs`, line 4, replace `namespace IndyPOS.StoreHub.Endpoints.Cash;` with `namespace IndyPOS.StoreHub.Endpoints.Common;`. Nothing else in that file changes.

Replace the whole of `Endpoints/Common/RequireUserIdFilter.cs`. Today's summary says it "Guards the whole /cash group". That stops being true now that `/sales` and `POST /sales/complete` use it, and Task 7 adds `adjust-quantity`:

```csharp
namespace IndyPOS.StoreHub.Endpoints.Common;

/// <summary>
/// Rejects a token without a usable user id up front (401), before the route runs. Add it to every
/// route or group that stamps the acting user on a write with
/// <see cref="ClaimsPrincipalExtensions.GetRequiredUserId"/>, so such a token cannot get as far as a
/// 500 from that call. The /cash group also guards its reads with it, so a token without an identity
/// never reaches cash data at all.
/// </summary>
internal sealed class RequireUserIdFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) =>
        context.HttpContext.User.FindUserId() is null
            ? ValueTask.FromResult<object?>(Results.Unauthorized())
            : next(context);
}
```

- [ ] **Step 6: Point the cash and sales files at the new namespace**

Add the line `using IndyPOS.StoreHub.Endpoints.Common;` to each file below, directly after the line quoted:

| File | Insert after |
|---|---|
| `Endpoints/Cash/CashCountEndpoints.cs` | line 2 `using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;` |
| `Endpoints/Cash/CashEndpoints.cs` | line 1 `using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Summary;` |
| `Endpoints/Cash/CashFloatEndpoints.cs` | line 4 `using IndyPOS.Domain.Entities.Core;` |
| `Endpoints/Cash/CashPayoutEndpoints.cs` | line 4 `using IndyPOS.Domain.Entities.Core;` |
| `Endpoints/Cash/DebtRepaymentEndpoints.cs` | line 4 `using IndyPOS.Domain.Entities.Core;` |
| `Endpoints/Cash/TodayOnlyBusinessDateFilter.cs` | line 2 `using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;` |

In each of these, replace `using IndyPOS.StoreHub.Endpoints.Cash;` with `using IndyPOS.StoreHub.Endpoints.Common;`:
- `Endpoints/Sales/SaleQueryEndpoints.cs` line 5;
- `Endpoints/Sales/SaleReprintEndpoints.cs` line 4;
- `Endpoints/Sales/SalesEndpoints.cs` line 1.

In `Program.cs`, keep line 35 `using IndyPOS.StoreHub.Endpoints.Cash;`, which still supplies `CashEndpoints` and `AddCashDrawer`. Insert `using IndyPOS.StoreHub.Endpoints.Common;` after it. `POST /sales/complete` (lines 553 and 567) still calls `GetRequiredUserId` and `RequireUserIdFilter` from `Program.cs` until Task 5.

- [ ] **Step 7: Build and run the StoreHub suite**

Run: `dotnet build src/IndyPOS.StoreHub` and expect 0 errors.
Then run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests`
Expected: all **242** pass (241 plus the pin).

- [ ] **Step 8: Commit**

```bash
git add src/IndyPOS.StoreHub
git commit -m "refactor(storehub): move the user-id filter and claims helpers to Endpoints/Common"
```

---

### Task 2: Move the system-info and auth routes

**Files:**
- Create: `src/IndyPOS.StoreHub/Endpoints/SystemInfo/SystemInfoEndpoints.cs`
- Create: `src/IndyPOS.StoreHub/Endpoints/Auth/AuthEndpoints.cs`
- Modify: `src/IndyPOS.StoreHub/Program.cs:258-332`

**Interfaces:**
- Consumes: nothing new.
- Produces: `SystemInfoEndpoints.MapSystemInfoEndpoints(this IEndpointRouteBuilder)` and `AuthEndpoints.MapAuthEndpoints(this IEndpointRouteBuilder)`, both returning `IEndpointRouteBuilder`. It also adds the "Routes" block in `Program.cs`, which Tasks 3-6 extend.

- [ ] **Step 1: Create `Endpoints/SystemInfo/SystemInfoEndpoints.cs`**

`/version` used to read `app.Environment.EnvironmentName`. An extension on `IEndpointRouteBuilder` has no `app.Environment`, so the route now injects `IWebHostEnvironment`. It is the same singleton and gives the same value.

```csharp
namespace IndyPOS.StoreHub.Endpoints.SystemInfo;

/// <summary>The two unauthenticated routes that say what is running: a banner and the build's version.</summary>
public static class SystemInfoEndpoints
{
    public static IEndpointRouteBuilder MapSystemInfoEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", () => "IndyPOS StoreHub API");
        MapVersion(app);
        return app;
    }

    // Version endpoint (Velopack prep - used for update checks)
    private static void MapVersion(IEndpointRouteBuilder app)
    {
        app.MapGet("/version", (IWebHostEnvironment environment) =>
        {
            var versionInfo = IndyPOS.Application.Common.AppVersion.GetVersionInfo(typeof(Program).Assembly);

            return Results.Ok(new
            {
                version = versionInfo.DisplayVersion,
                assemblyVersion = versionInfo.AssemblyVersion,
                fullVersion = versionInfo.InformationalVersion,
                name = "IndyPOS.StoreHub",
                environment = environment.EnvironmentName
            });
        });
    }
}
```

- [ ] **Step 2: Create `Endpoints/Auth/AuthEndpoints.cs`**

```csharp
using System.Security.Claims;
using IndyPOS.Application.UseCases.StoreHub.Auth;
using IndyPOS.Application.UseCases.StoreHub.Auth.ChangePassword;
using IndyPOS.Application.UseCases.StoreHub.Auth.Login;
using Nokpirab;

namespace IndyPOS.StoreHub.Endpoints.Auth;

/// <summary>
/// Sign-in and the signed-in user. The must_change gate in Program.cs matches /auth/change-password
/// by path: a token that must change its password may reach that route and no other.
/// </summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        MapLogin(app);
        MapCurrentUser(app);
        MapChangePassword(app);
        return app;
    }

    private static void MapLogin(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (
            ICommandHandler<LoginCommand, LoginResponse> handler,
            LoginRequest request,
            CancellationToken cancellationToken) =>
        {
            var command = new LoginCommand(request.Username, request.Password);
            var response = await handler.HandleAsync(command, cancellationToken);

            return response.Success
                ? Results.Ok(response)
                : Results.Unauthorized();
        });
    }

    private static void MapCurrentUser(IEndpointRouteBuilder app)
    {
        app.MapGet("/auth/me", (HttpContext context) =>
        {
            var user = context.User;
            if (user.Identity?.IsAuthenticated != true)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new
            {
                userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value,
                username = user.FindFirst("unique_name")?.Value,
                roleId = user.FindFirst("role_id")?.Value,
                storeId = user.FindFirst("store_id")?.Value,
                firstName = user.FindFirst("first_name")?.Value,
                lastName = user.FindFirst("last_name")?.Value
            });
        }).RequireAuthorization();
    }

    private static void MapChangePassword(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/change-password", async (
            ICommandHandler<ChangePasswordCommand, ChangePasswordResponse> handler,
            HttpContext context,
            ChangePasswordRequest request,
            CancellationToken cancellationToken) =>
        {
            var idValue = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? context.User.FindFirst("sub")?.Value;

            if (!Guid.TryParse(idValue, out var userId))
            {
                return Results.Unauthorized();
            }

            var command = new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword);
            var response = await handler.HandleAsync(command, cancellationToken);

            return response.Success
                ? Results.Ok(response)
                : Results.BadRequest(new { error = response.ErrorMessage });
        }).RequireAuthorization();
    }
}
```

- [ ] **Step 3: Cut the routes from `Program.cs`**

Delete `Program.cs` lines 258-332. The cut runs from `// Minimal API endpoints` through the closing `});` of `app.MapGet("/version", …)`. It includes, in order:
- `app.MapGet("/", …)`;
- `// Auth endpoints`;
- the three `/auth/*` routes;
- the two-line `// /health/ready is now served by …` comment;
- `// Version endpoint (Velopack prep - used for update checks)`;
- `/version`.

Insert in their place:

```csharp
// Routes, one file per area under Endpoints/. /health/ready is served by
// ServiceDefaults.MapDefaultEndpoints (tag filter on "ready"), backed by the DbContextCheck
// registered above.
app.MapSystemInfoEndpoints();
app.MapAuthEndpoints();
```

Add these usings to `Program.cs`, in the `IndyPOS.StoreHub.Endpoints.*` group (lines 35-36 plus the line Task 1 added), keeping it in alphabetical order:

```csharp
using IndyPOS.StoreHub.Endpoints.Auth;
using IndyPOS.StoreHub.Endpoints.SystemInfo;
```

- [ ] **Step 4: Build and run the proof**

Run:
- `dotnet build src/IndyPOS.StoreHub`, expecting 0 errors;
- `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~RouteTableTests|FullyQualifiedName~AuthEndpointTests|FullyQualifiedName~ChangePasswordEndpointTests|FullyQualifiedName~MustChangeGateTests"`, expecting all to pass;
- `dotnet test tests/IndyPOS.StoreHub.IntegrationTests`, expecting all 242 to pass.

- [ ] **Step 5: Commit**

```bash
git add src/IndyPOS.StoreHub
git commit -m "refactor(storehub): move the system-info and auth routes out of Program.cs"
```

---

### Task 3: Move the product routes

**Files:**
- Create: `src/IndyPOS.StoreHub/Endpoints/Products/ProductsEndpoints.cs`
- Modify: `src/IndyPOS.StoreHub/Program.cs:334-362` and `:445-540`

**Interfaces:**
- Consumes: nothing new.
- Produces: `ProductsEndpoints.MapProductsEndpoints(this IEndpointRouteBuilder)`, and the private method `MapAdjustQuantity(IEndpointRouteBuilder)`, which Task 7 changes.

- [ ] **Step 1: Create `Endpoints/Products/ProductsEndpoints.cs`**

```csharp
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.Products;
using IndyPOS.Application.UseCases.StoreHub.Products.AdjustQuantity;
using IndyPOS.Application.UseCases.StoreHub.Products.Create;
using IndyPOS.Application.UseCases.StoreHub.Products.Delete;
using IndyPOS.Application.UseCases.StoreHub.Products.GenerateBarcode;
using IndyPOS.Application.UseCases.StoreHub.Products.Get;
using IndyPOS.Application.UseCases.StoreHub.Products.GetStock;
using IndyPOS.Application.UseCases.StoreHub.Products.Update;
using Nokpirab;

namespace IndyPOS.StoreHub.Endpoints.Products;

/// <summary>The product catalogue, its stock balances, stock adjustments and barcode numbering.</summary>
public static class ProductsEndpoints
{
    public static IEndpointRouteBuilder MapProductsEndpoints(this IEndpointRouteBuilder app)
    {
        MapListProducts(app);
        MapProductStock(app);
        MapCreateProduct(app);
        MapUpdateProduct(app);
        MapDeleteProduct(app);
        MapAdjustQuantity(app);
        MapNextBarcode(app);
        return app;
    }

    private static void MapListProducts(IEndpointRouteBuilder app)
    {
        app.MapGet("/products", async (
            IQueryHandler<GetProductsQuery, IReadOnlyList<ProductDto>> handler,
            bool? activeOnly,
            string? category,
            string? search,
            CancellationToken cancellationToken) =>
        {
            var query = new GetProductsQuery(
                ActiveOnly: activeOnly ?? true,
                Category: category,
                SearchTerm: search);

            var products = await handler.HandleAsync(query, cancellationToken);
            return Results.Ok(products);
        }).RequireAuthorization("CanReadProducts");
    }

    // Current stock per product. Separate from /products on purpose: the POS caches
    // products for the session, and a quantity on that record would go stale at the
    // first sale on either terminal.
    private static void MapProductStock(IEndpointRouteBuilder app)
    {
        app.MapGet("/products/stock", async (
            IQueryHandler<GetProductStockQuery, IReadOnlyList<ProductStockDto>> handler,
            Guid? productId,
            CancellationToken cancellationToken) =>
        {
            var stock = await handler.HandleAsync(new GetProductStockQuery(productId), cancellationToken);
            return Results.Ok(stock);
        }).RequireAuthorization("CanReadProducts");
    }

    private static void MapCreateProduct(IEndpointRouteBuilder app)
    {
        app.MapPost("/products", async (
            ICommandHandler<CreateProductCommand, ProductDto> handler,
            CreateProductCommand command,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await handler.HandleAsync(command, cancellationToken);
                return Results.Created($"/products/{result.Id}", result);
            }
            catch (UnknownProductCategoryException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        }).RequireAuthorization("CanManageProducts");
    }

    private static void MapUpdateProduct(IEndpointRouteBuilder app)
    {
        app.MapPut("/products/{id:guid}", async (
            ICommandHandler<UpdateProductCommand, ProductDto> handler,
            Guid id,
            UpdateProductCommand command,
            CancellationToken cancellationToken) =>
        {
            // Ensure ID matches
            if (id != command.Id)
            {
                return Results.BadRequest("Product ID in URL does not match body");
            }

            try
            {
                var result = await handler.HandleAsync(command, cancellationToken);
                return Results.Ok(result);
            }
            catch (ProductNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (UnknownProductCategoryException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // Duplicate barcode or a store-type violation — both genuine conflicts.
                return Results.Conflict(new { error = ex.Message });
            }
        }).RequireAuthorization("CanManageProducts");
    }

    // Soft delete
    private static void MapDeleteProduct(IEndpointRouteBuilder app)
    {
        app.MapDelete("/products/{id:guid}", async (
            ICommandHandler<DeleteProductCommand> handler,
            Guid id,
            CancellationToken cancellationToken) =>
        {
            await handler.HandleAsync(new DeleteProductCommand(id), cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization("CanManageProducts");
    }

    // Adjust product quantity by a signed delta
    private static void MapAdjustQuantity(IEndpointRouteBuilder app)
    {
        app.MapPost("/products/{id:guid}/adjust-quantity", async (
            ICommandHandler<AdjustProductQuantityCommand, int> handler,
            Guid id,
            AdjustQuantityRequest request,
            CancellationToken cancellationToken) =>
        {
            if (request.Delta == 0)
            {
                return Results.BadRequest(new { error = "Delta must not be zero." });
            }

            var command = new AdjustProductQuantityCommand
            {
                ProductId = id,
                Delta = request.Delta,
                Reason = request.Reason
            };

            var newBalance = await handler.HandleAsync(command, cancellationToken);
            return Results.Ok(new AdjustQuantityResponse(id, newBalance));
        }).RequireAuthorization("CanAdjustInventory");
    }

    private static void MapNextBarcode(IEndpointRouteBuilder app)
    {
        app.MapPost("/products/next-barcode", async (
            IQueryHandler<GenerateBarcodeQuery, string> handler,
            CancellationToken cancellationToken) =>
        {
            var barcode = await handler.HandleAsync(new GenerateBarcodeQuery(), cancellationToken);
            return Results.Ok(new { barcode });
        }).RequireAuthorization("CanManageProducts");
    }
}
```

- [ ] **Step 2: Cut the routes from `Program.cs`**

Delete two blocks:
- **Lines 334-362:** from `// Products endpoint` through the blank line after `/products/stock`'s `}).RequireAuthorization("CanReadProducts");`.
- **Lines 445-540:** from `// Create product` through the blank line after `/products/next-barcode`'s `}).RequireAuthorization("CanManageProducts");`.

Leave lines 363-444 (payment methods and catalogue) in place for Task 4.

In the Routes block, add `app.MapProductsEndpoints();` after `app.MapAuthEndpoints();`. Then add `using IndyPOS.StoreHub.Endpoints.Products;` to the endpoint usings.

- [ ] **Step 3: Build and run the proof**

Run:
- `dotnet build src/IndyPOS.StoreHub`, expecting 0 errors;
- `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~RouteTableTests|FullyQualifiedName~ProductsEndpointTests|FullyQualifiedName~ProductStockEndpointTests"`, expecting all to pass;
- `dotnet test tests/IndyPOS.StoreHub.IntegrationTests`, expecting all 242 to pass.

- [ ] **Step 4: Commit**

```bash
git add src/IndyPOS.StoreHub
git commit -m "refactor(storehub): move the product routes out of Program.cs"
```

---

### Task 4: Move the payment-method and catalogue routes

**Files:**
- Create: `src/IndyPOS.StoreHub/Endpoints/PaymentMethods/PaymentMethodsEndpoints.cs`
- Create: `src/IndyPOS.StoreHub/Endpoints/Catalogue/CatalogueEndpoints.cs`
- Modify: `src/IndyPOS.StoreHub/Program.cs:363-444`

**Interfaces:**
- Consumes: nothing new.
- Produces: `PaymentMethodsEndpoints.MapPaymentMethodsEndpoints(this IEndpointRouteBuilder)` and `CatalogueEndpoints.MapCatalogueEndpoints(this IEndpointRouteBuilder)`.

- [ ] **Step 1: Create `Endpoints/PaymentMethods/PaymentMethodsEndpoints.cs`**

```csharp
using IndyPOS.Application.UseCases.StoreHub.PaymentMethods;
using Nokpirab;

namespace IndyPOS.StoreHub.Endpoints.PaymentMethods;

/// <summary>
/// Payment methods under two roots: /payment-methods is what the till may offer, and
/// /admin/payment-methods is the catalogue an admin manages.
/// </summary>
public static class PaymentMethodsEndpoints
{
    public static IEndpointRouteBuilder MapPaymentMethodsEndpoints(this IEndpointRouteBuilder app)
    {
        MapOfferable(app);
        MapCatalogue(app);
        MapAddCampaign(app);
        MapUpdateMethod(app);
        return app;
    }

    // Payment methods endpoint (offerable methods for this store)
    private static void MapOfferable(IEndpointRouteBuilder app)
    {
        app.MapGet("/payment-methods", async (
            IQueryHandler<GetOfferablePaymentMethodsQuery, IReadOnlyList<PaymentMethodDto>> handler,
            CancellationToken cancellationToken) =>
        {
            var methods = await handler.HandleAsync(new GetOfferablePaymentMethodsQuery(), cancellationToken);
            return Results.Ok(methods);
        }).RequireAuthorization("CanReadProducts");
    }

    // Admin: list all payment methods (enabled + disabled)
    private static void MapCatalogue(IEndpointRouteBuilder app)
    {
        app.MapGet("/admin/payment-methods", async (
            IQueryHandler<GetAllPaymentMethodsQuery, IReadOnlyList<PaymentMethodDto>> handler,
            CancellationToken cancellationToken) =>
        {
            var methods = await handler.HandleAsync(new GetAllPaymentMethodsQuery(), cancellationToken);
            return Results.Ok(methods);
        }).RequireAuthorization("CanManagePaymentMethods");
    }

    // Admin: add a government-campaign payment method
    private static void MapAddCampaign(IEndpointRouteBuilder app)
    {
        app.MapPost("/admin/payment-methods", async (
            ICommandHandler<AddCampaignPaymentMethodCommand, PaymentMethodMutationResponse> handler,
            AddCampaignPaymentMethodRequest request,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new AddCampaignPaymentMethodCommand(request.Code, request.DisplayName, request.DisplayOrder);
                var result = await handler.HandleAsync(command, cancellationToken);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        }).RequireAuthorization("CanManagePaymentMethods");
    }

    // Admin: toggle enabled state and/or edit display of a payment method
    private static void MapUpdateMethod(IEndpointRouteBuilder app)
    {
        app.MapPatch("/admin/payment-methods/{code}", async (
            ICommandHandler<TogglePaymentMethodCommand, PaymentMethodMutationResponse> toggleHandler,
            ICommandHandler<EditPaymentMethodDisplayCommand, PaymentMethodMutationResponse> editHandler,
            string code,
            UpdatePaymentMethodRequest request,
            CancellationToken cancellationToken) =>
        {
            try
            {
                if (request.IsEnabled is bool enabled)
                {
                    await toggleHandler.HandleAsync(new TogglePaymentMethodCommand(code, enabled), cancellationToken);
                }

                if (request.DisplayName is not null)
                {
                    await editHandler.HandleAsync(
                        new EditPaymentMethodDisplayCommand(code, request.DisplayName, request.DisplayOrder),
                        cancellationToken);
                }

                return Results.Ok();
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        }).RequireAuthorization("CanManagePaymentMethods");
    }
}
```

- [ ] **Step 2: Create `Endpoints/Catalogue/CatalogueEndpoints.cs`**

```csharp
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.StoreHub.ProductCategories;
using Nokpirab;

namespace IndyPOS.StoreHub.Endpoints.Catalogue;

/// <summary>What this store sells and how: its product categories and its store-type feature flags.</summary>
public static class CatalogueEndpoints
{
    public static IEndpointRouteBuilder MapCatalogueEndpoints(this IEndpointRouteBuilder app)
    {
        MapProductCategories(app);
        MapStoreFeatures(app);
        return app;
    }

    // Product categories for this store (the POS renders pickers from this)
    private static void MapProductCategories(IEndpointRouteBuilder app)
    {
        app.MapGet("/product-categories", async (
            IQueryHandler<GetProductCategoriesQuery, IReadOnlyList<ProductCategoryDto>> handler,
            CancellationToken cancellationToken) =>
        {
            var categories = await handler.HandleAsync(new GetProductCategoriesQuery(), cancellationToken);
            return Results.Ok(categories);
        }).RequireAuthorization("CanReadProducts");
    }

    // Store feature flags (store-type gating for WinForms clients)
    private static void MapStoreFeatures(IEndpointRouteBuilder app)
    {
        app.MapGet("/store/features", (IStoreIdentityService storeIdentity) =>
        {
            var f = storeIdentity.Features;
            return Results.Ok(new StoreFeaturesDto(f.PayLaterEnabled, f.MultipleProductTypesEnabled));
        }).RequireAuthorization();
    }
}
```

- [ ] **Step 3: Cut the routes from `Program.cs`**

Delete lines 363-444. The cut runs from `// Payment methods endpoint (offerable methods for this store)` through the blank line after `/admin/payment-methods/{code}`'s `}).RequireAuthorization("CanManagePaymentMethods");`. It holds:
- `/payment-methods`;
- `/product-categories`;
- `/store/features`;
- the three `/admin/payment-methods` routes.

In the Routes block, add these after `app.MapProductsEndpoints();`:

```csharp
app.MapPaymentMethodsEndpoints();
app.MapCatalogueEndpoints();
```

Add `using IndyPOS.StoreHub.Endpoints.Catalogue;` and `using IndyPOS.StoreHub.Endpoints.PaymentMethods;` to the endpoint usings.

- [ ] **Step 4: Build and run the proof**

Run:
- `dotnet build src/IndyPOS.StoreHub`, expecting 0 errors;
- `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~RouteTableTests|FullyQualifiedName~StoreFeaturesEndpointTests|FullyQualifiedName~PayLaterSaleEndpointTests"`, expecting all to pass;
- `dotnet test tests/IndyPOS.StoreHub.IntegrationTests`, expecting all 242 to pass.

`PayLaterSaleEndpointTests` is here because a credit sale depends on the offerable payment-method set.

- [ ] **Step 5: Commit**

```bash
git add src/IndyPOS.StoreHub
git commit -m "refactor(storehub): move the payment-method and catalogue routes out of Program.cs"
```

---

### Task 5: Move `POST /sales/complete` and the sync route

**Files:**
- Create: `src/IndyPOS.StoreHub/Endpoints/Sales/SaleCompletionEndpoints.cs`
- Create: `src/IndyPOS.StoreHub/Endpoints/Sync/SyncEndpoints.cs`
- Modify: `src/IndyPOS.StoreHub/Endpoints/Sales/SalesEndpoints.cs` (whole file)
- Modify: `src/IndyPOS.StoreHub/Program.cs:541-585`, `:738-740` and the `Endpoints.Common` using added in Task 1

**Interfaces:**
- Consumes: `RequireUserIdFilter` and `ClaimsPrincipalExtensions.GetRequiredUserId` from `IndyPOS.StoreHub.Endpoints.Common` (Task 1).
- Produces:
  - `SaleCompletionEndpoints.MapSaleCompletion(this IEndpointRouteBuilder)` (internal), called by `MapSalesEndpoints`, and changed again by Task 9;
  - `SyncEndpoints.MapSyncEndpoints(this IEndpointRouteBuilder)`.

- [ ] **Step 1: Create `Endpoints/Sales/SaleCompletionEndpoints.cs`**

```csharp
using System.Security.Claims;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.Sales;
using IndyPOS.Application.UseCases.StoreHub.Sales.Complete;
using IndyPOS.StoreHub.Endpoints.Common;
using Nokpirab;

namespace IndyPOS.StoreHub.Endpoints.Sales;

internal static class SaleCompletionEndpoints
{
    public static void MapSaleCompletion(this IEndpointRouteBuilder app)
    {
        app.MapPost("/sales/complete", async (
            ICommandHandler<CompleteSaleCommand, CompleteSaleResponse> handler,
            IStoreIdentityService storeIdentity,
            ClaimsPrincipal user,
            CompleteSaleRequest request,
            CancellationToken cancellationToken) =>
        {
            // The seller is whoever the token says, never the body: a body UserId let any caller ring a sale
            // up as someone else. request.UserId is deprecated and ignored.
            var command = new CompleteSaleCommand(
                StoreId: storeIdentity.StoreId,
                UserId: user.GetRequiredUserId(),
                Lines: request.Lines,
                Payments: request.Payments);

            // A refused sale is the caller's mistake, not the server's: a Thai reason for the cashier.
            try
            {
                return Results.Ok(await handler.HandleAsync(command, cancellationToken));
            }
            catch (SaleValidationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization("CanCompleteSales")
          .AddEndpointFilter<RequireUserIdFilter>();
    }
}
```

The two-line "seller" comment is still true here. Task 9 rewrites it when `request.UserId` goes away.

- [ ] **Step 2: Replace `Endpoints/Sales/SalesEndpoints.cs`**

Today's summary says (line 7) "POST /sales/complete is still mapped in Program.cs — renaming it is the route tidy-up PR." That becomes false with this task. Replace the whole file:

```csharp
using IndyPOS.StoreHub.Endpoints.Common;

namespace IndyPOS.StoreHub.Endpoints.Sales;

/// <summary>
/// Bills as one REST resource (spec §6). sales.reprint opens the /sales group; reports.view lifts the
/// today-only limit. POST /sales/complete is mapped beside the group, not inside it: it needs
/// CanCompleteSales instead of the group's policy, and it answers its own 400.
/// </summary>
public static class SalesEndpoints
{
    public const string Policy = "CanReprintSales";

    public static IEndpointRouteBuilder MapSalesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapSaleCompletion();

        var sales = app.MapGroup("/sales")
                       .RequireAuthorization(Policy)
                       .AddEndpointFilter<RequireUserIdFilter>()
                       .AddEndpointFilter<SalesExceptionFilter>();

        sales.MapSaleQueries();
        sales.MapSaleReprints();
        return app;
    }
}
```

- [ ] **Step 3: Create `Endpoints/Sync/SyncEndpoints.cs`**

```csharp
using IndyPOS.Application.Abstractions.StoreHub.Repositories;

namespace IndyPOS.StoreHub.Endpoints.Sync;

/// <summary>The outbox's health: how many events still wait to reach the cloud, and how many failed.</summary>
public static class SyncEndpoints
{
    public static IEndpointRouteBuilder MapSyncEndpoints(this IEndpointRouteBuilder app)
    {
        // Sync status endpoint (E4)
        app.MapGet("/sync/status", async (
            IOutboxRepository outboxRepository,
            CancellationToken cancellationToken) =>
        {
            var pendingCount = await outboxRepository.GetPendingCountAsync(cancellationToken);
            var failedCount = await outboxRepository.GetFailedCountAsync(cancellationToken);

            return Results.Ok(new
            {
                status = pendingCount == 0 ? "synced" : "pending",
                pending = pendingCount,
                failed = failedCount,
                timestamp = DateTime.UtcNow
            });
        }).RequireAuthorization("CanViewSyncStatus");

        return app;
    }
}
```

- [ ] **Step 4: Cut the routes from `Program.cs`**

Delete two blocks:
- **Lines 541-585:** from `// Sales endpoint` through the blank line after `/sync/status`'s `}).RequireAuthorization("CanViewSyncStatus");`. This holds `POST /sales/complete` and `/sync/status`.
- **Lines 738-740:** these are three lines:

  ```csharp

  // Sales history routes (/sales/...). POST /sales/complete above is unchanged.
  app.MapSalesEndpoints();
  ```

  That comment would now be wrong, because nothing "above" maps `/sales/complete` any more.

In the Routes block, add these after `app.MapCatalogueEndpoints();`:

```csharp
app.MapSalesEndpoints();
app.MapSyncEndpoints();
```

Remove `using IndyPOS.StoreHub.Endpoints.Common;` (added in Task 1). `Program.cs` no longer uses it. Add `using IndyPOS.StoreHub.Endpoints.Sync;`. Keep `using IndyPOS.StoreHub.Endpoints.Sales;`, which still supplies `SalesEndpoints.Policy` and `AddSalesHistory`.

- [ ] **Step 5: Build and run the proof**

Run:
- `dotnet build src/IndyPOS.StoreHub`, expecting 0 errors;
- `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~RouteTableTests|FullyQualifiedName~SalesEndpointTests|FullyQualifiedName~SalesHistoryEndpointsTests|FullyQualifiedName~SaleReprintEndpointsTests|FullyQualifiedName~SyncEndpointTests|FullyQualifiedName~PayLaterSaleEndpointTests"`, expecting all to pass. `CompleteSale_WithATokenWithoutAUserId_ReturnsUnauthorized` is the proof that the filter came along;
- `dotnet test tests/IndyPOS.StoreHub.IntegrationTests`, expecting all 242 to pass.

- [ ] **Step 6: Commit**

```bash
git add src/IndyPOS.StoreHub
git commit -m "refactor(storehub): move POST /sales/complete and /sync/status out of Program.cs"
```

---

### Task 6: Move the report and PayLater routes, so that `Program.cs` holds wiring only

**Files:**
- Create: `src/IndyPOS.StoreHub/Endpoints/Reports/ReportsEndpoints.cs`
- Create: `src/IndyPOS.StoreHub/Endpoints/PayLater/PayLaterEndpoints.cs`
- Modify: `src/IndyPOS.StoreHub/Endpoints/Cash/CashEndpoints.cs:6-9` (doc comment)
- Modify: `src/IndyPOS.StoreHub/Program.cs:1-41` (usings) and `:586-737`

**Interfaces:**
- Consumes: nothing new.
- Produces:
  - `ReportsEndpoints.MapReportsEndpoints(this IEndpointRouteBuilder)`, with private methods `MapSalesSummary`, `MapPayLaterReport`, `MapProductSales`, `MapLegacySalesSummary` and `MapLegacyPaymentsSummary`. Task 10 changes four of them.
  - `PayLaterEndpoints.MapPayLaterEndpoints(this IEndpointRouteBuilder)`.

- [ ] **Step 1: Create `Endpoints/Reports/ReportsEndpoints.cs`**

```csharp
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.StoreHub.Reports;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetLegacyPaymentsSummary;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetLegacySalesSummary;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetPayLaterReport;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetProductSales;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetSalesSummary;
using Nokpirab;

namespace IndyPOS.StoreHub.Endpoints.Reports;

/// <summary>
/// Aggregates for the back office, all behind reports.view. /reports/legacy/* answer in the WinForms
/// models' shape (for WinForms compatibility).
/// </summary>
public static class ReportsEndpoints
{
    public static IEndpointRouteBuilder MapReportsEndpoints(this IEndpointRouteBuilder app)
    {
        MapSalesSummary(app);
        MapPayLaterReport(app);
        MapProductSales(app);
        MapLegacySalesSummary(app);
        MapLegacyPaymentsSummary(app);
        return app;
    }

    // Sales summary (daily/weekly/monthly dashboard)
    private static void MapSalesSummary(IEndpointRouteBuilder app)
    {
        app.MapGet("/reports/sales-summary", async (
            IQueryHandler<GetSalesSummaryQuery, SalesSummaryDto> handler,
            DateOnly fromDate,
            DateOnly toDate,
            int? topProductsCount,
            CancellationToken cancellationToken) =>
        {
            var query = new GetSalesSummaryQuery(
                FromDate: fromDate,
                ToDate: toDate,
                TopProductsCount: topProductsCount ?? 10);

            var result = await handler.HandleAsync(query, cancellationToken);
            return Results.Ok(result);
        }).RequireAuthorization("CanViewReports");
    }

    // PayLater (accounts receivable) report
    private static void MapPayLaterReport(IEndpointRouteBuilder app)
    {
        app.MapGet("/reports/pay-later", async (
            IQueryHandler<GetPayLaterReportQuery, PayLaterReportDto> handler,
            bool? includeCompleted,
            int? page,
            int? pageSize,
            CancellationToken cancellationToken) =>
        {
            var query = new GetPayLaterReportQuery(
                IncludeCompleted: includeCompleted ?? false,
                Page: page ?? 1,
                PageSize: pageSize ?? 50);

            var result = await handler.HandleAsync(query, cancellationToken);
            return Results.Ok(result);
        }).RequireAuthorization("CanViewReports");
    }

    // Product sales report
    private static void MapProductSales(IEndpointRouteBuilder app)
    {
        app.MapGet("/reports/product-sales", async (
            IQueryHandler<GetProductSalesQuery, PagedResult<ProductSalesDto>> handler,
            DateOnly fromDate,
            DateOnly toDate,
            string? category,
            int? page,
            int? pageSize,
            CancellationToken cancellationToken) =>
        {
            var query = new GetProductSalesQuery(
                FromDate: fromDate,
                ToDate: toDate,
                Category: category,
                Page: page ?? 1,
                PageSize: pageSize ?? 50);

            var result = await handler.HandleAsync(query, cancellationToken);
            return Results.Ok(result);
        }).RequireAuthorization("CanViewReports");
    }

    // Legacy sales summary (returns SalesSummary model)
    private static void MapLegacySalesSummary(IEndpointRouteBuilder app)
    {
        app.MapGet("/reports/legacy/sales-summary", async (
            IQueryHandler<GetLegacySalesSummaryQuery, SalesSummary> handler,
            DateOnly fromDate,
            DateOnly toDate,
            CancellationToken cancellationToken) =>
        {
            var query = new GetLegacySalesSummaryQuery(fromDate, toDate);
            var result = await handler.HandleAsync(query, cancellationToken);
            return Results.Ok(result);
        }).RequireAuthorization("CanViewReports");
    }

    // Legacy payments summary (returns PaymentsSummary model)
    private static void MapLegacyPaymentsSummary(IEndpointRouteBuilder app)
    {
        app.MapGet("/reports/legacy/payments-summary", async (
            IQueryHandler<GetLegacyPaymentsSummaryQuery, PaymentsSummary> handler,
            DateOnly fromDate,
            DateOnly toDate,
            CancellationToken cancellationToken) =>
        {
            var query = new GetLegacyPaymentsSummaryQuery(fromDate, toDate);
            var result = await handler.HandleAsync(query, cancellationToken);
            return Results.Ok(result);
        }).RequireAuthorization("CanViewReports");
    }
}
```

- [ ] **Step 2: Create `Endpoints/PayLater/PayLaterEndpoints.cs`**

```csharp
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.PayLater;
using Nokpirab;

namespace IndyPOS.StoreHub.Endpoints.PayLater;

/// <summary>PayLater accounts: for cashiers to view and update the debts customers carry.</summary>
public static class PayLaterEndpoints
{
    public static IEndpointRouteBuilder MapPayLaterEndpoints(this IEndpointRouteBuilder app)
    {
        MapListDebts(app);
        MapDebtById(app);
        MapRecordPayment(app);
        return app;
    }

    // List pay-later records
    private static void MapListDebts(IEndpointRouteBuilder app)
    {
        app.MapGet("/pay-later", async (
            IQueryHandler<GetPayLaterQuery, GetPayLaterResponse> handler,
            bool? includeCompleted,
            string? search,
            CancellationToken cancellationToken) =>
        {
            var query = new GetPayLaterQuery(
                IncludeCompleted: includeCompleted ?? false,
                SearchTerm: search);

            var result = await handler.HandleAsync(query, cancellationToken);
            return Results.Ok(result);
        }).RequireAuthorization();
    }

    // Get single pay-later record
    private static void MapDebtById(IEndpointRouteBuilder app)
    {
        app.MapGet("/pay-later/{id:guid}", async (
            IQueryHandler<GetPayLaterByIdQuery, PayLaterDto> handler,
            Guid id,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await handler.HandleAsync(new GetPayLaterByIdQuery(id), cancellationToken);
                return Results.Ok(result);
            }
            catch (PayLaterPaymentNotFoundException)
            {
                return Results.NotFound();
            }
        }).RequireAuthorization();
    }

    // Record payment against pay-later
    private static void MapRecordPayment(IEndpointRouteBuilder app)
    {
        app.MapPost("/pay-later/{id:guid}/record-payment", async (
            ICommandHandler<RecordPayLaterPaymentCommand, PayLaterDto> handler,
            Guid id,
            RecordPaymentRequest request,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new RecordPayLaterPaymentCommand(id, request.PaymentAmount);
                var result = await handler.HandleAsync(command, cancellationToken);
                return Results.Ok(result);
            }
            catch (PayLaterPaymentNotFoundException)
            {
                return Results.NotFound();
            }
            catch (PayLaterPaymentNotUpdatedException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization();
    }
}
```

- [ ] **Step 3: Cut the last routes from `Program.cs`**

Delete lines 586-737. The cut runs from the `// ========================` banner above `// Report endpoints` through `app.MapCashEndpoints();`. It holds:
- the five `/reports/*` routes and their two banners;
- the PayLater banner and its three routes;
- `// Cash drawer routes (/cash/...)` and `app.MapCashEndpoints();`.

Keep the blank line and `app.Run();` that follow. The Routes block and the end of the file now read:

```csharp
// Routes, one file per area under Endpoints/. /health/ready is served by
// ServiceDefaults.MapDefaultEndpoints (tag filter on "ready"), backed by the DbContextCheck
// registered above.
app.MapSystemInfoEndpoints();
app.MapAuthEndpoints();
app.MapProductsEndpoints();
app.MapPaymentMethodsEndpoints();
app.MapCatalogueEndpoints();
app.MapSalesEndpoints();
app.MapSyncEndpoints();
app.MapReportsEndpoints();
app.MapPayLaterEndpoints();
app.MapCashEndpoints();

app.Run();

// Make the implicit Program class public so test projects can access it
public partial class Program;
```

- [ ] **Step 4: Tidy the `Program.cs` usings**

Only the moved routes used these four, so delete them:
- line 1 `using System.Security.Claims;`;
- line 3 `using IndyPOS.Application.Abstractions.StoreHub.Repositories;`;
- line 5 `using IndyPOS.Application.Common.Exceptions;`;
- line 6 `using IndyPOS.Application.Common.Interfaces;`.

Leave every other `IndyPOS.Application.*` using alone. The handler registrations at lines 92-125 still need them. Examples are `LoginResponse` (`…StoreHub.Auth`), `CompleteSaleResponse` (`…StoreHub.Sales`), `PagedResult` (`…StoreHub.Reports`) and `SalesSummary` (`…Common.Models`).

Add `using IndyPOS.StoreHub.Endpoints.PayLater;` and `using IndyPOS.StoreHub.Endpoints.Reports;`. The endpoint using group now reads:

```csharp
using IndyPOS.StoreHub.Endpoints.Auth;
using IndyPOS.StoreHub.Endpoints.Cash;
using IndyPOS.StoreHub.Endpoints.Catalogue;
using IndyPOS.StoreHub.Endpoints.PaymentMethods;
using IndyPOS.StoreHub.Endpoints.PayLater;
using IndyPOS.StoreHub.Endpoints.Products;
using IndyPOS.StoreHub.Endpoints.Reports;
using IndyPOS.StoreHub.Endpoints.Sales;
using IndyPOS.StoreHub.Endpoints.Sync;
using IndyPOS.StoreHub.Endpoints.SystemInfo;
```

- [ ] **Step 5: Correct the `/cash` summary comment**

`Endpoints/Cash/CashEndpoints.cs` lines 6-9 read:

```csharp
/// <summary>
/// The ลิ้นชักเก็บเงิน API. Cash routes live here rather than inline in Program.cs; moving the
/// existing routes out is a separate clean-up.
/// </summary>
```

That clean-up is now done. Replace those lines with:

```csharp
/// <summary>
/// The ลิ้นชักเก็บเงิน API: every /cash route, behind one policy and the group's three filters.
/// </summary>
```

- [ ] **Step 6: Check that `Program.cs` holds wiring only**

Run: `grep -nE "app\.Map(Get|Post|Put|Patch|Delete)\(" src/IndyPOS.StoreHub/Program.cs`
Expected: no output.

Run: `grep -rn "Program.cs" src/IndyPOS.StoreHub/Endpoints`
Expected: one hit, in `Endpoints/Auth/AuthEndpoints.cs` (the must_change gate). No comment may claim that a route is still mapped in `Program.cs`.

- [ ] **Step 7: Build and run the proof**

Run:
- `dotnet build src/IndyPOS.StoreHub`, expecting 0 errors;
- `dotnet test tests/IndyPOS.StoreHub.IntegrationTests`, expecting all 242 to pass, `RouteTableTests` among them;
- `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~StoreHubE2ETests|FullyQualifiedName~StoreHubHttpClientTests"`, expecting all to pass. These tests never call the real server, so they only confirm that the client side still compiles against the shared contracts.

- [ ] **Step 8: Commit**

```bash
git add src/IndyPOS.StoreHub
git commit -m "refactor(storehub): move the report and PayLater routes; Program.cs is wiring only"
```

---

### Task 7: `inventory_movement.created_by_user_id`, and `adjust-quantity` records its caller

**Files:**
- Modify: `src/IndyPOS.Domain/Entities/Core/InventoryMovement.cs:16-17`
- Modify: `src/IndyPOS.Infrastructure/Persistence/StoreHub/Configurations/InventoryMovementConfiguration.cs:40-42`
- Create: `src/IndyPOS.Infrastructure/Persistence/StoreHub/Migrations/<ts>_AddInventoryMovementUser.cs` and its `.Designer.cs` (generated); the snapshot changes too
- Modify: `src/IndyPOS.Application/UseCases/StoreHub/Products/AdjustQuantity/AdjustProductQuantityCommand.cs`
- Modify: `src/IndyPOS.Application/UseCases/StoreHub/Products/AdjustQuantity/AdjustProductQuantityCommandHandler.cs:9-13, 50-59`
- Modify: `src/IndyPOS.StoreHub/Endpoints/Products/ProductsEndpoints.cs` (`MapAdjustQuantity`, usings)
- Test: `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/ProductsEndpointTests.cs`

**Interfaces:**
- Consumes: `RequireUserIdFilter` and `GetRequiredUserId()` (Task 1). Test helpers: `IntegrationTestBase.TokenWithoutUserId(UserRole)`, `AuthenticateAsAsync(string, string, UserRole)` and `CreateTestProductAsync(...)`.
- Produces:
  - `InventoryMovement.CreatedByUserId : Guid?`, mapped to `created_by_user_id uuid NULL`;
  - `AdjustProductQuantityCommand.UserId : Guid` (`required`).
  Task 8 sets `CreatedByUserId` on sale movements.

- [ ] **Step 1: Write the failing tests**

In `ProductsEndpointTests.cs`, add these usings after line 2 `using System.Net.Http.Json;`:

```csharp
using System.Net.Http.Headers;
```

Add this after line 4 `using IndyPOS.Application.Common.Constants;`:

```csharp
using IndyPOS.Application.Common.Enums;
```

Add this after line 10 `using IndyPOS.Infrastructure.Persistence.StoreHub;`:

```csharp
using Microsoft.EntityFrameworkCore;
```

Add these constants after line 21, the constructor:

```csharp
    private const int Restock = 5;
    private const string AdjustmentReason = "Adjustment";
```

Add these three tests directly after `AdjustQuantity_AsManager_ShouldApplyTheDeltaToTheBalance`. The two negative tests come first.

```csharp
    // The adjustment now records who made it, so a token without a usable user id is refused up front:
    // a 401, not a 200 with a NULL user and not a 500 from GetRequiredUserId.
    [Fact]
    public async Task AdjustQuantity_WithATokenWithoutAUserId_ReturnsUnauthorized()
    {
        var product = await CreateTestProductAsync(initialStock: 100);
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TokenWithoutUserId(UserRole.StoreManager));

        var response = await Client.PostAsJsonAsync($"/products/{product.Id}/adjust-quantity", new { delta = Restock });

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdjustQuantity_WithATokenWithoutAUserId_WritesNoMovement()
    {
        var product = await CreateTestProductAsync(initialStock: 100);
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TokenWithoutUserId(UserRole.StoreManager));

        await Client.PostAsJsonAsync($"/products/{product.Id}/adjust-quantity", new { delta = Restock });

        (await AdjustmentsOfAsync(product.Id)).Should()
                                              .BeEmpty();
    }

    [Fact]
    public async Task AdjustQuantity_WithAValidToken_RecordsTheCaller()
    {
        var manager = $"manager_{Guid.NewGuid():N}";
        await AuthenticateAsAsync(manager, "Manager123!", UserRole.StoreManager);
        var product = await CreateTestProductAsync(initialStock: 100);

        await Client.PostAsJsonAsync($"/products/{product.Id}/adjust-quantity", new { delta = Restock });

        (await AdjustmentsOfAsync(product.Id)).Should()
                                              .ContainSingle()
                                              .Which.CreatedByUserId.Should()
                                                                    .Be(await UserIdOfAsync(manager));
    }
```

Add these helpers just above `private record NextBarcodeResponse(string Barcode);`, near the end of the class:

```csharp
    // Only this product's rows: the shared test database is never reset between tests.
    private async Task<List<InventoryMovement>> AdjustmentsOfAsync(Guid productId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();

        return await db.InventoryMovements.AsNoTracking()
                                          .Where(m => m.ProductId == productId && m.Reason == AdjustmentReason)
                                          .ToListAsync();
    }

    private async Task<Guid> UserIdOfAsync(string username)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();

        return await db.StoreUsers.Where(u => u.Username == username)
                                  .Select(u => u.Id)
                                  .SingleAsync();
    }
```

- [ ] **Step 2: Run them to see the RED**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~ProductsEndpointTests.AdjustQuantity"`
Expected: the build fails with `CS1061: 'InventoryMovement' does not contain a definition for 'CreatedByUserId'`.
Comment out only `AdjustQuantity_WithAValidToken_RecordsTheCaller` and run again. Now:
- `…ReturnsUnauthorized` FAILS: "Expected … to be HttpStatusCode.Unauthorized … but found HttpStatusCode.OK";
- `…WritesNoMovement` FAILS: "Expected collection to be empty, but found 1 item".

Uncomment the test once you have read both failures.

- [ ] **Step 3: Add the column**

In `src/IndyPOS.Domain/Entities/Core/InventoryMovement.cs`, insert this between line 16 `public string? Note { get; set; }` and line 17 `public DateTime CreatedUtc { get; set; }`:

```csharp

    /// <summary>
    /// Who moved the stock: the seller for a sale, the caller for an adjustment. Null for rows written
    /// before the column existed, and for movements no signed-in user makes (initial stock on product
    /// creation, migrated v3 history).
    /// </summary>
    public Guid? CreatedByUserId { get; set; }
```

In `InventoryMovementConfiguration.cs`, insert this after the `Note` block, which ends at line 42 `.HasMaxLength(500);`:

```csharp

        builder.Property(e => e.CreatedByUserId)
            .HasColumnName("created_by_user_id");
```

- [ ] **Step 4: Generate the migration**

Run:
```bash
dotnet ef migrations add AddInventoryMovementUser --project src/IndyPOS.Infrastructure --startup-project src/IndyPOS.StoreHub --context StoreHubDbContext --output-dir Persistence/StoreHub/Migrations
dotnet ef migrations has-pending-model-changes --project src/IndyPOS.Infrastructure --startup-project src/IndyPOS.StoreHub --context StoreHubDbContext
```
Expected:
- The generated `Up` holds exactly one `migrationBuilder.AddColumn<Guid>(name: "created_by_user_id", table: "inventory_movement", type: "uuid", nullable: true);`.
- There is no `AlterColumn`, `DropX` or `RenameX`. If anything else appears, stop: the model drifted, and this migration must be additive only.
- `Down` drops that one column.
- The second command prints `No changes have been made to the model since the last migration.`

Leave the Designer and the snapshot as generated.

- [ ] **Step 5: Take the user from the token**

Replace the whole of `AdjustProductQuantityCommand.cs`:

```csharp
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Products.AdjustQuantity;

/// <summary>
/// Command to adjust product quantity via inventory movement.
/// Writes Delta straight through; it is never derived from a balance read.
/// </summary>
public record AdjustProductQuantityCommand : ICommand<int>
{
    public required Guid ProductId { get; init; }
    public required int Delta { get; init; }
    public string? Reason { get; init; }

    /// <summary>Who made the adjustment: the user in the caller's token, never a value from the body.</summary>
    public required Guid UserId { get; init; }
}
```

In `AdjustProductQuantityCommandHandler.cs`, lines 9-13, replace the class summary:

```csharp
/// <summary>
/// Handler for adjusting product quantity via inventory movement.
/// Writes the caller's delta straight to an Adjustment movement; nothing is calculated
/// from a balance read.
/// </summary>
```

with:

```csharp
/// <summary>
/// Handler for adjusting product quantity via inventory movement.
/// Writes the caller's delta straight to an Adjustment movement stamped with the caller; nothing is
/// calculated from a balance read.
/// </summary>
```

Then, in the `new InventoryMovement { … }` initializer (lines 50-59), insert `CreatedByUserId = command.UserId,` after `Note = command.Reason ?? "Manual adjustment",`.

In `Endpoints/Products/ProductsEndpoints.cs`, add these usings:
- `using System.Security.Claims;` as the first line;
- `using IndyPOS.StoreHub.Endpoints.Common;` before `using Nokpirab;`.

Then replace the whole of `MapAdjustQuantity`:

```csharp
    // Adjust product quantity by a signed delta. The adjuster is the token's user, never the body's.
    private static void MapAdjustQuantity(IEndpointRouteBuilder app)
    {
        app.MapPost("/products/{id:guid}/adjust-quantity", async (
            ICommandHandler<AdjustProductQuantityCommand, int> handler,
            ClaimsPrincipal user,
            Guid id,
            AdjustQuantityRequest request,
            CancellationToken cancellationToken) =>
        {
            if (request.Delta == 0)
            {
                return Results.BadRequest(new { error = "Delta must not be zero." });
            }

            var command = new AdjustProductQuantityCommand
            {
                ProductId = id,
                Delta = request.Delta,
                Reason = request.Reason,
                UserId = user.GetRequiredUserId()
            };

            var newBalance = await handler.HandleAsync(command, cancellationToken);
            return Results.Ok(new AdjustQuantityResponse(id, newBalance));
        }).RequireAuthorization("CanAdjustInventory")
          .AddEndpointFilter<RequireUserIdFilter>();
    }
```

The filter runs before the zero-delta check. A token without a user id therefore gets 401 even for a zero delta, which is the order the spec asks for: identity first, then input.

- [ ] **Step 6: Run the tests to see the GREEN**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~ProductsEndpointTests|FullyQualifiedName~RouteTableTests|FullyQualifiedName~InventoryMovementRepositoryTests"`
Expected: all pass, including the three new tests and the unchanged `AdjustQuantity_WithAZeroDelta_ShouldReturnBadRequest`. The route-table row `POST /products/{id:guid}/adjust-quantity CanAdjustInventory` is unchanged, because a filter is not metadata.

Then run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests`
Expected: all **245** pass.

- [ ] **Step 7: Commit**

```bash
git add src/IndyPOS.Domain src/IndyPOS.Infrastructure src/IndyPOS.Application/UseCases/StoreHub/Products src/IndyPOS.StoreHub tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/ProductsEndpointTests.cs
git commit -m "feat(storehub): stock adjustments record the user from the token"
```

---

### Task 8: Sale movements record the seller

**Files:**
- Modify: `src/IndyPOS.Application/UseCases/StoreHub/Sales/Complete/CompleteSaleCommandHandler.cs:119-129`
- Test: `tests/IndyPOS.Application.Tests/StoreHub/Sales/Commands/CompleteSaleCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `InventoryMovement.CreatedByUserId : Guid?` (Task 7); `CompleteSaleCommand.UserId : Guid`, which is already the token's user.
- Produces: nothing new.

- [ ] **Step 1: Write the failing test**

In `CompleteSaleCommandHandlerTests.cs`, add this test directly after `HandleAsync_WithANonTrackableProduct_ShouldNotCreateAnInventoryMovement`. That test is the class's negative case for movements, and this is the positive one.

```csharp
    [Theory]
    [CustomAutoData]
    public async Task HandleAsync_WithATrackableLine_StampsTheSellerOnItsMovement(
        [Frozen] Mock<ISaleRepository> saleRepository,
        [Frozen] Mock<IProductRepository> productRepository,
        [Frozen] Mock<IPaymentMethodCatalogService> catalog,
        CompleteSaleCommandHandler sut)
    {
        var seller = Guid.NewGuid();
        var productId = Guid.NewGuid();
        productRepository.Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(CreateTestProduct(productId));
        catalog.Setup(c => c.GetOfferableAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(OfferableWith("Cash"));
        IReadOnlyList<InventoryMovement>? capturedMovements = null;
        saleRepository.Setup(x => x.CompleteSaleAsync(
                It.IsAny<Invoice>(),
                It.IsAny<IReadOnlyList<InvoiceLine>>(),
                It.IsAny<IReadOnlyList<Payment>>(),
                It.IsAny<IReadOnlyList<InventoryMovement>>(),
                It.IsAny<OutboxEvent>(),
                It.IsAny<CancellationToken>()))
            .Callback((Invoice _, IReadOnlyList<InvoiceLine> _, IReadOnlyList<Payment> _,
                IReadOnlyList<InventoryMovement> movements, OutboxEvent _, CancellationToken _) =>
            {
                capturedMovements = movements;
            })
            .ReturnsAsync((Invoice inv, IReadOnlyList<InvoiceLine> _, IReadOnlyList<Payment> _,
                IReadOnlyList<InventoryMovement> _, OutboxEvent _, CancellationToken _) => inv);
        var command = new CompleteSaleCommand(
            StoreId: "STORE-001",
            UserId: seller,
            Lines: [new SaleLineRequest(ProductId: productId, Quantity: 1, UnitPrice: 100m)],
            Payments: [new SalePaymentRequest(Method: "Cash", Amount: 100m)]);

        await sut.HandleAsync(command);

        capturedMovements.Should()
                         .ContainSingle()
                         .Which.CreatedByUserId.Should()
                                               .Be(seller);
    }
```

- [ ] **Step 2: Run it to see the RED**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~CompleteSaleCommandHandlerTests.HandleAsync_WithATrackableLine_StampsTheSellerOnItsMovement"`
Expected: FAIL with "Expected … CreatedByUserId to be {seller guid}, but found <null>".

- [ ] **Step 3: Stamp the seller**

In `CompleteSaleCommandHandler.cs`, lines 119-129 read:

```csharp
            // Create inventory movement (negative for sale)
            var movement = new InventoryMovement
            {
                Id = Guid.NewGuid(),
                StoreId = command.StoreId,
                ProductId = lineRequest.ProductId,
                QuantityDelta = -lineRequest.Quantity,
                Reason = "Sale",
                ReferenceId = invoiceId,
                CreatedUtc = now
            };
```

Replace them with:

```csharp
            // Create inventory movement (negative for sale), stamped with the seller
            var movement = new InventoryMovement
            {
                Id = Guid.NewGuid(),
                StoreId = command.StoreId,
                ProductId = lineRequest.ProductId,
                QuantityDelta = -lineRequest.Quantity,
                Reason = "Sale",
                ReferenceId = invoiceId,
                CreatedByUserId = command.UserId,
                CreatedUtc = now
            };
```

`InventoryMovementSnapshot` in the `InvoiceCompleted` payload is left alone. The event already carries `UserId` for the whole sale, and the spec does not ask the cloud for a per-movement user.

- [ ] **Step 4: Run the tests to see the GREEN**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~CompleteSaleCommandHandler"`
Expected: all pass.

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~SalesEndpointTests|FullyQualifiedName~PayLaterSaleEndpointTests"`
Expected: all pass. This proves the real save writes the new column.

- [ ] **Step 5: Commit**

```bash
git add src/IndyPOS.Application/UseCases/StoreHub/Sales/Complete/CompleteSaleCommandHandler.cs tests/IndyPOS.Application.Tests/StoreHub/Sales/Commands/CompleteSaleCommandHandlerTests.cs
git commit -m "feat(storehub): sale stock movements record the seller"
```

---

### Task 9: Remove `CompleteSaleRequest.UserId`

**Files:**
- Modify: `src/IndyPOS.Application/UseCases/StoreHub/Sales/CompleteSaleRequest.cs:3-14`
- Modify: `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubSaleService.cs:335-338`
- Modify: `src/IndyPOS.StoreHub/Endpoints/Sales/SaleCompletionEndpoints.cs` (comment)
- Modify (tests, compile fixes): `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/SalesEndpointTests.cs`, `PayLaterSaleEndpointTests.cs:176-184`, `SalesHistoryEndpointsTests.cs:43-48`, `tests/IndyPOS.Application.Tests/Integration/StoreHub/StoreHubE2ETests.cs:72-73`, `StoreHubHttpClientTests.cs:128-129`
- Modify (docs): `.bruno/StoreHub/sales/complete-sale.bru`, `.bruno/StoreHub/sales/complete-sale-example.bru`, `docs/development/getting-started.md:314`, `docs/diagrams/flows.md:315`

**Interfaces:**
- Consumes: nothing new.
- Produces: `public record CompleteSaleRequest(IReadOnlyList<SaleLineRequest> Lines, IReadOnlyList<SalePaymentRequest> Payments);`.

**About RED first:** a body that sends `userId` is accepted today, and it will still be accepted after the fix. So `CompleteSale_WithAStaleUserIdInTheBody_AcceptsTheSale` cannot fail before the fix. It is a regression pin. Step 6 proves it *can* fail by switching on `JsonUnmappedMemberHandling.Disallow` for a moment. That is the change it guards against, because a till older than this release still sends `userId`.

- [ ] **Step 1: Remove the property**

Replace lines 3-14 of `CompleteSaleRequest.cs`:

```csharp
/// <summary>
/// Request to complete a sale in StoreHub.
/// </summary>
/// <param name="UserId">
/// Deprecated and ignored. StoreHub records the sale under the user in the caller's token; a body
/// value let any caller ring a sale up as someone else. Kept only so existing tills keep sending a
/// valid body.
/// </param>
public record CompleteSaleRequest(
    Guid UserId,
    IReadOnlyList<SaleLineRequest> Lines,
    IReadOnlyList<SalePaymentRequest> Payments);
```

with:

```csharp
/// <summary>
/// Request to complete a sale in StoreHub. It carries no user: StoreHub records the sale under the
/// user in the caller's token. An older till that still sends "userId" is not refused, because
/// System.Text.Json ignores a member the record does not have.
/// </summary>
public record CompleteSaleRequest(
    IReadOnlyList<SaleLineRequest> Lines,
    IReadOnlyList<SalePaymentRequest> Payments);
```

- [ ] **Step 2: Fix the client and the server comment**

In `StoreHubSaleService.cs`, lines 335-338 read:

```csharp
        return new CompleteSaleRequest(
            UserId: _loggedInUser!.UserId,
            Lines: lines,
            Payments: payments);
```

Replace them with:

```csharp
        return new CompleteSaleRequest(
            Lines: lines,
            Payments: payments);
```

`_loggedInUser` stays: the null checks at lines 277 and 297 still use it.

In `Endpoints/Sales/SaleCompletionEndpoints.cs`, replace the two-line comment that ends `request.UserId is deprecated and ignored.` with:

```csharp
            // The seller is whoever the token says. The body has no user field: one once let any caller
            // ring a sale up as someone else.
```

- [ ] **Step 3: Fix the tests that no longer compile**

Run `dotnet build IndyPOS.sln`. The errors are exactly these call sites. Change each as shown. The line numbers are those before this step, so work from the bottom of each file up.

`tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/SalesEndpointTests.cs`:
- **Lines 34-35:** the comment becomes `// The route once took UserId from the body, so any caller could ring a sale up as anyone. The token is` / `// the only authority: a till that still sends a userId is ignored.`
- **Lines 43-46:** this test's point is a body that names someone else. It keeps that body by sending JSON with a `userId` member:

  ```csharp
          var request = new
          {
              userId = someoneElse.Id,
              lines = new[] { new SaleLineRequest(product.Id, Quantity: 1, UnitPrice: 100m) },
              payments = new[] { new SalePaymentRequest("Cash", Amount: 100m) }
          };
  ```

- **Lines 62, 97, 122, 151, 180, 203, 228 and 244:** delete the `UserId: …,` line.
- **Lines 94, 119, 148, 176, 200 and 225:** delete `var user = await CreateTestUserAsync($"seller_{Guid.NewGuid():N}", "Password123!");`. Nothing reads `user` any more.
- **Line 239:** the comment becomes `// The token decides the seller; the body carries only lines and payments.`

`PayLaterSaleEndpointTests.cs`:
- Delete line 179 `var seller = await CreateTestUserAsync($"seller_{Guid.NewGuid():N}", "Password123!");`.
- Delete line 182 `UserId: seller.Id,`.

`SalesHistoryEndpointsTests.cs`: delete line 45 `var seller = await CreateTestUserAsync($"seller_{Guid.NewGuid():N}", "Password123!");`. Lines 46-47 become:

```csharp
        var response = await Client.PostAsJsonAsync("/sales/complete", new CompleteSaleRequest(
            [new SaleLineRequest(product.Id, 1, 350m)], [new SalePaymentRequest("Cash", 500m)]));
```

`StoreHubE2ETests.cs`: delete line 73 `UserId: userId,`. Keep `userId`, because `SetupLoginEndpoint(userId)` uses it.

`StoreHubHttpClientTests.cs`: delete line 129 `UserId: Guid.NewGuid(),`.

Run `dotnet build IndyPOS.sln` again and expect 0 errors.

- [ ] **Step 4: Write the regression pin**

In `SalesEndpointTests.cs`, add this directly after `CompleteSale_WithATokenWithoutAUserId_ReturnsUnauthorized`, keeping the negative and edge cases first:

```csharp
    // A till older than this release still sends userId. The request no longer has the member, and
    // System.Text.Json ignores an unknown one by default, so the sale must still go through.
    [Fact]
    public async Task CompleteSale_WithAStaleUserIdInTheBody_AcceptsTheSale()
    {
        await AuthenticateAsCashierAsync();
        var product = await CreateTestProductAsync(unitPrice: 100m, initialStock: 10);
        var body = new
        {
            userId = Guid.NewGuid(),
            lines = new[] { new { productId = product.Id, quantity = 1, unitPrice = 100m } },
            payments = new[] { new { method = "Cash", amount = 100m } }
        };

        var response = await Client.PostAsJsonAsync("/sales/complete", body);

        response.StatusCode.Should()
                           .Be(HttpStatusCode.OK);
    }
```

- [ ] **Step 5: Run it**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~SalesEndpointTests"`
Expected: all pass.

- [ ] **Step 6: Prove the pin can fail, then revert**

In `src/IndyPOS.StoreHub/Program.cs`, temporarily add this line after `builder.Services.AddOpenApi();`:

```csharp
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow);
```

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~CompleteSale_WithAStaleUserIdInTheBody_AcceptsTheSale"`
Expected: FAIL with "Expected … OK, but found HttpStatusCode.BadRequest".
Revert with `git checkout -- src/IndyPOS.StoreHub/Program.cs`, then re-run and expect PASS.

- [ ] **Step 7: Take `userId` out of the docs**

- **`.bruno/StoreHub/sales/complete-sale.bru`:**
  - delete line 23 `"userId": "{{userId}}",`;
  - delete line 41 `userId: 00000000-0000-0000-0000-000000000000`;
  - in the docs block, replace steps 3-5 (lines 55-57) with `3. Update \`productId\` in the request body` and `4. Execute this request`. The seller comes from the token.
- **`.bruno/StoreHub/sales/complete-sale-example.bru`:**
  - delete line 23 `"userId": "578d0dbb-ccd3-4dd7-9df9-0e7943f83615",`;
  - delete line 55 ``- `userId`: from login response``.
- **`docs/development/getting-started.md`:** delete line 314 `"userId": "USER_GUID_HERE",`.
- **`docs/diagrams/flows.md` line 315:** replace `{userId, lines, payments}` with `{lines, payments}` followed by 8 spaces, so the line keeps its length. The line becomes:

  ```
        │ {lines, payments}                   │                    │                 │
  ```

  Check it: `awk 'NR>=313 && NR<=317 {print length, $0}' docs/diagrams/flows.md` must print the same length for line 315 as for lines 313, 314, 316 and 317.

The paths in these files (`/sales/complete`) stay as they are. PR B renames them.

- [ ] **Step 8: Run the affected suites**

Run:
- `dotnet test tests/IndyPOS.StoreHub.IntegrationTests`, expecting all **246** to pass;
- `dotnet test tests/IndyPOS.Application.Tests`, expecting all to pass.

- [ ] **Step 9: Commit**

```bash
git add src tests .bruno docs/development/getting-started.md docs/diagrams/flows.md
git commit -m "fix(sales): drop the deprecated UserId from the sale request"
```

---

### Task 10: One date rule for `/sales` and `/reports`

**Files:**
- Create: `src/IndyPOS.Application/Common/Validation/DateRangeRule.cs`
- Modify: `src/IndyPOS.Application/UseCases/StoreHub/Sales/History/SalesQueryRules.cs:1-45`
- Modify: `src/IndyPOS.StoreHub/Endpoints/Reports/ReportsEndpoints.cs`
- Test: `tests/IndyPOS.Application.Tests/Common/Validation/DateRangeRuleTests.cs` (new)
- Test: `tests/IndyPOS.Application.Tests/UseCases/StoreHub/Sales/SalesQueryRulesTests.cs:58`
- Test: `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/ReportsEndpointTests.cs`

**Interfaces:**
- Consumes: `SalesQueryValidationException(string)` (unchanged).
- Produces, in `IndyPOS.Application.Common.Validation`:

  ```csharp
  public static class DateRangeRule
  {
      public const string DateFormat;                    // "yyyy-MM-dd"
      public static readonly DateOnly EarliestDate;      // 2000-01-01
      public static readonly DateOnly LatestDate;        // 2099-12-31
      public const string ToBeforeFromMessage;
      public static readonly string OutOfBoundsMessage;
      public static string? FindViolation(DateOnly from, DateOnly to);  // Thai reason, or null when valid
  }
  ```

**The four dated report routes** are the routes in `ReportsEndpoints` that bind `DateOnly fromDate, DateOnly toDate`:
- `/reports/sales-summary`;
- `/reports/product-sales`;
- `/reports/legacy/sales-summary`;
- `/reports/legacy/payments-summary`.

`/reports/pay-later` takes no dates. Each dated handler calls `ReportDateRange.ToUtcRange` first, at line 37 or 42 of its `Infrastructure/QueryHandlers/Reports/*.cs`. That method throws `ArgumentException` on a swapped range and overflows `DateOnly.AddDays` on `9999-12-31`.

- [ ] **Step 1: Write the failing HTTP tests**

In `ReportsEndpointTests.cs`, add this after line 4 `using IndyPOS.Application.UseCases.StoreHub.Reports;`:

```csharp
using IndyPOS.Application.Common.Validation;
```

Add this after line 15, the constructor:

```csharp
    private const string AValidDay = "2026-10-01";
    private const string TheDayBefore = "2026-09-30";
    private const string JustBeforeTheEarliestDate = "1999-12-31";

    // DateOnly.AddDays(1) overflows here inside ReportDateRange.ToUtcRange: a 500 before this fix.
    private const string BeyondTheLatestDate = "9999-12-31";

    private const string TheEarliestDate = "2000-01-01";
    private const string TheLatestDate = "2099-12-31";

    public static TheoryData<string> DatedReportRoutes => new()
    {
        "/reports/sales-summary",
        "/reports/product-sales",
        "/reports/legacy/sales-summary",
        "/reports/legacy/payments-summary"
    };

    private sealed record ErrorResponse(string Error);
```

Add these tests after `GetSalesSummary_AsCashier_ReturnsForbidden`. The negative cases come first.

```csharp
    // A swapped range threw ArgumentException in ReportDateRange.ToUtcRange: a 500.
    [Theory]
    [MemberData(nameof(DatedReportRoutes))]
    public async Task DatedReport_WithToBeforeFrom_ReturnsBadRequest(string route)
    {
        await AuthenticateAsManagerAsync();

        var response = await Client.GetAsync($"{route}?fromDate={AValidDay}&toDate={TheDayBefore}");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [MemberData(nameof(DatedReportRoutes))]
    public async Task DatedReport_WithToBeforeFrom_ExplainsInThai(string route)
    {
        await AuthenticateAsManagerAsync();

        var response = await Client.GetAsync($"{route}?fromDate={AValidDay}&toDate={TheDayBefore}");

        (await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions))!.Error.Should()
                                                                               .Be(DateRangeRule.ToBeforeFromMessage);
    }

    [Theory]
    [MemberData(nameof(DatedReportRoutes))]
    public async Task DatedReport_WithADateBeyondTheLatest_ReturnsBadRequest(string route)
    {
        await AuthenticateAsManagerAsync();

        var response = await Client.GetAsync($"{route}?fromDate={AValidDay}&toDate={BeyondTheLatestDate}");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    // Does not crash today; it must be refused by the same rule rather than slip through as a 200.
    [Theory]
    [MemberData(nameof(DatedReportRoutes))]
    public async Task DatedReport_WithADateBeforeTheEarliest_ReturnsBadRequest(string route)
    {
        await AuthenticateAsManagerAsync();

        var response = await Client.GetAsync($"{route}?fromDate={JustBeforeTheEarliestDate}&toDate={AValidDay}");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [MemberData(nameof(DatedReportRoutes))]
    public async Task DatedReport_WithTheSupportedBounds_ReturnsOk(string route)
    {
        await AuthenticateAsManagerAsync();

        var response = await Client.GetAsync($"{route}?fromDate={TheEarliestDate}&toDate={TheLatestDate}");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.OK);
    }
```

- [ ] **Step 2: Run them to see the RED**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~ReportsEndpointTests.DatedReport"`
Expected: the build fails with `CS0103: The name 'DateRangeRule' does not exist`. Comment out only `DatedReport_WithToBeforeFrom_ExplainsInThai` and run again. Now:
- **`…WithToBeforeFrom_ReturnsBadRequest` ×4 FAIL.** TestServer rethrows the unhandled server exception, so each fails with `System.ArgumentException: Report end date must be on or after start date.` (or, if the host turns it into a response, "found HttpStatusCode.InternalServerError").
- **`…WithADateBeyondTheLatest_ReturnsBadRequest` ×4 FAIL** with `System.ArgumentOutOfRangeException` from `DateOnly.AddDays` (or a 500).
- **`…WithADateBeforeTheEarliest_ReturnsBadRequest` ×4 FAIL:** "Expected … BadRequest, but found HttpStatusCode.OK".
- **`…WithTheSupportedBounds_ReturnsOk` ×4 PASS.** This is the edge that must stay green.

Uncomment the Thai test once you have read the failures.

- [ ] **Step 3: Write the rule's unit tests**

Create `tests/IndyPOS.Application.Tests/Common/Validation/DateRangeRuleTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Common.Validation;
using Xunit;

namespace IndyPOS.Application.Tests.Common.Validation;

public class DateRangeRuleTests
{
    private static readonly DateOnly Day = new(2026, 10, 2);
    private static readonly DateOnly DayBeforeTheEarliest = DateRangeRule.EarliestDate.AddDays(-1);
    private static readonly DateOnly DayAfterTheLatest = DateRangeRule.LatestDate.AddDays(1);

    [Fact]
    public void FindViolation_WithToBeforeFrom_ReturnsTheSwappedRangeMessage()
    {
        DateRangeRule.FindViolation(Day, Day.AddDays(-1)).Should()
                                                         .Be(DateRangeRule.ToBeforeFromMessage);
    }

    [Fact]
    public void FindViolation_WithAFromBeforeTheEarliestDate_ReturnsTheBoundsMessage()
    {
        DateRangeRule.FindViolation(DayBeforeTheEarliest, Day).Should()
                                                              .Be(DateRangeRule.OutOfBoundsMessage);
    }

    [Fact]
    public void FindViolation_WithAToAfterTheLatestDate_ReturnsTheBoundsMessage()
    {
        DateRangeRule.FindViolation(Day, DayAfterTheLatest).Should()
                                                           .Be(DateRangeRule.OutOfBoundsMessage);
    }

    // ReportDateRange.ToUtcRange adds a day: DateOnly.MaxValue overflows into a 500.
    [Fact]
    public void FindViolation_WithTheLastRepresentableDate_ReturnsTheBoundsMessage()
    {
        DateRangeRule.FindViolation(Day, DateOnly.MaxValue).Should()
                                                           .Be(DateRangeRule.OutOfBoundsMessage);
    }

    [Fact]
    public void FindViolation_WithTheSupportedBounds_ReturnsNull()
    {
        DateRangeRule.FindViolation(DateRangeRule.EarliestDate, DateRangeRule.LatestDate).Should()
                                                                                         .BeNull();
    }

    [Fact]
    public void FindViolation_WithTheSameDay_ReturnsNull()
    {
        DateRangeRule.FindViolation(Day, Day).Should()
                                             .BeNull();
    }
}
```

- [ ] **Step 4: Create the rule**

Create `src/IndyPOS.Application/Common/Validation/DateRangeRule.cs`. The two Thai messages are copied character for character from `SalesQueryRules.EnsureValidRange` (lines 39-44), so `/sales` keeps answering exactly as it does today.

```csharp
using System.Globalization;

namespace IndyPOS.Application.Common.Validation;

/// <summary>
/// The one date-range rule for every query that takes a from/to pair: /sales and the dated /reports
/// routes, so the two cannot drift. A refusal is a Thai sentence for the cashier, never a 500 from
/// deeper down.
/// </summary>
public static class DateRangeRule
{
    public const string DateFormat = "yyyy-MM-dd";

    /// <summary>
    /// The dates a query may ask for. Well before any store opened and well after any till will run,
    /// yet far enough from DateOnly's own limits that ReportDateRange.ToUtcRange -- which adds a day
    /// and shifts by the store's offset -- can never overflow into a 500.
    /// </summary>
    public static readonly DateOnly EarliestDate = new(2000, 1, 1);
    public static readonly DateOnly LatestDate = new(2099, 12, 31);

    public const string ToBeforeFromMessage = "วันที่เริ่มต้นต้องไม่อยู่หลังวันที่สิ้นสุด";

    public static readonly string OutOfBoundsMessage =
        $"วันที่ต้องอยู่ระหว่าง {EarliestDate.ToString(DateFormat, CultureInfo.InvariantCulture)} " +
        $"ถึง {LatestDate.ToString(DateFormat, CultureInfo.InvariantCulture)}";

    /// <summary>Why the range is refused, in Thai, or null when it is valid.</summary>
    public static string? FindViolation(DateOnly from, DateOnly to)
    {
        if (from < EarliestDate || to > LatestDate)
            return OutOfBoundsMessage;

        return to < from ? ToBeforeFromMessage : null;
    }
}
```

Every invalid pair is caught. A `from` past `LatestDate`, or a `to` before `EarliestDate`, always fails one of the two checks: either it is out of bounds or the range is swapped. So nothing reaches `ToUtcRange` unless `EarliestDate ≤ from ≤ to ≤ LatestDate`.

- [ ] **Step 5: Make `SalesQueryRules` delegate**

In `SalesQueryRules.cs`:
1. Add `using IndyPOS.Application.Common.Validation;` after line 2 `using IndyPOS.Application.Common.Exceptions;`.
2. Line 12 `public const string DateFormat = "yyyy-MM-dd";` becomes `public const string DateFormat = DateRangeRule.DateFormat;`.
3. Delete lines 14-20: the `/// <summary> The dates a list may ask for…` comment and the `EarliestDate` and `LatestDate` fields. Their only reader was `EnsureValidRange` and one test (Step 6).
4. Replace `EnsureValidRange` (lines 36-45) with:

```csharp
    public static void EnsureValidRange(DateOnly from, DateOnly to)
    {
        if (DateRangeRule.FindViolation(from, to) is { } reason)
            throw new SalesQueryValidationException(reason);
    }
```

- [ ] **Step 6: Point the one stale test line at the rule**

In `SalesQueryRulesTests.cs`, line 58:

```csharp
        var act = () => SalesQueryRules.EnsureValidRange(SalesQueryRules.EarliestDate, SalesQueryRules.LatestDate);
```

becomes:

```csharp
        var act = () => SalesQueryRules.EnsureValidRange(DateRangeRule.EarliestDate, DateRangeRule.LatestDate);
```

Then add `using IndyPOS.Application.Common.Validation;` after line 2 `using IndyPOS.Application.Common.Exceptions;`.

- [ ] **Step 7: Use the rule in the four dated report routes**

In `ReportsEndpoints.cs`, add `using IndyPOS.Application.Common.Validation;` after `using IndyPOS.Application.Common.Models;`.

Add this helper as the last member of the class:

```csharp
    // Checked before the handler runs: ReportDateRange.ToUtcRange throws on a swapped range and
    // overflows on 9999-12-31, and either escaped as a 500.
    private static IResult? RejectInvalidRange(DateOnly fromDate, DateOnly toDate) =>
        DateRangeRule.FindViolation(fromDate, toDate) is { } error
            ? Results.BadRequest(new { error })
            : null;
```

In each of `MapSalesSummary`, `MapProductSales`, `MapLegacySalesSummary` and `MapLegacyPaymentsSummary`, make this the first statement of the lambda body, right after its opening `{`:

```csharp
            if (RejectInvalidRange(fromDate, toDate) is { } rejection)
            {
                return rejection;
            }

```

For example, `MapLegacySalesSummary` becomes:

```csharp
    // Legacy sales summary (returns SalesSummary model)
    private static void MapLegacySalesSummary(IEndpointRouteBuilder app)
    {
        app.MapGet("/reports/legacy/sales-summary", async (
            IQueryHandler<GetLegacySalesSummaryQuery, SalesSummary> handler,
            DateOnly fromDate,
            DateOnly toDate,
            CancellationToken cancellationToken) =>
        {
            if (RejectInvalidRange(fromDate, toDate) is { } rejection)
            {
                return rejection;
            }

            var query = new GetLegacySalesSummaryQuery(fromDate, toDate);
            var result = await handler.HandleAsync(query, cancellationToken);
            return Results.Ok(result);
        }).RequireAuthorization("CanViewReports");
    }
```

`MapPayLaterReport` is not changed. Each lambda now returns `IResult` from both branches. `Results.Ok(...)` already returns `IResult`, so the inferred return type does not change.

A malformed date such as `fromDate=abc`, or a missing one, still gets the framework's bare 400 from `DateOnly` binding. Changing that would change the routes' binding, which §3.1 freezes.

- [ ] **Step 8: Run the tests to see the GREEN**

Run:
- `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~DateRangeRuleTests|FullyQualifiedName~SalesQueryRulesTests|FullyQualifiedName~ListSalesQueryHandlerTests"`, expecting all to pass;
- `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~ReportsEndpointTests|FullyQualifiedName~SalesHistoryEndpointsTests|FullyQualifiedName~RouteTableTests"`, expecting all to pass. That includes the 20 new report cases and the unchanged `/sales` range tests;
- `dotnet test tests/IndyPOS.StoreHub.IntegrationTests`, expecting all **266** to pass.

- [ ] **Step 9: Commit**

```bash
git add src/IndyPOS.Application src/IndyPOS.StoreHub tests/IndyPOS.Application.Tests tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/ReportsEndpointTests.cs
git commit -m "fix(reports): a bad report date range is a Thai 400, never a 500"
```

---

### Task 11: The cloud's `/sync/status` needs a store token and counts only that store

**Files:**
- Modify: `src/IndyPOS.Application/Abstractions/Cloud/Repositories/ISyncedEventRepository.cs:46-65` (`SyncedEventEntity`)
- Modify: `src/IndyPOS.Application/UseCases/Cloud/Sync/IngestEvents/IngestEventsCommandHandler.cs:49-57`
- Modify: `src/IndyPOS.CloudApi/Infrastructure/CloudDbContext.cs:41-49` (the `SyncedEventEntity` block)
- Create: `src/IndyPOS.CloudApi/Infrastructure/Migrations/<ts>_AddSyncedEventSourceStore.cs` and its `.Designer.cs` (generated); the snapshot changes too
- Create: `src/IndyPOS.CloudApi/Endpoints/SyncStatusEndpoints.cs`
- Create: `src/IndyPOS.CloudApi/Infrastructure/StoreSyncStatusQuery.cs`
- Modify: `src/IndyPOS.CloudApi/Program.cs:14` (using) and `:196-214`
- Modify: `tests/IndyPOS.Application.Tests/UseCases/Cloud/Sync/IngestEventsCommandHandlerTests.cs`
- Modify: `tests/IndyPOS.CloudApi.IntegrationTests/IndyPOS.CloudApi.IntegrationTests.csproj`
- Create: `tests/IndyPOS.CloudApi.IntegrationTests/SyncStatusTestHost.cs`
- Create: `tests/IndyPOS.CloudApi.IntegrationTests/SyncStatusEndpointTests.cs`

**Interfaces:**
- Consumes:
  - `IngestEventsCommand.AuthenticatedStoreId`, the token's `store_id`, which `Program.cs:156` passes in;
  - `CloudPostgresFixture` (`CreateDatabaseAsync()`, `CreateContext(string)` and `Configure(DbContextOptionsBuilder, string)`, all public);
  - `CloudDbContext.SyncedEvents`, `ProcessedEvents` and `Invoices`.

  `IndyPOS.CloudApi.csproj` already has `InternalsVisibleTo` for `IndyPOS.CloudApi.IntegrationTests`.
- Produces:
  - `SyncedEventEntity.SourceStoreId : string?`, mapped to `"SyncedEvents"."SourceStoreId" character varying(50) NULL`, with a comment and an index;
  - `internal static class SyncStatusEndpoints { public static IEndpointRouteBuilder MapSyncStatus(this IEndpointRouteBuilder app); }`;
  - `internal static class StoreSyncStatusQuery { public static Task<StoreSyncStatus> CountAsync(CloudDbContext db, string storeId, CancellationToken cancellationToken); }`;
  - `internal sealed record StoreSyncStatus(int TotalEvents, int UnprocessedEvents, int ProcessedEvents, int TotalInvoices);`.

**How a store token works today:** the CloudApi's default scheme is OpenIddict validation (`Program.cs:60`). `/sync/events` uses `[Authorize]` plus `.RequireAuthorization()`, so it uses the default policy under that scheme. It then reads `user.FindFirst("store_id")`, which `TokenController` puts on every client-credentials token (`TokenController.cs:99`). `IngestEventsCommandHandler.EnsureAllForAuthenticatedStore` (line 24) refuses the whole batch unless every payload's `StoreId` equals that claim. `/sync/status` will use the same `.RequireAuthorization()` and the same claim.

**Why a new column (Pond, 2026-10-02):** the inbox's `StoreId` is an `int`, and `HttpCloudSyncClient.ParseStoreId` sends `0` for every real (string) store id. That column cannot say whose an event is. Ingest now stamps the token's `store_id` into a new nullable `SourceStoreId`, and `/sync/status` counts by it with plain LINQ.

Rows ingested before this release keep `NULL` and are **not counted** for any store. The `int StoreId` column and `ParseStoreId` stay exactly as they are. That bug is separate (spec §7), and the new column makes it irrelevant to this count.

`ProcessedEvents.StoreId` and `Invoices.StoreId` are already strings, and are filtered directly.

**Why a test host, not `WebApplicationFactory<Program>`:** the CloudApi has no HTTP test harness. Booting the real `Program` would need several things:
- an OpenIddict signing certificate;
- the Aspire connection string;
- a registered OAuth client;
- an HTTPS token exchange;
- a running `EventProcessor`.

`SyncStatusTestHost` maps the **real** `MapSyncStatus` on a TestServer instead. A stub authentication scheme turns `Bearer <storeId>` into a `store_id` claim. This proves the route's own behaviour: it needs an authenticated caller, and it scopes by `store_id`. It does not re-prove OpenIddict, which `/sync/events` already relies on in exactly the same way.

- [ ] **Step 1: Move the route into its own file, unchanged**

Create `src/IndyPOS.CloudApi/Endpoints/SyncStatusEndpoints.cs` with today's behaviour, still open and still counting every store. That way the status tests in Step 7 have a real RED:

```csharp
using IndyPOS.CloudApi.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.CloudApi.Endpoints;

internal static class SyncStatusEndpoints
{
    public static IEndpointRouteBuilder MapSyncStatus(this IEndpointRouteBuilder app)
    {
        // Sync status endpoint
        app.MapGet("/sync/status", async (CloudDbContext db, CancellationToken cancellationToken) =>
        {
            var totalEvents = await db.SyncedEvents.CountAsync(cancellationToken);
            var unprocessedEvents = await db.SyncedEvents.CountAsync(e => e.ProcessedAtUtc == null, cancellationToken);
            var processedEvents = await db.ProcessedEvents.CountAsync(cancellationToken);
            var totalInvoices = await db.Invoices.CountAsync(cancellationToken);

            return Results.Ok(new
            {
                status = "running",
                storage = "postgresql",
                totalEvents,
                unprocessedEvents,
                processedEvents,
                totalInvoices,
                timestamp = DateTime.UtcNow
            });
        });

        return app;
    }
}
```

In `src/IndyPOS.CloudApi/Program.cs`, delete lines 196-214, from `// Sync status endpoint` through that route's closing `});`. Put this in their place:

```csharp
// Sync status endpoint
app.MapSyncStatus();
```

Then add `using IndyPOS.CloudApi.Endpoints;` after line 14 `using IndyPOS.Application.UseCases.Cloud.Sync.BulkMigration;`.

Run: `dotnet build src/IndyPOS.CloudApi` and expect 0 errors.
Run: `dotnet test tests/IndyPOS.CloudApi.IntegrationTests` and `dotnet test tests/IndyPOS.CloudApi.Tests`, and expect all of them to pass (44 and 6).

```bash
git add src/IndyPOS.CloudApi
git commit -m "refactor(cloud): map /sync/status from its own file"
```

- [ ] **Step 2: Write the failing ingest test**

In `IngestEventsCommandHandlerTests.cs`, add this test directly before `HandleAsync_NewEvent_ShouldAcceptAndStore` (line 83). The class's existing negative cases come first; this is the positive one for the stamp.

```csharp
    // /sync/status counts a store's inbox by this stamp. The envelope's int StoreId cannot be used:
    // HttpCloudSyncClient.ParseStoreId sends 0 for every real store id.
    [Fact]
    public async Task HandleAsync_WithAnEventForItsOwnStore_StampsTheTokensStore()
    {
        var eventId = Guid.NewGuid();
        var command = new IngestEventsCommand(
            [new SyncEventRequest(eventId, StoreId: 0, EventType: "InvoiceCompleted", Payload: OwnPayload, CreatedAtUtc: DateTime.UtcNow)],
            OwnStoreId);

        await _handler.HandleAsync(command);

        Assert.Equal(OwnStoreId, _repository.Events[eventId].SourceStoreId);
    }
```

The class asserts with xUnit `Assert` throughout and has no FluentAssertions using, so this test follows the file.

- [ ] **Step 3: Run it to see the RED**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~IngestEventsCommandHandlerTests"`
Expected: the build fails with `CS1061: 'SyncedEventEntity' does not contain a definition for 'SourceStoreId'`.

Add the property (Step 4, first part only) and run again. Expected: FAIL with `Assert.Equal() Failure: Expected: "STORE-001" Actual: null`.

- [ ] **Step 4: Add the column and stamp it at ingest**

In `ISyncedEventRepository.cs`, inside `SyncedEventEntity`, insert this after line 50 `public int StoreId { get; set; }`:

```csharp

    /// <summary>
    /// The store the ingest token authenticated: its store_id claim, which ingest has already matched
    /// against the payload. Null for an event ingested before this column existed; such an event is
    /// counted for no store. Not the int StoreId above, which carries 0 for every real store.
    /// </summary>
    public string? SourceStoreId { get; set; }
```

In `IngestEventsCommandHandler.cs`, the entity initializer at lines 49-57 reads:

```csharp
                var entity = new SyncedEventEntity
                {
                    EventId = eventRequest.EventId,
                    StoreId = eventRequest.StoreId,
                    EventType = eventRequest.EventType,
                    Payload = eventRequest.Payload,
                    CreatedAtUtc = eventRequest.CreatedAtUtc,
                    ReceivedAtUtc = receivedAt
                };
```

Replace it with:

```csharp
                // SourceStoreId is safe to take from the token: EnsureAllForAuthenticatedStore above has
                // already refused the batch unless every payload names this store.
                var entity = new SyncedEventEntity
                {
                    EventId = eventRequest.EventId,
                    StoreId = eventRequest.StoreId,
                    SourceStoreId = command.AuthenticatedStoreId,
                    EventType = eventRequest.EventType,
                    Payload = eventRequest.Payload,
                    CreatedAtUtc = eventRequest.CreatedAtUtc,
                    ReceivedAtUtc = receivedAt
                };
```

In `CloudDbContext.cs`, the `SyncedEventEntity` block (lines 41-49) ends with `entity.Property(e => e.EventType).HasMaxLength(100);`. Insert this after that line, inside the block:

```csharp
            entity.Property(e => e.SourceStoreId)
                  .HasMaxLength(50)
                  .HasComment("The store that sent this event: the store_id of the ingest token, already matched against the payload's StoreId. NULL for events ingested before this column existed (not counted for any store). Use this, not StoreId, which is 0 for every real store.");
            entity.HasIndex(e => e.SourceStoreId);
```

- [ ] **Step 5: Generate the CloudApi migration**

Run:
```bash
dotnet ef migrations add AddSyncedEventSourceStore --project src/IndyPOS.CloudApi --output-dir Infrastructure/Migrations
dotnet ef migrations has-pending-model-changes --project src/IndyPOS.CloudApi
```

Expected:
- The generated `Up` holds exactly two calls:
  - `migrationBuilder.AddColumn<string>(name: "SourceStoreId", table: "SyncedEvents", type: "character varying(50)", maxLength: 50, nullable: true, comment: "The store that sent this event: …")`;
  - `migrationBuilder.CreateIndex(name: "IX_SyncedEvents_SourceStoreId", table: "SyncedEvents", column: "SourceStoreId")`.
- There is no `AlterColumn`, `DropX` or `RenameX`, and nothing touches the `int StoreId` column. If anything else appears, stop: the migration must be additive only.
- The second command prints `No changes have been made to the model since the last migration.`

`CloudDbContextDesignTimeFactory` supplies the model, so no connection is needed. Leave the Designer and the snapshot as generated.

- [ ] **Step 6: Run the ingest tests to see the GREEN, and commit**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~IngestEventsCommandHandlerTests"`
Expected: all pass, the new test among them.

Run: `dotnet test tests/IndyPOS.CloudApi.IntegrationTests`
Expected: all 44 pass. `CloudPostgresFixture` migrates every test database, so this also applies the new migration on real Postgres.

```bash
git add src/IndyPOS.Application/Abstractions/Cloud/Repositories/ISyncedEventRepository.cs src/IndyPOS.Application/UseCases/Cloud/Sync/IngestEvents/IngestEventsCommandHandler.cs src/IndyPOS.CloudApi/Infrastructure tests/IndyPOS.Application.Tests/UseCases/Cloud/Sync/IngestEventsCommandHandlerTests.cs
git commit -m "feat(cloud): stamp each ingested event with the token's store"
```

- [ ] **Step 7: Give the integration tests a TestServer**

In `tests/IndyPOS.CloudApi.IntegrationTests/IndyPOS.CloudApi.IntegrationTests.csproj`, add this after `<PackageReference Include="FluentAssertions" Version="8.8.0" />`:

```xml
    <PackageReference Include="Microsoft.AspNetCore.TestHost" Version="10.0.5" />
```

Add this new item group before `</Project>`:

```xml
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
```

`10.0.5` is the version that `Microsoft.AspNetCore.Mvc.Testing` 10.0.5 already pulls into `IndyPOS.StoreHub.IntegrationTests`, so it restores from the same cache.

- [ ] **Step 8: Create the test host**

Create `tests/IndyPOS.CloudApi.IntegrationTests/SyncStatusTestHost.cs`:

```csharp
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using IndyPOS.CloudApi.Endpoints;
using IndyPOS.CloudApi.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IndyPOS.CloudApi.IntegrationTests;

/// <summary>
/// The real /sync/status route on a TestServer. A stub scheme stands in for OpenIddict: it turns
/// "Bearer {storeId}" into the store_id claim a store token carries. Booting the real Program would need
/// an OpenIddict certificate, a registered client and an HTTPS token exchange to prove nothing more
/// about this route.
/// </summary>
internal sealed class SyncStatusTestHost : IAsyncDisposable
{
    /// <summary>A bearer value the stub authenticates without a store_id claim.</summary>
    public const string WithoutAStore = "no-store-claim";

    private readonly WebApplication _app;

    private SyncStatusTestHost(WebApplication app)
    {
        _app = app;
        Client = app.GetTestClient();
    }

    public HttpClient Client { get; }

    public static async Task<SyncStatusTestHost> StartAsync(string connectionString)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddDbContext<CloudDbContext>(options => CloudPostgresFixture.Configure(options, connectionString));
        builder.Services.AddAuthentication(StoreTokenStubHandler.SchemeName)
                        .AddScheme<AuthenticationSchemeOptions, StoreTokenStubHandler>(StoreTokenStubHandler.SchemeName, configureOptions: null);
        builder.Services.AddAuthorization();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapSyncStatus();
        await app.StartAsync();

        return new SyncStatusTestHost(app);
    }

    public async Task<HttpResponseMessage> GetAsStoreAsync(string storeId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/sync/status");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", storeId);

        return await Client.SendAsync(request);
    }

    public async Task<SyncStatusBody> GetStatusAsync(string storeId)
    {
        var response = await GetAsStoreAsync(storeId);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<SyncStatusBody>())!;
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.DisposeAsync();
    }
}

internal sealed record SyncStatusBody(int TotalEvents, int UnprocessedEvents, int ProcessedEvents, int TotalInvoices);

internal sealed class StoreTokenStubHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "StoreTokenStub";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var header) || header.Parameter is null)
            return Task.FromResult(AuthenticateResult.NoResult());

        Claim[] claims = header.Parameter == SyncStatusTestHost.WithoutAStore
            ? []
            : [new Claim("store_id", header.Parameter)];
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
```

- [ ] **Step 9: Write the failing status tests**

Create `tests/IndyPOS.CloudApi.IntegrationTests/SyncStatusEndpointTests.cs`:

```csharp
using System.Net;
using System.Text.Json;
using FluentAssertions;
using IndyPOS.Application.Abstractions.Cloud.Repositories;
using IndyPOS.CloudApi.Domain;
using Xunit;

namespace IndyPOS.CloudApi.IntegrationTests;

/// <summary>
/// /sync/status was open to anyone and counted every store. It now needs the store token that
/// /sync/events takes, and counts only that token's store, using the SourceStoreId stamped at ingest.
/// Each test gets a fresh database.
/// </summary>
public class SyncStatusEndpointTests(CloudPostgresFixture postgres) : IClassFixture<CloudPostgresFixture>
{
    private const string ThisStore = "store-a";
    private const string OtherStore = "store-b";

    // What HttpCloudSyncClient.ParseStoreId sends for every real (string) store id.
    private const int EnvelopeStoreIdOfEveryStore = 0;

    // An event ingested before SourceStoreId existed.
    private const string? NoSourceStore = null;

    [Fact]
    public async Task SyncStatus_WithoutAToken_ReturnsUnauthorized()
    {
        await using var host = await SyncStatusTestHost.StartAsync(await postgres.CreateDatabaseAsync());

        var response = await host.Client.GetAsync("/sync/status");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SyncStatus_WithATokenWithoutAStore_ReturnsForbidden()
    {
        await using var host = await SyncStatusTestHost.StartAsync(await postgres.CreateDatabaseAsync());

        var response = await host.GetAsStoreAsync(SyncStatusTestHost.WithoutAStore);

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SyncStatus_WithAnotherStoresInboxRow_DoesNotCountIt()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        await AddInboxRowAsync(connectionString, OtherStore, payloadStoreId: OtherStore);
        await using var host = await SyncStatusTestHost.StartAsync(connectionString);

        var status = await host.GetStatusAsync(ThisStore);

        status.TotalEvents.Should()
                          .Be(0);
    }

    // A row ingested before this release has no SourceStoreId. Even when its payload names this store,
    // it is counted for no store: the stamp, not the payload, decides.
    [Fact]
    public async Task SyncStatus_WithALegacyInboxRowWithoutASourceStore_DoesNotCountIt()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        await AddInboxRowAsync(connectionString, NoSourceStore, payloadStoreId: ThisStore);
        await using var host = await SyncStatusTestHost.StartAsync(connectionString);

        var status = await host.GetStatusAsync(ThisStore);

        status.TotalEvents.Should()
                          .Be(0);
    }

    [Fact]
    public async Task SyncStatus_WithAStoreToken_CountsOnlyThatStore()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        await SeedInboxAsync(connectionString, ThisStore, events: 2, processed: 1);
        await SeedInvoicesAsync(connectionString, ThisStore, count: 1);
        await SeedInboxAsync(connectionString, OtherStore, events: 3, processed: 1);
        await SeedInvoicesAsync(connectionString, OtherStore, count: 2);
        await using var host = await SyncStatusTestHost.StartAsync(connectionString);

        var status = await host.GetStatusAsync(ThisStore);

        status.Should()
              .BeEquivalentTo(new SyncStatusBody(TotalEvents: 2, UnprocessedEvents: 1, ProcessedEvents: 1, TotalInvoices: 1));
    }

    // The first `processed` events are marked processed in the inbox and recorded in ProcessedEvents.
    private static async Task SeedInboxAsync(string connectionString, string storeId, int events, int processed)
    {
        await using var db = CloudPostgresFixture.CreateContext(connectionString);

        for (var i = 0; i < events; i++)
        {
            var isProcessed = i < processed;
            var row = InboxRow(storeId, storeId, isProcessed ? DateTime.UtcNow : null);
            db.SyncedEvents.Add(row);

            if (isProcessed)
                db.ProcessedEvents.Add(new ProcessedEvent { EventId = row.EventId, EventType = row.EventType, StoreId = storeId, ProcessedAtUtc = DateTime.UtcNow });
        }

        await db.SaveChangesAsync();
    }

    private static async Task AddInboxRowAsync(string connectionString, string? sourceStoreId, string payloadStoreId)
    {
        await using var db = CloudPostgresFixture.CreateContext(connectionString);
        db.SyncedEvents.Add(InboxRow(sourceStoreId, payloadStoreId, processedAtUtc: null));
        await db.SaveChangesAsync();
    }

    private static async Task SeedInvoicesAsync(string connectionString, string storeId, int count)
    {
        await using var db = CloudPostgresFixture.CreateContext(connectionString);

        for (var i = 0; i < count; i++)
            db.Invoices.Add(new CloudInvoice { Id = Guid.NewGuid(), StoreId = storeId, UserId = Guid.NewGuid(), TotalAmount = 100m, CreatedAtUtc = DateTime.UtcNow, SyncedAtUtc = DateTime.UtcNow });

        await db.SaveChangesAsync();
    }

    private static SyncedEventEntity InboxRow(string? sourceStoreId, string payloadStoreId, DateTime? processedAtUtc) => new()
    {
        EventId = Guid.NewGuid(),
        StoreId = EnvelopeStoreIdOfEveryStore,
        SourceStoreId = sourceStoreId,
        EventType = "InvoiceCompleted",
        Payload = JsonSerializer.Serialize(new { StoreId = payloadStoreId }),
        CreatedAtUtc = DateTime.UtcNow,
        ReceivedAtUtc = DateTime.UtcNow,
        ProcessedAtUtc = processedAtUtc
    };
}
```

- [ ] **Step 10: Run them to see the RED**

Run: `dotnet test tests/IndyPOS.CloudApi.IntegrationTests --filter "FullyQualifiedName~SyncStatusEndpointTests"`
Expected: all five FAIL.
- `…WithoutAToken_ReturnsUnauthorized`: "Expected … Unauthorized, but found HttpStatusCode.OK".
- `…WithATokenWithoutAStore_ReturnsForbidden`: "… Forbidden, but found HttpStatusCode.OK".
- `…WithAnotherStoresInboxRow_DoesNotCountIt`: "Expected status.TotalEvents to be 0, but found 1".
- `…WithALegacyInboxRowWithoutASourceStore_DoesNotCountIt`: "Expected status.TotalEvents to be 0, but found 1".
- `…WithAStoreToken_CountsOnlyThatStore`: the global counts, not this store's. `TotalEvents` 5, `UnprocessedEvents` 3, `ProcessedEvents` 2 and `TotalInvoices` 3, where 2, 1, 1 and 1 were expected.

- [ ] **Step 11: Count one store**

Create `src/IndyPOS.CloudApi/Infrastructure/StoreSyncStatusQuery.cs`:

```csharp
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.CloudApi.Infrastructure;

/// <summary>
/// One store's sync counts, for GET /sync/status. The inbox is counted by SourceStoreId, stamped at
/// ingest from the token; events ingested before that column existed are NULL there and are counted
/// for no store.
/// </summary>
internal static class StoreSyncStatusQuery
{
    public static async Task<StoreSyncStatus> CountAsync(CloudDbContext db, string storeId, CancellationToken cancellationToken)
    {
        var inbox = db.SyncedEvents.Where(e => e.SourceStoreId == storeId);

        return new StoreSyncStatus(
            TotalEvents: await inbox.CountAsync(cancellationToken),
            UnprocessedEvents: await inbox.CountAsync(e => e.ProcessedAtUtc == null, cancellationToken),
            ProcessedEvents: await db.ProcessedEvents.CountAsync(e => e.StoreId == storeId, cancellationToken),
            TotalInvoices: await db.Invoices.CountAsync(i => i.StoreId == storeId, cancellationToken));
    }
}

internal sealed record StoreSyncStatus(int TotalEvents, int UnprocessedEvents, int ProcessedEvents, int TotalInvoices);
```

Replace the whole of `src/IndyPOS.CloudApi/Endpoints/SyncStatusEndpoints.cs`:

```csharp
using System.Security.Claims;
using IndyPOS.CloudApi.Infrastructure;

namespace IndyPOS.CloudApi.Endpoints;

/// <summary>
/// The cloud's sync health for one store. It takes the same store token as /sync/events and counts
/// only that token's store. It used to be open and to count every store, which told any caller how
/// much every other store trades.
/// </summary>
internal static class SyncStatusEndpoints
{
    public static IEndpointRouteBuilder MapSyncStatus(this IEndpointRouteBuilder app)
    {
        app.MapGet("/sync/status", async (CloudDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        {
            // Every store token carries store_id (TokenController); one without it is not a store's token.
            var storeId = user.FindFirst("store_id")?.Value;
            if (string.IsNullOrEmpty(storeId))
                return Results.Forbid();

            var counts = await StoreSyncStatusQuery.CountAsync(db, storeId, cancellationToken);

            return Results.Ok(new
            {
                status = "running",
                storage = "postgresql",
                totalEvents = counts.TotalEvents,
                unprocessedEvents = counts.UnprocessedEvents,
                processedEvents = counts.ProcessedEvents,
                totalInvoices = counts.TotalInvoices,
                timestamp = DateTime.UtcNow
            });
        }).RequireAuthorization();

        return app;
    }
}
```

The response has the same seven members, with the same names and order, so the shape is unchanged. `Program.cs` already reads `// Sync status endpoint` / `app.MapSyncStatus();`, which stays true.

- [ ] **Step 12: Run the tests to see the GREEN**

Run: `dotnet test tests/IndyPOS.CloudApi.IntegrationTests --filter "FullyQualifiedName~SyncStatusEndpointTests"`
Expected: all five pass.

Run: `dotnet test tests/IndyPOS.CloudApi.IntegrationTests` and `dotnet test tests/IndyPOS.CloudApi.Tests`
Expected: 49 and 6 pass.

- [ ] **Step 13: Commit**

```bash
git add src/IndyPOS.CloudApi tests/IndyPOS.CloudApi.IntegrationTests
git commit -m "fix(cloud): /sync/status needs a store token and counts only that store"
```

---

### Task 12: Run the forward-only migration gate on both databases and record it

**Files:**
- Modify: `docs/operations/upgrade-procedure.md`:
  - a StoreHub result paragraph after the invoice-history result, which ends at line 294;
  - a short CloudApi variant of the recipe, with its result;
  - a Change Log row after line 305.

**Interfaces:**
- Consumes:
  - the StoreHub migration `AddInventoryMovementUser` (Task 7); the previous StoreHub release's last migration is `20261002044203_AddInvoiceReprintTable`;
  - the CloudApi migration `AddSyncedEventSourceStore` (Task 11); the previous cloud release's last migration is `20261002073625_AddInvoiceReprints`.
- Produces: the recorded gate results.

Today's recipe (lines 245-294) is **StoreHub only**: the `storehub` database, and a sale written as `product` → … → `inventory_movement`. Steps 5-7 add the cloud variant. That variant proves a previous-release CloudApi can still write and process an inbox row once `SourceStoreId` exists. This is exactly what a cloud rollback of binaries without schema would do.

- [ ] **Step 1: StoreHub, apply the previous release's schema to a throwaway Postgres**

```powershell
docker run -d --name gate -e POSTGRES_PASSWORD=pass -e POSTGRES_DB=storehub -p 55510:5432 postgres:16-alpine
$gate = "Host=localhost;Port=55510;Database=storehub;Username=postgres;Password=pass"
dotnet ef database update 20261002044203_AddInvoiceReprintTable --project src/IndyPOS.Infrastructure --startup-project src/IndyPOS.StoreHub --context StoreHubDbContext --connection $gate
docker exec gate psql -U postgres -d storehub -At -c "SELECT table_name, column_name, data_type, is_nullable, coalesce(column_default, '') FROM information_schema.columns WHERE table_schema='public' ORDER BY 1,2" > $env:TEMP\gate-before.txt
```

- [ ] **Step 2: StoreHub, apply this release's schema and diff**

```powershell
dotnet ef database update --project src/IndyPOS.Infrastructure --startup-project src/IndyPOS.StoreHub --context StoreHubDbContext --connection $gate
docker exec gate psql -U postgres -d storehub -At -c "SELECT table_name, column_name, data_type, is_nullable, coalesce(column_default, '') FROM information_schema.columns WHERE table_schema='public' ORDER BY 1,2" > $env:TEMP\gate-after.txt
Compare-Object (Get-Content $env:TEMP\gate-before.txt) (Get-Content $env:TEMP\gate-after.txt)
```

Expected: exactly one difference, `=>  inventory_movement|created_by_user_id|uuid|YES|`. Anything else means the migration is not additive only. If so, **stop: the release must not ship.**

- [ ] **Step 3: StoreHub, write a sale and an adjustment the way the previous release does**

These `INSERT`s name only columns that existed before `AddInventoryMovementUser`. They never mention `created_by_user_id`.

```powershell
@'
BEGIN;
INSERT INTO product (id, store_id, barcode, name, unit_price, created_utc, last_modified_utc)
VALUES ('a0000000-0000-0000-0000-000000000001', 'GATE', 'GATE-0001', 'Gate product', 10.00, now(), now());
INSERT INTO invoice (id, store_id, user_id, total_amount, created_utc, last_modified_utc)
VALUES ('b0000000-0000-0000-0000-000000000001', 'GATE', 'c0000000-0000-0000-0000-000000000001', 10.00, now(), now());
INSERT INTO invoice_line (id, invoice_id, product_id, product_name, quantity, unit_price, created_utc)
VALUES ('d0000000-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000001', 'a0000000-0000-0000-0000-000000000001', 'Gate product', 1, 10.00, now());
INSERT INTO payment (id, invoice_id, method, amount, created_utc)
VALUES ('e0000000-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000001', 'Cash', 10.00, now());
INSERT INTO inventory_movement (id, store_id, product_id, quantity_delta, reason, reference_id, created_utc)
VALUES ('f0000000-0000-0000-0000-000000000001', 'GATE', 'a0000000-0000-0000-0000-000000000001', -1, 'Sale', 'b0000000-0000-0000-0000-000000000001', now());
INSERT INTO inventory_movement (id, store_id, product_id, quantity_delta, reason, note, created_utc)
VALUES ('f0000000-0000-0000-0000-000000000002', 'GATE', 'a0000000-0000-0000-0000-000000000001', 5, 'Adjustment', 'Gate restock', now());
COMMIT;
SELECT reason, created_by_user_id IS NULL AS user_is_null FROM inventory_movement WHERE store_id = 'GATE' ORDER BY reason;
'@ | docker exec -i gate psql -U postgres -d storehub -v ON_ERROR_STOP=1
```

Expected: `COMMIT`, then two rows, `Adjustment | t` and `Sale | t`. If any `INSERT` fails, **the rollback claim is false and the release must not ship** (CLAUDE.md gate).

Clean up with `docker rm -f gate`.

- [ ] **Step 4: Record the StoreHub result**

In `docs/operations/upgrade-procedure.md`, insert a blank line and this paragraph after line 294, which ends `…in a throwaway \`gate\` container on port 55510.`. Use the date you ran the gate:

```markdown
Result for the route tidy-up release, StoreHub (1 migration, `AddInventoryMovementUser`):
`inventory_movement` gained one column, `created_by_user_id uuid NULL`, with no default; no other
column changed. The schema was applied up to the previous release's last migration
(`20261002044203_AddInvoiceReprintTable`), then to the latest, and the two
`information_schema.columns` snapshots differed only by that column. A complete sale (`product` →
`invoice` → `invoice_line` → `payment` → `inventory_movement`) and a stock adjustment were then
written in one transaction, naming only the previous release's columns; both movements took `NULL`
for the new column. Verified <the date you ran it> with `postgres:16-alpine` in a throwaway `gate`
container on port 55510.
```

- [ ] **Step 5: Cloud, apply the previous release's schema**

`CloudDbContextDesignTimeFactory` reads `ConnectionStrings__cloud-db` from the environment, so `dotnet ef` is pointed at the gate that way. The variable name has a hyphen, so PowerShell needs the `${env:…}` form.

```powershell
docker run -d --name gate-cloud -e POSTGRES_PASSWORD=pass -e POSTGRES_DB=cloud -p 55511:5432 postgres:16-alpine
${env:ConnectionStrings__cloud-db} = "Host=localhost;Port=55511;Database=cloud;Username=postgres;Password=pass"
dotnet ef database update 20261002073625_AddInvoiceReprints --project src/IndyPOS.CloudApi
docker exec gate-cloud psql -U postgres -d cloud -At -c "SELECT table_name, column_name, data_type, is_nullable, coalesce(column_default, '') FROM information_schema.columns WHERE table_schema='public' ORDER BY 1,2" > $env:TEMP\gate-cloud-before.txt
dotnet ef database update --project src/IndyPOS.CloudApi
docker exec gate-cloud psql -U postgres -d cloud -At -c "SELECT table_name, column_name, data_type, is_nullable, coalesce(column_default, '') FROM information_schema.columns WHERE table_schema='public' ORDER BY 1,2" > $env:TEMP\gate-cloud-after.txt
Compare-Object (Get-Content $env:TEMP\gate-cloud-before.txt) (Get-Content $env:TEMP\gate-cloud-after.txt)
```

Expected: exactly one difference, `=>  SyncedEvents|SourceStoreId|character varying|YES|`. Anything else, **stop**.

- [ ] **Step 6: Cloud, ingest and process an event the way the previous release does**

The previous CloudApi's EF `INSERT` names every inbox column it knew about, and never `SourceStoreId`. `EventProcessor` then marks the row processed.

```powershell
@'
BEGIN;
INSERT INTO "SyncedEvents" ("EventId", "StoreId", "EventType", "Payload", "CreatedAtUtc", "ReceivedAtUtc", "ProcessedAtUtc", "Attempts", "NextAttemptAtUtc")
VALUES ('a1000000-0000-0000-0000-000000000001', 0, 'InvoiceCompleted', '{"StoreId":"GATE"}', now(), now(), NULL, 0, NULL);
UPDATE "SyncedEvents" SET "ProcessedAtUtc" = now() WHERE "EventId" = 'a1000000-0000-0000-0000-000000000001';
COMMIT;
SELECT "SourceStoreId" IS NULL AS source_is_null, "ProcessedAtUtc" IS NOT NULL AS processed FROM "SyncedEvents";
'@ | docker exec -i gate-cloud psql -U postgres -d cloud -v ON_ERROR_STOP=1
```

Expected: `COMMIT`, then one row, `t | t`. If the `INSERT` or `UPDATE` fails, a cloud rollback would break ingest: **stop, the release must not ship.**

Clean up with `docker rm -f gate-cloud` and `Remove-Item Env:\ConnectionStrings__cloud-db`.

- [ ] **Step 7: Record the cloud variant and its result**

After the StoreHub paragraph from Step 4, add this:

````markdown
**The cloud database needs the same gate** whenever a release adds a CloudApi migration: a cloud
rollback restores binaries, not schema, so the previous CloudApi must still ingest and process into
the new schema. `CloudDbContextDesignTimeFactory` reads `ConnectionStrings__cloud-db`, so point
`dotnet ef` at a throwaway database that way:

```powershell
docker run -d --name gate-cloud -e POSTGRES_PASSWORD=pass -e POSTGRES_DB=cloud -p 55511:5432 postgres:16-alpine
${env:ConnectionStrings__cloud-db} = "Host=localhost;Port=55511;Database=cloud;Username=postgres;Password=pass"
dotnet ef database update <previous release's last cloud migration> --project src/IndyPOS.CloudApi
# snapshot information_schema.columns, update to the latest, snapshot again and diff
```

Then `INSERT` a `"SyncedEvents"` row naming only the previous release's columns, and mark it
processed with an `UPDATE`, as the old `IngestEventsCommandHandler` and `EventProcessor` do.

Result for the route tidy-up release, cloud (1 migration, `AddSyncedEventSourceStore`): `SyncedEvents`
gained one column, `SourceStoreId character varying(50) NULL` with a comment, and one index; no other
column changed. A row inserted naming only the previous release's columns (from
`20261002073625_AddInvoiceReprints`) and then marked processed succeeded, and its `SourceStoreId`
stayed `NULL`. Such a row is counted for no store by `/sync/status`. Verified <the date you ran it>
with `postgres:16-alpine` in a throwaway `gate-cloud` container on port 55511.
````

Add this Change Log row after line 305:

```markdown
| <the date you ran it> | Ran the gate against the route tidy-up release's 2 migrations (StoreHub `AddInventoryMovementUser`, cloud `AddSyncedEventSourceStore`); added the cloud variant of the recipe |
```

- [ ] **Step 8: Commit**

```bash
git add docs/operations/upgrade-procedure.md
git commit -m "docs(ops): record the forward-only gate for both route tidy-up migrations"
```

---

### Task 13: Measure the whole solution and update the counts

**Files:**
- Modify: `CLAUDE.md:206-216` (the Docker-down comment block) and `:229-238` (the "Solution suites total" paragraph)
- Modify: `ONBOARDING.md:54-66` (Trap 1, its text and table) and `:114-129` (Expected counts)

**Interfaces:**
- Consumes: every earlier task.
- Produces: measured counts in the two docs.

- [ ] **Step 1: Run the whole solution with TRX output**

With Docker running, and the real store databases present if you have them:

```powershell
$trx = "$env:TEMP\indypos-route-tidy-a-trx"
Remove-Item -Recurse -Force $trx -ErrorAction SilentlyContinue
dotnet test --logger trx --results-directory $trx
Get-ChildItem $trx -Filter *.trx | ForEach-Object {
    [xml]$run = Get-Content $_.FullName
    $c = $run.TestRun.ResultSummary.Counters
    [pscustomobject]@{
        Suite       = [IO.Path]::GetFileNameWithoutExtension(($run.TestRun.TestDefinitions.UnitTest | Select-Object -First 1).storage)
        Total       = [int]$c.total
        Passed      = [int]$c.passed
        Failed      = [int]$c.failed
        NotExecuted = [int]$c.notExecuted
    }
} | Sort-Object Suite | Format-Table -AutoSize
```

Expected: `Failed` is 0 for every suite, and the exit code is 0. If a test fails, read its message in the `.trx` before re-running. A flake must be named, not re-rolled (CLAUDE.md).

The table shows the measured numbers, and those are what go in the docs. For a cross-check, this plan expects:

| Suite | Baseline | Added here | Expected |
|---|---|---|---|
| Application | 555 | +6 `DateRangeRuleTests`, +1 seller stamp, +1 ingest stamp | **563** |
| StoreHub.IntegrationTests | 241 | +1 route table, +3 adjust-quantity, +1 stale `userId`, +20 dated reports | **266** |
| CloudApi.IntegrationTests | 44 | +5 `/sync/status` | **49** |
| MigrationTool | 145 (1 skip) | — | 145 |
| Domain, Vault, CloudApi, Windows.Forms | 56, 17, 6, 47 | — | unchanged |
| **Total** | **1111** (1110 pass, 1 skip) | +38 | **1149** (1148 pass, 1 skip) |

If a measured number differs from this table, find out why before writing it down.

- [ ] **Step 2: Update `CLAUDE.md`**

In the "Solution suites total" paragraph (lines 229-238):
- **The total:** replace **1111** and "(1110 pass, 1 skipped) — measured 2026-10-02 (after invoice-history plan 2 …)" with the measured total, its pass and skip split, and the date. Add "after the route tidy-up PR A", and keep "1111 on 2026-10-02" in the history list.
- **Per suite:** put in the measured Application, StoreHub.IntegrationTests and CloudApi.IntegrationTests numbers.
- **The growth sentence:** it becomes the route tidy-up's own tests: Application +8, StoreHub.IntegrationTests +25 and CloudApi.IntegrationTests +5, or whatever was measured.
- **Without the real databases:** "**1091** (DERIVED as 1111 − 20 …)" becomes the measured total − 20, still marked **DERIVED**.

In the Docker-down comment block (lines 206-216):
- 359 becomes **389** = 258 + 82 + 49.
- Add one sentence: "The route tidy-up PR A added 25 StoreHub tests and 5 CloudApi tests, all needing a container, for 258 and 49 -- DERIVED, not re-measured with Docker down."
- The StoreHub row becomes `(258 of 266; 8 need no container …)`.
- The CloudApi row becomes `(49 of 49; all need a container)`.

Recompute each figure from the measured totals: StoreHub Docker-down = measured StoreHub total − 8.

- [ ] **Step 3: Update `ONBOARDING.md`**

In Trap 1 (lines 54-66):
- "Expect **359 failures**" becomes the same derived figure as in `CLAUDE.md` (389).
- Append this sentence to the derivation paragraph: "The route tidy-up PR A (2026-10-02) then added 25 StoreHub tests and 5 CloudApi tests, all on a container, bringing them to **258** and **49**, derived the same way."
- Table rows:
  - StoreHub becomes `| 266 | **258** (8 need no container, derived) |`;
  - CloudApi becomes `| 49 | **49** (all need a container) |`.

In "Expected counts" (lines 114-129):
- **1111 total (1110 pass, 1 skipped) — measured 2026-10-02 (after invoice-history plan 2 …** becomes the measured total, with "after the route tidy-up PR A". Move 1111 into the "supersedes" list.
- "**1091** (**derived** as 1111 − 20 …)" becomes measured − 20, still **derived**.
- The table rows for `IndyPOS.Application.Tests`, `IndyPOS.StoreHub.IntegrationTests` and `IndyPOS.CloudApi.IntegrationTests` take the measured numbers.

- [ ] **Step 4: Check that the docs agree**

Run: `grep -nE "1111|1149|1091|1129|359|389" CLAUDE.md ONBOARDING.md`
Expected: every current figure is the new one, and old figures appear only in "supersedes" or history lists. The Docker-down figures and "without the real databases" figures each carry **derived**/**DERIVED**.

- [ ] **Step 5: Commit**

```bash
git add CLAUDE.md ONBOARDING.md
git commit -m "docs: measured test counts after the route tidy-up PR A"
```

---

## Self-Review

**Spec coverage** (spec § → task):
- §3.1, one file per area with `Map<Area>Endpoints`: Tasks 2-6. Auth (2), Products (3), PaymentMethods and Catalogue (4), Sync (5), Reports and PayLater (6). System became `SystemInfo` (2; see File Structure for why). `POST /sales/complete` joins the existing `Endpoints/Sales` (5).
- §3.1, "`Program.cs` keeps … only": Task 6 Step 6 greps for any inline `app.MapX(`.
- §3.1, helpers move to `Endpoints/Common`: Task 1.
- §3.1, byte-for-byte and the existing tests as proof: Tasks 1-6 change no test file. Each re-runs the full StoreHub suite, plus the route-table pin (Task 1), which covers routes that have no test.
- §3.2.1:
  - the nullable column and `AddInventoryMovementUser`: Task 7;
  - `adjust-quantity` takes the token user, joins the filter, and gives 401: Task 7;
  - sale movements store the seller: Task 8;
  - older rows stay `NULL`: Task 12 Step 3.
- §3.2.2, property, doc comment and client assignment removed; a stale `userId` is accepted and pinned: Task 9.
- §3.2.3, shared rule (2000-01-01..2099-12-31, to ≥ from) used by `/sales` and every dated `/reports` route, with a Thai 400: Task 10.
- §3.2.4, cloud `/sync/status` needs the store token, counts that store only by `SourceStoreId` (stamped at ingest; legacy NULL rows not counted), and gives 401 with no token: Task 11. StoreHub `/sync/status` is unchanged: Task 5 moves it as it is.
- §3.3, gate run on both migrations (StoreHub and cloud) and recorded: Task 12.
- §8, the PR A test names:
  - `AdjustQuantity_WithATokenWithoutAUserId_ReturnsUnauthorized` and `AdjustQuantity_WithAValidToken_RecordsTheCaller`: Task 7;
  - `CompleteSale_WithAStaleUserIdInTheBody_AcceptsTheSale`: Task 9;
  - `…_WithToBeforeFrom_ReturnsBadRequest` and `…_WithADateBeyondTheLatest_ReturnsBadRequest`: Task 10, as `DatedReport_*` theories over the four routes;
  - `SyncStatus_WithoutAToken_ReturnsUnauthorized` and `SyncStatus_WithAStoreToken_CountsOnlyThatStore`, plus the ingest stamp, another store's row and a legacy NULL row: Task 11;
  - the gate recorded: Task 12;
  - counts measured: Task 13.
- §4-§7 are out of scope: PR B, or their own spec.

**Placeholder scan:** no TBD or TODO, and no "similar to Task N". Every code step shows its code. The only fill-ins are `<the date you ran it>` in Task 12 and the measured numbers in Task 13. Both are runtime facts, and the plan says exactly where each comes from.

**Type consistency:**
- `RequireUserIdFilter` and `GetRequiredUserId` are in `IndyPOS.StoreHub.Endpoints.Common` (Task 1), and Tasks 5 and 7 use them from there.
- `InventoryMovement.CreatedByUserId : Guid?` (Task 7) is set in Task 8.
- `AdjustProductQuantityCommand.UserId : Guid` (Task 7).
- `CompleteSaleRequest(Lines, Payments)` (Task 9) matches every call site listed.
- `DateRangeRule.FindViolation(DateOnly, DateOnly) : string?`, `ToBeforeFromMessage`, `OutOfBoundsMessage`, `EarliestDate` and `LatestDate` (Task 10) are used by `SalesQueryRules`, `ReportsEndpoints` and both test files.
- `SyncedEventEntity.SourceStoreId : string?` is set by `IngestEventsCommandHandler` and read by `StoreSyncStatusQuery.CountAsync`; `MapSyncStatus` and `StoreSyncStatus` (Task 11) match.
- The test-side `SyncStatusBody` matches the response members.

**Review Focus:** all five lines have a test in the task that owns the code: Task 1 (route table), Task 7 (no movement written), Task 10 (just-out-of-bounds date and Thai body), and Task 11 (legacy NULL-store inbox row, token without a store).
