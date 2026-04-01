using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;

public class PayLaterRepository : IPayLaterRepository
{
    private readonly StoreHubDbContext _dbContext;

    public PayLaterRepository(StoreHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PayLater>> GetAllAsync(
        bool includeCompleted = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PayLaters.AsNoTracking();

        if (!includeCompleted)
        {
            query = query.Where(p => !p.IsCompleted);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowerSearchTerm = searchTerm.ToLowerInvariant();
            query = query.Where(p => p.Description.ToLower().Contains(lowerSearchTerm));
        }

        return await query
            .OrderByDescending(p => p.CreatedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<PayLater?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PayLaters
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<PayLater?> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PayLaters
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.InvoiceId == invoiceId, cancellationToken);
    }

    public async Task UpdateAsync(PayLater payLater, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.PayLaters
            .FirstOrDefaultAsync(p => p.Id == payLater.Id, cancellationToken)
            ?? throw new InvalidOperationException($"PayLater with ID {payLater.Id} not found");

        existing.PaidAmount = payLater.PaidAmount;
        existing.IsCompleted = payLater.IsCompleted;
        existing.LastModifiedUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
