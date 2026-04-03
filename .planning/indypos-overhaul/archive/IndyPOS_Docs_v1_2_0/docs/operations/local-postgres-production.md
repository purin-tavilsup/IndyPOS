# Local PostgreSQL Production (Windows)

Version: 1.2.0
Updated: 2026-02-28

## Recommended Mode
Native PostgreSQL Windows Service (localhost only)

## Ops Kit Location
All operational scripts are bundled in:

/docs/operations/ops-kit/

Includes:
- install-config.ps1
- backup.ps1
- restore.ps1
- RUNBOOK.md

## Security Baseline
postgresql.conf:
listen_addresses = '127.0.0.1'
password_encryption = 'scram-sha-256'

pg_hba.conf:
local   all             all                                     scram-sha-256
host    all             all             127.0.0.1/32            scram-sha-256
host    all             all             ::1/128                 scram-sha-256

## Backup Recommendation
- Every 4 hours
- Retention 30–60 copies
- Test restore monthly
