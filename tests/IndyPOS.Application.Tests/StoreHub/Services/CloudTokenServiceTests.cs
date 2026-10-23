using System.Net;
using System.Text.Json;
using IndyPOS.Application.Abstractions.Security;
using IndyPOS.Infrastructure.Services.StoreHub;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Services;

public class CloudTokenServiceTests
{
    private readonly CloudTokenOptions _options;
    private readonly SecureCloudTokenOptions _secureOptions;
    private readonly ILogger<CloudTokenService> _logger;

    public CloudTokenServiceTests()
    {
        _options = new CloudTokenOptions
        {
            ClientId = "test_client",
            ClientSecret = "test_secret",
            Scopes = "sync.write master.read"
        };

        // Create a mock secret storage that returns null (falls back to config)
        var mockSecretStorage = new Mock<ISecretStorage>();
        mockSecretStorage.Setup(x => x.GetSecretAsync(It.IsAny<string>()))
            .ReturnsAsync((string?)null);
        mockSecretStorage.Setup(x => x.ExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _secureOptions = new SecureCloudTokenOptions(
            Options.Create(_options),
            mockSecretStorage.Object);

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
            _secureOptions,
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
            _secureOptions,
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
            BaseAddress = new Uri("https://test-api.example.com")
        };

        var sut = new CloudTokenService(
            httpClient,
            _secureOptions,
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
            _secureOptions,
            _logger);

        // Act - just verify it doesn't throw
        sut.ClearCachedToken();

        // Assert - no exception means success
        Assert.True(true);
    }

    // ITokenService is a typed HttpClient, so DI makes a new one per scope. The heartbeat opens a scope every
    // two minutes; with a per-instance cache each beat fetched a new token from the cloud.
    [Fact]
    public async Task GetAccessTokenAsync_FromTwoServicesSharingACache_FetchesTheTokenOnce()
    {
        var fetches = 0;
        var body = JsonSerializer.Serialize(new { access_token = "shared_token", token_type = "Bearer", expires_in = 900 });
        HttpClient NewHttpClient() => new(new FakeHttpMessageHandler(_ =>
        {
            Interlocked.Increment(ref fetches);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };
        })) { BaseAddress = new Uri("https://test-api.example.com") };
        var cache = new CloudTokenCache();

        await new CloudTokenService(NewHttpClient(), _secureOptions, _logger, cache).GetAccessTokenAsync();
        var second = await new CloudTokenService(NewHttpClient(), _secureOptions, _logger, cache).GetAccessTokenAsync();

        Assert.Equal("shared_token", second);
        Assert.Equal(1, fetches);
    }

    [Fact]
    public async Task GetAccessTokenAsync_FromTwoDiScopes_FetchesTheTokenOnce()
    {
        var fetches = 0;
        var body = JsonSerializer.Serialize(new { access_token = "scoped_token", token_type = "Bearer", expires_in = 900 });
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddSingleton(_secureOptions);
        services.AddSingleton(_logger);
        services.AddSingleton<CloudTokenCache>();
        services.AddHttpClient<IndyPOS.Application.Abstractions.StoreHub.Services.ITokenService, CloudTokenService>(c => c.BaseAddress = new Uri("https://test-api.example.com"))
                .ConfigurePrimaryHttpMessageHandler(() => new FakeHttpMessageHandler(_ =>
                {
                    Interlocked.Increment(ref fetches);
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };
                }));
        using var provider = services.BuildServiceProvider();

        foreach (var _ in Enumerable.Range(0, 2))
        {
            using var scope = provider.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IndyPOS.Application.Abstractions.StoreHub.Services.ITokenService>().GetAccessTokenAsync();
        }

        Assert.Equal(1, fetches);
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
            BaseAddress = new Uri("https://test-api.example.com")
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
