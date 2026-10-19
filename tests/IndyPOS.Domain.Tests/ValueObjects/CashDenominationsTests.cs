using FluentAssertions;
using IndyPOS.Domain.ValueObjects;
using Xunit;

namespace IndyPOS.Domain.Tests.ValueObjects;

public class CashDenominationsTests
{
    [Fact]
    public void Total_WithTooFewCounts_ThrowsArgumentException()
    {
        var act = () => CashDenominations.Total([1, 2, 3]);

        act.Should()
           .Throw<ArgumentException>();
    }

    [Fact]
    public void Total_WithOneOfEach_Returns1688()
    {
        var total = CashDenominations.Total([1, 1, 1, 1, 1, 1, 1, 1, 1]);

        total.Should()
             .Be(1688m);
    }
}
