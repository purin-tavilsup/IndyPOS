using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.Products.GetStock;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for GET /products/stock — the read path that makes migrated
/// stock visible at the till.
/// </summary>
[Collection("Integration")]
public class ProductStockEndpointTests : IntegrationTestBase
{
    public ProductStockEndpointTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetProductStock_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await Client.GetAsync("/products/stock");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProductStock_AsCashier_ShouldReturnTheProductBalance()
    {
        await AuthenticateAsCashierAsync();
        var product = await CreateTestProductAsync(initialStock: 7);

        var response = await Client.GetAsync("/products/stock");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.OK);

        var stock = await response.Content.ReadFromJsonAsync<List<ProductStockDto>>(JsonOptions);
        stock.Should()
             .Contain(s => s.ProductId == product.Id && s.Quantity == 7);
    }

    [Fact]
    public async Task GetProductStock_WithProductIdFilter_ShouldReturnOnlyThatProduct()
    {
        await AuthenticateAsCashierAsync();
        var wanted = await CreateTestProductAsync(initialStock: 7);
        var other = await CreateTestProductAsync(initialStock: 3);

        var response = await Client.GetAsync($"/products/stock?productId={wanted.Id}");

        var stock = await response.Content.ReadFromJsonAsync<List<ProductStockDto>>(JsonOptions);
        stock.Should()
             .ContainSingle(s => s.ProductId == wanted.Id && s.Quantity == 7);
        stock.Should()
             .NotContain(s => s.ProductId == other.Id);
    }

    [Fact]
    public async Task GetProductStock_ForAProductWithNoMovements_ShouldOmitIt()
    {
        // Absent means zero. The client fills the gap so the wire stays small on a
        // 10,000-product store.
        await AuthenticateAsCashierAsync();
        var product = await CreateTestProductAsync(initialStock: 0);

        var response = await Client.GetAsync("/products/stock");

        var stock = await response.Content.ReadFromJsonAsync<List<ProductStockDto>>(JsonOptions);
        stock.Should()
             .NotContain(s => s.ProductId == product.Id);
    }
}
