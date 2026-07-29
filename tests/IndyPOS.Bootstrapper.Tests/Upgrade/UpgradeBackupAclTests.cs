using FluentAssertions;
using IndyPOS.Bootstrapper.Upgrade;

namespace IndyPOS.Bootstrapper.Tests.Upgrade;

/// <summary>
/// Exercises the REAL ACL helper, unlike <see cref="UpgradeBackupTests"/> which fakes the
/// lock seam. That fake is what let the 2026-07-29 VM failure through: locking the stamp
/// directory with a protected, non-inheritable DACL retroactively strips its children's
/// inherited ACEs, so the freshly copied tree became readable by nobody - and the very next
/// statement enumerates that tree.
/// </summary>
public class UpgradeBackupAclTests : IDisposable
{
    private readonly string _root =
        Path.Combine(Path.GetTempPath(), "indypos-backup-acl-" + Guid.NewGuid().ToString("N"));

    private readonly string _backups;
    private readonly string _storeHub;

    public UpgradeBackupAclTests()
    {
        _backups = Path.Combine(_root, "backups");
        _storeHub = Path.Combine(_root, "StoreHub");
        Directory.CreateDirectory(_backups);
        Directory.CreateDirectory(Path.Combine(_storeHub, "runtimes", "win-x64"));
        File.WriteAllText(Path.Combine(_storeHub, "appsettings.json"), "{}");
        File.WriteAllText(Path.Combine(_storeHub, "IndyPOS.StoreHub.exe"), "old binary");
        File.WriteAllText(Path.Combine(_storeHub, "runtimes", "win-x64", "native.dll"), "native");
    }

    public void Dispose()
    {
        try
        {
            // The stamp is deliberately ACL-locked; re-grant before cleanup or this leaks temp dirs.
            foreach (var dir in Directory.EnumerateDirectories(_backups))
            {
                GrantFullControl(dir);
            }
        }
        catch { /* best effort */ }

        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }

    private static void GrantFullControl(string path)
    {
        var info = new DirectoryInfo(path);
        var security = info.GetAccessControl();
        security.SetAccessRuleProtection(isProtected: false, preserveInheritance: true);
        info.SetAccessControl(security);
    }

    private sealed class FakePgDump : IProcessRunner
    {
        public Task<ProcessRunResult> RunAsync(
            string fileName, string arguments,
            IReadOnlyDictionary<string, string>? environment, CancellationToken ct)
        {
            const string marker = "-f \"";
            var start = arguments.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
            File.WriteAllBytes(arguments[start..arguments.IndexOf('"', start)], new byte[4096]);

            return Task.FromResult(new ProcessRunResult(0, "", ""));
        }
    }

    [Fact]
    public async Task CreateAsync_WithRealAcls_ShouldLeaveTheBackedUpTreeReadable()
    {
        // A backup nobody can read is not a backup: RestoreStoreHubTree enumerates this
        // exact directory, so an unreadable tree also breaks every rollback.
        var backup = new UpgradeBackup(_backups, _storeHub, new FakePgDump(), () => "20260729-010203");

        var result = await backup.CreateAsync(
            "pg_dump.exe",
            "Host=127.0.0.1;Port=5432;Database=indypos_storehub;Username=indypos_app;Password=s3cret",
            CancellationToken.None);

        result.Success.Should().BeTrue(result.ErrorMessage);

        // Guard against a vacuous pass: if the real ACL helper silently no-opped, the
        // readability assertion below would prove nothing.
        result.Locked.Should().BeTrue("the real ACL helper must actually apply the lock");
        new DirectoryInfo(result.StampDirectory!).GetAccessControl()
            .AreAccessRulesProtected.Should().BeTrue("the stamp directory must drop inherited ACEs");

        var tree = Path.Combine(result.StampDirectory!, "StoreHub");
        var act = () => Directory.GetFiles(tree, "*", SearchOption.AllDirectories);

        act.Should().NotThrow<UnauthorizedAccessException>();
        act().Should().HaveCount(3);
    }

    [Fact]
    public async Task CreateAsync_ShouldNotWalkTheTreeItJustLocked()
    {
        // The 2026-07-29 failure was the walk itself, so assert it does not happen. A lock
        // seam that throws on any path under the tree would have failed the old code and
        // passes now: inheritance covers those files, nothing enumerates them.
        var treeFilesTouched = new List<string>();

        var backup = new UpgradeBackup(
            _backups, _storeHub, new FakePgDump(), () => "20260729-020304",
            lockPath: p => { treeFilesTouched.Add(p); return true; });

        var result = await backup.CreateAsync(
            "pg_dump.exe",
            "Host=127.0.0.1;Port=5432;Database=indypos_storehub;Username=indypos_app;Password=s3cret",
            CancellationToken.None);

        result.Success.Should().BeTrue(result.ErrorMessage);
        treeFilesTouched.Should().ContainSingle().Which.Should().EndWith("storehub.dump");
    }
}
