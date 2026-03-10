using IndyPOS.Application.Abstractions.Cloud.Repositories;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.UseCases.Cloud.Users.UpdateUser;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.Cloud.Users;

public class UpdateCloudUserCommandHandlerTests
{
    private readonly Mock<ICloudUserRepository> _repositoryMock;
    private readonly UpdateCloudUserCommandHandler _handler;

    public UpdateCloudUserCommandHandlerTests()
    {
        _repositoryMock = new Mock<ICloudUserRepository>();
        _handler = new UpdateCloudUserCommandHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task HandleAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new UpdateCloudUserCommand(
            Id: userId,
            FirstName: "Jane",
            LastName: null,
            RoleId: null,
            IsActive: null);

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
    public async Task HandleAsync_ValidUpdate_UpdatesFieldsAndIncrementsVersion()
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

        var command = new UpdateCloudUserCommand(
            Id: userId,
            FirstName: "Jane",
            LastName: "Smith",
            RoleId: (int)UserRole.StoreManager,
            IsActive: null);

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
        Assert.Contains("updated successfully", result.Message);

        _repositoryMock.Verify(r => r.UpdateAsync(
            It.Is<CloudUserEntity>(u =>
                u.FirstName == "Jane" &&
                u.LastName == "Smith" &&
                u.RoleId == (int)UserRole.StoreManager &&
                u.IsActive == true && // Unchanged
                u.Version == 6),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_PartialUpdate_OnlyUpdatesProvidedFields()
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
            Version = 1
        };

        var command = new UpdateCloudUserCommand(
            Id: userId,
            FirstName: "Jane", // Only update first name
            LastName: null,
            RoleId: null,
            IsActive: null);

        _repositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        _repositoryMock.Setup(r => r.GetNextVersionAsync("store1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(2L);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        _repositoryMock.Verify(r => r.UpdateAsync(
            It.Is<CloudUserEntity>(u =>
                u.FirstName == "Jane" &&
                u.LastName == "Doe" && // Unchanged
                u.RoleId == (int)UserRole.Cashier && // Unchanged
                u.IsActive == true), // Unchanged
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(99)]
    public async Task HandleAsync_InvalidRoleId_ThrowsInvalidOperationException(int invalidRoleId)
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
            Version = 1
        };

        var command = new UpdateCloudUserCommand(
            Id: userId,
            FirstName: null,
            LastName: null,
            RoleId: invalidRoleId,
            IsActive: null);

        _repositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.HandleAsync(command));

        Assert.Contains("Invalid RoleId", ex.Message);

        _repositoryMock.Verify(r => r.UpdateAsync(
            It.IsAny<CloudUserEntity>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_DeactivateViaUpdate_SetsIsActiveToFalse()
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
            Version = 1
        };

        var command = new UpdateCloudUserCommand(
            Id: userId,
            FirstName: null,
            LastName: null,
            RoleId: null,
            IsActive: false);

        _repositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        _repositoryMock.Setup(r => r.GetNextVersionAsync("store1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(2L);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        _repositoryMock.Verify(r => r.UpdateAsync(
            It.Is<CloudUserEntity>(u => u.IsActive == false),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_AlwaysIncrementsVersion()
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
            Version = 10
        };

        var command = new UpdateCloudUserCommand(
            Id: userId,
            FirstName: "Jane",
            LastName: null,
            RoleId: null,
            IsActive: null);

        _repositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        _repositoryMock.Setup(r => r.GetNextVersionAsync("store1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(11L);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.Equal(11L, result.Version);
        _repositoryMock.Verify(r => r.UpdateAsync(
            It.Is<CloudUserEntity>(u => u.Version == 11),
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

        var command = new UpdateCloudUserCommand(
            Id: userId,
            FirstName: "Jane",
            LastName: null,
            RoleId: null,
            IsActive: null);

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
}
