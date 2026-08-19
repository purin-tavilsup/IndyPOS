<#
.SYNOPSIS
    Read-only capture of a v3.7.0 store's on-disk and registry footprint.

.DESCRIPTION
    Epic 3 owes one v3.7.0-coexistence check before the first cutover.

    v3.7.0 is an XCOPY deployment: no installer, no uninstall entry, no versioned
    folder. The binary was copied into place and its data lives in non-versioned
    directories at the root of C:\ProgramData\IndyPOS -- Config\, db\Store.db, Logs\.
    So there is nothing to install to build a test machine, and no manifest to read a
    till's layout from. Capturing what a store actually has is the only way to know,
    and New-V3Footprint.ps1 replays it onto the test VM.

    Note v3 and v4 SHARE that root: v4 lives in v4\ beside v3's folders. Anything that
    deletes broadly under C:\ProgramData\IndyPOS would take the shop's live Store.db.

    STRICTLY READ-ONLY on the store machine. It records paths, sizes and timestamps.
    It never opens Store.db, never reads file CONTENTS, and never writes anywhere
    except the -OutputPath you choose.

    Run this on a till BEFORE v4 is installed on it. Running it after would capture
    v4's own directories as part of the "v3" baseline.

.PARAMETER OutputPath
    Where to write the JSON capture. Defaults to the desktop.

.PARAMETER StoreLabel
    A name for the store, recorded in the capture so several can be told apart.

.EXAMPLE
    .\Get-V3Footprint.ps1 -StoreLabel GeneralHardware
    # Writes v3-footprint-GeneralHardware.json to the desktop.

.NOTES
    IndyPOS is a PUBLIC repository. Do not commit the JSON this produces — it lists
    real store paths. Keep captures outside the repo.
#>

[CmdletBinding()]
param(
    [string]$OutputPath,
    [Parameter(Mandatory)][string]$StoreLabel
)

$ErrorActionPreference = "Stop"

$ProgramDataRoot = "C:\ProgramData\IndyPOS"

function Get-TreeEntries {
    param([string]$Root, [int]$Depth = 3)

    if (-not (Test-Path $Root)) { return @() }

    # -Force so hidden/system entries are captured too; a footprint that misses them
    # would replay as "clean" and the coexistence check would prove less than it claims.
    Get-ChildItem -LiteralPath $Root -Recurse -Depth $Depth -Force -ErrorAction SilentlyContinue |
        ForEach-Object {
            [PSCustomObject]@{
                RelativePath  = $_.FullName.Substring($Root.Length).TrimStart('\')
                IsDirectory   = $_.PSIsContainer
                SizeBytes     = if ($_.PSIsContainer) { $null } else { $_.Length }
                LastWriteUtc  = $_.LastWriteTimeUtc.ToString("o")
                Attributes    = $_.Attributes.ToString()
            }
        }
}

function Get-IndyServices {
    Get-ChildItem "HKLM:\SYSTEM\CurrentControlSet\Services" -ErrorAction SilentlyContinue |
        Where-Object { $_.PSChildName -like "*IndyPOS*" } |
        ForEach-Object {
            $props = Get-ItemProperty $_.PSPath -ErrorAction SilentlyContinue
            [PSCustomObject]@{
                Name      = $_.PSChildName
                ImagePath = $props.ImagePath
                Start     = $props.Start
            }
        }
}

function Get-IndyUninstallEntries {
    $roots = @(
        "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
        "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
        "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"
    )

    foreach ($root in $roots) {
        Get-ChildItem $root -ErrorAction SilentlyContinue | ForEach-Object {
            $props = Get-ItemProperty $_.PSPath -ErrorAction SilentlyContinue
            if ($props.DisplayName -like "*IndyPOS*") {
                [PSCustomObject]@{
                    Hive            = $root
                    DisplayName     = $props.DisplayName
                    DisplayVersion  = $props.DisplayVersion
                    InstallLocation = $props.InstallLocation
                }
            }
        }
    }
}

$appRoots = @(
    (Join-Path $env:LOCALAPPDATA "IndyPOS"),
    (Join-Path $env:LOCALAPPDATA "IndyPOS.POS"),
    (Join-Path $env:LOCALAPPDATA "IndyPOS.POS.v4"),
    "C:\Program Files\IndyPOS",
    "C:\Program Files (x86)\IndyPOS"
) | Where-Object { Test-Path $_ }

$capture = [PSCustomObject]@{
    StoreLabel        = $StoreLabel
    CapturedUtc       = (Get-Date).ToUniversalTime().ToString("o")
    MachineName       = $env:COMPUTERNAME
    ProgramDataRoot   = $ProgramDataRoot
    ProgramDataExists = (Test-Path $ProgramDataRoot)
    ProgramDataTree   = @(Get-TreeEntries -Root $ProgramDataRoot)
    AppRoots          = @($appRoots | ForEach-Object {
        [PSCustomObject]@{ Path = $_; Tree = @(Get-TreeEntries -Root $_ -Depth 2) }
    })
    Services          = @(Get-IndyServices)
    UninstallEntries  = @(Get-IndyUninstallEntries)
}

if (-not $OutputPath) {
    $desktop = [Environment]::GetFolderPath("Desktop")
    $OutputPath = Join-Path $desktop "v3-footprint-$StoreLabel.json"
}

$capture | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $OutputPath -Encoding UTF8

$topLevel = $capture.ProgramDataTree | Where-Object { $_.RelativePath -notmatch '\\' }
$versioned = $topLevel | Where-Object { $_.IsDirectory -and $_.RelativePath -match '^v\d+(\.\d+)*$' }

Write-Host ""
Write-Host "Captured $StoreLabel -> $OutputPath" -ForegroundColor Green
Write-Host "  $ProgramDataRoot exists : $($capture.ProgramDataExists)"
Write-Host "  entries under it        : $($capture.ProgramDataTree.Count)"
Write-Host "  top-level entries       : $($topLevel.Count)"
Write-Host "  IndyPOS services        : $($capture.Services.Count)"
Write-Host "  uninstall entries       : $($capture.UninstallEntries.Count)"

if ($versioned.Count -gt 0) {
    Write-Host ""
    Write-Host "  WARNING: found v*\ directories ($($versioned.RelativePath -join ', '))." -ForegroundColor Yellow
    Write-Host "  A v3-only till should have none. Was v4 already installed here?" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "This file lists real store paths. Do NOT commit it - IndyPOS is a public repo." -ForegroundColor Yellow
