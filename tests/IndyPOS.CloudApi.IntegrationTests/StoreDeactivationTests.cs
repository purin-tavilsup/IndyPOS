using FluentAssertions;
using IndyPOS.Application.Abstractions.Cloud.Auth;
using IndyPOS.CloudApi.Domain;
using IndyPOS.CloudApi.Infrastructure;
using IndyPOS.CloudApi.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace IndyPOS.CloudApi.IntegrationTests;

/// <summary>
/// Deactivation is the safety valve for a stolen till PC. CloudApi's IsActive used to stop tokens in its own token
/// endpoint; tokens are Auth's now, so deactivating disables the store's client in Auth, and only then marks the
/// store inactive. Final: a store that comes back is registered again.
/// </summary>
public class StoreDeactivationTests(CloudPostgresFixture postgres) : IClassFixture<CloudPostgresFixture>
{
    private const string StoreId = "store-a";

    private async Task<CloudDbContext> RegisteredStoreAsync()
    {
        var db = CloudPostgresFixture.CreateContext(await postgres.CreateDatabaseAsync());
        db.StoreConfigs.Add(new CloudStoreConfig
        {
            StoreId = StoreId, StoreName = "A", StoreFullName = "A", ClientId = "store_store-a", IsActive = true, LastModifiedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return db;
    }

    private static async Task<bool> IsActiveAsync(CloudDbContext db)
    {
        db.ChangeTracker.Clear();
        return (await db.StoreConfigs.SingleAsync()).IsActive;
    }

    [Fact]
    public async Task DeactivateAsync_ForARegisteredStore_DisablesItsClientInAuth()
    {
        await using var db = await RegisteredStoreAsync();
        var auth = new Mock<IAuthStoreClients>();

        var outcome = await StoreDeactivation.DeactivateAsync(db, auth.Object, StoreId, CancellationToken.None);

        outcome.Should().Be(StoreDeactivationOutcome.Deactivated);
        auth.Verify(a => a.DisableAsync(StoreId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_ForARegisteredStore_MarksItInactive()
    {
        await using var db = await RegisteredStoreAsync();

        await StoreDeactivation.DeactivateAsync(db, Mock.Of<IAuthStoreClients>(), StoreId, CancellationToken.None);

        (await IsActiveAsync(db)).Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateAsync_ForAnUnknownStore_ReturnsNotFound()
    {
        await using var db = await RegisteredStoreAsync();
        var auth = new Mock<IAuthStoreClients>();

        var outcome = await StoreDeactivation.DeactivateAsync(db, auth.Object, "store-z", CancellationToken.None);

        outcome.Should().Be(StoreDeactivationOutcome.NotFound);
        auth.VerifyNoOtherCalls();
    }

    // The client in Auth is the real gate: if it could not be disabled, the store must not look deactivated.
    [Fact]
    public async Task DeactivateAsync_WhenAuthIsDown_LeavesTheStoreActive()
    {
        await using var db = await RegisteredStoreAsync();
        var auth = new Mock<IAuthStoreClients>();
        auth.Setup(a => a.DisableAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new AuthUnavailableException("down"));

        Func<Task> act = () => StoreDeactivation.DeactivateAsync(db, auth.Object, StoreId, CancellationToken.None);

        await act.Should().ThrowAsync<AuthUnavailableException>();
        (await IsActiveAsync(db)).Should().BeTrue();
    }
}
