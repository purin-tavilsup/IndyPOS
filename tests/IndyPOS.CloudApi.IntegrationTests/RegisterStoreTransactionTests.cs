using FluentAssertions;
using IndyPOS.Application.Abstractions.Cloud.Auth;
using IndyPOS.Application.UseCases.Cloud.Stores.RegisterStore;
using IndyPOS.CloudApi.Infrastructure;
using IndyPOS.CloudApi.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Testcontainers.PostgreSql;
using Xunit;

namespace IndyPOS.CloudApi.IntegrationTests;

/// <summary>
/// Pins the store-registration transaction against real PostgreSQL configured exactly as Aspire's
/// AddNpgsqlDbContext configures it: with the Npgsql retrying execution strategy on. Under that
/// strategy a bare BeginTransactionAsync throws InvalidOperationException — the defect the InMemory
/// unit tests cannot reproduce (no retry strategy, transactions ignored). This test fails if the
/// handler's CreateExecutionStrategy().ExecuteAsync wrapper is ever removed.
///
/// Requires Docker (Testcontainers spins up postgres:16-alpine).
/// </summary>
public class RegisterStoreTransactionTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    private CloudDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<CloudDbContext>()
            .UseNpgsql(_postgres.GetConnectionString(), npgsql => npgsql.EnableRetryOnFailure())
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
}
