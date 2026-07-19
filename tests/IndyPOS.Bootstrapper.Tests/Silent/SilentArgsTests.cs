using FluentAssertions;
using IndyPOS.Bootstrapper.Silent;
using IndyPOS.Domain.Enums;
using Xunit;

namespace IndyPOS.Bootstrapper.Tests.Silent;

public class SilentArgsTests
{
    public static TheoryData<string[]> ValidSilentArgs => new()
    {
        new[] { "--silent", "--store-id", "ABC" },
        new[] { "--silent", "--store-id=ABC" },
        new[] { "--SILENT", "--Store-Id", "ABC" },
    };

    [Theory]
    [MemberData(nameof(ValidSilentArgs))]
    public void Parse_WithSilentAndStoreId_ShouldReturnSilentWithStoreId(string[] args)
    {
        var result = SilentArgs.Parse(args);

        result.Status.Should().Be(ParseStatus.Silent);
        result.Options!.StoreId.Should().Be("ABC");
        result.Options.TimeoutMinutes.Should().Be(45);
    }

    [Fact]
    public void Parse_WithoutStoreType_ShouldDefaultToGeneralHardware()
    {
        var result = SilentArgs.Parse(new[] { "--silent", "--store-id", "ABC" });

        result.Options!.StoreType.Should().Be(StoreType.GeneralHardware);
    }

    [Theory]
    [InlineData("Minimart", StoreType.Minimart)]
    [InlineData("minimart", StoreType.Minimart)]
    [InlineData("CoffeeShop", StoreType.CoffeeShop)]
    [InlineData("GeneralHardware", StoreType.GeneralHardware)]
    public void Parse_WithStoreType_ShouldParseCaseInsensitively(string value, StoreType expected)
    {
        var result = SilentArgs.Parse(new[] { "--silent", "--store-id", "ABC", "--store-type", value });

        result.Status.Should().Be(ParseStatus.Silent);
        result.Options!.StoreType.Should().Be(expected);
    }

    [Fact]
    public void Parse_WithBogusStoreType_ShouldReturnUsageError()
    {
        var result = SilentArgs.Parse(new[] { "--silent", "--store-id", "ABC", "--store-type", "Bogus" });

        result.Status.Should().Be(ParseStatus.UsageError);
        result.ErrorMessage.Should().Contain("--store-type");
    }

    [Fact]
    public void Parse_WithNumericStoreType_ShouldReturnUsageError()
    {
        // Enum.TryParse<StoreType> also accepts defined underlying numeric values
        // (e.g. "2" -> Minimart). Only a defined enum NAME is a valid token.
        var result = SilentArgs.Parse(new[] { "--silent", "--store-id", "ABC", "--store-type", "2" });

        result.Status.Should().Be(ParseStatus.UsageError);
        result.ErrorMessage.Should().Contain("--store-type");
    }

    [Fact]
    public void Parse_WithStoreIdValue_ShouldPreserveCaseAndTrim()
    {
        var result = SilentArgs.Parse(new[] { "--silent", "--store-id", "  Rungrat-001  " });

        result.Options!.StoreId.Should().Be("Rungrat-001");
    }

    [Fact]
    public void Parse_WithTimeoutOverride_ShouldUseIt()
    {
        var result = SilentArgs.Parse(new[] { "--silent", "--store-id", "A", "--timeout-minutes", "10" });

        result.Options!.TimeoutMinutes.Should().Be(10);
    }

    public static TheoryData<string[]> BadSilentArgs => new()
    {
        new[] { "--silent" },
        new[] { "--silent", "--store-id" },
        new[] { "--silent", "--store-id", " " },
        new[] { "--silent", "--store-id", "A", "--timeout-minutes", "0" },
        new[] { "--silent", "--store-id", "A", "--timeout-minutes", "notanumber" },
        new[] { "--silent", "--store-id", "A", "--bogus" },
        new[] { "--silent", "--store-id", "A", "--store-type", "Bogus" },
        new[] { "--silent", "--store-id", "A", "--store-type", "2" },
    };

    [Theory]
    [MemberData(nameof(BadSilentArgs))]
    public void Parse_WithBadSilentArgs_ShouldReturnUsageError(string[] args)
    {
        SilentArgs.Parse(args).Status.Should().Be(ParseStatus.UsageError);
    }

    public static TheoryData<string[]> NoSilentArgs => new()
    {
        Array.Empty<string>(),
        new[] { "--store-id", "ABC" },
    };

    [Theory]
    [MemberData(nameof(NoSilentArgs))]
    public void Parse_WithoutSilent_ShouldReturnNotSilent(string[] args)
    {
        SilentArgs.Parse(args).Status.Should().Be(ParseStatus.NotSilent);
    }
}
