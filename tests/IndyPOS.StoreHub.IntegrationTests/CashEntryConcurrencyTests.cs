using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Delete;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Payouts;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests;

/// <summary>
/// Two requests on the same cash entry, each with its own DbContext, forced into the worst
/// interleaving: both load the row before either saves, then the first saves, then the second.
/// Real PostgreSQL, because the guard is the UPDATE's WHERE clause.
/// </summary>
[Collection("Integration")]
public class CashEntryConcurrencyTests : IntegrationTestBase
{
    private const decimal FirstEditAmount = 200m;
    private const decimal SecondEditAmount = 300m;

    private static readonly DateOnly Day = new(2026, 9, 26);
    private static readonly DateTime NowUtc = new(2026, 9, 26, 3, 0, 0, DateTimeKind.Utc);
    private static readonly Guid FirstUserId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid SecondUserId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    private static readonly ICashDrawerClock Clock = new FixedClock(new CashDrawerInstant(NowUtc, Day));

    public CashEntryConcurrencyTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    private static Func<ICashEntryRepository<CashPayout>, Task> Delete(Guid id, Guid userId) =>
        repository => new DeleteCashEntryCommandHandler<CashPayout>(repository, Clock)
            .HandleAsync(new DeleteCashEntryCommand<CashPayout>(id, userId));

    private static Func<ICashEntryRepository<CashPayout>, Task> Edit(Guid id, Guid userId, decimal amount) =>
        repository => new EditCashPayoutCommandHandler(repository, Clock)
            .HandleAsync(new EditCashPayoutCommand(id, userId, amount, PayoutCategory.General, null));

    [Fact]
    public async Task Delete_WhenTwoDeletesRaceOnTheSameEntry_WritesOneOutboxEvent()
    {
        var id = await SeedActivePayoutAsync();

        await RaceAsync(Delete(id, FirstUserId), Delete(id, SecondUserId));

        (await CountEventsForAsync(id)).Should()
                                       .Be(1);
    }

    [Fact]
    public async Task Delete_WhenTwoDeletesRaceOnTheSameEntry_KeepsTheFirstDeletersAudit()
    {
        var id = await SeedActivePayoutAsync();

        await RaceAsync(Delete(id, FirstUserId), Delete(id, SecondUserId));

        (await StoredAsync(id)).LastModifiedByUserId.Should()
                                                    .Be(FirstUserId);
    }

    [Fact]
    public async Task Delete_WhenTwoDeletesRaceOnTheSameEntry_DoesNotThrowForTheSecond()
    {
        var id = await SeedActivePayoutAsync();

        var secondError = await RaceAsync(Delete(id, FirstUserId), Delete(id, SecondUserId));

        secondError.Should()
                   .BeNull();
    }

    [Fact]
    public async Task Edit_WhenTheEntryIsDeletedConcurrently_ThrowsNotFound()
    {
        var id = await SeedActivePayoutAsync();

        var secondError = await RaceAsync(Delete(id, FirstUserId), Edit(id, SecondUserId, SecondEditAmount));

        secondError.Should()
                   .BeOfType<CashEntryNotFoundException>();
    }

    [Fact]
    public async Task Edit_WhenTheEntryIsDeletedConcurrently_WritesNoEditEvent()
    {
        var id = await SeedActivePayoutAsync();

        await RaceAsync(Delete(id, FirstUserId), Edit(id, SecondUserId, SecondEditAmount));

        (await CountEventsForAsync(id)).Should()
                                       .Be(1);
    }

    [Fact]
    public async Task Edit_WhenTwoEditsRace_KeepsTheLastSave()
    {
        var id = await SeedActivePayoutAsync();

        await RaceAsync(Edit(id, FirstUserId, FirstEditAmount), Edit(id, SecondUserId, SecondEditAmount));

        (await StoredAsync(id)).Amount.Should()
                                      .Be(SecondEditAmount);
    }

