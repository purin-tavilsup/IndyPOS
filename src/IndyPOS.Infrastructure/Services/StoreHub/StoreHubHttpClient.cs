using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.StoreHub.Auth;
using IndyPOS.Application.UseCases.StoreHub.Auth.ChangePassword;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.DebtRepayments;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Floats;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Payouts;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Summary;
using IndyPOS.Application.UseCases.StoreHub.PayLater;
using IndyPOS.Application.UseCases.StoreHub.PaymentMethods;
using IndyPOS.Application.UseCases.StoreHub.ProductCategories;
using IndyPOS.Application.UseCases.StoreHub.Products;
using IndyPOS.Application.UseCases.StoreHub.Products.AdjustQuantity;
using IndyPOS.Application.UseCases.StoreHub.Products.Create;
using IndyPOS.Application.UseCases.StoreHub.Products.GetStock;
using IndyPOS.Application.UseCases.StoreHub.Products.Update;
using IndyPOS.Application.UseCases.StoreHub.Reports;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetPayLaterReport;
using IndyPOS.Application.UseCases.StoreHub.Sales;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Application.UseCases.StoreHub.Sales.Reprints;
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

    public async Task<IReadOnlyDictionary<Guid, int>> GetProductStockAsync(
        Guid? productId = null,
        CancellationToken cancellationToken = default)
    {
        var url = productId is { } id ? $"/products/stock?productId={id}" : "/products/stock";

        var stock = await SendAuthenticatedAsync<IReadOnlyList<ProductStockDto>>(
            HttpMethod.Get, url, content: null, cancellationToken);

        _logger.LogDebug("Fetched stock for {Count} products from StoreHub", stock?.Count ?? 0);

        return stock?.ToDictionary(s => s.ProductId, s => s.Quantity)
               ?? new Dictionary<Guid, int>();
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

    public async Task<AdjustQuantityResponse> AdjustProductQuantityAsync(
        Guid productId,
        AdjustQuantityRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adjusting stock for product: {Id}, Delta: {Delta}",
            productId, request.Delta);

        var result = await SendAuthenticatedAsync<AdjustQuantityResponse>(
            HttpMethod.Post, $"/products/{productId}/stock-adjustments", request, cancellationToken);

        _logger.LogInformation("Product stock adjusted. Id: {Id}, Delta: {Delta}, Balance: {Balance}",
            productId, request.Delta, result.Quantity);

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
            HttpMethod.Post, "/sales", request, cancellationToken);

        _logger.LogInformation("Sale completed successfully. InvoiceId: {InvoiceId}, Total: {Total}",
            result.InvoiceId, result.TotalAmount);
        return result;
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(StoreHubRoutes.HealthReady, cancellationToken);
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
            HttpMethod.Post, $"/pay-later/{payLaterId}/payments", request, cancellationToken);

        _logger.LogInformation("Payment recorded for PayLater: {Id}, New Paid Amount: {PaidAmount}, Completed: {IsCompleted}",
            result.Id, result.PaidAmount, result.IsCompleted);
        return result;
    }

    // ========================
    // Payment method catalog methods
    // ========================

    public Task<IReadOnlyList<PaymentMethodDto>> GetOfferablePaymentMethodsAsync(
        CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync<IReadOnlyList<PaymentMethodDto>>(
            HttpMethod.Get, "/payment-methods", content: null, cancellationToken);

    public Task<IReadOnlyList<PaymentMethodDto>> GetAllPaymentMethodsAsync(
        CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync<IReadOnlyList<PaymentMethodDto>>(
            HttpMethod.Get, "/payment-methods?include=all", content: null, cancellationToken);

    public Task AddCampaignPaymentMethodAsync(
        string code,
        string displayName,
        int displayOrder,
        CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync(
            HttpMethod.Post,
            "/payment-methods",
            new AddCampaignPaymentMethodRequest(code, displayName, displayOrder),
            cancellationToken);

    public Task SetPaymentMethodEnabledAsync(
        string code,
        bool enabled,
        CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync(
            HttpMethod.Patch,
            $"/payment-methods/{Uri.EscapeDataString(code)}",
            new UpdatePaymentMethodRequest(IsEnabled: enabled, DisplayName: null, DisplayOrder: null),
            cancellationToken);

    public Task UpdatePaymentMethodDisplayAsync(
        string code,
        string displayName,
        int displayOrder,
        CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync(
            HttpMethod.Patch,
            $"/payment-methods/{Uri.EscapeDataString(code)}",
            new UpdatePaymentMethodRequest(IsEnabled: null, DisplayName: displayName, DisplayOrder: displayOrder),
            cancellationToken);

    // ========================
    // Store feature flags
    // ========================

    public Task<StoreFeaturesDto> GetStoreFeaturesAsync(CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync<StoreFeaturesDto>(HttpMethod.Get, "/store/features", content: null, cancellationToken);

    // ========================
    // Product category catalog
    // ========================

    public Task<IReadOnlyList<ProductCategoryDto>> GetProductCategoriesAsync(
        CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync<IReadOnlyList<ProductCategoryDto>>(
            HttpMethod.Get, "/product-categories", content: null, cancellationToken);

    // ========================
    // Report methods (legacy format)
    // ========================

    public async Task<SalesSummary> GetLegacySalesSummaryAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        var url = $"/reports/legacy/sales-summary?{DateRangeQuery(fromDate, toDate)}";

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
        var url = $"/reports/legacy/payments-summary?{DateRangeQuery(fromDate, toDate)}";

        var result = await SendAuthenticatedAsync<PaymentsSummary>(
            HttpMethod.Get, url, content: null, cancellationToken);

        _logger.LogDebug("Fetched legacy payments summary from StoreHub: {FromDate} to {ToDate}", fromDate, toDate);
        return result;
    }

    public Task<SalesSummaryDto> GetSalesSummaryAsync(DateOnly fromDate, DateOnly toDate,
                                                      CancellationToken cancellationToken = default) =>
        // No top products: the till never shows them, and they cost a query.
        SendAuthenticatedAsync<SalesSummaryDto>(HttpMethod.Get,
            $"/reports/sales-summary?{DateRangeQuery(fromDate, toDate)}&topProductsCount=0", content: null, cancellationToken);

    public Task<SaleLinesPage> ListSaleLinesAsync(DateOnly fromDate, DateOnly toDate, int page, int pageSize,
                                                  CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync<SaleLinesPage>(HttpMethod.Get,
            $"/sales/lines?from={IsoDate(fromDate)}&to={IsoDate(toDate)}&page={page}&pageSize={pageSize}",
            content: null, cancellationToken);

    public Task<SalesPage> ListSalesAsync(DateOnly fromDate, DateOnly toDate, int page, int pageSize,
                                          CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync<SalesPage>(HttpMethod.Get,
            $"/sales?from={IsoDate(fromDate)}&to={IsoDate(toDate)}&page={page}&pageSize={pageSize}",
            content: null, cancellationToken);

    public Task<InvoiceDetailDto?> GetSaleAsync(Guid saleId, CancellationToken cancellationToken = default) =>
        GetAuthenticatedOrNotFoundAsync<InvoiceDetailDto>($"/sales/{saleId}", cancellationToken);

    public Task<InvoiceDetailDto?> GetSaleByNumberAsync(long invoiceNumber, CancellationToken cancellationToken = default) =>
        GetAuthenticatedOrNotFoundAsync<InvoiceDetailDto>(
            $"/sales/{invoiceNumber.ToString(CultureInfo.InvariantCulture)}", cancellationToken);

    public Task<InvoiceReprintResultDto> CreateReprintAsync(Guid saleId, CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync<InvoiceReprintResultDto>(HttpMethod.Post, $"/sales/{saleId}/reprints",
                                                        content: null, cancellationToken);

    public Task<PayLaterReportDto> GetPayLaterReportAsync(DateOnly? fromDate, DateOnly? toDate, int page, int pageSize,
                                                          CancellationToken cancellationToken = default)
    {
        // Completed debts too: the tab shows each customer's total ลงบัญชี as well as what is still owed.
        var url = $"/reports/pay-later?includeCompleted=true&page={page}&pageSize={pageSize}";
        if (fromDate is { } from && toDate is { } to)
            url += $"&{DateRangeQuery(from, to)}";

        return SendAuthenticatedAsync<PayLaterReportDto>(HttpMethod.Get, url, content: null, cancellationToken);
    }

    public Task<CashDrawerSummaryDto> GetCashSummaryAsync(DateOnly? businessDate, CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync<CashDrawerSummaryDto>(HttpMethod.Get, CashQuery("/cash/summary", businessDate), content: null, cancellationToken);

    public Task<IReadOnlyList<CashPayoutDto>> GetCashPayoutsAsync(DateOnly? businessDate, CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync<IReadOnlyList<CashPayoutDto>>(HttpMethod.Get, CashQuery(PayoutsRoute, businessDate), content: null, cancellationToken);

    public Task<CashPayoutDto> AddCashPayoutAsync(AddCashPayoutRequest request, CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync<CashPayoutDto>(HttpMethod.Post, PayoutsRoute, request, cancellationToken);

    public Task<CashPayoutDto> EditCashPayoutAsync(Guid id, EditCashPayoutRequest request, CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync<CashPayoutDto>(HttpMethod.Put, $"{PayoutsRoute}/{id}", request, cancellationToken);

    public Task DeleteCashPayoutAsync(Guid id, CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync(HttpMethod.Delete, $"{PayoutsRoute}/{id}", cancellationToken);

    public Task<IReadOnlyList<CashFloatDto>> GetCashFloatsAsync(DateOnly? businessDate, CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync<IReadOnlyList<CashFloatDto>>(HttpMethod.Get, CashQuery(FloatsRoute, businessDate), content: null, cancellationToken);

    public Task<CashFloatDto> AddCashFloatAsync(AddCashFloatRequest request, CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync<CashFloatDto>(HttpMethod.Post, FloatsRoute, request, cancellationToken);

    public Task<CashFloatDto> EditCashFloatAsync(Guid id, EditCashFloatRequest request, CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync<CashFloatDto>(HttpMethod.Put, $"{FloatsRoute}/{id}", request, cancellationToken);

    public Task DeleteCashFloatAsync(Guid id, CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync(HttpMethod.Delete, $"{FloatsRoute}/{id}", cancellationToken);

    public Task<IReadOnlyList<DebtRepaymentDto>> GetDebtRepaymentsAsync(DateOnly? businessDate, CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync<IReadOnlyList<DebtRepaymentDto>>(HttpMethod.Get, CashQuery(DebtRepaymentsRoute, businessDate), content: null, cancellationToken);

    public Task<DebtRepaymentDto> AddDebtRepaymentAsync(AddDebtRepaymentRequest request, CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync<DebtRepaymentDto>(HttpMethod.Post, DebtRepaymentsRoute, request, cancellationToken);

    public Task<DebtRepaymentDto> EditDebtRepaymentAsync(Guid id, EditDebtRepaymentRequest request, CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync<DebtRepaymentDto>(HttpMethod.Put, $"{DebtRepaymentsRoute}/{id}", request, cancellationToken);

    public Task DeleteDebtRepaymentAsync(Guid id, CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync(HttpMethod.Delete, $"{DebtRepaymentsRoute}/{id}", cancellationToken);

    public Task<IReadOnlyList<CashCountDto>> GetCashCountsAsync(DateOnly? businessDate, CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync<IReadOnlyList<CashCountDto>>(HttpMethod.Get, CashQuery(CountsRoute, businessDate), content: null, cancellationToken);

    public Task<CashCountDto> AddCashCountAsync(AddCashCountRequest request, CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync<CashCountDto>(HttpMethod.Post, CountsRoute, request, cancellationToken);

    private const string PayoutsRoute = "/cash/payouts";
    private const string FloatsRoute = "/cash/floats";
    private const string DebtRepaymentsRoute = "/cash/debt-repayments";
    private const string CountsRoute = "/cash/counts";

    // No date means today on the server, so it is left off rather than sent as the till's own date.
    private static string CashQuery(string path, DateOnly? businessDate) =>
        businessDate is { } day ? $"{path}?businessDate={IsoDate(day)}" : path;

    private static string IsoDate(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    #region Private Helpers

    /// <summary>
    /// The invariant culture is load-bearing: a Thai till formats years in the Buddhist calendar
    /// (2569), which StoreHub rejects as out of range.
    /// </summary>
    private static string DateRangeQuery(DateOnly fromDate, DateOnly toDate) =>
        $"fromDate={fromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}" +
        $"&toDate={toDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

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
        catch (Exception ex) when (IsTransportFailure(ex, cancellationToken))
        {
            throw ToClientException(ex, method, url);
        }
    }

    /// <summary>
    /// An authenticated GET where 404 means "no such thing" rather than a failure.
    /// </summary>
    private async Task<T?> GetAuthenticatedOrNotFoundAsync<T>(string url, CancellationToken cancellationToken)
        where T : class
    {
        EnsureAuthenticated();

        try
        {
            using var request = CreateAuthenticatedRequest(HttpMethod.Get, url);
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            await EnsureSuccessfulResponseAsync(response, HttpMethod.Get, url, cancellationToken);

            return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
                   ?? throw new StoreHubClientException("Invalid response from StoreHub");
        }
        catch (Exception ex) when (IsTransportFailure(ex, cancellationToken))
        {
            throw ToClientException(ex, HttpMethod.Get, url);
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
        catch (Exception ex) when (IsTransportFailure(ex, cancellationToken))
        {
            throw ToClientException(ex, method, url);
        }
    }

    /// <summary>
    /// Send an authenticated request with a JSON body, without expecting a response body.
    /// Use this when the endpoint returns an empty <c>Ok()</c> (e.g. PATCH) so
    /// <see cref="SendAuthenticatedAsync{T}"/>'s empty-body-throws behavior doesn't apply.
    /// </summary>
    private async Task SendAuthenticatedAsync(
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
        }
        catch (Exception ex) when (IsTransportFailure(ex, cancellationToken))
        {
            throw ToClientException(ex, method, url);
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

        var message = ServerError(responseBody) ?? response.StatusCode switch
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

    /// <summary>
    /// StoreHub's Thai reason, from the <c>{ error }</c> body every route writes; null for any other
    /// body (a proxy page, an empty 500), so the status message stands in.
    /// </summary>
    private static string? ServerError(string? responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
            return null;

        try
        {
            using var document = JsonDocument.Parse(responseBody);

            return document.RootElement.ValueKind == JsonValueKind.Object
                   && document.RootElement.TryGetProperty("error", out var error)
                   && error.ValueKind == JsonValueKind.String
                   && !string.IsNullOrWhiteSpace(error.GetString())
                ? error.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// A failure on the way to or from StoreHub, as opposed to the caller's own cancellation.
    /// HttpClient.Timeout surfaces as a TaskCanceledException with no caller token cancelled; unwrapped,
    /// it skipped every caller's StoreHub catch and reached the generic error dialog.
    /// </summary>
    private static bool IsTransportFailure(Exception ex, CancellationToken cancellationToken) =>
        ex is HttpRequestException or JsonException
        || (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested);

    private StoreHubClientException ToClientException(Exception ex, HttpMethod method, string url)
    {
        _logger.LogError(ex, "HTTP request failed: {Method} {Url}", method, url);

        var message = ex switch
        {
            OperationCanceledException => "StoreHub did not answer in time",
            JsonException => "Invalid response from StoreHub",
            _ => "Cannot connect to StoreHub"
        };

        return new StoreHubClientException(message, ex);
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
