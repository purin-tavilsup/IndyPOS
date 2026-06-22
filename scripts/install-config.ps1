<#
.SYNOPSIS
    Configures PostgreSQL and system directories for IndyPOS StoreHub.

.DESCRIPTION
    One-time setup script for deploying IndyPOS on a store machine.
    Run this BEFORE copying binaries or running the migration tool.

    PREREQUISITES:
    ==============
    - PostgreSQL 18 installed (https://www.postgresql.org/download/windows/)
    - PostgreSQL service running
    - Know the postgres superuser password

    WHAT THIS SCRIPT DOES:
    ======================

    [1/5] Creates System Directories
    --------------------------------
    Creates the following directories if they don't exist:
      C:\ProgramData\IndyPOS\Config\   - Store configuration files
      C:\ProgramData\IndyPOS\keys\     - JWT signing keys (secured)
      C:\ProgramData\IndyPOS\logs\     - Application logs
      C:\ProgramData\IndyPOS\backups\  - Database backup files
      C:\Program Files\IndyPOS\StoreHub\ - StoreHub binaries

    [2/5] Sets Up PostgreSQL
    ------------------------
    Connects to PostgreSQL as superuser and:
      - Creates application user (default: indypos_app)
      - Creates database (default: indypos_storehub)
      - Grants all privileges to the app user
      - Enables uuid-ossp extension (for UUID generation)

    Why separate user? Security - the app user has limited permissions,
    only what's needed for the database. If compromised, damage is limited.

    [3/5] Generates JWT Secret Key
    ------------------------------
    Creates a secure random key for signing authentication tokens:
      - Size: 64 bytes (512 bits), Base64 encoded
      - Written into appsettings (no separate key file)

    Why? Users log into WinForms, which calls StoreHub API. The API
    returns a JWT token signed with this key. On subsequent requests,
    StoreHub validates the token using the same key.

    [4/5] Creates StoreHub Configuration
    ------------------------------------
    Generates appsettings.Production.json with actual values:
      - Database connection string (with password)
      - JWT secret key
      - Store ID
      - Cloud sync settings (disabled by default)

    Location: C:\Program Files\IndyPOS\StoreHub\appsettings.Production.json

    [5/5] Creates Store Configuration Template
    ------------------------------------------
    Creates StoreConfiguration.json with placeholder values:
      - Store name and address (for receipts)
      - Printer name
      - Barcode scanner device

    Location: C:\ProgramData\IndyPOS\Config\StoreConfiguration.json

    NOTE: You must edit this file with your actual store details!

    AFTER RUNNING THIS SCRIPT:
    ==========================
    1. Copy StoreHub binaries to C:\Program Files\IndyPOS\StoreHub\
    2. Edit C:\ProgramData\IndyPOS\Config\StoreConfiguration.json
    3. Run MigrationTool to migrate data from SQLite
    4. Install StoreHub as Windows Service (command shown at end)
    5. Start the service and verify health endpoint

.PARAMETER PgBin
    Path to PostgreSQL bin directory containing psql.exe.
    Default: C:\Program Files\PostgreSQL\18\bin
    Change if using different PostgreSQL version or install location.

.PARAMETER StoreId
    Unique identifier for this store (REQUIRED).
    Examples: STORE-001, STORE-002, BANGKOK-01
    Used for: Entity ID prefixes, cloud sync identification.
    Must be consistent across all systems.

.PARAMETER DbName
    PostgreSQL database name.
    Default: indypos_storehub
    No need to change unless running multiple instances.

.PARAMETER AppUser
    PostgreSQL application user name.
    Default: indypos_app
    This user owns the database and is used by StoreHub.

.PARAMETER PostgresPassword
    Password for the 'postgres' superuser (REQUIRED).
    This is the password you set when installing PostgreSQL.
    Used only during setup to create the database and user.

.PARAMETER AppUserPassword
    Password for the application user (REQUIRED).
    Choose a strong password - this is stored in appsettings.
    Used by StoreHub to connect to the database.

.EXAMPLE
    .\install-config.ps1 -StoreId "STORE-001" -PostgresPassword "pg_admin_pass" -AppUserPassword "app_secure_pass"
    # Basic usage with required parameters

.EXAMPLE
    .\install-config.ps1 `
        -PgBin "C:\Program Files\PostgreSQL\15\bin" `
        -StoreId "BANGKOK-01" `
        -DbName "indypos_bangkok" `
        -AppUser "indypos_bkk" `
        -PostgresPassword "pg_admin_pass" `
        -AppUserPassword "app_secure_pass"
    # Full customization for a specific store

.NOTES
    Run as Administrator for best results (creating Program Files directories).
    Script is idempotent - safe to run multiple times (skips existing resources).
#>

param(
    [string]$PgBin = "C:\Program Files\PostgreSQL\18\bin",
    [Parameter(Mandatory=$true)]
    [string]$StoreId,
    [string]$DbName = "indypos_storehub",
    [string]$AppUser = "indypos_app",
    [Parameter(Mandatory=$true)]
    [string]$PostgresPassword,
    [Parameter(Mandatory=$true)]
    [string]$AppUserPassword
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "IndyPOS Installation Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Store ID: $StoreId"
Write-Host "Database: $DbName"
Write-Host "App User: $AppUser"
Write-Host "PG Bin: $PgBin"
Write-Host ""

# Verify PostgreSQL bin directory
if (-not (Test-Path "$PgBin\psql.exe")) {
    Write-Error "PostgreSQL not found at $PgBin. Please install PostgreSQL 18 or specify -PgBin parameter."
    exit 1
}

# Set environment for psql
$env:PGPASSWORD = $PostgresPassword

# -----------------------------------
# Step 1: Create system directories
# -----------------------------------
Write-Host "[1/5] Creating system directories..." -ForegroundColor Green

$directories = @(
    "C:\ProgramData\IndyPOS\Config",
    "C:\ProgramData\IndyPOS\keys",
    "C:\ProgramData\IndyPOS\logs",
    "C:\ProgramData\IndyPOS\backups",
    "C:\Program Files\IndyPOS\StoreHub"
)

foreach ($dir in $directories) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
        Write-Host "  Created: $dir" -ForegroundColor Gray
    } else {
        Write-Host "  Exists: $dir" -ForegroundColor Gray
    }
}

