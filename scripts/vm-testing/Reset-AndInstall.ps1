<#
.SYNOPSIS
    Stage 4 VM smoke-test orchestrator. One command from "clean VM" to
    "verified IndyPOS install".

.DESCRIPTION
    Fully automated VM smoke-test orchestrator. Takes a clean VM from snapshot
    to verified IndyPOS install via headless silent installer:

        1. Restore VM to Clean-Windows snapshot
        2. Start VM, wait for PowerShell Direct readiness
        3. Copy the installer into the guest (C:\Test\IndyPOS-Setup.exe)
        4. Run installer in guest: IndyPOS-Setup.exe --silent --store-id <TestStoreId>
        5. Poll for success markers in C:\ProgramData\IndyPOS\v4\logs\install-latest.log
           (RESULT=success and SERVICE_STARTED != false gate the flow)
        6. Run Test-IndyPOSInstallation.ps1 in the guest
        7. Pretty-print the report; exit non-zero if any check failed

    Prereqs (run once - see scripts/vm-testing/README.md):
        - VM 'IndyPOS-Test' exists with Windows 11 + PSRemoting enabled
        - Checkpoint 'Clean-Windows' captured
        - VM admin credential cached (first run prompts you)

.PARAMETER InstallerPath
    Path to IndyPOS-Setup.exe. Default: <repo>\publish\IndyPOS-Setup.exe.

.PARAMETER SnapshotName
    Snapshot to restore. Default: the clean baseline from VMTestConfig.psd1. Upgrade
    cases need a real store image instead (e.g. 'Pre-Upgrade-2026-07-25').

.PARAMETER InstallerArgs
    Full argument list for the in-guest installer. Default:
    --silent --store-id <TestStoreId>. An upgrade passes just --silent, because the
    store id is adopted from the installed config.

.PARAMETER ExpectRollback
    Invert the gate: require RESULT=failed with ROLLED_BACK=true, SERVICE_STARTED=true
    and HEALTH=ok. Use with --simulate-failure to prove a mid-upgrade failure leaves the
    store serving its previous version. Skips the install verifier, which audits a
    completed install.

.PARAMETER AssertDatabase
    Also run the verifier's payment_method / store-id assertions against the live
    database. Only meaningful on an upgrade of a real store image.

.PARAMETER SkipRestore
    Don't restore the snapshot or start the VM. Use when iterating on a VM
    that's already running with the installer staged.

.PARAMETER KeepRunning
    Don't stop the VM after verification. Default is to leave the VM running
    on success and stop it on success only when this switch is absent.

.PARAMETER RecreateCredential
    Forget the cached SecureString and re-prompt for the VM admin password.

.EXAMPLE
    .\Reset-AndInstall.ps1

.EXAMPLE
    .\Reset-AndInstall.ps1 -InstallerPath '..\..\publish\IndyPOS-Setup.exe' -KeepRunning

.EXAMPLE
    # Iterate without restoring (faster but assumes you already cleaned up):
    .\Reset-AndInstall.ps1 -SkipRestore

.EXAMPLE
    # Upgrade case 1: in-place upgrade of a real store image, with DB assertions.
    .\Reset-AndInstall.ps1 -SnapshotName 'Pre-Upgrade-2026-07-25' `
                           -InstallerArgs '--silent' -AssertDatabase

.EXAMPLE
    # Upgrade case 2: force a mid-upgrade failure and require a verified rollback.
    .\Reset-AndInstall.ps1 -SnapshotName 'Pre-Upgrade-2026-07-25' `
                           -InstallerArgs '--silent','--simulate-failure=deploy' -ExpectRollback
#>

[CmdletBinding()]
param(
    [string]$InstallerPath,
    [string]$SnapshotName,
    [switch]$ExpectRollback,
    [string[]]$InstallerArgs,
    [switch]$AssertDatabase,
    [switch]$SkipRestore,
    [switch]$KeepRunning,
    [switch]$RecreateCredential
)

$ErrorActionPreference = 'Stop'

# --- Setup ------------------------------------------------------------------

