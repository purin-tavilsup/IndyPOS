<#
.SYNOPSIS
    In-VM verifier for an IndyPOS v4 installation. Runs via PowerShell Direct.

.DESCRIPTION
    Designed to be invoked inside the Hyper-V test VM (via Invoke-Command
    -VMName) once the bootstrapper wizard has finished. Returns a structured
    PSCustomObject the host orchestrator (Reset-AndInstall.ps1) summarises.

    Subset of scripts/verify-install.ps1 — drops the v3.7.0 side-by-side
    invariants (a fresh test VM has no v3 footprint to protect) and the deep
    DB auth checks (smoke-test.ps1 covers API behaviour separately).

    No elevation required; checks are read-only.

.PARAMETER ManifestPath
    Override the discovered install-manifest.json. Mirrors verify-install.ps1
    for consistency.

.OUTPUTS
    PSCustomObject with:
      OverallPass : bool
      Summary     : @{ Pass; Fail; Skip }
      Checks      : array of @{ Category; Name; Pass; Skip; Detail }

.EXAMPLE
    Invoke-Command -VMName 'IndyPOS-Test' -Credential $cred `
                   -FilePath '.\Test-IndyPOSInstallation.ps1'
#>

[CmdletBinding()]
param(
    [string]$ManifestPath
)

$ErrorActionPreference = 'Stop'

$ProgramDataRoot = 'C:\ProgramData\IndyPOS'

$script:Checks = New-Object System.Collections.Generic.List[object]
$script:PassCount = 0
$script:FailCount = 0
$script:SkipCount = 0

function Add-Check {
    param(
        [string]$Category,
        [string]$Name,
        [bool]$Pass,
        [bool]$Skip = $false,
        [string]$Detail = ''
    )
    $script:Checks.Add([PSCustomObject]@{
        Category = $Category
        Name     = $Name
        Pass     = $Pass
        Skip     = $Skip
        Detail   = $Detail
    })
    if ($Skip)     { $script:SkipCount++ }
    elseif ($Pass) { $script:PassCount++ }
    else           { $script:FailCount++ }
}

function Read-Manifest {
    if ($ManifestPath) {
        if (-not (Test-Path $ManifestPath)) {
            throw "Manifest not found at -ManifestPath: $ManifestPath"
        }
        $picked = $ManifestPath
    } else {
        if (-not (Test-Path $ProgramDataRoot)) { return $null }
        $candidates = @(
            Get-ChildItem -Path $ProgramDataRoot -Directory -Filter 'v*' -ErrorAction SilentlyContinue |
                ForEach-Object { Join-Path $_.FullName 'install-manifest.json' } |
                Where-Object { Test-Path $_ }
        )
        if ($candidates.Count -eq 0) { return $null }
        if ($candidates.Count -gt 1) {
            throw "Multiple manifests found; pass -ManifestPath:`n  $($candidates -join "`n  ")"
        }
        $picked = $candidates[0]
    }

    $raw = Get-Content $picked -Raw | ConvertFrom-Json
    if ($raw.manifestVersion -ne 1) {
        throw "Unknown manifest version $($raw.manifestVersion); expected 1."
    }
    return $raw
}

# --- Check groups -----------------------------------------------------------

