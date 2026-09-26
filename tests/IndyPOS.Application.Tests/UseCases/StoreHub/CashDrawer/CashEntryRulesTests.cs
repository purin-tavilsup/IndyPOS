using FluentAssertions;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Domain.Enums;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.CashDrawer;

public class CashEntryRulesTests
{
    private const decimal ThreeDecimalPlaces = 10.005m;
    private const decimal JustAboveMaximum = CashEntryRules.MaxAmount + 0.01m;
    private const PayoutCategory UndefinedCategory = (PayoutCategory)7;

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void EnsureValidAmount_WithZeroOrNegative_Throws(decimal amount)
    {
        var act = () => CashEntryRules.EnsureValidAmount(amount);

        act.Should()
           .Throw<CashEntryValidationException>();
    }

    [Fact]
    public void EnsureValidAmount_WithThreeDecimalPlaces_Throws()
    {
        var act = () => CashEntryRules.EnsureValidAmount(ThreeDecimalPlaces);

        act.Should()
           .Throw<CashEntryValidationException>();
    }

    [Fact]
    public void EnsureValidAmount_AboveMaximum_Throws()
    {
        var act = () => CashEntryRules.EnsureValidAmount(JustAboveMaximum);

        act.Should()
           .Throw<CashEntryValidationException>();
    }

    [Fact]
    public void NormalizeDescription_AboveMaximumLength_Throws()
    {
        var tooLong = new string('ก', CashEntryRules.MaxDescriptionLength + 1);

        var act = () => CashEntryRules.NormalizeDescription(tooLong);

        act.Should()
           .Throw<CashEntryValidationException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeCustomerName_WithMissingOrBlank_Throws(string? customerName)
    {
        var act = () => CashEntryRules.NormalizeCustomerName(customerName);

        act.Should()
           .Throw<CashEntryValidationException>();
    }

    [Fact]
    public void NormalizeCustomerName_WithWhitespaceOnly_Throws()
    {
        var act = () => CashEntryRules.NormalizeCustomerName("\t  \n");

        act.Should()
           .Throw<CashEntryValidationException>();
    }

    [Fact]
    public void NormalizeCustomerName_AboveMaximumLength_Throws()
    {
        var tooLong = new string('ก', CashEntryRules.MaxCustomerNameLength + 1);

        var act = () => CashEntryRules.NormalizeCustomerName(tooLong);

        act.Should()
           .Throw<CashEntryValidationException>();
    }

    [Fact]
    public void EnsureDefined_WithUndefinedValue_Throws()
    {
        var act = () => CashEntryRules.EnsureDefined(UndefinedCategory);

        act.Should()
           .Throw<CashEntryValidationException>();
    }

    [Fact]
    public void EnsureValidCounts_WithOneNegativeCount_Throws()
    {
        var act = () => CashEntryRules.EnsureValidCounts(1, 0, -1);

        act.Should()
           .Throw<CashEntryValidationException>();
    }

    [Fact]
    public void NormalizeDescription_WithWhitespaceOnly_ReturnsNull()
    {
        var result = CashEntryRules.NormalizeDescription("   ");

        result.Should()
              .BeNull();
    }

    [Fact]
    public void NormalizeDescription_WithSurroundingSpaces_ReturnsTrimmed()
    {
        var result = CashEntryRules.NormalizeDescription("  ค่าน้ำแข็ง  ");

        result.Should()
              .Be("ค่าน้ำแข็ง");
    }

    [Fact]
    public void NormalizeCustomerName_WithSurroundingSpaces_ReturnsTrimmed()
    {
        var result = CashEntryRules.NormalizeCustomerName("  ลุงสมชาย ");

        result.Should()
              .Be("ลุงสมชาย");
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(9999999.99)]
    public void EnsureValidAmount_AtTheBoundaries_DoesNotThrow(decimal amount)
    {
        var act = () => CashEntryRules.EnsureValidAmount(amount);

        act.Should()
           .NotThrow();
    }

    [Fact]
    public void EnsureValidCounts_WithAllZero_DoesNotThrow()
    {
        var act = () => CashEntryRules.EnsureValidCounts(0, 0, 0, 0, 0, 0, 0, 0, 0);

        act.Should()
           .NotThrow();
    }
}
