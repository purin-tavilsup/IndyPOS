using System.Net.Http.Json;
using System.Text.Json.Serialization;
using IndyPOS.Application.Abstractions.Security;
using IndyPOS.Application.Abstractions.StoreHub.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IndyPOS.Infrastructure.Services.StoreHub;

/// <summary>
/// Service for acquiring and caching OAuth2 tokens for Cloud API communication.
/// Thread-safe with in-memory caching and automatic refresh before expiry.
/// BaseAddress is configured via Aspire service discovery in ConfigureServices.
/// Uses DPAPI for secure client secret storage when available (S5b).
/// </summary>
public class CloudTokenService : ITokenService
{
    private readonly HttpClient _httpClient;
    private readonly SecureCloudTokenOptions _secureOptions;
    private readonly ILogger<CloudTokenService> _logger;
    private readonly CloudTokenCache _cache;

    // Buffer time before actual expiry to trigger refresh
    private static readonly TimeSpan ExpiryBuffer = TimeSpan.FromSeconds(30);

    /// <summary>A service with a cache of its own, for use outside DI.</summary>
    public CloudTokenService(
        HttpClient httpClient,
        SecureCloudTokenOptions secureOptions,
        ILogger<CloudTokenService> logger)
        : this(httpClient, secureOptions, logger, new CloudTokenCache())
    {
    }

    /// <remarks>
    /// DI creates a new typed client per scope, and the sync and heartbeat workers open a scope every few seconds
    /// or minutes, so the token is cached in a singleton they share. A cache per instance fetched a new token on
    /// every heartbeat.
    /// </remarks>
    [ActivatorUtilitiesConstructor]
    public CloudTokenService(
        HttpClient httpClient,
        SecureCloudTokenOptions secureOptions,
        ILogger<CloudTokenService> logger,
        CloudTokenCache cache)
    {
        _httpClient = httpClient;
        _secureOptions = secureOptions;
        _logger = logger;
        _cache = cache;
        // BaseAddress is configured via DI in ConfigureServices (Aspire service discovery)
    }

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        // Fast path: return cached token if still valid
        if (!string.IsNullOrEmpty(_cache.Token) && DateTime.UtcNow < _cache.ExpiresAtUtc - ExpiryBuffer)
        {
            return _cache.Token;
        }

        // Slow path: acquire lock and refresh token
        await _cache.Gate.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (!string.IsNullOrEmpty(_cache.Token) && DateTime.UtcNow < _cache.ExpiresAtUtc - ExpiryBuffer)
            {
                return _cache.Token;
            }

            return await FetchNewTokenAsync(cancellationToken);
        }
        finally
        {
            _cache.Gate.Release();
        }
    }

    public void ClearCachedToken()
    {
        _cache.Token = null;
        _cache.ExpiresAtUtc = DateTime.MinValue;
        _logger.LogInformation("Cleared cached token");
    }

    private async Task<string?> FetchNewTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Requesting new access token from Cloud API");

            // Get client secret from secure storage (DPAPI)
            var clientSecret = await _secureOptions.GetClientSecretAsync();

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _secureOptions.ClientId,
                ["client_secret"] = clientSecret,
                ["scope"] = _secureOptions.Scopes
            });

            var response = await _httpClient.PostAsync("/oauth/token", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Token request failed with status {StatusCode}: {Error}",
                    response.StatusCode,
                    errorBody);
                return null;
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);

            if (tokenResponse is null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                _logger.LogWarning("Token response was empty or invalid");
                return null;
            }

            // Cache the token
            _cache.Token = tokenResponse.AccessToken;
            _cache.ExpiresAtUtc = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

            _logger.LogInformation(
                "Acquired new access token, expires in {ExpiresIn} seconds",
                tokenResponse.ExpiresIn);

            return _cache.Token;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to acquire token from Cloud API (offline mode?)");
            return null;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Token request timed out");
            return null;
        }
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
