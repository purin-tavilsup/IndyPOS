# Architecture Diagram

## System Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           STORE (Per Location)                              │
│                                                                             │
│  ┌─────────────────────┐     ┌─────────────────────┐                       │
│  │   Desktop App       │     │    Tablet App       │                       │
│  │  (Windows.Forms)    │     │   (Future: MAUI)    │                       │
│  └──────────┬──────────┘     └──────────┬──────────┘                       │
│             │ HTTP localhost            │ HTTP LAN                          │
│             └───────────────┬───────────┘                                   │
│                             ▼                                               │
│              ┌──────────────────────────────┐                              │
│              │    StoreHub (Windows Svc)    │                              │
│              │  ┌────────────────────────┐  │                              │
│              │  │    ASP.NET Core API    │  │                              │
│              │  └───────────┬────────────┘  │                              │
│              │  ┌───────────┴────────────┐  │                              │
│              │  │      SyncWorker        │  │                              │
│              │  │  (BackgroundService)   │  │                              │
│              │  └───────────┬────────────┘  │                              │
│              └──────────────┼──────────────┘                               │
│                             │ EF Core                                       │
│                             ▼                                               │
│              ┌──────────────────────────────┐                              │
│              │      Local PostgreSQL        │                              │
│              │  ┌────────────────────────┐  │                              │
│              │  │     Outbox Table       │──┼──────┐                       │
│              │  └────────────────────────┘  │      │                       │
│              └──────────────────────────────┘      │                       │
└───────────────────────────────────────────────────┼───────────────────────┘
                                                    │ HTTPS (when online)
                                                    ▼
                              ┌──────────────────────────────┐
                              │     CLOUD (Singapore)        │
                              │  ┌────────────────────────┐  │
                              │  │      Cloud API         │  │
                              │  └───────────┬────────────┘  │
                              │              ▼               │
                              │  ┌────────────────────────┐  │
                              │  │  Central PostgreSQL    │  │
                              │  │    (all stores)        │  │
                              │  └────────────────────────┘  │
                              └──────────────────────────────┘
```

## Data Flow: Complete Sale

```
 POS Client          StoreHub API         Local Postgres        SyncWorker          Cloud API
     │                    │                     │                    │                   │
     │ POST /sales/complete                     │                    │                   │
     ├───────────────────>│                     │                    │                   │
     │                    │ BEGIN TRANSACTION   │                    │                   │
     │                    ├────────────────────>│                    │                   │
     │                    │ INSERT Invoice      │                    │                   │
     │                    ├────────────────────>│                    │                   │
     │                    │ INSERT Products     │                    │                   │
     │                    ├────────────────────>│                    │                   │
     │                    │ INSERT Payments     │                    │                   │
     │                    ├────────────────────>│                    │                   │
     │                    │ UPDATE Inventory    │                    │                   │
     │                    ├────────────────────>│                    │                   │
     │                    │ INSERT Outbox Event │                    │                   │
     │                    ├────────────────────>│                    │                   │
     │                    │ COMMIT              │                    │                   │
     │                    ├────────────────────>│                    │                   │
     │                    │                     │                    │                   │
     │  200 OK (InvoiceId)│                     │                    │                   │
     │<───────────────────┤                     │                    │                   │
     │                    │                     │                    │                   │
     │                    │                     │   (Background)     │                   │
     │                    │                     │ SELECT unsent      │                   │
     │                    │                     │<───────────────────┤                   │
     │                    │                     │                    │                   │
     │                    │                     │                    │ POST /sync/events │
     │                    │                     │                    ├──────────────────>│
     │                    │                     │                    │                   │
     │                    │                     │                    │    200 OK         │
     │                    │                     │                    │<──────────────────┤
     │                    │                     │                    │                   │
     │                    │                     │ UPDATE sent=true   │                   │
     │                    │                     │<───────────────────┤                   │
     │                    │                     │                    │                   │
