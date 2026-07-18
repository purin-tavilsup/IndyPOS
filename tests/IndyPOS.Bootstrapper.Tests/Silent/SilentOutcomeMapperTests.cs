using FluentAssertions;
using IndyPOS.Bootstrapper.Silent;

namespace IndyPOS.Bootstrapper.Tests.Silent;

public class SilentOutcomeMapperTests
{
    [Fact]
    public void Map_WithSeededLockedSuccess_ShouldReturnZeroAndAllMarkers()
    {
        var outcome = new InstallSucceeded(AdminSeeded: true, CredFile: @"C:\x\admin-credentials.txt",
            CredLocked: true, ServiceStarted: true, HealthOk: true);

        var (exit, markers) = SilentOutcomeMapper.Map(outcome);

        exit.Should().Be(0);
        markers.Should().Contain("INDYPOS_MARKER RESULT=success");
        markers.Should().Contain("INDYPOS_MARKER ADMIN_SEEDED=true");
        markers.Should().Contain(@"INDYPOS_MARKER CRED_FILE=C:\x\admin-credentials.txt");
        markers.Should().Contain("INDYPOS_MARKER CRED_LOCKED=true");
        markers.Should().Contain("INDYPOS_MARKER SERVICE_STARTED=true");
        markers.Should().Contain("INDYPOS_MARKER HEALTH=ok");
    }

    [Fact]
    public void Map_WithUnlockedCredFile_ShouldStayZeroButFlagUnlocked()
    {
        var outcome = new InstallSucceeded(true, @"C:\x\admin-credentials.txt", CredLocked: false, true, true);

        var (exit, markers) = SilentOutcomeMapper.Map(outcome);

        exit.Should().Be(0);
        markers.Should().Contain("INDYPOS_MARKER CRED_LOCKED=false");
    }

    [Fact]
    public void Map_WithAdminAlreadyExisting_ShouldOmitCredFileAndEmitResetHint()
    {
        var outcome = new InstallSucceeded(AdminSeeded: false, CredFile: null, CredLocked: false,
            ServiceStarted: true, HealthOk: false);

        var (exit, markers) = SilentOutcomeMapper.Map(outcome);

        exit.Should().Be(0);
        markers.Should().Contain("INDYPOS_MARKER ADMIN_SEEDED=false");
        markers.Should().NotContain(m => m.StartsWith("INDYPOS_MARKER CRED_FILE="));
        markers.Should().Contain(m => m.StartsWith("INDYPOS_MARKER RESET_HINT="));
        markers.Should().Contain("INDYPOS_MARKER HEALTH=failed");
    }

    [Fact]
    public void Map_WithInstallFailed_ShouldReturnTwoAndOnlyResultMarker()
    {
        var (exit, markers) = SilentOutcomeMapper.Map(new InstallFailed("boom"));

        exit.Should().Be(2);
        markers.Should().ContainSingle().Which.Should().Be("INDYPOS_MARKER RESULT=failed");
    }

    [Fact]
    public void Map_WithTimeout_ShouldReturnFour()
    {
        var (exit, markers) = SilentOutcomeMapper.Map(new InstallTimedOut());

        exit.Should().Be(4);
        markers.Should().ContainSingle().Which.Should().Be("INDYPOS_MARKER RESULT=timeout");
    }

    [Fact]
    public void Map_WithUsageError_ShouldReturnOne()
    {
        SilentOutcomeMapper.Map(new UsageErrorOutcome("bad")).ExitCode.Should().Be(1);
    }

    [Fact]
    public void Map_WithNotElevated_ShouldReturnThree()
    {
        SilentOutcomeMapper.Map(new NotElevatedOutcome()).ExitCode.Should().Be(3);
    }
}