# -----------------------------------
# Step 2: Create PostgreSQL database and user
# -----------------------------------
Write-Host ""
Write-Host "[2/5] Setting up PostgreSQL..." -ForegroundColor Green

# Check if user exists
$userExists = & "$PgBin\psql.exe" -h 127.0.0.1 -U postgres -tAc "SELECT 1 FROM pg_roles WHERE rolname='$AppUser'" 2>$null
if ($userExists -ne "1") {
    Write-Host "  Creating user: $AppUser" -ForegroundColor Gray
    & "$PgBin\psql.exe" -h 127.0.0.1 -U postgres -c "CREATE USER $AppUser WITH PASSWORD '$AppUserPassword';"
    if ($LASTEXITCODE -ne 0) { Write-Error "Failed to create user"; exit 1 }
} else {
    Write-Host "  User exists: $AppUser" -ForegroundColor Gray
}

# Check if database exists
$dbExists = & "$PgBin\psql.exe" -h 127.0.0.1 -U postgres -tAc "SELECT 1 FROM pg_database WHERE datname='$DbName'" 2>$null
if ($dbExists -ne "1") {
    Write-Host "  Creating database: $DbName" -ForegroundColor Gray
    & "$PgBin\psql.exe" -h 127.0.0.1 -U postgres -c "CREATE DATABASE $DbName OWNER $AppUser;"
    if ($LASTEXITCODE -ne 0) { Write-Error "Failed to create database"; exit 1 }
} else {
    Write-Host "  Database exists: $DbName" -ForegroundColor Gray
}

# Grant permissions and create extensions
Write-Host "  Granting permissions..." -ForegroundColor Gray
& "$PgBin\psql.exe" -h 127.0.0.1 -U postgres -d $DbName -c "GRANT ALL PRIVILEGES ON DATABASE $DbName TO $AppUser;"
& "$PgBin\psql.exe" -h 127.0.0.1 -U postgres -d $DbName -c "CREATE EXTENSION IF NOT EXISTS ""uuid-ossp"";"

# -----------------------------------
# Step 3: Generate JWT signing key
# -----------------------------------
Write-Host ""
Write-Host "[3/5] Generating JWT signing key..." -ForegroundColor Green

