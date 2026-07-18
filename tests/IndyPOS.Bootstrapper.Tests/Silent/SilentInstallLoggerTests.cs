using FluentAssertions;
using IndyPOS.Bootstrapper.Installers;
using IndyPOS.Bootstrapper.Silent;

namespace IndyPOS.Bootstrapper.Tests.Silent;

public class SilentInstallLoggerTests
{
    [Fact]
    public void Report_WithLogMessage_ShouldWriteTimestampedLineToFile()
    {
        var sink = new StringWriter();
        using var logger = new SilentInstallLogger(sink);

        logger.Report(InstallationProgress.Log("Installing PostgreSQL"));

        sink.ToString().Should().Contain("Installing PostgreSQL");
    }

    [Fact]
    public void Report_WithErrorProgress_ShouldPrefixWarning()
    {
        var sink = new StringWriter();
        using var logger = new SilentInstallLogger(sink);

        logger.Report(InstallationProgress.Error("service failed"));

        sink.ToString().Should().Contain("WARNING: service failed");
    }

    [Fact]
    public void WriteMarker_ShouldWriteRawLineWithoutTimestamp()
    {
        var sink = new StringWriter();
        using var logger = new SilentInstallLogger(sink);

        logger.WriteMarker("INDYPOS_MARKER RESULT=success");

        var lines = sink.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        lines.Should().Contain("INDYPOS_MARKER RESULT=success");
    }
}
