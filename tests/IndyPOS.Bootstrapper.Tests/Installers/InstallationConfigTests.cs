using FluentAssertions;
using IndyPOS.Bootstrapper.Installers;

namespace IndyPOS.Bootstrapper.Tests.Installers;

public class InstallationConfigTests
{
    [Fact]
    public void InstallationConfig_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var config = new InstallationConfig
        {
            StoreId = "STORE-001",
            AppPassword = "SecurePassword123!"
        };

        // Assert
        config.StoreId.Should().Be("STORE-001");
        config.AppPassword.Should().Be("SecurePassword123!");
    }

    [Fact]
    public void InstallationConfig_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var config = new InstallationConfig
        {
            StoreId = "STORE-001",
            AppPassword = "test"
        };

        // Assert
        config.PostgresBinPath.Should().Be(@"C:\Program Files\PostgreSQL\18\bin");
        config.DatabaseName.Should().Be("indypos_storehub");
        config.AppUser.Should().Be("indypos_app");
    }

    [Fact]
    public void InstallationConfig_CanOverrideDefaultValues()
    {
        // Arrange & Act
        var config = new InstallationConfig
        {
            StoreId = "BANGKOK-01",
            AppPassword = "test",
            DatabaseName = "indypos_bangkok",
            AppUser = "indypos_bkk"
        };

        // Assert
        config.DatabaseName.Should().Be("indypos_bangkok");
        config.AppUser.Should().Be("indypos_bkk");
    }
}

public class InstallationProgressTests
{
    [Fact]
    public void Step_ShouldCreateProgressWithStepInfo()
    {
        // Act
        var progress = InstallationProgress.Step("Installing", "Copying files...", 50);

        // Assert
        progress.StepName.Should().Be("Installing");
        progress.StatusMessage.Should().Be("Copying files...");
        progress.Percentage.Should().Be(50);
        progress.LogMessage.Should().BeNull();
        progress.IsError.Should().BeFalse();
    }

    [Fact]
    public void Step_WithoutPercentage_ShouldBeIndeterminate()
    {
        // Act
        var progress = InstallationProgress.Step("Loading", "Please wait...");

        // Assert
        progress.Percentage.Should().Be(-1);
    }

    [Fact]
    public void Log_ShouldCreateLogOnlyProgress()
    {
        // Act
        var progress = InstallationProgress.Log("Database created successfully");

        // Assert
        progress.LogMessage.Should().Be("Database created successfully");
        progress.StepName.Should().BeEmpty();
        progress.StatusMessage.Should().BeEmpty();
        progress.IsError.Should().BeFalse();
    }

    [Fact]
    public void Error_ShouldCreateErrorProgress()
    {
        // Act
        var progress = InstallationProgress.Error("Connection failed");

        // Assert
        progress.LogMessage.Should().Be("Connection failed");
        progress.IsError.Should().BeTrue();
    }
}
