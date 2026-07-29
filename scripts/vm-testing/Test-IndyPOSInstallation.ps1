<#
.SYNOPSIS
    In-VM verifier for an IndyPOS v4 installation. Runs via PowerShell Direct.

.DESCRIPTION
    Designed to be invoked inside the Hyper-V test VM (via Invoke-Command
    -VMName) once the bootstrapper wizard has finished. Returns a structured
    PSCustomObject the host orchestrator (Reset-AndInstall.ps1) summarises.

    Subset of scripts/verify-install.ps1 - drops the v3.7.0 side-by-side
    invariants (a fresh test VM has no v3 footprint to protect) and the deep
    DB auth checks (smoke-test.ps1 covers API behaviour separately).

    No elevation required; checks are read-only.

.PARAMETER AssertDatabase
    Also assert the live payment_method catalogue and that exactly one store id owns
    it. Decrypts the DPAPI-sealed connection string in-guest and drives psql. Only
    meaningful after an upgrade of a real store image.

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
    # Position 0 so the host orchestrator can pass it through Invoke-Command -FilePath,
    # which binds arguments positionally only.
    [Parameter(Position = 0)]
    [switch]$AssertDatabase,

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

# --- Database assertions (upgrade runs only) --------------------------------

# Decrypt the DPAPI-sealed connection string in-guest. Machine scope, entropy =
# SHA256("IndyPOS:ConnectionStrings:storehub-db"). The guest runs PowerShell 5.1, which
# has no static SHA256.HashData, so use an instance.
function Get-StoreHubConnectionString {
    param([string]$AppSettingsPath)

    $raw = (Get-Content $AppSettingsPath -Raw | ConvertFrom-Json).connectionStrings.'storehub-db'
    if (-not $raw.StartsWith('DPAPI:')) { return $raw }

    Add-Type -AssemblyName System.Security
    $sha = [System.Security.Cryptography.SHA256]::Create()
    $entropy = $sha.ComputeHash([Text.Encoding]::UTF8.GetBytes('IndyPOS:ConnectionStrings:storehub-db'))
    $cipher = [Convert]::FromBase64String($raw.Substring(6))
    $plain = [System.Security.Cryptography.ProtectedData]::Unprotect(
        $cipher, $entropy, [System.Security.Cryptography.DataProtectionScope]::LocalMachine)

    return [Text.Encoding]::UTF8.GetString($plain)
}

function Invoke-StoreHubQuery {
    param([string]$ConnectionString, [string]$Sql, [string]$PsqlPath)

    $parts = @{}
    foreach ($kv in $ConnectionString.Split(';')) {
        if ($kv -match '^\s*([^=]+)=(.*)$') { $parts[$Matches[1].Trim()] = $Matches[2].Trim() }
    }

    $env:PGPASSWORD = $parts['Password']
    try {
        # SQL via stdin: native-argument quoting strips the double quotes psql needs.
        return $Sql | & $PsqlPath -h $parts['Host'] -p $parts['Port'] -U $parts['Username'] `
                                  -d $parts['Database'] -w -t -A -F '|'
    }
    finally { Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue }
}

function Test-Database {
    $appSettings = Join-Path $ProgramDataRoot 'v4\StoreHub\appsettings.json'
    $psql = 'C:\Program Files\PostgreSQL\18\bin\psql.exe'

    if (-not (Test-Path $appSettings)) {
        Add-Check 'Database' 'StoreHub appsettings.json present' $false -Detail $appSettings
        return
    }
    if (-not (Test-Path $psql)) {
        Add-Check 'Database' 'psql.exe available' $false -Detail $psql
        return
    }

    try {
        $conn = Get-StoreHubConnectionString -AppSettingsPath $appSettings
    } catch {
        Add-Check 'Database' 'Connection string decrypts on this machine' $false `
            -Detail $_.Exception.Message
        return
    }

    $rows = Invoke-StoreHubQuery -ConnectionString $conn -PsqlPath $psql `
        -Sql 'SELECT code, kind, is_enabled FROM payment_method ORDER BY code;'

    # The snapshot is a genuine PRE-reclassification store: PayLater kind=1, WelfareCard kind=1.
    Add-Check 'Database' 'PayLater reclassified to Special (kind=3)' `
        ([bool]($rows | Where-Object { $_ -like 'PayLater|3|*' }))

    Add-Check 'Database' 'WelfareCard is GovernmentCampaign (kind=2) and still enabled' `
        ([bool]($rows | Where-Object { $_ -eq 'WelfareCard|2|t' }))

    Add-Check 'Database' 'Cash and MoneyTransfer are Standard (kind=1)' `
        (@($rows | Where-Object { $_ -like 'Cash|1|*' -or $_ -like 'MoneyTransfer|1|*' }).Count -eq 2)

    $storeIds = Invoke-StoreHubQuery -ConnectionString $conn -PsqlPath $psql `
        -Sql 'SELECT DISTINCT store_id FROM payment_method;'

    # A second catalogue under STORE-<MachineName> is the exact orphaning that detection
    # rule 2's non-empty Store:Id check exists to prevent.
    Add-Check 'Database' 'Exactly one store id in payment_method' `
        (@($storeIds | Where-Object { $_ }).Count -eq 1) `
        -Detail ($storeIds -join ', ')
}

# --- Run --------------------------------------------------------------------

$manifest = Test-Manifest

if ($manifest) {
    Test-Prerequisites
    Test-StoreHubService -Manifest $manifest
    Test-Filesystem -Manifest $manifest
    Test-VelopackApp -Manifest $manifest
    Test-HealthEndpoint -Manifest $manifest

    if ($AssertDatabase) { Test-Database }
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
