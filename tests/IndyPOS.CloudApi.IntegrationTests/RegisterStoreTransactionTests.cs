using FluentAssertions;
using IndyPOS.Application.Abstractions.Cloud.Auth;
using IndyPOS.Application.UseCases.Cloud.Stores.RegisterStore;
using IndyPOS.CloudApi.Domain;
using IndyPOS.CloudApi.Infrastructure;
using IndyPOS.CloudApi.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using IndyPOS.Testing.Postgres;
using Xunit;

namespace IndyPOS.CloudApi.IntegrationTests;

/// <summary>
/// Pins the store-registration transaction against real PostgreSQL configured exactly as Aspire's
/// AddNpgsqlDbContext configures it: with the Npgsql retrying execution strategy on. Under that
/// strategy a bare BeginTransactionAsync throws InvalidOperationException — the defect the InMemory
/// unit tests cannot reproduce (no retry strategy, transactions ignored). This test fails if the
/// handler's CreateExecutionStrategy().ExecuteAsync wrapper is ever removed.
///
/// Needs Docker, or INDYPOS_TEST_POSTGRES pointing at a server (see TestPostgres).
/// </summary>
public class RegisterStoreTransactionTests : IAsyncLifetime
{
    private TestPostgres? _postgres;

    public async Task InitializeAsync() => _postgres = await TestPostgres.StartAsync();

    public async Task DisposeAsync()
    {
        if (_postgres is not null)
        {
            await _postgres.DisposeAsync();
        }
    }

    private CloudDbContext CreateContext(params Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor[] interceptors) =>
        new(new DbContextOptionsBuilder<CloudDbContext>()
            .UseNpgsql(_postgres!.ConnectionString, npgsql => npgsql.EnableRetryOnFailure())
            .AddInterceptors(interceptors)
            .Options);

    private static Mock<IAuthStoreClients> AuthCreating()
    {
        var auth = new Mock<IAuthStoreClients>();
        auth.Setup(a => a.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string storeId, string _, CancellationToken _) => new StoreClientCredentials($"store_{storeId}", "secret"));
        return auth;
    }

    private static RegisterStoreHandler Handler(CloudDbContext db, IAuthStoreClients auth) =>
        new(db, auth, NullLogger<RegisterStoreHandler>.Instance);

    [Fact]
    public async Task HandleAsync_UnderNpgsqlRetryStrategy_CommitsRegistration()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();

        var response = await Handler(db, AuthCreating().Object).HandleAsync(new RegisterStoreCommand("tx-store-1", "Tx Store", "Tx Store Full"));

        response.ClientId.Should().Be("store_tx-store-1");
        (await db.StoreConfigs.SingleAsync()).StoreId.Should().Be("tx-store-1");
    }

    [Fact]
    public async Task HandleAsync_WhenInsertViolatesUniqueConstraint_ThrowsConflictWithoutCallingAuth()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();

        // Owns the ClientId the new registration will use; the StoreId check passes and the insert loses the race to
        // the unique ClientId index (the TOCTOU path). It must be a conflict (409), not a 500, and Auth is not asked.
        db.StoreConfigs.Add(new CloudStoreConfig
        {
            StoreId = "existing-store", StoreName = "Existing", ClientId = "store_race-store", IsActive = true, LastModifiedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var auth = AuthCreating();

        var act = () => Handler(db, auth.Object).HandleAsync(new RegisterStoreCommand("race-store", "Race Store", "Race Store Full"));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*race-store*already exists*");
        auth.Verify(a => a.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenAuthIsUnavailable_SavesNothing()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
        var auth = new Mock<IAuthStoreClients>();
        auth.Setup(a => a.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AuthUnavailableException("Auth is down"));

        var act = () => Handler(db, auth.Object).HandleAsync(new RegisterStoreCommand("down-store", "Down", "Down Full"));

        await act.Should().ThrowAsync<AuthUnavailableException>();
        await using var check = CreateContext();
        (await check.StoreConfigs.AnyAsync()).Should().BeFalse();
    }

    // The client exists in Auth before CloudApi commits. If the commit then fails, the client must go too, or an
    // enabled client would be left for a store CloudApi does not have.
    [Fact]
    public async Task HandleAsync_WhenTheCommitFailsAfterAuth_DeletesTheClientInAuth()
    {
        await using (var setup = CreateContext())
            await setup.Database.MigrateAsync();
        await using var db = CreateContext(new FailingCommit());
        var auth = AuthCreating();

        var act = () => Handler(db, auth.Object).HandleAsync(new RegisterStoreCommand("commit-store", "Commit", "Commit Full"));

        await act.Should().ThrowAsync<Exception>();
        auth.Verify(a => a.DeleteAsync("commit-store", It.IsAny<CancellationToken>()), Times.Once);
    }

    // The commit went through but reported a failure (a dropped connection, or a retry that then hit the unique index).
    // The store is registered, so the admin must get its secret and the client must stay.
    [Fact]
    public async Task HandleAsync_WhenTheCommitLandsButReportsAFailure_ReturnsTheSecretAndKeepsTheClient()
    {
        await using (var setup = CreateContext())
            await setup.Database.MigrateAsync();
        await using var db = CreateContext(new FailAfterCommit());
        var auth = AuthCreating();

        var response = await Handler(db, auth.Object).HandleAsync(new RegisterStoreCommand("landed-store", "Landed", "Landed Full"));

        response.ClientSecret.Should().Be("secret");
        auth.Verify(a => a.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Cancelled after Auth created the client: CloudApi rolls back, so the client must go too.
    [Fact]
    public async Task HandleAsync_WhenCancelledAfterAuth_DeletesTheClientInAuth()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
        using var cancellation = new CancellationTokenSource();
        var auth = new Mock<IAuthStoreClients>();
        auth.Setup(a => a.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                cancellation.Cancel();
                return new StoreClientCredentials("store_cancel-store", "secret");
            });

        var act = () => Handler(db, auth.Object).HandleAsync(new RegisterStoreCommand("cancel-store", "Cancel", "Cancel Full"), cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        auth.Verify(a => a.DeleteAsync("cancel-store", It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed class FailAfterCommit : DbTransactionInterceptor
    {
        public override Task TransactionCommittedAsync(
            System.Data.Common.DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The connection dropped after the commit (test).");
    }

    private sealed class FailingCommit : DbTransactionInterceptor
    {
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(
            System.Data.Common.DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The commit failed (test).");
    }
}
