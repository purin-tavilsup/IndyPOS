using IndyPOS.Infrastructure.Services.StoreHub;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Auth;

public class BcryptPasswordHasherTests
{
    private readonly BcryptPasswordHasher _sut = new();

    [Fact]
    public void Hash_WithValidPassword_ReturnsNonEmptyHash()
    {
        // Arrange
        var password = "SecurePassword123!";

        // Act
        var hash = _sut.Hash(password);

        // Assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        Assert.StartsWith("$2", hash); // BCrypt hash format
    }

    [Fact]
    public void Hash_SamePasswordTwice_ReturnsDifferentHashes()
    {
        // Arrange
        var password = "SecurePassword123!";

        // Act
        var hash1 = _sut.Hash(password);
        var hash2 = _sut.Hash(password);

        // Assert - BCrypt uses random salt, so hashes should differ
        Assert.NotEqual(hash1, hash2);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Hash_WithEmptyOrWhitespacePassword_ThrowsArgumentException(string password)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _sut.Hash(password));
    }

    [Fact]
    public void Hash_WithNullPassword_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _sut.Hash(null!));
    }

    [Fact]
    public void Verify_WithCorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "SecurePassword123!";
        var hash = _sut.Hash(password);

        // Act
        var result = _sut.Verify(password, hash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Verify_WithIncorrectPassword_ReturnsFalse()
    {
        // Arrange
        var password = "SecurePassword123!";
        var wrongPassword = "WrongPassword456!";
        var hash = _sut.Hash(password);

        // Act
        var result = _sut.Verify(wrongPassword, hash);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("", "somehash")]
    [InlineData("password", "")]
    [InlineData(null, "somehash")]
    [InlineData("password", null)]
    public void Verify_WithInvalidInputs_ReturnsFalse(string? password, string? hash)
    {
        // Act
        var result = _sut.Verify(password!, hash!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Verify_WithInvalidHashFormat_ReturnsFalse()
    {
        // Arrange - This is a TripleDES encrypted value, not BCrypt
        var password = "test";
        var invalidHash = "SomeBase64EncodedTripleDESValue==";

        // Act
        var result = _sut.Verify(password, invalidHash);

        // Assert
        Assert.False(result);
    }
}