$ScriptRoot = $PSScriptRoot
$RepoRoot   = Resolve-Path (Join-Path $ScriptRoot '..\..')
$Config     = Import-PowerShellDataFile (Join-Path $ScriptRoot 'VMTestConfig.psd1')

if (-not $InstallerPath) {
    $InstallerPath = Join-Path $RepoRoot 'publish\IndyPOS-Setup.exe'
}

# Upgrade cases 1/2 run from a real store image; the fresh case from the clean baseline.
if (-not $SnapshotName) { $SnapshotName = $Config.CleanSnapshotName }
$VerifierPath = Join-Path $ScriptRoot 'Test-IndyPOSInstallation.ps1'

function Write-Section($title) {
    Write-Host ''
    Write-Host '========================================' -ForegroundColor Cyan
    Write-Host " $title" -ForegroundColor Cyan
    Write-Host '========================================' -ForegroundColor Cyan
}
function Write-Info($msg)    { Write-Host "[INFO] $msg" -ForegroundColor Gray }
function Write-OK($msg)      { Write-Host "[ OK ] $msg" -ForegroundColor Green }
function Write-Warn2($msg)   { Write-Host "[WARN] $msg" -ForegroundColor Yellow }
function Write-Err($msg)     { Write-Host "[FAIL] $msg" -ForegroundColor Red }

# --- Credential resolution --------------------------------------------------

function Get-VMTestCredential {
    param([switch]$Recreate)

    $cachePath = Join-Path $env:LOCALAPPDATA $Config.CredentialCacheRelativePath
    $cacheDir  = Split-Path $cachePath -Parent

    if ($Recreate -and (Test-Path $cachePath)) {
        Remove-Item $cachePath -Force
    }

    if (Test-Path $cachePath) {
        try {
            return Import-Clixml $cachePath
        } catch {
            Write-Warn2 "Cached credential at $cachePath unreadable ($($_.Exception.Message)). Re-prompting."
        }
    }

    if (-not (Test-Path $cacheDir)) {
        New-Item -ItemType Directory -Path $cacheDir -Force | Out-Null
    }
    $cred = Get-Credential -Message "Enter the VM admin credential for '$($Config.VMName)' (cached DPAPI-encrypted per Windows user)"
    $cred | Export-Clixml $cachePath
    Write-OK "Credential cached at $cachePath"
    return $cred
}

# --- Preflight --------------------------------------------------------------

function Test-Prerequisites {
    Write-Section '1. Preflight'

    if (-not (Test-Path $InstallerPath)) {
        throw "Installer not found: $InstallerPath`nBuild it first: .\scripts\publish.ps1"
    }
    $sizeMB = [math]::Round((Get-Item $InstallerPath).Length / 1MB, 1)
    Write-OK "Installer: $InstallerPath ($sizeMB MB)"

    if (-not (Test-Path $VerifierPath)) {
        throw "In-VM verifier missing: $VerifierPath"
    }
    Write-OK "Verifier:  $VerifierPath"

    $vm = Get-VM -Name $Config.VMName -ErrorAction SilentlyContinue
    if (-not $vm) {
        throw "VM '$($Config.VMName)' not found. See scripts/vm-testing/README.md for one-time setup."
    }
    Write-OK "VM:        $($Config.VMName) (State: $($vm.State))"

    $snap = Get-VMSnapshot -VMName $Config.VMName -Name $SnapshotName -ErrorAction SilentlyContinue
    if (-not $snap) {
        if ($SkipRestore) {
            Write-Warn2 "Snapshot '$($SnapshotName)' missing - proceeding because -SkipRestore is set."
        } else {
            throw "Snapshot '$($SnapshotName)' not found. Capture one after a clean Windows install (see README)."
        }
    } else {
        Write-OK "Snapshot:  $($SnapshotName)"
    }
}

# --- VM lifecycle -----------------------------------------------------------

