# Architecture Overview

## System Architecture (Current)

IndyPOS is a Point-of-Sale system designed for small retail stores with offline-first capabilities and cloud synchronization.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           STORE LOCATION                                     │
│                                                                              │
│   ┌─────────────────┐     ┌─────────────────┐                               │
│   │   Desktop POS   │     │  Avalonia POS   │                               │
│   │  (WinForms)     │     │  (Future)       │                               │
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
| Till ViewModels | CommunityToolkit.Mvvm (`IndyPOS.Presentation`) | UI logic with no WinForms, for the planned Avalonia port |
| Local API | ASP.NET Core Minimal APIs | StoreHub service |
| Local Database | PostgreSQL 18 + EF Core | Transactional data |
| Cloud API | ASP.NET Core | Central sync endpoint |
| Cloud Database | PostgreSQL (Managed) | Aggregated data |
| Dev Orchestration | .NET Aspire | Local development |
| Auth | JWT + BCrypt + RBAC (StoreHub); OpenIddict client credentials (CloudApi) | Security |
| Secrets | DPAPI (`IndyPOS.Vault`) | Connection string and JWT key at rest |
| Installer | Bootstrapper + Velopack | Fresh install and in-place upgrade |
| CQRS | Nokpirab | Command/Query separation |

## Clean Architecture Layers

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         PRESENTATION LAYER                                   │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │  Windows.Forms  │   StoreHub API   │   CloudApi   │   Presentation  │    │
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
│   ├── IndyPOS.Windows.Forms/    # Desktop UI (the till)
│   ├── IndyPOS.Presentation/     # Till ViewModels, no WinForms
│   ├── IndyPOS.Vault/            # DPAPI secret protection
│   ├── IndyPOS.MigrationTool/    # v3 SQLite -> PostgreSQL
│   ├── IndyPOS.AppHost/          # Aspire orchestrator
│   ├── IndyPOS.ServiceDefaults/  # Shared health checks, telemetry
│   └── IndyPOS.StoreProfiles/    # Dev store profiles (--store)
│
├── installer/
│   └── IndyPOS.Bootstrapper/     # Fresh install + in-place upgrade
│
├── tests/                        # One suite per project (see ONBOARDING.md for counts)
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
                                    │ (12 h expiry)   │
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
| **InventoryMovement** | Stock change | `ProductId`, `QuantityDelta`, `Reason`, `CreatedByUserId` |
| **StoreUser** | System user | `Username`, `PasswordHash`, `RoleId` |
| **PayLater** | ลงบัญชี debt (1:1 with its payment) | `PayLaterAmount`, `PaidAmount`, `IsCompleted`, `LegacyPaymentId` |
| **PayLaterRepayment** | One repayment, append-only | `PayLaterId`, `Amount`, `BusinessDate`, `CreatedByUserId` |
| **CashPayout / CashFloat / DebtRepayment** | Cash drawer entries (soft-deleted) | `Amount`, `BusinessDate`, `IsDeleted` |
| **CashCount** | A count of the drawer, append-only | nine denomination counts, `BusinessDate` |
| **InvoiceReprint** | Reprint audit | `InvoiceId`, `CreatedByUserId` |
| **PaymentMethod** | A store's payment methods | `StoreId`, `Code`, `IsEnabled` |
| **OutboxEvent** | Sync queue | `Type`, `PayloadJson`, `Status`, `Attempts` |

### ID Strategy

- **Guid IDs**: Used for all new entities (distributed-safe)
- **Legacy int IDs**: kept as `Legacy*Id` columns on rows migrated from v3, for traceability, nullable on most
  tables. `StoreUser.LegacyUserId` is the exception: a required, unique `int`. The v3 SQLite database is read only
  by the MigrationTool

## Related Documentation

- [Diagrams](../diagrams/README.md) - All ASCII diagrams
- [Developer Guide](../development/getting-started.md) - Setup and testing
- [Operations Runbook](../operations/RUNBOOK.md) - Production operations
- [Store Identity](store-identity.md) - Multi-store concepts
