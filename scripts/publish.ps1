<#
.SYNOPSIS
    Builds and publishes IndyPOS release binaries with Velopack packaging.

.DESCRIPTION
    Creates self-contained release builds for deployment to store machines,
    including Velopack packages for auto-updates.

    WHAT THIS SCRIPT DOES:
    ======================

    [1/4] Publishes StoreHub (Local API Service)
    ---------------------------------------------
    - Builds: src/IndyPOS.StoreHub
    - Output: <OutputDir>/StoreHub/
    - Mode: Self-contained (includes .NET runtime, no install needed)
    - Purpose: REST API that handles all data operations (products, sales, users)
    - Runs as: Windows Service on the store's main PC

    [2/4] Publishes WinForms (POS Terminal App)
    -------------------------------------------
    - Builds: src/IndyPOS.Windows.Forms
    - Output: <OutputDir>/WinForms/
    - Mode: Framework-dependent (requires .NET runtime installed)
    - Purpose: Desktop GUI for cashiers to process sales
    - Runs on: Each POS terminal (can be multiple per store)

    [3/4] Publishes MigrationTool (Data Migration)
    ----------------------------------------------
    - Builds: src/IndyPOS.MigrationTool
    - Output: <OutputDir>/Tools/
    - Mode: Self-contained, single-file executable
    - Purpose: One-time migration from SQLite to PostgreSQL
    - Runs: Once during initial deployment

    [4/4] Creates Velopack Package (Auto-Updates)
    ---------------------------------------------
    - Input: <OutputDir>/WinForms/
    - Output: <OutputDir>/Releases/
    - Creates: IndyPOS-Setup.exe, .nupkg files, releases.json
    - Purpose: Enable auto-updates via GitHub Releases

    OUTPUT STRUCTURE:
    =================
    <OutputDir>/
      StoreHub/           -> For Windows Service deployment
        IndyPOS.StoreHub.exe
        appsettings.json
        ...
      WinForms/           -> Raw WinForms binaries (for Velopack)
        IndyPOS.Windows.Forms.exe
        appsettings.json
        ...
      Tools/              -> Migration tool
        IndyPOS.MigrationTool.exe
      Releases/           -> Velopack packages for GitHub Releases
        IndyPOS-Setup.exe         -> Installer for new installs
        IndyPOS.POS-x.x.x-full.nupkg  -> Full update package
        releases.json             -> Velopack index file

    AFTER RUNNING THIS SCRIPT:
    ==========================
    1. Upload contents of Releases/ to GitHub Releases
    2. Run installer/build-installer.ps1 to create full bootstrapper
    3. Distribute IndyPOS-Setup.exe to new stores

.PARAMETER OutputDir
    Output directory for published binaries.
    Default: ./publish (relative to repo root)

.PARAMETER Runtime
    Target runtime identifier.
    Default: win-x64 (64-bit Windows)
    Other options: win-x86, win-arm64

.PARAMETER Configuration
    Build configuration.
    Default: Release (optimized, no debug symbols)
    Other option: Debug (for troubleshooting)

.PARAMETER Version
    Version number for the release.
    Default: Auto-detected from AssemblyVersion

.PARAMETER SkipVelopack
    Skip Velopack packaging (for quick local builds).

.EXAMPLE
    .\publish.ps1
    # Builds to ./publish with default settings

.EXAMPLE
    .\publish.ps1 -OutputDir "C:\Deploy\IndyPOS" -Version "1.2.0"
    # Builds to custom directory with specific version

.EXAMPLE
    .\publish.ps1 -SkipVelopack
    # Skip Velopack packaging for faster builds
#>

param(
    [string]$OutputDir = "$PSScriptRoot\..\publish",
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release",
    [string]$Version = "",
    [switch]$SkipVelopack
)

$ErrorActionPreference = "Stop"

# Resolve paths
$RootDir = Resolve-Path "$PSScriptRoot\.."
$OutputDir = [System.IO.Path]::GetFullPath($OutputDir)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "IndyPOS Publish Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Root Directory: $RootDir"
Write-Host "Output Directory: $OutputDir"
Write-Host "Runtime: $Runtime"
Write-Host "Configuration: $Configuration"
Write-Host ""

