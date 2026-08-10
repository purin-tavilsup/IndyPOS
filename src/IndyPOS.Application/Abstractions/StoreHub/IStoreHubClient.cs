using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.StoreHub.Auth;
using IndyPOS.Application.UseCases.StoreHub.Auth.ChangePassword;
using IndyPOS.Application.UseCases.StoreHub.PayLater;
using IndyPOS.Application.UseCases.StoreHub.PaymentMethods;
using IndyPOS.Application.UseCases.StoreHub.ProductCategories;
using IndyPOS.Application.UseCases.StoreHub.Products;
using IndyPOS.Application.UseCases.StoreHub.Products.AdjustQuantity;
using IndyPOS.Application.UseCases.StoreHub.Products.Create;
using IndyPOS.Application.UseCases.StoreHub.Products.GetStock;
using IndyPOS.Application.UseCases.StoreHub.Products.Update;
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
    /// Change the authenticated user's password. On success the returned token is
    /// a fresh JWT without the must_change claim.
    /// </summary>
    Task<ChangePasswordResponse> ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken = default);

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
    /// Get current stock for this store, keyed by product id. Pass productId to narrow
    /// it to one product. Products with no movements are absent — read them as zero.
    /// Requires authentication.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, int>> GetProductStockAsync(
        Guid? productId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new product in StoreHub.
    /// Requires authentication and ProductsManage capability.
    /// </summary>
    Task<ProductDto> CreateProductAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing product in StoreHub.
    /// Requires authentication and ProductsManage capability.
    /// </summary>
    Task<ProductDto> UpdateProductAsync(
        UpdateProductCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft delete a product in StoreHub (sets IsActive = false).
    /// Requires authentication and ProductsManage capability.
    /// </summary>
    Task DeleteProductAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adjust product stock by a signed delta via inventory movement.
    /// Returns the balance after the movement.
    /// Requires authentication and InventoryAdjust capability.
    /// </summary>
    Task<AdjustQuantityResponse> AdjustProductQuantityAsync(
        Guid productId,
        AdjustQuantityRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate the next available barcode for the store.
    /// Requires authentication.
    /// </summary>
    Task<string> GenerateBarcodeAsync(CancellationToken cancellationToken = default);

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

    // ========================
    // PayLater endpoints
    // ========================

    /// <summary>
    /// Get PayLater records with optional filtering.
    /// </summary>
    Task<GetPayLaterResponse> GetPayLaterAsync(
        bool includeCompleted = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a single PayLater record by ID.
    /// </summary>
    Task<PayLaterDto?> GetPayLaterByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Record a payment against a PayLater account.
    /// </summary>
    Task<PayLaterDto> RecordPayLaterPaymentAsync(
        Guid payLaterId,
        decimal paymentAmount,
        CancellationToken cancellationToken = default);

    // ========================
    // Report endpoints (legacy format for WinForms)
    // ========================

    /// <summary>
    /// Get sales summary in legacy format.
    /// </summary>
    Task<SalesSummary> GetLegacySalesSummaryAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get payments summary in legacy format.
    /// </summary>
    Task<PaymentsSummary> GetLegacyPaymentsSummaryAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);

    // ========================
    // Payment method catalog endpoints
    // ========================

    /// <summary>
    /// Get payment methods offerable at this store (enabled + gated by store type).
    /// Requires authentication.
    /// </summary>
    Task<IReadOnlyList<PaymentMethodDto>> GetOfferablePaymentMethodsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all payment methods (enabled and disabled) for the admin catalog screen.
    /// Requires authentication and CanManagePaymentMethods capability.
    /// </summary>
    Task<IReadOnlyList<PaymentMethodDto>> GetAllPaymentMethodsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new government-campaign payment method to the catalog.
    /// Requires authentication and CanManagePaymentMethods capability.
    /// </summary>
    Task AddCampaignPaymentMethodAsync(
        string code,
        string displayName,
        int displayOrder,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enable or disable an existing payment method.
    /// Requires authentication and CanManagePaymentMethods capability.
    /// </summary>
    Task SetPaymentMethodEnabledAsync(
        string code,
        bool enabled,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing payment method's display name and display order.
    /// Requires authentication and CanManagePaymentMethods capability.
    /// </summary>
    Task UpdatePaymentMethodDisplayAsync(
        string code,
        string displayName,
        int displayOrder,
        CancellationToken cancellationToken = default);

    // ========================
    // Store feature flags
    // ========================

    /// <summary>
    /// Get the current store's feature flags (store-type gating).
    /// Requires authentication.
    /// </summary>
    Task<StoreFeaturesDto> GetStoreFeaturesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get this store's product categories, enabled and disabled alike, in display order.
    /// The POS filters for pickers; a disabled category is still returned so an existing
    /// product referencing it can render its label. Requires authentication.
    /// </summary>
    Task<IReadOnlyList<ProductCategoryDto>> GetProductCategoriesAsync(
        CancellationToken cancellationToken = default);
}
