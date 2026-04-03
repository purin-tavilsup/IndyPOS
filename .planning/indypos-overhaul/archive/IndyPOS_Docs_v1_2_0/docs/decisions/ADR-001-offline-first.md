# ADR-001: Adopt Offline-First StoreHub Architecture

Status: Accepted  
Date: 2026-02-28

## Decision
Adopt a StoreHub + local PostgreSQL model where POS clients talk to StoreHub over LAN, and StoreHub syncs to cloud.

## Rationale
- Stores must keep selling when internet drops.
- Two terminals require concurrent local writes; Postgres handles this cleanly.
- Outbox pattern provides reliable sync and retry.

## Example impact
- POS clients become “API clients” to StoreHub.
- Database stays private (localhost).
- Cloud ingestion must be idempotent.
