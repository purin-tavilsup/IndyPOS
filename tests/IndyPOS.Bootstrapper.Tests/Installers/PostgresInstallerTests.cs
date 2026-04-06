using FluentAssertions;
using IndyPOS.Bootstrapper.Installers;

namespace IndyPOS.Bootstrapper.Tests.Installers;

public class PostgresInstallerResultTests
{
    [Fact]
    public void PostgresInstallerResult_Success_ShouldHaveAllProperties()
    {
        // Arrange & Act
        var result = new PostgresInstallerResult
        {
            Success = true,
            WasInstalled = true,
            BinPath = @"C:\Program Files\PostgreSQL\18\bin",
            SuperuserPassword = "SecurePassword123"
        };

        // Assert
        result.Success.Should().BeTrue();
        result.WasInstalled.Should().BeTrue();
        result.BinPath.Should().Be(@"C:\Program Files\PostgreSQL\18\bin");
        result.SuperuserPassword.Should().Be("SecurePassword123");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void PostgresInstallerResult_AlreadyInstalled_ShouldNotHavePassword()
    {
        // When PostgreSQL is already installed, we don't know the password
        var result = new PostgresInstallerResult
        {
            Success = true,
            WasInstalled = false,
            BinPath = @"C:\Program Files\PostgreSQL\18\bin",
            SuperuserPassword = "" // Unknown
        };

        // Assert
        result.Success.Should().BeTrue();
        result.WasInstalled.Should().BeFalse();
        result.SuperuserPassword.Should().BeEmpty();
    }

    [Fact]
    public void PostgresInstallerResult_Failure_ShouldHaveErrorMessage()
    {
        // Arrange & Act
        var result = new PostgresInstallerResult
        {
            Success = false,
            ErrorMessage = "Download failed"
        };

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Download failed");
    }
}

public class PostgresInstallerTests
{
    [Fact]
    public void PostgresInstaller_CanBeInstantiated()
    {
        // Act
        var installer = new PostgresInstaller();

        // Assert
        installer.Should().NotBeNull();
    }

    // Integration test - only runs on machines without PostgreSQL
    [Fact(Skip = "Integration test - requires PostgreSQL NOT to be installed")]
    public async Task EnsureInstalledAsync_WhenNotInstalled_ShouldAttemptDownload()
    {
        // This would test the download flow
        // Skipped because it would actually try to download PostgreSQL
    }

    // Integration test - only runs on machines with PostgreSQL
    [Fact(Skip = "Integration test - requires PostgreSQL to be installed")]
    public async Task EnsureInstalledAsync_WhenAlreadyInstalled_ShouldReturnSuccess()
    {
        // Arrange
        var installer = new PostgresInstaller();
        var progressReports = new List<DownloadProgress>();
        var progress = new Progress<DownloadProgress>(p => progressReports.Add(p));

        // Act
        var result = await installer.EnsureInstalledAsync(progress);

        // Assert
        result.Success.Should().BeTrue();
        result.WasInstalled.Should().BeFalse();
        result.BinPath.Should().Contain("PostgreSQL");
    }
}
