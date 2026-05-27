<#
.SYNOPSIS
    Teardown script for IndyPOS v4 installation. Reverses installer actions.

.DESCRIPTION
    Companion to installer/IndyPOS.Bootstrapper. Removes the v4 footprint so
    smoke-test cycles can start from a clean slate. Reverse order of install:

      1. Stop + delete Windows service 'IndyPOS.StoreHub.v4'
      2. Uninstall the Velopack WinForms app
      3. Drop the v4 PostgreSQL database
      4. Remove C:\ProgramData\IndyPOS\v4.0.0\
      5. Remove %LOCALAPPDATA%\IndyPOS.POS.v4\
      6. (Opt-in) uninstall PostgreSQL 18 itself

    The v3.7.0 footprint under C:\ProgramData\IndyPOS\ (Config\, etc.) is
    NEVER touched.

    DATABASE CREDENTIALS:
    The Postgres superuser password is generated randomly during install and
    not persisted, so we recover the v4 app-user credentials from
    appsettings.Production.json. The app user owns the database, so it can
    DROP DATABASE on its own. The role itself is preserved (installer is
    idempotent on existing roles). Pass -PostgresPassword to also DROP ROLE,
    or pass -SkipDatabase to leave the DB intact.

    REQUIRES ADMINISTRATOR PRIVILEGES.

.PARAMETER Force
    Skip the confirmation prompt.

.PARAMETER RemovePostgres
    Also uninstall PostgreSQL 18 (slow). Only needed when testing the
    from-scratch install path. The Postgres data directory is left behind.

.PARAMETER SkipDatabase
    Skip database cleanup entirely. Leaves indypos_storehub + indypos_app intact.

.PARAMETER PostgresPassword
    PostgreSQL superuser password. When provided, also DROP ROLE indypos_app.

.EXAMPLE
    .\cleanup-v4.ps1
    # Interactive teardown with confirmation. Keeps Postgres install.

.EXAMPLE
    .\cleanup-v4.ps1 -Force
    # Skip confirm. Fast teardown for smoke-test loops.

.EXAMPLE
    .\cleanup-v4.ps1 -Force -RemovePostgres
    # Full teardown including PostgreSQL 18 itself.
#>

[CmdletBinding()]
param(
    [switch]$Force,
    [switch]$RemovePostgres,
    [switch]$SkipDatabase,
    [string]$PostgresPassword
)

$ErrorActionPreference = "Stop"

# --- Defaults (used when install-manifest.json is missing, e.g. partial install) ---
# The installer writes a manifest at $SystemRoot\install-manifest.json that
# overrides these. See installer/IndyPOS.Bootstrapper/Installers/InstallManifest.cs
# for the schema.
$ProgramDataRoot = "C:\ProgramData\IndyPOS"
$ServiceName     = "IndyPOS.StoreHub.v4"
$VelopackAppId   = "IndyPOS.POS.v4"
$DatabaseName    = "indypos_storehub"
$AppUser         = "indypos_app"
$SystemRoot      = "$ProgramDataRoot\v4.0.0"
$PostgresBin     = "C:\Program Files\PostgreSQL\18\bin"
$VelopackRoot    = Join-Path $env:LOCALAPPDATA $VelopackAppId
$AppsettingsPath = Join-Path $SystemRoot "StoreHub\appsettings.Production.json"
$ManifestSource  = "defaults"

