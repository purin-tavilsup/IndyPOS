using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Abstractions.StoreHub.Services;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.Users;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Services.StoreHub;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Services;

public class UserSyncServiceTests
{
    private readonly Mock<ICloudSyncClient> _cloudClient;
    private readonly Mock<IStoreUserRepository> _userRepository;
    private readonly Mock<IStoreIdentityService> _storeIdentity;
    private readonly UserSyncService _sut;

    public UserSyncServiceTests()
    {
        _cloudClient = new Mock<ICloudSyncClient>();
        _userRepository = new Mock<IStoreUserRepository>();
        _storeIdentity = new Mock<IStoreIdentityService>();
        var logger = Moq.Mock.Of<ILogger<UserSyncService>>();

        _storeIdentity.Setup(x => x.StoreId).Returns("store-001");

        _sut = new UserSyncService(
            _cloudClient.Object,
            _userRepository.Object,
            _storeIdentity.Object,
            logger);
    }

    [Fact]
    public async Task SyncUsersFromCloudAsync_WithNewUsers_SyncsAllUsers()
    {
        // Arrange
        _userRepository.Setup(x => x.GetMaxCloudVersionAsync("store-001", default))
            .ReturnsAsync(0);

        var cloudUsers = new List<CloudUserDto>
        {
            new(Guid.NewGuid(), "john", "John", "Doe", 1, true, 1),
            new(Guid.NewGuid(), "jane", "Jane", "Smith", 2, true, 2)
        };

        _cloudClient.Setup(x => x.GetUsersAsync("store-001", 0, default))
            .ReturnsAsync(new CloudUserSyncResponse(2, cloudUsers, 2, DateTime.UtcNow));

        // Act
        var result = await _sut.SyncUsersFromCloudAsync();

        // Assert
        result.Should().Be(2);
        _userRepository.Verify(x => x.UpsertByCloudIdAsync(
            It.IsAny<StoreUser>(), default), Times.Exactly(2));
    }

    [Fact]
    public async Task SyncUsersFromCloudAsync_WithNoChanges_ReturnsZero()
    {
        // Arrange
        _userRepository.Setup(x => x.GetMaxCloudVersionAsync("store-001", default))
            .ReturnsAsync(5);

        _cloudClient.Setup(x => x.GetUsersAsync("store-001", 5, default))
            .ReturnsAsync(new CloudUserSyncResponse(0, [], 5, DateTime.UtcNow));

        // Act
        var result = await _sut.SyncUsersFromCloudAsync();

        // Assert
        result.Should().Be(0);
        _userRepository.Verify(x => x.UpsertByCloudIdAsync(
            It.IsAny<StoreUser>(), default), Times.Never);
    }

    [Fact]
    public async Task SyncUsersFromCloudAsync_WhenCloudUnreachable_ReturnsZero()
    {
        // Arrange
        _userRepository.Setup(x => x.GetMaxCloudVersionAsync("store-001", default))
            .ReturnsAsync(0);

        _cloudClient.Setup(x => x.GetUsersAsync("store-001", 0, default))
            .ReturnsAsync((CloudUserSyncResponse?)null); // Cloud unreachable

        // Act
        var result = await _sut.SyncUsersFromCloudAsync();

        // Assert
        result.Should().Be(0);
        _userRepository.Verify(x => x.UpsertByCloudIdAsync(
            It.IsAny<StoreUser>(), default), Times.Never);
    }

    [Fact]
    public async Task SyncUsersFromCloudAsync_UsesIncrementalSync()
    {
        // Arrange
        var existingVersion = 10L;
        _userRepository.Setup(x => x.GetMaxCloudVersionAsync("store-001", default))
            .ReturnsAsync(existingVersion);

        _cloudClient.Setup(x => x.GetUsersAsync("store-001", existingVersion, default))
            .ReturnsAsync(new CloudUserSyncResponse(0, [], existingVersion, DateTime.UtcNow));

        // Act
        await _sut.SyncUsersFromCloudAsync();

        // Assert - Should call with the existing version for incremental sync
        _cloudClient.Verify(x => x.GetUsersAsync("store-001", existingVersion, default), Times.Once);
    }

