# IndyPOS StoreHub - Operations Runbook

## Overview

IndyPOS StoreHub is a local API service that runs on a Store Hub PC, serving POS terminals over LAN and syncing with the cloud.

**Architecture:**
```
┌──────────────────────────────────────────────────────────────┐
│                        Store Hub PC                          │
│  ┌──────────────────┐  ┌─────────────────────────────────┐  │
│  │   PostgreSQL 18  │◄─│      IndyPOS.StoreHub.exe       │  │
│  │   (localhost)    │  │         (port 5000)             │  │
│  └──────────────────┘  └──────────────┬──────────────────┘  │
│                                       │                      │
└───────────────────────────────────────┼──────────────────────┘
                                        │ HTTP/REST
                    ┌───────────────────┼───────────────────┐
                    │                   │                   │
             ┌──────▼─────┐      ┌──────▼─────┐      ┌──────▼─────┐
             │ POS Term 1 │      │ POS Term 2 │      │ Cloud API  │
             │ (WinForms) │      │ (WinForms) │      │ (Sync)     │
             └────────────┘      └────────────┘      └────────────┘
```

---

## Quick Reference

### Service Management

```powershell
# Check all services
Get-Service IndyPOS.StoreHub, postgresql* | Format-Table Name, Status

# Restart StoreHub
Restart-Service IndyPOS.StoreHub

# Restart PostgreSQL
Restart-Service postgresql-x64-18
```

### Health Check

```powershell
# Quick health check
Invoke-RestMethod "http://localhost:5000/health"

# Full ready check (includes DB)
Invoke-RestMethod "http://localhost:5000/health/ready"

# Run smoke tests
.\smoke-test.ps1 -Username "admin" -Password "<PASSWORD>"
```

### View Logs

```powershell
# Last 100 lines
Get-Content "C:\ProgramData\IndyPOS\logs\storehub-*.log" -Tail 100

# Errors only
Get-Content "C:\ProgramData\IndyPOS\logs\storehub-*.log" -Tail 500 | Select-String "ERROR|Exception"
```

### Manual Backup

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

## Key Locations

| Item | Path |
|------|------|
| StoreHub Binaries | `C:\Program Files\IndyPOS\StoreHub\` |
| Ops Scripts | `C:\ProgramData\IndyPOS\ops\` |
| Logs | `C:\ProgramData\IndyPOS\logs\` |
| Backups | `C:\ProgramData\IndyPOS\backups\` |
| Configuration | `C:\ProgramData\IndyPOS\Config\` |
| PostgreSQL Data | `C:\Program Files\PostgreSQL\18\data\` |
| PostgreSQL Logs | `C:\Program Files\PostgreSQL\18\data\log\` |

---

## Key Metrics & Thresholds

| Metric | Normal | Warning | Critical |
|--------|--------|---------|----------|
| API Response Time | < 500ms | 500-2000ms | > 2000ms |
| Sale Completion Time | < 1s | 1-3s | > 3s |
| Sync Pending Count | 0-10 | 10-50 | > 50 |
| Disk Free Space | > 25% | 15-25% | < 15% |
| PostgreSQL Connections | < 20 | 20-40 | > 40 |
| Backup Age | < 8h | 8-24h | > 24h |
| Service Uptime | 100% | 99%+ | < 99% |

---

## Daily Operations

### Morning Checklist (Before Store Opens)

```powershell
# Run smoke test
.\smoke-test.ps1 -Username "admin" -Password "<PASSWORD>"
```

- [ ] StoreHub service running
- [ ] Health endpoints return 200
- [ ] Last backup < 24h old
- [ ] Sync status shows "synced" or low pending count

### End of Day Checklist

- [ ] Verify day's invoice count
- [ ] Check for errors in logs
- [ ] Confirm sync backlog is draining
- [ ] Ensure backup completed

---

## Weekly Maintenance

### Sunday Night Tasks

1. **Review error logs:**
   ```powershell
   Get-Content "C:\ProgramData\IndyPOS\logs\storehub-*.log" | Select-String "ERROR|WARN" | Select-Object -Last 100
   ```

2. **Check backup health:**
   ```powershell
   Get-ChildItem "C:\ProgramData\IndyPOS\backups\*.dump" | Measure-Object | Select-Object Count
   ```

3. **Run VACUUM on PostgreSQL:**
   ```powershell
   $env:PGPASSWORD = "<APP_PASSWORD>"
   & "C:\Program Files\PostgreSQL\18\bin\psql" -U indypos_app -d indypos_storehub -h 127.0.0.1 -c "VACUUM ANALYZE;"
   ```

4. **Verify disk space:**
   ```powershell
   Get-WmiObject Win32_LogicalDisk -Filter "DeviceID='C:'" | Select-Object @{N='FreeGB';E={[math]::Round($_.FreeSpace/1GB,2)}}
   ```

---

## Scheduled Tasks

### Backup Task

**Name:** `IndyPOS-Backup`
**Schedule:** Every 4 hours
**Action:**
```powershell
powershell.exe -ExecutionPolicy Bypass -File "C:\ProgramData\IndyPOS\ops\backup.ps1" `
  -PgBin "C:\Program Files\PostgreSQL\18\bin" `
  -DbName "indypos_storehub" `
  -DbUser "indypos_app" `
  -DbPassword "<APP_PASSWORD>" `
  -BackupDir "C:\ProgramData\IndyPOS\backups" `
  -Retention 60 `
  -OffsiteDir "D:\IndyPOS_OffsiteBackups"
```

