# Architecture Overview (Offline-First)

Version: 1.4.0
Updated: 2026-02-28

## System shape

**POS Clients (desktop + tablet)**  
→ **StoreHub API (ASP.NET Core)**  
→ **Local PostgreSQL (localhost only)**  
→ **Outbox + Sync Worker**  
→ **Cloud API (Singapore)**  
→ **Central PostgreSQL**

## Development orchestration

IndyPOS uses **.NET Aspire** for local orchestration of:
- Cloud API
- Sync Worker
- PostgreSQL
- future MCP / reporting services

Production remains:
- Docker containers
- DigitalOcean Droplet
- PostgreSQL

## Recommended repo direction

The solution should evolve toward:
- shared domain/application/infrastructure projects
- explicit `CloudApi`, `SyncWorker`, `StoreHub`
- `AppHost` + `ServiceDefaults` for Aspire
- operations docs kept under versioned `/docs`
