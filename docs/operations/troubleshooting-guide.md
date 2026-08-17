# IndyPOS StoreHub - Troubleshooting Guide

## Overview
This guide covers common issues and their resolutions for the IndyPOS StoreHub system.

---

## Quick Diagnostic Commands

```powershell
# Check all services
Get-Service IndyPOS.StoreHub, postgresql* | Format-Table Name, Status

# Check StoreHub logs (last 50 errors)
Get-Content "C:\ProgramData\IndyPOS\logs\storehub-*.log" -Tail 500 | Select-String "ERROR|Exception"

# Check health endpoints
Invoke-RestMethod -Uri "http://localhost:5000/health" -TimeoutSec 5
Invoke-RestMethod -Uri "http://localhost:5000/health/ready" -TimeoutSec 5

# Check PostgreSQL connectivity
$env:PGPASSWORD = "<APP_PASSWORD>"
& "C:\Program Files\PostgreSQL\18\bin\psql" -U indypos_app -d indypos_storehub -h 127.0.0.1 -c "SELECT 1;"
```

---

## Issue Categories

### 1. StoreHub Service Issues

#### StoreHub Won't Start

**Symptoms:**
- Service status shows "Stopped"
- Windows Event Log shows errors
- POS terminals show "Connection refused"

**Diagnostic Steps:**
```powershell
# Check service status
Get-Service IndyPOS.StoreHub

# Check Windows Event Log
Get-EventLog -LogName Application -Source "IndyPOS*" -Newest 10

# Check if port 5000 is in use
netstat -ano | findstr ":5000"
```

**Common Causes & Solutions:**

| Cause | Solution |
|-------|----------|
| Port 5000 already in use | Find and stop conflicting process: `taskkill /PID <PID> /F` |
| PostgreSQL not running | Start PostgreSQL: `Start-Service postgresql-x64-18` |
| Configuration file missing | Verify `appsettings.json` exists in install directory |
| Invalid connection string | Check PostgreSQL credentials in appsettings |
| Missing .NET Runtime | Install .NET 10 Runtime |

**Recovery:**
```powershell
# Restart with fresh logs
Stop-Service IndyPOS.StoreHub -Force
Start-Sleep -Seconds 2
Start-Service IndyPOS.StoreHub
Start-Sleep -Seconds 5
Get-Service IndyPOS.StoreHub
```

---

#### StoreHub Crashes Repeatedly

**Symptoms:**
- Service starts but stops within seconds/minutes
- Repeated restarts in Event Log
- POS terminals intermittently disconnect

**Diagnostic Steps:**
```powershell
# Check crash logs
Get-Content "C:\ProgramData\IndyPOS\logs\storehub-*.log" -Tail 200 | Select-String -Context 5 "FATAL|crash|unhandled"

# Check memory usage
Get-Process | Where-Object {$_.ProcessName -like "*IndyPOS*"} | Select-Object ProcessName, WorkingSet64
```

**Common Causes & Solutions:**

| Cause | Solution |
|-------|----------|
| Out of memory | Check for memory leaks, restart PC |
| Database connection pool exhausted | Restart PostgreSQL, check for connection leaks |
| Unhandled exception in code | Check logs for stack trace, report to developer |
| Disk full | Free up disk space (see Disk Space section) |

---

### 2. Database Issues

#### PostgreSQL Won't Start

**Symptoms:**
- Service status shows "Stopped"
- StoreHub health check fails
- "Connection refused" errors

**Diagnostic Steps:**
```powershell
# Check service status
Get-Service postgresql*

# Check PostgreSQL logs
Get-Content "C:\Program Files\PostgreSQL\18\data\log\*.log" -Tail 100
```

**Common Causes & Solutions:**

| Cause | Solution |
|-------|----------|
| Corrupted data files | Restore from backup (see restore.ps1) |
| Port 5432 in use | Find conflicting process |
| Insufficient disk space | Free up space |
| Service account issues | Verify NETWORK SERVICE has permissions |

**Recovery:**
```powershell
Start-Service postgresql-x64-18
Start-Sleep -Seconds 10
Get-Service postgresql-x64-18
```

