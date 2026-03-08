using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Application.Abstractions.StoreHub.Repositories;

/// <summary>
/// Repository interface for StoreHub sale operations.
/// Handles the complete sale transaction atomically.
/// </summary>
public interface ISaleRepository
{
    /// <summary>
    /// Completes a sale atomically - creates invoice, lines, payments, inventory movements, and outbox event.
    /// All operations are performed in a single transaction.
    /// </summary>
    Task<Invoice> CompleteSaleAsync(
        Invoice invoice,
        IReadOnlyList<InvoiceLine> lines,
        IReadOnlyList<Payment> payments,
        IReadOnlyList<InventoryMovement> inventoryMovements,
        OutboxEvent outboxEvent,
        CancellationToken cancellationToken = default);
}
