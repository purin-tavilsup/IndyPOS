# Per-Store UI, Slice 2 (Reports) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Each store's Reports screen shows its own pieces, and the two empty v4 report tabs show real data:
- the overview's money rows follow the store's payment catalogue, plus จัดส่ง / เอกสาร for MimyShop;
- the general/hardware and ลงบัญชี tiles are GeneralHardware-only;
- products sold lists every invoice line, so a product can be traced to its bill;
- the outstanding ลงบัญชี tab works, for GeneralHardware only.

**Architecture:**
- **StoreHub, all additive:**
  - `/reports/sales-summary` gains `paymentsByMethod` and `serviceSales`;
  - new `GET /sales/lines`, with the same rules as `GET /sales`;
  - `/reports/pay-later` gains an optional date range.
- **Application:**
  - `TillLayout` gains the report flags;
  - two pure helpers: `MoneyRows.From(summary)` and `PageReader.ReadAllAsync`.
- **Till:** the panels apply the layout and call the new client methods. The two stubbed `IReportService` members they used are removed.

**Tech Stack:** .NET 10, ASP.NET Core minimal APIs, EF Core + Npgsql, WinForms, xUnit, FluentAssertions 8, Moq. Tests use the `IntegrationTestBase` / `StoreProfileHosts` test hosts.

**Spec:** `docs/superpowers/specs/2026-10-07-per-store-ui-design.md` §2, §3, §4.2 and §4.2a (approved).

## Prerequisites

