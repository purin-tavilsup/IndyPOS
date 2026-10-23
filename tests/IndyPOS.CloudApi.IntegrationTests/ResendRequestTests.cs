using FluentAssertions;
using IndyPOS.CloudApi.Domain;
using IndyPOS.CloudApi.Infrastructure;
using Xunit;

namespace IndyPOS.CloudApi.IntegrationTests;

/// <summary>
/// An admin asks a store to resend its Failed events. Pressing Resend again before the store picked the request up
/// must not stack up duplicates. Each test gets a fresh database.
/// </summary>
public class ResendRequestTests(CloudPostgresFixture postgres) : IClassFixture<CloudPostgresFixture>
{
    private const string ThisStore = "store-a";
    private static readonly DateTime Now = new(2026, 10, 23, 12, 0, 0, DateTimeKind.Utc);

    private async Task<CloudDbContext> RegisteredStoreAsync()
    {
        var db = CloudPostgresFixture.CreateContext(await postgres.CreateDatabaseAsync());
        db.StoreConfigs.Add(new CloudStoreConfig { StoreId = ThisStore, StoreName = "Store A", StoreFullName = "Store A", LastModifiedAtUtc = Now });
        await db.SaveChangesAsync();
        return db;
    }

    private static async Task<(Guid RequestId, bool Created)> RequestAsync(CloudDbContext db, Guid? eventId = null) =>
        (await ResendRequests.RequestAsync(db, ThisStore, eventId, Now, CancellationToken.None))!.Value;

    [Fact]
    public async Task RequestAsync_Twice_ReturnsTheSameOpenRequest()
    {
        await using var db = await RegisteredStoreAsync();
        var first = await RequestAsync(db);

        var second = await RequestAsync(db);

        second.Should().Be((first.RequestId, false));
    }

    [Fact]
    public async Task RequestAsync_AfterTheFirstCompleted_CreatesANewOne()
    {
        await using var db = await RegisteredStoreAsync();
        var first = await RequestAsync(db);
        (await db.SyncResendRequests.FindAsync(first.RequestId))!.CompletedAtUtc = Now;
        await db.SaveChangesAsync();

        var second = await RequestAsync(db);

        second.Created.Should().BeTrue();
        second.RequestId.Should().NotBe(first.RequestId);
    }

    [Fact]
    public async Task RequestAsync_ForOneEventAndForAll_KeepsThemApart()
    {
        await using var db = await RegisteredStoreAsync();
        var forAll = await RequestAsync(db);

        var forOne = await RequestAsync(db, Guid.NewGuid());

        forOne.RequestId.Should().NotBe(forAll.RequestId);
    }

    [Fact]
    public async Task RequestAsync_ForAnUnregisteredStore_ReturnsNull()
    {
        await using var db = await RegisteredStoreAsync();

        var result = await ResendRequests.RequestAsync(db, "store-z", eventId: null, Now, CancellationToken.None);

        result.Should().BeNull();
    }
}
