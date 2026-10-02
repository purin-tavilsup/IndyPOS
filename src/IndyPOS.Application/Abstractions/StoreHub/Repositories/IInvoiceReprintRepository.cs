using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Application.Abstractions.StoreHub.Repositories;

/// <summary>Append-only: a reprint record is added, never updated or deleted — so there is no method to.</summary>
public interface IInvoiceReprintRepository
{
    /// <summary>Writes the row and its outbox event in ONE SaveChangesAsync.</summary>
    Task AddAsync(InvoiceReprint reprint, OutboxEvent outboxEvent, CancellationToken cancellationToken = default);
}
