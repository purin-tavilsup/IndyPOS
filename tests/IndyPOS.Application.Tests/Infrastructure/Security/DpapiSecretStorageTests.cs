using System.Runtime.Versioning;
using IndyPOS.Application.Abstractions.Security;
using IndyPOS.Infrastructure.Services.Security;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IndyPOS.Application.Tests.Infrastructure.Security;

/// <summary>
/// Tests for DpapiSecretStorage.
/// Note: These tests only run on Windows due to DPAPI dependency.
/// </summary>
[SupportedOSPlatform("windows")]
public class DpapiSecretStorageTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly ISecretStorage _storage;

    public DpapiSecretStorageTests()
    {
        // Create a unique test directory
        _testDirectory = Path.Combine(Path.GetTempPath(), $"IndyPOS_DpapiTests_{Guid.NewGuid():N}");
        var logger = new LoggerFactory().CreateLogger<DpapiSecretStorage>();
        _storage = new DpapiSecretStorage(_testDirectory, logger);
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task SetSecretAsync_StoresSecret_ReturnsTrue()
    {
        // Arrange
        const string key = "test-key";
        const string value = "super-secret-value";

        // Act
        var result = await _storage.SetSecretAsync(key, value);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetSecretAsync_ReturnsStoredValue()
    {
        // Arrange
        const string key = "test-key";
        const string value = "super-secret-value";
        await _storage.SetSecretAsync(key, value);

        // Act
        var result = await _storage.GetSecretAsync(key);

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetSecretAsync_NonExistentKey_ReturnsNull()
    {
        // Act
        var result = await _storage.GetSecretAsync("non-existent-key");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsAsync_ExistingKey_ReturnsTrue()
    {
        // Arrange
        const string key = "test-key";
        await _storage.SetSecretAsync(key, "value");

        // Act
        var result = await _storage.ExistsAsync(key);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_NonExistentKey_ReturnsFalse()
    {
        // Act
        var result = await _storage.ExistsAsync("non-existent-key");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteSecretAsync_ExistingKey_ReturnsTrue()
    {
        // Arrange
        const string key = "test-key";
        await _storage.SetSecretAsync(key, "value");

        // Act
        var result = await _storage.DeleteSecretAsync(key);

        // Assert
        Assert.True(result);
        Assert.False(await _storage.ExistsAsync(key));
    }

    [Fact]
    public async Task DeleteSecretAsync_NonExistentKey_ReturnsFalse()
    {
        // Act
        var result = await _storage.DeleteSecretAsync("non-existent-key");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SetSecretAsync_OverwritesExistingValue()
    {
        // Arrange
        const string key = "test-key";
        await _storage.SetSecretAsync(key, "original-value");

        // Act
        await _storage.SetSecretAsync(key, "new-value");
        var result = await _storage.GetSecretAsync(key);

        // Assert
        Assert.Equal("new-value", result);
    }

    [Fact]
    public async Task GetSecretAsync_HandlesSpecialCharacters()
    {
        // Arrange
        const string key = "test:key/with\\special:chars";
        const string value = "value with émojis 🔐 and ñ special chars";

        // Act
        await _storage.SetSecretAsync(key, value);
        var result = await _storage.GetSecretAsync(key);

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetSecretAsync_HandlesLongValues()
    {
        // Arrange
        const string key = "long-value-key";
        var value = new string('x', 10000); // 10KB value

        // Act
        await _storage.SetSecretAsync(key, value);
        var result = await _storage.GetSecretAsync(key);

        // Assert
        Assert.Equal(value, result);
    }
}
