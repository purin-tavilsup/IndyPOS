using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;

public class SaleRepository : ISaleRepository
{
    private readonly StoreHubDbContext _dbContext;

    public SaleRepository(StoreHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Invoice> CompleteSaleAsync(
        Invoice invoice,
        IReadOnlyList<InvoiceLine> lines,
        IReadOnlyList<Payment> payments,
        IReadOnlyList<InventoryMovement> inventoryMovements,
        OutboxEvent outboxEvent,
        CancellationToken cancellationToken = default)
    {
        // Use EF Core's built-in transaction via SaveChangesAsync
        // All entities added here will be committed atomically

        // 1. Add invoice
        _dbContext.Invoices.Add(invoice);

        // 2. Add invoice lines
        foreach (var line in lines)
        {
            line.InvoiceId = invoice.Id;
            _dbContext.InvoiceLines.Add(line);
        }

        // 3. Add payments
        foreach (var payment in payments)
        {
            payment.InvoiceId = invoice.Id;
            _dbContext.Payments.Add(payment);
        }

        // 4. Add inventory movements
        foreach (var movement in inventoryMovements)
        {
            movement.ReferenceId = invoice.Id;
            _dbContext.InventoryMovements.Add(movement);
        }

        // 5. Add outbox event for cloud sync
        _dbContext.OutboxEvents.Add(outboxEvent);

        // Commit all changes atomically
        await _dbContext.SaveChangesAsync(cancellationToken);

        return invoice;
    }
}
