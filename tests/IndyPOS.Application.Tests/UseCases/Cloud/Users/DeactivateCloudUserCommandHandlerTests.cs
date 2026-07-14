using IndyPOS.Application.Abstractions.Cloud.Repositories;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.UseCases.Cloud.Users.DeactivateUser;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.Cloud.Users;

public class DeactivateCloudUserCommandHandlerTests
{
    private readonly Mock<ICloudUserRepository> _repositoryMock;
    private readonly DeactivateCloudUserCommandHandler _handler;

    public DeactivateCloudUserCommandHandlerTests()
    {
        _repositoryMock = new Mock<ICloudUserRepository>();
        _handler = new DeactivateCloudUserCommandHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task HandleAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new DeactivateCloudUserCommand(userId);

        _repositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CloudUserEntity?)null);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.False(result.Found);
        Assert.Null(result.Id);
        Assert.Contains("not found", result.Message);

        _repositoryMock.Verify(r => r.UpdateAsync(
            It.IsAny<CloudUserEntity>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ActiveUser_DeactivatesAndIncrementsVersion()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingUser = new CloudUserEntity
        {
            Id = userId,
            StoreId = "store1",
            Username = "john.doe",
            FirstName = "John",
            LastName = "Doe",
            RoleId = (int)UserRole.Cashier,
            IsActive = true,
            Version = 5
        };

        var command = new DeactivateCloudUserCommand(userId);

        _repositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        _repositoryMock.Setup(r => r.GetNextVersionAsync("store1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(6L);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.Found);
        Assert.Equal(userId, result.Id);
        Assert.Equal(6L, result.Version);
        Assert.Contains("deactivated", result.Message);

        _repositoryMock.Verify(r => r.UpdateAsync(
            It.Is<CloudUserEntity>(u =>
                u.IsActive == false &&
                u.Version == 6),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_AlreadyDeactivated_ReturnsFoundWithMessage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingUser = new CloudUserEntity
        {
            Id = userId,
            StoreId = "store1",
            Username = "john.doe",
            FirstName = "John",
            LastName = "Doe",
            RoleId = (int)UserRole.Cashier,
            IsActive = false, // Already deactivated
            Version = 5
        };

        var command = new DeactivateCloudUserCommand(userId);

        _repositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.Found);
        Assert.Equal(userId, result.Id);
        Assert.Equal(5L, result.Version); // Version unchanged
        Assert.Contains("already deactivated", result.Message);

        // Should NOT call UpdateAsync since already deactivated
        _repositoryMock.Verify(r => r.UpdateAsync(
            It.IsAny<CloudUserEntity>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_IncrementsVersionForSync()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingUser = new CloudUserEntity
        {
            Id = userId,
            StoreId = "store1",
            Username = "john.doe",
            FirstName = "John",
            LastName = "Doe",
            RoleId = (int)UserRole.Cashier,
            IsActive = true,
            Version = 100
        };

        var command = new DeactivateCloudUserCommand(userId);

        _repositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        _repositoryMock.Setup(r => r.GetNextVersionAsync("store1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(101L);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.Equal(101L, result.Version);
        _repositoryMock.Verify(r => r.UpdateAsync(
            It.Is<CloudUserEntity>(u => u.Version == 101),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_UpdatesLastModifiedAtUtc()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingUser = new CloudUserEntity
        {
            Id = userId,
            StoreId = "store1",
            Username = "john.doe",
            FirstName = "John",
            LastName = "Doe",
            RoleId = (int)UserRole.Cashier,
            IsActive = true,
            Version = 1,
            LastModifiedAtUtc = DateTime.UtcNow.AddDays(-1)
        };

        var command = new DeactivateCloudUserCommand(userId);

        _repositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        _repositoryMock.Setup(r => r.GetNextVersionAsync("store1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(2L);

        var beforeTest = DateTime.UtcNow;

        // Act
        await _handler.HandleAsync(command);

        var afterTest = DateTime.UtcNow;

        // Assert
        _repositoryMock.Verify(r => r.UpdateAsync(
            It.Is<CloudUserEntity>(u =>
                u.LastModifiedAtUtc >= beforeTest &&
                u.LastModifiedAtUtc <= afterTest),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_PreservesOtherFieldsWhenDeactivating()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingUser = new CloudUserEntity
        {
            Id = userId,
            StoreId = "store1",
            Username = "john.doe",
            FirstName = "John",
            LastName = "Doe",
            RoleId = (int)UserRole.StoreManager,
            IsActive = true,
            Version = 1
        };

        var command = new DeactivateCloudUserCommand(userId);

        _repositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        _repositoryMock.Setup(r => r.GetNextVersionAsync("store1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(2L);

        // Act
        await _handler.HandleAsync(command);

        // Assert - all other fields should be unchanged
        _repositoryMock.Verify(r => r.UpdateAsync(
            It.Is<CloudUserEntity>(u =>
                u.Username == "john.doe" &&
                u.FirstName == "John" &&
                u.LastName == "Doe" &&
                u.RoleId == (int)UserRole.StoreManager &&
                u.IsActive == false), // Only this should change
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
