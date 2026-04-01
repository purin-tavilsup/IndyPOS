# Solution Structure (Project Dependencies)

Version: 2.0.0
Date: 2026-03-31
Status: ✅ Updated for Epic H completion (15 projects)

## Overview

This diagram shows the complete IndyPOS solution structure with all project dependencies, organized into logical solution folders.

## Solution Folder Structure

```
📁 IndyPOS.sln
│
├── 📁 Core (Clean Architecture layers)
│   ├── IndyPOS.Domain           ← Entities, value objects, business rules
│   ├── IndyPOS.Application      ← Use cases (CQRS), interfaces, DTOs
│   └── IndyPOS.Infrastructure   ← EF Core, repositories, external services
│
├── 📁 DesktopApp
│   └── IndyPOS.Windows.Forms    ← Legacy WinForms POS (migrating to StoreHub client)
│
├── 📁 Services
│   ├── IndyPOS.StoreHub         ← Local API service (ASP.NET Core)
│   └── IndyPOS.CloudApi         ← Central cloud API (ASP.NET Core)
│
├── 📁 DevAppHost (.NET Aspire)
│   ├── IndyPOS.AppHost          ← Aspire orchestrator
│   └── IndyPOS.ServiceDefaults  ← Shared health checks, OpenTelemetry
│
├── 📁 Tools
│   └── IndyPOS.MigrationTool    ← SQLite → PostgreSQL migration CLI
│
└── 📁 Tests
    ├── 📁 Core
    │   └── IndyPOS.Application.Tests       ← Unit tests (202 tests)
    ├── 📁 DesktopApp
    │   └── IndyPOS.Windows.Forms.Tests     ← WinForms unit tests
    ├── 📁 Services
    │   └── IndyPOS.StoreHub.IntegrationTests ← API integration tests (49 tests)
    ├── 📁 Tools
    │   ├── IndyPOS.Migration.Tests         ← Migration verification tests (15 tests)
    │   └── IndyPOS.MigrationTool.Tests     ← Migration tool tests (23 tests)
    └── IndyPOS.Mock                         ← Test doubles and fakes
```

**Total: 15 projects | 266+ tests**

---

## Project Dependency Graph

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         IndyPOS Solution (15 projects)                       │
└─────────────────────────────────────────────────────────────────────────────┘

                    ┌───────────────────────────────────┐
                    │       IndyPOS.AppHost             │
                    │      (Aspire Orchestrator)        │
                    │                                   │
                    │  • PostgreSQL container           │
                    │  • PgAdmin (on-demand)            │
                    │  • DbGate (on-demand)             │
                    └───────────────────┬───────────────┘
                                        │
           ┌────────────────────────────┼────────────────────────────┐
           │                            │                            │
           ▼                            ▼                            ▼
