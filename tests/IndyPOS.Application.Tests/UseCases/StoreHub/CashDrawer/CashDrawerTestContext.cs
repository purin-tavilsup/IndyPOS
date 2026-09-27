using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;
using IndyPOS.Mock;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.CashDrawer;

/// <summary>
/// One InMemory StoreHub database, a fake clock set to 10:00 Bangkok on <see cref="Today"/>, and a
/// store in the Bangkok timezone. Real repositories, so the soft-delete filter is exercised.
/// </summary>
internal sealed class CashDrawerTestContext : IAsyncDisposable
{
    public static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");
    public static readonly DateOnly Today = new(2026, 9, 26);
    public static readonly DateOnly Yesterday = Today.AddDays(-1);

    /// <summary>10:00 Bangkok (UTC+7) on <see cref="Today"/>.</summary>
    public static readonly DateTimeOffset TenAmBangkok = new(2026, 9, 26, 3, 0, 0, TimeSpan.Zero);

    public static readonly Guid CashierId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid OtherCashierId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public CashDrawerTestContext()
    {
        var options = new DbContextOptionsBuilder<StoreHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        Db = new StoreHubDbContext(options);
        Time = new FakeTimeProvider(TenAmBangkok);
        StoreIdentity = MockStoreIdentityService.GeneralHardware();
        StoreIdentity.TimeZone = Bangkok;
        Clock = new CashDrawerClock(Time, StoreIdentity);
    }

    public StoreHubDbContext Db { get; }
    public FakeTimeProvider Time { get; }
    public MockStoreIdentityService StoreIdentity { get; }
    public ICashDrawerClock Clock { get; }

    public CashEntryRepository<TEntry> EntryRepository<TEntry>() where TEntry : CashDrawerEntry => new(Db);

    public CashCountRepository CountRepository() => new(Db);

    /// <summary>Seeds a payout directly, bypassing the handlers (e.g. a row from an earlier day).</summary>
    public async Task<CashPayout> SeedPayoutAsync(DateOnly businessDate, bool isDeleted = false)
    {
        var payout = new CashPayout
        {
            Id = Guid.NewGuid(),
            StoreId = StoreIdentity.StoreId,
            Amount = 100m,
            BusinessDate = businessDate,
            CreatedUtc = TenAmBangkok.UtcDateTime,
            LastModifiedUtc = TenAmBangkok.UtcDateTime,
            CreatedByUserId = CashierId,
            IsDeleted = isDeleted,
            DeletedUtc = isDeleted ? TenAmBangkok.UtcDateTime : null,
            LastModifiedByUserId = isDeleted ? CashierId : null
        };
        Db.CashPayouts.Add(payout);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        return payout;
    }

    public List<OutboxEvent> OutboxEvents() => Db.OutboxEvents.AsNoTracking().ToList();

    public ValueTask DisposeAsync() => Db.DisposeAsync();
}
