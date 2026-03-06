# Component Relationship Diagram

Version: 1.0.0
Date: 2026-03-03

## Clean Architecture Layers

```
┌─────────────────────────────────────────────────────────────────────┐
│                         PRESENTATION LAYER                          │
│                                                                     │
│  ┌──────────────────────┐       ┌──────────────────────┐          │
│  │  POS Client          │       │  StoreHub API        │          │
│  │  (Windows.Forms)     │◄─────►│  (ASP.NET Core)      │          │
│  │                      │       │                      │          │
│  │  • UI Forms          │       │  Controllers:        │          │
│  │  • ViewModels        │       │  • SalesController   │          │
│  │  • Validation        │       │  • ProductsController│          │
│  └──────────┬───────────┘       │  • InventoryController│         │
│             │                   │  • SyncController    │          │
│             │                   └──────────┬───────────┘          │
│             │                              │                       │
└─────────────┼──────────────────────────────┼───────────────────────┘
              │                              │
              │ Calls                        │ Calls
              │ (HTTP/LAN)                   │ (DI)
              │                              │
┌─────────────┼──────────────────────────────┼───────────────────────┐
│             │     APPLICATION LAYER        │                       │
│             │                              │                       │
│             │     ┌────────────────────────▼─────────────────┐    │
│             │     │   Use Cases (Command/Query Handlers)     │    │
│             │     │                                          │    │
│             │     │  Commands:                               │    │
│             │     │  • CompleteSaleCommandHandler            │    │
│             │     │  • AdjustInventoryCommandHandler         │    │
│             │     │  • CreateProductCommandHandler           │    │
│             │     │                                          │    │
│             │     │  Queries:                                │    │
│             │     │  • GetProductByIdQueryHandler            │    │
│             │     │  • GetStockLevelQueryHandler             │    │
│             │     │  • GetPendingSalesQueryHandler           │    │
│             │     └──────────────┬───────────────────────────┘    │
│             │                    │                                │
│             │                    │ Uses                           │
│             │                    │                                │
│             │     ┌──────────────▼───────────────────────────┐   │
│             │     │   Interfaces (Contracts)                 │   │
│             │     │                                          │   │
│             │     │  • IInvoiceRepository                    │   │
│             │     │  • IProductRepository                    │   │
│             │     │  • IInventoryMovementRepository          │   │
│             │     │  • IOutboxRepository                     │   │
│             │     │  • IStoreIdentityService                 │   │
│             │     │  • ICloudSyncService                     │   │
│             │     └──────────────────────────────────────────┘   │
│             │                                                     │
└─────────────┼─────────────────────────────────────────────────────┘
              │                     ▲
              │                     │ Implements
              │                     │
┌─────────────┼─────────────────────┼───────────────────────────────┐
│             │    DOMAIN LAYER     │                               │
│             │                     │                               │
│             │     ┌───────────────┴──────────────────────────┐   │
│             │     │   Entities (Aggregate Roots)             │   │
│             │     │                                          │   │
│             │     │  • Invoice (root)                        │   │
│             │     │    └─ InvoiceLine (child)                │   │
│             │     │    └─ Payment (child)                    │   │
│             │     │  • Product                               │   │
│             │     │  • InventoryMovement                     │   │
│             │     │  • Customer                              │   │
│             │     │                                          │   │
│             │     │  Value Objects:                          │   │
│             │     │  • Money                                 │   │
│             │     │  • ProductCode                           │   │
│             │     │  • StoreId                               │   │
│             │     └──────────────────────────────────────────┘   │
│             │                                                     │
│             │     ┌────────────────────────────────────────┐     │
│             │     │   Domain Services                      │     │
│             │     │                                        │     │
│             │     │  • PricingService                      │     │
│             │     │  • InventoryService                    │     │
│             │     │  • DiscountService                     │     │
│             │     └────────────────────────────────────────┘     │
│             │                                                     │
└─────────────┼─────────────────────────────────────────────────────┘
              │                     ▲
              │                     │ Implements & Uses
              │                     │
┌─────────────┼─────────────────────┼───────────────────────────────┐
│             │  INFRASTRUCTURE LAYER│                              │
│             │                     │                               │
│             │     ┌───────────────┴──────────────────────────┐   │
│             │     │   Data Access (EF Core)                  │   │
│             │     │                                          │   │
│             │     │  • StoreHubDbContext                     │   │
│             │     │  • InvoiceRepository                     │   │
│             │     │  • ProductRepository                     │   │
│             │     │  • InventoryMovementRepository           │   │
│             │     │  • OutboxRepository                      │   │
│             │     │                                          │   │
│             │     │  Migrations/                             │   │
│             │     │  • 001_InitialSchema                     │   │
│             │     │  • 002_AddOutbox                         │   │
│             │     └──────────────┬───────────────────────────┘   │
│             │                    │                                │
│             │                    │ Persists to                    │
│             │                    ▼                                │
│             │     ┌──────────────────────────────────────────┐   │
│             │     │   PostgreSQL (Local)                     │   │
│             │     │   • Business tables                      │   │
│             │     │   • Outbox table                         │   │
│             │     └──────────────────────────────────────────┘   │
│             │                                                     │
│             │     ┌──────────────────────────────────────────┐   │
│             │     │   External Services                      │   │
│             │     │                                          │   │
│             │     │  • CloudSyncClient (HttpClient)          │   │
│             │     │  • StoreIdentityService                  │   │
│             │     └──────────────────────────────────────────┘   │
│             │                                                     │
│             │     ┌──────────────────────────────────────────┐   │
│             │     │   Background Services                    │   │
│             │     │                                          │   │
│             │     │  • SyncWorker (IHostedService)           │   │
│             │     │  • MasterDataSyncWorker                  │   │
│             │     │  • HealthCheckService                    │   │
│             │     └──────────────────────────────────────────┘   │
│             │                                                     │
└─────────────┼─────────────────────────────────────────────────────┘
              │
              │ Syncs to
              ▼
┌───────────────────────────────────────────────────────────────────┐
│                         CLOUD COMPONENTS                           │
│                                                                    │
│  ┌──────────────────────────────────────────────────────────┐    │
│  │  IndyPOS.Cloud API                                       │    │
│  │                                                          │    │
│  │  Controllers:                  Services:                 │    │
│  │  • SyncController              • EventProcessor          │    │
│  │  • MasterDataController        • IdempotencyChecker      │    │
│  │  • ReportsController           • ReportGenerator         │    │
│  └──────────────────┬───────────────────────────────────────┘    │
│                     │                                             │
│                     ▼                                             │
│  ┌──────────────────────────────────────────────────────────┐    │
│  │  CloudDbContext (EF Core)                                │    │
│  │                                                          │    │
│  │  • SyncedEvent (deduplication)                           │    │
│  │  • Invoice (all stores)                                  │    │
│  │  • Product (master)                                      │    │
│  │  • Store (metadata)                                      │    │
│  └──────────────────┬───────────────────────────────────────┘    │
│                     │                                             │
│                     ▼                                             │
│  ┌──────────────────────────────────────────────────────────┐    │
│  │  PostgreSQL (Central - Singapore)                        │    │
│  └──────────────────────────────────────────────────────────┘    │
│                                                                    │
└────────────────────────────────────────────────────────────────────┘
```