---

#### Database Connection Errors

**Symptoms:**
- "Connection refused" in logs
- "Too many connections" errors
- Slow response times

**Diagnostic Steps:**
```powershell
# Check active connections
$env:PGPASSWORD = "<APP_PASSWORD>"
& "C:\Program Files\PostgreSQL\18\bin\psql" -U indypos_app -d indypos_storehub -h 127.0.0.1 -c "SELECT count(*) FROM pg_stat_activity WHERE datname = 'indypos_storehub';"
```

**Common Causes & Solutions:**

| Cause | Solution |
|-------|----------|
| Too many connections | Increase `max_connections` in postgresql.conf or restart services |
| Wrong credentials | Verify password in StoreHub appsettings.json |
| PostgreSQL not listening | Check `listen_addresses` in postgresql.conf |
| Connection pooling issue | Restart StoreHub service |

---

#### Slow Database Queries

**Symptoms:**
- POS terminals lag on product search
- Sales take > 3 seconds to complete
- High CPU on Store Hub PC

**Diagnostic Steps:**
```powershell
# Check slow queries
& "C:\Program Files\PostgreSQL\18\bin\psql" -U indypos_app -d indypos_storehub -h 127.0.0.1 -c "
SELECT query, calls, mean_exec_time, total_exec_time
FROM pg_stat_statements
ORDER BY mean_exec_time DESC
LIMIT 10;"
```

**Solutions:**
```powershell
# Run VACUUM ANALYZE to update statistics
& "C:\Program Files\PostgreSQL\18\bin\psql" -U indypos_app -d indypos_storehub -h 127.0.0.1 -c "VACUUM ANALYZE;"

# Check for missing indexes
& "C:\Program Files\PostgreSQL\18\bin\psql" -U indypos_app -d indypos_storehub -h 127.0.0.1 -c "
SELECT relname, seq_scan, idx_scan
FROM pg_stat_user_tables
WHERE seq_scan > idx_scan
ORDER BY seq_scan DESC
LIMIT 10;"
```

---

### 3. Network Issues

#### POS Terminals Can't Connect

**Symptoms:**
- "Unable to connect to StoreHub" on POS
- Timeout errors
- Only some terminals affected

**Diagnostic Steps:**
```powershell
# From POS terminal - ping Store Hub
ping <STOREHUB_IP>

# From POS terminal - check port
Test-NetConnection -ComputerName <STOREHUB_IP> -Port 5000

# On Store Hub - check firewall
Get-NetFirewallRule | Where-Object {$_.DisplayName -like "*IndyPOS*" -or $_.DisplayName -like "*5000*"}
```

**Common Causes & Solutions:**

| Cause | Solution |
|-------|----------|
| Firewall blocking port 5000 | Add firewall rule (see below) |
| Wrong IP in POS config | Update `appsettings.json` on terminal |
| StoreHub not running | Start StoreHub service |
| Network cable issue | Check physical connection |

**Add Firewall Rule:**
```powershell
New-NetFirewallRule -DisplayName "IndyPOS StoreHub" -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow
```

---

### 4. Sync Issues

#### Sync Backlog Growing

**Symptoms:**
- `/sync/status` shows high pending count
- Changes not appearing in cloud
- "Sync pending" indicator on POS

**Diagnostic Steps:**
```powershell
# Check sync status
$token = "<GET_TOKEN_FIRST>"
Invoke-RestMethod -Uri "http://localhost:5000/sync/status" -Headers @{Authorization = "Bearer $token"}

# Check internet connectivity
Test-NetConnection -ComputerName "api.indypos.cloud" -Port 443
```

**Common Causes & Solutions:**

| Cause | Solution |
|-------|----------|
| Internet down | Wait for connection to restore - events will drain automatically |
| Cloud API unreachable | Check CloudApi status, firewall rules |
| Invalid sync credentials | Verify store credentials in config |
| Sync worker stopped | Restart StoreHub service |

---

#### Failed Sync Events

