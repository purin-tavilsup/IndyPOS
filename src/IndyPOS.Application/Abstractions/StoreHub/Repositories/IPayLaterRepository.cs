using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Application.Abstractions.StoreHub.Repositories;

/// <summary>
/// Repository interface for PayLater operations in StoreHub.
/// </summary>
public interface IPayLaterRepository
{
    /// <summary>
    /// The debts taken in [fromUtc, toUtc), completed ones too, filtered by the database (the cash drawer's day).
    /// </summary>
    Task<IReadOnlyList<PayLater>> GetCreatedBetweenAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all PayLater records, optionally filtered.
    /// </summary>
    Task<IReadOnlyList<PayLater>> GetAllAsync(
        bool includeCompleted = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a PayLater record by its ID.
    /// </summary>
    Task<PayLater?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a PayLater record by invoice ID.
    /// </summary>
    Task<PayLater?> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a repayment: the debt's new totals, the repayment row and its outbox events, in one SaveChangesAsync.
    /// Throws <see cref="Common.Exceptions.PayLaterPaymentConflictException"/>, saving nothing, when another
    /// repayment changed the debt after it was read.
    /// </summary>
    Task RecordRepaymentAsync(
        PayLater debt,
        PayLaterRepayment repayment,
        IReadOnlyList<OutboxEvent> events,
        CancellationToken cancellationToken = default);
}
