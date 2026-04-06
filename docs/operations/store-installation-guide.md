# IndyPOS Local Deployment Guide

This guide walks you through deploying IndyPOS on a local store machine.

---

## Quick Start (Automated Installer) 🚀

The easiest way to deploy IndyPOS is using the automated installer:

```powershell
# 1. Download IndyPOS-Setup.exe from GitHub Releases
# 2. Run as Administrator
.\IndyPOS-Setup.exe
```

The installer automatically handles:
- ✅ .NET 10 Runtime installation
- ✅ PostgreSQL 18 download & silent install
- ✅ StoreHub Windows Service setup
- ✅ Database configuration
- ✅ WinForms installation (with auto-updates via Velopack)
- ✅ First-run wizard for store configuration

**After installation:**
- WinForms auto-updates via GitHub Releases
- StoreHub updates can be managed from WinForms Settings

For manual installation or troubleshooting, follow the detailed steps below.

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Prerequisites](#prerequisites)
4. [Step 1: Install PostgreSQL](#step-1-install-postgresql)
5. [Step 2: Build Release Binaries](#step-2-build-release-binaries)
6. [Step 3: Configure the System](#step-3-configure-the-system)
7. [Step 4: Install StoreHub Service](#step-4-install-storehub-service)
8. [Step 5: Migrate Data](#step-5-migrate-data)
9. [Step 6: Configure WinForms](#step-6-configure-winforms)
10. [Step 7: Verify Installation](#step-7-verify-installation)
11. [Step 8: Set Up Backups](#step-8-set-up-backups)
12. [Multi-Terminal Setup](#multi-terminal-setup)
13. [Troubleshooting](#troubleshooting)

---

## Overview

IndyPOS consists of three main components:

| Component | Description | Runs On |
|-----------|-------------|---------|
| **StoreHub** | Local API service (REST) | Store's main PC (as Windows Service) |
| **WinForms** | POS terminal application | Each POS terminal |
| **PostgreSQL** | Database | Store's main PC |

**Data Flow:**
```
┌─────────────┐      HTTP       ┌─────────────┐      SQL       ┌─────────────┐
│  WinForms   │ ──────────────▶ │  StoreHub   │ ─────────────▶ │ PostgreSQL  │
│  (POS App)  │                 │  (API)      │                │  (Database) │
└─────────────┘                 └─────────────┘                └─────────────┘
     Terminal 1                   Main Store PC                  Main Store PC
     Terminal 2 ───────────────────────┘
```

---

## Architecture

### Single-Machine Deployment (Typical)

All components run on one machine:

```
┌──────────────────────────────────────────────────────────────┐
│                     Store Hub PC                             │
│                                                              │
│  ┌─────────────┐   ┌─────────────┐   ┌─────────────────────┐ │
│  │  WinForms   │──▶│  StoreHub   │──▶│     PostgreSQL      │ │
│  │  (Desktop)  │   │  (Service)  │   │     (Database)      │ │
│  └─────────────┘   └─────────────┘   └─────────────────────┘ │
│                           │                                  │
│                     Port 5000                                │
└──────────────────────────────────────────────────────────────┘
```

### Multi-Terminal Deployment

One Store Hub PC, multiple POS terminals:

```
┌─────────────┐
│ Terminal 1  │──┐
│  WinForms   │  │
└─────────────┘  │       ┌──────────────────────────────────────┐
                 │       │           Store Hub PC               │
┌─────────────┐  │  LAN  │  ┌─────────────┐  ┌───────────────┐  │
│ Terminal 2  │──┼───────┼─▶│  StoreHub   │─▶│  PostgreSQL   │  │
│  WinForms   │  │       │  │  (Service)  │  │  (Database)   │  │
└─────────────┘  │       │  └─────────────┘  └───────────────┘  │
                 │       │        │                             │
┌─────────────┐  │       │   Port 5000                          │
│ Terminal 3  │──┘       └──────────────────────────────────────┘
│  WinForms   │
└─────────────┘
```

---

## Prerequisites

### Hardware Requirements

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| CPU | 2 cores | 4 cores |
| RAM | 4 GB | 8 GB |
| Disk | 50 GB SSD | 100 GB SSD |
| Network | 100 Mbps LAN | Gigabit LAN |

### Software Requirements

| Software | Version | Download |
|----------|---------|----------|
| Windows | 10/11 (64-bit) | - |
| .NET Runtime | 10.0 | [Download](https://dotnet.microsoft.com/download/dotnet/10.0) |
| PostgreSQL | 18.x | [Download](https://www.postgresql.org/download/windows/) |

### Network Requirements

- Store Hub PC needs a **static IP address** (for multi-terminal setup)
- Firewall must allow port **5000** (StoreHub API) and **5432** (PostgreSQL)
- All terminals must be on the same LAN

---

## Step 1: Install PostgreSQL

### 1.1 Download and Install

1. Download PostgreSQL 18 from https://www.postgresql.org/download/windows/
2. Run the installer as Administrator
3. During installation:
   - Remember the **postgres** superuser password
   - Keep default port **5432**
   - Enable **pgAdmin** (optional, for database management)

### 1.2 Verify Installation

Open PowerShell and run:

```powershell
# Check PostgreSQL service is running
Get-Service postgresql*

# Expected output:
# Status   Name               DisplayName
# ------   ----               -----------
# Running  postgresql-x64-18  postgresql-x64-18
```

### 1.3 Test Connection

```powershell
# Connect to PostgreSQL
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -c "SELECT version();"
# Enter the postgres password when prompted
```

---

## Step 2: Build Release Binaries

On your development machine (or download pre-built binaries):

```powershell
# Clone the repository (if not already)
git clone https://github.com/your-org/IndyPOS.git
cd IndyPOS

# Build release binaries
.\scripts\publish.ps1
```

This creates:
```
publish/
  StoreHub/     # API service binaries
  WinForms/     # POS application binaries
  Tools/        # Migration tool
```

Copy the `publish/` folder to the store machine (USB drive, network share, etc.).

---

## Step 3: Configure the System

Run the configuration script on the store machine:

```powershell
# Open PowerShell as Administrator
cd C:\path\to\publish

# Run configuration script
.\scripts\install-config.ps1 `
    -StoreId "STORE-001" `
    -PostgresPassword "your_postgres_password" `
    -AppUserPassword "choose_a_strong_password"
```

### What This Script Does

1. **Creates directories:**
   - `C:\ProgramData\IndyPOS\Config\` - Configuration files
   - `C:\ProgramData\IndyPOS\keys\` - JWT signing key
   - `C:\ProgramData\IndyPOS\logs\` - Application logs
   - `C:\ProgramData\IndyPOS\backups\` - Database backups
   - `C:\Program Files\IndyPOS\StoreHub\` - Service binaries

2. **Sets up PostgreSQL:**
   - Creates database: `indypos_storehub`
   - Creates user: `indypos_app`
   - Grants permissions

3. **Generates JWT key:**
   - Secure random key for authentication
   - Saved to `C:\ProgramData\IndyPOS\keys\storehub.key`

4. **Creates configuration files:**
   - `C:\Program Files\IndyPOS\StoreHub\appsettings.Production.json`
   - `C:\ProgramData\IndyPOS\Config\StoreConfiguration.json`

### 3.1 Edit Store Configuration

Open `C:\ProgramData\IndyPOS\Config\StoreConfiguration.json` and update:

```json
{
  "StoreFullName": "My Store Name",
  "StoreName": "My Store",
  "StoreAddressLine1": "123 Main Street",
  "StoreAddressLine2": "Bangkok 10110",
  "StorePhoneNumber": "02-123-4567",
  "PrinterName": "XP-58",
  "BarcodeScannerDeviceName": "",
  "SerialPortName": "COM1",
  "Code": 1
}
```

---

## Step 4: Install StoreHub Service

### 4.1 Copy Binaries

```powershell
# Copy StoreHub to Program Files
Copy-Item -Path ".\publish\StoreHub\*" -Destination "C:\Program Files\IndyPOS\StoreHub\" -Recurse -Force
```

### 4.2 Install as Windows Service

```powershell
# Create the service
New-Service -Name "IndyPOS.StoreHub" `
    -BinaryPathName "C:\Program Files\IndyPOS\StoreHub\IndyPOS.StoreHub.exe" `
    -DisplayName "IndyPOS StoreHub" `
    -Description "IndyPOS local API service" `
    -StartupType Automatic

# Start the service
Start-Service -Name "IndyPOS.StoreHub"

# Verify it's running
Get-Service -Name "IndyPOS.StoreHub"
```

### 4.3 Verify Health Endpoints

```powershell
# Check service is alive
Invoke-RestMethod -Uri "http://localhost:5000/health/live"

# Check database connection
Invoke-RestMethod -Uri "http://localhost:5000/health/ready"

# Check version
Invoke-RestMethod -Uri "http://localhost:5000/version"
```

Expected output:
```json
{"status":"healthy","database":"connected"}
{"version":"1.0.0","assemblyVersion":"1.0.0.0",...}
```

---

## Step 5: Migrate Data

If you have existing data in SQLite, migrate it to PostgreSQL:

### 5.1 Backup SQLite Database

```powershell
# Create backup
Copy-Item "C:\ProgramData\IndyPOS\db\Store.db" "C:\ProgramData\IndyPOS\backups\Store.db.backup"
```

### 5.2 Run Dry Run First

```powershell
cd "C:\Program Files\IndyPOS\StoreHub"

.\IndyPOS.MigrationTool.exe `
    --sqlite "C:\ProgramData\IndyPOS\db\Store.db" `
    --postgres "Host=127.0.0.1;Database=indypos_storehub;Username=indypos_app;Password=YOUR_APP_PASSWORD" `
    --store-id "STORE-001" `
    --dry-run
```

Review the output for any errors.

### 5.3 Execute Migration

```powershell
.\IndyPOS.MigrationTool.exe `
    --sqlite "C:\ProgramData\IndyPOS\db\Store.db" `
    --postgres "Host=127.0.0.1;Database=indypos_storehub;Username=indypos_app;Password=YOUR_APP_PASSWORD" `
    --store-id "STORE-001"
```

### 5.4 Verify Migration

```powershell
.\IndyPOS.MigrationTool.exe verify `
    --sqlite "C:\ProgramData\IndyPOS\db\Store.db" `
    --postgres "Host=127.0.0.1;Database=indypos_storehub;Username=indypos_app;Password=YOUR_APP_PASSWORD" `
    --store-id "STORE-001"
```

---

## Step 6: Configure WinForms

### 6.1 Copy WinForms Binaries

Copy `publish\WinForms\` to each POS terminal:
- Same machine: `C:\Program Files\IndyPOS\WinForms\`
- Other terminals: Any location (e.g., `C:\IndyPOS\`)

### 6.2 Edit appsettings.json

Open `appsettings.json` in the WinForms folder:

```json
{
  "Report": {
    "Directory": "C:\\ProgramData\\IndyPOS\\Reports"
  },
  "Store": {
    "ConfigPath": "C:\\ProgramData\\IndyPOS\\Config\\StoreConfiguration.json"
  },
  "StoreHub": {
    "BaseUrl": "http://localhost:5000",
    "TimeoutSeconds": 30,
    "AutoSyncProductsOnStartup": true
  }
}
```

**For remote terminals**, change `BaseUrl` to the Store Hub PC's IP:

```json
"StoreHub": {
  "BaseUrl": "http://192.168.1.100:5000",
  ...
}
```

### 6.3 Create Desktop Shortcut

Create a shortcut to `IndyPOS.Windows.Forms.exe` on the desktop for easy access.

---

## Step 7: Verify Installation

### 7.1 Run Smoke Tests

```powershell
.\scripts\smoke-test.ps1 -BaseUrl "http://localhost:5000"
```

All tests should pass.

### 7.2 Manual Testing

1. **Launch WinForms** - Double-click the application
2. **Login** - Use migrated credentials (or seeded test users in dev)
3. **Search product** - Verify products load
4. **Complete a sale** - Add item, complete cash payment
5. **Check reports** - Verify sale appears in reports
6. **Print receipt** (if printer configured)

---

## Step 8: Set Up Backups

### 8.1 Create Backup Script

The backup script is included in `docs/operations/backup.ps1`.

### 8.2 Schedule Automatic Backups

```powershell
# Create scheduled task for backups every 4 hours
$action = New-ScheduledTaskAction -Execute "powershell.exe" `
    -Argument '-ExecutionPolicy Bypass -File "C:\ProgramData\IndyPOS\ops\backup.ps1"'

$trigger = New-ScheduledTaskTrigger -Once -At (Get-Date) `
    -RepetitionInterval (New-TimeSpan -Hours 4)

$principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount

Register-ScheduledTask -TaskName "IndyPOS-Backup" `
    -Action $action -Trigger $trigger -Principal $principal
```

### 8.3 Test Backup

```powershell
.\backup.ps1 -PgBin "C:\Program Files\PostgreSQL\18\bin" `
    -DbName "indypos_storehub" `
    -DbUser "indypos_app" `
    -DbPassword "YOUR_APP_PASSWORD" `
    -BackupDir "C:\ProgramData\IndyPOS\backups"
```

---

## Multi-Terminal Setup

### Store Hub PC Configuration

1. Assign a **static IP address** (e.g., `192.168.1.100`)
2. Open **Windows Firewall** for port 5000:

```powershell
New-NetFirewallRule -DisplayName "IndyPOS StoreHub" `
    -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow
```

### Terminal Configuration

On each POS terminal:

1. Copy WinForms binaries
2. Edit `appsettings.json`:

```json
{
  "StoreHub": {
    "BaseUrl": "http://192.168.1.100:5000",
    "TimeoutSeconds": 30,
    "AutoSyncProductsOnStartup": true
  }
}
```

3. Test connection:

```powershell
Invoke-RestMethod -Uri "http://192.168.1.100:5000/health/live"
```

---

## Troubleshooting

### StoreHub Service Won't Start

**Check service status:**
```powershell
Get-Service IndyPOS.StoreHub
Get-EventLog -LogName Application -Source "IndyPOS*" -Newest 10
```

**Check logs:**
```powershell
Get-Content "C:\ProgramData\IndyPOS\logs\storehub-*.log" -Tail 50
```

**Common causes:**
- PostgreSQL not running
- Incorrect connection string in appsettings.Production.json
- Port 5000 already in use

### WinForms Can't Connect to StoreHub

**Test connectivity:**
```powershell
Test-NetConnection -ComputerName 192.168.1.100 -Port 5000
```

**Common causes:**
- StoreHub service not running
- Firewall blocking port 5000
- Wrong IP address in appsettings.json

### Database Connection Failed

**Test PostgreSQL:**
```powershell
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" `
    -h 127.0.0.1 -U indypos_app -d indypos_storehub -c "SELECT 1"
```

**Common causes:**
- PostgreSQL service not running
- Wrong password in connection string
- Database doesn't exist

### Products Not Loading

**Check API:**
```powershell
# Login first
$login = Invoke-RestMethod -Uri "http://localhost:5000/auth/login" `
    -Method POST -ContentType "application/json" `
    -Body '{"username":"admin","password":"admin123"}'

# Get products
Invoke-RestMethod -Uri "http://localhost:5000/products" `
    -Headers @{Authorization="Bearer $($login.token)"}
```

**Check database has data:**
```powershell
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" `
    -h 127.0.0.1 -U indypos_app -d indypos_storehub `
    -c "SELECT COUNT(*) FROM products"
```

---

## Quick Reference

### Service Commands

```powershell
# Start service
Start-Service IndyPOS.StoreHub

# Stop service
Stop-Service IndyPOS.StoreHub

# Restart service
Restart-Service IndyPOS.StoreHub

# Check status
Get-Service IndyPOS.StoreHub
```

### Health Check URLs

| Endpoint | Purpose |
|----------|---------|
| `http://localhost:5000/health/live` | Service is running |
| `http://localhost:5000/health/ready` | Database connected |
| `http://localhost:5000/version` | Version info |

### File Locations

| File | Location |
|------|----------|
| StoreHub binaries | `C:\Program Files\IndyPOS\StoreHub\` |
| StoreHub config | `C:\Program Files\IndyPOS\StoreHub\appsettings.Production.json` |
| Store config | `C:\ProgramData\IndyPOS\Config\StoreConfiguration.json` |
| JWT key | `C:\ProgramData\IndyPOS\keys\storehub.key` |
| Logs | `C:\ProgramData\IndyPOS\logs\` |
| Backups | `C:\ProgramData\IndyPOS\backups\` |

---

## Next Steps

After successful installation:

1. **Train staff** on using the new system
2. **Run parallel operations** (old + new) for a few days
3. **Monitor logs** for any errors
4. **Set up cloud sync** when CloudApi is ready (Epic I)

For deployment checklist, see [pilot-checklist.md](pilot-checklist.md).

For troubleshooting, see [troubleshooting-guide.md](troubleshooting-guide.md).
