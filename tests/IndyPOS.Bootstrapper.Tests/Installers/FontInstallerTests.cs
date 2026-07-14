using FluentAssertions;
using IndyPOS.Bootstrapper.Installers;

namespace IndyPOS.Bootstrapper.Tests.Installers;

public class FontInstallerResultTests
{
    [Fact]
    public void FontInstallerResult_Success_CarriesCounts()
    {
        var result = new FontInstallerResult { Success = true, Installed = 2, Skipped = 0 };

        result.Success.Should().BeTrue();
        result.Installed.Should().Be(2);
        result.ErrorMessage.Should().BeNull();
    }
}

public class FontInstallerTests
{
    [Theory]
    [InlineData("FC Subject [Non-commercial] Reg", "FC Subject [Non-commercial] Reg (TrueType)")]
    [InlineData("FC Subject [Non-commercial] Bd", "FC Subject [Non-commercial] Bd (TrueType)")]
    public void BuildFontRegistryName_AppendsTrueTypeSuffix(string familyName, string expected)
    {
        FontInstaller.BuildFontRegistryName(familyName).Should().Be(expected);
    }
}
