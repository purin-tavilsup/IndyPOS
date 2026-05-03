<#
.SYNOPSIS
    Builds the IndyPOS full installer (bootstrapper).

.DESCRIPTION
    Creates IndyPOS-Setup.exe, a full installer that handles:
    - .NET 10 Runtime installation (if needed)
    - PostgreSQL 18 download and silent installation
    - StoreHub Windows Service setup
    - WinForms installation via Velopack
    - Database configuration

    PREREQUISITES:
    ==============
    Run publish.ps1 first to create the release binaries and Velopack packages.

    OUTPUT:
    =======
    <OutputDir>/
      IndyPOS-Setup.exe           -> Full installer for new stores
      IndyPOS-Setup.exe.config    -> Config file (if any)

    DISTRIBUTION:
    =============
    1. Copy IndyPOS-Setup.exe to store machine
    2. Run as Administrator
    3. Follow the installation wizard
    4. After installation, IndyPOS will auto-update via Velopack

.PARAMETER OutputDir
    Output directory for the bootstrapper.
    Default: ./publish (relative to repo root)

.PARAMETER Configuration
    Build configuration.
    Default: Release

.EXAMPLE
    .\build-installer.ps1
    # Builds installer to ./publish

.EXAMPLE
    .\build-installer.ps1 -OutputDir "C:\Deploy"
    # Builds to custom directory
#>

param(
    [string]$OutputDir = "$PSScriptRoot\..\publish",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

# Resolve paths
$RootDir = Resolve-Path "$PSScriptRoot\.."
$OutputDir = [System.IO.Path]::GetFullPath($OutputDir)
$BootstrapperProject = "$PSScriptRoot\IndyPOS.Bootstrapper\IndyPOS.Bootstrapper.csproj"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "IndyPOS Installer Build" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Root Directory: $RootDir"
Write-Host "Output Directory: $OutputDir"
Write-Host "Configuration: $Configuration"
Write-Host ""

# Verify publish was run first
$releasesDir = "$OutputDir\Releases"
if (-not (Test-Path $releasesDir)) {
    Write-Error "Releases directory not found at $releasesDir. Please run publish.ps1 first."
    exit 1
}

# Stage embedded payloads — bootstrapper csproj globs Resources\**\* as EmbeddedResource.
# Manifest names must match what the installers expect:
#   IndyPOS.Bootstrapper.Resources.IndyPOS.POS.v4-Setup.exe   (VelopackLauncher)
#   IndyPOS.Bootstrapper.Resources.StoreHub.zip               (StoreHubInstaller)
$resourcesDir = "$PSScriptRoot\IndyPOS.Bootstrapper\Resources"
$velopackPackId = "IndyPOS.POS.v4"

# vpk filenames vary by channel (e.g. {packId}-stable-Setup.exe). Glob then rename to
# the canonical name the bootstrapper looks up at runtime.
$velopackSetup = Get-ChildItem -Path $releasesDir -Filter "$velopackPackId*Setup.exe" -File `
    | Sort-Object LastWriteTime -Descending `
    | Select-Object -First 1
$storeHubZip = Get-ChildItem -Path $releasesDir -Filter "IndyPOS.StoreHub-*.zip" -File `
    | Sort-Object LastWriteTime -Descending `
    | Select-Object -First 1

if ($null -eq $velopackSetup) {
    Write-Error "Velopack setup not found in $releasesDir (pattern $velopackPackId*Setup.exe). Re-run publish.ps1."
    exit 1
}
if ($null -eq $storeHubZip) {
    Write-Error "StoreHub zip not found in $releasesDir (pattern IndyPOS.StoreHub-*.zip). Re-run publish.ps1."
    exit 1
}

Write-Host "Staging embedded payloads..." -ForegroundColor Green
if (Test-Path $resourcesDir) {
    Remove-Item -Path $resourcesDir -Recurse -Force
}
New-Item -ItemType Directory -Path $resourcesDir -Force | Out-Null

Copy-Item -Path $velopackSetup.FullName -Destination "$resourcesDir\$velopackPackId-Setup.exe" -Force
Copy-Item -Path $storeHubZip.FullName -Destination "$resourcesDir\StoreHub.zip" -Force

Write-Host "  Embedded: $velopackPackId-Setup.exe from $($velopackSetup.Name) ($([math]::Round($velopackSetup.Length / 1MB, 1)) MB)" -ForegroundColor Gray
Write-Host "  Embedded: StoreHub.zip from $($storeHubZip.Name) ($([math]::Round($storeHubZip.Length / 1MB, 1)) MB)" -ForegroundColor Gray

# Build and publish the bootstrapper
Write-Host ""
Write-Host "Building IndyPOS Bootstrapper..." -ForegroundColor Green

dotnet publish $BootstrapperProject `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -o "$OutputDir\Installer" `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to build bootstrapper"
    exit 1
}

# Rename to IndyPOS-Setup.exe
$bootstrapperExe = "$OutputDir\Installer\IndyPOS.Bootstrapper.exe"
$setupExe = "$OutputDir\IndyPOS-Setup.exe"

if (Test-Path $bootstrapperExe) {
    Copy-Item -Path $bootstrapperExe -Destination $setupExe -Force
    Write-Host "Created: $setupExe" -ForegroundColor Gray
}

# Clean up temp files
Remove-Item -Path "$OutputDir\Installer" -Recurse -Force -ErrorAction SilentlyContinue

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Installer Build Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Output:"
Write-Host "  $setupExe"
Write-Host ""
Write-Host "Distribution:"
Write-Host "  1. Copy IndyPOS-Setup.exe to store machine"
Write-Host "  2. Run as Administrator"
Write-Host "  3. Follow the installation wizard"
Write-Host ""
Write-Host "Note: The installer will download PostgreSQL during installation (~300MB)."
Write-Host "      Make sure the store has internet access."
Write-Host ""
