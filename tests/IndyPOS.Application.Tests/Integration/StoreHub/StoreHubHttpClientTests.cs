using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.Auth;
using IndyPOS.Application.UseCases.StoreHub.Products;
using IndyPOS.Application.UseCases.StoreHub.Sales;
using IndyPOS.Infrastructure.Services.StoreHub;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace IndyPOS.Application.Tests.Integration.StoreHub;

/// <summary>
/// Integration tests for StoreHubHttpClient.
/// Uses mocked HttpMessageHandler to simulate StoreHub API responses.
/// </summary>
public class StoreHubHttpClientTests
{
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly StoreHubHttpClient _sut;

    public StoreHubHttpClientTests()
    {
        _mockHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:5000")
        };

        _sut = new StoreHubHttpClient(_httpClient, NullLogger<StoreHubHttpClient>.Instance);
    }

    [Fact]
    public async Task LoginAsync_WhenCredentialsValid_ReturnsSuccessWithToken()
    {
        // Arrange
        var expectedResponse = new LoginResponse(
            Success: true,
            Token: "test-jwt-token",
            User: new StoreUserDto(
                Id: Guid.NewGuid(),
                Username: "testuser",
                FirstName: "Test",
                LastName: "User",
                RoleId: 3,
                StoreId: "STORE-001"),
            ErrorMessage: null);

        SetupMockResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _sut.LoginAsync("testuser", "password123");

        // Assert
        result.Success.Should().BeTrue();
        result.Token.Should().Be("test-jwt-token");
        result.User.Should().NotBeNull();
        result.User!.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task LoginAsync_WhenCredentialsInvalid_ReturnsFailure()
    {
        // Arrange
        SetupMockResponse(HttpStatusCode.Unauthorized, new { });

        // Act
        var result = await _sut.LoginAsync("baduser", "wrongpassword");

        // Assert
        result.Success.Should().BeFalse();
        result.Token.Should().BeNull();
    }

    [Fact]
    public async Task GetProductsAsync_WhenAuthenticated_ReturnsProducts()
    {
        // Arrange
        _sut.SetAuthToken("valid-token");

        var expectedProducts = new List<ProductDto>
        {
            new(Id: Guid.NewGuid(), Barcode: "123", Name: "Product 1", Description: null,
                Manufacturer: null, Brand: null, Category: "Test", UnitPrice: 10m,
                GroupPrice: null, GroupPriceQuantity: null, IsActive: true),
            new(Id: Guid.NewGuid(), Barcode: "456", Name: "Product 2", Description: null,
                Manufacturer: null, Brand: null, Category: "Test", UnitPrice: 20m,
                GroupPrice: null, GroupPriceQuantity: null, IsActive: true)
        };

        SetupMockResponse(HttpStatusCode.OK, expectedProducts);

        // Act
        var result = await _sut.GetProductsAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].Barcode.Should().Be("123");
        result[1].Barcode.Should().Be("456");
    }

    [Fact]
    public async Task CompleteSaleAsync_WhenAuthenticated_ReturnsSuccess()
    {
        // Arrange
        _sut.SetAuthToken("valid-token");

        var request = new CompleteSaleRequest(
            UserId: Guid.NewGuid(),
            Lines: new List<SaleLineRequest>
            {
                new(ProductId: Guid.NewGuid(), Quantity: 2, UnitPrice: 100m)
            },
            Payments: new List<SalePaymentRequest>
            {
                new(Method: "Cash", Amount: 200m)
            });

        var expectedResponse = new CompleteSaleResponse(
            InvoiceId: Guid.NewGuid(),
            TotalAmount: 200m,
            CreatedUtc: DateTime.UtcNow);

        SetupMockResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _sut.CompleteSaleAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TotalAmount.Should().Be(200m);
    }

    [Fact]
    public async Task IsHealthyAsync_WhenApiHealthy_ReturnsTrue()
    {
        // Arrange
        SetupMockResponse(HttpStatusCode.OK, new { status = "healthy" });

        // Act
        var result = await _sut.IsHealthyAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsHealthyAsync_WhenApiUnhealthy_ReturnsFalse()
    {
        // Arrange
        SetupMockResponse(HttpStatusCode.ServiceUnavailable, new { });

        // Act
        var result = await _sut.IsHealthyAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SetAuthToken_SetsIsAuthenticatedTrue()
    {
        // Arrange
        _sut.IsAuthenticated.Should().BeFalse();

        // Act
        _sut.SetAuthToken("test-token");

        // Assert
        _sut.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void ClearAuthToken_SetsIsAuthenticatedFalse()
    {
        // Arrange
        _sut.SetAuthToken("test-token");
        _sut.IsAuthenticated.Should().BeTrue();

        // Act
        _sut.ClearAuthToken();

        // Assert
        _sut.IsAuthenticated.Should().BeFalse();
    }

    private void SetupMockResponse<T>(HttpStatusCode statusCode, T content)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = JsonContent.Create(content, options: new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })
        };

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }
}
