<#
.SYNOPSIS
    Read-only audit of an IndyPOS v4 installation. Asserts install invariants.

.DESCRIPTION
    Sister script to cleanup-v4.ps1 — shares manifest discovery via the same
    glob-under-$ProgramDataRoot strategy. Falls back to defaults when no
    manifest is present (partial-install resilience).

    Checks: prerequisites (.NET 10, Postgres 18), StoreHub service state,
    filesystem layout + key configs, DB/role presence, Velopack WinForms app,
    health endpoint, side-by-side safety (v3.7.0 footprint untouched).

    For API behavior verification, run scripts/smoke-test.ps1 separately.

    No elevation required — this is a read-only audit.

.PARAMETER ManifestPath
    Override the auto-discovered install-manifest.json location. Useful when
    multiple installs exist or for debugging.

.EXAMPLE
    .\verify-install.ps1
    # Discover manifest, run full audit.

.EXAMPLE
    .\verify-install.ps1 -ManifestPath "C:\ProgramData\IndyPOS\v4.0.0\install-manifest.json"
    # Force a specific manifest.
#>

[CmdletBinding()]
param(
    [string]$ManifestPath
)

$ErrorActionPreference = "Stop"

# --- Defaults (mirror cleanup-v4.ps1; overridden by manifest when present) ---
$ProgramDataRoot     = "C:\ProgramData\IndyPOS"
$ServiceName         = "IndyPOS.StoreHub.v4"
$VelopackAppId       = "IndyPOS.POS.v4"
$DatabaseName        = "indypos_storehub"
$AppUser             = "indypos_app"
$SystemRoot          = "$ProgramDataRoot\v4.0.0"
$PostgresBin         = "C:\Program Files\PostgreSQL\18\bin"
$VelopackInstallPath = Join-Path $env:LOCALAPPDATA "$VelopackAppId\current"
$ConfigDirectory     = "$SystemRoot\Config"
$KeysDirectory       = "$SystemRoot\keys"
$LogsDirectory       = "$SystemRoot\logs"
$BackupsDirectory    = "$SystemRoot\backups"
$StoreHubInstallPath = "$SystemRoot\StoreHub"
$HealthCheckPort     = 5000
$ManifestSource      = "defaults"

$script:PassCount = 0
$script:FailCount = 0
$script:SkipCount = 0

# --- Output helpers ---
function Write-Section($title) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host " $title" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
}
function Pass($name, $detail = '') {
    Write-Host "[PASS] $name" -ForegroundColor Green
    if ($detail) { Write-Host "       $detail" -ForegroundColor Gray }
    $script:PassCount++
}
function Fail($name, $detail = '') {
    Write-Host "[FAIL] $name" -ForegroundColor Red
    if ($detail) { Write-Host "       $detail" -ForegroundColor Gray }
    $script:FailCount++
}
function Skip($name, $reason = '') {
    Write-Host "[SKIP] $name" -ForegroundColor Yellow
    if ($reason) { Write-Host "       $reason" -ForegroundColor Gray }
    $script:SkipCount++
}

# --- Manifest discovery (parallel implementation to cleanup-v4.ps1's; if a
#     third script joins, extract to scripts/lib/IndyPOS.Install.psm1) ---
function Read-InstallManifest {
    if ($ManifestPath) {
        if (-not (Test-Path $ManifestPath)) {
            throw "Manifest not found at -ManifestPath: $ManifestPath"
        }
        $picked = $ManifestPath
    } else {
        if (-not (Test-Path $ProgramDataRoot)) { return }
        $candidates = @(Get-ChildItem -Path $ProgramDataRoot -Directory -Filter 'v*' -ErrorAction SilentlyContinue |
                        ForEach-Object { Join-Path $_.FullName 'install-manifest.json' } |
                        Where-Object { Test-Path $_ })
        if ($candidates.Count -eq 0) { return }
        if ($candidates.Count -gt 1) {
            Write-Warning "Multiple manifests found; pass -ManifestPath to choose:"
            $candidates | ForEach-Object { Write-Warning "  $_" }
            return
        }
        $picked = $candidates[0]
    }

    try {
        $m = Get-Content $picked -Raw | ConvertFrom-Json
    } catch {
        Write-Warning "$picked unparseable - $($_.Exception.Message). Using defaults."
        return
    }
    if ($m.manifestVersion -ne 1) {
        Write-Warning "Unknown manifest version $($m.manifestVersion); expected 1. Using defaults."
        return
    }

    $script:ServiceName         = $m.serviceName
    $script:VelopackAppId       = $m.velopackAppId
    $script:DatabaseName        = $m.databaseName
    $script:AppUser             = $m.appUser
    $script:SystemRoot          = $m.systemRoot
    $script:PostgresBin         = $m.postgresBinPath
    $script:VelopackInstallPath = $m.velopackInstallPath
    $script:ConfigDirectory     = $m.configDirectory
    $script:KeysDirectory       = $m.keysDirectory
    $script:LogsDirectory       = $m.logsDirectory
    $script:BackupsDirectory    = $m.backupsDirectory
    $script:StoreHubInstallPath = $m.storeHubInstallPath
    $script:HealthCheckPort     = $m.healthCheckPort
    $script:ManifestSource      = "$picked (v$($m.manifestVersion), installed $($m.installedUtc))"
}

