# Route Tidy-Up B — Renames and Conventions Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Every StoreHub route follows the REST rules. The three verb paths become nouns (`POST /sales`, `POST /pay-later/{id}/payments`, `POST /products/{id}/stock-adjustments`), the two payment-method roots become one (`/payment-methods`, with `?include=all` for the catalogue), the till client moves with them in the same commits, and `docs/architecture/api-conventions.md` writes the rules down.

**Architecture:** Each rename is one task that moves the server route, the till client call (`StoreHubHttpClient`) and their tests together, so no commit leaves the till calling a path the server no longer has. The old paths are hard-removed (no alias, spec §2). A shared `RenamedRouteTests` class pins that each old path is gone. `GET /payment-methods` keeps the weaker `CanReadProducts` route policy; `?include=all` runs the `CanManagePaymentMethods` check inside the handler through `IAuthorizationService`, so one route serves both callers. Scripts, Bruno and docs follow in one task, and the conventions doc in another.

**Tech Stack:** C# .NET 10, ASP.NET Core minimal APIs, Nokpirab CQRS, xUnit + FluentAssertions 8 + Moq, WireMock.Net (client E2E), Testcontainers PostgreSQL (or `INDYPOS_TEST_POSTGRES`), Bruno, PowerShell smoke script.

**Spec:** `docs/superpowers/specs/2026-10-02-route-tidy-up-design.md`. This plan covers **§4 (PR B)** and writes §6 into the conventions doc. PR A (§3) merged as #111.

## Prerequisites

- Branch from `development` at or after `880f268`.
- **Docker is running**, or `INDYPOS_TEST_POSTGRES` points at a Postgres server (ONBOARDING.md, "Continuous integration and running without Docker"). `IndyPOS.StoreHub.IntegrationTests` fails in under a second without one, which looks like a regression but is not.
- **Line numbers** below are those at `880f268`. Find each block by the lines quoted, not only by number.
- This is the **first PR with CI checks**. CI must be green before merge.

## Global Constraints

- [§4] "**Every old path returns 404,** and one test per route pins this." — with one measured exception, below.
- [§4] "**`GET /payment-methods` with no `include`** stays the offerable list under `CanReadProducts`, exactly as today. Any `include` value other than `all` gets a Thai **400**."
- [§4] `POST /sales` "Returns **201** with `Location: /sales/{id}` and the same body (`InvoiceId`, `InvoiceNumber`, …)".
- [§4] `/pay-later/{id}/payments` and `/products/{id}/stock-adjustments`: "Same body and responses".
- [§4] `?include=all`, `POST /payment-methods` and `PATCH /payment-methods/{code}` need `CanManagePaymentMethods`, "else **403**".
- [§2] "**So every rename is a hard rename.** No deprecated alias is kept."
- [§4] "**The till client** (`StoreHubHttpClient`) moves to the new paths in the same PR." This plan is stricter: **in the same commit** as its server route.
- [§8] "The suite counts are measured, never derived, and CLAUDE.md and ONBOARDING.md are updated."
- **No migration.** PR B changes no schema, so the forward-only gate has nothing to run. If a task finds it needs one, stop and ask.
- **Offline-first is priority #1.** Nothing here touches the sale write path beyond its URL and status code. `SaleRepository.CompleteSaleAsync` is not touched.
- **Error bodies** are Thai `{ "error": "..." }`. A policy-level 401/403 has no body. Existing non-Thai bodies (`"Delta must not be zero."`) are not changed here; they keep their exact text ("same body and responses").
- **Test style:** `Subject_WhenScenario_DirectVerbOutcome` (`When`/`With`, a direct verb, never `Should`); one behaviour per test; negative cases first; Arrange/Act/Assert separated by blank lines; named constants for magic values; FluentAssertions chains on separate lines with the dots aligned.
- **Shared integration DB:** the StoreHub test DB is **not** reset between tests. Never disable or edit a seeded payment method (`Cash` etc.): other tests sell with it. Each test makes its own campaign method with a unique code and asserts only on that.
- **Comments must match behaviour.** Each task names the comments it corrects. No planning labels (`PR B`, `Task 3`) in code comments.
- **Diagrams (Pond, 2026-10-06):** `docs/diagrams/*` will move to Mermaid in a later, separate PR. Here, only swap the path text and pad each changed line back to its **exact old character width**. No redraws.
- **Commits:** conventional commits, at least one per task. Never push without Pond.

## Decisions this plan makes (flag them in the PR body)

1. **`POST /sales/complete` answers 405, not 404.** The `/sales` group maps a catch-all `GET /sales/{value}` (`SaleQueryEndpoints.cs:59`). Once the POST is gone, `/sales/complete` still matches that route's path, so ASP.NET routing answers *405 Method Not Allowed*. That is correct HTTP, and the path is just as gone. Making it 404 would mean weakening the 400 the invoice-history spec asks for on `GET /sales/abc`. The test pins **405** and says why.
2. **The spec's "as manager" catalogue test signs in as admin.** `RoleCapabilities` gives `payment_methods.manage` to `SystemAdmin` only, not `StoreManager`. So the test is `…AsAdmin_ReturnsTheWholeCatalogue`, and a manager gets its own **403** test.
3. **`include` is matched exactly (`all`, ordinal).** `ALL` is a 400. Query values are case-sensitive by convention, the only caller is our own client, and the spec says "any value other than `all`".
4. **An empty `include` (`?include=`) means no include,** so it returns the offerable list, not a 400. The handler checks `string.IsNullOrEmpty`, so this does not depend on how minimal APIs bind an empty value.
5. **`POST /payment-methods` keeps its 200** and `{ code }` body. The spec gives it only a path change, and the conventions doc does not add a "create returns 201" rule.

## Review Focus

These are the inputs the spec implies but its test list does not exercise, most likely first. Each has a test in the task that owns the code:

1. **A typo in a till client path.** No test pins any of the client's payment-method, adjust or repayment URLs today. A wrong path would only show up at the till, as a failed sale or a missing admin screen. Expected: each moved call sends exactly the new verb and path. → Task 1 (`RenamedCall_WithTheClient_SendsTheNewRoute`, rows added in Tasks 1-4).
2. **The till reading a 201.** The till has only ever seen a 200 from a sale. Expected: a 201 is a success and its body is read. → Task 2 (`CompleteSaleAsync_WithACreatedResponse_ReturnsTheSale`).
3. **A `Location` that points nowhere.** Expected: `GET` on the returned `Location` with the same cashier token returns that very sale. → Task 2 (`CreateSale_WithAValidSale_LocationResolvesToTheSale`).
4. **A store manager asking for `include=all`.** A manager can do almost everything else, so this is the likeliest surprise. Expected: 403, never the catalogue. → Task 1 (`ListPaymentMethods_WithIncludeAllAsStoreManager_ReturnsForbidden`).
5. **`?include=` with no value, and `include=ALL`.** Expected: the empty one is the offerable list (decision 4); `ALL` is a Thai 400 (decision 3). → Task 1 (`ListPaymentMethods_WithAnEmptyInclude_ReturnsTheOfferableList`, `ListPaymentMethods_WithAnUpperCaseIncludeAll_ReturnsBadRequest`).

---

## File Structure

