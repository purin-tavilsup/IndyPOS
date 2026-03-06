using FluentAssertions;
using IndyPOS.Application.Common.Models;
using Xunit;

namespace IndyPOS.Application.Tests.Models;

public class CashFlowDataTests
{
    [Fact]
    public void CalculateExpectedCash_WithAllPositiveValues_ReturnsCorrectTotal()
    {
        // Arrange
        var data = new CashFlowData
        {
            SalesTotalWithoutPayLaterPayments = 1000m,
            PaidPayLaterPayments = new[] { new PayLaterPayment { Amount = 200m } },
            Changes = new[] { new Change { Amount = 50m } },
            MoneyTransferTotal = 100m,
            WelfareCardTotal = 50m,
            Payouts = new[] { new Payout { Amount = 30m } }
        };

        // Act
        var expected = data.CalculateExpectedCash();

        // Assert
        // 1000 + 200 + 50 - 100 - 50 - 30 = 1070
        expected.Should().Be(1070m);
    }

    [Fact]
    public void CalculateExpectedCash_WithZeroValues_ReturnsZero()
    {
        // Arrange
        var data = new CashFlowData();

        // Act
        var expected = data.CalculateExpectedCash();

        // Assert
        expected.Should().Be(0m);
    }

    [Fact]
    public void CalculateActualCash_WithAllDenominations_ReturnsCorrectTotal()
    {
        // Arrange
        var data = new CashFlowData
        {
            BankNote1000Count = 2,  // 2000
            BankNote500Count = 3,   // 1500
            BankNote100Count = 5,   // 500
            BankNote50Count = 4,    // 200
            BankNote20Count = 10,   // 200
            Coin10Count = 15,       // 150
            Coin5Count = 8,         // 40
            Coin2Count = 5,         // 10
            Coin1Count = 3          // 3
        };

        // Act
        var actual = data.CalculateActualCash();

        // Assert
        // 2000 + 1500 + 500 + 200 + 200 + 150 + 40 + 10 + 3 = 4603
        actual.Should().Be(4603m);
    }

    [Fact]
    public void CalculateActualCash_WithZeroCounts_ReturnsZero()
    {
        // Arrange
        var data = new CashFlowData();

        // Act
        var actual = data.CalculateActualCash();

        // Assert
        actual.Should().Be(0m);
    }

    [Fact]
    public void CalculateCashDifference_WhenActualEqualsExpected_ReturnsZero()
    {
        // Arrange
        var data = new CashFlowData
        {
            SalesTotalWithoutPayLaterPayments = 1000m,
            BankNote1000Count = 1
        };

        // Act
        var diff = data.CalculateCashDifference();

        // Assert
        diff.Should().Be(0m);
    }

    [Fact]
    public void CalculateCashDifference_WhenActualGreaterThanExpected_ReturnsPositive()
    {
        // Arrange (overage scenario)
        var data = new CashFlowData
        {
            SalesTotalWithoutPayLaterPayments = 900m,
            BankNote1000Count = 1  // 1000 actual, 900 expected = +100 overage
        };

        // Act
        var diff = data.CalculateCashDifference();

        // Assert
        diff.Should().Be(100m);
    }

    [Fact]
    public void CalculateCashDifference_WhenActualLessThanExpected_ReturnsNegative()
    {
        // Arrange (shortage scenario)
        var data = new CashFlowData
        {
            SalesTotalWithoutPayLaterPayments = 1100m,
            BankNote1000Count = 1  // 1000 actual, 1100 expected = -100 shortage
        };

        // Act
        var diff = data.CalculateCashDifference();

        // Assert
        diff.Should().Be(-100m);
    }

    [Fact]
    public void ChangesTotal_SumsAllChanges()
    {
        // Arrange
        var data = new CashFlowData
        {
            Changes = new[]
            {
                new Change { Amount = 50m },
                new Change { Amount = 30m },
                new Change { Amount = 20m }
            }
        };

        // Act & Assert
        data.ChangesTotal.Should().Be(100m);
    }

    [Fact]
    public void PayoutsTotal_SumsAllPayouts()
    {
        // Arrange
        var data = new CashFlowData
        {
            Payouts = new[]
            {
                new Payout { Amount = 100m, Description = "Lunch" },
                new Payout { Amount = 50m, Description = "Supplies" }
            }
        };

        // Act & Assert
        data.PayoutsTotal.Should().Be(150m);
    }
}