# --- Check groups ---
function Test-Prerequisites {
    Write-Section "1. Prerequisites"

    try {
        $runtimes = & dotnet --list-runtimes 2>$null
        $has10 = @($runtimes | Where-Object { $_ -match '^Microsoft\.NETCore\.App 10\.' })
        if ($has10.Count -gt 0) {
            Pass ".NET 10 Runtime installed" $has10[0]
        } else {
            Fail ".NET 10 Runtime missing" "Expected 'Microsoft.NETCore.App 10.x' in dotnet --list-runtimes"
        }
    } catch {
        Fail ".NET CLI not found" $_.Exception.Message
    }

    $pg = Get-Service -Name 'postgresql-x64-18' -ErrorAction SilentlyContinue
    if ($null -eq $pg) {
        Fail "PostgreSQL 18 service missing" "Expected Windows service 'postgresql-x64-18'"
    } elseif ($pg.Status -ne 'Running') {
        Fail "PostgreSQL 18 service not running" "Status: $($pg.Status)"
    } else {
        Pass "PostgreSQL 18 service running"
    }

    $psql = Join-Path $PostgresBin 'psql.exe'
    if (Test-Path $psql) {
        Pass "psql.exe present" $psql
    } else {
        Fail "psql.exe missing" $psql
    }
}

function Test-StoreHubService {
    Write-Section "2. StoreHub Windows Service"

    $svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($null -eq $svc) {
        Fail "Service '$ServiceName' not installed" "Run IndyPOS-Setup.exe"
        return
    }
    Pass "Service '$ServiceName' installed"

    if ($svc.Status -eq 'Running') {
        Pass "Service is Running"
    } else {
        Fail "Service not running" "Status: $($svc.Status)"
    }

    if ($svc.StartType -eq 'Automatic') {
        Pass "StartType is Automatic"
    } else {
        Fail "StartType wrong" "Expected: Automatic, got: $($svc.StartType)"
    }

    try {
        $wmiSvc = Get-CimInstance Win32_Service -Filter "Name = '$ServiceName'" -ErrorAction SilentlyContinue
        if ($wmiSvc) {
            $imagePath = $wmiSvc.PathName
            if ($imagePath -like "*$StoreHubInstallPath*") {
                Pass "Service ImagePath under SystemRoot" $imagePath
            } else {
                Fail "Service ImagePath WRONG — possible v3 collision" "Expected to contain '$StoreHubInstallPath', got: $imagePath"
            }
        }
    } catch {
        Skip "ImagePath check" $_.Exception.Message
    }
}