function Test-Manifest {
    try {
        $m = Read-Manifest
    } catch {
        Add-Check 'Manifest' 'install-manifest.json readable' $false -Detail $_.Exception.Message
        return $null
    }

    if ($null -eq $m) {
        Add-Check 'Manifest' 'install-manifest.json present' $false `
            -Detail "No manifest found under $ProgramDataRoot\v*\. Installer probably never ran."
        return $null
    }

    Add-Check 'Manifest' 'install-manifest.json present' $true `
        -Detail "v$($m.installVersion), installed $($m.installedUtc)"
    return $m
}

function Test-Prerequisites {
    try {
        $runtimes = & dotnet --list-runtimes 2>$null
        $has10 = @($runtimes | Where-Object { $_ -match '^Microsoft\.NETCore\.App 10\.' })
        if ($has10.Count -gt 0) {
            Add-Check 'Prerequisites' '.NET 10 runtime installed' $true -Detail $has10[0]
        } else {
            Add-Check 'Prerequisites' '.NET 10 runtime installed' $false `
                -Detail 'dotnet --list-runtimes has no Microsoft.NETCore.App 10.x'
        }
    } catch {
        Add-Check 'Prerequisites' '.NET CLI present' $false -Detail $_.Exception.Message
    }

    $pg = Get-Service -Name 'postgresql-x64-18' -ErrorAction SilentlyContinue
    if ($null -eq $pg) {
        Add-Check 'Prerequisites' 'PostgreSQL 18 service present' $false `
            -Detail "Expected Windows service 'postgresql-x64-18'"
    } elseif ($pg.Status -ne 'Running') {
        Add-Check 'Prerequisites' 'PostgreSQL 18 service running' $false `
            -Detail "Status: $($pg.Status)"
    } else {
        Add-Check 'Prerequisites' 'PostgreSQL 18 service running' $true
    }
}

function Test-StoreHubService {
    param($Manifest)

    $name = $Manifest.serviceName
    $svc = Get-Service -Name $name -ErrorAction SilentlyContinue
    if ($null -eq $svc) {
        Add-Check 'Service' "$name installed" $false `
            -Detail 'Bootstrapper did not register the Windows service'
        return
    }
    Add-Check 'Service' "$name installed" $true

    Add-Check 'Service' 'Service Running' ($svc.Status -eq 'Running') `
        -Detail "Status: $($svc.Status)"
    Add-Check 'Service' 'StartType Automatic' ($svc.StartType -eq 'Automatic') `
        -Detail "StartType: $($svc.StartType)"

    try {
        $wmi = Get-CimInstance Win32_Service -Filter "Name = '$name'" -ErrorAction SilentlyContinue
        if ($wmi) {
            $expected = $Manifest.storeHubInstallPath
            $imageOk = $wmi.PathName -like "*$expected*"
            Add-Check 'Service' 'ImagePath under SystemRoot' $imageOk `
                -Detail $wmi.PathName
        }
    } catch {
        Add-Check 'Service' 'ImagePath introspection' $false -Skip $true `
            -Detail $_.Exception.Message
    }
}

function Test-Filesystem {
    param($Manifest)

    $required = @(
        @{ Name = 'SystemRoot';      Path = $Manifest.systemRoot },
        @{ Name = 'Config';          Path = $Manifest.configDirectory },
        @{ Name = 'Keys';            Path = $Manifest.keysDirectory },
        @{ Name = 'Logs';            Path = $Manifest.logsDirectory },
        @{ Name = 'Backups';         Path = $Manifest.backupsDirectory },
        @{ Name = 'StoreHub binary'; Path = $Manifest.storeHubInstallPath }
    )

    foreach ($d in $required) {
        $exists = Test-Path $d.Path
        Add-Check 'Filesystem' "$($d.Name) exists" $exists -Detail $d.Path
    }

    $appsettings = Join-Path $Manifest.storeHubInstallPath 'appsettings.json'
    Add-Check 'Filesystem' 'appsettings.json present' (Test-Path $appsettings) `
        -Detail $appsettings
}

function Test-VelopackApp {
    param($Manifest)

    $velopack = $Manifest.velopackInstallPath
    Add-Check 'Velopack' 'WinForms install path exists' (Test-Path $velopack) `
        -Detail $velopack

    # Must match publish.ps1 'vpk pack --mainExe' (the WinForms assembly name).
    $exe = Join-Path $velopack 'IndyPOS.Windows.Forms.exe'
    Add-Check 'Velopack' 'IndyPOS.Windows.Forms.exe present' (Test-Path $exe) -Detail $exe

    $desktopShortcut = Join-Path ([Environment]::GetFolderPath('Desktop')) `
        "$($Manifest.velopackAppId).lnk"
    Add-Check 'Velopack' 'Desktop shortcut present' (Test-Path $desktopShortcut) `
        -Detail $desktopShortcut
}

function Test-HealthEndpoint {
    param($Manifest)

    $port = $Manifest.healthCheckPort
    if (-not $port) { $port = 5000 }
    $url = "http://localhost:$port/health/ready"

    try {
        $r = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 30
        $ok = $r.StatusCode -eq 200
        Add-Check 'Health' "/health/ready returns 200" $ok `
            -Detail "Status: $($r.StatusCode), Body: $($r.Content)"
    } catch {
        Add-Check 'Health' "/health/ready returns 200" $false `
            -Detail "$url -> $($_.Exception.Message)"
    }
}

# --- Run --------------------------------------------------------------------

$manifest = Test-Manifest

if ($manifest) {
    Test-Prerequisites
    Test-StoreHubService -Manifest $manifest
    Test-Filesystem -Manifest $manifest
    Test-VelopackApp -Manifest $manifest
    Test-HealthEndpoint -Manifest $manifest
}

[PSCustomObject]@{
    OverallPass = ($script:FailCount -eq 0)
    Summary     = [PSCustomObject]@{
        Pass = $script:PassCount
        Fail = $script:FailCount
        Skip = $script:SkipCount
    }
    Checks      = $script:Checks.ToArray()
    Manifest    = $manifest
}
