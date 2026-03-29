# Architecture Overview

## System Architecture (Current)

IndyPOS is a Point-of-Sale system designed for small retail stores with offline-first capabilities and cloud synchronization.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           STORE LOCATION                                     │
│                                                                              │
│   ┌─────────────────┐     ┌─────────────────┐                               │
│   │   Desktop POS   │     │   Tablet POS    │                               │
│   │  (WinForms)     │     │  (Future: MAUI) │                               │
│   └────────┬────────┘     └────────┬────────┘                               │
│            │                       │                                         │
│            └───────────┬───────────┘                                         │
│                        │ HTTP (LAN)                                          │
│                        ▼                                                     │
│           ┌────────────────────────┐                                        │
│           │      StoreHub API      │ ← Windows Service                      │
│           │     (ASP.NET Core)     │   Port 5000                            │
│           │  ┌──────────────────┐  │                                        │
│           │  │   SyncWorker     │  │ ← Background sync                      │
│           │  │ (BackgroundSvc)  │  │                                        │
│           │  └──────────────────┘  │                                        │
│           └────────────┬───────────┘                                        │
│                        │ EF Core                                             │
│                        ▼                                                     │
│           ┌────────────────────────┐                                        │
│           │   Local PostgreSQL     │ ← Per-store database                   │
│           │  ┌──────────────────┐  │                                        │
│           │  │  Outbox Table    │──┼──────┐                                 │
│           │  └──────────────────┘  │      │                                 │
│           └────────────────────────┘      │                                 │
│                                           │                                  │
└───────────────────────────────────────────┼──────────────────────────────────┘
                                            │ HTTPS (when online)
                                            ▼
                           ┌────────────────────────────┐
                           │      CLOUD (Singapore)     │
                           │  ┌──────────────────────┐  │
                           │  │      Cloud API       │  │
                           │  │   (ASP.NET Core)     │  │
                           │  └──────────┬───────────┘  │
                           │             ▼              │
                           │  ┌──────────────────────┐  │
                           │  │  Central PostgreSQL  │  │
                           │  │    (all stores)      │  │
                           │  └──────────────────────┘  │
                           └────────────────────────────┘
```

## Key Design Principles

| Principle | Implementation |
|-----------|----------------|
| **Offline-First** | Sales work without internet; sync when online |
| **Single Source of Truth** | StoreHub PostgreSQL is authoritative for the store |
| **Reliable Sync** | Outbox pattern with exponential backoff retry |
| **Multi-Terminal Safe** | PostgreSQL transactions prevent race conditions |
| **Clean Architecture** | Domain → Application → Infrastructure layers |

## Technology Stack

| Component | Technology | Purpose |
|-----------|------------|---------|
| Desktop UI | Windows.Forms (.NET 10) | Current POS interface |
| Local API | ASP.NET Core Minimal APIs | StoreHub service |
| Local Database | PostgreSQL 16 + EF Core | Transactional data |
| Cloud API | ASP.NET Core | Central sync endpoint |
| Cloud Database | PostgreSQL (Managed) | Aggregated data |
| Dev Orchestration | .NET Aspire | Local development |
| Auth | JWT + BCrypt + RBAC | Security |
| CQRS | Nokpirab | Command/Query separation |

## Clean Architecture Layers

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         PRESENTATION LAYER                                   │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │  Windows.Forms  │   StoreHub API   │   CloudApi   │   (Future MAUI) │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │ Uses
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         APPLICATION LAYER                                    │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐    │
│  │   Commands   │  │   Queries    │  │   Handlers   │  │  Interfaces  │    │
│  │ CompleteSale │  │ GetProducts  │  │  (CQRS)      │  │ IRepository  │    │
│  │ CreateProduct│  │ GetInvoices  │  │              │  │ IService     │    │
│  └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘    │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │ Depends on
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         DOMAIN LAYER (No Dependencies)                       │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐    │
│  │   Entities   │  │Value Objects │  │Business Rules│  │   Enums      │    │
│  │   Invoice    │  │   Money      │  │  PayLater    │  │  PaymentType │    │
│  │   Product    │  │   Address    │  │  validation  │  │  UserRole    │    │
│  └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘    │
└─────────────────────────────────────────────────────────────────────────────┘
                                    ▲
                                    │ Implements
┌─────────────────────────────────────────────────────────────────────────────┐
│                         INFRASTRUCTURE LAYER                                 │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐    │
│  │  DbContext   │  │ Repositories │  │   Services   │  │ HTTP Clients │    │
│  │ StoreHubDb   │  │ Product      │  │ PasswordHash │  │ StoreHubHttp │    │
│  │ CloudDb      │  │ Invoice      │  │ TokenService │  │ CloudSync    │    │
│  └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘    │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Project Structure

```
IndyPOS/
├── src/
│   ├── IndyPOS.Domain/           # Entities, Business Rules
│   ├── IndyPOS.Application/      # Use Cases (Commands/Queries/Handlers)
│   ├── IndyPOS.Infrastructure/   # EF Core, Repositories, Services
│   ├── IndyPOS.StoreHub/         # Local API service
│   ├── IndyPOS.CloudApi/         # Cloud API service
│   ├── IndyPOS.Windows.Forms/    # Desktop UI
│   ├── IndyPOS.AppHost/          # Aspire orchestrator
│   └── IndyPOS.ServiceDefaults/  # Shared health checks, telemetry
│
├── tests/
│   ├── IndyPOS.Application.Tests/       # Unit tests (202 tests)
│   ├── IndyPOS.StoreHub.IntegrationTests/  # API integration tests (49 tests)
│   └── IndyPOS.Migration.Tests/         # Migration tests (15 tests)
│
└── docs/
    ├── architecture/             # This document
    ├── diagrams/                 # ASCII diagrams
    ├── development/              # Developer guide
    └── operations/               # Runbooks, troubleshooting
