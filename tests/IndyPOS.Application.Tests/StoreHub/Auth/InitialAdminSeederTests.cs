using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Abstractions.StoreHub.Services;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Persistence.StoreHub.Seeders;
using IndyPOS.Infrastructure.Services.StoreHub;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Auth;

public class InitialAdminSeederTests
{
    private readonly Mock<IStoreUserRepository> _repo = new();
    private readonly IPasswordHasher _hasher = new BcryptPasswordHasher();
    private readonly Mock<IStoreIdentityService> _identity = new();
    private readonly Mock<ILogger<InitialAdminSeeder>> _logger = new();

    private InitialAdminSeeder Build(string? username, string? password)
    {
        _identity.SetupGet(i => i.StoreId).Returns("STORE-1");
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["InitialAdmin:Username"] = username,
            ["InitialAdmin:Password"] = password
        }).Build();
        return new InitialAdminSeeder(_repo.Object, _hasher, _identity.Object, config, _logger.Object);
    }

    [Fact]
    public async Task SeedAsync_WhenAdminAbsent_SeedsWithMustChangeAndReturnsTrue()
    {
        _repo.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>())).ReturnsAsync((StoreUser?)null);
        var sut = Build("admin", "bootstrapPW123");

        var seeded = await sut.SeedAsync();

        Assert.True(seeded);
        _repo.Verify(r => r.AddAsync(It.Is<StoreUser>(u => u.MustChangePassword), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SeedAsync_WhenAdminExists_ReturnsFalse()
    {
        _repo.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>()))
             .ReturnsAsync(new StoreUser { Username = "admin" });
        var sut = Build("admin", "bootstrapPW123");

        var seeded = await sut.SeedAsync();

        Assert.False(seeded);
        _repo.Verify(r => r.AddAsync(It.IsAny<StoreUser>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SeedAsync_WhenNoConfig_ReturnsFalse()
    {
        var sut = Build(null, null);
        var seeded = await sut.SeedAsync();
        Assert.False(seeded);
    }

    [Fact]
    public async Task ResetAsync_WhenAdminExists_SetsPasswordAndMustChange()
    {
        var existing = new StoreUser { Id = Guid.NewGuid(), Username = "admin" };
        _repo.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        var sut = Build("admin", "ignored");

        await sut.ResetAsync("newBootstrap123");

        _repo.Verify(r => r.SetPasswordAsync(existing.Id, It.IsAny<string>(), true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetAsync_WhenAdminAbsent_CreatesAdminWithMustChange()
    {
        _repo.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>())).ReturnsAsync((StoreUser?)null);
        var sut = Build("admin", "ignored");

        await sut.ResetAsync("newBootstrap123");

        _repo.Verify(r => r.AddAsync(It.Is<StoreUser>(u => u.Username == "admin" && u.MustChangePassword), It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.SetPasswordAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