# Clean output directory
if (Test-Path $OutputDir) {
    Write-Host "Cleaning output directory..." -ForegroundColor Yellow
    Remove-Item -Path $OutputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

# Build and publish StoreHub
Write-Host ""
Write-Host "[1/3] Publishing StoreHub..." -ForegroundColor Green
$storeHubProject = "$RootDir\src\IndyPOS.StoreHub\IndyPOS.StoreHub.csproj"
$storeHubOutput = "$OutputDir\StoreHub"

dotnet publish $storeHubProject `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -o $storeHubOutput `
    /p:PublishSingleFile=false `
    /p:IncludeNativeLibrariesForSelfExtract=true

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to publish StoreHub"
    exit 1
}
Write-Host "StoreHub published to: $storeHubOutput" -ForegroundColor Gray

# Build and publish WinForms
Write-Host ""
Write-Host "[2/3] Publishing WinForms..." -ForegroundColor Green
$winFormsProject = "$RootDir\src\IndyPOS.Windows.Forms\IndyPOS.Windows.Forms.csproj"
$winFormsOutput = "$OutputDir\WinForms"

dotnet publish $winFormsProject `
    -c $Configuration `
    -r $Runtime `
    -o $winFormsOutput `
    /p:PublishSingleFile=false

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to publish WinForms"
    exit 1
}
Write-Host "WinForms published to: $winFormsOutput" -ForegroundColor Gray

# Build and publish MigrationTool
Write-Host ""
Write-Host "[3/3] Publishing MigrationTool..." -ForegroundColor Green
$migrationProject = "$RootDir\src\IndyPOS.MigrationTool\IndyPOS.MigrationTool.csproj"
$migrationOutput = "$OutputDir\Tools"

dotnet publish $migrationProject `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -o $migrationOutput `
    /p:PublishSingleFile=true

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to publish MigrationTool"
    exit 1
}
Write-Host "MigrationTool published to: $migrationOutput" -ForegroundColor Gray

# Create Velopack package (unless skipped)
if (-not $SkipVelopack) {
    Write-Host ""
    Write-Host "[4/4] Creating Velopack package..." -ForegroundColor Green

    # Detect version if not specified
    if ([string]::IsNullOrEmpty($Version)) {
        $exePath = "$winFormsOutput\IndyPOS.Windows.Forms.exe"
        if (Test-Path $exePath) {
            $versionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($exePath)
            $Version = $versionInfo.ProductVersion -replace '\+.*$', ''  # Remove build metadata
        } else {
            $Version = "1.0.0"
        }
    }
    Write-Host "Version: $Version" -ForegroundColor Gray

    $releasesOutput = "$OutputDir\Releases"
    New-Item -ItemType Directory -Path $releasesOutput -Force | Out-Null

    # Check if vpk is installed
    $vpkPath = Get-Command vpk -ErrorAction SilentlyContinue
    if ($null -eq $vpkPath) {
        Write-Host "Installing Velopack CLI tool..." -ForegroundColor Yellow
        dotnet tool install -g vpk
    }

    # Create Velopack package — packId is versioned ("v4") so v4 installs
    # never collide with the legacy v3.7.0 footprint on the same machine.
    $packId = "IndyPOS.POS.v4"

    vpk pack `
        --packId $packId `
        --packVersion $Version `
        --packDir $winFormsOutput `
        --mainExe "IndyPOS.Windows.Forms.exe" `
        --outputDir $releasesOutput `
        --channel stable

    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Velopack packaging failed. You can retry with: vpk pack --packId $packId --packVersion $Version --packDir $winFormsOutput --mainExe IndyPOS.Windows.Forms.exe --outputDir $releasesOutput"
    } else {
        Write-Host "Velopack package created at: $releasesOutput" -ForegroundColor Gray
    }

    # Create StoreHub zip for update distribution
    Write-Host ""
    Write-Host "Creating StoreHub update package..." -ForegroundColor Green
    $storeHubZip = "$releasesOutput\IndyPOS.StoreHub-$Version.zip"
    Compress-Archive -Path "$storeHubOutput\*" -DestinationPath $storeHubZip -Force
    Write-Host "StoreHub package: $storeHubZip" -ForegroundColor Gray
}

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Publish Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Output structure:"
Write-Host "  $OutputDir\"
Write-Host "    StoreHub\     - Local API service (run as Windows Service)"
Write-Host "    WinForms\     - POS terminal application"
Write-Host "    Tools\        - Migration tool (SQLite -> PostgreSQL)"
if (-not $SkipVelopack) {
    Write-Host "    Releases\     - Velopack packages for GitHub Releases"
}
Write-Host ""
Write-Host "Next steps:"
if (-not $SkipVelopack) {
    Write-Host "  1. Upload contents of Releases\ to GitHub Releases"
    Write-Host "  2. Run installer\build-installer.ps1 to create full bootstrapper"
    Write-Host "  3. Distribute IndyPOS-Setup.exe to new stores"
} else {
    Write-Host "  1. Copy StoreHub\ to C:\Program Files\IndyPOS\StoreHub"
    Write-Host "  2. Copy WinForms\ to POS terminal machines"
    Write-Host "  3. Run install-config.ps1 to set up PostgreSQL"
    Write-Host "  4. Use MigrationTool to migrate data from SQLite"
}
Write-Host ""
