# Invoice History, Till Plan B — Presentation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The till can list, find, open and reprint bills through two unit-tested ViewModels in a new `IndyPOS.Presentation` project, with StoreHub's Thai error messages reaching the user. No screens yet.

**Architecture:** `IStoreHubClient` gains the four bill calls the server has served since plan 1. A failed StoreHub call surfaces the server's own Thai `{ error }`, as a new Application-level `StoreHubException`, so a ViewModel can catch it without referencing Infrastructure. `IndyPOS.Presentation` (net10.0, CommunityToolkit.Mvvm, references Application only) holds `SalesHistoryAccess` (what the logged-in user may do), `StoreClock` (store-local "today" and times), `InvoiceDetailViewModel` (one bill, reprint) and `InvoiceHistoryViewModel` (list, periods, paging, bill-number search, latest request wins).

**Tech Stack:** .NET 10, CommunityToolkit.Mvvm 8.4.2, xUnit 2.9.3, Moq 4.20.72, FluentAssertions 8.8.0, Microsoft.Extensions.TimeProvider.Testing 10.10.0.

**Spec:** `docs/superpowers/specs/2026-09-27-invoice-history-v4-design.md` §5 (permissions), §6 (API), §7 (till ViewModels), §10 (tests). Plan 3 is split into three PRs: A — receipts (merged, #127), **B — Presentation (this plan)**, C — screens + stub removal.

## Global Constraints

- `IndyPOS.Presentation` is net10.0 with `CommunityToolkit.Mvvm`, references **Application only**, and has **no WinForms reference** (spec §7).
- Tests live in a new `tests/IndyPOS.Presentation.Tests`, faking `IStoreHubClient` (spec §7).
- Client methods `ListSalesAsync(from, to, page)`, `GetSaleAsync(Guid)`, `GetSaleByNumberAsync(long)`, `CreateReprintAsync(Guid)`, using the existing `SendAuthenticatedAsync` pattern, tested in `StoreHubHttpClientTests` (spec §7).
- Routes: `GET /sales?from=&to=&page=&pageSize=`, `GET /sales/{id:guid}`, `GET /sales/{number:long}`, `POST /sales/{id:guid}/reprints` (spec §6). Dates are `yyyy-MM-dd`, invariant culture (a th-TH till would send 2569).
- Page size 50; "load more" appends (spec §7).
- **History mode** (`reports.view`): Today / This Month / This Year / a date range. **Today-only mode** (`sales.reprint` alone): no period picker (spec §7).
- Bill-number search: non-numeric → Thai error; not found → `ไม่พบบิลเลขที่ …`; found → opens its detail directly (spec §7).
- **Latest request wins:** changing period or search cancels the in-flight load, so an older response never overwrites a newer list (spec §7).
- **No optimistic UI:** `IsBusy` and `ErrorMessage`; nothing changes until the server answers (spec §7).
- Detail times shown in **Bangkok time**; the server sends UTC (spec §7).
- The Hardware / General subtotal split moves into the detail ViewModel (spec §7).
- `ReprintCommand`: enabled with `sales.reprint` within its date rule; calls `CreateReprintAsync`; builds a copy receipt (`ReceiptDocumentFactory.ForReprint`) and prints it; on failure shows a Thai error and prints nothing (spec §7). The reprinter's name is the logged-in user's (plan 1, D9).
- "Today" is the store's business date: plain midnight in the store time zone (`TodayOnlyRule.BusinessDateOf`, spec §5).
- Tests: `Subject_WhenScenario_DirectVerbOutcome`, negative cases first, one behaviour per test, Arrange-Act-Assert separated by blank lines, FluentAssertions with vertically aligned chains.

## Review Focus

1. **A slow detail load answering after a faster, later one** (a manager clicks two bills quickly). The later bill stays shown. Pinned in Task 4.
2. **A date range whose start is after its end.** A Thai error, and no call to StoreHub. Pinned in Task 5.
3. **A bill number typed with spaces, a sign or digit grouping** (`" 1042 "`, `"-5"`, `"1,042"`). Spaces around it are trimmed; a sign or separator is not a number. Pinned in Task 5.
4. **The printer failing after the reprint was recorded.** The user is told the reprint was recorded but did not print, so they know a retry writes a second record. Pinned in Task 4.
5. **A StoreHub error body that is not the `{ error }` shape** (a proxy's HTML page, an empty 500). The client falls back to its status message instead of throwing a JSON error. Pinned in Task 1.

## Not in this plan (plan C)

- The WinForms views, DI registration of the ViewModels, the บิลวันนี้ button, the PayLater popup, removing the remaining `IReportService` stubs.
- **The RELEASE print gate.** `InvoiceDetailViewModel` calls `IReceiptPrinterService` directly. Today the gate is `[Conditional("RELEASE")]` in the forms, so plan C must keep a DEBUG till from printing to a missing XP-58: either register a no-op `IReceiptPrinterService` in DEBUG or gate in the view. Nothing in plan B is wired to a screen, so nothing prints yet.

---

## File Structure

| File | Responsibility |
|---|---|
| Create `src/IndyPOS.Application/Common/Exceptions/StoreHubException.cs` | A StoreHub call failed; the message is safe to show the user |
| Modify `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubClientException.cs` | Derives from `StoreHubException` |
| Modify `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubHttpClient.cs` | Server's `{ error }` as the message; the four bill calls; a GET that maps 404 to null |
| Modify `src/IndyPOS.Application/Abstractions/StoreHub/IStoreHubClient.cs` | The four bill calls |
| Create `src/IndyPOS.Presentation/IndyPOS.Presentation.csproj` | New project |
| Create `src/IndyPOS.Presentation/SalesHistory/SalesHistoryAccess.cs` | What the logged-in user may do with bills |
| Create `src/IndyPOS.Presentation/SalesHistory/StoreClock.cs` | Store-local today and times |
| Create `src/IndyPOS.Presentation/SalesHistory/InvoiceDetailViewModel.cs` | One bill: times, split, reprint |
| Create `src/IndyPOS.Presentation/SalesHistory/InvoiceHistoryViewModel.cs` | List, periods, paging, search; also `InvoiceRow`, `DateRange` |
| Create `tests/IndyPOS.Presentation.Tests/IndyPOS.Presentation.Tests.csproj` + test files | One test class per type |
| Modify `IndyPOS.sln` | Both projects |

**Branch:** `feat/sales-history-viewmodels` from `development`.

---

### Task 1: StoreHub's Thai error reaches the caller, as an Application exception

Today `EnsureSuccessfulResponseAsync` (`StoreHubHttpClient.cs`, near line 538) logs the server's body and throws an English message, so every Thai `{ error }` StoreHub writes is lost: a cashier asking for yesterday's bill would read "You do not have permission…" instead of the server's reason.

**Files:**
- Create: `src/IndyPOS.Application/Common/Exceptions/StoreHubException.cs`
- Modify: `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubClientException.cs`
- Modify: `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubHttpClient.cs` (`EnsureSuccessfulResponseAsync` + a new private `ServerError`)
- Test: `tests/IndyPOS.Application.Tests/Integration/StoreHub/StoreHubHttpClientTests.cs`

**Interfaces:**
- Produces: `class StoreHubException : Exception` in `IndyPOS.Application.Common.Exceptions`, constructors `(string message)` and `(string message, Exception innerException)`. `StoreHubClientException : StoreHubException` keeps both constructors and its namespace, so every existing `catch (StoreHubClientException)` still works.

- [ ] **Step 1: Write the failing tests**

Add to `StoreHubHttpClientTests` (next to `GetLegacySalesSummaryAsync_WhenForbidden_ThrowsPermissionError`), plus the raw-response helper beside `SetupMockResponse<T>`:

```csharp
    private const string ServerThaiError = "ดูได้เฉพาะบิลของวันนี้";

    [Fact]
    public async Task GetSalesSummaryAsync_WhenTheErrorBodyIsNotJson_ThrowsTheStatusMessage()
    {
        _sut.SetAuthToken("valid-token");
        SetupRawResponse(HttpStatusCode.BadGateway, "<html>proxy error</html>");

        var act = () => _sut.GetSalesSummaryAsync(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 15));

        (await act.Should()
                  .ThrowAsync<StoreHubClientException>()).Which.Message.Should()
                                                           .Contain("502");
    }

    [Fact]
    public async Task GetSalesSummaryAsync_WhenTheServerSendsAnError_ThrowsItsMessage()
    {
        _sut.SetAuthToken("valid-token");
        SetupMockResponse(HttpStatusCode.Forbidden, new { error = ServerThaiError });

        var act = () => _sut.GetSalesSummaryAsync(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 15));

        (await act.Should()
                  .ThrowAsync<StoreHubClientException>()).Which.Message.Should()
                                                           .Be(ServerThaiError);
    }

    [Fact]
    public async Task GetSalesSummaryAsync_WhenTheServerSendsAnError_ThrowsAnApplicationStoreHubException()
    {
        _sut.SetAuthToken("valid-token");
        SetupMockResponse(HttpStatusCode.BadRequest, new { error = ServerThaiError });

        var act = () => _sut.GetSalesSummaryAsync(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 15));

        await act.Should()
                 .ThrowAsync<IndyPOS.Application.Common.Exceptions.StoreHubException>();
    }

    private void SetupRawResponse(HttpStatusCode statusCode, string body)
    {
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "text/html")
            });
    }
```

- [ ] **Step 2: Run them to see them fail**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~StoreHubHttpClientTests"`
Expected: build FAILS — `IndyPOS.Application.Common.Exceptions.StoreHubException` does not exist. Then, after Step 3's first file only, `..._ThrowsItsMessage` FAILS (message is the English permission text) and `..._ThrowsAnApplicationStoreHubException` FAILS (wrong type). `..._ThrowsTheStatusMessage` may already pass: it pins the fallback.

- [ ] **Step 3: Implement**

`src/IndyPOS.Application/Common/Exceptions/StoreHubException.cs`:

```csharp
namespace IndyPOS.Application.Common.Exceptions;

/// <summary>
/// A StoreHub call failed. The message is safe to show the user: StoreHub's own Thai reason when it
/// sent one. Declared here so a ViewModel can catch it without referencing Infrastructure.
/// </summary>
public class StoreHubException : Exception
{
    public StoreHubException(string message) : base(message) { }

    public StoreHubException(string message, Exception innerException) : base(message, innerException) { }
}
```

`StoreHubClientException.cs` becomes:

```csharp
using IndyPOS.Application.Common.Exceptions;

namespace IndyPOS.Infrastructure.Services.StoreHub;

/// <summary>
/// Exception thrown when StoreHub API communication fails.
/// </summary>
public class StoreHubClientException : StoreHubException
{
    public StoreHubClientException(string message) : base(message) { }
    public StoreHubClientException(string message, Exception innerException) : base(message, innerException) { }
}
```

In `StoreHubHttpClient.EnsureSuccessfulResponseAsync`, replace `var message = response.StatusCode switch` with `var message = ServerError(responseBody) ?? response.StatusCode switch`, and add below `ReadResponseBodyAsync`:

```csharp
    /// <summary>
    /// StoreHub's Thai reason, from the <c>{ error }</c> body every route writes; null for any other
    /// body (a proxy page, an empty 500), so the status message stands in.
    /// </summary>
    private static string? ServerError(string? responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
            return null;

        try
        {
            using var document = JsonDocument.Parse(responseBody);

            return document.RootElement.ValueKind == JsonValueKind.Object
                   && document.RootElement.TryGetProperty("error", out var error)
                   && error.ValueKind == JsonValueKind.String
                   && !string.IsNullOrWhiteSpace(error.GetString())
                ? error.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
```

Add `using System.Text.Json;` if the file does not already have it.

- [ ] **Step 4: Run the client tests**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~StoreHubHttpClientTests"`
Expected: PASS, including the existing `..._WhenForbidden_ThrowsPermissionError` and `..._WhenUnauthorized_...` tests (their `{}` bodies carry no `error`, so the fallback still applies).

- [ ] **Step 5: Commit**

```bash
git add src/IndyPOS.Application/Common/Exceptions/StoreHubException.cs src/IndyPOS.Infrastructure/Services/StoreHub tests/IndyPOS.Application.Tests/Integration/StoreHub/StoreHubHttpClientTests.cs
git commit -m "feat(till): a failed StoreHub call carries the server's Thai reason, as an Application exception"
```

---

### Task 2: The four bill calls on `IStoreHubClient`

Plan 1 said plan 2 would add these; it never did, so the till still cannot reach `/sales`.

**Files:**
- Modify: `src/IndyPOS.Application/Abstractions/StoreHub/IStoreHubClient.cs` (beside `ListSaleLinesAsync`, about line 173)
- Modify: `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubHttpClient.cs` (beside `ListSaleLinesAsync`, about line 415; new private helper under `#region Private Helpers`)
- Test: `tests/IndyPOS.Application.Tests/Integration/StoreHub/StoreHubHttpClientTests.cs`

**Interfaces:**
- Consumes: `SalesPage`, `InvoiceDetailDto` (`IndyPOS.Application.UseCases.StoreHub.Sales.History`), `InvoiceReprintResultDto` (`...Sales.Reprints`).
- Produces (on `IStoreHubClient`):
  - `Task<SalesPage> ListSalesAsync(DateOnly fromDate, DateOnly toDate, int page, int pageSize, CancellationToken cancellationToken = default)`
  - `Task<InvoiceDetailDto?> GetSaleAsync(Guid saleId, CancellationToken cancellationToken = default)` — null when StoreHub answers 404
  - `Task<InvoiceDetailDto?> GetSaleByNumberAsync(long invoiceNumber, CancellationToken cancellationToken = default)` — null when StoreHub answers 404
  - `Task<InvoiceReprintResultDto> CreateReprintAsync(Guid saleId, CancellationToken cancellationToken = default)`
- `ListSalesAsync` takes four parameters, above the ≤3 guideline, to mirror its sibling `ListSaleLinesAsync` exactly.
- 404 is "not found" (an unknown bill, or another day's bill for a today-only caller — spec §5 hides which), a normal outcome rather than an exception; every other failure throws `StoreHubClientException`.

- [ ] **Step 1: Write the failing tests**

In `StoreHubHttpClientTests`:

1. Add four rows to `RenamedCall_WithTheClient_SendsTheNewRoute`:

```csharp
    [InlineData("ListSales", "GET", "/sales?from=2026-10-01&to=2026-10-15&page=2&pageSize=50")]
    [InlineData("GetSale", "GET", "/sales/6f1c2a4e-8d3b-4c7a-9e21-5b0d7f3a1c88")]
    [InlineData("GetSaleByNumber", "GET", "/sales/1042")]
    [InlineData("CreateReprint", "POST", "/sales/6f1c2a4e-8d3b-4c7a-9e21-5b0d7f3a1c88/reprints")]
```

2. Add four arms to `InvokeAsync`:

```csharp
        "ListSales" => _sut.ListSalesAsync(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 15), 2, 50),
        "GetSale" => _sut.GetSaleAsync(KnownId),
        "GetSaleByNumber" => _sut.GetSaleByNumberAsync(1042),
        "CreateReprint" => _sut.CreateReprintAsync(KnownId),
```

3. `ResponseFor` returns `"[]"` for every call starting with `Get`, which cannot bind to a bill object. Replace it with:

```csharp
    // The payment-method lists are arrays; every other call answers with an object.
    private static string ResponseFor(string call) =>
        call is "GetOfferablePaymentMethods" or "GetAllPaymentMethods" ? "[]" : "{}";
```

4. Add:

```csharp
    [Fact]
    public async Task GetSaleByNumberAsync_WhenStoreHubAnswersNotFound_ReturnsNull()
    {
        _sut.SetAuthToken("valid-token");
        SetupMockResponse(HttpStatusCode.NotFound, new { error = "ไม่พบบิล" });

        var bill = await _sut.GetSaleByNumberAsync(1042);

        bill.Should()
            .BeNull();
    }

    [Fact]
    public async Task GetSaleAsync_WhenStoreHubAnswersNotFound_ReturnsNull()
    {
        _sut.SetAuthToken("valid-token");
        SetupMockResponse(HttpStatusCode.NotFound, new { error = "ไม่พบบิล" });

        var bill = await _sut.GetSaleAsync(KnownId);

        bill.Should()
            .BeNull();
    }

    [Fact]
    public async Task GetSaleAsync_WhenForbidden_ThrowsTheServersReason()
    {
        _sut.SetAuthToken("valid-token");
        SetupMockResponse(HttpStatusCode.Forbidden, new { error = ServerThaiError });

        var act = () => _sut.GetSaleAsync(KnownId);

        (await act.Should()
                  .ThrowAsync<StoreHubClientException>()).Which.Message.Should()
                                                           .Be(ServerThaiError);
    }

    [Fact]
    public async Task GetSaleAsync_WithoutLoggingIn_ThrowsNotAuthenticated()
    {
        var act = () => _sut.GetSaleAsync(KnownId);

        await act.Should()
                 .ThrowAsync<StoreHubClientException>();
    }

    [Fact]
    public async Task ListSalesAsync_OnAThaiTill_SendsGregorianDates()
    {
        _sut.SetAuthToken("valid-token");
        var uri = CaptureRequestUri(new SalesPage([], 1, 50, false));

        await RunUnderThaiCultureAsync(() => _sut.ListSalesAsync(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 15), 1, 50));

        uri().Should()
             .Contain("from=2026-10-01&to=2026-10-15");
    }
```

Add `using IndyPOS.Application.UseCases.StoreHub.Sales.Reprints;` if the reprint DTO is referenced. Check `CaptureRequestUri<T>`'s return value is a path-and-query string (`grep -n "CaptureRequestUri" -A20` in the test file) before relying on `Contain`.

- [ ] **Step 2: Run them to see them fail**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~StoreHubHttpClientTests"`
Expected: build FAILS — `StoreHubHttpClient` has no `ListSalesAsync`, `GetSaleAsync`, `GetSaleByNumberAsync`, `CreateReprintAsync`.

- [ ] **Step 3: Implement**

In `IStoreHubClient.cs`, after `ListSaleLinesAsync` (add the `using`s for `...Sales.History` and `...Sales.Reprints` if absent):

```csharp
    /// <summary>
    /// GET /sales — bills newest first. A caller without reports.view may only ask for today.
    /// Requires authentication.
    /// </summary>
    Task<SalesPage> ListSalesAsync(DateOnly fromDate, DateOnly toDate, int page, int pageSize,
                                   CancellationToken cancellationToken = default);

    /// <summary>
    /// GET /sales/{id}. Null when StoreHub answers 404: an unknown bill, or another day's bill for a
    /// today-only caller (the server does not say which). Requires authentication.
    /// </summary>
    Task<InvoiceDetailDto?> GetSaleAsync(Guid saleId, CancellationToken cancellationToken = default);

    /// <summary>GET /sales/{number}: the bill a receipt names. Null on 404, as <see cref="GetSaleAsync"/>.</summary>
    Task<InvoiceDetailDto?> GetSaleByNumberAsync(long invoiceNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// POST /sales/{id}/reprints — records the reprint and returns the bill to print as a copy.
    /// Requires authentication.
    /// </summary>
    Task<InvoiceReprintResultDto> CreateReprintAsync(Guid saleId, CancellationToken cancellationToken = default);
```

In `StoreHubHttpClient.cs`, after `ListSaleLinesAsync`:

```csharp
    public Task<SalesPage> ListSalesAsync(DateOnly fromDate, DateOnly toDate, int page, int pageSize,
                                          CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync<SalesPage>(HttpMethod.Get,
            $"/sales?from={IsoDate(fromDate)}&to={IsoDate(toDate)}&page={page}&pageSize={pageSize}",
            content: null, cancellationToken);

    public Task<InvoiceDetailDto?> GetSaleAsync(Guid saleId, CancellationToken cancellationToken = default) =>
        GetAuthenticatedOrNotFoundAsync<InvoiceDetailDto>($"/sales/{saleId}", cancellationToken);

    public Task<InvoiceDetailDto?> GetSaleByNumberAsync(long invoiceNumber, CancellationToken cancellationToken = default) =>
        GetAuthenticatedOrNotFoundAsync<InvoiceDetailDto>(
            $"/sales/{invoiceNumber.ToString(CultureInfo.InvariantCulture)}", cancellationToken);

    public Task<InvoiceReprintResultDto> CreateReprintAsync(Guid saleId, CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync<InvoiceReprintResultDto>(HttpMethod.Post, $"/sales/{saleId}/reprints",
                                                        content: null, cancellationToken);
```

Under `#region Private Helpers`, after the generic `SendAuthenticatedAsync<T>`:

```csharp
    /// <summary>
    /// An authenticated GET where 404 means "no such thing" rather than a failure.
    /// </summary>
    private async Task<T?> GetAuthenticatedOrNotFoundAsync<T>(string url, CancellationToken cancellationToken)
        where T : class
    {
        EnsureAuthenticated();

        try
        {
            using var request = CreateAuthenticatedRequest(HttpMethod.Get, url);
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            await EnsureSuccessfulResponseAsync(response, HttpMethod.Get, url, cancellationToken);

            return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
                   ?? throw new StoreHubClientException("Invalid response from StoreHub");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed: GET {Url}", url);
            throw new StoreHubClientException("Cannot connect to StoreHub", ex);
        }
    }
```

Every existing `Mock<IStoreHubClient>` keeps compiling: Moq implements new members.

- [ ] **Step 4: Run the client tests**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~StoreHubHttpClientTests"`
Expected: PASS, the theory now running 12 rows.

- [ ] **Step 5: Commit**

```bash
git add src/IndyPOS.Application/Abstractions/StoreHub/IStoreHubClient.cs src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubHttpClient.cs tests/IndyPOS.Application.Tests/Integration/StoreHub/StoreHubHttpClientTests.cs
git commit -m "feat(till): the StoreHub client lists, opens, finds and reprints bills"
```

---

### Task 3: `IndyPOS.Presentation`, `SalesHistoryAccess` and `StoreClock`

**Files:**
- Create: `src/IndyPOS.Presentation/IndyPOS.Presentation.csproj`
- Create: `src/IndyPOS.Presentation/SalesHistory/SalesHistoryAccess.cs`
- Create: `src/IndyPOS.Presentation/SalesHistory/StoreClock.cs`
- Create: `tests/IndyPOS.Presentation.Tests/IndyPOS.Presentation.Tests.csproj`
- Create: `tests/IndyPOS.Presentation.Tests/SalesHistory/SalesHistoryAccessTests.cs`, `StoreClockTests.cs`
- Modify: `IndyPOS.sln`

**Interfaces:**
- Consumes: `ILoggedInUser` (`IndyPOS.Application.Common.Interfaces`: `UserId`, `FirstName`, `LastName`, `int RoleId`), `RoleCapabilities.HasCapability(int roleId, string capability)` and `Capability.ReportsView` / `Capability.SalesReprint` (`IndyPOS.Application.Common.Authorization`), `TodayOnlyRule.BusinessDateOf`.
- Produces (namespace `IndyPOS.Presentation.SalesHistory`):
  - `sealed record SalesHistoryAccess(string UserDisplayName, bool CanViewAnyDay, bool CanReprint)` with `bool IsTodayOnly => !CanViewAnyDay` and `static SalesHistoryAccess For(ILoggedInUser user)`
  - `sealed class StoreClock(TimeProvider time, TimeZoneInfo storeTimeZone)` with `DateOnly Today`, `DateTime ToStoreTime(DateTime utc)`, `DateOnly BusinessDateOf(DateTime utc)`

- [ ] **Step 1: Create the two projects**

`src/IndyPOS.Presentation/IndyPOS.Presentation.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <!-- ViewModels for the till. No WinForms reference: the Avalonia port reuses them. -->
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <PlatformTarget>x64</PlatformTarget>
    <Platforms>x64</Platforms>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.2" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\IndyPOS.Application\IndyPOS.Application.csproj" />
  </ItemGroup>
</Project>
```

`tests/IndyPOS.Presentation.Tests/IndyPOS.Presentation.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <PlatformTarget>x64</PlatformTarget>
    <Platforms>x64</Platforms>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="FluentAssertions" Version="8.8.0" />
    <PackageReference Include="Microsoft.Extensions.TimeProvider.Testing" Version="10.10.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.3.0" />
    <PackageReference Include="Moq" Version="4.20.72" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\IndyPOS.Presentation\IndyPOS.Presentation.csproj" />
  </ItemGroup>
</Project>
```

Add both to the solution, in the existing solution folders (`Core` for src, `Tests\Core` for tests):

```bash
dotnet sln IndyPOS.sln add src/IndyPOS.Presentation/IndyPOS.Presentation.csproj --solution-folder Core
dotnet sln IndyPOS.sln add tests/IndyPOS.Presentation.Tests/IndyPOS.Presentation.Tests.csproj --solution-folder Tests/Core
```

If `--solution-folder` creates a new top-level folder instead of nesting under the existing `Tests` → `Core`, move the project's `NestedProjects` line to the existing `Core` test folder GUID (`{A1B2C3D4-6666-6666-6666-000000000061}`) by hand, and delete the stray folder. Then run `dotnet build` and confirm both projects build.

- [ ] **Step 2: Write the failing tests**

`tests/IndyPOS.Presentation.Tests/SalesHistory/SalesHistoryAccessTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Presentation.SalesHistory;
using Moq;
using Xunit;

namespace IndyPOS.Presentation.Tests.SalesHistory;

public class SalesHistoryAccessTests
{
    private const int UnknownRole = 0;

    [Fact]
    public void For_WithAnUnknownRole_GrantsNothing()
    {
        var access = SalesHistoryAccess.For(User(UnknownRole));

        access.Should()
              .BeEquivalentTo(new { CanViewAnyDay = false, CanReprint = false });
    }

    [Fact]
    public void For_WithNoLastName_TrimsTheDisplayName()
    {
        var access = SalesHistoryAccess.For(User((int)UserRole.Cashier, lastName: ""));

        access.UserDisplayName.Should()
                              .Be("Malee");
    }

    [Fact]
    public void For_WithACashier_IsHeldToToday()
    {
        var access = SalesHistoryAccess.For(User((int)UserRole.Cashier));

        access.IsTodayOnly.Should()
                          .BeTrue();
    }

    [Fact]
    public void For_WithACashier_MayReprint()
    {
        var access = SalesHistoryAccess.For(User((int)UserRole.Cashier));

        access.CanReprint.Should()
                         .BeTrue();
    }

    [Fact]
    public void For_WithAStoreManager_MayViewAnyDay()
    {
        var access = SalesHistoryAccess.For(User((int)UserRole.StoreManager));

        access.CanViewAnyDay.Should()
                            .BeTrue();
    }

    private static ILoggedInUser User(int roleId, string lastName = "Cashier")
    {
        var user = new Mock<ILoggedInUser>();
        user.SetupGet(u => u.FirstName).Returns("Malee");
        user.SetupGet(u => u.LastName).Returns(lastName);
        user.SetupGet(u => u.RoleId).Returns(roleId);

        return user.Object;
    }
}
```

`tests/IndyPOS.Presentation.Tests/SalesHistory/StoreClockTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Presentation.SalesHistory;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace IndyPOS.Presentation.Tests.SalesHistory;

public class StoreClockTests
{
    // 23:30 UTC on the 14th is 06:30 on the 15th in Bangkok (UTC+7).
    private static readonly DateTimeOffset LateEveningUtc = new(2026, 10, 14, 23, 30, 0, TimeSpan.Zero);
    private static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    [Fact]
    public void Today_AfterMidnightInBangkokButNotInUtc_IsTheBangkokDate()
    {
        var clock = new StoreClock(new FakeTimeProvider(LateEveningUtc), Bangkok);

        clock.Today.Should()
                   .Be(new DateOnly(2026, 10, 15));
    }

    [Fact]
    public void ToStoreTime_WithAnUnspecifiedKind_ReadsItAsUtc()
    {
        var clock = new StoreClock(new FakeTimeProvider(LateEveningUtc), Bangkok);

        var local = clock.ToStoreTime(new DateTime(2026, 10, 14, 23, 30, 0, DateTimeKind.Unspecified));

        local.Should()
             .Be(new DateTime(2026, 10, 15, 6, 30, 0));
    }

    [Fact]
    public void BusinessDateOf_WithALateEveningUtcSale_IsTheNextDay()
    {
        var clock = new StoreClock(new FakeTimeProvider(LateEveningUtc), Bangkok);

        clock.BusinessDateOf(LateEveningUtc.UtcDateTime).Should()
                                                        .Be(new DateOnly(2026, 10, 15));
    }
}
```

- [ ] **Step 3: Run them to see them fail**

Run: `dotnet test tests/IndyPOS.Presentation.Tests`
Expected: build FAILS — `SalesHistoryAccess` and `StoreClock` do not exist.

- [ ] **Step 4: Implement**

`src/IndyPOS.Presentation/SalesHistory/SalesHistoryAccess.cs`:

```csharp
using IndyPOS.Application.Common.Authorization;
using IndyPOS.Application.Common.Interfaces;

namespace IndyPOS.Presentation.SalesHistory;

/// <summary>
/// What the logged-in user may do with bills (spec §5): reports.view sees any day; sales.reprint
/// alone is held to today. The till reads it from the shared role map, because /auth/me carries no
/// capabilities; StoreHub still enforces the same rules on every call.
/// </summary>
public sealed record SalesHistoryAccess(string UserDisplayName, bool CanViewAnyDay, bool CanReprint)
{
    public bool IsTodayOnly => !CanViewAnyDay;

    public static SalesHistoryAccess For(ILoggedInUser user) => new(
        $"{user.FirstName} {user.LastName}".Trim(),
        RoleCapabilities.HasCapability(user.RoleId, Capability.ReportsView),
        RoleCapabilities.HasCapability(user.RoleId, Capability.SalesReprint));
}
```

`src/IndyPOS.Presentation/SalesHistory/StoreClock.cs`:

```csharp
using IndyPOS.Application.Common.Authorization;

namespace IndyPOS.Presentation.SalesHistory;

/// <summary>
/// The store's own calendar: "today" is plain midnight in the store's time zone, the boundary StoreHub's
/// today-only rule uses, and times are shown store-local. StoreHub sends UTC.
/// </summary>
public sealed class StoreClock(TimeProvider time, TimeZoneInfo storeTimeZone)
{
    public DateOnly Today => BusinessDateOf(time.GetUtcNow().UtcDateTime);

    // JSON can leave the Kind Unspecified; StoreHub always means UTC.
    public DateTime ToStoreTime(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), storeTimeZone);

    public DateOnly BusinessDateOf(DateTime utc) =>
        TodayOnlyRule.BusinessDateOf(DateTime.SpecifyKind(utc, DateTimeKind.Utc), storeTimeZone);
}
```

- [ ] **Step 5: Run them to see them pass**

Run: `dotnet test tests/IndyPOS.Presentation.Tests`
Expected: PASS, 8 tests.

- [ ] **Step 6: Commit**

```bash
git add src/IndyPOS.Presentation tests/IndyPOS.Presentation.Tests IndyPOS.sln
git commit -m "feat(till): IndyPOS.Presentation, with the user's bill access and the store clock"
```

---

### Task 4: `InvoiceDetailViewModel`

**Files:**
- Create: `src/IndyPOS.Presentation/SalesHistory/InvoiceDetailViewModel.cs`
- Test: `tests/IndyPOS.Presentation.Tests/SalesHistory/InvoiceDetailViewModelTests.cs`

**Interfaces:**
- Consumes: Task 2's `GetSaleAsync`, `CreateReprintAsync`; Task 1's `StoreHubException`; Task 3's `SalesHistoryAccess`, `StoreClock`; `IReceiptPrinterService.PrintReceipt(ReceiptDocument)` and `ReceiptDocumentFactory(TimeZoneInfo).ForReprint(InvoiceReprintResultDto, string)` (plan A, `IndyPOS.Application.Common.Receipts`); `TodayOnlyRule.Allows(DateOnly day, DateOnly today, bool canViewAnyDay)`.
- Produces: `sealed partial class InvoiceDetailViewModel : ObservableObject`
  - ctor `(IStoreHubClient client, IReceiptPrinterService printer, ReceiptDocumentFactory receipts, StoreClock clock, SalesHistoryAccess access)`
  - `InvoiceDetailDto? Bill`, `bool IsBusy`, `string? ErrorMessage` (observable)
  - `DateTime? IssuedAt`, `decimal HardwareTotal`, `decimal GeneralTotal` (computed, raised when `Bill` changes)
  - `void Show(InvoiceDetailDto bill)`, `Task LoadAsync(Guid saleId)`
  - `IAsyncRelayCommand ReprintCommand`
  - constants `BillNotFound`, `LoadFailed`, `ReprintFailed`, `PrintFailed`

- [ ] **Step 1: Write the failing tests**

`tests/IndyPOS.Presentation.Tests/SalesHistory/InvoiceDetailViewModelTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Receipts;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Application.UseCases.StoreHub.Sales.Reprints;
using IndyPOS.Domain.Enums;
using IndyPOS.Presentation.SalesHistory;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;

namespace IndyPOS.Presentation.Tests.SalesHistory;

public class InvoiceDetailViewModelTests
{
    // Now is 10:00 on 2026-10-15 in Bangkok.
    private static readonly DateTimeOffset NowUtc = new(2026, 10, 15, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateTime TodayUtc = new(2026, 10, 15, 2, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime YesterdayUtc = new(2026, 10, 14, 2, 0, 0, DateTimeKind.Utc);
    private static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    private static readonly SalesHistoryAccess Cashier = new("Malee Cashier", CanViewAnyDay: false, CanReprint: true);
    private static readonly SalesHistoryAccess Manager = new("Somchai Manager", CanViewAnyDay: true, CanReprint: true);
    private static readonly SalesHistoryAccess NoReprint = new("Viewer", CanViewAnyDay: true, CanReprint: false);

    private readonly Mock<IStoreHubClient> _client = new();
    private readonly Mock<IReceiptPrinterService> _printer = new();

    [Fact]
    public void Reprint_WithoutSalesReprint_CannotExecute()
    {
        var vm = Detail(NoReprint);
        vm.Show(Bill(TodayUtc));

        vm.ReprintCommand.CanExecute(null).Should()
                                          .BeFalse();
    }

    [Fact]
    public void Reprint_ForATodayOnlyUserOnYesterdaysBill_CannotExecute()
    {
        var vm = Detail(Cashier);
        vm.Show(Bill(YesterdayUtc));

        vm.ReprintCommand.CanExecute(null).Should()
                                          .BeFalse();
    }

    [Fact]
    public void Reprint_WithNoBill_CannotExecute()
    {
        var vm = Detail(Manager);

        vm.ReprintCommand.CanExecute(null).Should()
                                          .BeFalse();
    }

    [Fact]
    public async Task Reprint_WhenStoreHubFails_PrintsNothing()
    {
        _client.Setup(c => c.CreateReprintAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new StoreHubException("ดูได้เฉพาะบิลของวันนี้"));
        var vm = Detail(Manager);
        vm.Show(Bill(TodayUtc));

        await vm.ReprintCommand.ExecuteAsync(null);

        _printer.Verify(p => p.PrintReceipt(It.IsAny<ReceiptDocument>()), Times.Never);
    }

    [Fact]
    public async Task Reprint_WhenStoreHubFails_ShowsItsReason()
    {
        _client.Setup(c => c.CreateReprintAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new StoreHubException("ดูได้เฉพาะบิลของวันนี้"));
        var vm = Detail(Manager);
        vm.Show(Bill(TodayUtc));

        await vm.ReprintCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should()
                       .Be(string.Format(InvoiceDetailViewModel.ReprintFailed, "ดูได้เฉพาะบิลของวันนี้"));
    }

    [Fact]
    public async Task Reprint_WhenThePrinterFails_SaysTheReprintWasRecorded()
    {
        SetupReprintOf(Bill(TodayUtc));
        _printer.Setup(p => p.PrintReceipt(It.IsAny<ReceiptDocument>()))
                .Throws(new InvalidOperationException("printer offline"));
        var vm = Detail(Manager);
        vm.Show(Bill(TodayUtc));

        await vm.ReprintCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should()
                       .Be(string.Format(InvoiceDetailViewModel.PrintFailed, "printer offline"));
    }

    [Fact]
    public async Task Load_WhenStoreHubAnswersNotFound_ShowsBillNotFound()
    {
        _client.Setup(c => c.GetSaleAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((InvoiceDetailDto?)null);
        var vm = Detail(Cashier);

        await vm.LoadAsync(Guid.NewGuid());

        vm.ErrorMessage.Should()
                       .Be(InvoiceDetailViewModel.BillNotFound);
    }

    [Fact]
    public async Task Load_WhenStoreHubFails_ShowsItsReason()
    {
        _client.Setup(c => c.GetSaleAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new StoreHubException("Cannot connect to StoreHub"));
        var vm = Detail(Cashier);

        await vm.LoadAsync(Guid.NewGuid());

        vm.ErrorMessage.Should()
                       .Be(string.Format(InvoiceDetailViewModel.LoadFailed, "Cannot connect to StoreHub"));
    }

    [Fact]
    public async Task Load_WhenAnEarlierLoadAnswersLast_KeepsTheLaterBill()
    {
        var slow = new TaskCompletionSource<InvoiceDetailDto?>();
        var first = Bill(TodayUtc) with { InvoiceNumber = 1 };
        var second = Bill(TodayUtc) with { InvoiceNumber = 2 };
        _client.Setup(c => c.GetSaleAsync(first.Id, It.IsAny<CancellationToken>())).Returns(slow.Task);
        _client.Setup(c => c.GetSaleAsync(second.Id, It.IsAny<CancellationToken>())).ReturnsAsync(second);
        var vm = Detail(Manager);
        var firstLoad = vm.LoadAsync(first.Id);
        await vm.LoadAsync(second.Id);

        slow.SetResult(first);
        await firstLoad;

        vm.Bill!.InvoiceNumber.Should()
                              .Be(2);
    }

    [Fact]
    public void Reprint_ForAManagerOnYesterdaysBill_CanExecute()
    {
        var vm = Detail(Manager);
        vm.Show(Bill(YesterdayUtc));

        vm.ReprintCommand.CanExecute(null).Should()
                                          .BeTrue();
    }

    [Fact]
    public async Task Reprint_WithABill_PrintsACopyByTheReprinter()
    {
        SetupReprintOf(Bill(TodayUtc));
        var vm = Detail(Cashier);
        vm.Show(Bill(TodayUtc));

        await vm.ReprintCommand.ExecuteAsync(null);

        _printer.Verify(p => p.PrintReceipt(It.Is<ReceiptDocument>(d => d.IsCopy && d.Reprint!.ReprintedBy == "Malee Cashier")));
    }

    [Fact]
    public void IssuedAt_WithAUtcBill_IsBangkokTime()
    {
        var vm = Detail(Manager);

        vm.Show(Bill(TodayUtc));

        vm.IssuedAt.Should()
                   .Be(new DateTime(2026, 10, 15, 9, 0, 0));
    }

    [Fact]
    public void HardwareTotal_WithMixedLines_SumsOnlyHardware()
    {
        var vm = Detail(Manager);

        vm.Show(Bill(TodayUtc));

        vm.HardwareTotal.Should()
                        .Be(250m);
    }

    [Fact]
    public void GeneralTotal_WithAnUncategorisedLine_CountsItAsGeneral()
    {
        var vm = Detail(Manager);

        vm.Show(Bill(TodayUtc));

        vm.GeneralTotal.Should()
                       .Be(130m);
    }

    private InvoiceDetailViewModel Detail(SalesHistoryAccess access) =>
        new(_client.Object, _printer.Object, new ReceiptDocumentFactory(Bangkok),
            new StoreClock(new FakeTimeProvider(NowUtc), Bangkok), access);

    private void SetupReprintOf(InvoiceDetailDto bill) =>
        _client.Setup(c => c.CreateReprintAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new InvoiceReprintResultDto(bill, new InvoiceReprintDto(Guid.NewGuid(), bill.Id, NowUtc.UtcDateTime, Guid.NewGuid())));

    // 250 hardware + 100 general + 30 with no category (a deleted product) = 380.
    private static InvoiceDetailDto Bill(DateTime createdUtc) => new(
        Id: Guid.NewGuid(),
        InvoiceNumber: 1042,
        StoreId: "store-a",
        UserId: Guid.NewGuid(),
        CashierName: "Malee Cashier",
        TotalAmount: 380m,
        AmountReceived: 400m,
        ChangeGiven: 20m,
        IsRefund: false,
        HasPayLater: false,
        PayLaterAmount: 0m,
        CreatedUtc: createdUtc,
        Lines:
        [
            Line("Hammer", ProductCategoryKind.Hardware, 250m),
            Line("Rice", ProductCategoryKind.GeneralGoods, 100m),
            Line("Old item", null, 30m)
        ],
        Payments: [new PaymentDto(Guid.NewGuid(), "Cash", "เงินสด", 400m, null)]);

    private static InvoiceLineDto Line(string name, ProductCategoryKind? kind, decimal total) =>
        new(Guid.NewGuid(), Guid.NewGuid(), name, null, null, kind, 1, total, total);
}
```

`Load_WhenAnEarlierLoadAnswersLast_KeepsTheLaterBill` sets up `GetSaleAsync` per id, so `first` and `second` must be built once and reused (they are: the `with` copies keep their `Id`s).

- [ ] **Step 2: Run them to see them fail**

Run: `dotnet test tests/IndyPOS.Presentation.Tests --filter "FullyQualifiedName~InvoiceDetailViewModelTests"`
Expected: build FAILS — `InvoiceDetailViewModel` does not exist.

- [ ] **Step 3: Implement**

`src/IndyPOS.Presentation/SalesHistory/InvoiceDetailViewModel.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Authorization;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Receipts;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Domain.Enums;

namespace IndyPOS.Presentation.SalesHistory;

/// <summary>
/// One bill (spec §7): its lines and payments in store time, the Hardware / General split, and
/// reprint. The void feature adds its command here later.
/// </summary>
public sealed partial class InvoiceDetailViewModel(
    IStoreHubClient client,
    IReceiptPrinterService printer,
    ReceiptDocumentFactory receipts,
    StoreClock clock,
    SalesHistoryAccess access) : ObservableObject
{
    public const string BillNotFound = "ไม่พบบิล";
    public const string LoadFailed = "โหลดบิลไม่สำเร็จ: {0}";
    public const string ReprintFailed = "พิมพ์ซ้ำไม่สำเร็จ: {0}";
    public const string PrintFailed = "บันทึกการพิมพ์ซ้ำแล้ว แต่เครื่องพิมพ์ไม่ทำงาน: {0}";

    // Bumped by every load; a response for an older load is dropped.
    private int _loadVersion;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IssuedAt), nameof(HardwareTotal), nameof(GeneralTotal))]
    [NotifyCanExecuteChangedFor(nameof(ReprintCommand))]
    private InvoiceDetailDto? _bill;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    public DateTime? IssuedAt => Bill is null ? null : clock.ToStoreTime(Bill.CreatedUtc);

    public decimal HardwareTotal =>
        Bill?.Lines.Where(l => l.CategoryKind == ProductCategoryKind.Hardware).Sum(l => l.LineTotal) ?? 0m;

    // Everything that is not hardware, including a line whose product has no category any more.
    public decimal GeneralTotal => (Bill?.Lines.Sum(l => l.LineTotal) ?? 0m) - HardwareTotal;

    /// <summary>Shows a bill the caller already has, e.g. from a bill-number search.</summary>
    public void Show(InvoiceDetailDto bill)
    {
        _loadVersion++;
        ErrorMessage = null;
        Bill = bill;
    }

    public async Task LoadAsync(Guid saleId)
    {
        var version = ++_loadVersion;
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var bill = await client.GetSaleAsync(saleId);
            if (version != _loadVersion)
                return;

            Bill = bill;
            ErrorMessage = bill is null ? BillNotFound : null;
        }
        catch (StoreHubException ex) when (version == _loadVersion)
        {
            Bill = null;
            ErrorMessage = string.Format(LoadFailed, ex.Message);
        }
        finally
        {
            if (version == _loadVersion)
                IsBusy = false;
        }
    }

    private bool CanReprint() =>
        Bill is not null
        && access.CanReprint
        && TodayOnlyRule.Allows(clock.BusinessDateOf(Bill.CreatedUtc), clock.Today, access.CanViewAnyDay);

    [RelayCommand(CanExecute = nameof(CanReprint))]
    private async Task ReprintAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await client.CreateReprintAsync(Bill!.Id, cancellationToken);
            Print(receipts.ForReprint(result, access.UserDisplayName));
        }
        catch (StoreHubException ex)
        {
            ErrorMessage = string.Format(ReprintFailed, ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // The reprint is already recorded; say so, so a retry is known to write a second record.
    private void Print(ReceiptDocument copy)
    {
        try
        {
            printer.PrintReceipt(copy);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = string.Format(PrintFailed, ex.Message);
        }
    }
}
```

If the toolkit's generator rejects `[ObservableProperty]` on a field of a class with a primary constructor, or warns (MVVMTK0045 and friends), convert that class to an explicit constructor assigning `private readonly` fields — the behaviour is the same — and ledger the ruling.

- [ ] **Step 4: Run them to see them pass**

Run: `dotnet test tests/IndyPOS.Presentation.Tests --filter "FullyQualifiedName~InvoiceDetailViewModelTests"`
Expected: PASS, 14 tests.

- [ ] **Step 5: Commit**

```bash
git add src/IndyPOS.Presentation/SalesHistory/InvoiceDetailViewModel.cs tests/IndyPOS.Presentation.Tests/SalesHistory/InvoiceDetailViewModelTests.cs
git commit -m "feat(till): InvoiceDetailViewModel shows a bill in store time and reprints it as a copy"
```

---

### Task 5: `InvoiceHistoryViewModel`

**Files:**
- Create: `src/IndyPOS.Presentation/SalesHistory/InvoiceHistoryViewModel.cs`
- Test: `tests/IndyPOS.Presentation.Tests/SalesHistory/InvoiceHistoryViewModelTests.cs`

**Interfaces:**
- Consumes: Task 2's `ListSalesAsync`, `GetSaleByNumberAsync`; Task 3's `SalesHistoryAccess`, `StoreClock`; Task 4's `InvoiceDetailViewModel` (`Show`, `LoadAsync`); `SalesPage(IReadOnlyList<InvoiceSummaryDto> Items, int Page, int PageSize, bool HasMore)`, `InvoiceSummaryDto(Guid Id, long InvoiceNumber, DateTime CreatedUtc, decimal TotalAmount, string PrimaryPaymentMethod, int LineCount)`.
- Produces:
  - `sealed record InvoiceRow(Guid Id, long InvoiceNumber, DateTime CreatedAt, decimal TotalAmount, string PrimaryPaymentMethod, int LineCount)` — `CreatedAt` store-local
  - `sealed record DateRange(DateOnly From, DateOnly To)`
  - `sealed partial class InvoiceHistoryViewModel : ObservableObject`, ctor `(IStoreHubClient client, StoreClock clock, SalesHistoryAccess access, InvoiceDetailViewModel detail)`
  - `InvoiceDetailViewModel Detail`, `bool IsTodayOnly`, `ObservableCollection<InvoiceRow> Bills`, observable `bool HasMore`, `bool IsBusy`, `string? ErrorMessage`, `string SearchText`, `DateOnly From`, `DateOnly To`
  - commands: `ShowTodayCommand`, `ShowThisMonthCommand`, `ShowThisYearCommand`, `ShowRangeCommand` (parameter `DateRange`), `LoadMoreCommand`, `SearchCommand`, `OpenCommand` (parameter `InvoiceRow`)
  - constants `PageSize = 50`, `NotANumber`, `NumberNotFound`, `RangeReversed`, `LoadFailed`, `SearchFailed`

- [ ] **Step 1: Write the failing tests**

`tests/IndyPOS.Presentation.Tests/SalesHistory/InvoiceHistoryViewModelTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Receipts;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Presentation.SalesHistory;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;

namespace IndyPOS.Presentation.Tests.SalesHistory;

public class InvoiceHistoryViewModelTests
{
    // Now is 10:00 on 2026-10-15 in Bangkok.
    private static readonly DateTimeOffset NowUtc = new(2026, 10, 15, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 15);
    private static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    private static readonly SalesHistoryAccess Cashier = new("Malee Cashier", CanViewAnyDay: false, CanReprint: true);
    private static readonly SalesHistoryAccess Manager = new("Somchai Manager", CanViewAnyDay: true, CanReprint: true);

    private readonly Mock<IStoreHubClient> _client = new();

    [Theory]
    [InlineData("abc")]
    [InlineData("-5")]
    [InlineData("1,042")]
    [InlineData("")]
    public async Task Search_WithTextThatIsNotABillNumber_ShowsNotANumber(string text)
    {
        var vm = History(Manager);
        vm.SearchText = text;

        await vm.SearchCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should()
                       .Be(InvoiceHistoryViewModel.NotANumber);
    }

    [Fact]
    public async Task Search_WithTextThatIsNotABillNumber_DoesNotCallStoreHub()
    {
        var vm = History(Manager);
        vm.SearchText = "abc";

        await vm.SearchCommand.ExecuteAsync(null);

        _client.Verify(c => c.GetSaleByNumberAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Search_WhenStoreHubAnswersNotFound_ShowsNumberNotFound()
    {
        _client.Setup(c => c.GetSaleByNumberAsync(1042, It.IsAny<CancellationToken>()))
               .ReturnsAsync((InvoiceDetailDto?)null);
        var vm = History(Cashier);
        vm.SearchText = "1042";

        await vm.SearchCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should()
                       .Be(string.Format(InvoiceHistoryViewModel.NumberNotFound, 1042));
    }

    [Fact]
    public async Task Search_WhenStoreHubFails_ShowsItsReason()
    {
        _client.Setup(c => c.GetSaleByNumberAsync(1042, It.IsAny<CancellationToken>()))
               .ThrowsAsync(new StoreHubException("Cannot connect to StoreHub"));
        var vm = History(Manager);
        vm.SearchText = "1042";

        await vm.SearchCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should()
                       .Be(string.Format(InvoiceHistoryViewModel.SearchFailed, "Cannot connect to StoreHub"));
    }

    [Fact]
    public void ShowThisMonth_ForATodayOnlyUser_CannotExecute()
    {
        var vm = History(Cashier);

        vm.ShowThisMonthCommand.CanExecute(null).Should()
                                                .BeFalse();
    }

    [Fact]
    public void ShowRange_ForATodayOnlyUser_CannotExecute()
    {
        var vm = History(Cashier);

        vm.ShowRangeCommand.CanExecute(new DateRange(Today, Today)).Should()
                                                                   .BeFalse();
    }

    [Fact]
    public async Task ShowRange_WithTheStartAfterTheEnd_ShowsRangeReversed()
    {
        var vm = History(Manager);

        await vm.ShowRangeCommand.ExecuteAsync(new DateRange(Today, Today.AddDays(-1)));

        vm.ErrorMessage.Should()
                       .Be(InvoiceHistoryViewModel.RangeReversed);
    }

    [Fact]
    public async Task ShowRange_WithTheStartAfterTheEnd_DoesNotCallStoreHub()
    {
        var vm = History(Manager);

        await vm.ShowRangeCommand.ExecuteAsync(new DateRange(Today, Today.AddDays(-1)));

        _client.Verify(c => c.ListSalesAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<int>(),
                                             It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ShowToday_WhenStoreHubFails_ShowsItsReason()
    {
        _client.Setup(c => c.ListSalesAsync(Today, Today, 1, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()))
               .ThrowsAsync(new StoreHubException("Cannot connect to StoreHub"));
        var vm = History(Cashier);

        await vm.ShowTodayCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should()
                       .Be(string.Format(InvoiceHistoryViewModel.LoadFailed, "Cannot connect to StoreHub"));
    }

    [Fact]
    public async Task ShowToday_WhenAnEarlierLoadAnswersLast_KeepsTheLaterList()
    {
        var slowMonth = new TaskCompletionSource<SalesPage>();
        _client.Setup(c => c.ListSalesAsync(new DateOnly(2026, 10, 1), Today, 1, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()))
               .Returns(slowMonth.Task);
        _client.Setup(c => c.ListSalesAsync(Today, Today, 1, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()))
               .ReturnsAsync(Page(hasMore: false, 2));
        var vm = History(Manager);
        var monthLoad = vm.ShowThisMonthCommand.ExecuteAsync(null);
        await vm.ShowTodayCommand.ExecuteAsync(null);

        slowMonth.SetResult(Page(hasMore: false, 1));
        await monthLoad;

        vm.Bills.Select(b => b.InvoiceNumber).Should()
                                             .Equal(2);
    }

    [Fact]
    public async Task Search_WhenFound_ShowsTheBillInTheDetail()
    {
        var bill = Detail(1042);
        _client.Setup(c => c.GetSaleByNumberAsync(1042, It.IsAny<CancellationToken>()))
               .ReturnsAsync(bill);
        var vm = History(Manager);
        vm.SearchText = " 1042 ";

        await vm.SearchCommand.ExecuteAsync(null);

        vm.Detail.Bill.Should()
                      .Be(bill);
    }

    [Fact]
    public async Task ShowThisMonth_ForAManager_AsksFromTheFirstOfTheMonth()
    {
        _client.Setup(c => c.ListSalesAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(Page(hasMore: false));
        var vm = History(Manager);

        await vm.ShowThisMonthCommand.ExecuteAsync(null);

        _client.Verify(c => c.ListSalesAsync(new DateOnly(2026, 10, 1), Today, 1, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task ShowThisYear_ForAManager_AsksFromTheFirstOfTheYear()
    {
        _client.Setup(c => c.ListSalesAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(Page(hasMore: false));
        var vm = History(Manager);

        await vm.ShowThisYearCommand.ExecuteAsync(null);

        _client.Verify(c => c.ListSalesAsync(new DateOnly(2026, 1, 1), Today, 1, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task LoadMore_WithAnotherPage_AppendsIt()
    {
        _client.Setup(c => c.ListSalesAsync(Today, Today, 1, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()))
               .ReturnsAsync(Page(hasMore: true, 3, 2));
        _client.Setup(c => c.ListSalesAsync(Today, Today, 2, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()))
               .ReturnsAsync(Page(hasMore: false, 1));
        var vm = History(Cashier);
        await vm.ShowTodayCommand.ExecuteAsync(null);

        await vm.LoadMoreCommand.ExecuteAsync(null);

        vm.Bills.Select(b => b.InvoiceNumber).Should()
                                             .Equal(3, 2, 1);
    }

    [Fact]
    public async Task ShowToday_WithABill_ShowsItsTimeInStoreTime()
    {
        _client.Setup(c => c.ListSalesAsync(Today, Today, 1, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()))
               .ReturnsAsync(Page(hasMore: false, 1));
        var vm = History(Cashier);

        await vm.ShowTodayCommand.ExecuteAsync(null);

        vm.Bills.Single().CreatedAt.Should()
                                   .Be(new DateTime(2026, 10, 15, 9, 0, 0));
    }

    [Fact]
    public void IsTodayOnly_ForACashier_IsTrue()
    {
        var vm = History(Cashier);

        vm.IsTodayOnly.Should()
                      .BeTrue();
    }

    private InvoiceHistoryViewModel History(SalesHistoryAccess access)
    {
        var clock = new StoreClock(new FakeTimeProvider(NowUtc), Bangkok);
        var detail = new InvoiceDetailViewModel(_client.Object, Mock.Of<IReceiptPrinterService>(),
                                                new ReceiptDocumentFactory(Bangkok), clock, access);

        return new InvoiceHistoryViewModel(_client.Object, clock, access, detail);
    }

    // Every bill at 02:00 UTC = 09:00 Bangkok today.
    private static SalesPage Page(bool hasMore, params long[] numbers) =>
        new(numbers.Select(n => new InvoiceSummaryDto(Guid.NewGuid(), n, new DateTime(2026, 10, 15, 2, 0, 0, DateTimeKind.Utc), 100m, "Cash", 1)).ToList(),
            1, InvoiceHistoryViewModel.PageSize, hasMore);

    private static InvoiceDetailDto Detail(long number) => new(
        Guid.NewGuid(), number, "store-a", Guid.NewGuid(), "Malee Cashier", 100m, 100m, 0m, false, false, 0m,
        new DateTime(2026, 10, 15, 2, 0, 0, DateTimeKind.Utc), [], []);
}
```

`Mock.Of<IReceiptPrinterService>()` here is Moq's: this test project has no `IndyPOS.Mock` namespace to collide with (plan A's ruling was about Application.Tests).

- [ ] **Step 2: Run them to see them fail**

Run: `dotnet test tests/IndyPOS.Presentation.Tests --filter "FullyQualifiedName~InvoiceHistoryViewModelTests"`
Expected: build FAILS — `InvoiceHistoryViewModel`, `DateRange`, `InvoiceRow` do not exist.

- [ ] **Step 3: Implement**

`src/IndyPOS.Presentation/SalesHistory/InvoiceHistoryViewModel.cs`:

```csharp
using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;

namespace IndyPOS.Presentation.SalesHistory;

/// <summary>A bill in the list, its time already store-local.</summary>
public sealed record InvoiceRow(Guid Id, long InvoiceNumber, DateTime CreatedAt, decimal TotalAmount,
                                string PrimaryPaymentMethod, int LineCount);

public sealed record DateRange(DateOnly From, DateOnly To);

/// <summary>
/// The bill list (spec §7). A manager picks a period; a cashier sees today only. Any newer request
/// cancels the one in flight, so an older answer never replaces a newer list. Nothing changes until
/// StoreHub answers.
/// </summary>
public sealed partial class InvoiceHistoryViewModel : ObservableObject
{
    public const int PageSize = 50;
    public const string NotANumber = "กรุณาใส่เลขที่บิลเป็นตัวเลข";
    public const string NumberNotFound = "ไม่พบบิลเลขที่ {0}";
    public const string RangeReversed = "วันที่เริ่มต้นต้องไม่เกินวันที่สิ้นสุด";
    public const string LoadFailed = "โหลดรายการบิลไม่สำเร็จ: {0}";
    public const string SearchFailed = "ค้นหาบิลไม่สำเร็จ: {0}";

    private const int FirstPage = 1;

    private readonly IStoreHubClient _client;
    private readonly StoreClock _clock;
    private readonly SalesHistoryAccess _access;
    private CancellationTokenSource _current = new();
    private int _page;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadMoreCommand))]
    private bool _hasMore;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public InvoiceHistoryViewModel(IStoreHubClient client, StoreClock clock, SalesHistoryAccess access,
                                   InvoiceDetailViewModel detail)
    {
        _client = client;
        _clock = clock;
        _access = access;
        Detail = detail;
    }

    public InvoiceDetailViewModel Detail { get; }

    public bool IsTodayOnly => _access.IsTodayOnly;

    public ObservableCollection<InvoiceRow> Bills { get; } = [];

    public DateOnly From { get; private set; }

    public DateOnly To { get; private set; }

    private bool CanPickPeriod() => _access.CanViewAnyDay;

    [RelayCommand]
    private Task ShowTodayAsync() => ShowAsync(_clock.Today, _clock.Today);

    [RelayCommand(CanExecute = nameof(CanPickPeriod))]
    private Task ShowThisMonthAsync()
    {
        var today = _clock.Today;

        return ShowAsync(new DateOnly(today.Year, today.Month, 1), today);
    }

    [RelayCommand(CanExecute = nameof(CanPickPeriod))]
    private Task ShowThisYearAsync()
    {
        var today = _clock.Today;

        return ShowAsync(new DateOnly(today.Year, 1, 1), today);
    }

    [RelayCommand(CanExecute = nameof(CanShowRange))]
    private Task ShowRangeAsync(DateRange range)
    {
        if (range.From > range.To)
        {
            ErrorMessage = RangeReversed;
            return Task.CompletedTask;
        }

        return ShowAsync(range.From, range.To);
    }

    private bool CanShowRange(DateRange? range) => CanPickPeriod();

    [RelayCommand(CanExecute = nameof(HasMore))]
    private Task LoadMoreAsync() => LoadPageAsync(_page + 1, _current.Token);

    [RelayCommand]
    private Task OpenAsync(InvoiceRow row) => Detail.LoadAsync(row.Id);

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (!long.TryParse(SearchText.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var number))
        {
            ErrorMessage = NotANumber;
            return;
        }

        var token = Restart();
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var bill = await _client.GetSaleByNumberAsync(number, token);
            if (token.IsCancellationRequested)
                return;

            if (bill is null)
                ErrorMessage = string.Format(NumberNotFound, number);
            else
                Detail.Show(bill);
        }
        catch (StoreHubException ex) when (!token.IsCancellationRequested)
        {
            ErrorMessage = string.Format(SearchFailed, ex.Message);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        finally
        {
            if (!token.IsCancellationRequested)
                IsBusy = false;
        }
    }

    private Task ShowAsync(DateOnly from, DateOnly to)
    {
        var token = Restart();
        From = from;
        To = to;

        return LoadPageAsync(FirstPage, token);
    }

    private async Task LoadPageAsync(int page, CancellationToken token)
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await _client.ListSalesAsync(From, To, page, PageSize, token);
            if (token.IsCancellationRequested)
                return;

            if (page == FirstPage)
                Bills.Clear();

            foreach (var bill in result.Items)
                Bills.Add(ToRow(bill));

            _page = page;
            HasMore = result.HasMore;
        }
        catch (StoreHubException ex) when (!token.IsCancellationRequested)
        {
            ErrorMessage = string.Format(LoadFailed, ex.Message);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        finally
        {
            if (!token.IsCancellationRequested)
                IsBusy = false;
        }
    }

    private CancellationToken Restart()
    {
        _current.Cancel();
        _current.Dispose();
        _current = new CancellationTokenSource();

        return _current.Token;
    }

    private InvoiceRow ToRow(InvoiceSummaryDto bill) =>
        new(bill.Id, bill.InvoiceNumber, _clock.ToStoreTime(bill.CreatedUtc), bill.TotalAmount,
            bill.PrimaryPaymentMethod, bill.LineCount);
}
```

Note on `LoadPageAsync`: the list is cleared only when the first page **arrives**, not when the request starts — "nothing changes until the server answers" (spec §7).

- [ ] **Step 4: Run them to see them pass**

Run: `dotnet test tests/IndyPOS.Presentation.Tests --filter "FullyQualifiedName~InvoiceHistoryViewModelTests"`
Expected: PASS, 19 tests (the theory counts 4).

- [ ] **Step 5: Commit**

```bash
git add src/IndyPOS.Presentation/SalesHistory/InvoiceHistoryViewModel.cs tests/IndyPOS.Presentation.Tests/SalesHistory/InvoiceHistoryViewModelTests.cs
git commit -m "feat(till): InvoiceHistoryViewModel lists, pages and finds bills, latest request winning"
```

---

### Task 6: Full run, docs, PR

- [ ] **Step 1: Run the whole solution with Docker up**

Run: `dotnet test --logger trx --results-directory <scratchpad>/trx-plan-b`
Expected: every suite green, with the new `IndyPOS.Presentation.Tests` suite present. Record per-suite counts.

- [ ] **Step 2: Docs**

- `CLAUDE.md`: add `IndyPOS.Presentation/` to the project-structure block ("ViewModels (CommunityToolkit.Mvvm), no WinForms reference"), add `IndyPOS.Presentation.Tests/` to the tests block, and update the measured counts paragraph (new suite, Application growth, total), appending one sentence in the existing style.
- `ONBOARDING.md`: add the suite to the per-suite table and update the totals.
- Measure; do not derive. Docker-down counts are unchanged (the new suite needs no container).
- Baseline: `development` measured **1422** on 2026-10-15 after #127 and #128 (Application 690, Windows.Forms 89); the docs still say 1421 and Windows.Forms 88, because #128 left its +1 to a follow-up. Record #128's +1 in the same sentence.

- [ ] **Step 3: Commit, push, open the PR**

```bash
git add CLAUDE.md ONBOARDING.md
git commit -m "docs: IndyPOS.Presentation, and measured counts"
git push -u origin feat/sales-history-viewmodels
gh pr create --base development --title "feat(till): sales-history ViewModels and the StoreHub bill calls"
```

PR body: what the till can now do (in ViewModels only), the Application-level `StoreHubException` and why, the 404-as-null decision, the plan-C notes (RELEASE print gate, DI wiring), the Review Focus list, and the counts. Wait for Codex before `gh pr merge --auto --merge`.

---

## Self-Review

**Spec coverage:** §7 project shape (net10.0, toolkit, Application only, test project) — Task 3. Client methods + `StoreHubHttpClientTests` — Task 2. History/today-only modes — Tasks 3 (`IsTodayOnly`), 5 (period `CanExecute`). Search rules — Task 5. Paging 50 + load more — Task 5. Latest request wins — Task 5 (list, search), Task 4 (detail). No optimistic UI — Tasks 4, 5 (`IsBusy`, list cleared only on arrival). Detail contents in Bangkok time, Hardware/General split — Task 4. `ReprintCommand` rules, copy receipt, failure prints nothing — Task 4. Thai errors — Task 1 (server reason), Tasks 4–5 (wrapping). §10 ViewModel tests: mode, search ×3, slower-earlier load, load more, reprint disabled, failed reprint prints nothing, split, Bangkok — all present.
**Deferred to plan C, on purpose:** screens, DI, บิลวันนี้ button, PayLater popup, stub removal, the RELEASE print gate.
**Placeholders:** none.
**Type consistency:** `StoreHubException`, `ListSalesAsync(from, to, page, pageSize, ct)`, `GetSaleAsync`/`GetSaleByNumberAsync` → `InvoiceDetailDto?`, `CreateReprintAsync` → `InvoiceReprintResultDto`, `SalesHistoryAccess(UserDisplayName, CanViewAnyDay, CanReprint)`, `StoreClock(TimeProvider, TimeZoneInfo)`, `InvoiceDetailViewModel(client, printer, receipts, clock, access)`, `InvoiceHistoryViewModel(client, clock, access, detail)`, `InvoiceRow`, `DateRange` — same in every task.
**Review Focus:** 1 → Task 4 `Load_WhenAnEarlierLoadAnswersLast_KeepsTheLaterBill`; 2 → Task 5 `ShowRange_WithTheStartAfterTheEnd_*`; 3 → Task 5 search theory + `" 1042 "`; 4 → Task 4 `Reprint_WhenThePrinterFails_SaysTheReprintWasRecorded`; 5 → Task 1 `..._WhenTheErrorBodyIsNotJson_ThrowsTheStatusMessage`.