**Symptoms:**
- `/sync/status` shows non-zero `failed` count
- Specific events repeatedly failing
- Cloud data out of sync

**Diagnostic Steps:**
```powershell
# Check failed events in logs
Get-Content "C:\ProgramData\IndyPOS\logs\storehub-*.log" -Tail 1000 | Select-String "sync.*failed|SyncWorker.*error"
```

**Solutions:**
1. Check logs for specific error messages
2. Verify cloud API is accepting the event type
3. May require manual data reconciliation
4. Contact developer for persistent failures

---

### 5. Backup Issues

#### Backups Not Running

**Symptoms:**
- No new `.dump` files in backup directory
- Backup older than 24 hours
- Scheduled task not executing

**Diagnostic Steps:**
```powershell
# Check scheduled task
Get-ScheduledTask -TaskName "IndyPOS-Backup" | Get-ScheduledTaskInfo

# Check task history
Get-WinEvent -LogName "Microsoft-Windows-TaskScheduler/Operational" |
    Where-Object {$_.Message -like "*IndyPOS*"} |
    Select-Object -First 10
```

**Common Causes & Solutions:**

| Cause | Solution |
|-------|----------|
| Task disabled | Enable task: `Enable-ScheduledTask -TaskName "IndyPOS-Backup"` |
| Wrong credentials | Update task to run as SYSTEM |
| pg_dump not found | Verify PgBin path in task arguments |
| Disk full | Free up space |

**Manual Backup Test:**
```powershell
& "C:\ProgramData\IndyPOS\ops\backup.ps1" `
  -PgBin "C:\Program Files\PostgreSQL\18\bin" `
  -DbName "indypos_storehub" `
  -DbUser "indypos_app" `
  -DbPassword "<APP_PASSWORD>" `
  -BackupDir "C:\ProgramData\IndyPOS\backups" `
  -Retention 60
```

---

#### Restore Fails

**Symptoms:**
- `restore.ps1` exits with error
- Database not restored
- "Permission denied" errors

**Diagnostic Steps:**
```powershell
# Verify backup file exists and is valid
Get-Item "C:\ProgramData\IndyPOS\backups\*.dump" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
```

**Common Causes & Solutions:**

| Cause | Solution |
|-------|----------|
| Backup file corrupted | Try older backup |
| Database in use | Stop StoreHub first |
| Wrong credentials | Verify PostgreSQL admin password |
| Disk full | Free up space |

---

### 6. Performance Issues

#### High Memory Usage

**Symptoms:**
- Store Hub PC slow
- StoreHub process using > 500MB RAM
- Out of memory errors

**Diagnostic Steps:**
```powershell
Get-Process | Where-Object {$_.ProcessName -like "*IndyPOS*" -or $_.ProcessName -like "*postgres*"} |
    Select-Object ProcessName, @{N='Memory(MB)';E={[math]::Round($_.WorkingSet64/1MB,2)}}
```

**Solutions:**
1. Restart StoreHub service (clears memory)
2. Check for memory leaks in logs
3. Increase RAM on Store Hub PC
4. Review PostgreSQL `shared_buffers` setting

---

#### Disk Space Low

**Symptoms:**
- Alerts for < 15% disk space
- Backups failing
- Database errors

**Diagnostic Steps:**
```powershell
# Check disk usage
Get-WmiObject Win32_LogicalDisk | Select-Object DeviceID, @{N='Free(GB)';E={[math]::Round($_.FreeSpace/1GB,2)}}, @{N='Total(GB)';E={[math]::Round($_.Size/1GB,2)}}

# Find large files
Get-ChildItem -Path "C:\ProgramData\IndyPOS" -Recurse | Sort-Object Length -Descending | Select-Object -First 20 FullName, @{N='Size(MB)';E={[math]::Round($_.Length/1MB,2)}}
```

**Solutions:**
1. Delete old log files (keep 7 days)
2. Reduce backup retention
3. Move backups to external drive
4. Clear Windows temp files

