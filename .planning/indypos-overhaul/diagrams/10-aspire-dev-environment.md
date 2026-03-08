# .NET Aspire Development Environment

Version: 1.0.0
Date: 2026-03-08

## Overview

IndyPOS uses .NET Aspire for local development orchestration. One command starts everything.

## Aspire Architecture

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
```

## Benefits

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         ASPIRE BENEFITS                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ✓ One command starts everything                                           │
│      dotnet run --project src/IndyPOS.AppHost                              │
│                                                                             │
│  ✓ Auto-wires connection strings between services                          │
│      No manual config needed                                                │
│                                                                             │
│  ✓ Built-in dashboard for traces, metrics, logs                            │
│      http://localhost:15000                                                 │
│                                                                             │
│  ✓ Health checks out of the box                                            │
│      Via ServiceDefaults                                                    │
│                                                                             │
│  ✓ No Docker knowledge needed for devs                                     │
│      Aspire handles containers automatically                                │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## ServiceDefaults Project

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    IndyPOS.ServiceDefaults                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  public static class Extensions                                             │
│  {                                                                          │
│      public static TBuilder AddIndyServiceDefaults<TBuilder>(               │
│          this TBuilder builder)                                             │
│          where TBuilder : IHostApplicationBuilder                           │
│      {                                                                      │
│          // OpenTelemetry                                                   │
│          builder.Services.AddOpenTelemetry()                                │
│              .WithTracing(tracing => { ... })                               │
│              .WithMetrics(metrics => { ... });                              │
│                                                                             │
│          // Health checks                                                   │
│          builder.Services.AddHealthChecks();                                │
│                                                                             │
│          // Service discovery                                               │
│          builder.Services.AddServiceDiscovery();                            │
│                                                                             │
│          return builder;                                                    │
│      }                                                                      │
│  }                                                                          │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Development vs Production

```
┌─────────────────────────────────┐     ┌─────────────────────────────────┐
│        DEVELOPMENT              │     │        PRODUCTION               │
│        (Aspire)                 │     │        (Docker)                 │
├─────────────────────────────────┤     ├─────────────────────────────────┤
│                                 │     │                                 │
│  Start command:                 │     │  Start command:                 │
│  dotnet run --project AppHost  │     │  docker-compose up -d           │
│                                 │     │                                 │
│  Database:                      │     │  Database:                      │
│  Auto-provisioned container     │     │  Managed PostgreSQL             │
│                                 │     │  (DigitalOcean)                 │
│                                 │     │                                 │
│  Observability:                 │     │  Observability:                 │
│  Aspire Dashboard               │     │  Grafana / Loki / etc.          │
│                                 │     │                                 │
│  Use for:                       │     │  Use for:                       │
│  • Local development            │     │  • Production runtime           │
│  • Integration testing          │     │  • Deployment                   │
│  • Debugging                    │     │  • Scaling                      │
│                                 │     │                                 │
└─────────────────────────────────┘     └─────────────────────────────────┘
```

## Developer Workflow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        DAILY WORKFLOW                                       │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  1. Start everything                                                        │
│     ┌──────────────────────────────────────────────────────────────────┐   │
│     │ $ dotnet run --project src/IndyPOS.AppHost                       │   │
│     └──────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  2. Open dashboard                                                          │
│     ┌──────────────────────────────────────────────────────────────────┐   │
│     │ http://localhost:15000                                            │   │
│     └──────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  3. Test endpoints                                                          │
│     ┌──────────────────────────────────────────────────────────────────┐   │
│     │ StoreHub:  http://localhost:5001/health                          │   │
│     │ CloudApi:  http://localhost:5002/health                          │   │
│     └──────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  4. View traces                                                             │
│     ┌──────────────────────────────────────────────────────────────────┐   │
│     │ Dashboard → Traces → Click any request → See full waterfall      │   │
│     └──────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  5. Stop                                                                    │
│     ┌──────────────────────────────────────────────────────────────────┐   │
│     │ Ctrl+C                                                            │   │
│     └──────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Project Setup Commands

```bash
# Create Aspire projects
dotnet new aspire-apphost -n IndyPOS.AppHost
dotnet new aspire-servicedefaults -n IndyPOS.ServiceDefaults

# Add to solution
dotnet sln add src/IndyPOS.AppHost
dotnet sln add src/IndyPOS.ServiceDefaults

# Add project references in AppHost
dotnet add src/IndyPOS.AppHost reference src/IndyPOS.StoreHub
dotnet add src/IndyPOS.AppHost reference src/IndyPOS.CloudApi
dotnet add src/IndyPOS.AppHost reference src/IndyPOS.SyncWorker

# Add ServiceDefaults to each service
dotnet add src/IndyPOS.StoreHub reference src/IndyPOS.ServiceDefaults
dotnet add src/IndyPOS.CloudApi reference src/IndyPOS.ServiceDefaults
dotnet add src/IndyPOS.SyncWorker reference src/IndyPOS.ServiceDefaults
```

---

**Related:**
- `11-solution-structure.md` - Project dependencies
- `IndyPOS_Docs_v1_4_0/docs/architecture/aspire.md` - Full Aspire spec
- `IndyPOS_Docs_v1_4_0/docs/decisions/ADR-002-adopt-aspire-for-development.md` - ADR