- Branch `feat/per-store-ui-reports`, stacked on `feat/per-store-ui` (PR #118). After #118 merges, rebase onto `development`.
- Docker running, or `INDYPOS_TEST_POSTGRES` set.

## Decisions this plan makes (flag them in the PR)

1. **The sales tiles are hidden, not repacked.**
   - MimyMart and MimyShop keep only "ยอดขาย : ทั้งหมด", which already sits in the first slot. GeneralHardware keeps everything.
   - No store today would leave a gap, so repacking would be code for a store that does not exist (YAGNI).
2. **The money section is drawn from data.**
   - Its six fixed rows are deleted from the designer, and a `FlowLayoutPanel` takes their place.
   - Each row is built in code with the old rows' exact styling.
   - The legacy `/reports/legacy/payments-summary` call is no longer made by this panel. The route stays for compatibility.
3. **`/sales/lines` row time is the bill's time** (`Invoice.CreatedUtc`), and its date filter is the bill's store day, the same as `GET /sales`.
4. **A line's total is `UnitPrice × Quantity`**, the same as `InvoiceLine.LineTotal` and `GET /sales/{id}`'s lines. A test pins that the two agree.
5. **`/reports/pay-later` with only one of `fromDate` / `toDate` is a Thai 400.** A range needs both ends.
6. **Cash flow is not touched.** `CashFlowCalculatorPanel` still calls the stubbed `GetPayLaterPaymentsByPeriodAsync` and `CreatePaymentsSummary*`. Slice 3 owns it.

## Global Constraints

- [Spec §1] Only on/off per store type, plus the §4.2a wiring. No other new features.
- [Spec §4.2] `paymentsByMethod`: one row `{code, displayName, total}` per catalogue method that is **enabled or has sales in the range**, in display order. A deprecated method appears only in a range where it was used. `serviceSales`: `{barcode, name, total}` per service product, empty without `ServiceProductsEnabled`.
- [Spec §4.2] Overview order: sales (always), general/hardware split if `MultipleProductTypesEnabled`, ลงบัญชี tiles if `PayLaterEnabled`, one money row per `paymentsByMethod` row, then the จัดส่ง / เอกสาร rows.
- [Spec §4.2] Outstanding ลงบัญชี tab: `PayLaterEnabled` only. Products-sold filter: `MultipleProductTypesEnabled` only.
- [Spec §4.2a] `GET /sales/lines?from=&to=&page=&pageSize=`: same rules as `GET /sales` (Thai 400 on a malformed date, today-only without `reports.view`); rows are bill id, bill number, barcode, product name, quantity, unit price, line total, category code, time (UTC), note; oldest first, paged.
- [Spec §6] Additive only: new fields have defaults, older tills ignore them, no migration.
- [API conventions] `/reports` is aggregates only; a list of records is its resource's `GET`. Dates are `yyyy-MM-dd`, invariant culture.
- **Tests:**
  - names are `Subject_WhenScenario_DirectVerbOutcome`;
  - negative cases first;
  - Arrange/Act/Assert separated by blank lines;
  - FluentAssertions chains broken onto aligned lines.
- **Commits:** conventional, at least one per task, ending with the session's `Claude-Session:` line.

## Review Focus

1. **A page boundary in the middle of a bill.** Expected: no line is lost or repeated across pages. → Task 2 (`ListSaleLines_AcrossPages_ReturnsEveryLineOnce`).
2. **Line totals disagree with the bill detail.** Expected: `/sales/lines` and `GET /sales/{id}` give the same total for the same line. → Task 2 (`ListSaleLines_ForASale_MatchesTheBillDetailsLineTotal`).
3. **A deprecated method (เราชนะ) used in the range, or a payment whose code is not in the catalogue.** Expected: both are listed, the unknown code last under its own code, and a disabled method with no sales is absent. → Task 1 (`PaymentMethodTotals` unit tests).
4. **A server that always says "more".** Expected: the till stops after a page cap with an error, instead of looping for ever. → Task 4 (`ReadAllAsync_WhenPagesNeverEnd_Throws`).
5. **A Thai-culture till** (Buddhist year) building report URLs. Expected: Gregorian `yyyy-MM-dd`. → Task 4 client tests.

---

### Task 1: `/reports/sales-summary` gains `paymentsByMethod` and `serviceSales`

**Files:**
- Modify: `src/IndyPOS.Application/UseCases/StoreHub/Reports/ReportDtos.cs` (two records; `SalesSummaryDto` gains two init properties)
- Create: `src/IndyPOS.Infrastructure/QueryHandlers/Reports/PaymentMethodTotals.cs`
- Modify: `src/IndyPOS.Infrastructure/QueryHandlers/Reports/GetSalesSummaryQueryHandler.cs`
- Create: `tests/IndyPOS.Application.Tests/Infrastructure/PaymentMethodTotalsTests.cs`
- Create: `tests/IndyPOS.StoreHub.IntegrationTests/StoreProfiles/SalesSummaryPerStoreTests.cs`

**Interfaces (Produces):**
- `record PaymentMethodTotalDto(string Code, string DisplayName, decimal Total)`;
- `record ServiceSaleDto(string Barcode, string Name, decimal Total)`;
- `SalesSummaryDto.PaymentsByMethod` and `.ServiceSales`: `IReadOnlyList<…> { get; init; } = []`;
- `static class PaymentMethodTotals { static IReadOnlyList<PaymentMethodTotalDto> Build(IReadOnlyList<PaymentMethod> catalogue, IReadOnlyList<Payment> payments); }`.

- [ ] **Step 1: Failing unit tests (`PaymentMethodTotalsTests`)**

```csharp
using FluentAssertions;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.QueryHandlers.Reports;
using Xunit;

namespace IndyPOS.Application.Tests.Infrastructure;

public class PaymentMethodTotalsTests
{
    private static readonly PaymentMethod Cash = Method("Cash", "เงินสด", enabled: true, order: 1);
    private static readonly PaymentMethod Transfer = Method("MoneyTransfer", "เงินโอน", enabled: true, order: 2);
    private static readonly PaymentMethod WeWin = Method("WeWin", "เราชนะ", enabled: false, order: 7);

    [Fact]
    public void Build_WithADisabledMethodAndNoSales_LeavesItOut()
    {
        var totals = PaymentMethodTotals.Build([Cash, WeWin], [Pay("Cash", 100m)]);

        totals.Select(t => t.Code).Should()
                                  .Equal("Cash");
    }

    [Fact]
    public void Build_WithADisabledMethodThatHadSales_ListsIt()
    {
        var totals = PaymentMethodTotals.Build([Cash, WeWin], [Pay("WeWin", 40m)]);

        totals.Should()
              .Contain(new PaymentMethodTotalDto("WeWin", "เราชนะ", 40m));
    }

    [Fact]
    public void Build_WithACodeOutsideTheCatalogue_ListsItLastUnderItsCode()
    {
        var totals = PaymentMethodTotals.Build([Cash], [Pay("Card", 25m), Pay("Cash", 10m)]);

        totals.Should()
              .Equal(new PaymentMethodTotalDto("Cash", "เงินสด", 10m), new PaymentMethodTotalDto("Card", "Card", 25m));
    }

    [Fact]
    public void Build_WithAnEnabledMethodAndNoSales_ListsItAtZero()
    {
        var totals = PaymentMethodTotals.Build([Cash, Transfer], []);

        totals.Should()
              .Equal(new PaymentMethodTotalDto("Cash", "เงินสด", 0m), new PaymentMethodTotalDto("MoneyTransfer", "เงินโอน", 0m));
    }

    [Fact]
    public void Build_WithMethodsOutOfOrder_OrdersByDisplayOrder()
    {
        var totals = PaymentMethodTotals.Build([Transfer, Cash], []);

        totals.Select(t => t.Code).Should()
                                  .Equal("Cash", "MoneyTransfer");
    }

    [Fact]
    public void Build_WithSeveralPaymentsOfOneMethod_SumsThem()
    {
        var totals = PaymentMethodTotals.Build([Cash], [Pay("Cash", 100m), Pay("Cash", 50.5m)]);

        totals.Single().Total.Should()
                             .Be(150.5m);
    }

    private static PaymentMethod Method(string code, string name, bool enabled, int order) =>
        new() { Code = code, DisplayName = name, IsEnabled = enabled, DisplayOrder = order, StoreId = "TEST" };

    private static Payment Pay(string method, decimal amount) =>
        new() { Id = Guid.NewGuid(), Method = method, Amount = amount };
}
```

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~PaymentMethodTotalsTests"`. Expected: build error, because `PaymentMethodTotals` and `PaymentMethodTotalDto` do not exist.

- [ ] **Step 2: DTOs and `PaymentMethodTotals`**

In `ReportDtos.cs`, give `SalesSummaryDto` a body and add the two records:

```csharp
public record SalesSummaryDto(
    DateOnly FromDate,
    DateOnly ToDate,
    int InvoiceCount,
    decimal TotalRevenue,
    PaymentBreakdownDto PaymentBreakdown,
    IReadOnlyList<TopProductDto> TopProducts)
{
    /// <summary>One row per catalogue method that is enabled or had sales in the range, in display order.</summary>
    public IReadOnlyList<PaymentMethodTotalDto> PaymentsByMethod { get; init; } = [];

    /// <summary>The service products' sales (จัดส่ง, เอกสาร). Empty for a store without services.</summary>
    public IReadOnlyList<ServiceSaleDto> ServiceSales { get; init; } = [];
}

/// <summary>Takings by payment method, named as the store's catalogue names it.</summary>
public record PaymentMethodTotalDto(string Code, string DisplayName, decimal Total);

/// <summary>Sales of one service product.</summary>
public record ServiceSaleDto(string Barcode, string Name, decimal Total);
```

`PaymentMethodTotals.cs`:

```csharp
using IndyPOS.Application.UseCases.StoreHub.Reports;
using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Infrastructure.QueryHandlers.Reports;

/// <summary>
/// Takings per payment method for a report. A method shows when the store offers it, or when it was
/// used in the range: a deprecated campaign (เราชนะ) still shows in the months it ran.
/// </summary>
public static class PaymentMethodTotals
{
    public static IReadOnlyList<PaymentMethodTotalDto> Build(IReadOnlyList<PaymentMethod> catalogue,
                                                             IReadOnlyList<Payment> payments)
    {
        var totals = payments.GroupBy(p => p.Method)
                             .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

        var known = catalogue.Where(m => m.IsEnabled || totals.ContainsKey(m.Code))
                             .OrderBy(m => m.DisplayOrder)
                             .Select(m => new PaymentMethodTotalDto(m.Code, m.DisplayName, totals.GetValueOrDefault(m.Code)));

        // A code no catalogue row names (an old "Card", say) still counts, under its own code, last.
        var catalogueCodes = catalogue.Select(m => m.Code).ToHashSet();
        var unknown = totals.Where(t => !catalogueCodes.Contains(t.Key))
                            .OrderBy(t => t.Key, StringComparer.Ordinal)
                            .Select(t => new PaymentMethodTotalDto(t.Key, t.Key, t.Value));

        return known.Concat(unknown).ToList();
    }
}
```

Run Step 1's command. Expected: 6 pass.

- [ ] **Step 3: Failing per-store tests (`SalesSummaryPerStoreTests`)**

```csharp
using System.Net.Http.Json;
using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.Reports;
using IndyPOS.Application.UseCases.StoreHub.Sales;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.ValueObjects;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Profiles = IndyPOS.StoreProfiles.StoreProfiles;

namespace IndyPOS.StoreHub.IntegrationTests.StoreProfiles;

/// <summary>
/// The sales summary's per-store lists. The hosts' databases are shared across this collection, so
/// each test measures a before/after difference or a code list, never an absolute total.
/// </summary>
[Collection(StoreProfilesCollection.Name)]
public class SalesSummaryPerStoreTests(StoreProfileHosts hosts)
{
    private static readonly string Today = DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd");

    [Fact]
    public async Task SalesSummary_ForMimyMart_HasNoServiceSales()
    {
        var summary = await SummaryAsync("MimyMart");

        summary.ServiceSales.Should()
                            .BeEmpty();
    }

    [Fact]
    public async Task SalesSummary_ForMimyMart_ListsOnlyItsEnabledMethods()
    {
        var summary = await SummaryAsync("MimyMart");

        summary.PaymentsByMethod.Select(p => p.Code).Should()
                                                    .Equal("Cash", "MoneyTransfer");
    }

    [Fact]
    public async Task SalesSummary_ForGeneralHardwareWithAWeWinSaleToday_ListsWeWin()
    {
        await SeedPaymentAsync("GeneralHardware", "WeWin", 40m);

        var summary = await SummaryAsync("GeneralHardware");

        summary.PaymentsByMethod.Should()
                                .Contain(p => p.Code == "WeWin" && p.DisplayName == "เราชนะ");
    }

    [Fact]
    public async Task SalesSummary_ForMimyShopAfterADeliverySale_AddsItToDelivery()
    {
        var before = await SummaryAsync("MimyShop");

        await SellAsync("MimyShop", ServiceProductBarcodes.Delivery, 30m);
        var after = await SummaryAsync("MimyShop");

        Total(after, ServiceProductBarcodes.Delivery).Should()
                                                     .Be(Total(before, ServiceProductBarcodes.Delivery) + 30m);
    }

    [Fact]
    public async Task SalesSummary_ForMimyShop_ListsBothServicesInOrder()
    {
        var summary = await SummaryAsync("MimyShop");

        summary.ServiceSales.Select(s => s.Barcode).Should()
                                                   .Equal(ServiceProductBarcodes.All);
    }

    private static decimal Total(SalesSummaryDto summary, string barcode) =>
        summary.ServiceSales.Single(s => s.Barcode == barcode).Total;

    private async Task<SalesSummaryDto> SummaryAsync(string key)
    {
        var client = await hosts.SignedInAsync(key, "manager", "manager123");
        return (await client.GetFromJsonAsync<SalesSummaryDto>(
            $"/reports/sales-summary?fromDate={Today}&toDate={Today}&topProductsCount=0"))!;
    }

    private async Task SellAsync(string key, string barcode, decimal price)
    {
        var client = await hosts.SignedInAsync(key);
        var product = await ProductAsync(key, barcode);
        var response = await client.PostAsJsonAsync("/sales", new CompleteSaleRequest(
            Lines: [new SaleLineRequest(product.Id, Quantity: 1, UnitPrice: price)],
            Payments: [new SalePaymentRequest("Cash", price)]));
        response.EnsureSuccessStatusCode();
    }

    private async Task SeedPaymentAsync(string key, string method, decimal amount)
    {
        await using var scope = hosts.ServicesFor(key).CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        var now = DateTime.UtcNow;
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(), StoreId = Profiles.Find(key)!.StoreId, UserId = Guid.NewGuid(),
            TotalAmount = amount, CreatedUtc = now, LastModifiedUtc = now
        };
        invoice.Payments.Add(new Payment { Id = Guid.NewGuid(), InvoiceId = invoice.Id, Method = method, Amount = amount, CreatedUtc = now });
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
    }

    private async Task<Product> ProductAsync(string key, string barcode)
    {
        await using var scope = hosts.ServicesFor(key).CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<StoreHubDbContext>().Products
                          .AsNoTracking()
                          .SingleAsync(p => p.Barcode == barcode);
    }
}
```

If `Invoice.Payments` is not initialised by the entity, use `invoice.Payments = [new Payment { … }]`. Record that as a ruling.

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~SalesSummaryPerStoreTests"`. Expected: it builds, but every list test fails because the lists are empty. `SalesSummary_ForMimyMart_HasNoServiceSales` passes already: it guards against a wrong implementation.

- [ ] **Step 4: Fill the lists in `GetSalesSummaryQueryHandler`**

Before the `return`:

```csharp
        var catalogue = await _dbContext.PaymentMethods.AsNoTracking().ToListAsync(cancellationToken);
        var serviceSales = _storeIdentity.Features.ServiceProductsEnabled
            ? await ServiceSalesAsync(invoices, cancellationToken)
            : [];
```

Change the `return` to:

```csharp
        return new SalesSummaryDto(
            FromDate: query.FromDate,
            ToDate: query.ToDate,
            InvoiceCount: invoiceCount,
            TotalRevenue: totalRevenue,
            PaymentBreakdown: paymentBreakdown,
            TopProducts: topProductsWithCategory)
        {
            PaymentsByMethod = PaymentMethodTotals.Build(catalogue, payments),
            ServiceSales = serviceSales
        };
```

Add the method:

```csharp
    // One row per service product, in ServiceProductBarcodes order, even at zero: the cashier reads a
    // fixed pair of figures. A product missing from the catalogue still shows, under its barcode.
    private async Task<IReadOnlyList<ServiceSaleDto>> ServiceSalesAsync(
        IReadOnlyList<Invoice> invoices, CancellationToken cancellationToken)
    {
        var products = await _dbContext.Products
            .AsNoTracking()
            .Where(p => ServiceProductBarcodes.All.Contains(p.Barcode))
            .Select(p => new { p.Id, p.Barcode, p.Name })
            .ToListAsync(cancellationToken);

        var lines = invoices.SelectMany(i => i.Lines).ToList();

        return ServiceProductBarcodes.All
            .Select(barcode =>
            {
                var product = products.FirstOrDefault(p => p.Barcode == barcode);
                var total = product is null ? 0m : lines.Where(l => l.ProductId == product.Id).Sum(l => l.LineTotal);
                return new ServiceSaleDto(barcode, product?.Name ?? barcode, total);
            })
            .ToList();
    }
```

Add `using IndyPOS.Domain.Entities.Core;` and `using IndyPOS.Domain.ValueObjects;`. `invoices` is already a `List<Invoice>`, which is an `IReadOnlyList<Invoice>`.

Run Step 3's command. Expected: 5 pass. Also run `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~ReportsEndpointTests"`. Expected: all pass.

- [ ] **Step 5: Commit**

`feat(storehub): the sales summary lists takings by payment method and service sales`

---

### Task 2: `GET /sales/lines`

**Files:**
- Modify: `src/IndyPOS.Application/UseCases/StoreHub/Sales/History/SaleDtos.cs` (two records)
- Create: `src/IndyPOS.Application/UseCases/StoreHub/Sales/History/ListSaleLinesQuery.cs`
- Create: `src/IndyPOS.Infrastructure/QueryHandlers/Sales/ListSaleLinesQueryHandler.cs`
- Modify: `src/IndyPOS.StoreHub/Endpoints/Sales/SaleQueryEndpoints.cs` (map `/lines`)
- Modify: the StoreHub DI registration. Find it with `grep -rn "ListSalesQueryHandler" src/IndyPOS.StoreHub`, and register the new handler the same way.
- Modify: `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/RouteTableTests.cs` (pin the route)
- Create: `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/SaleLinesEndpointsTests.cs`

**Interfaces (Produces):**
- `record SaleLineRowDto(Guid InvoiceId, long InvoiceNumber, string Barcode, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal, string? CategoryCode, DateTime CreatedUtc, string? Note)`;
- `record SaleLinesPage(IReadOnlyList<SaleLineRowDto> Items, int Page, int PageSize, bool HasMore)`;
- `record ListSaleLinesQuery(DateOnly? From, DateOnly? To, int Page, int PageSize, bool CanViewAnyDay) : IQuery<SaleLinesPage>`.

- [ ] **Step 1: Failing tests (`SaleLinesEndpointsTests`)**

Model it on `SalesHistoryEndpointsTests`: `[Collection("Integration")]`, `: IntegrationTestBase`, a constructor taking `StoreHubWebApplicationFactory`, and the same `Today`, `TwoDaysAgoUtc` and `Day(...)` helpers. Copy that file's `using` block, then add the new DTOs' namespace.

```csharp
    private async Task<CompleteSaleResponse> SellAsync(params (string Name, decimal Price)[] lines)
    {
        var requests = new List<SaleLineRequest>();
        foreach (var (name, price) in lines)
        {
            var product = await CreateTestProductAsync(name: name, unitPrice: price, initialStock: 10);
            requests.Add(new SaleLineRequest(product.Id, 1, price));
        }

        var response = await Client.PostAsJsonAsync("/sales", new CompleteSaleRequest(
            requests, [new SalePaymentRequest("Cash", requests.Sum(r => r.UnitPrice))]));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CompleteSaleResponse>(JsonOptions))!;
    }

    private async Task<SaleLinesPage> LinesAsync(string query) =>
        (await Client.GetFromJsonAsync<SaleLinesPage>($"/sales/lines?{query}", JsonOptions))!;

    [Fact]
    public async Task ListSaleLines_WithAMalformedDate_ReturnsBadRequest()
    {
        await AuthenticateAsManagerAsync();

        var response = await Client.GetAsync("/sales/lines?from=07-10-2026&to=2026-10-07");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListSaleLines_AsCashierForAnotherDay_ReturnsForbidden()
    {
        await AuthenticateAsCashierAsync();
        var day = Day(TwoDaysAgoUtc);

        var response = await Client.GetAsync($"/sales/lines?from={day}&to={day}");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListSaleLines_WithAPageSizeOverTheMaximum_ReturnsBadRequest()
    {
        await AuthenticateAsManagerAsync();

        var response = await Client.GetAsync($"/sales/lines?pageSize={SalesQueryRules.MaxPageSize + 1}");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListSaleLines_WithoutAuth_ReturnsUnauthorized()
    {
        ClearAuthentication();

        var response = await Client.GetAsync("/sales/lines");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListSaleLines_AfterASale_ReturnsItsLineWithTheBillNumber()
    {
        await AuthenticateAsCashierAsync();
        var sale = await SellAsync(("Traced Product", 120m));

        var page = await LinesAsync($"pageSize={SalesQueryRules.MaxPageSize}");

        page.Items.Should()
                  .Contain(l => l.InvoiceId == sale.InvoiceId
                             && l.InvoiceNumber == sale.InvoiceNumber
                             && l.ProductName == "Traced Product"
                             && l.Quantity == 1
                             && l.LineTotal == 120m);
    }

    [Fact]
    public async Task ListSaleLines_ForASale_MatchesTheBillDetailsLineTotal()
    {
        await AuthenticateAsManagerAsync();
        var sale = await SellAsync(("Detail Check", 75m));

        var detail = (await Client.GetFromJsonAsync<InvoiceDetailDto>($"/sales/{sale.InvoiceId}", JsonOptions))!;
        var page = await LinesAsync($"pageSize={SalesQueryRules.MaxPageSize}");

        page.Items.Single(l => l.InvoiceId == sale.InvoiceId).LineTotal.Should()
                                                                       .Be(detail.Lines.Single().LineTotal);
    }

    [Fact]
    public async Task ListSaleLines_AcrossPages_ReturnsEveryLineOnce()
    {
        await AuthenticateAsManagerAsync();
        var sale = await SellAsync(("Paged A", 10m), ("Paged B", 20m), ("Paged C", 30m));

        var lines = new List<SaleLineRowDto>();
        for (var page = 1; ; page++)
        {
            var result = await LinesAsync($"page={page}&pageSize=1");
            lines.AddRange(result.Items);
            if (!result.HasMore) break;
        }

        lines.Where(l => l.InvoiceId == sale.InvoiceId).Select(l => l.ProductName).Should()
                                                                                   .BeEquivalentTo(["Paged A", "Paged B", "Paged C"]);
    }

    [Fact]
    public async Task ListSaleLines_WithTwoSales_ListsTheOlderFirst()
    {
        await AuthenticateAsManagerAsync();
        var first = await SellAsync(("Order First", 10m));
        var second = await SellAsync(("Order Second", 10m));

        var page = await LinesAsync($"pageSize={SalesQueryRules.MaxPageSize}");
        var ids = page.Items.Select(l => l.InvoiceId).ToList();

        ids.IndexOf(first.InvoiceId).Should()
                                    .BeLessThan(ids.IndexOf(second.InvoiceId));
    }
```

Check these names before relying on them: `CompleteSaleResponse.InvoiceId` / `.InvoiceNumber`, `InvoiceDetailDto.Lines` / `.LineTotal`, and `CreateTestProductAsync`'s `name:` parameter. They are in `SaleDtos.cs`, the `CompleteSaleResponse` record and `IntegrationTestBase.cs:250`. Use the real names and record any difference as a ruling.

The today-only page holds every sale this shared test database made today. That is why each test finds its own rows by bill id, and why a full page, `MaxPageSize` (200), is enough.

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~SaleLinesEndpointsTests"`. Expected: build error, because `SaleLinesPage` does not exist yet.

- [ ] **Step 2: DTOs, query, handler, route**

`SaleDtos.cs`, appended:

```csharp
/// <summary>One sold line, with its bill, so a product can be traced to the bill it was sold on.</summary>
public record SaleLineRowDto(
    Guid InvoiceId,
    long InvoiceNumber,
    string Barcode,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    string? CategoryCode,
    DateTime CreatedUtc,
    string? Note);

public record SaleLinesPage(IReadOnlyList<SaleLineRowDto> Items, int Page, int PageSize, bool HasMore);
```

`ListSaleLinesQuery.cs`, using the same `using`s as `ListSalesQuery.cs`:

```csharp
namespace IndyPOS.Application.UseCases.StoreHub.Sales.History;

/// <param name="From">Null = today's business date.</param>
/// <param name="To">Null = today's business date.</param>
/// <param name="CanViewAnyDay">The caller holds reports.view; otherwise both dates must be today.</param>
public record ListSaleLinesQuery(DateOnly? From, DateOnly? To, int Page, int PageSize, bool CanViewAnyDay)
    : IQuery<SaleLinesPage>;
```

`ListSaleLinesQueryHandler.cs`: copy the `using` block of `ListSalesQueryHandler.cs` and add `using IndyPOS.Domain.Entities.Core;`.

```csharp
namespace IndyPOS.Infrastructure.QueryHandlers.Sales;

/// <summary>
/// Every sold line in the range, oldest bill first, under the same date rules as the bill list. The
/// order is total (bill time, bill number, line priority, line id), so paging never repeats or skips a line.
/// </summary>
public sealed class ListSaleLinesQueryHandler(StoreHubDbContext db, IStoreIdentityService storeIdentity, ICashDrawerClock clock)
    : IQueryHandler<ListSaleLinesQuery, SaleLinesPage>
{
    public async Task<SaleLinesPage> HandleAsync(ListSaleLinesQuery query, CancellationToken cancellationToken = default)
    {
        var today = clock.Now().BusinessDate;
        var from = query.From ?? today;
        var to = query.To ?? today;

        // Malformed input is a 400 before the today-only rule is a 403.
        SalesQueryRules.EnsureValidRange(from, to);
        SalesQueryRules.EnsureValidPage(query.Page, query.PageSize);
        TodayOnlyRule.EnsureAllowed(from, today, query.CanViewAnyDay);
        TodayOnlyRule.EnsureAllowed(to, today, query.CanViewAnyDay);

        var range = ReportDateRange.ToUtcRange(from, to, storeIdentity.TimeZone);
        var storeId = storeIdentity.StoreId;

        var rows = await db.Set<InvoiceLine>()
            .AsNoTracking()
            .Where(l => l.Invoice.StoreId == storeId
                     && l.Invoice.CreatedUtc >= range.StartUtc
                     && l.Invoice.CreatedUtc < range.EndExclusiveUtc)
            .OrderBy(l => l.Invoice.CreatedUtc)
            .ThenBy(l => l.Invoice.InvoiceNumber)
            .ThenBy(l => l.Priority)
            .ThenBy(l => l.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize + 1)
            .Select(l => new SaleLineRowDto(
                l.InvoiceId,
                l.Invoice.InvoiceNumber,
                l.Product.Barcode,
                l.ProductName,
                l.Quantity,
                l.UnitPrice,
                l.UnitPrice * l.Quantity,
                l.Product.Category,
                l.Invoice.CreatedUtc,
                l.Note))
            .ToListAsync(cancellationToken);

        return new SaleLinesPage(rows.Take(query.PageSize).ToList(), query.Page, query.PageSize, HasMore: rows.Count > query.PageSize);
    }
}
```

In `SaleQueryEndpoints.MapSaleQueries`, after the `""` route:

```csharp
        sales.MapGet("/lines", async (
            IQueryHandler<ListSaleLinesQuery, SaleLinesPage> handler,
            ClaimsPrincipal user,
            string? from,
            string? to,
            int? page,
            int? pageSize,
            CancellationToken cancellationToken) =>
        {
            var query = new ListSaleLinesQuery(
                From: SalesQueryRules.ParseDate(from),
                To: SalesQueryRules.ParseDate(to),
                Page: page ?? SalesQueryRules.FirstPage,
                PageSize: pageSize ?? SalesQueryRules.DefaultPageSize,
                CanViewAnyDay: CanViewAnyDay(user));

            return Results.Ok(await handler.HandleAsync(query, cancellationToken));
        });
```

Register `IQueryHandler<ListSaleLinesQuery, SaleLinesPage>` → `ListSaleLinesQueryHandler` exactly as `ListSalesQueryHandler` is registered.

- [ ] **Step 3: Pin the route**

In `RouteTableTests.MovedRoutes`, after `"POST /sales CanCompleteSales",`, add `"GET /sales/lines CanReprintSales",`.

The group's policy is `SalesEndpoints.Policy` (`"CanReprintSales"`). If the test prints the route differently, pin what it prints and record a ruling.

- [ ] **Step 4: Run, then commit**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~SaleLinesEndpointsTests|FullyQualifiedName~RouteTableTests|FullyQualifiedName~SalesHistoryEndpointsTests"`. Expected: all pass.

Commit: `feat(storehub): GET /sales/lines lists every sold line with its bill`

---

### Task 3: `/reports/pay-later` takes an optional date range

**Files:**
- Modify: `src/IndyPOS.Application/UseCases/StoreHub/Reports/GetPayLaterReport/GetPayLaterReportQuery.cs`
- Modify: `src/IndyPOS.Infrastructure/QueryHandlers/Reports/GetPayLaterReportQueryHandler.cs`
- Modify: `src/IndyPOS.StoreHub/Endpoints/Reports/ReportsEndpoints.cs` (`MapPayLaterReport`)
- Modify: `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/ReportsEndpointTests.cs`

**Interfaces (Produces):** `record GetPayLaterReportQuery(bool IncludeCompleted = false, int Page = 1, int PageSize = 50, DateOnly? FromDate = null, DateOnly? ToDate = null)`.

- [ ] **Step 1: Failing tests (in `ReportsEndpointTests`)**

```csharp
    private static string Day(DateTime utc) => DateOnly.FromDateTime(utc.ToLocalTime()).ToString("yyyy-MM-dd");

    private async Task SeedDebtAsync(string customer, DateTime createdUtc, decimal amount = 200m)
    {
        var invoice = await SeedInvoiceAsync(createdUtc, amount);
        await using var db = GetDbContext();
        var payment = new Payment { Id = Guid.NewGuid(), InvoiceId = invoice.Id, Method = "PayLater", Amount = amount, CreatedUtc = createdUtc };
        db.Payments.Add(payment);
        db.PayLaters.Add(new PayLater
        {
            Id = Guid.NewGuid(), PaymentId = payment.Id, InvoiceId = invoice.Id, Description = customer,
            PayLaterAmount = amount, PaidAmount = 0m, CreatedUtc = createdUtc, LastModifiedUtc = createdUtc
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetPayLaterReport_WithOnlyAFromDate_ReturnsBadRequest()
    {
        await AuthenticateAsManagerAsync();

        var response = await Client.GetAsync("/reports/pay-later?fromDate=2026-10-01");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPayLaterReport_WithASwappedRange_ReturnsBadRequest()
    {
        await AuthenticateAsManagerAsync();

        var response = await Client.GetAsync("/reports/pay-later?fromDate=2026-10-05&toDate=2026-10-01");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPayLaterReport_WithARange_LeavesOutADebtFromAnotherDay()
    {
        var oldDay = DateTime.UtcNow.AddDays(-2);
        var todaysCustomer = $"today-{Guid.NewGuid():N}";
        await SeedDebtAsync(todaysCustomer, DateTime.UtcNow);
        await AuthenticateAsManagerAsync();

        var report = await Client.GetFromJsonAsync<PayLaterReportDto>(
            $"/reports/pay-later?includeCompleted=true&pageSize=500&fromDate={Day(oldDay)}&toDate={Day(oldDay)}", JsonOptions);

        report!.Customers.Items.Should()
                               .NotContain(c => c.CustomerName == todaysCustomer);
    }

    [Fact]
    public async Task GetPayLaterReport_WithARange_ListsADebtFromThatDay()
    {
        var oldDay = DateTime.UtcNow.AddDays(-2);
        var customer = $"old-{Guid.NewGuid():N}";
        await SeedDebtAsync(customer, oldDay);
        await AuthenticateAsManagerAsync();

        var report = await Client.GetFromJsonAsync<PayLaterReportDto>(
            $"/reports/pay-later?includeCompleted=true&pageSize=500&fromDate={Day(oldDay)}&toDate={Day(oldDay)}", JsonOptions);

        report!.Customers.Items.Should()
                               .Contain(c => c.CustomerName == customer && c.RemainingBalance == 200m);
    }

    [Fact]
    public async Task GetPayLaterReport_WithoutDates_ListsADebtFromAnyDay()
    {
        var customer = $"any-{Guid.NewGuid():N}";
        await SeedDebtAsync(customer, DateTime.UtcNow.AddDays(-40));
        await AuthenticateAsManagerAsync();

        var report = await Client.GetFromJsonAsync<PayLaterReportDto>("/reports/pay-later?pageSize=500", JsonOptions);

        report!.Customers.Items.Should()
                               .Contain(c => c.CustomerName == customer);
    }
```

Add the `using`s: `IndyPOS.Domain.Entities.Core`, `IndyPOS.Application.UseCases.StoreHub.Reports.GetPayLaterReport`, and `System.Net.Http.Json` if missing. `GetDbContext()` is `IntegrationTestBase.cs:338`. If it is not `IAsyncDisposable`, open a scope the way `SeedInvoiceAsync` does, and record that as a ruling.

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~GetPayLaterReport"`. Expected:
- the one-date and swapped-range tests fail (200, not 400);
- `…LeavesOutADebtFromAnotherDay` fails, because there is no filter yet;
- the other two pass. They guard against an over-eager filter.

- [ ] **Step 2: Query, handler, route**

Query:

```csharp
/// <param name="FromDate">With <paramref name="ToDate"/>: only debts made in that store-day range.</param>
public record GetPayLaterReportQuery(
    bool IncludeCompleted = false,
    int Page = 1,
    int PageSize = 50,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null) : IQuery<PayLaterReportDto>;
```

Handler:
- add `IStoreIdentityService storeIdentity` to the constructor and store it as `_storeIdentity`;
- after the `IncludeCompleted` filter, add the code below.

```csharp
        if (query.FromDate is { } from && query.ToDate is { } to)
        {
            var range = ReportDateRange.ToUtcRange(from, to, _storeIdentity.TimeZone);
            baseQuery = baseQuery.Where(p => p.CreatedUtc >= range.StartUtc && p.CreatedUtc < range.EndExclusiveUtc);
        }
```

Route (`MapPayLaterReport`): add `DateOnly? fromDate, DateOnly? toDate` parameters, and before building the query:

```csharp
            if (fromDate.HasValue != toDate.HasValue)
            {
                return Results.BadRequest(new { error = "ต้องระบุทั้งวันที่เริ่มต้นและวันที่สิ้นสุด" });
            }

            if (fromDate is { } from && toDate is { } to && RejectInvalidRange(from, to) is { } rejection)
            {
                return rejection;
            }
```

Pass `FromDate: fromDate, ToDate: toDate` into the query.

- [ ] **Step 3: Run, then commit**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~ReportsEndpointTests"`. Expected: all pass.

Commit: `feat(storehub): the pay-later report takes an optional date range`

---

### Task 4: Till client methods and `PageReader`

**Files:**
- Modify: `src/IndyPOS.Application/Abstractions/StoreHub/IStoreHubClient.cs` (report section, after line 161)
- Modify: `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubHttpClient.cs` (after `GetLegacyPaymentsSummaryAsync`)
- Create: `src/IndyPOS.Application/Common/Models/PageReader.cs`
- Modify: `tests/IndyPOS.Application.Tests/Integration/StoreHub/StoreHubHttpClientTests.cs`
- Create: `tests/IndyPOS.Application.Tests/Models/PageReaderTests.cs`

**Interfaces (Produces):**
- `Task<SalesSummaryDto> GetSalesSummaryAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)`;
- `Task<SaleLinesPage> ListSaleLinesAsync(DateOnly fromDate, DateOnly toDate, int page, int pageSize, CancellationToken cancellationToken = default)`;
- `Task<PayLaterReportDto> GetPayLaterReportAsync(DateOnly? fromDate, DateOnly? toDate, int page, int pageSize, CancellationToken cancellationToken = default)`. It always asks with `includeCompleted=true`;
- `static class PageReader { const int MaxPages = 500; static Task<IReadOnlyList<T>> ReadAllAsync<T>(Func<int, Task<(IReadOnlyList<T> Items, bool HasMore)>> readPage); }`.

- [ ] **Step 1: Failing `PageReader` tests**

```csharp
using FluentAssertions;
using IndyPOS.Application.Common.Models;
using Xunit;

namespace IndyPOS.Application.Tests.Models;

public class PageReaderTests
{
    [Fact]
    public async Task ReadAllAsync_WhenPagesNeverEnd_Throws()
    {
        var act = () => PageReader.ReadAllAsync<int>(_ => Task.FromResult<(IReadOnlyList<int>, bool)>(([1], true)));

        await act.Should()
                 .ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ReadAllAsync_WithAnEmptyFirstPage_ReturnsNothing()
    {
        var items = await PageReader.ReadAllAsync<int>(_ => Task.FromResult<(IReadOnlyList<int>, bool)>(([], false)));

        items.Should()
             .BeEmpty();
    }

    [Fact]
    public async Task ReadAllAsync_WithThreePages_ReturnsEveryItemInOrder()
    {
        var items = await PageReader.ReadAllAsync<int>(page =>
            Task.FromResult<(IReadOnlyList<int>, bool)>(([page * 10, page * 10 + 1], page < 3)));

        items.Should()
             .Equal(10, 11, 20, 21, 30, 31);
    }
}
```

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~PageReaderTests"`. Expected: build error.

- [ ] **Step 2: `PageReader`**

```csharp
namespace IndyPOS.Application.Common.Models;

/// <summary>
/// Reads a paged list to the end. The cap stops a server that always says "more" from looping the till for
/// ever: 500 pages of 200 rows is far beyond any store's report.
/// </summary>
public static class PageReader
{
    public const int MaxPages = 500;

    public static async Task<IReadOnlyList<T>> ReadAllAsync<T>(Func<int, Task<(IReadOnlyList<T> Items, bool HasMore)>> readPage)
    {
        var all = new List<T>();
        for (var page = 1; page <= MaxPages; page++)
        {
            var (items, hasMore) = await readPage(page);
            all.AddRange(items);
            if (!hasMore)
                return all;
        }

        throw new InvalidOperationException($"The report did not end within {MaxPages} pages.");
    }
}
```

Run Step 1's command. Expected: 3 pass.

- [ ] **Step 3: Failing client tests (in `StoreHubHttpClientTests`)**

They follow `GetLegacySalesSummaryAsync_WithAThaiCulture_SendsGregorianDates`, using the file's `CaptureRequestUri` and `RunUnderThaiCultureAsync` helpers:

```csharp
    private static readonly PayLaterReportDto EmptyPayLaterReport =
        new(0m, 0m, 0, 0, new PagedResult<PayLaterSummaryDto>([], 0, 1, 200));

    [Fact]
    public async Task GetPayLaterReportAsync_WithoutDates_SendsNoDates()
    {
        _sut.SetAuthToken("valid-token");
        var requestUri = CaptureRequestUri(EmptyPayLaterReport);

        await _sut.GetPayLaterReportAsync(null, null, page: 1, pageSize: 200);

        requestUri().Should()
                    .NotContain("fromDate");
    }

    [Fact]
    public async Task GetPayLaterReportAsync_WithAThaiCulture_SendsGregorianDatesAndCompletedDebts()
    {
        _sut.SetAuthToken("valid-token");
        var requestUri = CaptureRequestUri(EmptyPayLaterReport);

        await RunUnderThaiCultureAsync(() => _sut.GetPayLaterReportAsync(
            new DateOnly(2026, 10, 2), new DateOnly(2026, 10, 3), page: 2, pageSize: 200));

        requestUri().Should()
                    .Contain("/reports/pay-later?includeCompleted=true&page=2&pageSize=200")
                    .And.Contain("fromDate=2026-10-02")
                    .And.Contain("toDate=2026-10-03");
    }

    [Fact]
    public async Task ListSaleLinesAsync_WithAThaiCulture_SendsGregorianDates()
    {
        _sut.SetAuthToken("valid-token");
        var requestUri = CaptureRequestUri(new SaleLinesPage([], 1, 200, false));

        await RunUnderThaiCultureAsync(() => _sut.ListSaleLinesAsync(
            new DateOnly(2026, 10, 2), new DateOnly(2026, 10, 3), page: 3, pageSize: 200));

        requestUri().Should()
                    .Contain("/sales/lines?from=2026-10-02&to=2026-10-03&page=3&pageSize=200");
    }

    [Fact]
    public async Task GetSalesSummaryAsync_WithAThaiCulture_SendsGregorianDates()
    {
        _sut.SetAuthToken("valid-token");
        var requestUri = CaptureRequestUri(new SalesSummaryDto(
            new DateOnly(2026, 10, 2), new DateOnly(2026, 10, 3), 0, 0m, new PaymentBreakdownDto(0, 0, 0, 0, 0, 0), []));

        await RunUnderThaiCultureAsync(() => _sut.GetSalesSummaryAsync(new DateOnly(2026, 10, 2), new DateOnly(2026, 10, 3)));

        requestUri().Should()
                    .Contain("/reports/sales-summary?fromDate=2026-10-02&toDate=2026-10-03");
    }
```

Add the `using`s: `IndyPOS.Application.UseCases.StoreHub.Reports`, `IndyPOS.Application.UseCases.StoreHub.Reports.GetPayLaterReport`, `IndyPOS.Application.UseCases.StoreHub.Sales.History`. If `RunUnderThaiCultureAsync` takes a `Func<Task>` rather than a generic one, wrap the calls as `async () => await …`.

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~StoreHubHttpClientTests"`. Expected: build error.

- [ ] **Step 4: Client methods**

`IStoreHubClient`, after `GetLegacyPaymentsSummaryAsync`:

```csharp
    /// <summary>The sales summary, with takings per payment method and service sales.</summary>
    Task<SalesSummaryDto> GetSalesSummaryAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);

    /// <summary>One page of sold lines, oldest bill first (GET /sales/lines).</summary>
    Task<SaleLinesPage> ListSaleLinesAsync(DateOnly fromDate, DateOnly toDate, int page, int pageSize,
                                           CancellationToken cancellationToken = default);

    /// <summary>One page of the outstanding-ลงบัญชี report, completed debts included; no dates = every day.</summary>
    Task<PayLaterReportDto> GetPayLaterReportAsync(DateOnly? fromDate, DateOnly? toDate, int page, int pageSize,
                                                   CancellationToken cancellationToken = default);
```

`StoreHubHttpClient`:

```csharp
    public Task<SalesSummaryDto> GetSalesSummaryAsync(DateOnly fromDate, DateOnly toDate,
                                                      CancellationToken cancellationToken = default) =>
        // No top products: the till never shows them, and they cost a query.
        SendAuthenticatedAsync<SalesSummaryDto>(HttpMethod.Get,
            $"/reports/sales-summary?{DateRangeQuery(fromDate, toDate)}&topProductsCount=0", content: null, cancellationToken);

    public Task<SaleLinesPage> ListSaleLinesAsync(DateOnly fromDate, DateOnly toDate, int page, int pageSize,
                                                  CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync<SaleLinesPage>(HttpMethod.Get,
            $"/sales/lines?from={IsoDate(fromDate)}&to={IsoDate(toDate)}&page={page}&pageSize={pageSize}",
            content: null, cancellationToken);

    public Task<PayLaterReportDto> GetPayLaterReportAsync(DateOnly? fromDate, DateOnly? toDate, int page, int pageSize,
                                                          CancellationToken cancellationToken = default)
    {
        // Completed debts too: the tab shows each customer's total ลงบัญชี as well as what is still owed.
        var url = $"/reports/pay-later?includeCompleted=true&page={page}&pageSize={pageSize}";
        if (fromDate is { } from && toDate is { } to)
            url += $"&{DateRangeQuery(from, to)}";

        return SendAuthenticatedAsync<PayLaterReportDto>(HttpMethod.Get, url, content: null, cancellationToken);
    }

    private static string IsoDate(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
```

Add the `using`s for the three DTO namespaces. `page` and `pageSize` are `int`s, so interpolation is culture-safe.

Run Step 3's command. Expected: all pass.

- [ ] **Step 5: Commit**

`feat(till): client methods for the sales summary, sold lines and the pay-later report`

---

### Task 5: `TillLayout` report flags, `MoneyRows`, and `MenuLayout.Row`

**Files:**
- Modify: `src/IndyPOS.Application/Common/Models/TillLayout.cs`
- Create: `src/IndyPOS.Application/Common/Models/MoneyRows.cs`
- Modify: `tests/IndyPOS.Application.Tests/Models/TillLayoutTests.cs`
- Create: `tests/IndyPOS.Application.Tests/Models/MoneyRowsTests.cs`
- Modify: `src/IndyPOS.Windows.Forms/UI/MenuLayout.cs`
- Modify: `tests/IndyPOS.Windows.Forms.Tests/UI/MenuLayoutTests.cs`

**Interfaces (Produces):**
- `TillLayout(bool ShowHardwareButton, bool ShowServiceButtons, bool ShowAccountsReceivableMenu, bool ShowPayLaterReports, bool ShowProductTypeSplit)`:
  - `ShowPayLaterReports` covers the outstanding tab and the ลงบัญชี tiles;
  - `ShowProductTypeSplit` covers the products-sold filter and category column, and the general/hardware tiles;
- `record MoneyRow(string Title, decimal Amount)` and `static class MoneyRows { static IReadOnlyList<MoneyRow> From(SalesSummaryDto summary); }`;
- `MenuLayout.Row(IReadOnlyList<Control> shownLeftToRight, IReadOnlyList<int> slotLefts)`.

- [ ] **Step 1: Failing tests**

`TillLayoutTests`: every `new TillLayout(...)` gains the two new arguments.
- MimyShop and MimyMart: `ShowPayLaterReports: false, ShowProductTypeSplit: false`.
- GeneralHardware and `WhenFeaturesUnavailable`: `ShowPayLaterReports: true, ShowProductTypeSplit: true`.

`MoneyRowsTests`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.StoreHub.Reports;
using Xunit;

namespace IndyPOS.Application.Tests.Models;

public class MoneyRowsTests
{
    private static SalesSummaryDto Summary(IReadOnlyList<PaymentMethodTotalDto> methods, IReadOnlyList<ServiceSaleDto> services) =>
        new(new DateOnly(2026, 10, 7), new DateOnly(2026, 10, 7), 0, 0m, new PaymentBreakdownDto(0, 0, 0, 0, 0, 0), [])
        {
            PaymentsByMethod = methods,
            ServiceSales = services
        };

    [Fact]
    public void From_WithNoMethodsOrServices_ReturnsNothing()
    {
        MoneyRows.From(Summary([], [])).Should()
                                       .BeEmpty();
    }

    [Fact]
    public void From_WithMethodsAndServices_ListsMethodsThenServicesByTheirNames()
    {
        var rows = MoneyRows.From(Summary(
            [new PaymentMethodTotalDto("Cash", "เงินสด", 100m), new PaymentMethodTotalDto("MoneyTransfer", "เงินโอน", 50m)],
            [new ServiceSaleDto("2002500000014", "จัดส่ง", 30m)]));

        rows.Should()
            .Equal(new MoneyRow("เงินสด", 100m), new MoneyRow("เงินโอน", 50m), new MoneyRow("จัดส่ง", 30m));
    }
}
```

`MenuLayoutTests`, appended:

```csharp
    private static readonly int[] TabSlots = [13, 262, 511, 755, 999];

    [Fact]
    public void Row_WithMoreButtonsThanSlots_Throws()
    {
        var act = () => MenuLayout.Row(Buttons(6), TabSlots);

        act.Should()
           .Throw<ArgumentException>();
    }

    [Fact]
    public void Row_WithATabLeftOut_MovesTheTabsAfterItLeft()
    {
        var tabs = Enumerable.Range(0, 5).Select(i => (Control)new Button { Left = TabSlots[i] }).ToList();

        MenuLayout.Row([tabs[0], tabs[1], tabs[2], tabs[4]], TabSlots);

        tabs[4].Left.Should()
                    .Be(755);
    }
```

Run:
- `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~TillLayoutTests|FullyQualifiedName~MoneyRowsTests"`;
- `dotnet test tests/IndyPOS.Windows.Forms.Tests --filter "FullyQualifiedName~MenuLayoutTests"`.

Expected: build errors.

- [ ] **Step 2: Implement**

`TillLayout.cs`:

```csharp
public sealed record TillLayout(
    bool ShowHardwareButton,
    bool ShowServiceButtons,
    bool ShowAccountsReceivableMenu,
    bool ShowPayLaterReports,
    bool ShowProductTypeSplit)
{
    public static TillLayout WhenFeaturesUnavailable { get; } = new(
        ShowHardwareButton: true,
        ShowServiceButtons: false,
        ShowAccountsReceivableMenu: true,
        ShowPayLaterReports: true,
        ShowProductTypeSplit: true);

    public static TillLayout For(StoreFeaturesDto features) => new(
        ShowHardwareButton: features.MultipleProductTypesEnabled,
        ShowServiceButtons: features.ServiceProductsEnabled,
        ShowAccountsReceivableMenu: features.PayLaterEnabled,
        ShowPayLaterReports: features.PayLaterEnabled,
        ShowProductTypeSplit: features.MultipleProductTypesEnabled);
}
```

Keep the existing XML comments. Add a one-line summary to each new parameter:
- `ShowPayLaterReports`: the outstanding ลงบัญชี tab and the ลงบัญชี tiles;
- `ShowProductTypeSplit`: the general/hardware tiles, the products-sold filter and its category column.

`MoneyRows.cs`:

```csharp
using IndyPOS.Application.UseCases.StoreHub.Reports;

namespace IndyPOS.Application.Common.Models;

/// <summary>One line of the overview's money section: a payment method's takings, or a service's sales.</summary>
public sealed record MoneyRow(string Title, decimal Amount);

/// <summary>The overview's money section, in the spec's order: payment methods, then services.</summary>
public static class MoneyRows
{
    public static IReadOnlyList<MoneyRow> From(SalesSummaryDto summary) =>
        summary.PaymentsByMethod.Select(m => new MoneyRow(m.DisplayName, m.Total))
               .Concat(summary.ServiceSales.Select(s => new MoneyRow(s.Name, s.Total)))
               .ToList();
}
```

`MenuLayout.cs`, appended:

```csharp
    /// <summary>The same for a row of tabs: puts the shown tabs into the row's slots, left first.</summary>
    public static void Row(IReadOnlyList<Control> shownLeftToRight, IReadOnlyList<int> slotLefts)
    {
        if (shownLeftToRight.Count > slotLefts.Count)
            throw new ArgumentException("More tabs than tab slots.", nameof(shownLeftToRight));

        for (var i = 0; i < shownLeftToRight.Count; i++)
            shownLeftToRight[i].Left = slotLefts[i];
    }
```

Run Step 1's commands. Expected: all pass. Then run `dotnet build src/IndyPOS.Windows.Forms`. Expected: 0 errors, since `TillLayout`'s positional constructor is only called in Application.

- [ ] **Step 3: Commit**

`feat(app): TillLayout's report flags and the overview's money rows`

---

### Task 6: Overview panel: tiles and money rows per store

**Files:**
- Modify: `src/IndyPOS.Windows.Forms/UI/Report/SalesReportPanel.Designer.cs`
- Modify: `src/IndyPOS.Windows.Forms/UI/Report/SalesReportPanel.cs`
- Create: `src/IndyPOS.Windows.Forms/UI/Report/MoneyRowView.cs`

- [ ] **Step 1: Delete the six fixed money rows from the designer**

Run this script. It keeps the BOM and CRLF, and removes every line that names one of the controls, plus each one's `// name` comment block:

```python
import re
p = 'src/IndyPOS.Windows.Forms/UI/Report/SalesReportPanel.Designer.cs'
s = open(p, encoding='utf-8-sig', newline='').read().replace('\r\n', '\n')
names = ['panel3', 'panel8', 'panel10', 'panel11', 'panel12', 'panel13',
         'label14', 'label16', 'label18', 'label20', 'label22', 'label6',
         'PaymentByTransferLabel', 'PaymentByKlkLabel', 'PaymentByWelfareCardLabel',
         'PaymentByM33Label', 'PaymentByWeWinLabel', 'PaymentByArLabel']
alt = '|'.join(names)
s = re.sub(rf'            // \n            // ({alt})\n            // \n', '', s)
s = '\n'.join(l for l in s.split('\n') if not re.search(rf'(\b|this\.)({alt})\b', l)) + ''
open(p, 'w', encoding='utf-8-sig', newline='').write(s.replace('\n', '\r\n'))
```

The `\b` boundary keeps `panel1`, `panel14`, `label13` and the other tiles' controls. After running it, check:
- `git diff --stat` shows deletions only, about 230 lines;
- `grep -n "panel9.Controls.Add" SalesReportPanel.Designer.cs` lists only `label13`.

- [ ] **Step 2: Add the `FlowLayoutPanel`**

In the designer:
- `MoneyRowsPanel = new FlowLayoutPanel();` next to the other `new` lines;
- `panel9.Controls.Add(MoneyRowsPanel);` before `panel9.Controls.Add(label13);`;
- a property block:

```csharp
            // 
            // MoneyRowsPanel
            // 
            MoneyRowsPanel.AutoScroll = true;
            MoneyRowsPanel.Location = new Point(16, 39);
            MoneyRowsPanel.Name = "MoneyRowsPanel";
            MoneyRowsPanel.Size = new Size(1240, 170);
            MoneyRowsPanel.TabIndex = 100;
```

- the field `private FlowLayoutPanel MoneyRowsPanel;`.

- [ ] **Step 3: `MoneyRowView`, the old rows' exact styling**

```csharp
using IndyPOS.Application.Common.Models;

namespace IndyPOS.Windows.Forms.UI.Report;

/// <summary>Draws one money row as the designer drew the fixed ones: caption left, amount right.</summary>
internal static class MoneyRowView
{
    private const string FontName = "FC Subject [Non-commercial] Reg";
    private static readonly Color RowBack = Color.FromArgb(34, 34, 34);
    private static readonly Color LabelBack = Color.FromArgb(35, 35, 35);

    public static Control Create(MoneyRow row)
    {
        var panel = new Panel { BackColor = RowBack, Size = new Size(406, 45), Margin = new Padding(0, 0, 6, 6) };
        panel.Controls.Add(new Label
        {
            BackColor = LabelBack, Dock = DockStyle.Fill, ForeColor = Color.Gainsboro,
            Font = new Font(FontName, 15.75F, FontStyle.Regular, GraphicsUnit.Point),
            Text = $"{row.Amount:N2}", TextAlign = ContentAlignment.MiddleRight
        });
        panel.Controls.Add(new Label
        {
            BackColor = LabelBack, Dock = DockStyle.Left, Width = 196, ForeColor = Color.Gainsboro,
            Font = new Font(FontName, 12F, FontStyle.Regular, GraphicsUnit.Point),
            Text = row.Title, TextAlign = ContentAlignment.MiddleCenter
        });
        return panel;
    }
}
```

The Fill label is added first, so it docks last, the same as the designer's order.

- [ ] **Step 4: `SalesReportPanel.cs`**
  - **Constructor:** it becomes `(IReportService reportService, IStoreHubClient storeHubClient, IStoreFeaturesProvider storeFeatures, MessageForm messageForm)`. Store the new two.
  - **Tiles:** add a field that groups them by the features they need:

```csharp
    // Which tiles need which store features. The others ("ยอดขาย : ทั้งหมด") show for every store.
    private Control[] LedgerTiles => [panel14, panel21, panel17, panel18];
    private Control[] ProductTypeSplitTiles => [panel4, panel5];
    private Control[] SplitLedgerTiles => [panel20, panel22, panel19, panel16];

    private async Task ApplyStoreLayoutAsync()
    {
        TillLayout layout;
        try
        {
            layout = TillLayout.For(await _storeFeatures.GetAsync());
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not load store features for the sales overview");
            layout = TillLayout.WhenFeaturesUnavailable;
        }

        foreach (var tile in LedgerTiles) tile.Visible = layout.ShowPayLaterReports;
        foreach (var tile in ProductTypeSplitTiles) tile.Visible = layout.ShowProductTypeSplit;
        foreach (var tile in SplitLedgerTiles) tile.Visible = layout.ShowPayLaterReports && layout.ShowProductTypeSplit;
    }
```

  - **`ShowSummary(SalesSummary salesSummary, SalesSummaryDto summary)`:**
    - keep the 11 tile assignments;
    - delete the six `PaymentBy…` lines;
    - end with the money rows:

```csharp
        MoneyRowsPanel.SuspendLayout();
        MoneyRowsPanel.Controls.Clear();
        foreach (var row in MoneyRows.From(summary))
            MoneyRowsPanel.Controls.Add(MoneyRowView.Create(row));
        MoneyRowsPanel.ResumeLayout();
```

  - **`ShowReportByPeriodAsync` / `ShowReportByDateRangeAsync`:**
    - start with `await ApplyStoreLayoutAsync();`;
    - replace the payments call with `await _storeHubClient.GetSalesSummaryAsync(start, end)`;
    - for a period, get the dates from `period.ToDateRange()` (`IndyPOS.Application.Common.Extensions`), as `StoreHubReportService` does;
    - delete the two `GetPaymentsReportBy…` helpers.
  - **`using`s:** `IndyPOS.Application.Abstractions.StoreHub`, `IndyPOS.Application.UseCases.StoreHub.Reports`, `IndyPOS.Windows.Forms.Services`, `Serilog`.

- [ ] **Step 5: Build and the WinForms suite, then commit**

Run: `dotnet build src/IndyPOS.Windows.Forms` → 0 errors; `dotnet test tests/IndyPOS.Windows.Forms.Tests` → all pass.

Commit: `feat(winforms): the sales overview shows each store's tiles and its payment-method and service rows`

---

### Task 7: Products sold, from `/sales/lines`

**Files:**
- Modify: `src/IndyPOS.Windows.Forms/UI/Report/InvoiceProductsReportPanel.cs`
- Modify: `src/IndyPOS.Application/Common/Interfaces/IReportService.cs` (remove `GetInvoiceProductsByDateAsync` and `GetInvoiceProductsByDateRangeAsync`)
- Modify: `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubReportService.cs` (remove their stubs)

- [ ] **Step 1: The panel reads sold lines**
  - **Constructor:** it becomes `(IStoreHubClient storeHubClient, IStoreFeaturesProvider storeFeatures, MessageForm messageForm)`. Drop `IReportService`.
  - **Data:** `_products` becomes `IReadOnlyList<SaleLineRowDto>`, initially `[]`. `GetInvoiceProductsAsync` becomes:

```csharp
	private const int LinesPerPage = 200;

	private async Task<IReadOnlyList<SaleLineRowDto>> GetSoldLinesAsync()
	{
		var startDate = StartDatePicker.Value.ToDateOnly();
		var endDate = EndDatePicker.Value.ToDateOnly();

		await RefreshHardwareCodesAsync();
		await ApplyStoreLayoutAsync();

		return await PageReader.ReadAllAsync(async page =>
		{
			var result = await _storeHubClient.ListSaleLinesAsync(startDate, endDate, page, LinesPerPage);
			return (result.Items, result.HasMore);
		});
	}
```

  - **Classification:** `IsHardwareProductGroup(SaleLineRowDto line) => !string.IsNullOrEmpty(line.CategoryCode) && _hardwareCodes.Contains(line.CategoryCode);`. `IsGeneralProductGroup` takes `SaleLineRowDto` too.
  - **`AddProductToInvoiceDataView(SaleLineRowDto line)`:** the row becomes:
    - `InvoiceId` → `line.InvoiceNumber`;
    - `ProductCode` → `line.Barcode`;
    - `Description` → `line.ProductName`;
    - `Quantity` → `line.Quantity`;
    - `UnitPrice` → `line.UnitPrice`;
    - `Total` → `line.LineTotal`;
    - `Category` → hardware or general, as today;
    - `DateCreated` → `line.CreatedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")`;
    - `Note` → `line.Note`.
  - **Column header:** the `InvoiceId` column's header becomes `"เลขที่บิล"`.
  - **On/off:**

```csharp
	private async Task ApplyStoreLayoutAsync()
	{
		TillLayout layout;
		try
		{
			layout = TillLayout.For(await _storeFeatures.GetAsync());
		}
		catch (Exception ex)
		{
			Log.Warning(ex, "Could not load store features for products sold");
			layout = TillLayout.WhenFeaturesUnavailable;
		}

		groupBox1.Visible = layout.ShowProductTypeSplit;
		InvoiceProductsDataView.Columns[(int)ProductColumn.Category].Visible = layout.ShowProductTypeSplit;
		if (!layout.ShowProductTypeSplit)
			AllProductGroupsButton.Checked = true;
	}
```

  - **Rename the call sites** from `GetInvoiceProductsAsync` to `GetSoldLinesAsync`. The cache check `!_products.Any()` becomes `_products.Count == 0`.
  - **`using`s:** `IndyPOS.Application.Common.Models`, `IndyPOS.Application.UseCases.StoreHub.Sales.History`, `IndyPOS.Windows.Forms.Services`, `Serilog`.

- [ ] **Step 2: Remove the dead stubs**
  - Delete `GetInvoiceProductsByDateAsync` and `GetInvoiceProductsByDateRangeAsync` from `IReportService` and `StoreHubReportService`.
  - Run `grep -rn "GetInvoiceProductsByDate" src tests`. Expected: no matches.

- [ ] **Step 3: Build, suites, commit**

Run: `dotnet build src/IndyPOS.Windows.Forms` → 0 errors; `dotnet test tests/IndyPOS.Windows.Forms.Tests` and `dotnet test tests/IndyPOS.Application.Tests` → all pass.

Commit: `feat(winforms): products sold lists every sold line with its bill number`

---

### Task 8: Outstanding ลงบัญชี, from `/reports/pay-later`

**Files:**
- Modify: `src/IndyPOS.Windows.Forms/UI/Report/PayLaterPaymentsReportPanel.cs`
- Modify: `IReportService.cs` and `StoreHubReportService.cs` (remove `GetPayLaterPaymentsAsync`; **keep** `GetPayLaterPaymentsByPeriodAsync`, which cash flow still calls, slice 3)

- [ ] **Step 1: The panel reads the report**
  - **Constructor:** it becomes `(IStoreHubClient storeHubClient, MessageForm messageForm)`.
  - **Reading every page:**

```csharp
    private const int CustomersPerPage = 200;

    private async Task<IReadOnlyList<PayLaterSummaryDto>> GetCustomersAsync(DateOnly? fromDate, DateOnly? toDate) =>
        await PageReader.ReadAllAsync(async page =>
        {
            var report = await _storeHubClient.GetPayLaterReportAsync(fromDate, toDate, page, CustomersPerPage);
            return (report.Customers.Items, report.Customers.HasNextPage);
        });
```

  - **Period:** `ShowPaymentsByPeriodAsync` does `var range = period.ToDateRange();` and then `ShowCustomers(await GetCustomersAsync(range.StartDate, range.EndDate))`.
  - **ทั้งหมด:** the button calls `ShowCustomers(await GetCustomersAsync(null, null))`.
  - **`ShowPayments` becomes `ShowCustomers(IEnumerable<PayLaterSummaryDto> customers)`:**
    - it clears the rows;
    - for each customer with `RemainingBalance > 0` it adds `[CustomerName, TotalOwed, RemainingBalance]` into the existing three columns, with the same alternating row colours.
  - **Delete** `AddToPayLaterPaymentsSummaryDataView`.

- [ ] **Step 2: Remove the dead stub**
  - Delete `GetPayLaterPaymentsAsync` from `IReportService` and `StoreHubReportService`.
  - Run `grep -rn "GetPayLaterPaymentsAsync\b" src tests`. Expected: no matches.

- [ ] **Step 3: Build, suites, commit**

Run: `dotnet build src/IndyPOS.Windows.Forms` → 0 errors; `dotnet test tests/IndyPOS.Windows.Forms.Tests` → all pass.

Commit: `feat(winforms): the outstanding ลงบัญชี tab lists each customer's debt`

---

### Task 9: Report tabs per store

**Files:**
- Modify: `src/IndyPOS.Windows.Forms/UI/Report/ReportsPanel.cs`

- [ ] **Step 1: Hide the ลงบัญชี tab without a gap**
  - **Constructor:** add `IStoreFeaturesProvider storeFeatures` and store it.
  - **Add:**

```csharp
    // The tab buttons' lefts as laid out, after any display scaling. Recorded on first use.
    private int[]? _tabSlotLefts;

    private Control[] AllTabs() =>
        [ShowSalesOverviewReportButton, ShowInvoiceProductsButton, ShowSalesHistoryButton,
         PayLaterPaymentsReportButton, CashFlowCalculatorButton];

    private async Task ApplyStoreLayoutAsync()
    {
        TillLayout layout;
        try
        {
            layout = TillLayout.For(await _storeFeatures.GetAsync());
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not load store features for the report tabs");
            layout = TillLayout.WhenFeaturesUnavailable;
        }

        _tabSlotLefts ??= AllTabs().Select(tab => tab.Left).ToArray();
        PayLaterPaymentsReportButton.Visible = layout.ShowPayLaterReports;
        MenuLayout.Row(AllTabs().Where(tab => tab != PayLaterPaymentsReportButton || layout.ShowPayLaterReports).ToArray(),
                       _tabSlotLefts);
    }
```

  - **`ActivePanel_VisibleChanged`** becomes `async void`, and calls `await ApplyStoreLayoutAsync();` before `SwitchToPanel(ReportSubPanel.SalesReport);`.
  - **`using`s:** `IndyPOS.Application.Common.Models`, `IndyPOS.Windows.Forms.Services`, `Serilog`.

- [ ] **Step 2: Build, suite, commit**

Run: `dotnet build src/IndyPOS.Windows.Forms` → 0 errors; `dotnet test tests/IndyPOS.Windows.Forms.Tests` → all pass.

Commit: `feat(winforms): the outstanding ลงบัญชี report tab only for stores with PayLater`

---

### Task 10: By hand, docs and counts

- [ ] **Step 1: The till starts**
  - Launch it against `--store MimyShop` the way slice 1 did: run `dotnet run --project src/IndyPOS.AppHost -- --store MimyShop`, then start `IndyPOS.Windows.Forms.exe` with `StoreHub__BaseUrl=http://localhost:5014`, `Store__ConfigPath` and `IndyPOS_TillLogsDirectory` from `src/IndyPOS.AppHost/obj/dev-stores/MimyShop/`.
  - Check the window opens and the log has no new `Fatal`.
  - Check, as `manager` / `manager123`:
    - `/reports/sales-summary?fromDate=<today>&toDate=<today>` has `paymentsByMethod` and `serviceSales`;
    - `/sales/lines` answers 200.
  - Stop the till and the AppHost.

- [ ] **Step 2: Pond's check, for the PR**
  - **MimyShop:**
    - **Overview:** only "ยอดขาย : ทั้งหมด", then rows เงินสด, เงินโอน, บัตรสวัสดิการแห่งรัฐ, คนละครึ่ง, จัดส่ง, เอกสาร.
    - **No** ยอดการลงบัญชีค้างชำระ tab; the นับเงินสด tab sits in its place.
    - **Products sold:** shows today's lines with bill numbers, and no hardware filter or category column.
  - **MimyMart:** the overview rows are เงินสด and เงินโอน. There is no ลงบัญชี tab.
  - **GeneralHardware:**
    - **Overview:** every tile shows.
    - **Money rows:** เงินสด, เงินโอน, บัตรสวัสดิการแห่งรัฐ, ลงบัญชี, คนละครึ่ง, plus เราชนะ / ม.33 only in a range where they were used.
    - **ลงบัญชี tab:** lists owing customers. ทั้งหมด and the period buttons work.
    - **Products sold:** has its filter.

- [ ] **Step 3: Docs and measured counts**
  - **ONBOARDING "Running as a store":** extend the "till follows the store type" bullet with the report differences.
  - **`docs/architecture/api-conventions.md`:** none needed. `/sales/lines` follows the existing rules.
  - **Counts:**
    - run the whole solution with `--logger trx`;
    - write the measured per-suite and total counts into CLAUDE.md and ONBOARDING;
    - the new StoreHub tests all need a container, so the Docker-down figures in both files rise by the StoreHub delta.

Commit: `docs: per-store reports; measured counts`

---

## Self-Review

**Spec coverage:**
- §4.2:
  - `paymentsByMethod` with the enabled-or-used rule and display order: Task 1;
  - `serviceSales`: Task 1;
  - the fixed `PaymentBreakdown` is kept: Task 1 does not touch it;
  - overview order (sales, split, ลงบัญชี, methods, services): Tasks 5 and 6;
  - the outstanding tab only with PayLater: Task 9;
  - the products-sold filter only with multiple types: Task 7.
- §4.2a:
  - `/sales/lines` with its rules, row fields, order and paging: Task 2;
  - the till reads every page: Tasks 4 and 7;
  - `/reports/pay-later` dates: Task 3;
  - the till with `includeCompleted=true` and the owing filter: Tasks 4 and 8.
- §5 tests:
  - StoreHub, once per store: Task 1;
  - the ViewModel layer (`TillLayout`, `MoneyRows`, `PageReader`): Tasks 4 and 5;
  - by hand: Task 10.
- §6 additive: the new DTO properties default to `[]`, the query params are optional, and there is no migration.

**Placeholders:** none. Two steps ask the executor to check a name (`CompleteSaleResponse`, `GetDbContext`), and each one names the exact file and line to read.

**Type consistency:**
- `PaymentMethodTotalDto`, `ServiceSaleDto`, `SaleLineRowDto`, `SaleLinesPage` and `ListSaleLinesQuery` are used the same way in every task;
- `TillLayout`'s five parameters are the same throughout;
- so are `MoneyRow` / `MoneyRows.From`, `PageReader.ReadAllAsync` and `MenuLayout.Row`;
- the client methods' signatures match between Tasks 4, 6, 7 and 8.

**Review Focus:** each of the five lines has its test in the owning task: items 1 and 2 in Task 2, item 3 in Task 1, item 4 (`ReadAllAsync_WhenPagesNeverEnd_Throws`) and item 5 (the Thai-culture client tests) in Task 4.