```

## Component Relationships (Clean Architecture)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         PRESENTATION LAYER                                  │
│  ┌────────────────────────────────────────────────────────────────────┐    │
│  │                      Windows.Forms UI                               │    │
│  └────────────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────┬───────────────────────────────────┘
                                          │ Uses
                                          ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         APPLICATION LAYER                                   │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │
│  │   Commands   │  │   Queries    │  │   Handlers   │  │   Services   │   │
│  │ (CQRS Write) │  │ (CQRS Read)  │  │              │  │ (Interfaces) │   │
│  └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘   │
└─────────────────────────────────────────┬───────────────────────────────────┘
                                          │ Depends on
                                          ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                           DOMAIN LAYER                                      │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐                      │
│  │   Entities   │  │ Value Objects│  │Business Rules│                      │
│  │              │  │              │  │              │                      │
│  └──────────────┘  └──────────────┘  └──────────────┘                      │
└─────────────────────────────────────────────────────────────────────────────┘
                                          ▲
                                          │ Implements
┌─────────────────────────────────────────┴───────────────────────────────────┐
│                       INFRASTRUCTURE LAYER                                  │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐                      │
│  │ Repositories │  │   Services   │  │  Db Context  │                      │
│  │  (Dapper)    │  │  (External)  │  │ (EF Core)    │                      │
│  └──────────────┘  └──────────────┘  └──────────────┘                      │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Offline Resilience

```
                    ┌─────────────────────────────────────────┐
                    │             NORMAL OPERATION            │
                    │                                         │
    ┌───────────┐   │   ┌───────────┐       ┌───────────┐   │
    │  ONLINE   │───┼──>│Processing │──────>│  Syncing  │   │
    │           │   │   │           │<──────│           │   │
    └─────┬─────┘   │   └───────────┘       └───────────┘   │
          │         │                                        │
          │ Network │                                        │
          │  Lost   └────────────────────────────────────────┘
          │
          ▼
    ┌───────────┐   ┌─────────────────────────────────────────┐
    │  OFFLINE  │   │          OFFLINE OPERATION              │
    │           │───│                                         │
    └─────┬─────┘   │   ┌───────────┐       ┌───────────┐   │
          │         │   │Local Only │──────>│  Queued   │   │
          │ Network │   │           │<──────│ (Outbox)  │   │
          │Restored │   └───────────┘       └───────────┘   │
          │         │                                        │
          └─────────│   Sales continue normally.             │
                    │   Events queue in Outbox.              │
                    │   Sync resumes when online.            │
                    └─────────────────────────────────────────┘
```

## Terminal Concurrency (Multi-Terminal Safety)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           STORE (Single Location)                           │
│                                                                             │
│   ┌─────────────────┐                     ┌─────────────────┐              │
│   │  Terminal 1     │                     │  Terminal 2     │              │
│   │  (Desktop)      │                     │  (Tablet)       │              │
│   │                 │                     │                 │              │
│   │  Cart: 3 items  │                     │  Cart: 2 items  │              │
│   └────────┬────────┘                     └────────┬────────┘              │
│            │                                       │                        │
│            │ POST /sales/complete                  │ POST /sales/complete   │
│            │ (nearly same time)                    │                        │
│            │                                       │                        │
│            └───────────────────┬───────────────────┘                        │
│                                │                                            │
│                                ▼                                            │
│            ┌───────────────────────────────────────┐                       │
│            │          StoreHub API                 │                       │
│            │  ┌─────────────────────────────────┐  │                       │
│            │  │  PostgreSQL Transaction Lock    │  │                       │
│            │  │                                 │  │                       │
│            │  │  Terminal 1: BEGIN TX           │  │                       │
│            │  │    → Lock Product A row         │  │                       │
│            │  │    → Check stock: 5 available   │  │                       │
│            │  │    → Sell qty 2 ✓               │  │                       │
│            │  │    → Generate INV-001           │  │                       │
│            │  │  COMMIT                         │  │                       │
│            │  │                                 │  │                       │
│            │  │  Terminal 2: BEGIN TX           │  │                       │
│            │  │    → Lock Product A row         │  │                       │
│            │  │    → Check stock: 3 available   │  │  ← Updated!           │
│            │  │    → Sell qty 1 ✓               │  │                       │
│            │  │    → Generate INV-002           │  │  ← Sequential!        │
│            │  │  COMMIT                         │  │                       │
│            │  └─────────────────────────────────┘  │                       │
│            └───────────────────┬───────────────────┘                       │
│                                │                                            │
│                                ▼                                            │
│            ┌───────────────────────────────────────┐                       │
│            │          Local PostgreSQL             │                       │
│            │                                       │                       │
│            │  Product A: stock = 2 (was 5)        │                       │
│            │  Invoice INV-001: Terminal 1         │                       │
│            │  Invoice INV-002: Terminal 2         │                       │
│            │  Outbox: 2 events pending            │                       │
│            └───────────────────────────────────────┘                       │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

KEY SAFETY RULES:
  ✓ Both terminals call SAME StoreHub (not separate DBs)
  ✓ StoreHub generates invoice numbers (not terminals)
  ✓ PostgreSQL row locking prevents double-selling last item
  ✓ Stock calculated from movements, not "SET quantity = X"
```

