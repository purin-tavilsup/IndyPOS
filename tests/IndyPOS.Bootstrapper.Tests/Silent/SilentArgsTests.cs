using FluentAssertions;
using IndyPOS.Bootstrapper.Silent;
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