---

## Project Structure Mapping

```
IndyPOS.sln
│
├── src/
│   │
│   ├── IndyPOS.Domain/                    [DOMAIN LAYER]
│   │   ├── Entities/
│   │   │   ├── Invoice.cs
│   │   │   ├── InvoiceLine.cs
│   │   │   ├── Payment.cs
│   │   │   ├── Product.cs
│   │   │   ├── InventoryMovement.cs
│   │   │   └── Customer.cs
│   │   ├── ValueObjects/
│   │   │   ├── Money.cs
│   │   │   ├── ProductCode.cs
│   │   │   └── StoreId.cs
│   │   └── Services/
│   │       ├── PricingService.cs
│   │       └── InventoryService.cs
│   │
│   ├── IndyPOS.Application/               [APPLICATION LAYER]
│   │   ├── Commands/
│   │   │   ├── CompleteSale/
│   │   │   │   ├── CompleteSaleCommand.cs
│   │   │   │   └── CompleteSaleCommandHandler.cs
│   │   │   └── AdjustInventory/
│   │   │       ├── AdjustInventoryCommand.cs
│   │   │       └── AdjustInventoryCommandHandler.cs
│   │   ├── Queries/
│   │   │   ├── GetProductById/
│   │   │   └── GetStockLevel/
│   │   ├── Interfaces/
│   │   │   ├── IInvoiceRepository.cs
│   │   │   ├── IProductRepository.cs
│   │   │   ├── IOutboxRepository.cs
│   │   │   └── IStoreIdentityService.cs
│   │   └── DTOs/
│   │       ├── InvoiceDto.cs
│   │       └── ProductDto.cs
│   │
│   ├── IndyPOS.Infrastructure/            [INFRASTRUCTURE LAYER]
│   │   ├── Persistence/
│   │   │   ├── StoreHubDbContext.cs
│   │   │   ├── Repositories/
│   │   │   │   ├── InvoiceRepository.cs
│   │   │   │   ├── ProductRepository.cs
│   │   │   │   └── OutboxRepository.cs
│   │   │   ├── Configurations/
│   │   │   │   ├── InvoiceConfiguration.cs
│   │   │   │   └── ProductConfiguration.cs
│   │   │   └── Migrations/
│   │   │       └── 001_InitialSchema.cs
│   │   ├── Services/
│   │   │   ├── CloudSyncClient.cs
│   │   │   └── StoreIdentityService.cs
│   │   ├── BackgroundServices/
│   │   │   ├── SyncWorker.cs
│   │   │   └── MasterDataSyncWorker.cs
│   │   └── ConfigureServices.cs
│   │
│   ├── IndyPOS.StoreHub/                  [PRESENTATION - API]
│   │   ├── Controllers/
│   │   │   ├── SalesController.cs
│   │   │   ├── ProductsController.cs
│   │   │   ├── InventoryController.cs
│   │   │   └── SyncController.cs
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── Dockerfile
│   │
│   ├── IndyPOS.Windows.Forms/             [PRESENTATION - UI]
│   │   ├── Forms/
│   │   │   ├── MainForm.cs
│   │   │   ├── SaleForm.cs
│   │   │   └── ProductForm.cs
│   │   ├── ViewModels/
│   │   └── Program.cs
│   │
│   └── IndyPOS.Cloud/                     [CLOUD API]
│       ├── Controllers/
│       │   ├── SyncController.cs
│       │   ├── MasterDataController.cs
│       │   └── ReportsController.cs
│       ├── Services/
│       │   ├── EventProcessor.cs
│       │   └── IdempotencyChecker.cs
│       ├── Persistence/
│       │   └── CloudDbContext.cs
│       ├── Program.cs
│       └── Dockerfile
│
└── tests/
    ├── IndyPOS.Domain.Tests/
    ├── IndyPOS.Application.Tests/
    ├── IndyPOS.Infrastructure.Tests/
    ├── IndyPOS.StoreHub.Tests/
    └── IndyPOS.Cloud.Tests/
```

