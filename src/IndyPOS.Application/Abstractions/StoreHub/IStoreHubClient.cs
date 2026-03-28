using IndyPOS.Application.UseCases.StoreHub.Auth;
using IndyPOS.Application.UseCases.StoreHub.Products;
using IndyPOS.Application.UseCases.StoreHub.Sales;

namespace IndyPOS.Application.Abstractions.StoreHub;

/// <summary>
/// HTTP client abstraction for communicating with StoreHub API.
/// Used by WinForms app to call StoreHub endpoints.
/// </summary>
public interface IStoreHubClient
{
    /// <summary>
    /// Authenticate user and get JWT token.
    /// </summary>
    Task<LoginResponse> LoginAsync(string username, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all products from StoreHub.
    /// Requires authentication.
    /// </summary>
    Task<IReadOnlyList<ProductDto>> GetProductsAsync(
        bool activeOnly = true,
        string? category = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Complete a sale in StoreHub.
    /// Requires authentication.
    /// </summary>
    Task<CompleteSaleResponse> CompleteSaleAsync(
        CompleteSaleRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if StoreHub API is reachable and healthy.
    /// </summary>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Set the JWT token for authenticated requests.
    /// Called after successful login.
    /// </summary>
    void SetAuthToken(string token);

    /// <summary>
    /// Clear the current auth token (logout).
    /// </summary>
    void ClearAuthToken();

    /// <summary>
    /// Check if currently authenticated (has valid token).
    /// </summary>
    bool IsAuthenticated { get; }
}
