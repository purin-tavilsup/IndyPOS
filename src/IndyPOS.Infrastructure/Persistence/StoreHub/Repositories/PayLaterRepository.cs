using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;

public class PayLaterRepository : IPayLaterRepository
{
    private readonly StoreHubDbContext _dbContext;
    private readonly IStoreIdentityService _storeIdentity;

    public PayLaterRepository(StoreHubDbContext dbContext, IStoreIdentityService storeIdentity)
    {
        _dbContext = dbContext;
        _storeIdentity = storeIdentity;
    }

    /// <summary>
    /// Base query that filters PayLater records by store via Invoice relationship.
    /// </summary>
    private IQueryable<PayLater> StorePayLaters => _dbContext.PayLaters
        .Where(p => _dbContext.Invoices
            .Any(i => i.Id == p.InvoiceId && i.StoreId == _storeIdentity.StoreId));

    public async Task<IReadOnlyList<PayLater>> GetAllAsync(
        bool includeCompleted = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var query = StorePayLaters.AsNoTracking();

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
        return await StorePayLaters
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<PayLater?> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        return await StorePayLaters
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.InvoiceId == invoiceId, cancellationToken);
    }

    public async Task UpdateAsync(PayLater payLater, CancellationToken cancellationToken = default)
    {
        // Verify PayLater belongs to this store
        var existing = await StorePayLaters
            .FirstOrDefaultAsync(p => p.Id == payLater.Id, cancellationToken)
            ?? throw new InvalidOperationException($"PayLater with ID {payLater.Id} not found");

        existing.PaidAmount = payLater.PaidAmount;
        existing.IsCompleted = payLater.IsCompleted;
        existing.LastModifiedUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