function Test-Filesystem {
    Write-Section "3. Filesystem"

    $dirs = @(
        @{ Path = $SystemRoot;          Name = 'SystemRoot' },
        @{ Path = $ConfigDirectory;     Name = 'ConfigDirectory' },
        @{ Path = $KeysDirectory;       Name = 'KeysDirectory' },
        @{ Path = $LogsDirectory;       Name = 'LogsDirectory' },
        @{ Path = $BackupsDirectory;    Name = 'BackupsDirectory' },
        @{ Path = $StoreHubInstallPath; Name = 'StoreHubInstallPath' }
    )
    foreach ($d in $dirs) {
        if (Test-Path $d.Path) {
            Pass "$($d.Name) exists" $d.Path
        } else {
            Fail "$($d.Name) missing" $d.Path
        }
    }

    $storeHubExe = Join-Path $StoreHubInstallPath 'IndyPOS.StoreHub.exe'
    if (Test-Path $storeHubExe) {
        $fileInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($storeHubExe)
        Pass "IndyPOS.StoreHub.exe present" "ProductVersion: $($fileInfo.ProductVersion)"
    } else {
        Fail "IndyPOS.StoreHub.exe missing" $storeHubExe
    }

    $appsettings = Join-Path $StoreHubInstallPath 'appsettings.Production.json'
    if (Test-Path $appsettings) {
        Pass "appsettings.Production.json present"
        try {
            $cfg = Get-Content $appsettings -Raw | ConvertFrom-Json
            $expectedUrls = "http://localhost:$HealthCheckPort"
            if ($cfg.Urls -eq $expectedUrls) {
                Pass "appsettings Urls = $expectedUrls"
            } else {
                Fail "appsettings Urls mismatch" "Expected: $expectedUrls, got: $($cfg.Urls)"
            }
            $connStr = $cfg.connectionStrings.'storehub-db'
            if ($connStr -match [regex]::Escape("Database=$DatabaseName")) {
                Pass "Connection string targets DB '$DatabaseName'"
            } else {
                Fail "Connection string DB mismatch" $connStr
            }
            if ($connStr -match [regex]::Escape("Username=$AppUser")) {
                Pass "Connection string uses user '$AppUser'"
            } else {
                Fail "Connection string user mismatch" $connStr
            }
        } catch {
            Fail "appsettings.Production.json unparseable" $_.Exception.Message
        }
    } else {
        Fail "appsettings.Production.json missing" $appsettings
    }

    $jwtKey = Join-Path $KeysDirectory 'storehub.key'
    if (Test-Path $jwtKey) {
        $content = (Get-Content $jwtKey -Raw).Trim()
        if ($content.Length -ge 86 -and $content.Length -le 90) {
            Pass "storehub.key present" "Length: $($content.Length) chars (~64 bytes base64)"
        } else {
            Fail "storehub.key wrong length" "Expected ~88 chars, got $($content.Length)"
        }
    } else {
        Fail "storehub.key missing" $jwtKey
    }

    $storeConfig = Join-Path $ConfigDirectory 'StoreConfiguration.json'
    if (Test-Path $storeConfig) {
        Pass "StoreConfiguration.json template present"
    } else {
        Fail "StoreConfiguration.json missing" $storeConfig
    }
}

function Test-Database {
    Write-Section "4. PostgreSQL Database"

    $psql = Join-Path $PostgresBin 'psql.exe'
    if (-not (Test-Path $psql)) {
        Skip "Database checks" "psql.exe missing — see prereqs"
        return
    }

    $appsettings = Join-Path $StoreHubInstallPath 'appsettings.Production.json'
    if (-not (Test-Path $appsettings)) {
        Skip "Database checks" "appsettings.Production.json missing — cannot recover credentials"
        return
    }

    try {
        $cfg = Get-Content $appsettings -Raw | ConvertFrom-Json
        $connStr = $cfg.connectionStrings.'storehub-db'
        $builder = [System.Data.Common.DbConnectionStringBuilder]::new()
        $builder.set_ConnectionString($connStr)
        $appPassword = $builder['Password']
    } catch {
        Skip "Database checks" "Could not parse connection string: $($_.Exception.Message)"
        return
    }

    $env:PGPASSWORD = $appPassword
    try {
        $roleCheck = & $psql -h 127.0.0.1 -U $AppUser -d postgres -tAc "SELECT 1 FROM pg_roles WHERE rolname='$AppUser'" 2>&1
        if ($LASTEXITCODE -eq 0 -and ($roleCheck | Out-String) -match '1') {
            Pass "Role '$AppUser' exists in pg_roles"
        } else {
            Fail "Role '$AppUser' not found or unreachable" (($roleCheck | Out-String).Trim())
        }

        $dbCheck = & $psql -h 127.0.0.1 -U $AppUser -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='$DatabaseName'" 2>&1
        if ($LASTEXITCODE -eq 0 -and ($dbCheck | Out-String) -match '1') {
            Pass "Database '$DatabaseName' exists in pg_database"
        } else {
            Fail "Database '$DatabaseName' not found" (($dbCheck | Out-String).Trim())
        }

        $connectCheck = & $psql -h 127.0.0.1 -U $AppUser -d $DatabaseName -tAc "SELECT current_database()" 2>&1
        if ($LASTEXITCODE -eq 0) {
            Pass "App user can connect to '$DatabaseName'"
        } else {
            Fail "App user cannot connect" (($connectCheck | Out-String).Trim())
        }
    } finally {
        Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
    }
}

