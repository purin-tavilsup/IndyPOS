using FluentAssertions;
using IndyPOS.Bootstrapper.Installers;

namespace IndyPOS.Bootstrapper.Tests.Installers;

public class InstallationResultTests
{
    [Fact]
    public void InstallationResult_ShouldCarryServiceAndHealthFlags()
    {
        var result = new InstallationResult { AdminSeeded = true, ServiceStarted = true, HealthOk = false };

        result.AdminSeeded.Should().BeTrue();
        result.ServiceStarted.Should().BeTrue();
        result.HealthOk.Should().BeFalse();
    }
}
