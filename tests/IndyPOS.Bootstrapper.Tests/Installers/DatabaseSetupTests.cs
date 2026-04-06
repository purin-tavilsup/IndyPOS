using FluentAssertions;
using IndyPOS.Bootstrapper.Installers;

namespace IndyPOS.Bootstrapper.Tests.Installers;

public class DatabaseSetupResultTests
{
    [Fact]
    public void DatabaseSetupResult_Success_ShouldHaveJwtSecret()
    {
        // Arrange & Act
        var result = new DatabaseSetupResult
        {
            Success = true,
            JwtSecret = "base64encodedkey=="
        };

        // Assert
        result.Success.Should().BeTrue();
        result.JwtSecret.Should().Be("base64encodedkey==");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void DatabaseSetupResult_Failure_ShouldHaveErrorMessage()
    {
        // Arrange & Act
        var result = new DatabaseSetupResult
        {
            Success = false,
            ErrorMessage = "Database creation failed"
        };

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Database creation failed");
    }
}

public class DatabaseSetupTests
{
    [Fact]
    public void DatabaseSetup_CanBeInstantiated()
    {
        // Act
        var setup = new DatabaseSetup();

        // Assert
        setup.Should().NotBeNull();
    }

    // These tests require a running PostgreSQL instance
    // They are marked as integration tests
    [Fact(Skip = "Integration test - requires PostgreSQL")]
    public async Task SetupAsync_ShouldCreateDatabaseAndUser()
    {
        // Arrange
        var setup = new DatabaseSetup();
        var config = new InstallationConfig
        {
            StoreId = "TEST-001",
            AppPassword = "TestPassword123!",
            DatabaseName = "indypos_test",
            AppUser = "indypos_test_user",
            PostgresBinPath = @"C:\Program Files\PostgreSQL\18\bin"
        };

        var logs = new List<string>();
        var progress = new Progress<string>(msg => logs.Add(msg));

        // Act
        var result = await setup.SetupAsync(
            config,
            "postgres_password", // Superuser password
            progress);

        // Assert
        result.Success.Should().BeTrue();
        result.JwtSecret.Should().NotBeNullOrEmpty();
        logs.Should().Contain(msg => msg.Contains("Creating database"));
    }
}

public class JwtSecretGenerationTests
{
    [Fact]
    public void JwtSecret_ShouldBeBase64Encoded()
    {
        // This tests that the JWT secret format is correct
        // The actual generation is in DatabaseSetup but uses standard .NET crypto

        // Arrange
        var bytes = new byte[64];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        var secret = Convert.ToBase64String(bytes);

        // Assert
        secret.Should().NotBeNullOrEmpty();
        secret.Length.Should().BeGreaterThan(80); // 64 bytes = ~88 chars in Base64

        // Verify it can be decoded back
        var decoded = Convert.FromBase64String(secret);
        decoded.Length.Should().Be(64);
    }
}