# Fresh 64-byte (512-bit) signing key, written directly into appsettings below.
# No longer persisted to a separate storehub.key file.
$bytes = New-Object byte[] 64
[System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
$jwtSecret = [Convert]::ToBase64String($bytes)
Write-Host "  Generated JWT signing key (64 bytes, Base64)" -ForegroundColor Gray

# -----------------------------------
# Step 4: Create StoreHub appsettings
# -----------------------------------
Write-Host ""
Write-Host "[4/5] Creating StoreHub configuration..." -ForegroundColor Green

$storeHubConfigPath = "C:\Program Files\IndyPOS\StoreHub\appsettings.Production.json"
$storeHubConfig = @{
    ConnectionStrings = @{
        "storehub-db" = "Host=127.0.0.1;Port=5432;Database=$DbName;Username=$AppUser;Password=$AppUserPassword"
    }
    LocalToken = @{
        SecretKey = $jwtSecret
        Issuer = "IndyPOS.StoreHub"
        Audience = "IndyPOS.POS"
        ExpiryHours = 12
    }
    StoreIdentity = @{
        StoreId = $StoreId
    }
    CloudApi = @{
        BaseUrl = ""
        ClientId = ""
        ClientSecret = ""
        Scopes = "sync.write master.read"
    }
    SyncWorker = @{
        Enabled = $false
    }
    Logging = @{
        LogLevel = @{
            Default = "Warning"
            "Microsoft.AspNetCore" = "Warning"
            "Microsoft.EntityFrameworkCore" = "Warning"
        }
    }
} | ConvertTo-Json -Depth 4

# Create directory if it doesn't exist
$storeHubDir = Split-Path $storeHubConfigPath
if (-not (Test-Path $storeHubDir)) {
    New-Item -ItemType Directory -Path $storeHubDir -Force | Out-Null
}

Set-Content -Path $storeHubConfigPath -Value $storeHubConfig
Write-Host "  Created: $storeHubConfigPath" -ForegroundColor Gray

# -----------------------------------
# Step 5: Create Store Configuration
# -----------------------------------
Write-Host ""
Write-Host "[5/5] Creating store configuration template..." -ForegroundColor Green

$storeConfigPath = "C:\ProgramData\IndyPOS\Config\StoreConfiguration.json"
if (-not (Test-Path $storeConfigPath)) {
    $storeConfig = @{
        StoreFullName = "My Store"
        StoreName = "My Store"
        StoreAddressLine1 = "123 Main Street"
        StoreAddressLine2 = "City 12345"
        StorePhoneNumber = "000-000-0000"
        PrinterName = ""
        BarcodeScannerDeviceName = ""
        SerialPortName = "COM1"
        Code = 1
    } | ConvertTo-Json -Depth 2

    Set-Content -Path $storeConfigPath -Value $storeConfig
    Write-Host "  Created: $storeConfigPath" -ForegroundColor Gray
    Write-Host "  NOTE: Please edit this file with your store details!" -ForegroundColor Yellow
} else {
    Write-Host "  Exists: $storeConfigPath" -ForegroundColor Gray
}

# Clear password from environment
$env:PGPASSWORD = $null

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Installation Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Database:"
Write-Host "  Host: 127.0.0.1:5432"
Write-Host "  Database: $DbName"
Write-Host "  User: $AppUser"
Write-Host ""
Write-Host "Configuration files:"
Write-Host "  StoreHub: $storeHubConfigPath"
Write-Host "  Store: $storeConfigPath"
Write-Host ""
Write-Host "Next steps:"
Write-Host "  1. Copy StoreHub binaries to C:\Program Files\IndyPOS\StoreHub\"
Write-Host "  2. Edit $storeConfigPath with your store details"
Write-Host "  3. Run MigrationTool to migrate data from SQLite"
Write-Host "  4. Install StoreHub as a Windows Service:"
Write-Host ""
Write-Host "     New-Service -Name 'IndyPOS.StoreHub' \" -ForegroundColor White
Write-Host "       -BinaryPathName 'C:\Program Files\IndyPOS\StoreHub\IndyPOS.StoreHub.exe' \" -ForegroundColor White
Write-Host "       -DisplayName 'IndyPOS StoreHub' \" -ForegroundColor White
Write-Host "       -StartupType Automatic" -ForegroundColor White
Write-Host ""
