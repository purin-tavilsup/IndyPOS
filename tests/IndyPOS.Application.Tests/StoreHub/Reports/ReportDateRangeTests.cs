using FluentAssertions;
using IndyPOS.Infrastructure.QueryHandlers.Reports;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Reports;

public class ReportDateRangeTests
{
    private static readonly TimeZoneInfo UtcMinusSeven = TimeZoneInfo.CreateCustomTimeZone(
        id: "ReportTestUtcMinusSeven",
        baseUtcOffset: TimeSpan.FromHours(-7),
        displayName: "Report Test UTC-07",
        standardDisplayName: "Report Test UTC-07");

    [Fact]
    public void ToUtcRange_ShouldTreatReportDateAsLocalDay()
    {
        // Arrange
        var reportDate = new DateOnly(2026, 4, 26);

        // Act
        var range = ReportDateRange.ToUtcRange(reportDate, reportDate, UtcMinusSeven);

        // Assert
        range.StartUtc.Should().Be(new DateTime(2026, 4, 26, 7, 0, 0, DateTimeKind.Utc));
        range.EndExclusiveUtc.Should().Be(new DateTime(2026, 4, 27, 7, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ToUtcRange_ShouldUseExclusiveEndBoundary()
    {
        // Arrange
        var fromDate = new DateOnly(2026, 4, 26);
        var toDate = new DateOnly(2026, 4, 27);

        // Act
        var range = ReportDateRange.ToUtcRange(fromDate, toDate, UtcMinusSeven);

        // Assert
        range.StartUtc.Should().Be(new DateTime(2026, 4, 26, 7, 0, 0, DateTimeKind.Utc));
        range.EndExclusiveUtc.Should().Be(new DateTime(2026, 4, 28, 7, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ToUtcRange_WhenEndDateIsBeforeStartDate_ShouldThrow()
    {
        // Arrange
        var fromDate = new DateOnly(2026, 4, 27);
        var toDate = new DateOnly(2026, 4, 26);

        // Act
        var act = () => ReportDateRange.ToUtcRange(fromDate, toDate, UtcMinusSeven);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
