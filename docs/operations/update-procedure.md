# IndyPOS StoreHub - Update Procedure

> **For a store already running IndyPOS v4, use [upgrade-procedure.md](upgrade-procedure.md)
> instead.** `IndyPOS-Setup.exe --silent` performs the whole sequence below - backup,
> deploy, config restore, migrate, verify, and rollback on failure - and is the authoritative
> in-place upgrade path. This document remains as background on the underlying mechanics.

## Overview
This document describes the procedure for updating IndyPOS StoreHub to a new version.

**Estimated Downtime:** 5-15 minutes (depending on migration complexity)

---

## Pre-Update Checklist

Before starting the update:

- [ ] New version binaries available on deployment share
- [ ] Release notes reviewed for breaking changes
- [ ] Database migrations identified (if any)
- [ ] Backup verified < 4 hours old
- [ ] Store manager notified of maintenance window
- [ ] POS terminals idle (no active transactions)

---

## Update Procedure

### Step 1: Create Pre-Update Backup

```powershell
# Run as Administrator
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$env:PGPASSWORD = "<APP_PASSWORD>"

& "C:\Program Files\PostgreSQL\18\bin\pg_dump" `
    -h 127.0.0.1 -U indypos_app -d indypos_storehub `
    -F c -f "C:\ProgramData\IndyPOS\backups\pre_update_$timestamp.dump"

# Verify backup created
Get-Item "C:\ProgramData\IndyPOS\backups\pre_update_$timestamp.dump"
```

**Checkpoint:** Backup file exists with size > 0

---

### Step 2: Stop StoreHub Service

```powershell
# Stop the service
Stop-Service -Name "IndyPOS.StoreHub" -Force

# Wait and verify
Start-Sleep -Seconds 5
Get-Service -Name "IndyPOS.StoreHub"
# Status should be "Stopped"
```

**Checkpoint:** Service status is "Stopped"

---

### Step 3: Backup Current Binaries

```powershell
$installDir = "C:\Program Files\IndyPOS\StoreHub"
$backupDir = "C:\ProgramData\IndyPOS\updates\backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')"

# Create backup of current version
Copy-Item -Path $installDir -Destination $backupDir -Recurse

# Verify
Test-Path $backupDir
```

**Checkpoint:** Binary backup directory exists

---

### Step 4: Deploy New Binaries

```powershell
$sourceDir = "\\deployment-share\IndyPOS\StoreHub\v2.x.x"  # Adjust version
$installDir = "C:\Program Files\IndyPOS\StoreHub"

# Copy new version (excludes appsettings.json to preserve config)
Get-ChildItem $sourceDir -Exclude "appsettings.json", "appsettings.*.json" |
    Copy-Item -Destination $installDir -Recurse -Force

# Verify key files
Test-Path "$installDir\IndyPOS.StoreHub.exe"
```

**Checkpoint:** New executable exists

---

### Step 5: Run Database Migrations (If Required)

If the release includes database schema changes:

```powershell
# Option A: Using EF Core migrations bundled in executable
& "C:\Program Files\IndyPOS\StoreHub\IndyPOS.StoreHub.exe" --migrate

# Option B: Using separate migration tool
& "C:\Program Files\IndyPOS\StoreHub\efbundle.exe" --connection "<CONNECTION_STRING>"
```

**Migration Verification:**
```powershell
$env:PGPASSWORD = "<APP_PASSWORD>"
& "C:\Program Files\PostgreSQL\18\bin\psql" -U indypos_app -d indypos_storehub -h 127.0.0.1 -c "
SELECT * FROM __EFMigrationsHistory ORDER BY MigrationId DESC LIMIT 5;"
```

**Checkpoint:** Latest migration applied successfully

---

### Step 6: Start StoreHub Service

```powershell
# Start the service
Start-Service -Name "IndyPOS.StoreHub"

# Wait for startup
Start-Sleep -Seconds 10

# Check status
Get-Service -Name "IndyPOS.StoreHub"
```

**Checkpoint:** Service status is "Running"

---

### Step 7: Verify Health

```powershell
# Basic health
$health = Invoke-RestMethod -Uri "http://localhost:5000/health" -TimeoutSec 10
$health

# Ready check (includes database)
$ready = Invoke-RestMethod -Uri "http://localhost:5000/health/ready" -TimeoutSec 10
$ready
```

**Checkpoint:** Both endpoints return `{"status": "Healthy"}`

---

### Step 8: Run Smoke Tests

```powershell
cd "C:\ProgramData\IndyPOS\ops"
.\smoke-test.ps1 -Username "admin" -Password "<ADMIN_PASSWORD>"
```

**Checkpoint:** All smoke tests pass

---

### Step 9: Verify POS Terminals

- [ ] Open WinForms app on each terminal
- [ ] Login succeeds
- [ ] Product search works
- [ ] Complete a test sale

**Checkpoint:** All terminals operational

---

## Post-Update Verification

After 1 hour of operation:

- [ ] Check logs for errors: `Get-Content "C:\ProgramData\IndyPOS\logs\storehub-*.log" -Tail 200 | Select-String "ERROR"`
- [ ] Verify sync status: `GET /sync/status` shows low pending count
- [ ] Verify backup runs successfully

---

## Rollback Procedure

If the update fails at any step:

### Quick Rollback (Before Migration)

```powershell
# Stop service
Stop-Service -Name "IndyPOS.StoreHub" -Force

# Restore previous binaries
$backupDir = "C:\ProgramData\IndyPOS\updates\backup_<TIMESTAMP>"  # Use actual backup
$installDir = "C:\Program Files\IndyPOS\StoreHub"

Remove-Item "$installDir\*" -Recurse -Force
Copy-Item -Path "$backupDir\*" -Destination $installDir -Recurse

# Start service
Start-Service -Name "IndyPOS.StoreHub"
```

### Full Rollback (After Failed Migration)

```powershell
# Stop service
Stop-Service -Name "IndyPOS.StoreHub" -Force

# Restore database
$backupFile = "C:\ProgramData\IndyPOS\backups\pre_update_<TIMESTAMP>.dump"
& "C:\ProgramData\IndyPOS\ops\restore.ps1" `
    -PgBin "C:\Program Files\PostgreSQL\18\bin" `
    -DbName "indypos_storehub" `
    -DbUser "postgres" `
    -DbPassword "<POSTGRES_PASSWORD>" `
    -BackupFile $backupFile

# Restore previous binaries
$backupDir = "C:\ProgramData\IndyPOS\updates\backup_<TIMESTAMP>"
$installDir = "C:\Program Files\IndyPOS\StoreHub"

Remove-Item "$installDir\*" -Recurse -Force
Copy-Item -Path "$backupDir\*" -Destination $installDir -Recurse

# Start service
Start-Service -Name "IndyPOS.StoreHub"

# Verify
.\smoke-test.ps1 -Username "admin" -Password "<ADMIN_PASSWORD>"
```

---

## Version History Log

Document each update:

| Date | From Version | To Version | Migrated | Notes |
|------|--------------|------------|----------|-------|
| | | | | |

---

## Emergency Contacts

| Role | Name | Phone |
|------|------|-------|
| IT Support | | |
| Store Manager | | |
| Developer | | |
