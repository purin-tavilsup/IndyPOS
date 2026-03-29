# IndyPOS StoreHub - Post-Deployment Monitoring Guide

## Overview
This guide describes what to monitor after deploying StoreHub at a pilot store, including key metrics, alert thresholds, and troubleshooting procedures.

---

## Monitoring Dashboard

### Key Metrics to Watch

| Metric | Normal Range | Warning | Critical |
|--------|--------------|---------|----------|
| API Response Time | < 500ms | 500-2000ms | > 2000ms |
| Sale Completion Time | < 1s | 1-3s | > 3s |
| Sync Pending Count | 0-10 | 10-50 | > 50 |
| Disk Free Space | > 25% | 15-25% | < 15% |
| PostgreSQL Connections | < 20 | 20-40 | > 40 |
| Backup Age | < 8h | 8-24h | > 24h |
| Service Uptime | 100% | 99%+ | < 99% |

---

## Daily Checks (First Week)

### Morning (Before Store Opens)
```powershell
# Run smoke test
.\smoke-test.ps1 -Username "admin" -Password "***"
```

Verify:
- [ ] StoreHub service is running
- [ ] Health endpoints return 200
- [ ] Backup from previous night exists
- [ ] Sync status shows "synced" or low pending count

### End of Day
```powershell
# Check today's sales count
$today = (Get-Date).ToString("yyyy-MM-dd")
Invoke-RestMethod -Uri "http://localhost:5000/reports/sales-summary?fromDate=$today&toDate=$today" `
  -Headers @{Authorization = "Bearer <TOKEN>"}
```

Verify:
- [ ] Invoice count matches expected
- [ ] No error logs in last 8 hours
- [ ] Sync backlog is draining

---

## Automated Health Check Script

Create a scheduled task to run this hourly:

```powershell
# health-check.ps1 - Run as scheduled task
param(
    [string]$StoreHubUrl = "http://localhost:5000",
    [string]$EventLogSource = "IndyPOS.Monitor"
)

# Create event log source if needed
if (-not [System.Diagnostics.EventLog]::SourceExists($EventLogSource)) {
    New-EventLog -LogName Application -Source $EventLogSource
}

function Write-Alert {
    param([string]$Message, [int]$EventId, [string]$EntryType = "Warning")
    Write-EventLog -LogName Application -Source $EventLogSource -EventId $EventId -EntryType $EntryType -Message $Message
}

# Check health endpoint
try {
    $response = Invoke-RestMethod -Uri "$StoreHubUrl/health/ready" -TimeoutSec 10
    if ($response.status -ne "Healthy") {
        Write-Alert "StoreHub health check failed: $($response | ConvertTo-Json)" -EventId 1001
    }
}
catch {
    Write-Alert "StoreHub unreachable: $($_.Exception.Message)" -EventId 1002 -EntryType "Error"
}

# Check backup age
$backupDir = "C:\ProgramData\IndyPOS\backups"
$latestBackup = Get-ChildItem -Path $backupDir -Filter "*.dump" -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if ($latestBackup) {
    $age = (Get-Date) - $latestBackup.LastWriteTime
    if ($age.TotalHours -gt 24) {
        Write-Alert "Backup is stale: $($latestBackup.Name) is $([math]::Round($age.TotalHours, 1)) hours old" -EventId 1003
    }
}
else {
    Write-Alert "No backup files found in $backupDir" -EventId 1004 -EntryType "Error"
}

# Check disk space
$drive = Get-WmiObject Win32_LogicalDisk -Filter "DeviceID='C:'"
$freePercent = ($drive.FreeSpace / $drive.Size) * 100
if ($freePercent -lt 15) {
    Write-Alert "Low disk space: C: drive only $([math]::Round($freePercent, 1))% free" -EventId 1005
}

# Check PostgreSQL service
$pgService = Get-Service -Name "postgresql*" | Where-Object { $_.Status -eq "Running" }
if (-not $pgService) {
    Write-Alert "PostgreSQL service not running" -EventId 1006 -EntryType "Error"
}
```

### Schedule the Task
```powershell
$action = New-ScheduledTaskAction -Execute "powershell.exe" `
  -Argument '-ExecutionPolicy Bypass -File "C:\ProgramData\IndyPOS\ops\health-check.ps1"'

$trigger = New-ScheduledTaskTrigger -Once -At (Get-Date) -RepetitionInterval (New-TimeSpan -Hours 1)
$principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount

Register-ScheduledTask -TaskName "IndyPOS-HealthCheck" -Action $action -Trigger $trigger -Principal $principal
```

---

## Log Locations

