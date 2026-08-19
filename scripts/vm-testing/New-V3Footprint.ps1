<#
.SYNOPSIS
    Replays a captured v3.7.0 footprint onto a test machine.

.DESCRIPTION
    Consumes the JSON produced by Get-V3Footprint.ps1 and recreates the directory
    and file layout under C:\ProgramData\IndyPOS, so v4 can be installed over a
    realistic v3 footprint and verify-install.ps1's side-by-side section can assert
    coexistence against something real instead of skipping.

    File CONTENTS are not captured and are not reproduced — files are created at
    their recorded length (zero-filled). That is enough for the question being asked,
    which is whether the v4 install leaves the v3 footprint alone. It is NOT a working
    v3 install and will not launch.

    Services and uninstall entries are NOT replayed. A real v3.7.0 has neither a
    Windows service nor Postgres, so recreating them would model a machine that does
    not exist. The script warns if the capture contains any.

.PARAMETER CapturePath
    The JSON written by Get-V3Footprint.ps1.

.PARAMETER MaxFileBytes
    Files longer than this are created truncated, and listed at the end. Keeps a
    multi-hundred-MB Store.db from being materialised for no benefit.

.PARAMETER Force
    Required when the target root already has a v*\ install root. See the safety guard below.

.PARAMETER TargetRoot
    Where to replay the footprint. Defaults to the real C:\ProgramData\IndyPOS.
    Overriding it is how this script gets tested without writing to a live layout.

.EXAMPLE
    .\New-V3Footprint.ps1 -CapturePath .\v3-footprint-GeneralHardware.json

.EXAMPLE
    .\New-V3Footprint.ps1 -CapturePath .\capture.json -TargetRoot $env:TEMP\fp
    # Dry run into a throwaway directory.

.NOTES
    SAFETY: refuses to run on a machine that already has a v*\ install root unless
    -Force is given, so it cannot be run against a real till by accident.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$CapturePath,
    [string]$TargetRoot = "C:\ProgramData\IndyPOS",
    [long]$MaxFileBytes = 4MB,
    [switch]$Force
)

$ErrorActionPreference = "Stop"

$ProgramDataRoot = $TargetRoot

if (-not (Test-Path -LiteralPath $CapturePath)) {
    throw "Capture not found: $CapturePath"
}

$capture = Get-Content -LiteralPath $CapturePath -Raw | ConvertFrom-Json

# --- Safety guard -------------------------------------------------------------
# A real till is exactly the machine we must never write to. The tell is an
# existing versioned install root, so treat that as a stop unless overridden.
$existingVersioned = @()
if (Test-Path $ProgramDataRoot) {
    $existingVersioned = @(Get-ChildItem $ProgramDataRoot -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -match '^v\d+(\.\d+)*$' })
}

if ($existingVersioned.Count -gt 0 -and -not $Force) {
    Write-Host ""
    Write-Host "REFUSING to run." -ForegroundColor Red
    Write-Host "  $ProgramDataRoot already contains: $($existingVersioned.Name -join ', ')"
    Write-Host "  That looks like a real install, not a clean test VM."
    Write-Host "  Re-run with -Force only if you are certain this is a throwaway machine."
    exit 2
}

# --- Replay -------------------------------------------------------------------
$sep = [char]92
$made = [ordered]@{ Directories = 0; Files = 0 }
$truncated = New-Object System.Collections.Generic.List[string]

# Directories first, deepest last, so a file never precedes its parent.
foreach ($entry in ($capture.ProgramDataTree | Where-Object { $_.IsDirectory } |
                    Sort-Object { $_.RelativePath.Split($sep).Count })) {

    $target = Join-Path $ProgramDataRoot $entry.RelativePath

    if (-not (Test-Path -LiteralPath $target)) {
        $null = New-Item -ItemType Directory -Path $target -Force
        $made.Directories++
    }
}

foreach ($entry in ($capture.ProgramDataTree | Where-Object { -not $_.IsDirectory })) {
    $target = Join-Path $ProgramDataRoot $entry.RelativePath
    $parent = Split-Path $target -Parent

    if (-not (Test-Path -LiteralPath $parent)) {
        $null = New-Item -ItemType Directory -Path $parent -Force
        $made.Directories++
    }

    if (Test-Path -LiteralPath $target) { continue }

    $length = [long]($entry.SizeBytes ?? 0)

    if ($length -gt $MaxFileBytes) {
        $truncated.Add("$($entry.RelativePath) ($length bytes -> $MaxFileBytes)")
        $length = $MaxFileBytes
    }

    # SetLength on a new file gives a zero-filled file of the right size without
    # allocating a buffer for it.
    $stream = [System.IO.File]::Create($target)
    try { $stream.SetLength($length) } finally { $stream.Dispose() }

    [System.IO.File]::SetLastWriteTimeUtc($target, [datetime]::Parse($entry.LastWriteUtc).ToUniversalTime())
    $made.Files++
}

Write-Host ""
Write-Host "Replayed footprint '$($capture.StoreLabel)' captured $($capture.CapturedUtc)" -ForegroundColor Green
Write-Host "  directories created : $($made.Directories)"
Write-Host "  files created       : $($made.Files)"

if ($truncated.Count -gt 0) {
    Write-Host ""
    Write-Host "  $($truncated.Count) file(s) created truncated (contents are not part of the capture):" -ForegroundColor Yellow
    $truncated | ForEach-Object { Write-Host "    $_" -ForegroundColor Yellow }
}

if ($capture.Services.Count -gt 0) {
    Write-Host ""
    Write-Host "  NOT replayed: $($capture.Services.Count) IndyPOS service(s) in the capture" -ForegroundColor Yellow
    Write-Host "  ($($capture.Services.Name -join ', ')). A real v3.7.0 has no Windows service," -ForegroundColor Yellow
    Write-Host "  so the capture may have come from a machine that already had v4." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Next: install v4, then run scripts\verify-install.ps1 and read section 7." -ForegroundColor Cyan
Write-Host "It must now report the v3-era entries rather than Skip." -ForegroundColor Cyan
