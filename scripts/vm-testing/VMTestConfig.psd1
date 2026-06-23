@{
    # VM identity (matches plan: vm-installer-testing-plan.md § Hyper-V Quick-Start)
    VMName              = 'IndyPOS-Test'
    # Clean-Windows-Ready = bare Win11 + two harness fixes baked in:
    #   1. vmicvmsession StartupType=Automatic (PowerShell Direct rides on it;
    #      can't be remediated remotely, so it must be in the snapshot)
    #   2. Static DNS 8.8.8.8/1.1.1.1 (Default Switch forwarder is flaky)
    # Parent 'Clean-Windows' retained as the pristine bare-OS fallback.
    CleanSnapshotName   = 'Clean-Windows-Ready'
    SwitchName          = 'Default Switch'

    # VM resource sizing (one-time New-VM block in README)
    MemoryGB            = 4
    DiskGB              = 60
    Processors          = 4

    # Host -> guest staging path. Bootstrapper writes manifest, scripts poll it.
    GuestStagingDir     = 'C:\Test'
    GuestInstallerName  = 'IndyPOS-Setup.exe'
    GuestManifestPath   = 'C:\ProgramData\IndyPOS\v4\install-manifest.json'

    # Credential storage. SecureString XML, DPAPI-encrypted per Windows user.
    # First run prompts for VM admin password and caches here. Path is relative
    # to %LOCALAPPDATA% (Import-PowerShellDataFile bans dynamic expressions).
    CredentialCacheRelativePath = 'IndyPOS\vm-test-cred.xml'

    # Timeouts (seconds unless suffixed)
    VMStartTimeoutSec       = 180
    PSDirectReadyTimeoutSec = 300
    InstallPollIntervalSec  = 15
    InstallTimeoutMinutes   = 30   # Postgres extraction alone is ~9m on host
    HealthProbeTimeoutSec   = 60
    HealthCheckPort         = 5000
}
