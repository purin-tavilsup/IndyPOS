using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using IndyPOS.Application.Common.Validation;
using IndyPOS.Application.UseCases.StoreHub.Reports;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for /reports/* endpoints.
/// </summary>
[Collection("Integration")]
public class ReportsEndpointTests : IntegrationTestBase
{
    public ReportsEndpointTests(StoreHubWebApplicationFactory factory) : base(factory) { }

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

    [Fact]
    public async Task GetSalesSummary_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var response = await Client.GetAsync($"/reports/sales-summary?fromDate={today}&toDate={today}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSalesSummary_AsCashier_ReturnsForbidden()
    {
        // Arrange - Cashiers don't have CanViewReports capability
        await AuthenticateAsCashierAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var response = await Client.GetAsync($"/reports/sales-summary?fromDate={today}&toDate={today}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

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

    [Fact]
    public async Task GetSalesSummary_AsManager_ReturnsOk()
    {
        // Arrange
        await AuthenticateAsManagerAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var response = await Client.GetAsync($"/reports/sales-summary?fromDate={today}&toDate={today}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SalesSummaryDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.FromDate.Should().Be(today);
        result.ToDate.Should().Be(today);
    }

    [Fact]
    public async Task GetSalesSummary_WithSales_ReturnsCorrectTotals()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var product = await CreateTestProductAsync(unitPrice: 100m, initialStock: 50);
        var user = await CreateTestUserAsync($"seller_{Guid.NewGuid():N}", "Password123!");

        // Create a sale
        var saleRequest = new
        {
            UserId = user.Id,
            Lines = new[] { new { ProductId = product.Id, Quantity = 3, UnitPrice = 100m } },
            Payments = new[] { new { Method = "Cash", Amount = 300m } }
        };
        var saleResponse = await Client.PostAsJsonAsync("/sales/complete", saleRequest);
        saleResponse.EnsureSuccessStatusCode();

        // Use local date since ReportDateRange converts using local timezone
        var today = DateOnly.FromDateTime(DateTime.Now);

        // Act
        var response = await Client.GetAsync($"/reports/sales-summary?fromDate={today}&toDate={today}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SalesSummaryDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.InvoiceCount.Should().BeGreaterThan(0);
        result.TotalRevenue.Should().BeGreaterThanOrEqualTo(300m);
    }

    [Fact]
    public async Task GetPayLaterReport_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/reports/pay-later");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPayLaterReport_AsManager_ReturnsOk()
    {
        // Arrange
        await AuthenticateAsManagerAsync();

        // Act
        var response = await Client.GetAsync("/reports/pay-later");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProductSales_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var response = await Client.GetAsync($"/reports/product-sales?fromDate={today}&toDate={today}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProductSales_AsManager_ReturnsPagedResult()
    {
        // Arrange
        await AuthenticateAsManagerAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var response = await Client.GetAsync($"/reports/product-sales?fromDate={today}&toDate={today}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<ProductSalesDto>>(JsonOptions);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetProductSales_WithCategoryFilter_ReturnsFiltered()
    {
        // Arrange
        await AuthenticateAsManagerAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var response = await Client.GetAsync($"/reports/product-sales?fromDate={today}&toDate={today}&category=TestCategory");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<ProductSalesDto>>(JsonOptions);
        result.Should().NotBeNull();
    }
}
