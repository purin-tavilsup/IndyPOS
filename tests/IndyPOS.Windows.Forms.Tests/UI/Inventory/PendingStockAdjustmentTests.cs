using FluentAssertions;
using IndyPOS.Windows.Forms.UI.Inventory;
using Xunit;

namespace IndyPOS.Windows.Forms.Tests.UI.Inventory;

public class PendingStockAdjustmentTests
{
    [Fact]
    public void Delta_WithNoChanges_ShouldBeZero()
    {
        var adjustment = new PendingStockAdjustment(startingQuantity: 10);

        adjustment.Delta.Should()
                        .Be(0);
        adjustment.HasChange.Should()
                            .BeFalse();
    }

    [Fact]
    public void Increase_ShouldRaiseTheDisplayedQuantityAndTheDelta()
    {
        var adjustment = new PendingStockAdjustment(startingQuantity: 10);

        adjustment.Increase(5);

        adjustment.DisplayedQuantity.Should()
                                    .Be(15);
        adjustment.Delta.Should()
                        .Be(5);
    }

    [Theory]
    [InlineData(3, 2, 5)]
    [InlineData(10, -4, 6)]
    public void Increase_AppliedTwice_ShouldAccumulate(int first, int second, int expectedDelta)
    {
        var adjustment = new PendingStockAdjustment(startingQuantity: 10);

        adjustment.Increase(first);
        adjustment.Increase(second);

        adjustment.Delta.Should()
                        .Be(expectedDelta);
    }

    [Fact]
    public void Increase_ThenBackToTheStartingQuantity_ShouldReportNoChange()
    {
        // The form must not send a zero-delta adjustment: the endpoint rejects it, and
        // an empty movement row would be noise in the audit trail.
        var adjustment = new PendingStockAdjustment(startingQuantity: 10);

        adjustment.Increase(5);
        adjustment.Increase(-5);

        adjustment.HasChange.Should()
                            .BeFalse();
    }

    [Fact]
    public void Increase_BelowZero_ShouldBeAllowed()
    {
        // Nothing clamps. Negative stock is real and must stay visible - hiding it is
        // what defect 14b was.
        var adjustment = new PendingStockAdjustment(startingQuantity: 2);

        adjustment.Increase(-5);

        adjustment.DisplayedQuantity.Should()
                                    .Be(-3);
        adjustment.Delta.Should()
                        .Be(-5);
    }
}