```
src/IndyPOS.StoreHub/Endpoints/
  PaymentMethods/PaymentMethodsEndpoints.cs   MODIFY  one root; ?include=all checks the manage policy
  Sales/SaleCompletionEndpoints.cs            MODIFY  POST /sales, 201 + Location
  Sales/SalesEndpoints.cs                     MODIFY  doc comment names POST /sales
  PayLater/PayLaterEndpoints.cs               MODIFY  POST /pay-later/{id}/payments
  Products/ProductsEndpoints.cs               MODIFY  POST /products/{id}/stock-adjustments
src/IndyPOS.Application/Common/Exceptions/
  SaleValidationException.cs                  MODIFY  comment names POST /sales
src/IndyPOS.Infrastructure/Services/StoreHub/
  StoreHubHttpClient.cs                       MODIFY  every moved call
tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/
  PaymentMethodsEndpointTests.cs              CREATE  first HTTP tests for payment methods
  RenamedRouteTests.cs                        CREATE  each old path is gone
  RouteTableTests.cs                          MODIFY  pin the new paths
  SalesEndpointTests.cs                       MODIFY  new path, 201, Location
  SalesHistoryEndpointsTests.cs, PayLaterSaleEndpointTests.cs,
  ReportsEndpointTests.cs, ProductsEndpointTests.cs   MODIFY  URLs only
tests/IndyPOS.Application.Tests/Integration/StoreHub/
  StoreHubHttpClientTests.cs                  MODIFY  path pins + 201 test
  StoreHubE2ETests.cs                         MODIFY  WireMock on POST /sales, 201
scripts/smoke-test.ps1                        MODIFY
.bruno/                                       MODIFY  inventory/ folded into products/, new payment-methods/
docs/development/getting-started.md, docs/diagrams/{README,architecture-overview,flows}.md   MODIFY
docs/architecture/api-conventions.md          CREATE
CLAUDE.md, ONBOARDING.md                      MODIFY  measured counts
```

---

### Task 1: One root for payment methods

**Files:**
- Modify: `src/IndyPOS.StoreHub/Endpoints/PaymentMethods/PaymentMethodsEndpoints.cs` (whole file)
- Modify: `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubHttpClient.cs:316-357`
- Modify: `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/RouteTableTests.cs:36-39`
- Create: `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/PaymentMethodsEndpointTests.cs`
- Create: `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/RenamedRouteTests.cs`
- Modify: `tests/IndyPOS.Application.Tests/Integration/StoreHub/StoreHubHttpClientTests.cs`

**Interfaces:**
- Consumes: `IntegrationTestBase` (`AuthenticateAsCashierAsync`, `AuthenticateAsManagerAsync`, `AuthenticateAsAdminAsync`, `ClearAuthentication`, `Client`, `JsonOptions`); `PaymentMethodDto(string Code, string DisplayName, PaymentMethodKind Kind, bool IsEnabled, int DisplayOrder)`; `AddCampaignPaymentMethodRequest(string Code, string DisplayName, int DisplayOrder)`; `UpdatePaymentMethodRequest(bool? IsEnabled, string? DisplayName, int? DisplayOrder)`.
- Produces:
  - `RenamedRouteTests.RenamedRoute_OnTheOldPath_ReturnsNotFound(string method, string path)` — a `[Theory]`; Tasks 3 and 4 add `[InlineData]` rows.
  - `StoreHubHttpClientTests.RenamedCall_WithTheClient_SendsTheNewRoute(string call, string expectedMethod, string expectedPathAndQuery)` — a `[Theory]` over a `switch` on `call`; Tasks 2-4 add a row **and** a `switch` arm.
  - `StoreHubHttpClientTests.CaptureRequest(string responseJson)` returning `Func<(string Method, string PathAndQuery)>`.
  - Route table entries `GET /payment-methods CanReadProducts`, `POST /payment-methods CanManagePaymentMethods`, `PATCH /payment-methods/{code} CanManagePaymentMethods`.
  - `RouteTableTests.RemovedRoutes` (a `string[]` in the same `"VERB pattern policy"` form) and `RouteTable_WithTheAppBuilt_ContainsNoRemovedRoute`; Tasks 2-4 add their old pattern to the list.

- [ ] **Step 1: Write the failing endpoint tests**

Create `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/PaymentMethodsEndpointTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.PaymentMethods;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

/// <summary>
/// One root for payment methods. Each test makes its own campaign method with a unique code: the test
/// database is shared and never reset, and other tests sell with the seeded methods, so a seeded one
/// is never disabled here.
/// </summary>
[Collection("Integration")]
public class PaymentMethodsEndpointTests : IntegrationTestBase
{
    private const string Route = "/payment-methods";
    private const string CatalogueRoute = "/payment-methods?include=all";
    private const string SeededMethod = "Cash";
    private const string UnknownCode = "NoSuchMethod";

    public PaymentMethodsEndpointTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task ListPaymentMethods_WithIncludeAllWithoutAToken_ReturnsUnauthorized()
    {
        ClearAuthentication();

        var response = await Client.GetAsync(CatalogueRoute);

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListPaymentMethods_WithAnUnknownInclude_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();

        var response = await Client.GetAsync($"{Route}?include=disabled");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListPaymentMethods_WithAnUnknownInclude_ExplainsInThai()
    {
        await AuthenticateAsAdminAsync();

        var response = await Client.GetAsync($"{Route}?include=disabled");

        (await ErrorOfAsync(response)).Should()
                                      .Be("ค่า include ไม่ถูกต้อง: disabled (ใช้ได้เฉพาะ all)");
    }

    // Query values are case-sensitive by convention, and the only caller is our own client.
    [Fact]
    public async Task ListPaymentMethods_WithAnUpperCaseIncludeAll_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();

        var response = await Client.GetAsync($"{Route}?include=ALL");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListPaymentMethods_WithIncludeAllAsCashier_ReturnsForbidden()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.GetAsync(CatalogueRoute);

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Forbidden);
    }

    // payment_methods.manage belongs to SystemAdmin only. A manager can do almost everything else,
    // so this is the likeliest surprise.
    [Fact]
    public async Task ListPaymentMethods_WithIncludeAllAsStoreManager_ReturnsForbidden()
    {
        await AuthenticateAsManagerAsync();

        var response = await Client.GetAsync(CatalogueRoute);

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddPaymentMethod_AsCashier_ReturnsForbidden()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.PostAsJsonAsync(Route, NewCampaign(UniqueCode()));

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdatePaymentMethod_WithAnUnknownCode_ReturnsNotFound()
    {
        await AuthenticateAsAdminAsync();

        var response = await Client.PatchAsJsonAsync($"{Route}/{UnknownCode}", Disable());

        response.StatusCode.Should()
                           .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListPaymentMethods_WithIncludeAllAsAdmin_ReturnsTheWholeCatalogue()
    {
        await AuthenticateAsAdminAsync();
        var disabled = await AddDisabledCampaignAsync();

        var methods = await ListAsync(CatalogueRoute);

        methods.Should()
               .Contain(m => m.Code == disabled && !m.IsEnabled);
    }

    [Fact]
    public async Task ListPaymentMethods_WithoutInclude_ReturnsTheOfferableList()
    {
        await AuthenticateAsAdminAsync();
        var disabled = await AddDisabledCampaignAsync();

        var codes = (await ListAsync(Route)).Select(m => m.Code);

        codes.Should()
             .Contain(SeededMethod)
             .And
             .NotContain(disabled);
    }

    [Fact]
    public async Task ListPaymentMethods_WithAnEmptyInclude_ReturnsTheOfferableList()
    {
        await AuthenticateAsAdminAsync();
        var disabled = await AddDisabledCampaignAsync();

        var codes = (await ListAsync($"{Route}?include=")).Select(m => m.Code);

        codes.Should()
             .Contain(SeededMethod)
             .And
             .NotContain(disabled);
    }

    [Fact]
    public async Task AddPaymentMethod_AsAdmin_AddsTheCampaignToTheCatalogue()
    {
        await AuthenticateAsAdminAsync();
        var code = UniqueCode();

        var response = await Client.PostAsJsonAsync(Route, NewCampaign(code));

        response.EnsureSuccessStatusCode();
        (await ListAsync(CatalogueRoute)).Should()
                                         .Contain(m => m.Code == code);
    }

    [Fact]
    public async Task UpdatePaymentMethod_AsAdmin_DisablesTheMethod()
    {
        await AuthenticateAsAdminAsync();
        var code = UniqueCode();
        (await Client.PostAsJsonAsync(Route, NewCampaign(code))).EnsureSuccessStatusCode();

        var response = await Client.PatchAsJsonAsync($"{Route}/{code}", Disable());

        response.EnsureSuccessStatusCode();
        (await ListAsync(CatalogueRoute)).Should()
                                         .Contain(m => m.Code == code && !m.IsEnabled);
    }

    private static string UniqueCode() => $"Test{Guid.NewGuid():N}";

    private static AddCampaignPaymentMethodRequest NewCampaign(string code) =>
        new(code, DisplayName: "โครงการทดสอบ", DisplayOrder: 99);

    private static UpdatePaymentMethodRequest Disable() =>
        new(IsEnabled: false, DisplayName: null, DisplayOrder: null);

    private async Task<string> AddDisabledCampaignAsync()
    {
        var code = UniqueCode();
        (await Client.PostAsJsonAsync(Route, NewCampaign(code))).EnsureSuccessStatusCode();
        (await Client.PatchAsJsonAsync($"{Route}/{code}", Disable())).EnsureSuccessStatusCode();
        return code;
    }

    private async Task<IReadOnlyList<PaymentMethodDto>> ListAsync(string url)
    {
        var response = await Client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IReadOnlyList<PaymentMethodDto>>(JsonOptions))!;
    }

    private static async Task<string?> ErrorOfAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString();
}
```

