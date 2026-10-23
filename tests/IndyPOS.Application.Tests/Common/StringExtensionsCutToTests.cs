using FluentAssertions;
using IndyPOS.Application.Common.Extensions;
using Xunit;

namespace IndyPOS.Application.Tests.Common;

/// <summary>
/// Reasons are cut to fit a column or a light heartbeat. A cut between the two halves of a character outside the
/// BMP leaves a lone high surrogate, which PostgreSQL's UTF-8 encoding refuses with an exception.
/// </summary>
public class StringExtensionsCutToTests
{
    private const string Emoji = "\U0001F600";

    [Fact]
    public void CutTo_WithNull_ReturnsNull() =>
        ((string?)null).CutTo(10).Should().BeNull();

    [Fact]
    public void CutTo_WithAShortText_ReturnsItUnchanged() =>
        "short".CutTo(10).Should().Be("short");

    [Fact]
    public void CutTo_WithALongText_ReturnsTheFirstCharacters() =>
        "abcdefghij".CutTo(4).Should().Be("abcd");

    [Fact]
    public void CutTo_WhenTheCutFallsInsideAPair_DropsTheWholePair() =>
        ("abc" + Emoji + "def").CutTo(4).Should().Be("abc");

    [Fact]
    public void CutTo_WhenThePairFitsExactly_KeepsIt() =>
        ("abc" + Emoji + "def").CutTo(5).Should().Be("abc" + Emoji);
}
