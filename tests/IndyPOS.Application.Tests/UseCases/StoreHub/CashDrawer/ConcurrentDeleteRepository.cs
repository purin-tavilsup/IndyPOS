using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Domain.Entities.Core;
using Moq;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.CashDrawer;

/// <summary>
/// A repository whose entry loads as active but whose save reports it was deleted concurrently —
/// the loser's view of a race. The real race runs on PostgreSQL in StoreHub.IntegrationTests.
/// </summary>
internal static class ConcurrentDeleteRepository
{
    public static Mock<ICashEntryRepository<TEntry>> For<TEntry>(TEntry entry) where TEntry : CashDrawerEntry
    {
        var repository = new Mock<ICashEntryRepository<TEntry>>();
        repository.Setup(r => r.FindAsync(entry.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(entry);
        repository.Setup(r => r.FindIncludingDeletedAsync(entry.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(entry);
        repository.Setup(r => r.TrySaveChangesAsync(It.IsAny<OutboxEvent>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(false);
        return repository;
    }

    /// <summary>An active entry on <see cref="CashDrawerTestContext.Today"/>, so the day rule passes.</summary>
    public static TEntry ActiveToday<TEntry>() where TEntry : CashDrawerEntry, new() => new()
    {
        Id = Guid.NewGuid(),
        StoreId = "store",
        Amount = 100m,
        BusinessDate = CashDrawerTestContext.Today,
        CreatedByUserId = CashDrawerTestContext.CashierId
    };
}