Create `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/RenamedRouteTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

/// <summary>
/// Every renamed route was a hard rename: the old path is gone, with no alias. Signed in as admin,
/// who holds every capability, so a 403 can never stand in for a missing route.
/// </summary>
[Collection("Integration")]
public class RenamedRouteTests : IntegrationTestBase
{
    private const string AnyId = "6f1c2a4e-8d3b-4c7a-9e21-5b0d7f3a1c88";

    public RenamedRouteTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    [Theory]
    [InlineData("GET", "/admin/payment-methods")]
    [InlineData("POST", "/admin/payment-methods")]
    [InlineData("PATCH", "/admin/payment-methods/Cash")]
    public async Task RenamedRoute_OnTheOldPath_ReturnsNotFound(string method, string path)
    {
        await AuthenticateAsAdminAsync();

        var response = await SendAsync(method, path);

        response.StatusCode.Should()
                           .Be(HttpStatusCode.NotFound);
    }

    private Task<HttpResponseMessage> SendAsync(string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path.Replace("{id}", AnyId));
        if (method != "GET")
        {
            request.Content = JsonContent.Create(new { });
        }

        return Client.SendAsync(request);
    }
}
```

- [ ] **Step 2: Write the failing client path test**

In `tests/IndyPOS.Application.Tests/Integration/StoreHub/StoreHubHttpClientTests.cs`, add `using IndyPOS.Application.UseCases.StoreHub.PaymentMethods;` and, after `GetLegacyPaymentsSummaryAsync_WithAThaiCulture_SendsGregorianDates`:

```csharp
    private const string CampaignCode = "Campaign2569";

    // Nothing else pins the client's URLs, and a wrong one only shows at the till.
    // InlineData (not delegates) keeps one test case per row in the runner's count.
    [Theory]
    [InlineData("GetOfferablePaymentMethods", "GET", "/payment-methods")]
    [InlineData("GetAllPaymentMethods", "GET", "/payment-methods?include=all")]
    [InlineData("AddCampaignPaymentMethod", "POST", "/payment-methods")]
    [InlineData("SetPaymentMethodEnabled", "PATCH", "/payment-methods/Campaign2569")]
    [InlineData("UpdatePaymentMethodDisplay", "PATCH", "/payment-methods/Campaign2569")]
    public async Task RenamedCall_WithTheClient_SendsTheNewRoute(
        string call, string expectedMethod, string expectedPathAndQuery)
    {
        _sut.SetAuthToken("valid-token");
        var request = CaptureRequest(ResponseFor(call));

        await InvokeAsync(call);

        request().Should()
                 .Be((expectedMethod, expectedPathAndQuery));
    }

    private Task InvokeAsync(string call) => call switch
    {
        "GetOfferablePaymentMethods" => _sut.GetOfferablePaymentMethodsAsync(),
        "GetAllPaymentMethods" => _sut.GetAllPaymentMethodsAsync(),
        "AddCampaignPaymentMethod" => _sut.AddCampaignPaymentMethodAsync(CampaignCode, "โครงการ", 9),
        "SetPaymentMethodEnabled" => _sut.SetPaymentMethodEnabledAsync(CampaignCode, enabled: false),
        "UpdatePaymentMethodDisplay" => _sut.UpdatePaymentMethodDisplayAsync(CampaignCode, "โครงการ", 9),
        _ => throw new ArgumentOutOfRangeException(nameof(call), call, "No such client call.")
    };

    private static string ResponseFor(string call) =>
        call.StartsWith("Get", StringComparison.Ordinal) ? "[]" : "{}";
```

And beside `CaptureRequestUri`:

```csharp
    private Func<(string Method, string PathAndQuery)> CaptureRequest(string responseJson)
    {
        (string, string) captured = default;
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) =>
                captured = (request.Method.Method, request.RequestUri!.PathAndQuery))
            .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
            });

        return () => captured;
    }
```

- [ ] **Step 3: Run the new tests and watch them fail**

```powershell
dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~PaymentMethodsEndpointTests|FullyQualifiedName~RenamedRouteTests"
dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~RenamedCall_WithTheClient_SendsTheNewRoute"
```

Expected:
- `ListPaymentMethods_WithAnUnknownInclude_*`, `…UpperCaseIncludeAll…` FAIL with 200 (today `include` is ignored).
- `…WithIncludeAllAsCashier…`, `…AsStoreManager…` FAIL with 200 (today `?include=all` returns the offerable list to anyone with `CanReadProducts`).
- `…WithIncludeAllAsAdmin_ReturnsTheWholeCatalogue` FAILS: the disabled campaign is missing.
- `AddPaymentMethod_*`, `UpdatePaymentMethod_*` FAIL with 405 (`/payment-methods` has only a GET today).
- All 3 `RenamedRoute_OnTheOldPath_ReturnsNotFound` rows FAIL (the old routes still answer).
- If Step 6's `RemovedRoutes` pin is already written: `RouteTable_WithTheAppBuilt_ContainsNoRemovedRoute` FAILS listing the 3 `/admin/payment-methods` routes.
- `…WithoutInclude…`, `…WithAnEmptyInclude…` and `…WithoutAToken…` PASS. They pin today's behaviour, which must survive.
- Client rows: `GetOfferablePaymentMethods` PASSES; the other 4 FAIL showing `/admin/payment-methods…`.

- [ ] **Step 4: Rewrite `PaymentMethodsEndpoints.cs`**

Replace the whole file:

```csharp
using System.Security.Claims;
using IndyPOS.Application.UseCases.StoreHub.PaymentMethods;
using Microsoft.AspNetCore.Authorization;
using Nokpirab;

namespace IndyPOS.StoreHub.Endpoints.PaymentMethods;

/// <summary>
/// Payment methods under one root. GET /payment-methods is what the till may offer;
/// ?include=all widens it to the whole catalogue, enabled or not. The catalogue, adding a campaign
/// and editing a method all need CanManagePaymentMethods.
/// </summary>
public static class PaymentMethodsEndpoints
{
    private const string ManagePolicy = "CanManagePaymentMethods";
    private const string IncludeAll = "all";

    public static IEndpointRouteBuilder MapPaymentMethodsEndpoints(this IEndpointRouteBuilder app)
    {
        MapList(app);
        MapAddCampaign(app);
        MapUpdateMethod(app);
        return app;
    }

    internal static string UnknownIncludeMessage(string value) =>
        $"ค่า include ไม่ถูกต้อง: {value} (ใช้ได้เฉพาะ all)";

    // The route carries the weaker policy so a cashier still reaches the offerable list. The
    // catalogue's stronger check runs here, and only when the catalogue is asked for.
    private static void MapList(IEndpointRouteBuilder app)
    {
        app.MapGet("/payment-methods", async (
            IQueryHandler<GetOfferablePaymentMethodsQuery, IReadOnlyList<PaymentMethodDto>> offerable,
            IQueryHandler<GetAllPaymentMethodsQuery, IReadOnlyList<PaymentMethodDto>> catalogue,
            IAuthorizationService authorization,
            ClaimsPrincipal user,
            string? include,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrEmpty(include))
            {
                return Results.Ok(await offerable.HandleAsync(new GetOfferablePaymentMethodsQuery(), cancellationToken));
            }

            if (!string.Equals(include, IncludeAll, StringComparison.Ordinal))
            {
                return Results.BadRequest(new { error = UnknownIncludeMessage(include) });
            }

            var canManage = await authorization.AuthorizeAsync(user, ManagePolicy);
            return canManage.Succeeded
                ? Results.Ok(await catalogue.HandleAsync(new GetAllPaymentMethodsQuery(), cancellationToken))
                : Results.Forbid();
        }).RequireAuthorization("CanReadProducts");
    }
```

