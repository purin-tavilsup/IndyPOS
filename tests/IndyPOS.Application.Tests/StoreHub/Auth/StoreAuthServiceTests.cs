using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Abstractions.StoreHub.Services;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Services.StoreHub;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Auth;

public class StoreAuthServiceTests
{
    private readonly Mock<IStoreUserRepository> _userRepositoryMock;
    private readonly Mock<ICryptographyService> _legacyCryptoMock;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILocalTokenService _tokenService;
    private readonly Mock<ILogger<StoreAuthService>> _loggerMock;
    private readonly StoreAuthService _sut;

    public StoreAuthServiceTests()
    {
        _userRepositoryMock = new Mock<IStoreUserRepository>();
        _legacyCryptoMock = new Mock<ICryptographyService>();
        _passwordHasher = new BcryptPasswordHasher();
        _loggerMock = new Mock<ILogger<StoreAuthService>>();

        var tokenOptions = new LocalTokenOptions
        {
            SecretKey = "TestSecretKeyThatIsAtLeast32Characters!",
            Issuer = "IndyPOS.Test",
            Audience = "IndyPOS.TestClient",
            ExpiryHours = 12
        };
        _tokenService = new LocalTokenService(Options.Create(tokenOptions));

        _sut = new StoreAuthService(
            _userRepositoryMock.Object,
            _passwordHasher,
            _legacyCryptoMock.Object,
            _tokenService,
            _loggerMock.Object);
    }

    [Fact]
    public async Task AuthenticateAsync_WithValidBcryptCredentials_ReturnsSuccess()
    {
        // Arrange
        var password = "SecurePassword123!";
        var hashedPassword = _passwordHasher.Hash(password);
        var user = CreateTestUser(hashedPassword, passwordVersion: 2);

        _userRepositoryMock
            .Setup(x => x.GetByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.AuthenticateAsync("testuser", password);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Token);
        Assert.NotNull(result.User);
        Assert.Equal(user.Id, result.User.Id);
        Assert.Equal(user.Username, result.User.Username);
    }

    [Fact]
    public async Task AuthenticateAsync_WithLegacyTripleDesCredentials_MigratesToBcrypt()
    {
        // Arrange
        var password = "LegacyPassword123!";
        var legacyEncryptedPassword = "EncryptedBase64Value==";
        var user = CreateTestUser(legacyEncryptedPassword, passwordVersion: 1);

        _userRepositoryMock
            .Setup(x => x.GetByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _legacyCryptoMock
            .Setup(x => x.Encrypt(password))
            .Returns(legacyEncryptedPassword);

        // Act
        var result = await _sut.AuthenticateAsync("testuser", password);

        // Assert
        Assert.True(result.Success);

        // Verify password was migrated to BCrypt
        _userRepositoryMock.Verify(
            x => x.UpdatePasswordHashAsync(
                user.Id,
                It.Is<string>(h => h.StartsWith("$2")), // BCrypt format
                2, // Version 2
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AuthenticateAsync_WithInvalidPassword_ReturnsFailed()
    {
        // Arrange
        var correctPassword = "CorrectPassword123!";
        var wrongPassword = "WrongPassword456!";
        var hashedPassword = _passwordHasher.Hash(correctPassword);
        var user = CreateTestUser(hashedPassword, passwordVersion: 2);

        _userRepositoryMock
            .Setup(x => x.GetByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.AuthenticateAsync("testuser", wrongPassword);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Token);
        Assert.Equal("Invalid credentials", result.ErrorMessage);
    }

    [Fact]
    public async Task AuthenticateAsync_WithNonExistentUser_ReturnsFailed()
    {
        // Arrange
        _userRepositoryMock
            .Setup(x => x.GetByUsernameAsync("unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync((StoreUser?)null);

        // Act
        var result = await _sut.AuthenticateAsync("unknown", "anypassword");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid credentials", result.ErrorMessage);
    }

    [Fact]
    public async Task AuthenticateAsync_WithInactiveUser_ReturnsFailed()
    {
        // Arrange
        var password = "SecurePassword123!";
        var hashedPassword = _passwordHasher.Hash(password);
        var user = CreateTestUser(hashedPassword, passwordVersion: 2);
        user.IsActive = false;

        _userRepositoryMock
            .Setup(x => x.GetByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.AuthenticateAsync("testuser", password);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Account is inactive", result.ErrorMessage);
    }

    [Theory]
    [InlineData("", "password")]
    [InlineData("username", "")]
    [InlineData(" ", "password")]
    [InlineData("username", " ")]
    public async Task AuthenticateAsync_WithEmptyCredentials_ReturnsFailed(string username, string password)
    {
        // Act
        var result = await _sut.AuthenticateAsync(username, password);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Username and password are required", result.ErrorMessage);
    }

    [Fact]
    public async Task AuthenticateAsync_UpdatesLastLoginTimestamp()
    {
        // Arrange
        var password = "SecurePassword123!";
        var hashedPassword = _passwordHasher.Hash(password);
        var user = CreateTestUser(hashedPassword, passwordVersion: 2);

        _userRepositoryMock
            .Setup(x => x.GetByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _sut.AuthenticateAsync("testuser", password);

        // Assert
        _userRepositoryMock.Verify(
            x => x.UpdateLastLoginAsync(
                user.Id,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenUserMustChangePassword_ReturnsFlagTrue()
    {
        var password = "SecurePassword123!";
        var user = CreateTestUser(_passwordHasher.Hash(password), passwordVersion: 2);
        user.MustChangePassword = true;

        _userRepositoryMock
            .Setup(x => x.GetByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _sut.AuthenticateAsync("testuser", password);

        Assert.True(result.Success);
        Assert.True(result.MustChangePassword);
    }

    private static StoreUser CreateTestUser(string passwordHash, int passwordVersion) => new()
    {
        Id = Guid.NewGuid(),
        StoreId = "store-001",
        LegacyUserId = 1,
        Username = "testuser",
        PasswordHash = passwordHash,
        PasswordHashVersion = passwordVersion,
        FirstName = "Test",
        LastName = "User",
        RoleId = 1, // Cashier
        IsActive = true,
        CreatedAtUtc = DateTime.UtcNow,
        LastModifiedAtUtc = DateTime.UtcNow
    };
}
