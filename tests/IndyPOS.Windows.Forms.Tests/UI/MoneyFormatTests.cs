using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace IndyPOS.Windows.Forms.Tests.UI;

/// <summary>
/// A bare "N" or "F" takes its decimal count from the till's culture, and .NET's ICU data gives
/// th-TH three: the sale screen's total read 50.000. Money on the till is formatted "N2"; this pins
/// it, since a label's text is not otherwise tested.
/// </summary>
public class MoneyFormatTests
{
    // An interpolation hole whose format is exactly N or F, e.g. {total:N}; {total:N2} passes.
    private static readonly Regex BareNumericFormat = new(@"\{[^{}:""]+:[NF]\}");

    [Fact]
    public void TillSource_WithInterpolatedNumbers_HasNoBareNOrFFormat()
    {
        var sites = Directory.EnumerateFiles(TillSourceDirectory(), "*.cs", SearchOption.AllDirectories)
                             .Where(IsSourceFile)
                             .SelectMany(BareFormatSites);

        sites.Should()
             .BeEmpty();
    }

    private static IEnumerable<string> BareFormatSites(string file) =>
        File.ReadLines(file)
            .Select((line, index) => (line, number: index + 1))
            .Where(l => BareNumericFormat.IsMatch(l.line))
            .Select(l => $"{Path.GetFileName(file)}:{l.number}");

    private static bool IsSourceFile(string file) =>
        !file.Split(Path.DirectorySeparatorChar).Any(part => part is "obj" or "bin");

    private static string TillSourceDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "IndyPOS.sln")))
            directory = directory.Parent;

        return Path.Combine(directory?.FullName ?? throw new DirectoryNotFoundException("IndyPOS.sln not found"),
                            "src", "IndyPOS.Windows.Forms");
    }
}
