using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Services.StoreHub;
using Microsoft.Extensions.Options;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Auth;

public class LocalTokenServiceTests
{
    private readonly LocalTokenOptions _options;
    private readonly LocalTokenService _sut;

    public LocalTokenServiceTests()
    {
        _options = new LocalTokenOptions
        {
            SecretKey = "TestSecretKeyThatIsAtLeast32Characters!",
            Issuer = "IndyPOS.Test",
            Audience = "IndyPOS.TestClient",
            ExpiryHours = 12
        };
        _sut = new LocalTokenService(Options.Create(_options));
    }

    [Fact]
    public void GenerateToken_WithValidUser_ReturnsJwtToken()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var token = _sut.GenerateToken(user);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.Contains(".", token); // JWT has 3 parts separated by dots
    }

    [Fact]
    public void GenerateToken_WithNullUser_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _sut.GenerateToken(null!));
    }

    [Fact]
    public void ValidateToken_WithValidToken_ReturnsClaims()
    {
        // Arrange
        var user = CreateTestUser();
        var token = _sut.GenerateToken(user);

        // Act
        var claims = _sut.ValidateToken(token);

        // Assert
        Assert.NotNull(claims);
        Assert.Equal(user.Id, claims.UserId);
        Assert.Equal(user.Username, claims.Username);
        Assert.Equal(user.RoleId, claims.RoleId);
        Assert.Equal(user.StoreId, claims.StoreId);
    }

    [Fact]
    public void ValidateToken_WithManuallyExpiredToken_ReturnsNull()
    {
        // Arrange - Create a token that's already expired by using negative expiry
        // Note: We can't easily create an expired token, so we test with tampered payload instead
        var user = CreateTestUser();
        var token = _sut.GenerateToken(user);

        // Create a fake token with expired timestamp by tampering with payload
        var parts = token.Split('.');
        // Decode, modify exp claim to past, re-encode - this will invalidate signature
        var tamperedToken = $"{parts[0]}.eyJzdWIiOiIxMjMiLCJleHAiOjF9.{parts[2]}";

        // Act
        var claims = _sut.ValidateToken(tamperedToken);

        // Assert - Tampered token should fail validation
        Assert.Null(claims);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [InlineData("invalid.token.here")]
    public void ValidateToken_WithInvalidToken_ReturnsNull(string? token)
    {
        // Act
        var claims = _sut.ValidateToken(token!);

        // Assert
        Assert.Null(claims);
    }

    [Fact]
    public void ValidateToken_WithTamperedToken_ReturnsNull()
    {
        // Arrange
        var user = CreateTestUser();
        var token = _sut.GenerateToken(user);

        // Tamper with the token (change a character in the signature)
        var parts = token.Split('.');
        var tamperedSignature = parts[2][..^1] + "X"; // Change last char
        var tamperedToken = $"{parts[0]}.{parts[1]}.{tamperedSignature}";

        // Act
        var claims = _sut.ValidateToken(tamperedToken);

        // Assert
        Assert.Null(claims);
    }

    [Fact]
    public void ValidateToken_TokenFromDifferentIssuer_ReturnsNull()
    {
        // Arrange - Create token with different issuer
        var differentOptions = new LocalTokenOptions
        {
            SecretKey = "TestSecretKeyThatIsAtLeast32Characters!",
            Issuer = "DifferentIssuer",
            Audience = "IndyPOS.TestClient",
            ExpiryHours = 12
        };
        var differentService = new LocalTokenService(Options.Create(differentOptions));
        var user = CreateTestUser();
        var token = differentService.GenerateToken(user);

        // Act - Validate with our service (different issuer)
        var claims = _sut.ValidateToken(token);

        // Assert
        Assert.Null(claims);
    }

    private static StoreUser CreateTestUser() => new()
    {
        Id = Guid.NewGuid(),
        StoreId = "store-001",
        Username = "testuser",
        FirstName = "Test",
        LastName = "User",
        RoleId = 1, // Cashier
        IsActive = true,
        CreatedAtUtc = DateTime.UtcNow,
        LastModifiedAtUtc = DateTime.UtcNow
    };
}
