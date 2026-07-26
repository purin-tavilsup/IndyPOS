using System.Security.Cryptography;
using FluentAssertions;
using IndyPOS.Bootstrapper.Upgrade;
using IndyPOS.Domain.Enums;

namespace IndyPOS.Bootstrapper.Tests.Upgrade;

public class StoreHubConfigReaderTests : IDisposable
{
    private readonly string _dir =
        Path.Combine(Path.GetTempPath(), "indypos-cfgread-" + Guid.NewGuid().ToString("N"));

    public StoreHubConfigReaderTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    // Tests never touch real DPAPI: a machine-scoped blob cannot be authored
    // portably, and the reader's contract is "whatever unprotect does".
    private static string PassThrough(string key, string value) => value;

    private static string Fail(string key, string value) =>
        throw new CryptographicException("Key not valid for use in specified state.");

    private string Write(string json)
    {
        var path = Path.Combine(_dir, "appsettings.json");
        File.WriteAllText(path, json);
        return path;
    }

    [Fact]
    public void Read_WhenFileIsAbsent_ShouldReportNotExists()
    {
        var facts = StoreHubConfigReader.Read(Path.Combine(_dir, "appsettings.json"), PassThrough);

        facts.Exists.Should().BeFalse();
        facts.ConnectionStringUsable.Should().BeFalse();
        facts.StoreId.Should().BeNull();
        facts.StoreType.Should().BeNull();
    }

    [Fact]
    public void Read_WithARealStoreConfig_ShouldReturnEveryFact()
    {
        var path = Write("""
        {
          "connectionStrings": { "storehub-db": "DPAPI:AQAAANCM" },
          "store": { "id": "Rungrat-001", "type": "Minimart" }
        }
        """);

        var facts = StoreHubConfigReader.Read(path, PassThrough);

        facts.Exists.Should().BeTrue();
        facts.ConnectionStringUsable.Should().BeTrue();
        facts.StoreId.Should().Be("Rungrat-001");
        facts.StoreType.Should().Be(StoreType.Minimart);
    }

    [Fact]
    public void Read_WithPascalCaseKeys_ShouldStillBindThem()
    {
        // ASP.NET config binding is case-insensitive, so a hand-edited file is valid.
        var path = Write("""
        {
          "ConnectionStrings": { "storehub-db": "Host=127.0.0.1" },
          "Store": { "Id": "Rungrat-001", "Type": "GeneralHardware" }
        }
        """);

        var facts = StoreHubConfigReader.Read(path, PassThrough);

        facts.StoreId.Should().Be("Rungrat-001");
        facts.StoreType.Should().Be(StoreType.GeneralHardware);
    }

    [Fact]
    public void Read_WhenDpapiValueFailsToDecrypt_ShouldReportUnusable()
    {
        // A value protected on another machine. That store is Unusable, not upgradeable.
        var path = Write("""
        {
          "connectionStrings": { "storehub-db": "DPAPI:AQAAANCM" },
          "store": { "id": "Rungrat-001", "type": "Minimart" }
        }
        """);

        var facts = StoreHubConfigReader.Read(path, Fail);

        facts.Exists.Should().BeTrue();
        facts.ConnectionStringUsable.Should().BeFalse();
    }

    [Fact]
    public void Read_WhenDpapiValueIsNotBase64_ShouldReportUnusable()
    {
        // Unprotect throws FormatException, not CryptographicException, on bad base64.
        var path = Write("""{ "connectionStrings": { "storehub-db": "DPAPI:not-base64!!" } }""");

        var facts = StoreHubConfigReader.Read(
            path, (_, _) => throw new FormatException("Invalid base64."));

        facts.ConnectionStringUsable.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Read_WithBlankConnectionString_ShouldReportUnusable(string value)
    {
        var path = Write($$"""{ "connectionStrings": { "storehub-db": "{{value}}" } }""");

        StoreHubConfigReader.Read(path, PassThrough).ConnectionStringUsable.Should().BeFalse();
    }

    [Fact]
    public void Read_WithBlankStoreId_ShouldReturnNullStoreId()
    {
        // Empty is as dangerous as absent: StoreIdentityService falls back to the
        // machine name, which would orphan the store's sales history.
        var path = Write("""{ "store": { "id": "   ", "type": "Minimart" } }""");

        StoreHubConfigReader.Read(path, PassThrough).StoreId.Should().BeNull();
    }

    [Fact]
    public void Read_WithAbsentStoreType_ShouldReturnNullStoreType()
    {
        // Stores installed before 2026-07-18 have no Store:Type key.
        var path = Write("""{ "store": { "id": "Rungrat-001" } }""");

        StoreHubConfigReader.Read(path, PassThrough).StoreType.Should().BeNull();
    }

    [Fact]
    public void Read_WithUnparseableStoreType_ShouldReturnNullStoreType()
    {
        var path = Write("""{ "store": { "id": "Rungrat-001", "type": "Bakery" } }""");

        StoreHubConfigReader.Read(path, PassThrough).StoreType.Should().BeNull();
    }

    [Fact]
    public void Read_WithMalformedJson_ShouldReportExistsButUnusable()
    {
        var path = Write("{ this is not json");

        var facts = StoreHubConfigReader.Read(path, PassThrough);

        facts.Exists.Should().BeTrue();
        facts.ConnectionStringUsable.Should().BeFalse();
        facts.StoreId.Should().BeNull();
    }

    [Fact]
    public void Read_WithNonStringStoreId_ShouldDegradeGracefully()
    {
        // Hand-edited config with numeric store ID instead of string.
        var path = Write("""{ "store": { "id": 12345, "type": "Minimart" } }""");

        var facts = StoreHubConfigReader.Read(path, PassThrough);

        facts.Exists.Should().BeTrue();
        facts.StoreId.Should().BeNull();
        facts.StoreType.Should().Be(StoreType.Minimart);
    }

    [Fact]
    public void Read_WithNonStringStoreType_ShouldDegradeGracefully()
    {
        // Hand-edited config with numeric store type instead of string enum name.
        var path = Write("""{ "store": { "id": "Rungrat-001", "type": 2 } }""");

        var facts = StoreHubConfigReader.Read(path, PassThrough);

        facts.Exists.Should().BeTrue();
        facts.StoreId.Should().Be("Rungrat-001");
        facts.StoreType.Should().BeNull();
    }
}