    [Fact]
    public async Task TrySaveChangesAsync_AfterLosingARace_LeavesNothingForALaterSaveToRetry()
    {
        var id = await SeedActivePayoutAsync();
        await using var winnerScope = Factory.Services.CreateAsyncScope();
        await using var loserScope = Factory.Services.CreateAsyncScope();
        var loserDb = loserScope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        var winner = new CashEntryRepository<CashPayout>(winnerScope.ServiceProvider.GetRequiredService<StoreHubDbContext>());
        var loser = new CashEntryRepository<CashPayout>(loserDb);
        var winnerEntry = (await winner.FindAsync(id))!;
        var loserEntry = (await loser.FindAsync(id))!;
        winnerEntry.MarkDeleted(FirstUserId, NowUtc);
        loserEntry.MarkDeleted(SecondUserId, NowUtc);
        await winner.TrySaveChangesAsync(CashDrawerOutbox.Changed(winnerEntry, NowUtc));
        await loser.TrySaveChangesAsync(CashDrawerOutbox.Changed(loserEntry, NowUtc));

        await loserDb.SaveChangesAsync();

        (await CountEventsForAsync(id)).Should()
                                       .Be(1);
    }

    /// <summary>
    /// Runs both operations so each has loaded the entry before either saves, then lets the first
    /// save complete before the second saves. Returns what the second threw, or null.
    /// </summary>
    private async Task<Exception?> RaceAsync(
        Func<ICashEntryRepository<CashPayout>, Task> first,
        Func<ICashEntryRepository<CashPayout>, Task> second)
    {
        await using var firstScope = Factory.Services.CreateAsyncScope();
        await using var secondScope = Factory.Services.CreateAsyncScope();
        var firstRepository = PausingRepository.In(firstScope);
        var secondRepository = PausingRepository.In(secondScope);

        var firstRun = first(firstRepository);
        var secondRun = second(secondRepository);
        await Task.WhenAll(firstRepository.Loaded, secondRepository.Loaded);

        firstRepository.ReleaseSave();
        await firstRun;
        secondRepository.ReleaseSave();
        return await Record.ExceptionAsync(() => secondRun);
    }

    private async Task<Guid> SeedActivePayoutAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        var payout = new CashPayout
        {
            Id = Guid.NewGuid(),
            StoreId = TestStoreIdentityService.TestStoreId,
            Category = PayoutCategory.General,
            Amount = 100m,
            BusinessDate = Day,
            CreatedUtc = NowUtc,
            LastModifiedUtc = NowUtc,
            CreatedByUserId = Guid.NewGuid()
        };
        db.CashPayouts.Add(payout);
        await db.SaveChangesAsync();
        return payout.Id;
    }

    private async Task<CashPayout> StoredAsync(Guid id)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<StoreHubDbContext>()
                          .CashPayouts
                          .IgnoreQueryFilters()
                          .AsNoTracking()
                          .SingleAsync(p => p.Id == id);
    }

    private async Task<int> CountEventsForAsync(Guid id)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var idText = id.ToString();
        return await scope.ServiceProvider.GetRequiredService<StoreHubDbContext>()
                          .OutboxEvents
                          .CountAsync(e => e.Type == CashDrawerOutbox.CashPayoutChanged
                                           && e.PayloadJson.Contains(idText));
    }

    private sealed class FixedClock(CashDrawerInstant instant) : ICashDrawerClock
    {
        public CashDrawerInstant Now() => instant;
    }

    /// <summary>The real repository, paused after the load and before the save so the test decides the interleaving.</summary>
    private sealed class PausingRepository(ICashEntryRepository<CashPayout> inner) : ICashEntryRepository<CashPayout>
    {
        private readonly TaskCompletionSource _loaded = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _saveReleased = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public static PausingRepository In(AsyncServiceScope scope) =>
            new(new CashEntryRepository<CashPayout>(scope.ServiceProvider.GetRequiredService<StoreHubDbContext>()));

        public Task Loaded => _loaded.Task;

        public void ReleaseSave() => _saveReleased.SetResult();

        public async Task<CashPayout?> FindAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entry = await inner.FindAsync(id, cancellationToken);
            _loaded.SetResult();
            return entry;
        }

        public async Task<CashPayout?> FindIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entry = await inner.FindIncludingDeletedAsync(id, cancellationToken);
            _loaded.SetResult();
            return entry;
        }

        public Task<IReadOnlyList<CashPayout>> ListAsync(string storeId, DateOnly businessDate, CancellationToken cancellationToken = default) =>
            inner.ListAsync(storeId, businessDate, cancellationToken);

        public Task AddAsync(CashPayout entry, OutboxEvent outboxEvent, CancellationToken cancellationToken = default) =>
            inner.AddAsync(entry, outboxEvent, cancellationToken);

        public async Task<bool> TrySaveChangesAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default)
        {
            await _saveReleased.Task;
            return await inner.TrySaveChangesAsync(outboxEvent, cancellationToken);
        }
    }
}
