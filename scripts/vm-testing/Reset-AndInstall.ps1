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

    Prereqs (run once — see scripts/vm-testing/README.md):
        - VM 'IndyPOS-Test' exists with Windows 11 + PSRemoting enabled
        - Checkpoint 'Clean-Windows' captured
        - VM admin credential cached (first run prompts you)

.PARAMETER InstallerPath
    Path to IndyPOS-Setup.exe. Default: <repo>\publish\IndyPOS-Setup.exe.

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
#>

[CmdletBinding()]
param(
    [string]$InstallerPath,
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

    $snap = Get-VMSnapshot -VMName $Config.VMName -Name $Config.CleanSnapshotName -ErrorAction SilentlyContinue
    if (-not $snap) {
        if ($SkipRestore) {
            Write-Warn2 "Snapshot '$($Config.CleanSnapshotName)' missing — proceeding because -SkipRestore is set."
        } else {
            throw "Snapshot '$($Config.CleanSnapshotName)' not found. Capture one after a clean Windows install (see README)."
        }
    } else {
        Write-OK "Snapshot:  $($Config.CleanSnapshotName)"
    }
}

# --- VM lifecycle -----------------------------------------------------------

function Restore-CleanVM {
    Write-Section '2. Restore + Start VM'

    if ($SkipRestore) {
        Write-Info '-SkipRestore set; skipping snapshot restore.'
    } else {
        Write-Info "Restoring snapshot '$($Config.CleanSnapshotName)'..."
        Restore-VMSnapshot -VMName $Config.VMName -Name $Config.CleanSnapshotName -Confirm:$false
        Write-OK 'Snapshot restored.'
    }

    # Guest Service Interface is the one integration service Hyper-V disables by
    # default, and Copy-VMFile (step 4) rides on it. Snapshots don't reliably
    # carry the host-side toggle, so assert it here — idempotent, host-side only.
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
        Write-Warn2 "Heartbeat not 'Ok' yet ($($vm.Heartbeat)). Continuing — PSDirect probe is authoritative."
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
    $storeId        = $Config.TestStoreId
    $logDir         = "C:\ProgramData\IndyPOS\v4\logs"
    $latestLog      = Join-Path $logDir 'install-latest.log'

    Write-Info "Running (in guest): $guestInstaller --silent --store-id $storeId"

    $timeoutSec = $Config.InstallTimeoutMinutes * 60
    $job = Invoke-Command -VMName $Config.VMName -Credential $Credential -AsJob -ScriptBlock {
        param($exe, $id)
        $p = Start-Process -FilePath $exe -ArgumentList '--silent', '--store-id', $id -Wait -PassThru
        [pscustomobject]@{ ExitCode = $p.ExitCode }
    } -ArgumentList $guestInstaller, $storeId

    if (-not (Wait-Job -Job $job -Timeout $timeoutSec)) {
        Stop-Job -Job $job
        Remove-Job -Job $job -Force
        throw "Silent install exceeded the $($Config.InstallTimeoutMinutes)-min harness watchdog (in-guest install hung). See $latestLog in the guest."
    }

    $run = Receive-Job -Job $job
    Remove-Job -Job $job

    Write-Info "Installer exit code: $($run.ExitCode)"

    $markers = Invoke-Command -VMName $Config.VMName -Credential $Credential -ScriptBlock {
        param($p)
        if (Test-Path $p) { Get-Content $p | Where-Object { $_ -like 'INDYPOS_MARKER *' } } else { @() }
    } -ArgumentList $latestLog

    # Parse the LAST occurrence of each marker key (D9).
    $marker = @{}
    foreach ($line in $markers) {
        if ($line -match '^INDYPOS_MARKER\s+([A-Z_]+)=(.*)$') { $marker[$Matches[1]] = $Matches[2] }
    }
    foreach ($k in $marker.Keys) { Write-Info "marker $k=$($marker[$k])" }

    # Authoritative gating rule: exit code + RESULT + service.
    $failed = ($run.ExitCode -ne 0) -or ($marker['RESULT'] -ne 'success') -or ($marker['SERVICE_STARTED'] -eq 'false')
    if ($failed) {
        throw "Silent install failed (exit=$($run.ExitCode), RESULT=$($marker['RESULT']), SERVICE_STARTED=$($marker['SERVICE_STARTED'])). See $latestLog in the guest."
    }
    if ($marker['CRED_LOCKED'] -eq 'false') {
        Write-Warn2 "Credential file could not be ACL-locked (CRED_LOCKED=false)."
    }
    Write-OK "Silent install completed (RESULT=success)."
}

# --- Verification + report --------------------------------------------------

function Invoke-Verifier {
    param($Credential)

    Write-Section '7. In-VM verification'

    Write-Info 'Running Test-IndyPOSInstallation.ps1 inside guest...'
    $result = Invoke-Command -VMName $Config.VMName -Credential $Credential `
                             -FilePath $VerifierPath
    return $result
}

function Format-Report {
    param($Result)

    Write-Section '8. Results'

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
Write-Host '=== IndyPOS Stage 4 — VM smoke-test ===' -ForegroundColor Magenta

try {
    Test-Prerequisites
    $cred = Get-VMTestCredential -Recreate:$RecreateCredential
    Restore-CleanVM
    Wait-PowerShellDirect -Credential $cred
    Copy-Installer -Credential $cred
    Invoke-SilentInstall -Credential $cred
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
