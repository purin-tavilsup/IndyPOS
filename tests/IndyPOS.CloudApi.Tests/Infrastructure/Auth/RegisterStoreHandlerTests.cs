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

/// <summary>
/// Register-store writes CloudApi's record of the store and asks Cloud.Auth for its client. The all-or-nothing part
/// across a failure is proved on real PostgreSQL (RegisterStoreTransactionTests); InMemory ignores transactions.
/// </summary>
public class RegisterStoreHandlerTests
{
    private static CloudDbContext CreateInMemoryContext() =>
        new(new DbContextOptionsBuilder<CloudDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    [Fact]
    public async Task HandleAsync_WhenAuthCreatesTheClient_SavesTheStoreAndReturnsTheSecret()
    {
        await using var db = CreateInMemoryContext();
        var auth = new Mock<IAuthStoreClients>();
        auth.Setup(a => a.CreateAsync("store1", "Test Store", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoreClientCredentials("store_store1", "secret-from-auth"));
        var handler = new RegisterStoreHandler(db, auth.Object, NullLogger<RegisterStoreHandler>.Instance);

        var response = await handler.HandleAsync(new RegisterStoreCommand("store1", "Test Store", "Test Store Full"));

        response.ClientId.Should().Be("store_store1");
        response.ClientSecret.Should().Be("secret-from-auth");
        var row = await db.StoreConfigs.SingleAsync();
        row.ClientId.Should().Be("store_store1");
        row.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ForAnExistingStore_ThrowsWithoutCallingAuth()
    {
        await using var db = CreateInMemoryContext();
        db.StoreConfigs.Add(new CloudStoreConfig { StoreId = "store1", ClientId = "store_store1", IsActive = true });
        await db.SaveChangesAsync();
        var auth = new Mock<IAuthStoreClients>();
        var handler = new RegisterStoreHandler(db, auth.Object, NullLogger<RegisterStoreHandler>.Instance);

        var act = () => handler.HandleAsync(new RegisterStoreCommand("store1", "Test Store", "Test Store Full"));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*store1*already exists*");
        auth.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("a/b")]
    [InlineData("a%2Fb")]
    [InlineData(" ")]
    public async Task HandleAsync_WithAnInvalidStoreId_ThrowsWithoutCallingAuth(string storeId)
    {
        await using var db = CreateInMemoryContext();
        var auth = new Mock<IAuthStoreClients>();
        var handler = new RegisterStoreHandler(db, auth.Object, NullLogger<RegisterStoreHandler>.Instance);

        var act = () => handler.HandleAsync(new RegisterStoreCommand(storeId, "Test Store", "Test Store Full"));

        await act.Should().ThrowAsync<ArgumentException>();
        auth.VerifyNoOtherCalls();
        (await db.StoreConfigs.AnyAsync()).Should().BeFalse();
    }
}