## .NET Aspire Development Environment

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    DEVELOPER MACHINE (Local)                                │
│                                                                             │
│   $ dotnet run --project src/IndyPOS.AppHost                               │
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐  │
│   │                     IndyPOS.AppHost                                  │  │
│   │                   (Aspire Orchestrator)                              │  │
│   │                                                                      │  │
│   │  var builder = DistributedApplication.CreateBuilder(args);          │  │
│   │                                                                      │  │
│   │  var postgres = builder.AddPostgres("postgres");                    │  │
│   │  var storeDb = postgres.AddDatabase("indypos_store");               │  │
│   │  var cloudDb = postgres.AddDatabase("indypos_cloud");               │  │
│   │                                                                      │  │
│   │  builder.AddProject<Projects.IndyPOS_StoreHub>("storehub")          │  │
│   │         .WithReference(storeDb);                                     │  │
│   │                                                                      │  │
│   │  builder.AddProject<Projects.IndyPOS_CloudApi>("cloud-api")         │  │
│   │         .WithReference(cloudDb);                                     │  │
│   │                                                                      │  │
│   │  builder.AddProject<Projects.IndyPOS_SyncWorker>("sync-worker")     │  │
│   │         .WithReference(storeDb)                                      │  │
│   │         .WithReference(cloudDb);                                     │  │
│   └─────────────────────────────────────────────────────────────────────┘  │
│                                    │                                        │
│            ┌───────────────────────┼───────────────────────┐               │
│            │                       │                       │               │
│            ▼                       ▼                       ▼               │
│   ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐       │
│   │    StoreHub     │    │    CloudApi     │    │   SyncWorker    │       │
│   │   :5001         │    │   :5002         │    │   (Background)  │       │
│   │                 │    │                 │    │                 │       │
│   │ POST /sales     │    │ POST /sync      │    │ Poll Outbox     │       │
│   │ GET /products   │    │ GET /master     │    │ Push to Cloud   │       │
│   │ GET /health     │    │ GET /health     │    │ Retry on fail   │       │
│   └────────┬────────┘    └────────┬────────┘    └────────┬────────┘       │
│            │                      │                      │                 │
│            │ ServiceDefaults      │ ServiceDefaults      │ ServiceDefaults │
│            │ (Health, OTel)       │ (Health, OTel)       │ (Health, OTel)  │
│            │                      │                      │                 │
│            └──────────────────────┼──────────────────────┘                 │
│                                   │                                        │
│                                   ▼                                        │
│            ┌───────────────────────────────────────┐                       │
│            │         PostgreSQL Container          │                       │
│            │              :5432                    │                       │
│            │  ┌─────────────┐  ┌─────────────┐    │                       │
│            │  │indypos_store│  │indypos_cloud│    │                       │
│            │  └─────────────┘  └─────────────┘    │                       │
│            └───────────────────────────────────────┘                       │
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐  │
│   │                   Aspire Dashboard                                   │  │
│   │                  http://localhost:15000                              │  │
│   │  ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌────────────┐       │  │
│   │  │  Traces    │ │  Metrics   │ │   Logs     │ │  Health    │       │  │
│   │  │            │ │            │ │            │ │            │       │  │
│   │  │ Request    │ │ CPU/Memory │ │ Structured │ │ All green  │       │  │
│   │  │ waterfall  │ │ charts     │ │ JSON logs  │ │ checks     │       │  │
│   │  └────────────┘ └────────────┘ └────────────┘ └────────────┘       │  │
│   └─────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

ASPIRE BENEFITS:
  ✓ One command starts everything: dotnet run --project src/IndyPOS.AppHost
  ✓ Auto-wires connection strings between services
  ✓ Built-in dashboard for traces, metrics, logs
  ✓ Health checks out of the box
  ✓ No Docker knowledge needed for devs
```

## Solution Structure (Project Dependencies)

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

DEPENDENCY RULES:
  ✓ Domain has NO dependencies (pure business logic)
  ✓ Application depends only on Domain
  ✓ Infrastructure implements Application interfaces
  ✓ Services (StoreHub, CloudApi, SyncWorker) depend on Application + Infrastructure
  ✓ AppHost references runnable projects only (no code dependency)
```