function Read-InstallManifest {
    # Glob for install-manifest.json under any v*\ subdir of $ProgramDataRoot.
    # Each install version gets its own folder, so we can't assume which one
    # is present — discovery lets cleanup work for v4.0.0, v4.0.1, etc.
    # If none found, keep the baked-in defaults so cleanup still copes with
    # a half-installed system.
    if (-not (Test-Path $ProgramDataRoot)) { return }

    $candidates = @(Get-ChildItem -Path $ProgramDataRoot -Directory -Filter 'v*' -ErrorAction SilentlyContinue |
                    ForEach-Object { Join-Path $_.FullName 'install-manifest.json' } |
                    Where-Object { Test-Path $_ })

    if ($candidates.Count -eq 0) { return }
    if ($candidates.Count -gt 1) {
        Write-Warn "Multiple manifests found; refusing to guess. Pass -SystemRoot to disambiguate:"
        $candidates | ForEach-Object { Write-Warn "  $_" }
        return
    }

    $manifestPath = $candidates[0]
    try {
        $m = Get-Content $manifestPath -Raw | ConvertFrom-Json
    } catch {
        Write-Warn "$manifestPath present but unparseable - $($_.Exception.Message). Using defaults."
        return
    }

    # Schema-version guard so future manifests don't silently break this script.
    if ($m.manifestVersion -ne 1) {
        Write-Warn "Unknown manifest version $($m.manifestVersion); expected 1. Using defaults."
        return
    }

    $script:ServiceName     = $m.serviceName
    $script:VelopackAppId   = $m.velopackAppId
    $script:DatabaseName    = $m.databaseName
    $script:AppUser         = $m.appUser
    $script:SystemRoot      = $m.systemRoot
    $script:PostgresBin     = $m.postgresBinPath
    $script:VelopackRoot    = Split-Path $m.velopackInstallPath -Parent
    $script:AppsettingsPath = Join-Path $m.storeHubInstallPath "appsettings.Production.json"
    $script:ManifestSource  = "$manifestPath (v$($m.manifestVersion), installed $($m.installedUtc))"
}

# --- Output helpers ---
function Write-Section($title) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host " $title" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
}
function Write-Ok($msg)   { Write-Host "[OK]   $msg" -ForegroundColor Green }
function Write-Info($msg) { Write-Host "[..]   $msg" -ForegroundColor Gray }
function Write-Warn($msg) { Write-Host "[WARN] $msg" -ForegroundColor Yellow }
function Write-Err($msg)  { Write-Host "[ERR]  $msg" -ForegroundColor Red }

# --- Preflight ---
function Test-IsAdmin {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Assert-V4Path {
    # Refuse anything that isn't a v<Major>.<Minor>.<Patch> subdirectory of
    # the shared parent. Catches accidental SystemRoot collapses, typos, and
    # future-version drift.
    param([Parameter(Mandatory)] [string]$Path)
    $resolved = [System.IO.Path]::GetFullPath($Path).TrimEnd('\').ToLowerInvariant()
    $parentResolved = [System.IO.Path]::GetFullPath($ProgramDataRoot).TrimEnd('\').ToLowerInvariant()
    $expectedPrefix = $parentResolved + '\v'
    if (-not $resolved.StartsWith($expectedPrefix)) {
        throw "Safety guard: '$Path' is not a v-prefixed subdirectory of '$ProgramDataRoot'."
    }
    $suffix = $resolved.Substring($parentResolved.Length + 1)
    if ($suffix -notmatch '^v\d+\.\d+\.\d+(\\|$)') {
        throw "Safety guard: '$Path' does not match v<Major>.<Minor>.<Patch> pattern."
    }
}

function Assert-SafetyGuards {
    Assert-V4Path -Path $SystemRoot
    if (-not (Test-IsAdmin)) {
        Write-Err "This script must be run as Administrator (sc.exe + ProgramData writes require elevation)."
        exit 1
    }
}

# --- Step 1: service ---
function Wait-ServiceGone {
    # sc.exe delete is async — SCM marks for deletion but the entry survives
    # until all handles close. A follow-up install would then see the stale
    # entry and skip recreation. Poll until Get-Service stops finding it.
    param([string]$Name, [int]$TimeoutSeconds = 30)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if ($null -eq (Get-Service -Name $Name -ErrorAction SilentlyContinue)) {
            return $true
        }
        Start-Sleep -Milliseconds 500
    }
    return $false
}

function Remove-StoreHubService {
    Write-Section "1. Windows Service"
    $svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($null -eq $svc) {
        Write-Ok "Service '$ServiceName' not installed; nothing to do."
        return
    }

    if ($svc.Status -ne 'Stopped') {
        Write-Info "Stopping service '$ServiceName' (current status: $($svc.Status))..."
        try {
            Stop-Service -Name $ServiceName -Force -ErrorAction Stop
            $svc.WaitForStatus('Stopped', '00:00:30')
        } catch {
            Write-Warn "Stop-Service failed: $($_.Exception.Message). Continuing — sc.exe delete will try anyway."
        }
    }

    Write-Info "Deleting service registration..."
    & sc.exe delete $ServiceName | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "sc.exe delete '$ServiceName' failed with exit code $LASTEXITCODE"
    }

    if (Wait-ServiceGone -Name $ServiceName -TimeoutSeconds 30) {
        Write-Ok "Service '$ServiceName' removed."
    } else {
        Write-Warn "Service '$ServiceName' is marked for deletion but still visible after 30s. A reboot may be required; the next install may collide."
    }
}

# --- Step 2: Velopack ---
function Wait-VelopackProcessExit {
    # Velopack's Update.exe --uninstall relaunches itself from %TEMP% to delete
    # current\, then exits. The parent returns immediately; if we wipe the dir
    # while the detached child is still iterating it, the cleanup races.
    param([int]$TimeoutSeconds = 60)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $running = @(Get-CimInstance Win32_Process `
                        -Filter "Name = 'Update.exe' OR Name = 'IndyPOS.Windows.Forms.exe'" `
                        -ErrorAction SilentlyContinue) |
                    Where-Object { $_.ExecutablePath -like "*$VelopackAppId*" }
        if ($running.Count -eq 0) { return $true }
        Start-Sleep -Milliseconds 500
    }
    return $false
}