```powershell
# Clean old logs
Get-ChildItem "C:\ProgramData\IndyPOS\logs\*.log" | Where-Object {$_.LastWriteTime -lt (Get-Date).AddDays(-7)} | Remove-Item

# Clean old backups beyond retention
Get-ChildItem "C:\ProgramData\IndyPOS\backups\*.dump" | Sort-Object LastWriteTime -Descending | Select-Object -Skip 60 | Remove-Item
```

---

### 7. POS App Error Codes (`ERR-XXXX`)

#### Operator Reports an ERR- Code

**Symptoms:**
- A Thai dialog on the POS till reads a message ending in a code like `แจ้งรหัส ERR-9FDF`
- No English crash dialog — the app's global error handler caught it and stayed usable (or, on a
  fatal error, closed cleanly)

**Diagnostic Steps:**

The POS app's log is separate from the StoreHub log referenced above. It lives at
`C:\ProgramData\IndyPOS\v{Major}\logs\log<date>.json` on the till itself (e.g.
`C:\ProgramData\IndyPOS\v4\logs\log20260731.json` — the version segment matches the installed
major version, per `InstallPaths.LogsDirectory`).

```powershell
# Find the entry for a reported code (adjust the version segment and date)
Select-String -Path "C:\ProgramData\IndyPOS\v4\logs\log20260731.json" -Pattern 'ERR-9FDF'
```

The matched line is a compact-JSON event carrying the full exception and an `Operation` property
naming what was happening when it failed — both useful for a developer, neither shown to the
operator.

**Note on severity:** the level varies by how the failure arrived, so **search by the `ERR-` code
itself rather than by level** — a filter on `Error` alone misses two of the four cases:

| Source | Level |
|---|---|
| A UI-thread failure caught by the global handler | `Error` |
| A crash taking the process down | `Fatal` |
| Report-load failures (`ReportErrorHandler`) | `Warning` |
| An unobserved background task (`TaskScheduler.UnobservedTaskException`) | `Warning` |

---

## Error Code Reference

| Error Pattern | Meaning | Resolution |
|---------------|---------|------------|
| `ECONNREFUSED` | Service not listening | Start StoreHub/PostgreSQL |
| `ETIMEDOUT` | Network timeout | Check network connectivity |
| `23505` | Duplicate key violation | Data integrity issue - check for duplicates. **During a data migration** see the note below |
| `42P01` | Table does not exist | Run EF migrations |
| `28P01` | Authentication failed | Check credentials |
| `53300` | Too many connections | Restart services, check for leaks |
| `57014` | Query cancelled | Timeout - check slow queries |

### `23505` during a data migration

`IX_product_store_id_barcode` in the message means two products are competing for one barcode. Two
causes, and the difference matters:

1. **The migration was already run against this database.** A second run is now **refused** before
   it writes anything — you will see `Migration REFUSED … already has N migrated invoice(s)` and an
   ABORTED banner, not a 23505. If you somehow reach 23505 this way, the store was first migrated by a
   build older than the legacy-id guard, so the guard could not recognise it; check
   `SELECT COUNT(*) FROM invoice` before doing anything else, and restore from backup if the turnover
   has already been doubled.
2. **Two legacy products share the first 50 characters of their barcode.** `Product.Barcode` is capped
   at 50, so both truncate to the same value and cannot coexist. Real barcodes hit this — scanned TISI
   certification QR codes are 90 characters, and their first 45 are a shared URL prefix. Shorten one
   barcode **in the legacy database**, then migrate into a clean database.

`verify`'s `Barcode keys` row detects case 2 before you migrate. Run the dry run first and it costs
nothing.

---

## Escalation Matrix

| Level | Condition | Who | Response |
|-------|-----------|-----|----------|
| L1 | Service restart resolves | Store IT | 15 min |
| L2 | Data restore required | IT Manager | 1 hour |
| L3 | Bug fix or code change needed | Developer | 4 hours |

---

## Related Documentation

- [Pilot Checklist](pilot-checklist.md)
- [Rollback Plan](rollback-plan.md)
- [Post-Deployment Monitoring](post-deployment-monitoring.md)
- [Smoke Test Script](smoke-test.ps1)
