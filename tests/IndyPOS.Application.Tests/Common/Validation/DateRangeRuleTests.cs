using FluentAssertions;
using IndyPOS.Application.Common.Validation;
using Xunit;

namespace IndyPOS.Application.Tests.Common.Validation;

public class DateRangeRuleTests
{
    private static readonly DateOnly Day = new(2026, 10, 2);
    private static readonly DateOnly DayBeforeTheEarliest = DateRangeRule.EarliestDate.AddDays(-1);
    private static readonly DateOnly DayAfterTheLatest = DateRangeRule.LatestDate.AddDays(1);

    [Fact]
    public void FindViolation_WithToBeforeFrom_ReturnsTheSwappedRangeMessage()
    {
        DateRangeRule.FindViolation(Day, Day.AddDays(-1)).Should()
                                                         .Be(DateRangeRule.ToBeforeFromMessage);
    }

    [Fact]
    public void FindViolation_WithAFromBeforeTheEarliestDate_ReturnsTheBoundsMessage()
    {
        DateRangeRule.FindViolation(DayBeforeTheEarliest, Day).Should()
                                                              .Be(DateRangeRule.OutOfBoundsMessage);
    }

    [Fact]
    public void FindViolation_WithAToAfterTheLatestDate_ReturnsTheBoundsMessage()
    {
        DateRangeRule.FindViolation(Day, DayAfterTheLatest).Should()
                                                           .Be(DateRangeRule.OutOfBoundsMessage);
    }

    // ReportDateRange.ToUtcRange adds a day: DateOnly.MaxValue overflows into a 500.
    [Fact]
    public void FindViolation_WithTheLastRepresentableDate_ReturnsTheBoundsMessage()
    {
        DateRangeRule.FindViolation(Day, DateOnly.MaxValue).Should()
                                                           .Be(DateRangeRule.OutOfBoundsMessage);
    }

    [Fact]
    public void FindViolation_WithTheSupportedBounds_ReturnsNull()
    {
        DateRangeRule.FindViolation(DateRangeRule.EarliestDate, DateRangeRule.LatestDate).Should()
                                                                                         .BeNull();
    }

    [Fact]
    public void FindViolation_WithTheSameDay_ReturnsNull()
    {
        DateRangeRule.FindViolation(Day, Day).Should()
                                             .BeNull();
    }
}
