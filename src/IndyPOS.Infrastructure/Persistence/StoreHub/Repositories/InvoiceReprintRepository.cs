using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;

public class InvoiceReprintRepository(StoreHubDbContext db) : IInvoiceReprintRepository
{
    public async Task AddAsync(InvoiceReprint reprint, OutboxEvent outboxEvent, CancellationToken cancellationToken = default)
    {
        db.InvoiceReprints.Add(reprint);
        db.OutboxEvents.Add(outboxEvent);
        await db.SaveChangesAsync(cancellationToken);
    }
}
