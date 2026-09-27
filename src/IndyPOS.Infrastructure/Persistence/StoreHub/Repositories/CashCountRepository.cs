using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;

public class CashCountRepository(StoreHubDbContext db) : ICashCountRepository
{
    public async Task AddAsync(CashCount count, OutboxEvent outboxEvent, CancellationToken cancellationToken = default)
    {
        db.CashCounts.Add(count);
        db.OutboxEvents.Add(outboxEvent);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CashCount>> ListNewestFirstAsync(
        string storeId,
        DateOnly businessDate,
        CancellationToken cancellationToken = default) =>
        await NewestFirst(storeId, businessDate).ToListAsync(cancellationToken);

    public Task<CashCount?> GetLatestAsync(
        string storeId,
        DateOnly businessDate,
        CancellationToken cancellationToken = default) =>
        NewestFirst(storeId, businessDate).FirstOrDefaultAsync(cancellationToken);

    private IQueryable<CashCount> NewestFirst(string storeId, DateOnly businessDate) =>
        db.CashCounts
          .AsNoTracking()
          .Where(c => c.StoreId == storeId && c.BusinessDate == businessDate)
          .OrderByDescending(c => c.CreatedUtc)
          .ThenByDescending(c => c.Id);
}
