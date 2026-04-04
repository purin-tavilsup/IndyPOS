<#
.SYNOPSIS
    Builds and publishes IndyPOS release binaries.

.DESCRIPTION
    Creates self-contained release builds for deployment to store machines.

    WHAT THIS SCRIPT DOES:
    ======================

    [1/3] Publishes StoreHub (Local API Service)
    ---------------------------------------------
    - Builds: src/IndyPOS.StoreHub
    - Output: <OutputDir>/StoreHub/
    - Mode: Self-contained (includes .NET runtime, no install needed)
    - Purpose: REST API that handles all data operations (products, sales, users)
    - Runs as: Windows Service on the store's main PC

    [2/3] Publishes WinForms (POS Terminal App)
    -------------------------------------------
    - Builds: src/IndyPOS.Windows.Forms
    - Output: <OutputDir>/WinForms/
    - Mode: Framework-dependent (requires .NET runtime installed)
    - Purpose: Desktop GUI for cashiers to process sales
    - Runs on: Each POS terminal (can be multiple per store)

    [3/3] Publishes MigrationTool (Data Migration)
    ----------------------------------------------
    - Builds: src/IndyPOS.MigrationTool
    - Output: <OutputDir>/Tools/
    - Mode: Self-contained, single-file executable
    - Purpose: One-time migration from SQLite to PostgreSQL
    - Runs: Once during initial deployment

    OUTPUT STRUCTURE:
    =================
    <OutputDir>/
      StoreHub/           -> Copy to C:\Program Files\IndyPOS\StoreHub\
        IndyPOS.StoreHub.exe
        appsettings.json
        appsettings.Production.json
        ...
      WinForms/           -> Copy to each POS terminal
        IndyPOS.Windows.Forms.exe
        appsettings.json
        ...
      Tools/              -> Run once for data migration
        IndyPOS.MigrationTool.exe

    AFTER RUNNING THIS SCRIPT:
    ==========================
    1. Copy StoreHub/ to store's main PC
    2. Copy WinForms/ to each POS terminal
    3. Run install-config.ps1 to set up PostgreSQL
    4. Run MigrationTool to migrate existing SQLite data

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

.EXAMPLE
    .\publish.ps1
    # Builds to ./publish with default settings

.EXAMPLE
    .\publish.ps1 -OutputDir "C:\Deploy\IndyPOS"
    # Builds to custom directory

.EXAMPLE
    .\publish.ps1 -Configuration Debug
    # Builds debug version for troubleshooting
#>

param(
    [string]$OutputDir = "$PSScriptRoot\..\publish",
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release"
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
Write-Host ""
Write-Host "Next steps:"
Write-Host "  1. Copy StoreHub\ to C:\Program Files\IndyPOS\StoreHub"
Write-Host "  2. Copy WinForms\ to POS terminal machines"
Write-Host "  3. Run install-config.ps1 to set up PostgreSQL"
Write-Host "  4. Use MigrationTool to migrate data from SQLite"
Write-Host ""
