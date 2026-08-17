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
- ✅ Admin credential generation (see [Admin Bootstrap Credentials](#admin-bootstrap-credentials) below)

**After installation:**
- WinForms auto-updates via GitHub Releases
- StoreHub updates can be managed from WinForms Settings
- Admin must change one-time password on first sign-in

For manual installation or troubleshooting, follow the detailed steps below.

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Prerequisites](#prerequisites)
4. [Admin Bootstrap Credentials](#admin-bootstrap-credentials)
5. [Step 1: Install PostgreSQL](#step-1-install-postgresql)
6. [Step 2: Build Release Binaries](#step-2-build-release-binaries)
7. [Step 3: Configure the System](#step-3-configure-the-system)
8. [Step 4: Install StoreHub Service](#step-4-install-storehub-service)
9. [Step 5: Migrate Data](#step-5-migrate-data)
10. [Step 6: Configure WinForms](#step-6-configure-winforms)
11. [Step 7: Verify Installation](#step-7-verify-installation)
12. [Step 8: Set Up Backups](#step-8-set-up-backups)
13. [Multi-Terminal Setup](#multi-terminal-setup)
14. [Troubleshooting](#troubleshooting)

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

## Admin Bootstrap Credentials

The installer automatically creates an admin account and generates a one-time password. This section explains the bootstrap flow and how to recover credentials if needed.

### Initial Setup (First Installation)

1. **Installer Wizard** - The wizard asks for the **Store ID only**. No admin username or password is requested.

2. **Finish Screen** - At the end of installation, the finish screen displays:
   - **Username:** `admin`
   - **One-time Password:** A random 14-character password (e.g., `xK7fQm2Vd9Pz3H`)
   - A note to save this password and delete `admin-credentials.txt` after first sign-in

3. **Credential File** - The credentials are also written to:
   ```
   C:\ProgramData\IndyPOS\v4\Config\admin-credentials.txt
   ```
   This file contains the username and one-time password for reference.

### First Sign-In (Force Password Change)

When `admin` logs in with the one-time password:
- The login token carries a `must_change` marker, and **StoreHub (the server) enforces it** — every request except the change-password call is rejected while the marker is set
- The WinForms app shows the change-password prompt as the UX for this, but enforcement happens server-side, so a dismissed dialog or a different client cannot bypass it
- Admin must enter a new, permanent password
- The one-time password expires and cannot be used again

**After first sign-in, delete the credential file:**
```powershell
Remove-Item "C:\ProgramData\IndyPOS\v4\Config\admin-credentials.txt"
```

### Reinstalling Over Existing Database

If you run the installer on a machine with an existing IndyPOS database:
- The installer **retains the existing admin** account
- **No new credential is generated**
- The finish screen indicates that the existing admin was kept
- Use the current admin password to sign in

### Admin Credential Recovery

If the one-time password is lost or the admin needs to reset it, use the `reset-admin` command:

```powershell
"%ProgramData%\IndyPOS\v4\StoreHub\IndyPOS.StoreHub.exe" reset-admin
```

**What this command does:**
- Generates a fresh random 14-character password
- Re-arms the force-change (`must_change`) marker for the next sign-in
- Prints the new password to the console **only** — it does not write or update `admin-credentials.txt` or any other file

**Example output:**
```
ADMIN_RESET=true
New admin password (change it on next sign-in): xK7fQm2Vd9Pz3H
```

> ⚠️ **Caution:** Copy the password from the console immediately. It is not saved anywhere — if you lose it before signing in, you'll need to run `reset-admin` again.

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

> ⚠️ **Deprecated.** `install-config.ps1` has been removed. Use the automated installer
> (`IndyPOS-Setup.exe`, see Quick Start above), which configures PostgreSQL, the StoreHub
> service, and DPAPI-protected secrets. The manual steps below are retained for reference
> only and are not maintained for v4.

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
   - Stored DPAPI-protected inside `appsettings.json` (no separate key file)

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

Read the output properly — a dry run reports the same problems as the real migration while writing
nothing to the database, so anything surprising is far cheaper to meet here. It takes seconds. It does
write a clamped-stock report beside the legacy database if any product has negative stock; that file
is useful, keep it.

### 5.3 Execute Migration

> ### ⛔ Run this exactly once
>
> A second run against a store already migrated is **refused**: the tool reports
> `Migration REFUSED … already has N migrated invoice(s)`, aborts and writes nothing. That guard exists
> because it used to silently **double** the store's recorded turnover — measured on a real store: 15
> invoices and ฿1,056 became 30 and ฿2,112.
>
> **A non-zero exit code does not mean "try again".** Only one of the four outcomes wrote nothing and
> is safe to re-run:
>
> | What it prints | Exit | Written? | Action |
> |---|---|---|---|
> | `✓ Migration completed successfully!` | 0 | all | Continue to 5.4 |
> | `✓ Migration completed with warnings` | 0 | all | Read the warnings, then 5.4 |
> | `✗ Migration completed with errors` | 1 | **the successful rows ARE saved** | **Do NOT re-run.** Fix the causes in the legacy database or settle them by hand |
> | `✗ Migration ABORTED - nothing was written` | 1 | **nothing** | Safe to fix and re-run |
>
> On the largest real store this takes **about a minute** and peaks under **750 MB**. If it runs for
> many minutes, stop and investigate — that is not normal.

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

Every row should read ✓. Four rows check **values** rather than counts, and are the ones worth reading
closely:

| Row | A ✗ means |
|---|---|
| `Stock (units)` | Migrated stock does not match legacy `QuantityInStock`. A real problem |
| `Categories` | A product carries the wrong catalogue code, or a raw legacy id. A real problem |
| `Barcode keys` | Two products share their barcode's first 50 characters and cannot both migrate. Shorten one **in the legacy database** |
| `Payments (no invoice)` | Payments point at an invoice that no longer exists, so nothing can hold them. **Not fixable by the tool** and re-running will not help — settle the amount by hand |

⚠️ **GeneralHardware is expected to fail `Payments (no invoice)` permanently** — two payments totalling
฿1,000, with every other row green. That is a known inconsistency in that store's legacy data, not a
migration bug. Record it and move on; do **not** re-run.

**Never re-run the migration to try to clear a ✗.** Re-running duplicates invoices; it cannot repair
anything.

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
| StoreHub binaries | `C:\ProgramData\IndyPOS\v4\StoreHub\` |
| StoreHub config | `C:\Program Files\IndyPOS\StoreHub\appsettings.Production.json` |
| Store config | `C:\ProgramData\IndyPOS\v4\Config\StoreConfiguration.json` |
| Admin credentials (initial install) | `C:\ProgramData\IndyPOS\v4\Config\admin-credentials.txt` |
| JWT key | DPAPI-protected inside StoreHub `appsettings.json` (no separate file) |
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
