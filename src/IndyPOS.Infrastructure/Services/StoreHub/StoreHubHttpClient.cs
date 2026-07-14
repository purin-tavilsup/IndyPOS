using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.StoreHub.Auth;
using IndyPOS.Application.UseCases.StoreHub.Auth.ChangePassword;
using IndyPOS.Application.UseCases.StoreHub.PayLater;
using IndyPOS.Application.UseCases.StoreHub.Products;
using IndyPOS.Application.UseCases.StoreHub.Products.AdjustQuantity;
using IndyPOS.Application.UseCases.StoreHub.Products.Create;
using IndyPOS.Application.UseCases.StoreHub.Products.Update;
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

    public async Task<ChangePasswordResponse> ChangePasswordAsync(
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        try
        {
            using var request = CreateAuthenticatedRequest(HttpMethod.Post, "/auth/change-password");
            request.Content = JsonContent.Create(
                new ChangePasswordRequest(currentPassword, newPassword), options: JsonOptions);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadFromJsonAsync<ChangePasswordResponse>(JsonOptions, cancellationToken);
                return body ?? new ChangePasswordResponse(false, null, "Invalid response from server");
            }

            var errorBody = await ReadResponseBodyAsync(response, cancellationToken);
            return new ChangePasswordResponse(false, null, $"Change password failed: {response.StatusCode}. {errorBody}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during change-password");
            return new ChangePasswordResponse(false, null, "Cannot connect to StoreHub.");
        }
    }

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(
        bool activeOnly = true,
        string? category = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string> { $"activeOnly={activeOnly}" };
        if (!string.IsNullOrEmpty(category))
            queryParams.Add($"category={Uri.EscapeDataString(category)}");
        if (!string.IsNullOrEmpty(searchTerm))
            queryParams.Add($"search={Uri.EscapeDataString(searchTerm)}");

        var url = $"/products?{string.Join("&", queryParams)}";

        var products = await SendAuthenticatedAsync<IReadOnlyList<ProductDto>>(
            HttpMethod.Get, url, content: null, cancellationToken);

        _logger.LogDebug("Fetched {Count} products from StoreHub", products?.Count ?? 0);
        return products ?? [];
    }

    public async Task<ProductDto> CreateProductAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating product: {Name}, Barcode: {Barcode}", command.Name, command.Barcode);

        var result = await SendAuthenticatedAsync<ProductDto>(
            HttpMethod.Post, "/products", command, cancellationToken);

        _logger.LogInformation("Product created successfully. Id: {Id}, Name: {Name}", result.Id, result.Name);
        return result;
    }

    public async Task<ProductDto> UpdateProductAsync(
        UpdateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating product: {Id}, Name: {Name}", command.Id, command.Name);

        var result = await SendAuthenticatedAsync<ProductDto>(
            HttpMethod.Put, $"/products/{command.Id}", command, cancellationToken);

        _logger.LogInformation("Product updated successfully. Id: {Id}, Name: {Name}", result.Id, result.Name);
        return result;
    }

    public async Task DeleteProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting product: {Id}", productId);

        await SendAuthenticatedAsync(HttpMethod.Delete, $"/products/{productId}", cancellationToken);

        _logger.LogInformation("Product deleted successfully. Id: {Id}", productId);
    }

    public async Task<ProductDto> AdjustProductQuantityAsync(
        Guid productId,
        AdjustQuantityRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adjusting quantity for product: {Id}, Target: {TargetQuantity}",
            productId, request.TargetQuantity);

        var result = await SendAuthenticatedAsync<ProductDto>(
            HttpMethod.Post, $"/products/{productId}/adjust-quantity", request, cancellationToken);

        _logger.LogInformation("Product quantity adjusted. Id: {Id}, Target: {TargetQuantity}",
            result.Id, request.TargetQuantity);
        return result;
    }

    public async Task<string> GenerateBarcodeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating next barcode");

        var result = await SendAuthenticatedAsync<GenerateBarcodeResponse>(
            HttpMethod.Post, "/products/next-barcode", content: null, cancellationToken);

        _logger.LogInformation("Barcode generated: {Barcode}", result.Barcode);
        return result.Barcode;
    }

    public async Task<CompleteSaleResponse> CompleteSaleAsync(
        CompleteSaleRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Completing sale with {LineCount} lines, {PaymentCount} payments",
            request.Lines.Count, request.Payments.Count);

        var result = await SendAuthenticatedAsync<CompleteSaleResponse>(
            HttpMethod.Post, "/sales/complete", request, cancellationToken);

        _logger.LogInformation("Sale completed successfully. InvoiceId: {InvoiceId}, Total: {Total}",
            result.InvoiceId, result.TotalAmount);
        return result;
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

    // ========================
    // PayLater methods
    // ========================

    public async Task<GetPayLaterResponse> GetPayLaterAsync(
        bool includeCompleted = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string> { $"includeCompleted={includeCompleted}" };
        if (!string.IsNullOrEmpty(searchTerm))
            queryParams.Add($"search={Uri.EscapeDataString(searchTerm)}");

        var url = $"/pay-later?{string.Join("&", queryParams)}";

        var result = await SendAuthenticatedAsync<GetPayLaterResponse>(
            HttpMethod.Get, url, content: null, cancellationToken);

        _logger.LogDebug("Fetched {Count} pay-later records from StoreHub", result.Items.Count);
        return result;
    }

    public async Task<PayLaterDto?> GetPayLaterByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await SendAuthenticatedAsync<PayLaterDto>(
                HttpMethod.Get, $"/pay-later/{id}", content: null, cancellationToken);

            return result;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<PayLaterDto> RecordPayLaterPaymentAsync(
        Guid payLaterId,
        decimal paymentAmount,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Recording payment for PayLater: {Id}, Amount: {Amount}", payLaterId, paymentAmount);

        var request = new RecordPaymentRequest(paymentAmount);
        var result = await SendAuthenticatedAsync<PayLaterDto>(
            HttpMethod.Post, $"/pay-later/{payLaterId}/record-payment", request, cancellationToken);

        _logger.LogInformation("Payment recorded for PayLater: {Id}, New Paid Amount: {PaidAmount}, Completed: {IsCompleted}",
            result.Id, result.PaidAmount, result.IsCompleted);
        return result;
    }

    // ========================
    // Report methods (legacy format)
    // ========================

    public async Task<SalesSummary> GetLegacySalesSummaryAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        var url = $"/reports/legacy/sales-summary?fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}";

        var result = await SendAuthenticatedAsync<SalesSummary>(
            HttpMethod.Get, url, content: null, cancellationToken);

        _logger.LogDebug("Fetched legacy sales summary from StoreHub: {FromDate} to {ToDate}", fromDate, toDate);
        return result;
    }

    public async Task<PaymentsSummary> GetLegacyPaymentsSummaryAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        var url = $"/reports/legacy/payments-summary?fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}";

        var result = await SendAuthenticatedAsync<PaymentsSummary>(
            HttpMethod.Get, url, content: null, cancellationToken);

        _logger.LogDebug("Fetched legacy payments summary from StoreHub: {FromDate} to {ToDate}", fromDate, toDate);
        return result;
    }

    #region Private Helpers

    /// <summary>
    /// Send an authenticated request and deserialize the response.
    /// </summary>
    private async Task<T> SendAuthenticatedAsync<T>(
        HttpMethod method,
        string url,
        object? content,
        CancellationToken cancellationToken)
    {
        EnsureAuthenticated();

        try
        {
            using var request = CreateAuthenticatedRequest(method, url);

            if (content is not null)
            {
                request.Content = JsonContent.Create(content, options: JsonOptions);
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessfulResponseAsync(response, method, url, cancellationToken);

            var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);

            if (result is null)
            {
                throw new StoreHubClientException("Invalid response from StoreHub");
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed: {Method} {Url}", method, url);
            throw new StoreHubClientException("Cannot connect to StoreHub", ex);
        }
    }

    /// <summary>
    /// Send an authenticated request without expecting a response body.
    /// </summary>
    private async Task SendAuthenticatedAsync(
        HttpMethod method,
        string url,
        CancellationToken cancellationToken)
    {
        EnsureAuthenticated();

        try
        {
            using var request = CreateAuthenticatedRequest(method, url);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessfulResponseAsync(response, method, url, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed: {Method} {Url}", method, url);
            throw new StoreHubClientException("Cannot connect to StoreHub", ex);
        }
    }

    private async Task EnsureSuccessfulResponseAsync(
        HttpResponseMessage response,
        HttpMethod method,
        string url,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var responseBody = await ReadResponseBodyAsync(response, cancellationToken);

        _logger.LogWarning(
            "StoreHub request failed: {Method} {Url} responded {StatusCode}. {ResponseBody}",
            method,
            url,
            response.StatusCode,
            responseBody);

        var message = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "StoreHub session expired. Please log in again.",
            HttpStatusCode.Forbidden => "You do not have permission to perform this action.",
            _ => $"StoreHub request failed: {(int)response.StatusCode} {response.StatusCode}"
        };

        throw new StoreHubClientException(message);
    }

    private static async Task<string?> ReadResponseBodyAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            return response.Content is null
                ? null
                : await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch
        {
            return null;
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

    #endregion

    // Internal record for barcode response deserialization
    private record GenerateBarcodeResponse(string Barcode);
}
