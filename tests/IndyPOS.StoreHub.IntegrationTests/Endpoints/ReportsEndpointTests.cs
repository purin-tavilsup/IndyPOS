using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
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
        await Client.PostAsJsonAsync("/sales/complete", saleRequest);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

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
    public async Task GetInvoices_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var response = await Client.GetAsync($"/reports/invoices?fromDate={today}&toDate={today}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetInvoices_AsManager_ReturnsPagedResult()
    {
        // Arrange
        await AuthenticateAsManagerAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var response = await Client.GetAsync($"/reports/invoices?fromDate={today}&toDate={today}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<InvoiceSummaryDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
        result.PageSize.Should().Be(50);
    }

    [Fact]
    public async Task GetInvoices_WithPagination_RespectsPageSize()
    {
        // Arrange
        await AuthenticateAsManagerAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var response = await Client.GetAsync($"/reports/invoices?fromDate={today}&toDate={today}&page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<InvoiceSummaryDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetInvoiceDetail_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"/reports/invoices/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetInvoiceDetail_NonExistent_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsManagerAsync();

        // Act
        var response = await Client.GetAsync($"/reports/invoices/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