Then keep `MapAddCampaign` and `MapUpdateMethod` exactly as they are, except:
- `app.MapPost("/admin/payment-methods", …` becomes `app.MapPost("/payment-methods", …`, and its comment `// Admin: add a government-campaign payment method` becomes `// Add a government-campaign payment method`.
- `app.MapPatch("/admin/payment-methods/{code}", …` becomes `app.MapPatch("/payment-methods/{code}", …`, and its comment `// Admin: toggle enabled state and/or edit display of a payment method` becomes `// Toggle enabled state and/or edit display of a payment method`.
- Both `.RequireAuthorization("CanManagePaymentMethods")` become `.RequireAuthorization(ManagePolicy)`.
- `MapOfferable` and `MapCatalogue` are deleted; `MapList` replaces both.

`Results.Forbid()` gives a bodiless 403 because `Program.cs:144` sets JWT bearer as the default scheme.

- [ ] **Step 5: Move the client calls**

In `StoreHubHttpClient.cs`:
- `GetAllPaymentMethodsAsync` (line 324): `"/admin/payment-methods"` → `"/payment-methods?include=all"`.
- `AddCampaignPaymentMethodAsync` (line 333): `"/admin/payment-methods"` → `"/payment-methods"`.
- `SetPaymentMethodEnabledAsync` (line 343) and `UpdatePaymentMethodDisplayAsync` (line 354): `$"/admin/payment-methods/{Uri.EscapeDataString(code)}"` → `$"/payment-methods/{Uri.EscapeDataString(code)}"`.

- [ ] **Step 6: Update the route pin**

In `RouteTableTests.cs`, replace lines 37-39:

```csharp
        "GET /admin/payment-methods CanManagePaymentMethods",
        "POST /admin/payment-methods CanManagePaymentMethods",
        "PATCH /admin/payment-methods/{code} CanManagePaymentMethods",
```

with:

```csharp
        "POST /payment-methods CanManagePaymentMethods",
        "PATCH /payment-methods/{code} CanManagePaymentMethods",
```

`"GET /payment-methods CanReadProducts"` (line 36) stays. It is the proof that the list route kept the weaker policy.

Then pin the removals. An old path's HTTP status alone cannot always prove it is gone (a handler can answer the same 404, Task 3), so the route table must not contain it. Add beside `MovedRoutes`:

```csharp
    // Hard renames (no alias before go-live). Tasks that rename a route add its old pattern here.
    private static readonly string[] RemovedRoutes =
    [
        "GET /admin/payment-methods CanManagePaymentMethods",
        "POST /admin/payment-methods CanManagePaymentMethods",
        "PATCH /admin/payment-methods/{code} CanManagePaymentMethods"
    ];
```

and after `RouteTable_WithTheAppBuilt_ContainsEveryMovedRouteWithItsPolicy`:

```csharp
    [Fact]
    public void RouteTable_WithTheAppBuilt_ContainsNoRemovedRoute()
    {
        var table = DescribeRouteTable();

        table.Should()
             .NotContain(RemovedRoutes);
    }
```

Write this list and test **before** Step 4 when following the steps strictly: it fails until the endpoints change. The class doc comment gains one sentence: "It also pins that each hard-renamed route is gone."

- [ ] **Step 7: Run the tests and watch them pass**

```powershell
dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~PaymentMethodsEndpointTests|FullyQualifiedName~RenamedRouteTests|FullyQualifiedName~RouteTableTests"
dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~StoreHubHttpClientTests"
```

Expected: all PASS (13 + 3 + 2 StoreHub; every `StoreHubHttpClientTests` test, including the 5 new rows).

- [ ] **Step 8: Check no old path is left in code**

Run: `git grep -n "admin/payment-methods" -- src tests`
Expected: only the 3 `RenamedRouteTests` rows.

- [ ] **Step 9: Commit**

```bash
git add src/IndyPOS.StoreHub/Endpoints/PaymentMethods/PaymentMethodsEndpoints.cs src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubHttpClient.cs tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/PaymentMethodsEndpointTests.cs tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/RenamedRouteTests.cs tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/RouteTableTests.cs tests/IndyPOS.Application.Tests/Integration/StoreHub/StoreHubHttpClientTests.cs
git commit -m "feat(storehub)!: one /payment-methods root, ?include=all for the catalogue"
```

---

### Task 2: `POST /sales` returns 201 with its `Location`

**Files:**
- Modify: `src/IndyPOS.StoreHub/Endpoints/Sales/SaleCompletionEndpoints.cs:15,33`
- Modify: `src/IndyPOS.StoreHub/Endpoints/Sales/SalesEndpoints.cs:5-9` (doc comment)
- Modify: `src/IndyPOS.Application/Common/Exceptions/SaleValidationException.cs:5` (comment)
- Modify: `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubHttpClient.cs:237`
- Modify: `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/SalesEndpointTests.cs`, `SalesHistoryEndpointsTests.cs:45`, `PayLaterSaleEndpointTests.cs` (7 sale calls), `ReportsEndpointTests.cs:161`, `RouteTableTests.cs:42`, `RenamedRouteTests.cs`
- Modify: `tests/IndyPOS.Application.Tests/Integration/StoreHub/StoreHubHttpClientTests.cs`, `StoreHubE2ETests.cs:214,218`

**Interfaces:**
- Consumes: Task 1's `RenamedCall_WithTheClient_SendsTheNewRoute` and its `InvokeAsync`/`ResponseFor`; `RenamedRouteTests`; `CompleteSaleResponse(Guid InvoiceId, decimal TotalAmount, DateTime CreatedUtc, long InvoiceNumber)`; `InvoiceDetailDto` (from `GET /sales/{id}`).
- Produces: `POST /sales` → `201 Created`, `Location: /sales/{InvoiceId}`, body `CompleteSaleResponse`.

- [ ] **Step 1: Write the failing tests**

In `SalesEndpointTests.cs`, add after `CompleteSale_WithoutAuth_ReturnsUnauthorized` (it needs `using IndyPOS.Application.UseCases.StoreHub.Sales.History;` if `InvoiceDetailDto` lives there; check with `git grep -n "record InvoiceDetailDto"`):

```csharp
    [Fact]
    public async Task CreateSale_WithAValidSale_ReturnsCreatedWithItsLocation()
    {
        await AuthenticateAsCashierAsync();
        var request = await OneCashLineAsync();

        var response = await Client.PostAsJsonAsync("/sales", request);

        var sale = await response.Content.ReadFromJsonAsync<CompleteSaleResponse>(JsonOptions);
        response.StatusCode.Should()
                           .Be(HttpStatusCode.Created);
        response.Headers.Location!.OriginalString.Should()
                                                 .Be($"/sales/{sale!.InvoiceId}");
    }

    // The Location must name a real resource, readable by the same cashier who rang the sale up.
    [Fact]
    public async Task CreateSale_WithAValidSale_LocationResolvesToTheSale()
    {
        await AuthenticateAsCashierAsync();
        var created = await Client.PostAsJsonAsync("/sales", await OneCashLineAsync());
        var sale = await created.Content.ReadFromJsonAsync<CompleteSaleResponse>(JsonOptions);

        var detail = await Client.GetFromJsonAsync<InvoiceDetailDto>(created.Headers.Location, JsonOptions);

        detail!.Id.Should()
                  .Be(sale!.InvoiceId);
    }

    private async Task<CompleteSaleRequest> OneCashLineAsync()
    {
        var product = await CreateTestProductAsync(unitPrice: 100m, initialStock: 10);
        return new CompleteSaleRequest(
            Lines: [new SaleLineRequest(product.Id, Quantity: 1, UnitPrice: 100m)],
            Payments: [new SalePaymentRequest("Cash", Amount: 100m)]);
    }
```