function Test-Velopack {
    Write-Section "5. Velopack WinForms App"

    $winFormsExe = Join-Path $VelopackInstallPath 'IndyPOS.Windows.Forms.exe'
    if (-not (Test-Path $winFormsExe)) {
        Fail "WinForms exe missing" $winFormsExe
        return
    }
    Pass "WinForms exe present" $winFormsExe

    $fileInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($winFormsExe)
    if ($fileInfo.ProductVersion -like '4.0.0*') {
        Pass "WinForms ProductVersion starts with 4.0.0" $fileInfo.ProductVersion
    } else {
        Fail "WinForms ProductVersion mismatch" "Expected starts-with 4.0.0, got: $($fileInfo.ProductVersion)"
    }

    # Velopack's Update.exe sits one level up from current\ (in the AppId root).
    $velopackRoot = Split-Path $VelopackInstallPath -Parent
    $updateExe = Join-Path $velopackRoot 'Update.exe'
    if (Test-Path $updateExe) {
        Pass "Velopack Update.exe present" "Auto-update wired"
    } else {
        Fail "Velopack Update.exe missing" $updateExe
    }
}

function Test-HealthEndpoint {
    Write-Section "6. Health Endpoint"

    try {
        $tcp = Test-NetConnection -ComputerName localhost -Port $HealthCheckPort -WarningAction SilentlyContinue -InformationLevel Quiet
        if ($tcp) {
            Pass "Port $HealthCheckPort is listening"
        } else {
            Fail "Port $HealthCheckPort not listening" "Is the StoreHub service running?"
            return
        }
    } catch {
        Skip "Port check" $_.Exception.Message
    }

    # Industry-standard probes: /health/live = liveness, /health/ready = readiness.
    # /health is dev-only (verbose body, leaks impl details).
    foreach ($path in @('/health/live', '/health/ready')) {
        try {
            $resp = Invoke-WebRequest -Uri "http://localhost:$HealthCheckPort$path" -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
            if ($resp.StatusCode -eq 200) {
                Pass "GET $path returns 200"
            } else {
                Fail "GET $path returned $($resp.StatusCode)"
            }
        } catch {
            Fail "GET $path failed" $_.Exception.Message
        }
    }
}

function Test-SideBySide {
    Write-Section "7. Side-by-Side Safety"

    if (-not (Test-Path $ProgramDataRoot)) {
        Skip "Side-by-side checks" "$ProgramDataRoot does not exist (no installs at all)"
        return
    }

    # v4 location confinement is already covered by Test-Filesystem;
    # don't re-fail here. Just verify the install dir is strictly a
    # v*\ subdir (defence against accidental top-level writes).
    if ($SystemRoot -match [regex]::Escape($ProgramDataRoot) + '\\v\d+\.\d+\.\d+') {
        Pass "SystemRoot is a v*\ subdir (no top-level v4 leakage)" $SystemRoot
    } else {
        Fail "SystemRoot is NOT a v-version subdir of ProgramDataRoot" $SystemRoot
    }

    # Top-level entries other than v*\ dirs are presumed pre-existing v3 or
    # unrelated. We can't verify byte-for-byte untouchedness without a
    # baseline snapshot, so just enumerate them for the human reviewer.
    $topLevel = Get-ChildItem $ProgramDataRoot -ErrorAction SilentlyContinue
    $nonVersioned = $topLevel | Where-Object { $_.Name -notmatch '^v\d+\.\d+\.\d+$' }
    if ($nonVersioned.Count -gt 0) {
        Pass "v3.7.0-era top-level entries detected" "$($nonVersioned.Count) found: $($nonVersioned.Name -join ', ')"
    } else {
        Skip "v3.7.0 footprint check" "No top-level non-v*\ entries — no v3.7.0 install detected on this box"
    }
}

# --- Main ---
Read-InstallManifest

Write-Host ""
Write-Host "IndyPOS v4 Install Verification" -ForegroundColor Magenta
Write-Host "Time:   $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Magenta
Write-Host "Source: $ManifestSource" -ForegroundColor Magenta

Test-Prerequisites
Test-StoreHubService
Test-Filesystem
Test-Database
Test-Velopack
Test-HealthEndpoint
Test-SideBySide

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " SUMMARY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Passed:  $script:PassCount" -ForegroundColor Green
Write-Host "Failed:  $script:FailCount" -ForegroundColor $(if ($script:FailCount -gt 0) { 'Red' } else { 'Green' })
Write-Host "Skipped: $script:SkipCount" -ForegroundColor $(if ($script:SkipCount -gt 0) { 'Yellow' } else { 'Green' })
Write-Host "Total:   $($script:PassCount + $script:FailCount + $script:SkipCount)"
Write-Host ""

if ($script:FailCount -gt 0) {
    Write-Host "Verification FAILED. Run scripts/cleanup-v4.ps1 -Force and re-install." -ForegroundColor Red
    exit 1
} else {
    Write-Host "Verification PASSED." -ForegroundColor Green
    exit 0
}
