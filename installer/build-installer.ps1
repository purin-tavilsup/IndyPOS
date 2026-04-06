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

# Build and publish the bootstrapper
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