Check `InvoiceDetailDto`'s id property name with `git grep -n "record InvoiceDetailDto" -A3`; if it is `InvoiceId`, use that in the assertion.

In `RenamedRouteTests.cs`, add:

```csharp
    // Not 404: the /sales group's catch-all GET /sales/{value} still matches this path, so routing
    // answers 405 for the POST. The route is just as gone.
    [Fact]
    public async Task CompleteSale_OnTheOldPath_ReturnsMethodNotAllowed()
    {
        await AuthenticateAsAdminAsync();

        var response = await SendAsync("POST", "/sales/complete");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.MethodNotAllowed);
    }
```

In `StoreHubHttpClientTests.cs`:
- add a row `[InlineData("CompleteSale", "POST", "/sales")]` to `RenamedCall_WithTheClient_SendsTheNewRoute`;
- add an arm `"CompleteSale" => _sut.CompleteSaleAsync(new CompleteSaleRequest([], [])),` to `InvokeAsync`, before the `_` arm;
- add, after `CompleteSaleAsync_WhenAuthenticated_ReturnsSuccess`:

```csharp
    // The till only ever saw a 200 from a sale. A 201 must still be a success, with its body read.
    [Fact]
    public async Task CompleteSaleAsync_WithACreatedResponse_ReturnsTheSale()
    {
        _sut.SetAuthToken("valid-token");
        var invoiceId = Guid.NewGuid();
        SetupMockResponse(HttpStatusCode.Created, new CompleteSaleResponse(invoiceId, 14m, DateTime.UtcNow, 1001));

        var result = await _sut.CompleteSaleAsync(new CompleteSaleRequest([], []));

        result.InvoiceId.Should()
                        .Be(invoiceId);
    }
```

- [ ] **Step 2: Run them and watch them fail**

```powershell
dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~CreateSale_WithAValidSale|FullyQualifiedName~CompleteSale_OnTheOldPath"
dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~RenamedCall_WithTheClient_SendsTheNewRoute|FullyQualifiedName~CompleteSaleAsync_WithACreatedResponse"
```

Expected:
- Both `CreateSale_*` FAIL: `POST /sales` matches no POST route yet, so 405.
- `CompleteSale_OnTheOldPath_ReturnsMethodNotAllowed` FAILS with 200.
- The `CompleteSale` client row FAILS showing `/sales/complete`.
- `CompleteSaleAsync_WithACreatedResponse_ReturnsTheSale` **PASSES already**: the client treats any 2xx as success. It is a guard, not a RED test; say so in the commit body.

- [ ] **Step 3: Move the route**

In `SaleCompletionEndpoints.cs`:
- `app.MapPost("/sales/complete", async (` → `app.MapPost("/sales", async (`
- `return Results.Ok(await handler.HandleAsync(command, cancellationToken));` becomes:

```csharp
                var sale = await handler.HandleAsync(command, cancellationToken);
                return Results.Created($"/sales/{sale.InvoiceId}", sale);
```

In `SalesEndpoints.cs`, the doc comment's `POST /sales/complete is mapped beside the group, not inside it:` becomes `POST /sales is mapped beside the group, not inside it:`. The rest of the sentence stays.

In `SaleValidationException.cs:5`, `/sales/complete answers it with 400.` becomes `POST /sales answers it with 400.`

In `StoreHubHttpClient.cs:237`, `"/sales/complete"` → `"/sales"`.

- [ ] **Step 4: Move every caller test to the new path**

```powershell
$files = 'SalesEndpointTests','SalesHistoryEndpointsTests','PayLaterSaleEndpointTests','ReportsEndpointTests' |
    ForEach-Object { "tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/$_.cs" }
foreach ($f in $files) { (Get-Content $f -Raw) -replace '"/sales/complete"', '"/sales"' | Set-Content $f -NoNewline }
```

Then, in `SalesEndpointTests.cs`, every assertion on a sale POST's status that says `HttpStatusCode.OK` becomes `HttpStatusCode.Created`. At `880f268` they are at lines **117, 151, 178, 205, 228, 250, 272** (find them with `git grep -n "HttpStatusCode.OK" -- tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/SalesEndpointTests.cs`). Tests that use `EnsureSuccessStatusCode()` need no change.

In `RouteTableTests.cs:42`, `"POST /sales/complete CanCompleteSales",` → `"POST /sales CanCompleteSales",`, and add `"POST /sales/complete CanCompleteSales",` to `RemovedRoutes`.

In `StoreHubE2ETests.cs` `SetupCompleteSaleEndpoint`: `.WithPath("/sales/complete")` → `.WithPath("/sales")`, and its `.WithStatusCode(200)` → `.WithStatusCode(201)`, so the E2E sale runs against the server's real status.

- [ ] **Step 5: Run the sale suites and watch them pass**

```powershell
dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~SalesEndpointTests|FullyQualifiedName~SalesHistoryEndpointsTests|FullyQualifiedName~PayLaterSaleEndpointTests|FullyQualifiedName~ReportsEndpointTests|FullyQualifiedName~RenamedRouteTests|FullyQualifiedName~RouteTableTests"
dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~StoreHub"
```

Expected: all PASS.

- [ ] **Step 6: Check no old path is left in code**

Run: `git grep -n "sales/complete" -- src tests`
Expected: only `RenamedRouteTests.CompleteSale_OnTheOldPath_ReturnsMethodNotAllowed`.

- [ ] **Step 7: Commit**

```bash
git add src tests
git commit -m "feat(storehub)!: POST /sales returns 201 with the sale's Location" -m "CompleteSaleAsync_WithACreatedResponse_ReturnsTheSale passed before the change: the client already treats any 2xx as success. It guards that, it was not a RED test. The old path answers 405, not 404: GET /sales/{value} still owns the path."
```

---

### Task 3: `POST /pay-later/{id}/payments`

**Files:**
- Modify: `src/IndyPOS.StoreHub/Endpoints/PayLater/PayLaterEndpoints.cs:56-59`
- Modify: `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubHttpClient.cs:305`
- Modify: `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/PayLaterSaleEndpointTests.cs:134`, `RouteTableTests.cs:51`, `RenamedRouteTests.cs`
- Modify: `tests/IndyPOS.Application.Tests/Integration/StoreHub/StoreHubHttpClientTests.cs`

**Interfaces:**
- Consumes: Task 1's `RenamedRoute_OnTheOldPath_ReturnsNotFound` (its `{id}` placeholder) and `RenamedCall_WithTheClient_SendsTheNewRoute`.
- Produces: `POST /pay-later/{id:guid}/payments`, any authenticated user, body `RecordPaymentRequest(decimal PaymentAmount)`, 200 `PayLaterDto`.

- [ ] **Step 1: Write the failing tests**

`RenamedRouteTests.RenamedRoute_OnTheOldPath_ReturnsNotFound` gains a row:

```csharp
    [InlineData("POST", "/pay-later/{id}/record-payment")]
```

`StoreHubHttpClientTests`:
- add a constant beside `CampaignCode`: `private static readonly Guid KnownId = Guid.Parse("6f1c2a4e-8d3b-4c7a-9e21-5b0d7f3a1c88");`
- add a row `[InlineData("RecordPayLaterPayment", "POST", "/pay-later/6f1c2a4e-8d3b-4c7a-9e21-5b0d7f3a1c88/payments")]`;
- add an arm `"RecordPayLaterPayment" => _sut.RecordPayLaterPaymentAsync(KnownId, 100m),`.

- [ ] **Step 2: Run them and watch them fail**

```powershell
dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~RenamedRouteTests"
dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~RenamedCall_WithTheClient_SendsTheNewRoute"
```

Also add `$"POST /pay-later/{{id:guid}}/record-payment {AnyAuthenticatedUser}",` to `RouteTableTests.RemovedRoutes` (Task 1).

