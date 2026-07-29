using FluentAssertions;
using IndyPOS.Bootstrapper.Upgrade;

namespace IndyPOS.Bootstrapper.Tests.Upgrade;

public class UpgradeBackupTests : IDisposable
{
    private readonly string _root =
        Path.Combine(Path.GetTempPath(), "indypos-backup-" + Guid.NewGuid().ToString("N"));

    private readonly string _backups;
    private readonly string _storeHub;
    private readonly List<string> _lockedPaths = [];

    public UpgradeBackupTests()
    {
        _backups = Path.Combine(_root, "backups");
        _storeHub = Path.Combine(_root, "StoreHub");
        Directory.CreateDirectory(_backups);
        Directory.CreateDirectory(_storeHub);
        File.WriteAllText(Path.Combine(_storeHub, "IndyPOS.StoreHub.exe"), "old binary");
        File.WriteAllText(Path.Combine(_storeHub, "appsettings.json"), """{"store":{"id":"Rungrat-001"}}""");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    // Writes a dump of the requested size instead of shelling out to pg_dump.
    private sealed class FakePgDump(int exitCode, int dumpBytes) : IProcessRunner
    {
        public string? LastArguments { get; private set; }

        public Task<ProcessRunResult> RunAsync(
            string fileName, string arguments,
            IReadOnlyDictionary<string, string>? environment, CancellationToken ct)
        {
            LastArguments = arguments;

            // -f <path> is the last argument; mirror what pg_dump would produce.
            var marker = "-f \"";
            var start = arguments.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
            var path = arguments[start..arguments.IndexOf('"', start)];

            if (dumpBytes > 0)
            {
                File.WriteAllBytes(path, new byte[dumpBytes]);
            }

            return Task.FromResult(new ProcessRunResult(exitCode, "", exitCode == 0 ? "" : "boom"));
        }
    }

    // Both lock seams record into the same list; which helper ran is asserted separately by
    // UpgradeBackupAclTests, which uses the real ACL code.
    private UpgradeBackup Build(IProcessRunner runner, string stamp = "20260726-010203") =>
        new(_backups, _storeHub, runner, () => stamp,
            p => { _lockedPaths.Add(p); return true; },
            p => { _lockedPaths.Add(p); return true; });

    private const string Conn = "Host=127.0.0.1;Port=5432;Database=indypos_storehub;Username=indypos_app;Password=s3cret";

    [Fact]
    public async Task CreateAsync_OnSuccess_ShouldCopyTheStoreHubTreeIntoTheStamp()
    {
        var result = await Build(new FakePgDump(0, 4096)).CreateAsync("pg_dump.exe", Conn, CancellationToken.None);

        result.Success.Should().BeTrue();
        File.ReadAllText(Path.Combine(result.StampDirectory!, "StoreHub", "IndyPOS.StoreHub.exe"))
            .Should().Be("old binary");
    }

    [Fact]
    public async Task CreateAsync_OnSuccess_ShouldWriteTheDumpBesideTheTree()
    {
        var result = await Build(new FakePgDump(0, 4096)).CreateAsync("pg_dump.exe", Conn, CancellationToken.None);

        File.Exists(Path.Combine(result.StampDirectory!, "storehub.dump")).Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_OnSuccess_ShouldLockTheStampAndBothArtifacts()
    {
        // BackupsDirectory inherits ProgramData's DACL, which grants BUILTIN\Users read.
        // The dump is the whole sales history plus BCrypt admin hashes.
        var result = await Build(new FakePgDump(0, 4096)).CreateAsync("pg_dump.exe", Conn, CancellationToken.None);

        result.Locked.Should().BeTrue();
        _lockedPaths.Should().Contain(result.StampDirectory!);
        _lockedPaths.Should().Contain(Path.Combine(result.StampDirectory!, "storehub.dump"));
        _lockedPaths.Should().Contain(Path.Combine(result.StampDirectory!, "StoreHub"));

        // Individual files inside the tree are covered by the stamp directory's inheritable
        // ACEs. Walking them was not merely wasteful (483 files on a real store) - the walk
        // itself was the 2026-07-29 failure, see UpgradeBackupAclTests.
        _lockedPaths.Should().NotContain(Path.Combine(result.StampDirectory!, "StoreHub", "appsettings.json"));
    }

    [Fact]
    public async Task CreateAsync_WhenPgDumpFails_ShouldReportFailure()
    {
        var result = await Build(new FakePgDump(1, 4096)).CreateAsync("pg_dump.exe", Conn, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("pg_dump");
    }

    [Fact]
    public async Task CreateAsync_WhenTheDumpIsTruncated_ShouldRejectIt()
    {
        // A disk-full pg_dump can exit 0 having written almost nothing; "present" is not
        // "restorable", and BACKUP_DIR would otherwise advertise a dead artifact.
        var result = await Build(new FakePgDump(0, 10)).CreateAsync("pg_dump.exe", Conn, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("too small");
    }

    [Fact]
    public async Task CreateAsync_ShouldNeverPutThePasswordOnTheCommandLine()
    {
        // A command line is visible to every local process.
        var runner = new FakePgDump(0, 4096);
        await Build(runner).CreateAsync("pg_dump.exe", Conn, CancellationToken.None);

        runner.LastArguments.Should().NotContain("s3cret");
    }

    [Fact]
    public async Task CreateAsync_ShouldUseCustomFormatSoPgRestoreCanReadIt()
    {
        var runner = new FakePgDump(0, 4096);
        await Build(runner).CreateAsync("pg_dump.exe", Conn, CancellationToken.None);

        runner.LastArguments.Should().Contain("-Fc");
    }

    [Fact]
    public void RestoreStoreHubTree_ShouldDeleteBeforeCopying()
    {
        // Extraction is per-entry overwrite with no clean step, so after a deploy the tree
        // is old-union-new. Copying over that leaves new-version files behind.
        var stamp = Path.Combine(_backups, "20260726-010203");
        Directory.CreateDirectory(Path.Combine(stamp, "StoreHub"));
        File.WriteAllText(Path.Combine(stamp, "StoreHub", "IndyPOS.StoreHub.exe"), "old binary");

        File.WriteAllText(Path.Combine(_storeHub, "BrandNew.dll"), "from the failed upgrade");

        Build(new FakePgDump(0, 4096)).RestoreStoreHubTree(stamp);

        File.Exists(Path.Combine(_storeHub, "BrandNew.dll")).Should().BeFalse();
        File.ReadAllText(Path.Combine(_storeHub, "IndyPOS.StoreHub.exe")).Should().Be("old binary");
    }

    [Fact]
    public void RestoreStoreHubTree_WhenTheBackupIsMissing_ShouldThrow()
    {
        // Silently doing nothing here would report a rollback that never happened.
        var act = () => Build(new FakePgDump(0, 4096))
            .RestoreStoreHubTree(Path.Combine(_backups, "no-such-stamp"));

        act.Should().Throw<DirectoryNotFoundException>();
    }

    [Fact]
    public void Prune_WithMoreThanTheRetainedStamps_ShouldKeepOnlyTheNewest()
    {
        foreach (var stamp in new[] { "20260101-000000", "20260201-000000", "20260301-000000", "20260401-000000" })
        {
            Directory.CreateDirectory(Path.Combine(_backups, stamp));
        }

        var removed = Build(new FakePgDump(0, 4096)).Prune();

        removed.Should().Be(2);
        Directory.GetDirectories(_backups).Select(Path.GetFileName)
            .Should().BeEquivalentTo("20260301-000000", "20260401-000000");
    }

    [Fact]
    public void Prune_WithFewerThanTheRetainedStamps_ShouldRemoveNothing()
    {
        Directory.CreateDirectory(Path.Combine(_backups, "20260101-000000"));

        Build(new FakePgDump(0, 4096)).Prune().Should().Be(0);
    }
}