### Health Check Task

**Name:** `IndyPOS-HealthCheck`
**Schedule:** Every 1 hour
**Action:**
```powershell
powershell.exe -ExecutionPolicy Bypass -File "C:\ProgramData\IndyPOS\ops\health-check.ps1"
```

---

## Emergency Procedures

### StoreHub Down

1. Check service status: `Get-Service IndyPOS.StoreHub`
2. Check recent logs for errors
3. Restart service: `Restart-Service IndyPOS.StoreHub`
4. Run health check
5. If persists: check PostgreSQL, see [Troubleshooting Guide](troubleshooting-guide.md)

### PostgreSQL Down

1. Check service: `Get-Service postgresql*`
2. Check PostgreSQL logs
3. Restart service: `Restart-Service postgresql-x64-18`
4. If persists: check disk space, consider restore

### Data Loss Suspected

1. **STOP** all operations
2. Document the issue
3. Check latest backup
4. Follow [Rollback Plan](rollback-plan.md)
5. Contact IT Manager

### Complete System Failure

1. Follow [Rollback Plan](rollback-plan.md) to restore SQLite system
2. Resume operations on legacy system
3. Document failure details
4. Schedule root cause analysis

---

## Documentation Index

| Document | Purpose |
|----------|---------|
| [Pilot Checklist](pilot-checklist.md) | Initial deployment steps |
| [Smoke Test Script](smoke-test.ps1) | Automated health verification |
| [Post-Deployment Monitoring](post-deployment-monitoring.md) | Metrics and alerting |
| [Troubleshooting Guide](troubleshooting-guide.md) | Common issues and fixes |
| [Update Procedure](update-procedure.md) | How to apply updates |
| [Rollback Plan](rollback-plan.md) | Emergency recovery |

---

## Ops Kit Scripts

| Script | Purpose |
|--------|---------|
| `install-config.ps1` | Configure PostgreSQL and create database |
| `backup.ps1` | Create pg_dump backup with retention |
| `restore.ps1` | Restore from .dump backup |
| `smoke-test.ps1` | Run health checks |
| `health-check.ps1` | Scheduled monitoring (writes to Event Log) |

---

## Escalation Matrix

| Level | Condition | Contact | Response Time |
|-------|-----------|---------|---------------|
| L1 | Service restart needed | Store IT | 15 min |
| L2 | Data restore required | IT Manager | 1 hour |
| L3 | Bug fix required | Developer | 4 hours |

---

## Emergency Contacts

| Role | Name | Phone |
|------|------|-------|
| IT Support | | |
| Store Manager | | |
| Developer (L3) | | |

---

## Change Log

| Date | Change | Author |
|------|--------|--------|
| 2026-03-29 | Initial runbook created | |
