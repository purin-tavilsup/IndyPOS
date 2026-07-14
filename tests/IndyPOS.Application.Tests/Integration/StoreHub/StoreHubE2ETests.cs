using System.Text.Json;
using FluentAssertions;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.UseCases.StoreHub.Auth;
using IndyPOS.Application.UseCases.StoreHub.Products;
using IndyPOS.Application.UseCases.StoreHub.Sales;
using IndyPOS.Infrastructure.Services.StoreHub;
using Microsoft.Extensions.Logging.Abstractions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace IndyPOS.Application.Tests.Integration.StoreHub;

/// <summary>
/// End-to-end integration tests using WireMock to simulate StoreHub API.
/// Tests the full flow: login → get products → complete sale.
/// </summary>
public class StoreHubE2ETests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly StoreHubHttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public StoreHubE2ETests()
    {
        _server = WireMockServer.Start();

        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(_server.Url!)
        };

        _client = new StoreHubHttpClient(httpClient, NullLogger<StoreHubHttpClient>.Instance);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    [Fact]
    public async Task E2E_LoginGetProductsAndCompleteSale_Succeeds()
    {
        // Arrange - Setup mock endpoints
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        SetupLoginEndpoint(userId);
        SetupProductsEndpoint(productId);
        SetupCompleteSaleEndpoint(invoiceId);

        // Act 1 - Login
        var loginResult = await _client.LoginAsync("cashier", "cashier123");

        // Assert 1
        loginResult.Success.Should().BeTrue();
        loginResult.Token.Should().NotBeNullOrEmpty();
        _client.IsAuthenticated.Should().BeTrue();

        // Act 2 - Get Products
        var products = await _client.GetProductsAsync();

        // Assert 2
        products.Should().HaveCount(2);
        products[0].Name.Should().Be("น้ำดื่ม 600ml");
        products[1].Name.Should().Be("โค้ก 325ml");

        // Act 3 - Complete Sale
        var saleRequest = new CompleteSaleRequest(
            UserId: userId,
            Lines: new List<SaleLineRequest>
            {
                new(ProductId: productId, Quantity: 2, UnitPrice: 7m)
            },
            Payments: new List<SalePaymentRequest>
            {
                new(Method: "Cash", Amount: 14m)
            });

        var saleResult = await _client.CompleteSaleAsync(saleRequest);

        // Assert 3
        saleResult.InvoiceId.Should().Be(invoiceId);
        saleResult.TotalAmount.Should().Be(14m);
    }

    [Fact]
    public async Task E2E_LoginWithInvalidCredentials_ReturnsFailure()
    {
        // Arrange
        _server.Given(
            Request.Create()
                .WithPath("/auth/login")
                .UsingPost())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(401));

        // Act
        var result = await _client.LoginAsync("baduser", "wrongpass");

        // Assert
        result.Success.Should().BeFalse();
        _client.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task E2E_GetProductsWithoutAuth_ThrowsException()
    {
        // Arrange - No login

        // Act & Assert
        await Assert.ThrowsAsync<StoreHubClientException>(
            () => _client.GetProductsAsync());
    }

    [Fact]
    public async Task E2E_HealthCheck_ReturnsHealthy()
    {
        // Arrange
        _server.Given(
            Request.Create()
                .WithPath("/health/ready")
                .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithBody("""{"status":"healthy","database":"connected"}"""));

        // Act
        var isHealthy = await _client.IsHealthyAsync();

        // Assert
        isHealthy.Should().BeTrue();
    }

    [Fact]
    public async Task E2E_Logout_ClearsAuthToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupLoginEndpoint(userId);

        await _client.LoginAsync("cashier", "cashier123");
        _client.IsAuthenticated.Should().BeTrue();

        // Act
        _client.ClearAuthToken();

        // Assert
        _client.IsAuthenticated.Should().BeFalse();
    }

    private void SetupLoginEndpoint(Guid userId)
    {
        var loginResponse = new LoginResponse(
            Success: true,
            Token: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test-token",
            User: new StoreUserDto(
                Id: userId,
                Username: "cashier",
                FirstName: "Test",
                LastName: "Cashier",
                RoleId: (int)UserRole.Cashier,
                StoreId: "STORE-001"),
            ErrorMessage: null);

        _server.Given(
            Request.Create()
                .WithPath("/auth/login")
                .UsingPost())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(loginResponse, _jsonOptions)));
    }

    private void SetupProductsEndpoint(Guid firstProductId)
    {
        var products = new List<ProductDto>
        {
            new(Id: firstProductId, Barcode: "8850000000001", Name: "น้ำดื่ม 600ml",
                Description: null, Manufacturer: null, Brand: null, Category: "เครื่องดื่ม",
                UnitPrice: 7m, GroupPrice: null, GroupPriceQuantity: null, IsActive: true),
            new(Id: Guid.NewGuid(), Barcode: "8850000000002", Name: "โค้ก 325ml",
                Description: null, Manufacturer: null, Brand: null, Category: "เครื่องดื่ม",
                UnitPrice: 15m, GroupPrice: null, GroupPriceQuantity: null, IsActive: true)
        };

        _server.Given(
            Request.Create()
                .WithPath("/products")
                .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(products, _jsonOptions)));
    }

    private void SetupCompleteSaleEndpoint(Guid invoiceId)
    {
        var response = new CompleteSaleResponse(
            InvoiceId: invoiceId,
            TotalAmount: 14m,
            CreatedUtc: DateTime.UtcNow);

        _server.Given(
            Request.Create()
                .WithPath("/sales/complete")
                .UsingPost())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(response, _jsonOptions)));
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
    }
}
