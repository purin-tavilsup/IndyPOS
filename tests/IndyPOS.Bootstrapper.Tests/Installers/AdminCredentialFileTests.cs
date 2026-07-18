using FluentAssertions;
using IndyPOS.Bootstrapper.Installers;
using Xunit;

namespace IndyPOS.Bootstrapper.Tests.Installers;

public class AdminCredentialFileTests
{
    [Fact]
    public void Write_WithSeededAdmin_ShouldWriteCredentialFileAndReturnPath()
    {
        var config = new InstallationConfig { StoreId = "STORE-1" };
        var expectedPath = Path.Combine(config.ConfigDirectory, "admin-credentials.txt");

        try
        {
            var (path, locked) = AdminCredentialFile.Write(config);

            path.Should().Be(expectedPath);
            File.Exists(path).Should().BeTrue();
            var contents = File.ReadAllText(path);
            contents.Should().Contain($"Username: {config.AdminUsername}");
            contents.Should().Contain($"Password: {config.AdminPassword}");
            locked.Should().BeTrue();
        }
        finally
        {
            if (File.Exists(expectedPath)) File.Delete(expectedPath);
        }
    }
}