Expected:
- The `record-payment` HTTP row **PASSES already.** `RecordPayLaterPaymentCommandHandler` looks the debt up before it checks the amount, so an unknown id gets the handler's own bodiless 404. The row still earns its place: it proves the path stays a 404 after the rename. Say so in the commit body.
- `RouteTable_WithTheAppBuilt_ContainsNoRemovedRoute` FAILS listing `POST /pay-later/{id:guid}/record-payment`. This is the RED for this task.
- The client row FAILS showing `/record-payment`.

- [ ] **Step 3: Move the route and the client**

- `PayLaterEndpoints.cs:59`: `"/pay-later/{id:guid}/record-payment"` → `"/pay-later/{id:guid}/payments"`. The comment above (`// Record payment against pay-later`) becomes `// Record a repayment against a pay-later debt`.
- `StoreHubHttpClient.cs:305`: `$"/pay-later/{payLaterId}/record-payment"` → `$"/pay-later/{payLaterId}/payments"`.
- `PayLaterSaleEndpointTests.cs:134`: `$"/pay-later/{debt.Id}/record-payment"` → `$"/pay-later/{debt.Id}/payments"`.
- `RouteTableTests.cs:51`: `$"POST /pay-later/{{id:guid}}/record-payment {AnyAuthenticatedUser}"` → `$"POST /pay-later/{{id:guid}}/payments {AnyAuthenticatedUser}"`.

- [ ] **Step 4: Run and watch them pass**

```powershell
dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~PayLaterSaleEndpointTests|FullyQualifiedName~RenamedRouteTests|FullyQualifiedName~RouteTableTests"
dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~StoreHubHttpClientTests"
```

Expected: all PASS.

- [ ] **Step 5: Check, then commit**

Run: `git grep -n "record-payment" -- src tests` → only the `RenamedRouteTests` row.

```bash
git add src tests
git commit -m "feat(storehub)!: POST /pay-later/{id}/payments replaces record-payment"
```

---

### Task 4: `POST /products/{id}/stock-adjustments`

**Files:**
- Modify: `src/IndyPOS.StoreHub/Endpoints/Products/ProductsEndpoints.cs:137-139`
- Modify: `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubHttpClient.cs:210`
- Modify: `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/ProductsEndpointTests.cs` (6 calls), `RouteTableTests.cs:34`, `RenamedRouteTests.cs`
- Modify: `tests/IndyPOS.Application.Tests/Integration/StoreHub/StoreHubHttpClientTests.cs`

**Interfaces:**
- Consumes: as Task 3, plus Task 3's `KnownId`.
- Produces: `POST /products/{id:guid}/stock-adjustments`, `CanAdjustInventory` + `RequireUserIdFilter`, body `AdjustQuantityRequest(int Delta, string? Reason)`, 200 `AdjustQuantityResponse(Guid ProductId, int Quantity)`.

- [ ] **Step 1: Write the failing tests**

`RenamedRouteTests` row:

```csharp
    [InlineData("POST", "/products/{id}/adjust-quantity")]
```

Because the old route answers 400 for the empty body (`Delta` is 0) before the fix, this row goes RED with 400, as it should. Also add `"POST /products/{id:guid}/adjust-quantity CanAdjustInventory",` to `RouteTableTests.RemovedRoutes`.

`StoreHubHttpClientTests`: row `[InlineData("AdjustProductQuantity", "POST", "/products/6f1c2a4e-8d3b-4c7a-9e21-5b0d7f3a1c88/stock-adjustments")]` and arm `"AdjustProductQuantity" => _sut.AdjustProductQuantityAsync(KnownId, new AdjustQuantityRequest(10)),` (add `using IndyPOS.Application.UseCases.StoreHub.Products.AdjustQuantity;` if it is not there).

- [ ] **Step 2: Run them and watch them fail**

```powershell
dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~RenamedRouteTests"
dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~RenamedCall_WithTheClient_SendsTheNewRoute"
```

Expected: the `adjust-quantity` row FAILS with 400; the client row FAILS showing `/adjust-quantity`.

- [ ] **Step 3: Move the route, the client and the callers**

- `ProductsEndpoints.cs:139`: `"/products/{id:guid}/adjust-quantity"` → `"/products/{id:guid}/stock-adjustments"`. The comment above it (`// Adjust product quantity by a signed delta. The adjuster is the token's user, never the body's.`) becomes `// Record a stock adjustment: a signed delta. The adjuster is the token's user, never the body's.`. Keep `MapAdjustQuantity` as the method name.
- `StoreHubHttpClient.cs:210`: `$"/products/{productId}/adjust-quantity"` → `$"/products/{productId}/stock-adjustments"`.
- `ProductsEndpointTests.cs`: `(Get-Content $f -Raw) -replace '/adjust-quantity"', '/stock-adjustments"'` over the file (6 calls, lines 284-388).
- `RouteTableTests.cs:34`: `"POST /products/{id:guid}/adjust-quantity CanAdjustInventory",` → `"POST /products/{id:guid}/stock-adjustments CanAdjustInventory",`.

- [ ] **Step 4: Run and watch them pass**

```powershell
dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~ProductsEndpointTests|FullyQualifiedName~RenamedRouteTests|FullyQualifiedName~RouteTableTests"
dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~StoreHubHttpClientTests"
```

Expected: all PASS, including `AdjustQuantity_WithATokenWithoutAUserId_ReturnsUnauthorized` (the filter moved with the route).

- [ ] **Step 5: Check, then commit**

Run: `git grep -n "adjust-quantity" -- src tests` → only the `RenamedRouteTests` row.

```bash
git add src tests
git commit -m "feat(storehub)!: POST /products/{id}/stock-adjustments replaces adjust-quantity"
```

---

### Task 5: Smoke script, Bruno and docs on the new paths

**Files:**
- Modify: `scripts/smoke-test.ps1:331,349,351,355,399,417,419,423`
- Move/modify: `.bruno/StoreHub/inventory/*` → `.bruno/StoreHub/products/`; `.bruno/StoreHub/sales/complete-sale*.bru`; `.bruno/StoreHub/pay-later/record-payment.bru`
- Create: `.bruno/StoreHub/payment-methods/{list-offerable,list-catalogue,add-campaign,update-method}.bru`
- Modify: `.bruno/StoreHub/environments/local.bru`, `.bruno/README.md`
- Modify: `docs/development/getting-started.md:310`, `docs/diagrams/README.md:42`, `docs/diagrams/architecture-overview.md:55,170,313`, `docs/diagrams/flows.md:314,833-834`

**Interfaces:**
- Consumes: Tasks 1-4's routes.
- Produces: nothing code depends on.

- [ ] **Step 1: Smoke script**

In `scripts/smoke-test.ps1`, every `"/sales/complete"` becomes `"/sales"`, and every test name `"POST /sales/complete"` / `"POST /sales/complete (pay later)"` becomes `"POST /sales"` / `"POST /sales (pay later)"`. `Invoke-RestMethod` treats 201 as success, so nothing else changes.

Run: `git grep -n "sales/complete" -- scripts` → nothing.

- [ ] **Step 2: Bruno — fold `inventory/` into `products/`, rename the verb requests**

```bash
git mv .bruno/StoreHub/inventory/adjust-quantity.bru .bruno/StoreHub/products/create-stock-adjustment.bru
git mv .bruno/StoreHub/inventory/generate-barcode.bru .bruno/StoreHub/products/generate-barcode.bru
git mv .bruno/StoreHub/sales/complete-sale.bru .bruno/StoreHub/sales/create-sale.bru
git mv .bruno/StoreHub/sales/complete-sale-example.bru .bruno/StoreHub/sales/create-sale-example.bru
git mv .bruno/StoreHub/pay-later/record-payment.bru .bruno/StoreHub/pay-later/create-payment.bru
```

Then edit:
- `products/create-stock-adjustment.bru`: `name: Adjust Product Quantity` → `name: Create Stock Adjustment`; `url: …/adjust-quantity` → `url: {{baseUrl}}/products/{{productId}}/stock-adjustments`; the docs heading `# Adjust Product Quantity` → `# Create Stock Adjustment`. Set `seq` to `6`, and `generate-barcode.bru`'s `seq` to `7` (products already uses 1-5).
- `sales/create-sale.bru` and `create-sale-example.bru`: `url: {{baseUrl}}/sales/complete` → `url: {{baseUrl}}/sales`; `name: Complete Sale…` → `name: Create Sale…`; in their `docs` blocks, any "returns 200" for the sale becomes "returns 201 with a `Location: /sales/{id}` header".
- `pay-later/create-payment.bru`: `name: Record Payment` → `name: Create Payment`; `url: …/record-payment` → `url: {{baseUrl}}/pay-later/{{payLaterId}}/payments`.
- `environments/local.bru`: add `paymentMethodCode:` after `payLaterId:`.

