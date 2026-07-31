using FluentAssertions;
using IndyPOS.Windows.Forms.UI.Errors;
using Xunit;

namespace IndyPOS.Windows.Forms.Tests.UI.Errors;

public class UiErrorReferenceTests
{
    [Fact]
    public void New_ShouldReturnTheAgreedShape()
    {
        // Staff read this aloud over the phone, so the shape is a contract:
        // ERR- plus four uppercase hex characters.
        UiErrorReference.New().Should().MatchRegex("^ERR-[0-9A-F]{4}$");
    }

    [Fact]
    public void New_ShouldUseHexOnly()
    {
        // Hex avoids the 0/O ambiguity when a code is spoken, because O is not
        // a hex digit. Sample enough times to catch a stray character class.
        // Assert the full string (not a `[4..]` slice) so the test does not
        // silently start checking the wrong thing if the "ERR-" prefix length
        // ever changes.
        for (var i = 0; i < 500; i++)
        {
            UiErrorReference.New().Should().MatchRegex("^ERR-[0-9A-F]{4}$");
        }
    }

    [Fact]
    public void New_CalledRepeatedly_ShouldVaryPerOccurrence()
    {
        // The code identifies an occurrence, not an error type, so two calls
        // must not return the same value in practice.
        var codes = Enumerable.Range(0, 200).Select(_ => UiErrorReference.New()).ToHashSet();

        codes.Should().HaveCountGreaterThan(150, "codes identify occurrences, not types");
    }
}