function Restore-CleanVM {
    Write-Section '2. Restore + Start VM'

    if ($SkipRestore) {
        Write-Info '-SkipRestore set; skipping snapshot restore.'
    } else {
        Write-Info "Restoring snapshot '$($SnapshotName)'..."
        Restore-VMSnapshot -VMName $Config.VMName -Name $SnapshotName -Confirm:$false
        Write-OK 'Snapshot restored.'
    }

    # Guest Service Interface is the one integration service Hyper-V disables by
    # default, and Copy-VMFile (step 4) rides on it. Snapshots don't reliably
    # carry the host-side toggle, so assert it here - idempotent, host-side only.
    if (-not (Get-VMIntegrationService -VMName $Config.VMName -Name 'Guest Service Interface').Enabled) {
        Write-Info "Enabling 'Guest Service Interface' integration service..."
        Enable-VMIntegrationService -VMName $Config.VMName -Name 'Guest Service Interface'
    }

    $vm = Get-VM -Name $Config.VMName
    if ($vm.State -ne 'Running') {
        Write-Info 'Starting VM...'
        Start-VM -Name $Config.VMName
    } else {
        Write-Info 'VM already running.'
    }

    Write-Info "Waiting for VM heartbeat (timeout $($Config.VMStartTimeoutSec)s)..."
    $deadline = (Get-Date).AddSeconds($Config.VMStartTimeoutSec)
    while ((Get-Date) -lt $deadline) {
        $vm = Get-VM -Name $Config.VMName
        if ($vm.Heartbeat -in 'OkApplicationsHealthy','OkApplicationsUnknown') { break }
        Start-Sleep -Seconds 2
    }
    if ($vm.Heartbeat -notin 'OkApplicationsHealthy','OkApplicationsUnknown') {
        Write-Warn2 "Heartbeat not 'Ok' yet ($($vm.Heartbeat)). Continuing - PSDirect probe is authoritative."
    } else {
        Write-OK "Heartbeat: $($vm.Heartbeat)"
    }
}

