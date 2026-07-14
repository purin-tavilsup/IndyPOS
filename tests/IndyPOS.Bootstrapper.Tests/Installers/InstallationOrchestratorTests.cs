using FluentAssertions;
using IndyPOS.Bootstrapper.Installers;

namespace IndyPOS.Bootstrapper.Tests.Installers;

public class InstallationExceptionTests
{
    [Fact]
    public void InstallationException_ShouldHaveMessage()
    {
        // Arrange & Act
        var exception = new InstallationException("Installation failed");

        // Assert
        exception.Message.Should().Be("Installation failed");
    }

    [Fact]
    public void InstallationException_ShouldWrapInnerException()
    {
        // Arrange
        var inner = new InvalidOperationException("Database error");

        // Act
        var exception = new InstallationException("Installation failed", inner);

        // Assert
        exception.Message.Should().Be("Installation failed");
        exception.InnerException.Should().Be(inner);
    }
}

public class InstallationOrchestratorTests
{
    [Fact]
    public void InstallationOrchestrator_CanBeInstantiated()
    {
        // Act
        var orchestrator = new InstallationOrchestrator();

        // Assert
        orchestrator.Should().NotBeNull();
    }

    // Full integration test - requires all prerequisites
    [Fact(Skip = "Full integration test - requires all prerequisites")]
    public async Task InstallAsync_FullInstallation_ShouldComplete()
    {
        // This would run the full installation on a clean machine
        // Skipped because it requires actual system changes
    }

    [Fact]
    public async Task InstallAsync_WithCancellation_ShouldThrow()
    {
        // Arrange
        var orchestrator = new InstallationOrchestrator();
        var config = new InstallationConfig
        {
            StoreId = "TEST-001",
            AppPassword = "TestPassword123!",
            AdminPassword = "AdminPassword123!"
        };

        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        var progress = new Progress<InstallationProgress>(_ => { });

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => orchestrator.InstallAsync(config, progress, cts.Token));
    }
}

public class StoreHubInstallerParseTests
{
    [Theory]
    [InlineData("ADMIN_SEEDED=true", true)]
    [InlineData("some log\nADMIN_SEEDED=true\nmore", true)]
    [InlineData("ADMIN_SEEDED=false", false)]
    [InlineData("no marker here", false)]
    public void ParseAdminSeeded_ReadsMarkerFromStdout(string stdout, bool expected)
    {
        StoreHubInstaller.ParseAdminSeeded(stdout).Should().Be(expected);
    }
}

public class ProgressReportingTests
{
    [Fact]
    public async Task Progress_ShouldReportSteps()
    {
        // Arrange
        var progressReports = new List<InstallationProgress>();
        IProgress<InstallationProgress> progress = new Progress<InstallationProgress>(p => progressReports.Add(p));

        // Act - Simulate progress reporting
        progress.Report(InstallationProgress.Step("Step 1", "Starting...", 0));
        progress.Report(InstallationProgress.Log("Processing item 1"));
        progress.Report(InstallationProgress.Step("Step 1", "In progress...", 50));
        progress.Report(InstallationProgress.Step("Step 1", "Complete", 100));

        // Allow async progress events to be processed
        await Task.Delay(100);

        // Assert
        progressReports.Should().HaveCountGreaterOrEqualTo(4);
        progressReports.First().StepName.Should().Be("Step 1");
        progressReports.Last().Percentage.Should().Be(100);
    }

    [Fact]
    public void Progress_ShouldDistinguishErrorsFromLogs()
    {
        // Arrange & Act
        var log = InstallationProgress.Log("Normal log message");
        var error = InstallationProgress.Error("Error message");

        // Assert
        log.IsError.Should().BeFalse();
        error.IsError.Should().BeTrue();
    }
}
