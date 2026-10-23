using FluentAssertions;
using IndyPOS.MigrationTool.Services;

namespace IndyPOS.MigrationTool.Tests;

/// <summary>
/// Pure unit tests. A --store-id that is not the installed StoreHub's made every migrated debt event a
/// 403 at the cloud, which used to stall the store's sync for good. The migrator now refuses to start
/// unless it matches StoreHub's explicitly configured Store:Id; the machine-name fallback does not count.
/// </summary>
public sealed class StoreHubStoreIdCheckTests : IDisposable
{
    private const string InstalledStoreId = "STORE-001";

    private readonly string _directory = Directory.CreateTempSubdirectory("indypos-storeid-check-").FullName;

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private string ConfigWith(string json)
    {
        var path = Path.Combine(_directory, "appsettings.json");
        File.WriteAllText(path, json);
        return path;
    }

    private string ConfigWithStoreId(string storeId) =>
        ConfigWith($$$"""{"Store":{"Id":"{{{storeId}}}","Type":"GeneralHardware"}}""");

    [Fact]
    public void Check_WhenTheIdsMatch_ReturnsAMatch()
    {
        var result = StoreHubStoreIdCheck.Check(InstalledStoreId, ConfigWithStoreId(InstalledStoreId));

        result.IsMatch.Should().BeTrue();
    }

    [Fact]
    public void Check_WhenTheIdsDiffer_ReturnsAMismatchNamingBoth()
    {
        var result = StoreHubStoreIdCheck.Check("STORE-002", ConfigWithStoreId(InstalledStoreId));

        result.IsMatch.Should().BeFalse();
        result.Message.Should().Contain("STORE-002");
        result.Message.Should().Contain(InstalledStoreId);
    }

    // The cloud compares store ids ordinally, so a case difference is a different store.
    [Fact]
    public void Check_WhenTheIdsDifferOnlyInCase_ReturnsAMismatch()
    {
        var result = StoreHubStoreIdCheck.Check("store-001", ConfigWithStoreId(InstalledStoreId));

        result.IsMatch.Should().BeFalse();
    }

    [Fact]
    public void Check_WhenTheConfigFileIsMissing_ReturnsAMismatchNamingTheOption()
    {
        var result = StoreHubStoreIdCheck.Check(InstalledStoreId, Path.Combine(_directory, "missing.json"));

        result.IsMatch.Should().BeFalse();
        result.Message.Should().Contain("--storehub-config");
    }

    [Fact]
    public void Check_WhenStoreIdIsNotSet_ReturnsAMismatch()
    {
        var result = StoreHubStoreIdCheck.Check(InstalledStoreId, ConfigWith("""{"Store":{"Type":"Minimart"}}"""));

        result.IsMatch.Should().BeFalse();
        result.Message.Should().Contain("Store:Id");
    }

    [Fact]
    public void Check_WhenStoreIdIsBlank_ReturnsAMismatch()
    {
        var result = StoreHubStoreIdCheck.Check(InstalledStoreId, ConfigWithStoreId("   "));

        result.IsMatch.Should().BeFalse();
    }

    // The installer locks StoreHub's appsettings.json to Administrators and SYSTEM, so a non-elevated run
    // can see the file but not read it. That must refuse with a message, not crash with a stack trace.
    [Fact]
    public void Check_WhenTheConfigCannotBeRead_ReturnsAMismatchSayingSo()
    {
        var path = ConfigWithStoreId(InstalledStoreId);
        using var lockedByAnotherProcess = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        var result = StoreHubStoreIdCheck.Check(InstalledStoreId, path);

        result.IsMatch.Should().BeFalse();
        result.Message.Should().Contain("could not be read");
    }

    [Fact]
    public void Check_WhenTheConfigIsNotJson_ReturnsAMismatch()
    {
        var result = StoreHubStoreIdCheck.Check(InstalledStoreId, ConfigWith("not json"));

        result.IsMatch.Should().BeFalse();
    }
}
