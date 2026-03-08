# IndyPOS StoreHub – Local PostgreSQL Production Runbook (Windows)

## Overview
Each store has a Store Hub PC that runs:
- IndyPOS StoreHub
- PostgreSQL locally
- POS terminals connect to StoreHub over LAN

## Key rules
- PostgreSQL is localhost-only
- Terminals never access DB directly
- Backups are mandatory and tested

## Install & configure PostgreSQL
1. Install PostgreSQL using the official Windows installer.
2. Note the version path and service name.
3. Copy the ops kit into `C:\ProgramData\IndyPOS\ops\`
4. Run `install-config.ps1` as Administrator.

## Backups
Recommended schedule:
- every 4 hours, or nightly at minimum
- retention: 60 backups

Example task action:
```powershell
powershell.exe -ExecutionPolicy Bypass -File "C:\ProgramData\IndyPOS\ops\backup.ps1" `
  -PgBin "C:\Program Files\PostgreSQL\16\bin" `
  -DbName "indypos_storehub" -DbUser "indypos_app" -DbPassword "<APP_PASSWORD>" `
  -BackupDir "C:\ProgramData\IndyPOS\backups" -Retention 60 `
  -OffsiteDir "D:\IndyPOS_OffsiteBackups"
```

## Restore
Use `restore.ps1` with the latest `.dump` backup, then verify StoreHub health and POS connectivity.

## Weekly checks
- Disk free > 15%
- Latest backup < 24h old
- Sync backlog drains after internet returns
