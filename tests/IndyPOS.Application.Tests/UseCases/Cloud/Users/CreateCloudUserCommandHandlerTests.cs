using IndyPOS.Application.Abstractions.Cloud.Repositories;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.UseCases.Cloud.Users.CreateUser;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.Cloud.Users;

public class CreateCloudUserCommandHandlerTests
{
    private readonly Mock<ICloudUserRepository> _repositoryMock;
    private readonly CreateCloudUserCommandHandler _handler;

    public CreateCloudUserCommandHandlerTests()
    {
        _repositoryMock = new Mock<ICloudUserRepository>();
        _handler = new CreateCloudUserCommandHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_CreatesUser()
    {
        // Arrange
        var command = new CreateCloudUserCommand(
            StoreId: "store1",
            Username: "john.doe",
            FirstName: "John",
            LastName: "Doe",
            RoleId: (int)UserRole.Cashier);

        _repositoryMock.Setup(r => r.StoreExistsAsync("store1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repositoryMock.Setup(r => r.GetByUsernameAsync("store1", "john.doe", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CloudUserEntity?)null);
        _repositoryMock.Setup(r => r.GetNextVersionAsync("store1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("john.doe", result.Username);
        Assert.Equal("store1", result.StoreId);
        Assert.Equal(1L, result.Version);
        Assert.Contains("created successfully", result.Message);

        _repositoryMock.Verify(r => r.AddAsync(
            It.Is<CloudUserEntity>(u =>
                u.Username == "john.doe" &&
                u.StoreId == "store1" &&
                u.FirstName == "John" &&
                u.LastName == "Doe" &&
                u.RoleId == (int)UserRole.Cashier &&
                u.IsActive &&
                u.Version == 1),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_StoreDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange
        var command = new CreateCloudUserCommand(
            StoreId: "nonexistent",
            Username: "john.doe",
            FirstName: "John",
            LastName: "Doe",
            RoleId: (int)UserRole.Cashier);

        _repositoryMock.Setup(r => r.StoreExistsAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.HandleAsync(command));

        Assert.Contains("does not exist", ex.Message);
        Assert.Contains("nonexistent", ex.Message);

        _repositoryMock.Verify(r => r.AddAsync(
            It.IsAny<CloudUserEntity>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_UsernameAlreadyExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var command = new CreateCloudUserCommand(
            StoreId: "store1",
            Username: "existing.user",
            FirstName: "John",
            LastName: "Doe",
            RoleId: (int)UserRole.Cashier);

        _repositoryMock.Setup(r => r.StoreExistsAsync("store1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repositoryMock.Setup(r => r.GetByUsernameAsync("store1", "existing.user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudUserEntity { Id = Guid.NewGuid(), Username = "existing.user" });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.HandleAsync(command));

        Assert.Contains("already exists", ex.Message);
        Assert.Contains("existing.user", ex.Message);

        _repositoryMock.Verify(r => r.AddAsync(
            It.IsAny<CloudUserEntity>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(99)]
    public async Task HandleAsync_InvalidRoleId_ThrowsInvalidOperationException(int invalidRoleId)
    {
        // Arrange
        var command = new CreateCloudUserCommand(
            StoreId: "store1",
            Username: "john.doe",
            FirstName: "John",
            LastName: "Doe",
            RoleId: invalidRoleId);

        _repositoryMock.Setup(r => r.StoreExistsAsync("store1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repositoryMock.Setup(r => r.GetByUsernameAsync("store1", "john.doe", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CloudUserEntity?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.HandleAsync(command));

        Assert.Contains("Invalid RoleId", ex.Message);

        _repositoryMock.Verify(r => r.AddAsync(
            It.IsAny<CloudUserEntity>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(1, UserRole.Cashier)]
    [InlineData(2, UserRole.StoreManager)]
    [InlineData(3, UserRole.SystemAdmin)]
    public async Task HandleAsync_ValidRoles_CreatesUserWithCorrectRole(int roleId, UserRole expectedRole)
    {
        // Arrange
        var command = new CreateCloudUserCommand(
            StoreId: "store1",
            Username: "john.doe",
            FirstName: "John",
            LastName: "Doe",
            RoleId: roleId);

        _repositoryMock.Setup(r => r.StoreExistsAsync("store1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repositoryMock.Setup(r => r.GetByUsernameAsync("store1", "john.doe", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CloudUserEntity?)null);
        _repositoryMock.Setup(r => r.GetNextVersionAsync("store1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        _repositoryMock.Verify(r => r.AddAsync(
            It.Is<CloudUserEntity>(u => u.RoleId == (int)expectedRole),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_SetsVersionFromRepository()
    {
        // Arrange
        var command = new CreateCloudUserCommand(
            StoreId: "store1",
            Username: "john.doe",
            FirstName: "John",
            LastName: "Doe",
            RoleId: (int)UserRole.Cashier);

        _repositoryMock.Setup(r => r.StoreExistsAsync("store1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repositoryMock.Setup(r => r.GetByUsernameAsync("store1", "john.doe", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CloudUserEntity?)null);
        _repositoryMock.Setup(r => r.GetNextVersionAsync("store1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(42L); // Existing users, next version is 42

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.Equal(42L, result.Version);
        _repositoryMock.Verify(r => r.AddAsync(
            It.Is<CloudUserEntity>(u => u.Version == 42),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_SetsTimestamps()
    {
        // Arrange
        var command = new CreateCloudUserCommand(
            StoreId: "store1",
            Username: "john.doe",
            FirstName: "John",
            LastName: "Doe",
            RoleId: (int)UserRole.Cashier);

        _repositoryMock.Setup(r => r.StoreExistsAsync("store1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repositoryMock.Setup(r => r.GetByUsernameAsync("store1", "john.doe", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CloudUserEntity?)null);
        _repositoryMock.Setup(r => r.GetNextVersionAsync("store1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        var beforeTest = DateTime.UtcNow;

        // Act
        await _handler.HandleAsync(command);

        var afterTest = DateTime.UtcNow;

        // Assert
        _repositoryMock.Verify(r => r.AddAsync(
            It.Is<CloudUserEntity>(u =>
                u.CreatedAtUtc >= beforeTest &&
                u.CreatedAtUtc <= afterTest &&
                u.LastModifiedAtUtc >= beforeTest &&
                u.LastModifiedAtUtc <= afterTest),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_SetsIsActiveToTrue()
    {
        // Arrange
        var command = new CreateCloudUserCommand(
            StoreId: "store1",
            Username: "john.doe",
            FirstName: "John",
            LastName: "Doe",
            RoleId: (int)UserRole.Cashier);

        _repositoryMock.Setup(r => r.StoreExistsAsync("store1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repositoryMock.Setup(r => r.GetByUsernameAsync("store1", "john.doe", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CloudUserEntity?)null);
        _repositoryMock.Setup(r => r.GetNextVersionAsync("store1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        _repositoryMock.Verify(r => r.AddAsync(
            It.Is<CloudUserEntity>(u => u.IsActive),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
