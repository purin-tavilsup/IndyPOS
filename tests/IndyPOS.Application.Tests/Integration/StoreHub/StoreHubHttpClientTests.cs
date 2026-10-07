using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.StoreHub.Auth;
using IndyPOS.Application.UseCases.StoreHub.PaymentMethods;
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
                RoleId: (int)UserRole.Cashier,
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
    public async Task GetLegacySalesSummaryAsync_WhenForbidden_ThrowsPermissionError()
    {
        // Arrange
        _sut.SetAuthToken("valid-token");
        SetupMockResponse(HttpStatusCode.Forbidden, new { });

        // Act
        var act = () => _sut.GetLegacySalesSummaryAsync(DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today));

        // Assert
        var exception = await Assert.ThrowsAsync<StoreHubClientException>(act);
        exception.Message.Should().Contain("permission");
    }

    [Fact]
    public async Task CompleteSaleAsync_WhenAuthenticated_ReturnsSuccess()
    {
        // Arrange
        _sut.SetAuthToken("valid-token");

        var request = new CompleteSaleRequest(
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
            CreatedUtc: DateTime.UtcNow,
            InvoiceNumber: 1001);

        SetupMockResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _sut.CompleteSaleAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TotalAmount.Should().Be(200m);
    }

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

    [Fact]
    public async Task GetLegacySalesSummaryAsync_WithAThaiCulture_SendsGregorianDates()
    {
        // Arrange
        _sut.SetAuthToken("valid-token");
        var requestUri = CaptureRequestUri(new SalesSummary());

        // Act
        await RunUnderThaiCultureAsync(() => _sut.GetLegacySalesSummaryAsync(
            new DateOnly(2026, 10, 2), new DateOnly(2026, 10, 3)));

        // Assert
        requestUri().Should()
                    .Contain("fromDate=2026-10-02")
                    .And.Contain("toDate=2026-10-03");
    }

    [Fact]
    public async Task GetLegacyPaymentsSummaryAsync_WithAThaiCulture_SendsGregorianDates()
    {
        // Arrange
        _sut.SetAuthToken("valid-token");
        var requestUri = CaptureRequestUri(new PaymentsSummary());

        // Act
        await RunUnderThaiCultureAsync(() => _sut.GetLegacyPaymentsSummaryAsync(
            new DateOnly(2026, 10, 2), new DateOnly(2026, 10, 3)));

        // Assert
        requestUri().Should()
                    .Contain("fromDate=2026-10-02")
                    .And.Contain("toDate=2026-10-03");
    }

    private const string CampaignCode = "Campaign2569";

    // Nothing else pins the client's URLs, and a wrong one only shows at the till.
    // InlineData (not delegates) keeps one test case per row in the runner's count.
    [Theory]
    [InlineData("GetOfferablePaymentMethods", "GET", "/payment-methods")]
    [InlineData("GetAllPaymentMethods", "GET", "/payment-methods?include=all")]
    [InlineData("AddCampaignPaymentMethod", "POST", "/payment-methods")]
    [InlineData("SetPaymentMethodEnabled", "PATCH", "/payment-methods/Campaign2569")]
    [InlineData("UpdatePaymentMethodDisplay", "PATCH", "/payment-methods/Campaign2569")]
    [InlineData("CompleteSale", "POST", "/sales")]
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
        "CompleteSale" => _sut.CompleteSaleAsync(new CompleteSaleRequest([], [])),
        _ => throw new ArgumentOutOfRangeException(nameof(call), call, "No such client call.")
    };

    private static string ResponseFor(string call) =>
        call.StartsWith("Get", StringComparison.Ordinal) ? "[]" : "{}";

    private static async Task RunUnderThaiCultureAsync(Func<Task> action)
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("th-TH");
            await action();
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

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

    private Func<string> CaptureRequestUri<T>(T responseBody)
    {
        string? captured = null;
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(responseBody)
        };

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => captured = request.RequestUri!.ToString())
            .ReturnsAsync(response);

        return () => captured!;
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
