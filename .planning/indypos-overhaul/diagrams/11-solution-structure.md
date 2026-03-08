# Solution Structure (Project Dependencies)

Version: 1.0.0
Date: 2026-03-08

## Overview

This diagram shows the complete IndyPOS solution structure with all project dependencies.

## Project Dependency Graph

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         IndyPOS Solution                                    │
└─────────────────────────────────────────────────────────────────────────────┘

                    ┌───────────────────────────────────┐
                    │       IndyPOS.AppHost             │
                    │      (Aspire Orchestrator)        │
                    │                                   │
                    │  References all runnable projects │
                    └───────────────────┬───────────────┘
                                        │
           ┌────────────────────────────┼────────────────────────────┐
           │                            │                            │
           ▼                            ▼                            ▼
┌─────────────────────┐    ┌─────────────────────┐    ┌─────────────────────┐
│  IndyPOS.StoreHub   │    │  IndyPOS.CloudApi   │    │ IndyPOS.SyncWorker  │
│   (ASP.NET Core)    │    │   (ASP.NET Core)    │    │ (BackgroundService) │
│                     │    │                     │    │                     │
│ • /sales/complete   │    │ • /sync/events      │    │ • Poll outbox       │
│ • /products         │    │ • /master/products  │    │ • Push to cloud     │
│ • /health           │    │ • /health           │    │ • Retry logic       │
└──────────┬──────────┘    └──────────┬──────────┘    └──────────┬──────────┘
           │                          │                          │
           │                          │                          │
           └──────────────────────────┼──────────────────────────┘
                                      │
                          ┌───────────┴───────────┐
                          │                       │
                          ▼                       ▼
           ┌─────────────────────┐    ┌─────────────────────┐
           │IndyPOS.ServiceDefaults│  │ IndyPOS.Infrastructure│
           │   (Aspire Shared)    │    │    (EF Core, Repos)  │
           │                      │    │                      │
           │ • Health checks      │    │ • StoreHubDbContext  │
           │ • OpenTelemetry      │    │ • CloudDbContext     │
           │ • Service conventions│    │ • Repositories       │
           └──────────────────────┘    │ • CloudSyncClient    │
                                       └──────────┬───────────┘
                                                  │
                                                  ▼
                                    ┌─────────────────────┐
                                    │ IndyPOS.Application │
                                    │   (Use Cases)       │
                                    │                     │
                                    │ • Commands/Queries  │
                                    │ • Handlers          │
                                    │ • Interfaces        │
                                    │ • DTOs              │
                                    └──────────┬──────────┘
                                               │
                                               ▼
                                    ┌─────────────────────┐
                                    │   IndyPOS.Domain    │
                                    │   (Core Entities)   │
                                    │                     │
                                    │ • Invoice, Payment  │
                                    │ • Product           │
                                    │ • OutboxEvent       │
                                    │ • Business Rules    │
                                    └─────────────────────┘
```

## Legacy Migration Path

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       LEGACY (During Migration)                             │
│                                                                             │
│   ┌─────────────────────┐                                                  │
│   │IndyPOS.Windows.Forms│ ─────────────────────────────────────────────┐   │
│   │  (Desktop POS UI)   │                                               │   │
│   │                     │   Currently: Direct DB access                 │   │
│   │ • Sale screens      │   Future: HTTP client to StoreHub            │   │
│   │ • Inventory mgmt    │                                               │   │
│   │ • Reports           │ ──► IndyPOS.Application                       │   │
│   └─────────────────────┘ ──► IndyPOS.Infrastructure                    │   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

Migration Phases:
─────────────────

Phase 1 (Current):
  Windows.Forms → Application → Infrastructure → SQLite
                                              ↘ PostgreSQL (StoreHub)

Phase 2 (Epic G):
  Windows.Forms → HTTP Client → StoreHub API → PostgreSQL

Phase 3 (Future):
  Windows.Forms decommissioned
  New MAUI App → StoreHub API → PostgreSQL
```

## Dependency Rules

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         DEPENDENCY RULES                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ✓ Domain has NO dependencies (pure business logic)                        │
│                                                                             │
│  ✓ Application depends only on Domain                                      │
│                                                                             │
│  ✓ Infrastructure implements Application interfaces                        │
│                                                                             │
│  ✓ Services depend on Application + Infrastructure                         │
│    (StoreHub, CloudApi, SyncWorker)                                        │
│                                                                             │
│  ✓ AppHost references runnable projects only (no code dependency)          │
│                                                                             │
│  ✓ ServiceDefaults has no project dependencies                             │
│    (only NuGet packages)                                                    │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Folder Structure