    [Fact]
    public async Task SyncUsersFromCloudAsync_MapsUserFieldsCorrectly()
    {
        // Arrange
        _userRepository.Setup(x => x.GetMaxCloudVersionAsync("store-001", default))
            .ReturnsAsync(0);

        var cloudUserId = Guid.NewGuid();
        var cloudUser = new CloudUserDto(
            cloudUserId,
            "testuser",
            "Test",
            "User",
            2, // Manager
            true,
            5);

        _cloudClient.Setup(x => x.GetUsersAsync("store-001", 0, default))
            .ReturnsAsync(new CloudUserSyncResponse(1, [cloudUser], 5, DateTime.UtcNow));

        StoreUser? capturedUser = null;
        _userRepository.Setup(x => x.UpsertByCloudIdAsync(It.IsAny<StoreUser>(), default))
            .Callback<StoreUser, CancellationToken>((u, _) => capturedUser = u);

        // Act
        await _sut.SyncUsersFromCloudAsync();

        // Assert
        capturedUser.Should().NotBeNull();
        capturedUser!.CloudUserId.Should().Be(cloudUserId);
        capturedUser.Username.Should().Be("testuser");
        capturedUser.FirstName.Should().Be("Test");
        capturedUser.LastName.Should().Be("User");
        capturedUser.RoleId.Should().Be(2);
        capturedUser.IsActive.Should().BeTrue();
        capturedUser.CloudVersion.Should().Be(5);
        capturedUser.StoreId.Should().Be("store-001");
        capturedUser.LastSyncedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task SyncUsersFromCloudAsync_HandlesDeactivatedUsers()
    {
        // Arrange
        _userRepository.Setup(x => x.GetMaxCloudVersionAsync("store-001", default))
            .ReturnsAsync(0);

        var deactivatedUser = new CloudUserDto(
            Guid.NewGuid(),
            "inactive",
            "Inactive",
            "User",
            1,
            false, // Deactivated
            3);

        _cloudClient.Setup(x => x.GetUsersAsync("store-001", 0, default))
            .ReturnsAsync(new CloudUserSyncResponse(1, [deactivatedUser], 3, DateTime.UtcNow));

        StoreUser? capturedUser = null;
        _userRepository.Setup(x => x.UpsertByCloudIdAsync(It.IsAny<StoreUser>(), default))
            .Callback<StoreUser, CancellationToken>((u, _) => capturedUser = u);

        // Act
        await _sut.SyncUsersFromCloudAsync();

        // Assert - IsActive should be false (explicit deactivation)
        capturedUser.Should().NotBeNull();
        capturedUser!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task SyncUsersFromCloudAsync_ContinuesOnIndividualFailure()
    {
        // Arrange
        _userRepository.Setup(x => x.GetMaxCloudVersionAsync("store-001", default))
            .ReturnsAsync(0);

        var users = new List<CloudUserDto>
        {
            new(Guid.NewGuid(), "user1", "User", "One", 1, true, 1),
            new(Guid.NewGuid(), "user2", "User", "Two", 1, true, 2),
            new(Guid.NewGuid(), "user3", "User", "Three", 1, true, 3)
        };

        _cloudClient.Setup(x => x.GetUsersAsync("store-001", 0, default))
            .ReturnsAsync(new CloudUserSyncResponse(3, users, 3, DateTime.UtcNow));

        var callCount = 0;
        _userRepository.Setup(x => x.UpsertByCloudIdAsync(It.IsAny<StoreUser>(), default))
            .Callback<StoreUser, CancellationToken>((_, _) =>
            {
                callCount++;
                if (callCount == 2) // Fail on second user
                    throw new Exception("Database error");
            });

        // Act
        var result = await _sut.SyncUsersFromCloudAsync();

        // Assert - Should have attempted all 3, succeeded on 2 (1st and 3rd)
        result.Should().Be(2);
        _userRepository.Verify(x => x.UpsertByCloudIdAsync(
            It.IsAny<StoreUser>(), default), Times.Exactly(3));
    }

    [Fact]
    public async Task SyncUsersFromCloudAsync_SetsEmptyPasswordFields()
    {
        // Arrange - Cloud users don't have passwords, they're managed locally
        _userRepository.Setup(x => x.GetMaxCloudVersionAsync("store-001", default))
            .ReturnsAsync(0);

        var cloudUser = new CloudUserDto(
            Guid.NewGuid(), "newuser", "New", "User", 1, true, 1);

        _cloudClient.Setup(x => x.GetUsersAsync("store-001", 0, default))
            .ReturnsAsync(new CloudUserSyncResponse(1, [cloudUser], 1, DateTime.UtcNow));

        StoreUser? capturedUser = null;
        _userRepository.Setup(x => x.UpsertByCloudIdAsync(It.IsAny<StoreUser>(), default))
            .Callback<StoreUser, CancellationToken>((u, _) => capturedUser = u);

        // Act
        await _sut.SyncUsersFromCloudAsync();

        // Assert - Password fields should be empty (managed locally)
        capturedUser.Should().NotBeNull();
        capturedUser!.PasswordHash.Should().BeEmpty();
        capturedUser.PasswordHashVersion.Should().Be(0);
    }
}
