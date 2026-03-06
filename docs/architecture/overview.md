# Architecture Overview

## Current Architecture (Desktop + SQLite)

```
┌─────────────────────────────────────────┐
│         Windows.Forms Desktop           │
│                                         │
│  ┌─────────────────────────────────┐   │
│  │     Application Layer (CQRS)    │   │
│  │   Commands, Queries, Handlers   │   │
│  └─────────────────────────────────┘   │
│                  │                      │
│  ┌─────────────────────────────────┐   │
│  │    Infrastructure Layer         │   │
│  │  Services, Repositories (Dapper)│   │
│  └─────────────────────────────────┘   │
│                  │                      │
│  ┌─────────────────────────────────┐   │
│  │          SQLite Database        │   │
│  │        (Local file: Store.db)   │   │
│  └─────────────────────────────────┘   │
└─────────────────────────────────────────┘
```

## Target Architecture (Offline-First + Cloud Sync)

```
┌────────────────────────┐    ┌────────────────────────┐
│   POS Desktop Client   │    │   POS Tablet Client    │
│   (Windows.Forms)      │    │   (Future: MAUI)       │
└───────────┬────────────┘    └───────────┬────────────┘
            │                             │
            └──────────┬──────────────────┘
                       │ HTTP (localhost)
            ┌──────────▼──────────┐
            │   StoreHub API      │
            │  (Windows Service)  │
            │   ASP.NET Core      │
            └──────────┬──────────┘
                       │
            ┌──────────▼──────────┐
            │  Local PostgreSQL   │
            │    (per store)      │
            └──────────┬──────────┘
                       │
            ┌──────────▼──────────┐
            │   Outbox Table +    │
            │   SyncWorker        │
            └──────────┬──────────┘
                       │ HTTPS (when online)
            ┌──────────▼──────────┐
            │    Cloud API        │
            │   (Singapore)       │
            └──────────┬──────────┘
                       │
            ┌──────────▼──────────┐
            │ Central PostgreSQL  │
            │   (all stores)      │
            └─────────────────────┘
```

## Key Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Local DB | PostgreSQL | 2+ terminals need concurrent writes |
| Identifier | GUID (PublicId) | Distributed identity across stores |
| Hub Hosting | Windows Service | Separate from UI, stable |
| Sync Pattern | Outbox + Background Worker | Reliable, retry-safe |
| Cloud Region | Singapore | Low latency for Thailand stores |

## Layer Responsibilities

### Domain Layer
- Pure business logic and entities
- No external dependencies
- Business rules and validation

### Application Layer
- CQRS with Nokpirab library
- Use cases (Commands & Queries)
- DTOs and mapping
- Service interfaces

### Infrastructure Layer
- Repository implementations
- External service adapters
- Database access (Dapper for SQLite, EF Core for Postgres)

### Presentation Layer
- Windows.Forms UI (current)
- Future: MAUI for cross-platform

## Store Identity

Each store has a unique identifier (`StoreId`) used for:
- Multi-store data partitioning
- Cloud sync attribution
- Cross-store reporting

See [Store Identity](store-identity.md) for details.