Run: `git grep -n "complete\b\|record-payment\|adjust-quantity" -- .bruno` → nothing but prose that is still true.

- [ ] **Step 3: Bruno — the payment-method requests**

Create `.bruno/StoreHub/payment-methods/list-offerable.bru`:

```
meta {
  name: List Offerable Payment Methods
  type: http
  seq: 1
}

get {
  url: {{baseUrl}}/payment-methods
  body: none
  auth: bearer
}

auth:bearer {
  token: {{token}}
}

docs {
  # List Offerable Payment Methods

  The methods this store's till may offer: enabled, and allowed for the store type.

  ## Authorization

  Requires `products.read` (Cashier, Manager, Admin).
}
```

Create `list-catalogue.bru` the same way with `seq: 2`, `name: List Payment Method Catalogue`, `url: {{baseUrl}}/payment-methods?include=all`, and this docs body:

```
  # List Payment Method Catalogue

  Every payment method, enabled or not. `include` accepts only `all`; any other value is a 400.

  ## Authorization

  Requires `payment_methods.manage` (Admin only). Anyone else gets 403.
```

Create `add-campaign.bru` (`seq: 3`, `name: Add Campaign Payment Method`, `post`, `url: {{baseUrl}}/payment-methods`, `body: json`):

```
body:json {
  {
    "code": "{{paymentMethodCode}}",
    "displayName": "คนละครึ่ง",
    "displayOrder": 9
  }
}
```

with docs: "Adds an enabled government-campaign method. 409 if the code already exists. Requires `payment_methods.manage` (Admin only)."

Create `update-method.bru` (`seq: 4`, `name: Update Payment Method`, `patch`, `url: {{baseUrl}}/payment-methods/{{paymentMethodCode}}`, `body: json`):

```
body:json {
  {
    "isEnabled": false
  }
}
```

with docs: "Fields left out are not changed: `isEnabled`, `displayName`, `displayOrder`. 404 for an unknown code. Requires `payment_methods.manage` (Admin only)."

- [ ] **Step 4: Bruno README**

In `.bruno/README.md`:
- Replace the whole tree in "## Structure" with the real one (it already listed a `get-invoices.bru` that never existed). Run `find .bruno -name "*.bru" | sort` and write the tree from that output, in the same `├──`/`└──` style.
- "Quick Start" step 3: `**Complete Sale** → Create a sale` → `**Create Sale** → Ring up a sale`.
- "Environment Variables": add `| \`paymentMethodCode\` | Campaign code for add/update | Manual |`.
- "API Endpoints": the `### Inventory` table goes; its two rows join `### Products` as `| POST | /products/{id}/stock-adjustments | Record a stock adjustment | Manager+ |` and `| POST | /products/next-barcode | Generate barcode | Manager+ |`. In `### Sales`, `| POST | /sales/complete | Complete a sale | Cashier+ |` → `| POST | /sales | Ring up a sale (201 + Location) | Cashier+ |`. In `### Pay Later`, the `record-payment` row → `| POST | /pay-later/{id}/payments | Record a repayment | Cashier+ |`. Add a `### Payment Methods` table:

```
| Method | Endpoint | Description | Role |
|--------|----------|-------------|------|
| GET | /payment-methods | What the till may offer | Cashier+ |
| GET | /payment-methods?include=all | The whole catalogue | Admin |
| POST | /payment-methods | Add a campaign method | Admin |
| PATCH | /payment-methods/{code} | Enable, disable or rename | Admin |
```

- [ ] **Step 5: Docs and diagrams (pad, never redraw)**

- `docs/development/getting-started.md:310`: `curl -X POST http://localhost:5012/sales/complete \` → `curl -X POST http://localhost:5012/sales \`.
- `docs/diagrams/README.md:42`: `| \`/sales/complete\` | POST | Complete a sale |` → `| \`/sales\` | POST | Ring up a sale (201) |`.
- `docs/diagrams/architecture-overview.md` lines 55, 170 and 313, and `docs/diagrams/flows.md` line 314: replace `POST /sales/complete` with `POST /sales` (or `/sales/complete` with `/sales` at 313) and **add 9 spaces right after it**, so the line keeps its exact width.
- `docs/diagrams/flows.md:833`: `POST /products/{id}/adjust-quantity` → `POST /products/{id}/stock-adjustments`, which is 2 characters **longer**: remove 2 of the spaces after it. Line 834 shows the wrong body (`{quantity: 10, …}`; the field is `delta`): change `quantity` to `delta` and add 3 spaces after the closing `}`.

Check the widths in characters (not bytes; the diagrams use Unicode box drawing):

