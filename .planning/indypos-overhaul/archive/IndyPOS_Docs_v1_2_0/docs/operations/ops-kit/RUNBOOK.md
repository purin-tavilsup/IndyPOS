# IndyPOS StoreHub – Local PostgreSQL Production Runbook (Windows)

## Overview
Each store has a **Store Hub PC** (desktop) that runs:
- IndyPOS StoreHub (local API + sync worker)
- PostgreSQL locally
- POS terminals connect to StoreHub over LAN

**Key rules**
- PostgreSQL is **localhost-only** (no LAN/WAN access)
- Terminals never access DB directly (only StoreHub API)
- Backups are mandatory and tested

---

## 1) Install & configure PostgreSQL (one-time)

1. Install PostgreSQL using the official Windows installer.
2. Note:
   - Version path, e.g. `C:\Program Files\PostgreSQL\16\bin`
   - Service name, e.g. `postgresql-x64-16`

3. Copy the ops kit into:
   - `C:\ProgramData\IndyPOS\ops\`

4. Run configuration (PowerShell as Admin):

```powershell
powershell.exe -ExecutionPolicy Bypass -File "C:\ProgramData\IndyPOS\ops\install-config.ps1" `
  -PgBin "C:\Program Files\PostgreSQL\16\bin" `
  -PgServiceName "postgresql-x64-16" `
  -StoreId "STORE-001" `
  -DbName "indypos_storehub" `
  -AppUser "indypos_app" `
  -PostgresPassword "<POSTGRES_PASSWORD>" `
  -AppUserPassword "<APP_PASSWORD>"
```

This configures:
- `listen_addresses = 127.0.0.1`
- `pg_hba.conf` localhost-only + SCRAM
- DB + app role

---

## 2) Backups (Task Scheduler)

### Recommended schedule
- Every **4 hours** OR at least nightly 2am
- Retention: 60 backups (adjust)

### Create scheduled task action
```powershell
powershell.exe -ExecutionPolicy Bypass -File "C:\ProgramData\IndyPOS\ops\backup.ps1" `
  -PgBin "C:\Program Files\PostgreSQL\16\bin" `
  -DbName "indypos_storehub" -DbUser "indypos_app" -DbPassword "<APP_PASSWORD>" `
  -BackupDir "C:\ProgramData\IndyPOS\backups" -Retention 60 `
  -OffsiteDir "D:\IndyPOS_OffsiteBackups"
```

Verify backups exist under:
- `C:\ProgramData\IndyPOS\backups\`

---

## 3) Restore (emergency)

```powershell
powershell.exe -ExecutionPolicy Bypass -File "C:\ProgramData\IndyPOS\ops\restore.ps1" `
  -PgBin "C:\Program Files\PostgreSQL\16\bin" `
  -DumpFile "C:\ProgramData\IndyPOS\backups\indypos_indypos_storehub_YYYYMMDD_HHMMSS.dump" `
  -DbName "indypos_storehub" `
  -DbAdminUser "postgres" -DbAdminPassword "<POSTGRES_PASSWORD>" `
  -AppUser "indypos_app"
```

After restore:
- Start StoreHub
- Check `/health`
- Verify POS loads products and can create a test sale

---

## 4) Weekly checks
- Disk free > 15%
- Latest backup < 24h old
- Sync backlog drains after internet returns
- Windows updates scheduled outside business hours
