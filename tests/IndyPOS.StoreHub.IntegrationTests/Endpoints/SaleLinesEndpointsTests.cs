using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.Sales;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

/// <summary>
/// GET /sales/lines: every sold line with its bill, so the products-sold tab can trace a product to the
/// bill it was sold on. The shared test database keeps every sale made today, so each test finds its own
/// rows by bill id.
/// </summary>
[Collection("Integration")]
public class SaleLinesEndpointsTests : IntegrationTestBase
{
    public SaleLinesEndpointsTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    /// <summary>Two days back, so a run near midnight cannot land it on "today".</summary>
    private static DateTime TwoDaysAgoUtc => DateTime.UtcNow.AddDays(-2);

    // The catch-all GET /sales/{value} also answers "/sales/lines" with a 400 (about bill numbers), so a
    // bare status check would pass with no /lines route at all. The message proves which rule fired.
    private sealed record ErrorBody(string Error);

    private static string Day(DateTime utc) => DateOnly.FromDateTime(utc.ToLocalTime()).ToString("yyyy-MM-dd");

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

    // Every page of today's lines. The shared test database is never reset, so today's lines grow with
    // every test that sells; reading only page 1 would one day miss the sale a test just made.
    private async Task<List<SaleLineRowDto>> AllTodaysLinesAsync()
    {
        var lines = new List<SaleLineRowDto>();
        for (var page = 1; ; page++)
        {
            var result = await LinesAsync($"page={page}&pageSize={SalesQueryRules.MaxPageSize}");
            lines.AddRange(result.Items);
            if (!result.HasMore)
                return lines;
        }
    }

    [Fact]
    public async Task ListSaleLines_WithAMalformedDate_ReturnsTheThaiDateError()
    {
        await AuthenticateAsManagerAsync();

        var response = await Client.GetAsync("/sales/lines?from=07-10-2026&to=2026-10-07");

        (await response.Content.ReadFromJsonAsync<ErrorBody>())!.Error.Should()
                                                                .StartWith("วันที่ไม่ถูกต้อง");
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
    public async Task ListSaleLines_WithAPageSizeOverTheMaximum_ReturnsThePageSizeError()
    {
        await AuthenticateAsManagerAsync();

        var response = await Client.GetAsync($"/sales/lines?pageSize={SalesQueryRules.MaxPageSize + 1}");

        (await response.Content.ReadFromJsonAsync<ErrorBody>())!.Error.Should()
                                                                .StartWith("จำนวนต่อหน้า");
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

        var lines = await AllTodaysLinesAsync();

        lines.Should()
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
        var lines = await AllTodaysLinesAsync();

        lines.Single(l => l.InvoiceId == sale.InvoiceId).LineTotal.Should()
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

        var ids = (await AllTodaysLinesAsync()).Select(l => l.InvoiceId).ToList();

        ids.IndexOf(first.InvoiceId).Should()
                                    .BeLessThan(ids.IndexOf(second.InvoiceId));
    }
}
