using FluentAssertions;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests;

/// <summary>
/// Real-PostgreSQL checks for the cash-drawer tables: the soft-delete filter, the category stored
/// by name, and the deterministic "latest count" order. InMemory cannot prove uuid ordering.
/// </summary>
[Collection("Integration")]
public class CashDrawerPersistenceTests : IntegrationTestBase
{
    private const string StoreId = "test-store";
    private static readonly DateOnly Day = new(2026, 9, 26);
    private static readonly DateTime SameInstantUtc = new(2026, 9, 26, 3, 0, 0, DateTimeKind.Utc);

    public CashDrawerPersistenceTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    private static CashPayout NewPayout(bool isDeleted = false) => new()
    {
        Id = Guid.NewGuid(),
        StoreId = StoreId,
        Category = PayoutCategory.Hardware,
        Amount = 120m,
        BusinessDate = Day,
        CreatedUtc = SameInstantUtc,
        LastModifiedUtc = SameInstantUtc,
        CreatedByUserId = Guid.NewGuid(),
        IsDeleted = isDeleted,
        DeletedUtc = isDeleted ? SameInstantUtc : null
    };

    private static CashCount NewCount(Guid id) => new()
    {
        Id = id,
        StoreId = StoreId,
        BusinessDate = Day,
        BankNote100Count = 1,
        CreatedUtc = SameInstantUtc,
        LastModifiedUtc = SameInstantUtc,
        CreatedByUserId = Guid.NewGuid()
    };

    private static OutboxEvent NewEvent() => new()
    {
        Id = Guid.NewGuid(), StoreId = StoreId, Type = "Test", PayloadJson = "{}", CreatedUtc = SameInstantUtc
    };

    private async Task<Guid> SeedPayoutAsync(bool isDeleted)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        var payout = NewPayout(isDeleted);
        db.CashPayouts.Add(payout);
        await db.SaveChangesAsync();
        return payout.Id;
    }

    private CashEntryRepository<CashPayout> PayoutRepository(out IServiceScope scope)
    {
        scope = Factory.Services.CreateScope();
        return new CashEntryRepository<CashPayout>(scope.ServiceProvider.GetRequiredService<StoreHubDbContext>());
    }

    [Fact]
    public async Task FindAsync_WithSoftDeletedPayout_ReturnsNull()
    {
        var id = await SeedPayoutAsync(isDeleted: true);
        var repository = PayoutRepository(out var scope);
        using var _ = scope;

        var found = await repository.FindAsync(id);

        found.Should()
             .BeNull();
    }

    [Fact]
    public async Task ListAsync_WithSoftDeletedPayout_ExcludesIt()
    {
        var id = await SeedPayoutAsync(isDeleted: true);
        var repository = PayoutRepository(out var scope);
        using var _ = scope;

        var list = await repository.ListAsync(StoreId, Day);

        list.Should()
            .NotContain(p => p.Id == id);
    }

    [Fact]
    public async Task FindIncludingDeletedAsync_WithSoftDeletedPayout_ReturnsIt()
    {
        var id = await SeedPayoutAsync(isDeleted: true);
        var repository = PayoutRepository(out var scope);
        using var _ = scope;

        var found = await repository.FindIncludingDeletedAsync(id);

        found!.IsDeleted.Should()
                        .BeTrue();
    }

    [Fact]
    public async Task SaveChangesAsync_WithCategory_StoresItsName()
    {
        var id = await SeedPayoutAsync(isDeleted: false);
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();

        var stored = await db.Database
                             .SqlQuery<string>($"SELECT category AS \"Value\" FROM cash_payout WHERE id = {id}")
                             .SingleAsync();

        stored.Should()
              .Be("Hardware");
    }

    [Fact]
    public async Task GetLatestAsync_WithTwoCountsAtTheSameInstant_ReturnsTheSameRowEveryTime()
    {
        await ResetDatabaseAsync();
        var repository = new CashCountRepository(GetDbContext());
        await repository.AddAsync(NewCount(Guid.NewGuid()), NewEvent());
        await repository.AddAsync(NewCount(Guid.NewGuid()), NewEvent());

        var first = await new CashCountRepository(GetDbContext()).GetLatestAsync(StoreId, Day);
        var second = await new CashCountRepository(GetDbContext()).GetLatestAsync(StoreId, Day);

        second!.Id.Should()
                  .Be(first!.Id);
    }

    [Fact]
    public async Task ListNewestFirstAsync_WithCountsAtDifferentTimes_ReturnsNewestFirst()
    {
        await ResetDatabaseAsync();
        var repository = new CashCountRepository(GetDbContext());
        var morning = NewCount(Guid.NewGuid());
        var evening = NewCount(Guid.NewGuid());
        evening.CreatedUtc = SameInstantUtc.AddHours(10);
        await repository.AddAsync(morning, NewEvent());
        await repository.AddAsync(evening, NewEvent());

        var list = await new CashCountRepository(GetDbContext()).ListNewestFirstAsync(StoreId, Day);

        list.Select(c => c.Id).Should()
                              .ContainInOrder(evening.Id, morning.Id);
    }

    [Fact]
    public async Task AddAsync_WithOutboxEvent_PersistsBothInOneSave()
    {
        var repository = PayoutRepository(out var scope);
        using var _ = scope;
        var payout = NewPayout();
        var outboxEvent = NewEvent();

        await repository.AddAsync(payout, outboxEvent);

        var db = GetDbContext();
        (await db.CashPayouts.AnyAsync(p => p.Id == payout.Id)).Should()
                                                               .BeTrue();
        (await db.OutboxEvents.AnyAsync(e => e.Id == outboxEvent.Id)).Should()
                                                                     .BeTrue();
    }
}
