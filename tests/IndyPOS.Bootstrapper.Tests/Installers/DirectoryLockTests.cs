using FluentAssertions;
using IndyPOS.Bootstrapper.Installers;

namespace IndyPOS.Bootstrapper.Tests.Installers;

/// <summary>
/// The inheritance semantics of <see cref="DatabaseSetup.TryRestrictDirectoryPermissions"/>.
/// A directory locked without inheritable ACEs makes everything inside it unreachable, which
/// is how the 2026-07-29 upgrade destroyed access to a backup it had just written correctly.
/// </summary>
public class DirectoryLockTests : IDisposable
{
    private readonly string _dir =
        Path.Combine(Path.GetTempPath(), "indypos-dirlock-" + Guid.NewGuid().ToString("N"));

    public DirectoryLockTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try
        {
            var info = new DirectoryInfo(_dir);
            var security = info.GetAccessControl();
            security.SetAccessRuleProtection(isProtected: false, preserveInheritance: true);
            info.SetAccessControl(security);
        }
        catch { /* best effort */ }

        try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public void TryRestrictDirectoryPermissions_ShouldProtectTheDirectory()
    {
        DatabaseSetup.TryRestrictDirectoryPermissions(_dir).Should().BeTrue();

        new DirectoryInfo(_dir).GetAccessControl().AreAccessRulesProtected.Should().BeTrue();
    }

    [Fact]
    public void TryRestrictDirectoryPermissions_ShouldMarkEveryAceInheritable()
    {
        // This single flag is the whole fix. Without it, protecting the directory drops the
        // inherited ACEs and Windows recomputes its children's as nothing at all.
        DatabaseSetup.TryRestrictDirectoryPermissions(_dir);

        var rules = new DirectoryInfo(_dir).GetAccessControl()
            .GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount))
            .Cast<System.Security.AccessControl.FileSystemAccessRule>()
            .ToList();

        rules.Should().NotBeEmpty();
        rules.Should().OnlyContain(r =>
            r.InheritanceFlags.HasFlag(System.Security.AccessControl.InheritanceFlags.ContainerInherit) &&
            r.InheritanceFlags.HasFlag(System.Security.AccessControl.InheritanceFlags.ObjectInherit));
    }

    [Fact]
    public void TryRestrictDirectoryPermissions_ShouldLeaveContentCreatedAfterwardsReachable()
    {
        // The operation that actually failed in production: write into the locked directory,
        // then walk it.
        DatabaseSetup.TryRestrictDirectoryPermissions(_dir);

        var child = Path.Combine(_dir, "StoreHub", "runtimes");
        Directory.CreateDirectory(child);
        File.WriteAllText(Path.Combine(child, "native.dll"), "native");

        var act = () => Directory.GetFiles(_dir, "*", SearchOption.AllDirectories);

        act.Should().NotThrow<UnauthorizedAccessException>();
        act().Should().ContainSingle();
    }
}
