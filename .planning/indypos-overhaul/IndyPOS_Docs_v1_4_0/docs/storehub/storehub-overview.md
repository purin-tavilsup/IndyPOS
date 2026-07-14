# StoreHub Overview

Version: 1.3.0
Updated: 2026-02-28

## Responsibilities
- Serve LAN API for POS terminals
- Perform all writes to local DB
- Create Outbox events at commit points
- Run SyncWorker to push/pull
- Provide health + sync status endpoints