function Remove-VelopackShortcuts {
    # Velopack creates Start Menu shortcuts that point into $VelopackRoot.
    # Remove only the ones whose target is inside our app dir.
    $startMenus = @(
        (Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs'),
        (Join-Path $env:ProgramData 'Microsoft\Windows\Start Menu\Programs')
    )
    $wsh = New-Object -ComObject WScript.Shell
    $removed = 0
    foreach ($menu in $startMenus) {
        if (-not (Test-Path $menu)) { continue }
        Get-ChildItem $menu -Recurse -Filter '*.lnk' -ErrorAction SilentlyContinue | ForEach-Object {
            try {
                $shortcut = $wsh.CreateShortcut($_.FullName)
                if ($shortcut.TargetPath -like "$VelopackRoot*") {
                    Remove-Item $_.FullName -Force -ErrorAction SilentlyContinue
                    $removed++
                }
            } catch {
                # Skip shortcuts we can't read.
            }
        }
    }
    if ($removed -gt 0) {
        Write-Info "Swept $removed orphan Start Menu shortcut(s)."
    }
}

function Remove-VelopackTempSetup {
    # VelopackLauncher.cs copies Setup.exe to %TEMP% and never deletes it.
    $tempSetup = Join-Path $env:TEMP "$VelopackAppId-Setup.exe"
    if (Test-Path $tempSetup) {
        try {
            Remove-Item $tempSetup -Force
            Write-Info "Removed orphan installer: $tempSetup"
        } catch {
            Write-Warn "Could not remove $tempSetup - $($_.Exception.Message)"
        }
    }
}

function Remove-VelopackApp {
    Write-Section "2. Velopack WinForms App"
    $updateExe = Join-Path $VelopackRoot "current\Update.exe"

    if (Test-Path $updateExe) {
        Write-Info "Invoking '$updateExe --uninstall'..."
        try {
            Start-Process -FilePath $updateExe -ArgumentList "--uninstall" -NoNewWindow
        } catch {
            Write-Warn "Velopack uninstall invocation failed: $($_.Exception.Message)."
        }

        Write-Info "Waiting for Velopack to finish (up to 60s)..."
        if (Wait-VelopackProcessExit -TimeoutSeconds 60) {
            Write-Ok "Velopack uninstall completed."
        } else {
            Write-Warn "Velopack processes still running after 60s; proceeding anyway."
        }
    } else {
        Write-Ok "Velopack not installed at $VelopackRoot; checking for orphan artifacts."
    }

    Remove-VelopackShortcuts
    Remove-VelopackTempSetup
}

# --- Step 3: database ---
function Get-AppPasswordFromConfig {
    if (-not (Test-Path $AppsettingsPath)) { return $null }
    try {
        $json = Get-Content $AppsettingsPath -Raw | ConvertFrom-Json
        $connStr = $json.connectionStrings.'storehub-db'
        if (-not $connStr) { return $null }

        # Use DbConnectionStringBuilder for correct handling of escaped/quoted
        # values. A simple regex breaks when the password contains ';' or '"'.
        $builder = [System.Data.Common.DbConnectionStringBuilder]::new()
        $builder.set_ConnectionString($connStr)
        return $builder['Password']
    } catch {
        Write-Warn "Could not parse $AppsettingsPath - $($_.Exception.Message)"
        return $null
    }
}

function Invoke-Psql {
    param(
        [Parameter(Mandatory)] [string]$User,
        [Parameter(Mandatory)] [string]$Password,
        [Parameter(Mandatory)] [string]$Database,
        [Parameter(Mandatory)] [string]$Sql
    )
    $psql = Join-Path $PostgresBin "psql.exe"
    $env:PGPASSWORD = $Password
    try {
        $output = & $psql -h 127.0.0.1 -U $User -d $Database -tAc $Sql 2>&1
        return [pscustomobject]@{
            Success  = ($LASTEXITCODE -eq 0)
            ExitCode = $LASTEXITCODE
            Output   = ($output | Out-String).Trim()
        }
    } finally {
        Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
    }
}

function Remove-StoreHubDatabase {
    Write-Section "3. Database"
    if ($SkipDatabase) {
        Write-Warn "-SkipDatabase set; leaving '$DatabaseName' and role '$AppUser' intact."
        return
    }
    if (-not (Test-Path (Join-Path $PostgresBin "psql.exe"))) {
        Write-Warn "psql.exe not found at $PostgresBin; cannot drop database."
        return
    }

    $appPassword = Get-AppPasswordFromConfig
    $droppedViaAppUser = $false

    if ($appPassword) {
        Write-Info "Recovered '$AppUser' credentials from appsettings.Production.json."
        Write-Info "Dropping database '$DatabaseName' as owner '$AppUser'..."
        $result = Invoke-Psql -User $AppUser -Password $appPassword -Database "postgres" `
                              -Sql "DROP DATABASE IF EXISTS $DatabaseName"
        if ($result.Success) {
            Write-Ok "Database '$DatabaseName' dropped."
            $droppedViaAppUser = $true
        } else {
            Write-Warn "App-user drop failed (exit $($result.ExitCode)): $($result.Output)"
        }
    } else {
        Write-Info "No app-user credentials available (appsettings.Production.json missing or unparseable)."
    }

    # Need superuser to drop the role itself. Optional unless requested.
    $superPw = $PostgresPassword
    if (-not $droppedViaAppUser -and -not $superPw -and -not $Force) {
        $secure = Read-Host -Prompt "Enter postgres superuser password (blank to skip)" -AsSecureString
        if ($secure.Length -gt 0) {
            $bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
            try {
                $superPw = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
            } finally {
                [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
            }
        }
    }

    if ($superPw) {
        if (-not $droppedViaAppUser) {
            Write-Info "Dropping database '$DatabaseName' as postgres superuser..."
            $r = Invoke-Psql -User "postgres" -Password $superPw -Database "postgres" `
                             -Sql "DROP DATABASE IF EXISTS $DatabaseName"
            if ($r.Success) { Write-Ok "Database '$DatabaseName' dropped." }
            else { Write-Err "DROP DATABASE failed (exit $($r.ExitCode)): $($r.Output)"; return }
        }

        # Reassign + drop any objects/grants still owned by the role in the
        # postgres system db, otherwise DROP ROLE fails with:
        #   "role cannot be dropped because some objects depend on it".
        Write-Info "Reassigning/dropping objects owned by '$AppUser'..."
        Invoke-Psql -User "postgres" -Password $superPw -Database "postgres" `
                    -Sql "REASSIGN OWNED BY $AppUser TO postgres" | Out-Null
        Invoke-Psql -User "postgres" -Password $superPw -Database "postgres" `
                    -Sql "DROP OWNED BY $AppUser CASCADE" | Out-Null

        Write-Info "Dropping role '$AppUser'..."
        $r2 = Invoke-Psql -User "postgres" -Password $superPw -Database "postgres" `
                          -Sql "DROP ROLE IF EXISTS $AppUser"
        if ($r2.Success) { Write-Ok "Role '$AppUser' dropped." }
        else { Write-Warn "DROP ROLE failed (exit $($r2.ExitCode)): $($r2.Output)" }
    } else {
        Write-Info "Role '$AppUser' preserved (installer reuses existing role on next install)."
    }
}

# --- Step 4: directories ---
function Remove-V4Directories {
    Write-Section "4. Filesystem"

    # Belt-and-braces: re-run the path-pattern guard right before Remove-Item.
    Assert-V4Path -Path $SystemRoot

    if (Test-Path $SystemRoot) {
        Write-Info "Removing $SystemRoot ..."
        Remove-Item -Path $SystemRoot -Recurse -Force
        Write-Ok "Removed $SystemRoot"
    } else {
        Write-Ok "$SystemRoot already absent."
    }

    if (Test-Path $VelopackRoot) {
        Write-Info "Removing $VelopackRoot ..."
        Remove-Item -Path $VelopackRoot -Recurse -Force
        Write-Ok "Removed $VelopackRoot"
    } else {
        Write-Ok "$VelopackRoot already absent."
    }
}

# --- Step 5 (opt-in): Postgres ---
function Uninstall-Postgres18 {
    Write-Section "5. PostgreSQL 18 (opt-in)"
    $uninstaller = "C:\Program Files\PostgreSQL\18\uninstall-postgresql.exe"
    if (-not (Test-Path $uninstaller)) {
        Write-Warn "PostgreSQL uninstaller not found at $uninstaller; skipping."
        return
    }
    Write-Info "Running unattended uninstall (this can take a minute)..."
    Start-Process -FilePath $uninstaller -ArgumentList "--mode", "unattended" -Wait -NoNewWindow
    if ($LASTEXITCODE -ne 0) {
        Write-Err "Uninstaller exited with code $LASTEXITCODE"
        return
    }
    $dataDir = "C:\Program Files\PostgreSQL\18\data"
    if (Test-Path $dataDir) {
        Write-Warn "Data directory '$dataDir' was left behind by the uninstaller. Remove manually if desired."
    }
    Write-Ok "PostgreSQL 18 uninstalled."
}

# --- Main ---
Read-InstallManifest
Assert-SafetyGuards

Write-Host ""
Write-Host "IndyPOS v4 Cleanup" -ForegroundColor Magenta
Write-Host "Time:   $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Magenta
Write-Host "Source: $ManifestSource" -ForegroundColor Magenta
Write-Host ""
Write-Host "About to remove:"
Write-Host "  - Service:    $ServiceName"
Write-Host "  - Velopack:   $VelopackRoot"
if ($SkipDatabase) {
    Write-Host "  - Database:   (skipped)"
} else {
    Write-Host "  - Database:   $DatabaseName (role $AppUser preserved unless -PostgresPassword given)"
}
Write-Host "  - Directory:  $SystemRoot"
if ($RemovePostgres) {
    Write-Host "  - PostgreSQL: FULL UNINSTALL of 18.x"
}
Write-Host ""
Write-Host "v3.7.0 footprint under $ProgramDataRoot\Config\ will NOT be touched." -ForegroundColor Gray
Write-Host ""

if (-not $Force) {
    $confirm = Read-Host "Continue? (y/N)"
    if ($confirm -notin @('y','Y')) {
        Write-Host "Aborted." -ForegroundColor Yellow
        exit 0
    }
}

Remove-StoreHubService
Remove-VelopackApp
Remove-StoreHubDatabase
Remove-V4Directories
if ($RemovePostgres) { Uninstall-Postgres18 }

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Cleanup complete." -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
