using FluentAssertions;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.Common.Validation;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.Sales;

public class SalesQueryRulesTests
{
    private const int PageSizeAboveMaximum = SalesQueryRules.MaxPageSize + 1;
    private const int PageBeyondAddressableRows = int.MaxValue;
    private static readonly DateOnly Day = new(2026, 9, 27);

    [Theory]
    [InlineData("27/09/2026")]
    [InlineData("2026-02-30")]
    [InlineData("today")]
    public void ParseDate_WithAMalformedValue_Throws(string value)
    {
        var act = () => SalesQueryRules.ParseDate(value);

        act.Should()
           .Throw<SalesQueryValidationException>();
    }

    [Fact]
    public void EnsureValidRange_WithToBeforeFrom_Throws()
    {
        var act = () => SalesQueryRules.EnsureValidRange(Day, Day.AddDays(-1));

        act.Should()
           .Throw<SalesQueryValidationException>();
    }

    // ReportDateRange.ToUtcRange does toDate.AddDays(1) and converts from the store's timezone, so
    // DateOnly.MaxValue overflows and DateOnly.MinValue underflows at +07:00 -- each a 500.
    [Fact]
    public void EnsureValidRange_WithTheLastRepresentableDate_Throws()
    {
        var act = () => SalesQueryRules.EnsureValidRange(DateOnly.MaxValue, DateOnly.MaxValue);

        act.Should()
           .Throw<SalesQueryValidationException>();
    }

    [Fact]
    public void EnsureValidRange_WithTheFirstRepresentableDate_Throws()
    {
        var act = () => SalesQueryRules.EnsureValidRange(DateOnly.MinValue, Day);

        act.Should()
           .Throw<SalesQueryValidationException>();
    }

    [Fact]
    public void EnsureValidRange_WithTheSupportedBounds_DoesNotThrow()
    {
        var act = () => SalesQueryRules.EnsureValidRange(DateRangeRule.EarliestDate, DateRangeRule.LatestDate);

        act.Should()
           .NotThrow();
    }

    [Fact]
    public void EnsureValidPage_WithPageZero_Throws()
    {
        var act = () => SalesQueryRules.EnsureValidPage(0, SalesQueryRules.DefaultPageSize);

        act.Should()
           .Throw<SalesQueryValidationException>();
    }

    [Fact]
    public void EnsureValidPage_WithPageSizeZero_Throws()
    {
        var act = () => SalesQueryRules.EnsureValidPage(SalesQueryRules.FirstPage, 0);

        act.Should()
           .Throw<SalesQueryValidationException>();
    }

    [Fact]
    public void EnsureValidPage_WithPageSizeAboveMaximum_Throws()
    {
        var act = () => SalesQueryRules.EnsureValidPage(SalesQueryRules.FirstPage, PageSizeAboveMaximum);

        act.Should()
           .Throw<SalesQueryValidationException>();
    }

    [Fact]
    public void EnsureValidPage_WithPageBeyondAddressableRows_Throws()
    {
        // (page - 1) * pageSize would overflow int and reach Skip as a negative number.
        var act = () => SalesQueryRules.EnsureValidPage(PageBeyondAddressableRows, SalesQueryRules.MaxPageSize);

        act.Should()
           .Throw<SalesQueryValidationException>();
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-3L)]
    public void EnsureValidNumber_WithANonPositiveNumber_Throws(long number)
    {
        var act = () => SalesQueryRules.EnsureValidNumber(number);

        act.Should()
           .Throw<SalesQueryValidationException>();
    }

    [Fact]
    public void ParseDate_WithNull_ReturnsNull()
    {
        SalesQueryRules.ParseDate(null).Should()
                                       .BeNull();
    }

    [Fact]
    public void ParseDate_WithAnIsoDate_ReturnsIt()
    {
        SalesQueryRules.ParseDate("2026-09-27").Should()
                                               .Be(Day);
    }

    [Fact]
    public void EnsureValidRange_WithTheSameDay_DoesNotThrow()
    {
        var act = () => SalesQueryRules.EnsureValidRange(Day, Day);

        act.Should()
           .NotThrow();
    }

    [Fact]
    public void EnsureValidPage_WithPageSizeAtMaximum_DoesNotThrow()
    {
        var act = () => SalesQueryRules.EnsureValidPage(SalesQueryRules.FirstPage, SalesQueryRules.MaxPageSize);

        act.Should()
           .NotThrow();
    }
}
