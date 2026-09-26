using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;

public class CashEntryRepository<TEntry>(StoreHubDbContext db) : ICashEntryRepository<TEntry>
    where TEntry : CashDrawerEntry
{
    public Task<TEntry?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Set<TEntry>()
          .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<TEntry?> FindIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Set<TEntry>()
          .IgnoreQueryFilters([CashDrawerQueryFilters.SoftDelete])
          .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IReadOnlyList<TEntry>> ListAsync(
        string storeId,
        DateOnly businessDate,
        CancellationToken cancellationToken = default) =>
        await db.Set<TEntry>()
                .AsNoTracking()
                .Where(e => e.StoreId == storeId && e.BusinessDate == businessDate)
                .OrderBy(e => e.CreatedUtc)
                .ThenBy(e => e.Id)
                .ToListAsync(cancellationToken);

    public async Task AddAsync(TEntry entry, OutboxEvent outboxEvent, CancellationToken cancellationToken = default)
    {
        db.Set<TEntry>().Add(entry);
        db.OutboxEvents.Add(outboxEvent);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default)
    {
        db.OutboxEvents.Add(outboxEvent);
        await db.SaveChangesAsync(cancellationToken);
    }
}
