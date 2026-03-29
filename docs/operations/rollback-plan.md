# IndyPOS StoreHub - Rollback Plan

## Overview
This document describes the rollback procedure to restore the legacy SQLite-based system if the StoreHub deployment fails.

**Recovery Time Objective (RTO):** 30 minutes
**Recovery Point Objective (RPO):** Last SQLite backup (pre-migration)

---

## Rollback Trigger Conditions

Initiate rollback if ANY of the following occur:

| Condition | Threshold | Action |
|-----------|-----------|--------|
| StoreHub service crashes | > 3 times in 1 hour | Rollback |
| Sales cannot be completed | Any critical failure | Rollback |
| Data loss detected | Any missing records | Rollback |
| Response time degradation | > 10x normal (5+ seconds) | Rollback |
| PostgreSQL service failure | Cannot restart | Rollback |

---

## Pre-Rollback Checklist

Before starting rollback:

- [ ] Document the failure reason
- [ ] Note current time and last successful transaction
- [ ] Ensure SQLite backup is accessible
- [ ] Notify store manager and staff
- [ ] Stop any ongoing transactions (tell cashiers to wait)

---

## Rollback Procedure

### Step 1: Stop StoreHub Service
```powershell
# Run as Administrator
Stop-Service -Name "IndyPOS.StoreHub" -Force
Set-Service -Name "IndyPOS.StoreHub" -StartupType Disabled
```

**Verification:**
```powershell
Get-Service -Name "IndyPOS.StoreHub"
# Status should be "Stopped"
```

### Step 2: Restore SQLite Database
```powershell
# Backup current (potentially corrupted) SQLite DB
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$sqliteDir = "C:\ProgramData\IndyPOS\db"

if (Test-Path "$sqliteDir\Store.db") {
    Copy-Item "$sqliteDir\Store.db" "$sqliteDir\Store.db.failed_$timestamp"
}

# Restore from pre-migration backup
$backupPath = "D:\Backups\IndyPOS\Store.db.pre_migration"  # Adjust path
Copy-Item $backupPath "$sqliteDir\Store.db" -Force
```

**Verification:**
```powershell
# Verify SQLite file exists and has data
Get-Item "$sqliteDir\Store.db"
# File size should be > 0
```

### Step 3: Update WinForms Configuration
Edit `appsettings.json` on each POS terminal:
```json
{
  "StoreHub": {
    "Enabled": false
  },
  "Database": {
    "Path": "C:\\ProgramData\\IndyPOS\\db\\Store.db"
  }
}
```

Or use PowerShell:
```powershell
$configPath = "C:\Program Files\IndyPOS\appsettings.json"
$config = Get-Content $configPath | ConvertFrom-Json
$config.StoreHub.Enabled = $false
$config | ConvertTo-Json -Depth 10 | Set-Content $configPath
```

### Step 4: Restart WinForms Application
```powershell
# Kill any running IndyPOS processes
Get-Process -Name "IndyPOS*" | Stop-Process -Force

# Restart application (users will need to log in again)
Start-Process "C:\Program Files\IndyPOS\IndyPOS.Windows.Forms.exe"
```

### Step 5: Verify Legacy System
- [ ] WinForms app starts without errors
- [ ] Login works
- [ ] Products display correctly
- [ ] Test sale completes successfully
- [ ] Receipt prints (if applicable)

---

## Post-Rollback Actions

### Immediate (Within 1 hour)
1. [ ] Verify all POS terminals are operational
2. [ ] Inform IT support of rollback
3. [ ] Document failure details in incident log
4. [ ] Verify backup schedule is running for SQLite

### Short-term (Within 24 hours)
1. [ ] Collect StoreHub logs for analysis
   ```powershell
   Copy-Item "C:\ProgramData\IndyPOS\logs\*" "C:\Temp\storehub_failure_logs\" -Recurse
   ```
2. [ ] Export PostgreSQL logs (if accessible)
3. [ ] Review Windows Event Log
4. [ ] Schedule incident review meeting

### Recovery Data (If Needed)
If transactions occurred in StoreHub before rollback:

1. Export PostgreSQL invoices:
   ```sql
   COPY (SELECT * FROM invoices WHERE created_utc > '<CUTOVER_TIME>')
   TO '/tmp/missed_invoices.csv' WITH CSV HEADER;
   ```

2. Manually reconcile any missed transactions

---

## Data Recovery from PostgreSQL

If you need to recover data from PostgreSQL after rollback:

### Export Invoices
```powershell
$pgBin = "C:\Program Files\PostgreSQL\16\bin"
$env:PGPASSWORD = "<APP_PASSWORD>"

& "$pgBin\psql" -U indypos_app -d indypos_storehub -h 127.0.0.1 -c `
  "COPY (SELECT * FROM invoices WHERE created_utc > '2024-01-01') TO STDOUT WITH CSV HEADER" `
  > C:\Temp\invoices_recovery.csv

Remove-Item Env:\PGPASSWORD
```

### Export Products (If Modified)
```powershell
& "$pgBin\psql" -U indypos_app -d indypos_storehub -h 127.0.0.1 -c `
  "COPY (SELECT * FROM products) TO STDOUT WITH CSV HEADER" `
  > C:\Temp\products_recovery.csv
```

---

## PostgreSQL Cleanup (Optional)

After successful rollback and data recovery, you may optionally clean up:

```powershell
# Stop PostgreSQL service
Stop-Service -Name "postgresql-x64-16"
Set-Service -Name "postgresql-x64-16" -StartupType Disabled

# Keep data for potential future analysis
# DO NOT delete until incident is fully resolved
```

---

## Rollback Verification Checklist

| Check | Status |
|-------|--------|
| StoreHub service stopped | [ ] |
| SQLite database restored | [ ] |
| WinForms config updated | [ ] |
| Application restarted | [ ] |
| Login works | [ ] |
| Products display | [ ] |
| Test sale completed | [ ] |
| All terminals verified | [ ] |
| Staff notified | [ ] |
| Incident documented | [ ] |

**Rollback Completed:** [ ] YES / [ ] NO

**Verified By:** _________________
**Date/Time:** _________________

---

## Lessons Learned Template

After incident resolution, complete this section:

**Incident Date:** _________________

**Root Cause:** _________________

**What Worked:**
-

**What Didn't Work:**
-

**Improvements for Next Attempt:**
-

---

## Emergency Contacts

| Role | Name | Phone |
|------|------|-------|
| IT Support | | |
| Store Manager | | |
| Developer (escalation) | | |