```
IndyPOS/
├── src/
│   ├── IndyPOS.Domain/              ← Core entities, no dependencies
│   │   └── Entities/
│   │       ├── Invoice.cs
│   │       ├── Product.cs
│   │       └── Core/                ← New UUID-based entities
│   │
│   ├── IndyPOS.Application/         ← Use cases, interfaces
│   │   ├── UseCases/
│   │   ├── Abstractions/
│   │   └── Features/
│   │
│   ├── IndyPOS.Infrastructure/      ← EF Core, repositories
│   │   ├── Persistence/
│   │   │   ├── StoreHub/            ← StoreHubDbContext
│   │   │   └── Repositories/
│   │   └── Services/
│   │
│   ├── IndyPOS.Windows.Forms/       ← Legacy desktop UI
│   │
│   ├── IndyPOS.StoreHub/            ← Local API (Epic C)
│   │   ├── Controllers/
│   │   └── Program.cs
│   │
│   ├── IndyPOS.CloudApi/            ← Cloud API (Epic F)
│   │   ├── Controllers/
│   │   └── Program.cs
│   │
│   ├── IndyPOS.SyncWorker/          ← Background sync (Epic E)
│   │   └── Program.cs
│   │
│   ├── IndyPOS.AppHost/             ← Aspire orchestrator
│   │   └── Program.cs
│   │
│   └── IndyPOS.ServiceDefaults/     ← Shared Aspire config
│       └── Extensions.cs
│
├── tests/
│   ├── IndyPOS.Application.Tests/
│   ├── IndyPOS.Domain.Tests/
│   └── IndyPOS.Infrastructure.Tests/
│
├── docs/
│   └── diagrams/
│
└── .planning/
    └── indypos-overhaul/
        ├── diagrams/                ← You are here
        └── implementation-status.md
```

## Project References Summary

```
┌────────────────────────┬────────────────────────────────────────────────────┐
│ Project                │ References                                         │
├────────────────────────┼────────────────────────────────────────────────────┤
│ IndyPOS.Domain         │ (none)                                             │
├────────────────────────┼────────────────────────────────────────────────────┤
│ IndyPOS.Application    │ IndyPOS.Domain                                     │
├────────────────────────┼────────────────────────────────────────────────────┤
│ IndyPOS.Infrastructure │ IndyPOS.Domain                                     │
│                        │ IndyPOS.Application                                │
├────────────────────────┼────────────────────────────────────────────────────┤
│ IndyPOS.StoreHub       │ IndyPOS.Application                                │
│                        │ IndyPOS.Infrastructure                             │
│                        │ IndyPOS.ServiceDefaults                            │
├────────────────────────┼────────────────────────────────────────────────────┤
│ IndyPOS.CloudApi       │ IndyPOS.Application                                │
│                        │ IndyPOS.Infrastructure                             │
│                        │ IndyPOS.ServiceDefaults                            │
├────────────────────────┼────────────────────────────────────────────────────┤
│ IndyPOS.SyncWorker     │ IndyPOS.Application                                │
│                        │ IndyPOS.Infrastructure                             │
│                        │ IndyPOS.ServiceDefaults                            │
├────────────────────────┼────────────────────────────────────────────────────┤
│ IndyPOS.Windows.Forms  │ IndyPOS.Application                                │
│                        │ IndyPOS.Infrastructure                             │
├────────────────────────┼────────────────────────────────────────────────────┤
│ IndyPOS.ServiceDefaults│ (none - only NuGet packages)                       │
├────────────────────────┼────────────────────────────────────────────────────┤
│ IndyPOS.AppHost        │ IndyPOS.StoreHub (project reference only)          │
│                        │ IndyPOS.CloudApi (project reference only)          │
│                        │ IndyPOS.SyncWorker (project reference only)        │
└────────────────────────┴────────────────────────────────────────────────────┘
```

---

**Related:**
- `03-component-relationships.md` - Clean Architecture details
- `10-aspire-dev-environment.md` - Aspire setup
- `IndyPOS_Docs_v1_4_0/docs/solution-structure/recommended-layout.md` - Full spec
