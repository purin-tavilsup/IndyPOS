using FluentAssertions;
using IndyPOS.Application.Abstractions.Cloud.Auth;
using IndyPOS.Application.UseCases.Cloud.Stores.RegisterStore;
using IndyPOS.CloudApi.Domain;
using IndyPOS.CloudApi.Infrastructure;
using IndyPOS.CloudApi.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
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

    private CloudDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<CloudDbContext>()
            .UseNpgsql(_postgres!.ConnectionString, npgsql => npgsql.EnableRetryOnFailure())
            .Options);

    [Fact]
    public async Task HandleAsync_UnderNpgsqlRetryStrategy_CommitsRegistration()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();

        var credentialStore = new Mock<IStoreClientCredentialStore>();
        credentialStore
            .Setup(s => s.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new RegisterStoreHandler(db, credentialStore.Object, NullLogger<RegisterStoreHandler>.Instance);

        var response = await handler.HandleAsync(new RegisterStoreCommand("tx-store-1", "Tx Store", "Tx Store Full"));

        response.ClientId.Should().Be("store_tx-store-1");

        var row = await db.StoreConfigs.SingleAsync();
        row.StoreId.Should().Be("tx-store-1");
        row.ClientId.Should().Be("store_tx-store-1");

        credentialStore.Verify(
            s => s.CreateAsync("store_tx-store-1", It.IsAny<string>(), "Tx Store", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenInsertViolatesUniqueConstraint_ThrowsConflictNotDbError()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();

        // Pre-seed a row that already owns the ClientId the new registration will generate. The new
        // command's StoreId differs, so the handler's StoreId existence check passes, but the insert
        // then loses the race to the unique ClientId index — the TOCTOU path. It must surface as a
        // conflict (InvalidOperationException -> 409), not a raw DbUpdateException (500).
        db.StoreConfigs.Add(new CloudStoreConfig
        {
            StoreId = "existing-store",
            StoreName = "Existing",
            ClientId = "store_race-store",
            IsActive = true,
            LastModifiedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var credentialStore = new Mock<IStoreClientCredentialStore>();
        credentialStore
            .Setup(s => s.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new RegisterStoreHandler(db, credentialStore.Object, NullLogger<RegisterStoreHandler>.Instance);

        var act = () => handler.HandleAsync(new RegisterStoreCommand("race-store", "Race Store", "Race Store Full"));

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*race-store*already exists*");
    }
}