```

## Security Architecture

### Authentication Flow

```
┌───────────┐     POST /auth/login      ┌───────────┐
│  WinForms │ ─────────────────────────►│ StoreHub  │
│    POS    │     {username, password}  │    API    │
└───────────┘                           └─────┬─────┘
                                              │
                                              ▼
                                    ┌─────────────────┐
                                    │ BCrypt Verify   │
                                    │ (hashed in DB)  │
                                    └────────┬────────┘
                                              │
                                              ▼
                                    ┌─────────────────┐
                                    │ Generate JWT    │
                                    │ (15min expiry)  │
                                    └────────┬────────┘
                                              │
      ◄───────────────────────────────────────┘
      {token, user, capabilities}
```

### Role-Based Access Control (RBAC)

| Role | Capabilities | Typical User |
|------|--------------|--------------|
| **Cashier** | `products.read`, `sales.complete` | Store staff |
| **Manager** | + `sync.view_status`, `reports.view` | Store manager |
| **Admin** | + `users.*`, `products.manage`, `admin.*` | Owner/IT |

## Offline Resilience

```
                 ┌────────────────────────────────────────┐
                 │            ONLINE MODE                  │
                 │                                         │
  ┌────────┐     │   ┌─────────┐       ┌─────────┐        │
  │ ONLINE │─────┼──►│ Process │──────►│  Sync   │        │
  │        │     │   │  Sale   │       │ to Cloud│        │
  └───┬────┘     │   └─────────┘       └─────────┘        │
      │          └────────────────────────────────────────┘
      │ Network
      │  Lost
      ▼
  ┌────────┐     ┌────────────────────────────────────────┐
  │OFFLINE │     │           OFFLINE MODE                  │
  │        │─────│                                         │
  └───┬────┘     │   ┌─────────┐       ┌─────────┐        │
      │          │   │ Process │──────►│ Queue   │        │
      │ Network  │   │  Sale   │       │ Outbox  │        │
      │ Restored │   └─────────┘       └─────────┘        │
      ▼          │                                         │
  ┌────────┐     │   Sales continue normally.              │
  │ ONLINE │     │   Events queue in Outbox.               │
  │        │     │   Auto-sync when online.                │
  └────────┘     └────────────────────────────────────────┘
```

## Data Entities

### Core Entities

| Entity | Description | Key Fields |
|--------|-------------|------------|
| **Product** | Inventory item | `Id (Guid)`, `Barcode`, `UnitPrice`, `IsActive` |
| **Invoice** | Sale transaction | `Id`, `InvoiceNumber`, `TotalAmount`, `UserId` |
| **InvoiceLine** | Invoice line item | `ProductId`, `Quantity`, `UnitPrice` |
| **Payment** | Payment record | `Method`, `Amount`, `InvoiceId` |
| **InventoryMovement** | Stock change | `ProductId`, `Quantity`, `MovementType` |
| **StoreUser** | System user | `Username`, `PasswordHash`, `RoleId` |
| **OutboxEvent** | Sync queue | `EventType`, `Payload`, `SentAt` |

### ID Strategy

- **Guid IDs**: Used for all new entities (distributed-safe)
- **Legacy int IDs**: Still in WinForms SQLite (compatibility layer)

## Related Documentation

- [Diagrams](../diagrams/README.md) - All ASCII diagrams
- [Developer Guide](../development/getting-started.md) - Setup and testing
- [Operations Runbook](../operations/RUNBOOK.md) - Production operations
- [Store Identity](store-identity.md) - Multi-store concepts
