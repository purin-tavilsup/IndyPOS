# IndyPOS StoreHub Postgres Production Kit

Files:
- install-config.ps1  -> configure PostgreSQL service to localhost-only, create role+db
- backup.ps1          -> pg_dump backup + retention + optional offsite copy
- restore.ps1         -> restore from .dump (overwrites DB)
- RUNBOOK.md          -> operational instructions
