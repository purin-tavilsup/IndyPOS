using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.UseCases.StoreHub.Sales;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

/// <summary>
/// GET /sales, /sales/{id}, /sales/{number}. Includes the tests moved from the retired
/// /reports/invoices routes (spec §6).
/// </summary>
[Collection("Integration")]
public class SalesHistoryEndpointsTests : IntegrationTestBase
{
    /// <summary>Matches <c>SalesReprintCapabilityTests.UnknownRoleId</c> — no role maps it to sales.reprint.</summary>
    private const int RoleWithoutSalesReprint = 99;
    private const int PageSizeAboveMaximum = SalesQueryRules.MaxPageSize + 1;
    private const string OverflowingNumber = "99999999999999999999";

    // 32 digits: a valid "N"-format GUID that also parses as the long 1, so it fits both typed routes.
    private const string DigitsOnlyGuid = "00000000000000000000000000000001";

    public SalesHistoryEndpointsTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Now);

    /// <summary>Two days back, so a run near midnight cannot land it on "today".</summary>
    private static DateTime TwoDaysAgoUtc => DateTime.UtcNow.AddDays(-2);

    private static string Day(DateTime utc) => DateOnly.FromDateTime(utc.ToLocalTime()).ToString("yyyy-MM-dd");

    private sealed record ErrorBody(string Error);

    private void UseToken(string token) =>
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private async Task<CompleteSaleResponse> SellOneAsync()
    {
        var product = await CreateTestProductAsync(unitPrice: 350m, initialStock: 10);
        var seller = await CreateTestUserAsync($"seller_{Guid.NewGuid():N}", "Password123!");
        var response = await Client.PostAsJsonAsync("/sales/complete", new CompleteSaleRequest(
            seller.Id, [new SaleLineRequest(product.Id, 1, 350m)], [new SalePaymentRequest("Cash", 500m)]));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CompleteSaleResponse>(JsonOptions))!;
    }

    // ---- negative: authentication and capability ----

    [Fact]
    public async Task ListSales_WithoutAuth_ReturnsUnauthorized()
    {
        ClearAuthentication();

        var response = await Client.GetAsync("/sales");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListSales_WithTokenMissingUserId_ReturnsUnauthorized()
    {
        UseToken(TokenWithoutUserId(UserRole.Cashier));

        var response = await Client.GetAsync("/sales");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListSales_WithRoleLackingSalesReprint_ReturnsForbidden()
    {
        UseToken(TokenWithRole(RoleWithoutSalesReprint));

        var response = await Client.GetAsync("/sales");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetSaleById_WithoutAuth_ReturnsUnauthorized()
    {
        ClearAuthentication();

        var response = await Client.GetAsync($"/sales/{Guid.NewGuid()}");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Unauthorized);
    }

    // ---- negative: today-only ----

    [Fact]
    public async Task ListSales_AsCashierForAnotherDay_ReturnsForbidden()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.GetAsync($"/sales?from={Day(TwoDaysAgoUtc)}&to={Day(TwoDaysAgoUtc)}");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListSales_AsCashierForAnotherDay_ReturnsAThaiError()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.GetAsync($"/sales?from={Day(TwoDaysAgoUtc)}&to={Day(TwoDaysAgoUtc)}");

        (await response.Content.ReadFromJsonAsync<ErrorBody>(JsonOptions))!.Error.Should()
                                                                          .Contain("วันนี้");
    }

    [Fact]
    public async Task GetSaleByNumber_AsCashierForAnotherDaysBill_ReturnsNotFound()
    {
        var old = await SeedInvoiceAsync(TwoDaysAgoUtc);
        await AuthenticateAsCashierAsync();

        var response = await Client.GetAsync($"/sales/{old.InvoiceNumber}");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSaleById_AsCashierForAnotherDaysBill_ReturnsNotFound()
    {
        var old = await SeedInvoiceAsync(TwoDaysAgoUtc);
        await AuthenticateAsCashierAsync();

        var response = await Client.GetAsync($"/sales/{old.Id}");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.NotFound);
    }

    // ---- negative: malformed input ----

    [Fact]
    public async Task ListSales_WithAMalformedFrom_ReturnsBadRequest()
    {
        await AuthenticateAsManagerAsync();

        var response = await Client.GetAsync("/sales?from=27/09/2026");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListSales_WithToBeforeFrom_ReturnsBadRequest()
    {
        await AuthenticateAsManagerAsync();

        var response = await Client.GetAsync($"/sales?from={Today:yyyy-MM-dd}&to={Today.AddDays(-5):yyyy-MM-dd}");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListSales_WithPageSizeAboveMaximum_ReturnsBadRequest()
    {
        await AuthenticateAsManagerAsync();

        var response = await Client.GetAsync($"/sales?pageSize={PageSizeAboveMaximum}");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetSaleByNumber_WithNonNumericValue_ReturnsBadRequest()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.GetAsync("/sales/abc");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    // The /{value} catch-all must stay inside the authorised group: an anonymous caller gets 401,
    // never the 400 that would confirm the route exists.
    [Fact]
    public async Task GetSaleByNumber_WithNonNumericValueWithoutAuth_ReturnsUnauthorized()
    {
        ClearAuthentication();

        var response = await Client.GetAsync("/sales/abc");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSaleByNumber_WithOverflowingValue_ReturnsBadRequest()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.GetAsync($"/sales/{OverflowingNumber}");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetSaleByNumber_WithNegativeNumber_ReturnsBadRequest()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.GetAsync("/sales/-3");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetSale_WithADigitsOnlyGuid_ReturnsNotFound()
    {
        await AuthenticateAsManagerAsync();

        var response = await Client.GetAsync($"/sales/{DigitsOnlyGuid}");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSaleByNumber_WithUnknownNumber_ReturnsNotFound()
    {
        await AuthenticateAsManagerAsync();

        var response = await Client.GetAsync($"/sales/{long.MaxValue}");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSaleById_WithUnknownId_ReturnsNotFound()
    {
        await AuthenticateAsManagerAsync();

        var response = await Client.GetAsync($"/sales/{Guid.NewGuid()}");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReportsInvoices_AfterRetirement_ReturnsNotFound()
    {
        await AuthenticateAsManagerAsync();

        var response = await Client.GetAsync($"/reports/invoices?fromDate={Today:yyyy-MM-dd}&toDate={Today:yyyy-MM-dd}");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.NotFound);
    }

    // ---- positive ----

    [Fact]
    public async Task ListSales_Always_OmitsAPageTotal()
    {
        await AuthenticateAsManagerAsync();

        var response = await Client.GetAsync("/sales");

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.EnumerateObject().Select(p => p.Name).Should()
                                                              .BeEquivalentTo(["items", "page", "pageSize", "hasMore"]);
    }

    [Fact]
    public async Task ListSales_WithoutPageSize_UsesTheDefault()
    {
        await AuthenticateAsManagerAsync();

        var page = await Client.GetFromJsonAsync<SalesPage>("/sales", JsonOptions);

        page!.PageSize.Should()
                      .Be(SalesQueryRules.DefaultPageSize);
    }

    [Fact]
    public async Task ListSales_WithAPageSize_EchoesIt()
    {
        await AuthenticateAsManagerAsync();

        var page = await Client.GetFromJsonAsync<SalesPage>("/sales?page=1&pageSize=10", JsonOptions);

        page!.PageSize.Should()
                      .Be(10);
    }

    [Fact]
    public async Task ListSales_AsCashierWithoutDates_IncludesTheSaleJustMade()
    {
        await AuthenticateAsCashierAsync();
        var sale = await SellOneAsync();

        var page = await Client.GetFromJsonAsync<SalesPage>($"/sales?pageSize={SalesQueryRules.MaxPageSize}", JsonOptions);

        page!.Items.Should()
                   .Contain(i => i.Id == sale.InvoiceId);
    }

    [Fact]
    public async Task ListSales_AsManagerForAnotherDay_ReturnsThatDaysBill()
    {
        var old = await SeedInvoiceAsync(TwoDaysAgoUtc);
        await AuthenticateAsManagerAsync();

        var page = await Client.GetFromJsonAsync<SalesPage>(
            $"/sales?from={Day(old.CreatedUtc)}&to={Day(old.CreatedUtc)}&pageSize={SalesQueryRules.MaxPageSize}", JsonOptions);

        page!.Items.Should()
                   .Contain(i => i.Id == old.Id);
    }

    [Fact]
    public async Task GetSaleByNumber_WithAKnownNumber_ReturnsTheSameBillAsTheGuidRoute()
    {
        await AuthenticateAsCashierAsync();
        var sale = await SellOneAsync();

        var byNumber = await Client.GetFromJsonAsync<InvoiceDetailDto>($"/sales/{sale.InvoiceNumber}", JsonOptions);
        var byId = await Client.GetFromJsonAsync<InvoiceDetailDto>($"/sales/{sale.InvoiceId}", JsonOptions);

        byNumber.Should()
                .BeEquivalentTo(byId);
    }

    [Fact]
    public async Task GetSaleById_AfterASale_ReturnsTheSellersName()
    {
        await AuthenticateAsCashierAsync();
        var sale = await SellOneAsync();

        var detail = await Client.GetFromJsonAsync<InvoiceDetailDto>($"/sales/{sale.InvoiceId}", JsonOptions);

        detail!.CashierName.Should()
                           .Be("Test User");
    }

    [Fact]
    public async Task GetSaleById_AsManagerForAnotherDaysBill_ReturnsOk()
    {
        var old = await SeedInvoiceAsync(TwoDaysAgoUtc);
        await AuthenticateAsManagerAsync();

        var response = await Client.GetAsync($"/sales/{old.Id}");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.OK);
    }
}
