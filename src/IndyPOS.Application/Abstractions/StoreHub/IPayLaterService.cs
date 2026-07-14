using IndyPOS.Application.UseCases.StoreHub.PayLater;

namespace IndyPOS.Application.Abstractions.StoreHub;

/// <summary>
/// Service interface for PayLater operations.
/// Abstracts the underlying data source (SQLite or StoreHub API).
/// </summary>
public interface IPayLaterService
{
    /// <summary>
    /// Get all PayLater records with optional filtering.
    /// </summary>
    Task<GetPayLaterResponse> GetAllAsync(
        bool includeCompleted = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a single PayLater record by ID.
    /// </summary>
    Task<PayLaterDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Record a payment against a PayLater account.
    /// Returns the updated PayLater record.
    /// </summary>
    Task<PayLaterDto> RecordPaymentAsync(
        Guid payLaterId,
        decimal paymentAmount,
        CancellationToken cancellationToken = default);
}