function Wait-PowerShellDirect {
    param($Credential)

    Write-Section '3. PowerShell Direct readiness'

    $deadline = (Get-Date).AddSeconds($Config.PSDirectReadyTimeoutSec)
    while ((Get-Date) -lt $deadline) {
        try {
            $hostname = Invoke-Command -VMName $Config.VMName -Credential $Credential `
                -ScriptBlock { $env:COMPUTERNAME } -ErrorAction Stop
            Write-OK "PSDirect ready (guest hostname: $hostname)"
            return
        } catch {
            Start-Sleep -Seconds 5
        }
    }
    throw "PowerShell Direct did not become ready within $($Config.PSDirectReadyTimeoutSec)s. Check credentials and that PSRemoting is enabled inside the VM."
}

# --- Wizard handoff ---------------------------------------------------------

function Copy-Installer {
    param($Credential)

    Write-Section '4. Stage installer in guest'

    $guestDir = $Config.GuestStagingDir
    Invoke-Command -VMName $Config.VMName -Credential $Credential -ScriptBlock {
        param($d)
        if (-not (Test-Path $d)) { New-Item -ItemType Directory -Path $d -Force | Out-Null }
    } -ArgumentList $guestDir | Out-Null

    $dest = Join-Path $guestDir $Config.GuestInstallerName
    Write-Info "Copying $InstallerPath -> $dest"
    Copy-VMFile -Name $Config.VMName -SourcePath $InstallerPath -DestinationPath $dest `
                -CreateFullPath -FileSource Host -Force
    Write-OK 'Installer staged in guest.'
}

function Invoke-SilentInstall {
    param($Credential)

    Write-Section '5. Run installer (silent)'

    $guestInstaller = Join-Path $Config.GuestStagingDir $Config.GuestInstallerName
    $logDir         = "C:\ProgramData\IndyPOS\v4\logs"
    $latestLog      = Join-Path $logDir 'install-latest.log'

    $arguments = if ($InstallerArgs) { $InstallerArgs } else { @('--silent', '--store-id', $Config.TestStoreId) }

    # A single element holding "--silent,--simulate-failure=deploy" reaches the installer as
    # ONE unrecognised argument, which silently falls through to the interactive wizard and
    # crashes headless. Caught once for real on 2026-07-29; never diagnose that twice.
    foreach ($a in $arguments) {
        if ($a -match '[,\s]') {
            throw ("-InstallerArgs element '$a' contains a comma or whitespace, so it would " +
                   "reach the installer as one malformed argument. Pass each flag as its own " +
                   "element: -InstallerArgs '--silent','--simulate-failure=deploy'")
        }
    }

    Write-Info "Running (in guest): $guestInstaller $($arguments -join ' ')"

    # Log names carry a timestamp and pid, so anything not in this set was written by THIS
    # run. Required because a crash before the first log write leaves the snapshot's own
    # 2026-07-19 install log as the newest one - which reads as RESULT=success.
    $preExistingLogs = @(Invoke-Command -VMName $Config.VMName -Credential $Credential -ScriptBlock {
        param($dir)
        Get-ChildItem -Path $dir -Filter 'install-2*.log' -ErrorAction SilentlyContinue |
            Select-Object -ExpandProperty Name
    } -ArgumentList $logDir)

    $timeoutSec = $Config.InstallTimeoutMinutes * 60
    $job = Invoke-Command -VMName $Config.VMName -Credential $Credential -AsJob -ScriptBlock {
        param($exe, $argList)
        $p = Start-Process -FilePath $exe -ArgumentList $argList -Wait -PassThru
        [pscustomobject]@{ ExitCode = $p.ExitCode }
    } -ArgumentList $guestInstaller, $arguments

    if (-not (Wait-Job -Job $job -Timeout $timeoutSec)) {
        Stop-Job -Job $job
        Remove-Job -Job $job -Force
        throw "Silent install exceeded the $($Config.InstallTimeoutMinutes)-min harness watchdog (in-guest install hung). See $latestLog in the guest."
    }

    $run = Receive-Job -Job $job
    Remove-Job -Job $job

    Write-Info "Installer exit code: $($run.ExitCode)"

    # THIS run's log only - never install-latest.log (can hold a previous RESULT=success) and
    # never merely the newest (a snapshot ships with its own install log).
    $fresh = Invoke-Command -VMName $Config.VMName -Credential $Credential -ScriptBlock {
        param($dir, $before)
        $log = Get-ChildItem -Path $dir -Filter 'install-2*.log' -ErrorAction SilentlyContinue |
               Where-Object { $_.Name -notin $before } |
               Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if (-not $log) { return $null }
        [pscustomobject]@{
            Name    = $log.Name
            Markers = @(Get-Content $log.FullName | Where-Object { $_ -like 'INDYPOS_MARKER *' })
        }
    } -ArgumentList $logDir, $preExistingLogs

    if (-not $fresh) {
        throw ("The installer wrote no log for this run (exit=$($run.ExitCode)), so it died " +
               "before logging anything - a malformed argument list falling through to the " +
               "wizard does exactly this. Refusing to grade the run against a stale log.")
    }

    Write-Info "Log: $($fresh.Name)"
    $markers = $fresh.Markers

    # Parse the LAST occurrence of each marker key (D9).
    $marker = @{}
    foreach ($line in $markers) {
        if ($line -match '^INDYPOS_MARKER\s+([A-Z_]+)=(.*)$') { $marker[$Matches[1]] = $Matches[2] }
    }
    foreach ($k in $marker.Keys) { Write-Info "marker $k=$($marker[$k])" }

    if ($ExpectRollback) {
        # A deliberate mid-upgrade failure. Success here means the store came BACK, which
        # is the only claim the rollback design actually makes.
        $ok = ($marker['RESULT'] -eq 'failed') -and
              ($marker['ROLLED_BACK'] -eq 'true') -and
              ($marker['SERVICE_STARTED'] -eq 'true') -and
              ($marker['HEALTH'] -eq 'ok')
        if (-not $ok) {
            throw ("Expected a verified rollback but got RESULT=$($marker['RESULT']), " +
                   "ROLLED_BACK=$($marker['ROLLED_BACK']), SERVICE_STARTED=$($marker['SERVICE_STARTED']), " +
                   "HEALTH=$($marker['HEALTH']). See $latestLog in the guest.")
        }
        Write-OK 'Rollback verified: store is serving its previous version.'
        return
    }

    # Authoritative gating rule: exit code + RESULT + service.
    $failed = ($run.ExitCode -ne 0) -or ($marker['RESULT'] -ne 'success') -or ($marker['SERVICE_STARTED'] -eq 'false')
    if ($failed) {
        throw "Silent install failed (exit=$($run.ExitCode), RESULT=$($marker['RESULT']), SERVICE_STARTED=$($marker['SERVICE_STARTED'])). See $latestLog in the guest."
    }
    if ($marker['CRED_LOCKED'] -eq 'false') {
        Write-Warn2 "Credential file could not be ACL-locked (CRED_LOCKED=false)."
    }
    Write-OK "Silent install completed (RESULT=$($marker['RESULT']), MODE=$($marker['MODE']))."
}

# --- Verification + report --------------------------------------------------

function Invoke-Verifier {
    param($Credential)

    Write-Section '6. In-VM verification'

    Write-Info 'Running Test-IndyPOSInstallation.ps1 inside guest...'

    # -FilePath binds positionally, so the verifier declares -AssertDatabase at Position 0.
    $result = Invoke-Command -VMName $Config.VMName -Credential $Credential `
                             -FilePath $VerifierPath -ArgumentList $AssertDatabase.IsPresent
    return $result
}

function Format-Report {
    param($Result)

    Write-Section '7. Results'

    $byCategory = $Result.Checks | Group-Object Category
    foreach ($g in $byCategory) {
        Write-Host ''
        Write-Host "[$($g.Name)]" -ForegroundColor Cyan
        foreach ($c in $g.Group) {
            if     ($c.Skip) { $tag = '[SKIP]'; $col = 'Yellow' }
            elseif ($c.Pass) { $tag = '[PASS]'; $col = 'Green' }
            else             { $tag = '[FAIL]'; $col = 'Red' }
            Write-Host ("  {0,-6} {1}" -f $tag, $c.Name) -ForegroundColor $col
            if ($c.Detail) {
                Write-Host "         $($c.Detail)" -ForegroundColor DarkGray
            }
        }
    }

    Write-Host ''
    Write-Host '========================================' -ForegroundColor Cyan
    $summary = "Pass: $($Result.Summary.Pass)  Fail: $($Result.Summary.Fail)  Skip: $($Result.Summary.Skip)"
    if ($Result.OverallPass) {
        Write-Host " RESULT: PASS  $summary" -ForegroundColor Green
    } else {
        Write-Host " RESULT: FAIL  $summary" -ForegroundColor Red
    }
    Write-Host '========================================' -ForegroundColor Cyan
}

# --- Main -------------------------------------------------------------------

$start = Get-Date
Write-Host ''
Write-Host '=== IndyPOS Stage 4 - VM smoke-test ===' -ForegroundColor Magenta

try {
    Test-Prerequisites
    $cred = Get-VMTestCredential -Recreate:$RecreateCredential
    Restore-CleanVM
    Wait-PowerShellDirect -Credential $cred
    Copy-Installer -Credential $cred
    Invoke-SilentInstall -Credential $cred

    if ($ExpectRollback) {
        # The verifier audits a COMPLETED install; a rolled-back store is not one.
        Write-OK 'Expect-rollback run complete; skipping the install verifier.'
        exit 0
    }

    $result = Invoke-Verifier -Credential $cred
    Format-Report -Result $result

    $elapsed = (Get-Date) - $start
    Write-Host ("Total elapsed: {0:hh\:mm\:ss}" -f $elapsed) -ForegroundColor Gray

    if (-not $KeepRunning) {
        Write-Info "VM left running (default). Pass -KeepRunning to silence this hint, or stop manually with: Stop-VM '$($Config.VMName)' -Force"
    }

    if (-not $result.OverallPass) { exit 1 }
}
catch {
    Write-Err $_.Exception.Message
    exit 2
}