---

## Dependency Flow

```
┌────────────────────────────────────────────────────┐
│  Dependency Rule: Inner layers never depend on     │
│  outer layers. Dependencies point INWARD.          │
└────────────────────────────────────────────────────┘

                    ┌─────────────┐
                    │ Presentation│
                    │  (UI/API)   │
                    └──────┬──────┘
                           │
                     depends on
                           │
                           ▼
                  ┌─────────────────┐
                  │   Application   │
                  │   (Use Cases)   │
                  └────────┬─────────┘
                           │
                     depends on
                           │
                           ▼
                    ┌──────────────┐
                    │    Domain    │
                    │ (Core Logic) │
                    └──────────────┘
                           ▲
                           │
                    implements &
                        uses
                           │
                  ┌────────┴─────────┐
                  │  Infrastructure  │
                  │ (I/O, DB, HTTP)  │
                  └──────────────────┘
```

---

## Communication Patterns

### 1. POS Client ↔ StoreHub (HTTP/REST)
```
POS Client
  │
  │ HTTP POST /sales/complete
  │ {Authorization: "Bearer token"}
  │
  ▼
StoreHub API
  │
  │ Validates request
  │ Calls CommandHandler
  │ Returns DTO
  │
  ▼
POS Client (updates UI)
```

### 2. StoreHub ↔ Application Layer (Method Calls)
```
Controller
  │
  │ var command = new CompleteSaleCommand { ... };
  │ var result = await _mediator.Send(command);
  │
  ▼
CommandHandler
  │
  │ Business logic
  │ Repository calls
  │
  ▼
Repository (via interface)
```

### 3. StoreHub → Cloud (Background + HTTP)
```
SyncWorker (Background)
  │
  │ Polls outbox every 3 sec
  │
  ▼
CloudSyncClient
  │
  │ HTTP POST /sync/events
  │ {Authorization: "ApiKey STORE-001:secret"}
  │
  ▼
Cloud API
```

---

## Cross-Cutting Concerns

```
┌────────────────────────────────────────────────────────┐
│                  Logging (Serilog)                     │
│  • Structured logs                                     │
│  • Written to: Console, File, Seq (optional)           │
└────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────┐
│            Error Handling (Middleware)                 │
│  • Global exception handler                            │
│  • Returns problem details (RFC 7807)                  │
└────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────┐
│            Validation (FluentValidation)               │
│  • Request DTOs validated                              │
│  • Domain rules in entities                            │
└────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────┐
│         Configuration (IOptions<T>)                    │
│  • StoreOptions (StoreId, Name)                        │
│  • CloudOptions (BaseUrl, ApiKey)                      │
│  • DatabaseOptions (ConnectionString)                  │
└────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────┐
│         Health Checks (ASP.NET Core)                   │
│  • Database connectivity                               │
│  • Outbox backlog size                                 │
│  • Last successful sync timestamp                      │
└────────────────────────────────────────────────────────┘
```

---

**Next:** See `04-database-schema.md` for detailed schema
