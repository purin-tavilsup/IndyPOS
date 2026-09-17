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
using Xunit;

namespace IndyPOS.CloudApi.Tests.Infrastructure.Auth;

public class RegisterStoreHandlerTests
{
    private static CloudDbContext CreateInMemoryContext() =>
        new(new DbContextOptionsBuilder<CloudDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    [Fact]
    public async Task HandleAsync_ValidCommand_WritesConfigRowAndCreatesClientWithSameSecret()
    {
        await using var db = CreateInMemoryContext();
        string? secretHandedToClientStore = null;
        var credentialStore = new Mock<IStoreClientCredentialStore>();
        credentialStore
            .Setup(s => s.CreateAsync("store_store1", It.IsAny<string>(), "Test Store", It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, secret, _, _) => secretHandedToClientStore = secret)
            .Returns(Task.CompletedTask);

        var handler = new RegisterStoreHandler(db, credentialStore.Object, NullLogger<RegisterStoreHandler>.Instance);
        var command = new RegisterStoreCommand("store1", "Test Store", "Test Store Full");

        var response = await handler.HandleAsync(command);

        response.ClientId.Should().Be("store_store1");
        response.ClientSecret.Should().NotBeNullOrEmpty();
        response.ClientSecret.Should().Be(secretHandedToClientStore);

        var row = await db.StoreConfigs.SingleAsync();
        row.StoreId.Should().Be("store1");
        row.ClientId.Should().Be("store_store1");
        row.IsActive.Should().BeTrue();

        credentialStore.Verify(
            s => s.CreateAsync("store_store1", It.IsAny<string>(), "Test Store", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_DuplicateStoreId_ThrowsAndCreatesNoClient()
    {
        await using var db = CreateInMemoryContext();
        db.StoreConfigs.Add(new CloudStoreConfig { StoreId = "store1", ClientId = "store_store1", IsActive = true });
        await db.SaveChangesAsync();

        var credentialStore = new Mock<IStoreClientCredentialStore>();
        var handler = new RegisterStoreHandler(db, credentialStore.Object, NullLogger<RegisterStoreHandler>.Instance);

        var act = () => handler.HandleAsync(new RegisterStoreCommand("store1", "Test Store", "Test Store Full"));

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*store1*already exists*");
        credentialStore.Verify(
            s => s.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