┌─────────────────────┐    ┌─────────────────────┐    ┌─────────────────────┐
│  IndyPOS.StoreHub   │    │  IndyPOS.CloudApi   │    │IndyPOS.MigrationTool│
│   (ASP.NET Core)    │    │   (ASP.NET Core)    │    │   (Console App)     │
│                     │    │                     │    │                     │
│ Endpoints:          │    │ Endpoints:          │    │ • SQLite → Postgres │
│ • POST /auth/login  │    │ • POST /oauth/token │    │ • Products          │
│ • GET  /products    │    │ • POST /sync/events │    │ • Invoices          │
│ • POST /products    │    │ • GET  /master/*    │    │ • Users             │
│ • POST /sales/complete│  │ • CRUD /admin/users │    │ • Thai locale       │
│ • GET  /reports/*   │    │ • /admin/stores     │    └──────────┬──────────┘
│ • GET  /sync/status │    └──────────┬──────────┘               │
└──────────┬──────────┘               │                          │
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
           │ • Service discovery  │    │ • All repositories   │
           │ • HTTP resilience    │    │ • DpapiSecretStorage │
           └──────────────────────┘    │ • CloudSyncClient    │
                                       └──────────┬───────────┘
                                                  │
                                                  ▼
                                    ┌─────────────────────┐
                                    │ IndyPOS.Application │
                                    │   (Use Cases)       │
                                    │                     │
                                    │ • Commands/Queries  │
                                    │ • Handlers (CQRS)   │
                                    │ • Interfaces        │
                                    │ • DTOs              │
                                    │ • Authorization     │
                                    └──────────┬──────────┘
                                               │
                                               ▼
                                    ┌─────────────────────┐
                                    │   IndyPOS.Domain    │
                                    │   (Core Entities)   │
                                    │                     │
                                    │ • Invoice, Payment  │
                                    │ • Product, PayLater │
                                    │ • OutboxEvent       │
                                    │ • CloudUser         │
                                    │ • Business Rules    │
                                    └─────────────────────┘
```

---

## Desktop Client Architecture (Epic G)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       DESKTOP CLIENT (Epic G - Complete)                     │
│                                                                             │
│   ┌─────────────────────┐                                                  │
│   │IndyPOS.Windows.Forms│                                                  │
│   │  (Desktop POS UI)   │                                                  │
│   │                     │                                                  │
│   │ • Sale screens      │───► IStoreHubClient ───► StoreHub API            │
│   │ • Inventory mgmt    │───► IProductCacheService (in-memory)             │
│   │ • Reports           │───► IInventoryProductService                     │
│   │ • User login        │───► IUserLogInService                            │
│   └─────────────────────┘                                                  │
│              │                                                              │
│              ▼                                                              │
│   ┌─────────────────────────────────────────────┐                          │
│   │  StoreHub HTTP Client Flow                  │                          │
│   │                                             │                          │
│   │  Login → JWT Token → Cached in memory       │                          │
│   │  Products → Synced to IProductCacheService  │                          │
│   │  Sales → POST /sales/complete               │                          │
│   │  Inventory → POST /products/adjust-quantity │                          │
│   └─────────────────────────────────────────────┘                          │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

Migration Status:
─────────────────
✅ Phase 1 (Complete): StoreHub Product Write API
✅ Phase 2 (Complete): IStoreHubClient + cache invalidation
✅ Phase 3 (Complete): Guid ID migration
✅ Phase 4-5 (Complete): IInventoryProductService + WinForms migration
✅ Phase 6-7 (Complete): EF Core migrations + unit tests
⏭️ G2 (Deferred): Tablet support (post-MAUI, 2027+)
```

---

## Test Project Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         TEST PROJECTS (266+ tests)                           │
└─────────────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────┐    ┌──────────────────────────────┐
│ IndyPOS.Application.Tests    │    │ IndyPOS.StoreHub.Integration │
│        (202 tests)           │    │        Tests (49 tests)      │
│                              │    │                              │
│ • Unit tests                 │    │ • WebApplicationFactory      │
│ • EF Core InMemory           │    │ • Testcontainers PostgreSQL  │
│ • Moq for mocking            │    │ • Respawn for isolation      │
│ • xUnit                      │    │ • Real API testing           │
└──────────────────────────────┘    └──────────────────────────────┘

┌──────────────────────────────┐    ┌──────────────────────────────┐
│ IndyPOS.Migration.Tests      │    │ IndyPOS.MigrationTool.Tests  │
│        (15 tests)            │    │        (23 tests)            │
│                              │    │                              │
│ • SQLite source fixture      │    │ • End-to-end migration       │
│ • PostgreSQL target          │    │ • Thai locale support        │
│ • Product/Invoice migration  │    │ • Bogus fake data            │
└──────────────────────────────┘    └──────────────────────────────┘

┌──────────────────────────────┐    ┌──────────────────────────────┐
│ IndyPOS.Windows.Forms.Tests  │    │ IndyPOS.Mock                 │
│                              │    │                              │
│ • WinForms component tests   │    │ • Shared test doubles        │
│ • Service unit tests         │    │ • Fake repositories          │
└──────────────────────────────┘    └──────────────────────────────┘
```

---

## Dependency Rules

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         DEPENDENCY RULES                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ✅ Domain has NO dependencies (pure business logic)                        │
│                                                                             │
│  ✅ Application depends only on Domain                                      │
│                                                                             │
│  ✅ Infrastructure implements Application interfaces                        │
│                                                                             │
│  ✅ Services (StoreHub, CloudApi) depend on Application + Infrastructure   │
│                                                                             │
│  ✅ Desktop (Windows.Forms) depends on Application + Infrastructure        │
│     └─► Uses IStoreHubClient for API communication                         │
│                                                                             │
│  ✅ AppHost references runnable projects only (no code dependency)          │
│                                                                             │
│  ✅ ServiceDefaults has no project dependencies (only NuGet packages)       │
│                                                                             │
│  ✅ Test projects reference their target + Mock project                     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## File System Layout

```
IndyPOS/
├── src/
│   ├── IndyPOS.Domain/              ← Core entities, no dependencies
│   │   └── Entities/
│   │       ├── Invoice.cs, Product.cs, Payment.cs
│   │       ├── Core/                ← New UUID-based entities (StoreHub)
│   │       └── Cloud/               ← Cloud-specific entities
│   │
│   ├── IndyPOS.Application/         ← Use cases, interfaces
│   │   ├── UseCases/
│   │   │   ├── StoreHub/            ← StoreHub commands/queries
│   │   │   └── Cloud/               ← CloudApi commands/queries
│   │   ├── Abstractions/            ← Interfaces (repositories, services)
│   │   ├── Common/
│   │   │   └── Authorization/       ← RBAC capabilities, policies
│   │   └── DTOs/
│   │
│   ├── IndyPOS.Infrastructure/      ← EF Core, repositories, services
│   │   ├── Persistence/
│   │   │   ├── StoreHub/            ← StoreHubDbContext + configs
│   │   │   ├── Cloud/               ← CloudDbContext + configs
│   │   │   └── Repositories/
│   │   └── Services/
│   │       ├── StoreHub/            ← SyncWorker, CloudTokenService
│   │       └── Security/            ← DpapiSecretStorage
│   │
│   ├── IndyPOS.Windows.Forms/       ← Desktop UI
│   │   ├── Panels/                  ← Main UI panels
│   │   ├── Forms/                   ← Dialog forms
│   │   └── Services/                ← StoreHub client services
│   │
│   ├── IndyPOS.StoreHub/            ← Local API (ASP.NET Core)
│   │   ├── Controllers/             ← API endpoints
│   │   ├── Infrastructure/          ← Auth, seeding
│   │   └── Program.cs
│   │
│   ├── IndyPOS.CloudApi/            ← Cloud API (ASP.NET Core)
│   │   ├── Controllers/             ← Sync, master data, admin
│   │   ├── Infrastructure/          ← OpenIddict auth
│   │   └── Program.cs
│   │
│   ├── IndyPOS.MigrationTool/       ← SQLite → PostgreSQL CLI
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
│   ├── IndyPOS.Windows.Forms.Tests/
│   ├── IndyPOS.StoreHub.IntegrationTests/
│   ├── IndyPOS.Migration.Tests/
│   ├── IndyPOS.MigrationTool.Tests/
│   └── IndyPOS.Mock/
│
├── docs/
│   ├── architecture/
│   ├── diagrams/
│   └── operations/                  ← Runbook, pilot checklist
│
└── .planning/
    └── indypos-overhaul/
        ├── diagrams/                ← You are here
        ├── security/
        └── implementation-status.md
```

---

## Project References Summary

```
┌────────────────────────────────┬────────────────────────────────────────────┐
│ Project                        │ References                                 │
├────────────────────────────────┼────────────────────────────────────────────┤
│ IndyPOS.Domain                 │ (none)                                     │
├────────────────────────────────┼────────────────────────────────────────────┤
│ IndyPOS.Application            │ IndyPOS.Domain                             │
├────────────────────────────────┼────────────────────────────────────────────┤
│ IndyPOS.Infrastructure         │ IndyPOS.Domain                             │
│                                │ IndyPOS.Application                        │
├────────────────────────────────┼────────────────────────────────────────────┤
│ IndyPOS.StoreHub               │ IndyPOS.Application                        │
│                                │ IndyPOS.Infrastructure                     │
│                                │ IndyPOS.ServiceDefaults                    │
├────────────────────────────────┼────────────────────────────────────────────┤
│ IndyPOS.CloudApi               │ IndyPOS.Application                        │
│                                │ IndyPOS.Infrastructure                     │
│                                │ IndyPOS.ServiceDefaults                    │
├────────────────────────────────┼────────────────────────────────────────────┤
│ IndyPOS.Windows.Forms          │ IndyPOS.Application                        │
│                                │ IndyPOS.Infrastructure                     │
├────────────────────────────────┼────────────────────────────────────────────┤
│ IndyPOS.MigrationTool          │ IndyPOS.Domain                             │
│                                │ IndyPOS.Application                        │
│                                │ IndyPOS.Infrastructure                     │
├────────────────────────────────┼────────────────────────────────────────────┤
│ IndyPOS.ServiceDefaults        │ (none - only NuGet packages)               │
├────────────────────────────────┼────────────────────────────────────────────┤
│ IndyPOS.AppHost                │ IndyPOS.StoreHub (project ref only)        │
│                                │ IndyPOS.CloudApi (project ref only)        │
├────────────────────────────────┼────────────────────────────────────────────┤
│ IndyPOS.Application.Tests      │ IndyPOS.Application                        │
│                                │ IndyPOS.Infrastructure                     │
│                                │ IndyPOS.Mock                               │
├────────────────────────────────┼────────────────────────────────────────────┤
│ IndyPOS.StoreHub.IntegrationTests │ IndyPOS.StoreHub                        │
│                                │ IndyPOS.Mock                               │
├────────────────────────────────┼────────────────────────────────────────────┤
│ IndyPOS.Migration.Tests        │ IndyPOS.Application                        │
│                                │ IndyPOS.Infrastructure                     │
├────────────────────────────────┼────────────────────────────────────────────┤
│ IndyPOS.MigrationTool.Tests    │ IndyPOS.MigrationTool                      │
└────────────────────────────────┴────────────────────────────────────────────┘
```

---

## Key Package Dependencies

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    KEY NUGET PACKAGES (March 2026)                          │
├────────────────────────────┬────────────────┬───────────────────────────────┤
│ Package                    │ Version        │ Used In                       │
├────────────────────────────┼────────────────┼───────────────────────────────┤
│ Microsoft.EntityFrameworkCore │ 10.0.5      │ Infrastructure                │
│ Npgsql.EntityFrameworkCore │ 10.0.5         │ StoreHub, CloudApi            │
│ Microsoft.Extensions.*     │ 10.0.5         │ All projects                  │
│ Aspire.*                   │ 9.3.0          │ AppHost, ServiceDefaults      │
│ OpenIddict.*               │ 6.4.0          │ CloudApi                      │
│ BCrypt.Net-Next            │ 4.0.3          │ Auth (password hashing)       │
│ Testcontainers.PostgreSql  │ 4.3.0          │ Integration tests             │
│ Respawn                    │ 6.2.1          │ Test isolation                │
│ WireMock.Net               │ 1.6.9          │ E2E tests                     │
│ Bogus                      │ 35.6.1         │ Fake data generation          │
└────────────────────────────┴────────────────┴───────────────────────────────┘
```

---

**Related:**
- `03-component-relationships.md` - Clean Architecture details
- `10-aspire-dev-environment.md` - Aspire setup
- `12-security-auth-flow.md` - OAuth2 and RBAC flow
