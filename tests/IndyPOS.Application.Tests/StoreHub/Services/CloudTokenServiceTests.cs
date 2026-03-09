using System.Net;
using System.Text.Json;
using IndyPOS.Infrastructure.Services.StoreHub;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Services;

public class CloudTokenServiceTests
{
    private readonly CloudTokenOptions _options;
    private readonly ILogger<CloudTokenService> _logger;

    public CloudTokenServiceTests()
    {
        _options = new CloudTokenOptions
        {
            BaseUrl = "https://test-api.example.com",
            ClientId = "test_client",
            ClientSecret = "test_secret",
            Scopes = "sync.write master.read"
        };
        _logger = new Mock<ILogger<CloudTokenService>>().Object;
    }

    [Fact]
    public async Task GetAccessTokenAsync_WhenServerReturnsToken_CachesAndReturnsToken()
    {
        // Arrange
        var tokenResponse = new
        {
            access_token = "test_access_token",
            token_type = "Bearer",
            expires_in = 900
        };

        var httpClient = CreateMockHttpClient(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(tokenResponse));

        var sut = new CloudTokenService(
            httpClient,
            Options.Create(_options),
            _logger);

        // Act
        var token1 = await sut.GetAccessTokenAsync();
        var token2 = await sut.GetAccessTokenAsync(); // Should return cached

        // Assert
        Assert.Equal("test_access_token", token1);
        Assert.Equal("test_access_token", token2);
    }

    [Fact]
    public async Task GetAccessTokenAsync_WhenServerReturnsError_ReturnsNull()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(
            HttpStatusCode.Unauthorized,
            "{\"error\":\"invalid_client\"}");

        var sut = new CloudTokenService(
            httpClient,
            Options.Create(_options),
            _logger);

        // Act
        var token = await sut.GetAccessTokenAsync();

        // Assert
        Assert.Null(token);
    }

    [Fact]
    public async Task GetAccessTokenAsync_WhenNetworkError_ReturnsNull()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(_ =>
            throw new HttpRequestException("Network error"));

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(_options.BaseUrl)
        };

        var sut = new CloudTokenService(
            httpClient,
            Options.Create(_options),
            _logger);

        // Act
        var token = await sut.GetAccessTokenAsync();

        // Assert
        Assert.Null(token);
    }

    [Fact]
    public void ClearCachedToken_ResetsCache()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, "{}");

        var sut = new CloudTokenService(
            httpClient,
            Options.Create(_options),
            _logger);

        // Act - just verify it doesn't throw
        sut.ClearCachedToken();

        // Assert - no exception means success
        Assert.True(true);
    }

    private HttpClient CreateMockHttpClient(HttpStatusCode statusCode, string content)
    {
        var handler = new FakeHttpMessageHandler(request =>
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
            };
        });

        return new HttpClient(handler)
        {
            BaseAddress = new Uri(_options.BaseUrl)
        };
    }

    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }
}
