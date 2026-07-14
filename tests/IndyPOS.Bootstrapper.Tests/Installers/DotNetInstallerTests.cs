using FluentAssertions;
using IndyPOS.Bootstrapper.Installers;

namespace IndyPOS.Bootstrapper.Tests.Installers;

public class DotNetInstallerResultTests
{
    [Fact]
    public void DotNetInstallerResult_Success_ShouldHaveCorrectState()
    {
        // Arrange & Act
        var result = new DotNetInstallerResult
        {
            Success = true,
            WasInstalled = false
        };

        // Assert
        result.Success.Should().BeTrue();
        result.WasInstalled.Should().BeFalse();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void DotNetInstallerResult_Failure_ShouldHaveErrorMessage()
    {
        // Arrange & Act
        var result = new DotNetInstallerResult
        {
            Success = false,
            WasInstalled = false,
            ErrorMessage = "User cancelled installation"
        };

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("User cancelled installation");
    }

    [Fact]
    public void DotNetInstallerResult_WhenInstalled_ShouldIndicateInstallation()
    {
        // Arrange & Act
        var result = new DotNetInstallerResult
        {
            Success = true,
            WasInstalled = true
        };

        // Assert
        result.Success.Should().BeTrue();
        result.WasInstalled.Should().BeTrue();
    }
}

// Note: Full DotNetInstaller tests would require mocking the registry and process execution.
// These are integration tests that verify the detection logic works on the actual machine.
public class DotNetInstallerIntegrationTests
{
    [Fact]
    public void DotNetInstaller_CanBeInstantiated()
    {
        // Act
        var installer = new DotNetInstaller();

        // Assert
        installer.Should().NotBeNull();
    }

    // This test verifies .NET detection on the current machine
    // It should pass on development machines with .NET 10 installed
    [Fact(Skip = "Integration test - requires .NET 10 to be installed")]
    public async Task EnsureInstalledAsync_WhenDotNet10Present_ShouldReturnSuccess()
    {
        // Arrange
        var installer = new DotNetInstaller();
        var progress = new Progress<string>(_ => { });

        // Act
        var result = await installer.EnsureInstalledAsync(progress);

        // Assert
        result.Success.Should().BeTrue();
        result.WasInstalled.Should().BeFalse(); // Already installed
    }
}
