using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.UseCases.StoreHub.Auth;
using IndyPOS.Application.UseCases.StoreHub.Products;
using IndyPOS.Application.UseCases.StoreHub.Sales;
using Microsoft.Extensions.Logging;

namespace IndyPOS.Infrastructure.Services.StoreHub;

/// <summary>
/// HTTP client implementation for StoreHub API.
/// Handles authentication, serialization, and error handling.
/// </summary>
public class StoreHubHttpClient : IStoreHubClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StoreHubHttpClient> _logger;
    private string? _authToken;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public StoreHubHttpClient(HttpClient httpClient, ILogger<StoreHubHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_authToken);

    public void SetAuthToken(string token)
    {
        _authToken = token;
        _logger.LogDebug("Auth token set");
    }

    public void ClearAuthToken()
    {
        _authToken = null;
        _logger.LogDebug("Auth token cleared");
    }

    public async Task<LoginResponse> LoginAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Attempting login for user: {Username}", username);

            var request = new LoginRequest(username, password);
            var response = await _httpClient.PostAsJsonAsync("/auth/login", request, JsonOptions, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions, cancellationToken);

                if (loginResponse?.Success == true && loginResponse.Token is not null)
                {
                    SetAuthToken(loginResponse.Token);
                    _logger.LogInformation("Login successful for user: {Username}", username);
                }

                return loginResponse ?? new LoginResponse(false, null, null, "Invalid response from server");
            }

            _logger.LogWarning("Login failed for user: {Username}, Status: {StatusCode}", username, response.StatusCode);
            return new LoginResponse(false, null, null, $"Login failed: {response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during login");
            return new LoginResponse(false, null, null, "Cannot connect to StoreHub. Please check if the service is running.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login");
            return new LoginResponse(false, null, null, $"Unexpected error: {ex.Message}");
        }
    }

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(
        bool activeOnly = true,
        string? category = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        try
        {
            var queryParams = new List<string> { $"activeOnly={activeOnly}" };
            if (!string.IsNullOrEmpty(category))
                queryParams.Add($"category={Uri.EscapeDataString(category)}");
            if (!string.IsNullOrEmpty(searchTerm))
                queryParams.Add($"search={Uri.EscapeDataString(searchTerm)}");

            var url = $"/products?{string.Join("&", queryParams)}";

            using var request = CreateAuthenticatedRequest(HttpMethod.Get, url);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            response.EnsureSuccessStatusCode();

            var products = await response.Content.ReadFromJsonAsync<IReadOnlyList<ProductDto>>(JsonOptions, cancellationToken);
            _logger.LogDebug("Fetched {Count} products from StoreHub", products?.Count ?? 0);

            return products ?? [];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch products from StoreHub");
            throw new StoreHubClientException("Cannot connect to StoreHub", ex);
        }
    }

    public async Task<CompleteSaleResponse> CompleteSaleAsync(
        CompleteSaleRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        try
        {
            _logger.LogDebug("Completing sale with {LineCount} lines, {PaymentCount} payments",
                request.Lines.Count, request.Payments.Count);

            using var httpRequest = CreateAuthenticatedRequest(HttpMethod.Post, "/sales/complete");
            httpRequest.Content = JsonContent.Create(request, options: JsonOptions);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<CompleteSaleResponse>(JsonOptions, cancellationToken);

            if (result is null)
            {
                throw new StoreHubClientException("Invalid response from StoreHub");
            }

            _logger.LogInformation("Sale completed successfully. InvoiceId: {InvoiceId}, Total: {Total}",
                result.InvoiceId, result.TotalAmount);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to complete sale");
            throw new StoreHubClientException("Cannot connect to StoreHub", ex);
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/health/ready", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private void EnsureAuthenticated()
    {
        if (!IsAuthenticated)
        {
            throw new StoreHubClientException("Not authenticated. Please login first.");
        }
    }

    private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);
        return request;
    }
}

/// <summary>
/// Exception thrown when StoreHub API communication fails.
/// </summary>
public class StoreHubClientException : Exception
{
    public StoreHubClientException(string message) : base(message) { }
    public StoreHubClientException(string message, Exception innerException) : base(message, innerException) { }
}