| Log | Location | Retention |
|-----|----------|-----------|
| StoreHub Logs | `C:\ProgramData\IndyPOS\logs\` | 7 days |
| PostgreSQL Logs | `C:\ProgramData\PostgreSQL\16\log\` | 7 days |
| Windows Events | Event Viewer > Application | 30 days |
| Backup Logs | `C:\ProgramData\IndyPOS\backups\*.log` | 60 files |

### View Recent Errors
```powershell
# StoreHub logs (last 100 lines with errors)
Get-Content "C:\ProgramData\IndyPOS\logs\storehub-*.log" -Tail 1000 |
    Select-String -Pattern "ERROR|Exception|WARN"

# Windows Event Log
Get-EventLog -LogName Application -Source "IndyPOS*" -Newest 20
```

---

## Sync Monitoring

### Check Sync Status
```powershell
# Requires authentication
$token = (Invoke-RestMethod -Uri "http://localhost:5000/auth/login" -Method POST -Body '{"username":"admin","password":"***"}' -ContentType "application/json").token

Invoke-RestMethod -Uri "http://localhost:5000/sync/status" -Headers @{Authorization = "Bearer $token"}
```

**Expected Response:**
```json
{
  "status": "synced",    // or "pending"
  "pending": 0,
  "failed": 0,
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### Troubleshoot Sync Issues

| Status | Cause | Resolution |
|--------|-------|------------|
| `pending` > 50 | Internet down | Check connectivity, events will drain when restored |
| `failed` > 0 | API errors | Check logs for specific errors, may need manual intervention |
| `pending` growing | SyncWorker stopped | Restart StoreHub service |

---

## Performance Monitoring

### API Latency Check
```powershell
# Measure response times
$endpoints = @(
    "/health",
    "/products",
    "/reports/sales-summary?fromDate=2024-01-01&toDate=2024-01-01"
)

$token = "..."  # Get token first

foreach ($endpoint in $endpoints) {
    $time = Measure-Command {
        Invoke-RestMethod -Uri "http://localhost:5000$endpoint" -Headers @{Authorization = "Bearer $token"}
    }
    Write-Host "$endpoint : $($time.TotalMilliseconds)ms"
}
```

### PostgreSQL Query Performance
```powershell
$env:PGPASSWORD = "<PASSWORD>"
& "C:\Program Files\PostgreSQL\16\bin\psql" -U indypos_app -d indypos_storehub -h 127.0.0.1 -c `
  "SELECT query, calls, mean_exec_time, total_exec_time
   FROM pg_stat_statements
   ORDER BY mean_exec_time DESC
   LIMIT 10;"
```

---

## Alert Response Procedures

### StoreHub Service Down
1. Check service status: `Get-Service IndyPOS.StoreHub`
2. Check for recent crashes in Event Log
3. Review StoreHub logs for errors
4. Restart service: `Restart-Service IndyPOS.StoreHub`
5. If persists, check PostgreSQL connectivity
6. Escalate if > 3 restarts in 1 hour

### PostgreSQL Service Down
1. Check service: `Get-Service postgresql*`
2. Check PostgreSQL logs for errors
3. Verify disk space
4. Restart service: `Restart-Service postgresql-x64-16`
5. If data corruption suspected, restore from backup

### High Sync Backlog
1. Check internet connectivity
2. Verify CloudApi is reachable
3. Check for failed events in logs
4. If > 100 pending for > 1 hour, investigate CloudApi

### Backup Failure
1. Check scheduled task: `Get-ScheduledTask IndyPOS-Backup`
2. Check backup logs for errors
3. Verify disk space
4. Run manual backup test
5. Check PostgreSQL connectivity

---

## Weekly Maintenance

### Sunday Night
1. Review week's error logs
2. Check backup file counts and sizes
3. Verify sync is fully caught up
4. Run VACUUM on PostgreSQL:
   ```sql
   VACUUM ANALYZE;
   ```

### Monthly
1. Review disk usage trends
2. Check PostgreSQL index health
3. Verify backup restoration works
4. Update documentation if needed

---

## Support Escalation

| Level | Condition | Contact | Response Time |
|-------|-----------|---------|---------------|
| L1 | Service restart needed | Store IT | 15 min |
| L2 | Data issue, restore needed | IT Manager | 1 hour |
| L3 | Bug fix required | Developer | 4 hours |

---

## Useful Commands Reference

```powershell
# Service management
Get-Service IndyPOS.StoreHub
Start-Service IndyPOS.StoreHub
Stop-Service IndyPOS.StoreHub
Restart-Service IndyPOS.StoreHub

# View logs
Get-Content "C:\ProgramData\IndyPOS\logs\storehub-*.log" -Tail 100

# Check backup
Get-ChildItem "C:\ProgramData\IndyPOS\backups" -Filter "*.dump" | Sort-Object LastWriteTime -Descending | Select-Object -First 5

# PostgreSQL status
Get-Service postgresql*
& "C:\Program Files\PostgreSQL\16\bin\psql" -U indypos_app -d indypos_storehub -h 127.0.0.1 -c "SELECT version();"

# Network connectivity
Test-NetConnection -ComputerName "api.indypos.cloud" -Port 443
```