```powershell
foreach ($f in 'architecture-overview','flows') {
    $old = (git show "HEAD:docs/diagrams/$f.md") -split "`n"
    $new = Get-Content "docs/diagrams/$f.md" -Encoding utf8
    for ($i = 0; $i -lt $new.Count; $i++) {
        if ($old[$i].TrimEnd("`r") -ne $new[$i] -and $old[$i].TrimEnd("`r").Length -ne $new[$i].Length) {
            "$f.md:$($i + 1) was $($old[$i].TrimEnd("`r").Length), now $($new[$i].Length)"
        }
    }
}
```

Expected: no output.

- [ ] **Step 6: Check nothing outside history still names an old path**

Run: `git grep -n "sales/complete\|record-payment\|adjust-quantity\|admin/payment-methods" -- . ':!docs/superpowers' ':!.planning' ':!tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/RenamedRouteTests.cs'`
Expected: nothing. (`docs/superpowers` and `.planning` are dated history and stay as written.)

- [ ] **Step 7: Commit**

```bash
git add scripts .bruno docs/development docs/diagrams
git commit -m "docs: smoke script, Bruno and diagrams on the renamed routes"
```

---

### Task 6: `docs/architecture/api-conventions.md`

**Files:**
- Create: `docs/architecture/api-conventions.md`
- Modify: `ONBOARDING.md` (one link, where it lists architecture docs; find with `git grep -n "docs/architecture" ONBOARDING.md`; if there is no such list, add the link under its "Where things live" or equivalent section)

**Interfaces:** none.

- [ ] **Step 1: Write the doc**

```markdown
# StoreHub API conventions

Every StoreHub route follows these rules. A new route that cannot follow one names the rule it
breaks, and why, in a comment beside its `Map…` call.

## Paths

- **One root per resource.** `/payment-methods` serves the till and the admin. A wider view is a
  query, not a second root (`GET /payment-methods?include=all`).
- **Nouns, not verbs.** A command is a sub-resource it creates: `POST /sales`,
  `POST /pay-later/{id}/payments`, `POST /products/{id}/stock-adjustments`,
  `POST /sales/{id}/reprints`.
- **Typed keys share a slot.** `/sales/{id:guid}` and `/sales/{number:long}` sit on the same segment
  and the route constraint picks one.
- **Filters and paging are query parameters.** `GET /sales?from=…&to=…`.
- **`/reports` is for aggregates only.** A list of records is its resource's `GET`, not a report.
- **Left alone on purpose:** `POST /products/next-barcode` advances a counter, so it cannot be a
  safe `GET`, and `POST /barcodes` adds no clarity. `/reports/legacy/*` keeps the WinForms shapes
  until the Avalonia port drops them.

## Who did it

- **The user comes from the token, never the body.** A write that records a user reads
  `GetRequiredUserId()` and adds `RequireUserIdFilter`, so a token without a user id gets 401.

## Responses

- **A refused request** answers 400/404/409 with a Thai `{ "error": "..." }` the cashier can read.
- **A policy-level 401 or 403 has no body.** So does a check run inside a handler with
  `Results.Forbid()`.
- **A new sale** answers 201 with `Location: /sales/{id}`.

## Renaming or removing a route after go-live

Never rename or remove a shipped route in one step:

1. Add the new route.
2. Keep the old one as a deprecated alias for **one release.** It calls the same handler and returns
   a `Deprecation` header.
3. Move the till client to the new route.
4. Remove the alias in the next release.

The reason: the installer upgrades StoreHub before the till and never rolls the till back. A till
that cannot reach its route cannot ring up a sale, and offline-first is priority #1.

Before go-live a hard rename was safe, and the route tidy-up of October 2026 did exactly that. An
old path then answers 404, or 405 where another route still owns the path for a different verb
(`POST /sales/complete`, because of `GET /sales/{value}`).
```

- [ ] **Step 2: Link it**

Add `- [StoreHub API conventions](docs/architecture/api-conventions.md) — route rules, and how to rename a route after go-live.` to the ONBOARDING.md list found above.

- [ ] **Step 3: Commit**

```bash
git add docs/architecture/api-conventions.md ONBOARDING.md
git commit -m "docs: StoreHub API conventions, with the after-go-live rename rule"
```

---

### Task 7: Measure the whole solution, update the counts, and check the till

**Files:**
- Modify: `CLAUDE.md` (the Docker-down comment block and the "Solution suites total" paragraph)
- Modify: `ONBOARDING.md` (Trap 1 text and table; "Expected counts")

**Interfaces:**
- Consumes: every earlier task.
- Produces: measured counts in the two docs.

- [ ] **Step 1: Run the whole solution with TRX output**

With Docker running, and the real store databases present if you have them:

```powershell
$trx = "$env:TEMP\indypos-route-tidy-b-trx"
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
dotnet test tests/IndyPOS.Bootstrapper.Tests
```

Expected: `Failed` is 0 for every suite, and the exit code is 0. A flake must be named from its `.trx` message, not re-rolled (CLAUDE.md). The measured numbers go in the docs. For a cross-check, this plan expects:

| Suite | Baseline | Added here | Expected |
|---|---|---|---|
| Application | 565 | +5 client rows (T1), +1 row and +1 201 guard (T2), +1 row (T3), +1 row (T4) | **574** |
| StoreHub.IntegrationTests | 267 | +13 payment methods, +3 old-path rows, +1 removed-route pin (T1); +2 `CreateSale`, +1 old path (T2); +1 row (T3); +1 row (T4) | **289** |
| Others (Domain 56, Vault 17, CloudApi 6, CloudApi.IntegrationTests 49, MigrationTool 145, Windows.Forms 47) | — | — | unchanged |
| **Total** | **1152** (1151 pass, 1 skip) | +31 | **1183** (1182 pass, 1 skip) |
| Installer (not in the solution) | 231 (223 pass, 8 skip) | — | unchanged |

If a measured number differs, find out why before writing it down.

- [ ] **Step 2: Update `CLAUDE.md`**

- "Solution suites total **1152** … measured 2026-10-02 (after route tidy-up A …)" becomes the measured total, its pass/skip split and today's date, "after route tidy-up B: the hard renames". Keep "1152 on 2026-10-02" in the history list.
- Per suite: the measured Application and StoreHub.IntegrationTests numbers.
- The growth sentence becomes route tidy-up B's own tests: Application +9 and StoreHub.IntegrationTests +22, or whatever was measured.
- "Without the real store databases the suite discovers **1132** (1152 − 20, measured by CI's first run …)" becomes measured − 20, marked as **derived** until this PR's CI run reports it; then replace it with CI's number and say "measured by CI".
- Docker-down block: StoreHub's 259 becomes measured StoreHub − 8 (expected 281), the total 390 becomes 281 + 82 + 49 (expected 412), with one sentence: "Route tidy-up B (2026-10-06) added 22 StoreHub tests, all needing a container -- DERIVED, not re-measured with Docker down."

- [ ] **Step 3: Update `ONBOARDING.md`**

The same figures in Trap 1 (text and table) and "Expected counts", with the old figures moved into the "supersedes" list.

Run: `git grep -nE "1152|1183|1132|1163|390|412" -- CLAUDE.md ONBOARDING.md`
Expected: every current figure is the new one; old figures appear only in history lists; Docker-down and no-store-DB figures say derived or measured-by-CI truthfully.

- [ ] **Step 4: Commit**

```bash
git add CLAUDE.md ONBOARDING.md
git commit -m "docs: measured test counts after the route tidy-up PR B"
```

- [ ] **Step 5: Smoke on a running stack (Pond, or the implementer with Docker)**

```powershell
dotnet run --project src/IndyPOS.AppHost --launch-profile https
# in another shell, once storehub-api is healthy:
pwsh scripts/smoke-test.ps1
```

Expected: every `POST /sales` result passes.

Then, at the WinForms till against the same stack (spec §9: "The till rings up a sale, adjusts stock, records a repayment and manages payment methods on the new paths"):
1. Sign in as `cashier`, ring up a cash sale. It completes and prints.
2. Sign in as `manager`, adjust a product's stock by +1. The new balance shows.
3. Ring up a PayLater (ลงบัญชี) sale on a GeneralHardware config, then record a repayment against it. The debt's paid amount rises.
4. Sign in as `admin`, open payment-method management. The whole catalogue lists, including a disabled method; disable and re-enable a campaign method.

Record the results in the PR body. This step is not code, so it has no commit.

---

## Self-Review

**Spec coverage** (spec § → task):
- §4 table, `POST /sales` 201 + `Location`: Task 2.
- §4 table, `/pay-later/{id}/payments`: Task 3. `/products/{id}/stock-adjustments`: Task 4.
- §4 table, the three `/admin/payment-methods` routes onto `/payment-methods`: Task 1.
- §4 "`GET /payment-methods` with no `include` stays …", "any other value … Thai 400": Task 1 (`…WithoutInclude…`, `…WithAnUnknownInclude_*`).
- §4 "Every old path returns 404, one test per route": `RenamedRouteTests`, one row or fact per route (Tasks 1-4). `POST /sales/complete` answers 405 (decision 1).
- §4 "The till client moves … The E2E tests follow": every task moves its client call; Task 2 moves the E2E WireMock path and status.
- §4 "Updated in the same PR: the smoke script, Bruno (requests, folder and README), getting-started and the diagrams": Task 5.
- §4 / §6 `api-conventions.md` with every listed rule and the after-go-live rule: Task 6.
- §8 PR B test names: `CreateSale_WithAValidSale_ReturnsCreatedWithItsLocation` (T2), `ListPaymentMethods_WithIncludeAllAsCashier_ReturnsForbidden` (T1), `…AsManager_ReturnsTheWholeCatalogue` → `…AsAdmin_…` (T1, decision 2), `…WithAnUnknownInclude_ReturnsBadRequest` (T1), `…WithoutInclude_ReturnsTheOfferableList` (T1), "client E2E tests pass on the new paths" (T2), "counts measured" (T7). The `…_OnTheNewPath_…` behaviour is carried by the existing endpoint tests, which now call the new paths unedited apart from the URL and the sale's 201.
- §9: `Program.cs` already holds wiring only (PR A); "the till rings up a sale …" is Task 7 Step 5; the other two success lines belong to PR A.

**Known weak spot, handled:** Task 3's old-path HTTP row passes before the fix, because the repayment handler answers its own 404 for an unknown debt id. `RouteTable_WithTheAppBuilt_ContainsNoRemovedRoute` is the RED for every rename instead, and the HTTP rows prove what a caller then sees.

**Placeholders:** none. The two "check the property name" notes (Task 2, `InvoiceDetailDto`) name the exact command to run.

**Type consistency:** `RenamedRoute_OnTheOldPath_ReturnsNotFound(string method, string path)`, `RenamedCall_WithTheClient_SendsTheNewRoute(string call, string expectedMethod, string expectedPathAndQuery)`, `InvokeAsync(string call)`, `ResponseFor(string call)`, `CaptureRequest(string responseJson)`, `KnownId`, `CampaignCode` and `AnyId` are used with the same names and types in every task.
