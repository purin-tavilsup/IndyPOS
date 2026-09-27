using FluentAssertions;
using IndyPOS.Domain.Entities.Core;
using Xunit;

namespace IndyPOS.Domain.Tests.Entities;

public class CashCountTests
{
    [Fact]
    public void CountedTotal_WithNoNotesOrCoins_ReturnsZero()
    {
        var count = new CashCount();

        count.CountedTotal.Should()
                          .Be(0m);
    }

    [Theory]
    [InlineData(nameof(CashCount.BankNote1000Count), 1000)]
    [InlineData(nameof(CashCount.BankNote500Count), 500)]
    [InlineData(nameof(CashCount.BankNote100Count), 100)]
    [InlineData(nameof(CashCount.BankNote50Count), 50)]
    [InlineData(nameof(CashCount.BankNote20Count), 20)]
    [InlineData(nameof(CashCount.Coin10Count), 10)]
    [InlineData(nameof(CashCount.Coin5Count), 5)]
    [InlineData(nameof(CashCount.Coin2Count), 2)]
    [InlineData(nameof(CashCount.Coin1Count), 1)]
    public void CountedTotal_WithOneOfADenomination_ReturnsItsFaceValue(string property, int faceValue)
    {
        var count = new CashCount();
        typeof(CashCount).GetProperty(property)!.SetValue(count, 1);

        count.CountedTotal.Should()
                          .Be(faceValue);
    }

    [Fact]
    public void CountedTotal_WithMixedDenominations_ReturnsTheSum()
    {
        var count = new CashCount { BankNote1000Count = 2, BankNote100Count = 3, Coin5Count = 4, Coin1Count = 7 };

        count.CountedTotal.Should()
                          .Be(2327m);
    }
}
