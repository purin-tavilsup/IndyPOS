# IndyPOS Developer Guide

## Overview

IndyPOS is a Point-of-Sale system designed for small retail stores in Thailand. It supports offline-first operations with cloud sync capabilities.

```
┌─────────────────────────────────────────────────────────────────────────┐
│                              STORE                                       │
│                                                                          │
│   ┌─────────────┐         ┌─────────────┐                               │
│   │  Desktop    │         │   Tablet    │                               │
│   │  (WinForms) │         │ (Future)    │                               │
│   └──────┬──────┘         └──────┬──────┘                               │
│          │ HTTP                  │ HTTP                                  │
│          └──────────┬────────────┘                                       │
│                     ▼                                                    │
│          ┌─────────────────────┐                                        │
│          │     StoreHub API    │ ← Local service (per store)            │
│          │   (ASP.NET Core)    │                                        │
│          └──────────┬──────────┘                                        │
│                     │ EF Core                                            │
│                     ▼                                                    │
│          ┌─────────────────────┐                                        │
│          │  Local PostgreSQL   │ ← Per-store database                   │
│          │    + Outbox         │                                        │
│          └──────────┬──────────┘                                        │
│                     │                                                    │
└─────────────────────┼────────────────────────────────────────────────────┘
                      │ HTTPS (when online)
                      ▼
           ┌─────────────────────┐
           │     Cloud API       │ ← Central (Singapore)
           │ + Central Postgres  │
           └─────────────────────┘
```

## Tech Stack

| Layer          | Technology                          |
|----------------|-------------------------------------|
| UI (current)   | Windows.Forms (.NET 10)             |
| UI (future)    | MAUI (cross-platform)               |
| API            | ASP.NET Core Minimal APIs           |
| Database       | PostgreSQL (EF Core)                |
| Legacy DB      | SQLite (Dapper) - being migrated    |
| Dev Orchestration | .NET Aspire                      |
| CQRS           | Nokpirab                            |
| Auth           | JWT + BCrypt                        |
| Sync           | Outbox pattern + Background Worker  |

---

## Prerequisites

- [.NET 10 SDK](https://dot.net/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for PostgreSQL)
- IDE: Visual Studio 2022 / VS Code / Rider
- Windows 10/11 (for Windows.Forms UI)

---

## Quick Start

### 1. Clone and Build

```bash
git clone https://github.com/your-repo/IndyPOS.git
cd IndyPOS
dotnet build
```

### 2. Run with Aspire (Recommended)

```bash
# Start everything: PostgreSQL, StoreHub, CloudApi
dotnet run --project src/IndyPOS.AppHost --launch-profile https
```

This will:
- Start a PostgreSQL container via Docker
- Run StoreHub API on http://localhost:5012
- Run CloudApi on http://localhost:5011
- Open Aspire Dashboard at https://localhost:17222

### 3. Create Store Configuration (Required)

Create `C:\ProgramData\IndyPOS\Config\StoreConfiguration.json`:

```json
{
  "StoreFullName": "Test Store",
  "StoreName": "Test Store",
  "StoreAddressLine1": "123 Test Street",
  "StoreAddressLine2": "Test City 12345",
  "StorePhoneNumber": "000-000-0000",
  "PrinterName": "XP-58",
  "BarcodeScannerDeviceName": "",
  "SerialPortName": "COM1",
  "Code": 1
}
```

### 4. Verify Services

```bash
# Health check
curl http://localhost:5012/health/ready

# Login with test user
curl -X POST http://localhost:5012/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"cashier","password":"cashier123"}'
```

---

## Project Structure

```
IndyPOS/
├── src/
│   ├── IndyPOS.Domain/          # Entities, Value Objects, Business Rules
│   │   └── Entities/            # Invoice, Product, StoreUser, etc.
│   │
│   ├── IndyPOS.Application/     # Use Cases (CQRS)
│   │   ├── UseCases/            # Commands & Queries
│   │   │   ├── StoreHub/        # Login, Products, Sales
│   │   │   └── Cloud/           # Sync, User Management
│   │   ├── Abstractions/        # Interfaces (IRepository, IService)
│   │   └── Common/              # Authorization, DTOs, Enums
│   │
│   ├── IndyPOS.Infrastructure/  # Implementation
│   │   ├── Persistence/         # EF Core DbContexts, Repositories
│   │   │   ├── StoreHub/        # StoreHubDbContext
│   │   │   └── Cloud/           # CloudDbContext
│   │   └── Services/            # External services (HTTP clients)
│   │
│   ├── IndyPOS.StoreHub/        # Local API service
│   │   ├── Endpoints/           # Minimal API endpoints
│   │   └── Program.cs           # Service configuration
│   │
│   ├── IndyPOS.CloudApi/        # Central cloud API
│   │
│   ├── IndyPOS.Windows.Forms/   # Desktop UI (legacy)
│   │
│   ├── IndyPOS.AppHost/         # Aspire orchestrator
│   └── IndyPOS.ServiceDefaults/ # Shared config (health, telemetry)
│
├── tests/
│   └── IndyPOS.Application.Tests/
│       ├── Unit tests           # Direct folder structure
│       └── Integration/         # WireMock-based tests
│
├── docs/                        # Documentation
│   ├── architecture/
│   ├── diagrams/
│   └── development/             # This guide
│
└── .planning/                   # Planning docs, ADRs
    └── indypos-overhaul/
```

---

## Running Tests

### All Tests (No Docker Required)

```bash
dotnet test tests/IndyPOS.Application.Tests/
```

**Current Status: 266 tests** (202 unit + 49 integration + 15 migration)

### Test Categories

```
tests/
├── IndyPOS.Application.Tests/        # 202 unit tests
│   ├── Common/Authorization/         # RoleCapabilities tests
│   ├── Integration/StoreHub/         # WireMock E2E tests
│   ├── StoreHub/                     # Auth, Products, Sales handlers
│   ├── UseCases/Cloud/               # CloudApi handlers
│   └── Infrastructure/               # DPAPI, storage tests
│
├── IndyPOS.StoreHub.IntegrationTests/  # 49 integration tests
│   ├── StoreHubWebApplicationFactory.cs
│   ├── IntegrationTestBase.cs
│   └── Endpoints/
│       ├── AuthEndpointTests.cs
│       ├── ProductsEndpointTests.cs
│       ├── SalesEndpointTests.cs
│       ├── ReportsEndpointTests.cs
│       └── SyncEndpointTests.cs
│
└── IndyPOS.Migration.Tests/          # 15 migration tests
    ├── MigrationTestFixture.cs
    ├── ProductMigrationTests.cs
    └── InvoiceMigrationTests.cs
```

### Test Frameworks Used

| Framework        | Purpose                           |
|-----------------|-----------------------------------|
| xUnit           | Test runner                       |
| FluentAssertions| Readable assertions               |
| Moq             | Mocking dependencies              |
| AutoFixture     | Auto-generate test data           |
| WireMock.Net    | HTTP API simulation for E2E tests |

### Run Specific Test Categories

```bash
# Run only StoreHub tests
dotnet test --filter "FullyQualifiedName~StoreHub"

# Run only integration tests
dotnet test --filter "FullyQualifiedName~Integration"

# Run only authorization tests
dotnet test --filter "FullyQualifiedName~Authorization"
```

### Do Tests Need Docker?

| Test Type          | Docker Required? | Notes                           |
|--------------------|------------------|---------------------------------|
| Unit tests         | No               | All mocked (202 tests)          |
| Integration tests  | Yes              | Testcontainers PostgreSQL (49)  |
| Migration tests    | Yes              | Testcontainers PostgreSQL (15)  |
| Manual E2E testing | Yes              | Real PostgreSQL via Aspire      |

---

## Running WinForms with Aspire

To test the full stack (WinForms → StoreHub → PostgreSQL):

### 1. Start Aspire (StoreHub + PostgreSQL)

```bash
# Ensure Docker Desktop is running
dotnet run --project src/IndyPOS.AppHost --launch-profile https
```

### 2. Check StoreHub URL

Open Aspire Dashboard at `https://localhost:17222` and find the StoreHub endpoint (usually `http://localhost:5000`).

### 3. Configure WinForms

Edit `src/IndyPOS.Windows.Forms/appsettings.json`:
```json
{
  "StoreHub": {
    "BaseUrl": "http://localhost:5000",
    "TimeoutSeconds": 30,
    "AutoSyncProductsOnStartup": true
  }
}
```

### 4. Run WinForms (Separate Process)

```bash
dotnet run --project src/IndyPOS.Windows.Forms
```

Or in Rider: Right-click `IndyPOS.Windows.Forms` → Run

### Pro Tip: Compound Run Configuration (Rider)

1. **Run → Edit Configurations**
2. Click **+** → **Compound**
3. Name: `Full Stack (Aspire + WinForms)`
4. Add: `IndyPOS.AppHost`, `IndyPOS.Windows.Forms`
5. Now one click runs everything!

---

## Manual E2E Testing

### Start Services

```bash
# Ensure Docker Desktop is running
dotnet run --project src/IndyPOS.AppHost --launch-profile https
```

### Test Endpoints

```bash
# 1. Login as cashier
curl -X POST http://localhost:5012/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"cashier","password":"cashier123"}'

# Save the token from response
export TOKEN="eyJhbG..."

# 2. Get products
curl http://localhost:5012/products \
  -H "Authorization: Bearer $TOKEN"

# 3. Complete a sale
curl -X POST http://localhost:5012/sales/complete \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "USER_GUID_HERE",
    "lines": [{"productId": "PRODUCT_GUID", "quantity": 2, "unitPrice": 15.00}],
    "payments": [{"method": "Cash", "amount": 30.00}]
  }'

# 4. Check sync status (requires Manager or Admin role)
curl http://localhost:5012/sync/status \
  -H "Authorization: Bearer $TOKEN"
```

### Test Users (Seeded in Development)

| Username | Password    | Role (RoleId) | Capabilities                    |
|----------|-------------|---------------|---------------------------------|
| cashier  | cashier123  | Cashier (1)   | products.read, sales.complete   |
| manager  | manager123  | Manager (2)   | + sync.view_status              |
| admin    | admin123    | Admin (3)     | + users.*, admin.*              |

---

## Architecture Deep Dive

### Clean Architecture Layers

```
┌────────────────────────────────────────────────────────────────────┐
│                    PRESENTATION (UI / API)                         │
│  Windows.Forms │ StoreHub API │ CloudApi                          │
└────────────────────────────────┬───────────────────────────────────┘
                                 │ Uses
                                 ▼
┌────────────────────────────────────────────────────────────────────┐
│                    APPLICATION (Use Cases)                         │
│  Commands: CompleteSaleCommand, LoginCommand                       │
│  Queries: GetProductsQuery, GetSyncStatusQuery                     │
│  Handlers: CompleteSaleCommandHandler                              │
│  Interfaces: IInvoiceRepository, IPasswordHasher                   │
└────────────────────────────────┬───────────────────────────────────┘
                                 │ Depends on
                                 ▼
┌────────────────────────────────────────────────────────────────────┐
│                    DOMAIN (Business Logic)                         │
│  Entities: Invoice, Product, StoreUser, OutboxEvent                │
│  Value Objects: Money, Address                                     │
│  Business Rules: No external dependencies                          │
└────────────────────────────────────────────────────────────────────┘
                                 ▲
                                 │ Implements
┌────────────────────────────────┴───────────────────────────────────┐
│                    INFRASTRUCTURE                                   │
│  EF Core: StoreHubDbContext, CloudDbContext                        │
│  Repositories: InvoiceRepository, ProductRepository                 │
│  Services: BcryptPasswordHasher, StoreHubHttpClient                │
└────────────────────────────────────────────────────────────────────┘
```

### RBAC (Role-Based Access Control)

```
┌─────────────────────────────────────────────────────────────────┐
│                    Capability Matrix                             │
├──────────────┬──────────┬──────────┬──────────┬────────────────┤
│ Capability   │ Cashier  │ Manager  │ Admin    │ Endpoint       │
├──────────────┼──────────┼──────────┼──────────┼────────────────┤
│ products.read│    ✓     │    ✓     │    ✓     │ GET /products  │
│ sales.complete│   ✓     │    ✓     │    ✓     │ POST /sales/*  │
│ sync.view    │    ✗     │    ✓     │    ✓     │ GET /sync/*    │
│ users.read   │    ✗     │    ✗     │    ✓     │ GET /users     │
│ users.create │    ✗     │    ✗     │    ✓     │ POST /users    │
│ admin.*      │    ✗     │    ✗     │    ✓     │ /admin/*       │
└──────────────┴──────────┴──────────┴──────────┴────────────────┘
```

---

## Common Development Tasks

### Add a New Endpoint

1. Create Command/Query in `Application/UseCases/`
2. Create Handler
3. Add endpoint in `StoreHub/Endpoints/`
4. Add authorization policy if needed
5. Write tests

### Add a New Entity

1. Create entity in `Domain/Entities/`
2. Add to `StoreHubDbContext` in `Infrastructure/Persistence/`
3. Create migration: `dotnet ef migrations add NewEntity`
4. Create repository interface in `Application/Abstractions/`
5. Implement repository in `Infrastructure/Persistence/`

### Database Migrations

```bash
# Add migration
cd src/IndyPOS.Infrastructure
dotnet ef migrations add MigrationName \
  --startup-project ../IndyPOS.StoreHub \
  --context StoreHubDbContext

# Apply migrations (done automatically by Aspire)
dotnet ef database update \
  --startup-project ../IndyPOS.StoreHub \
  --context StoreHubDbContext
```

---

## Troubleshooting

### Aspire Won't Start

```bash
# Check if ports are in use
netstat -ano | findstr :5012
netstat -ano | findstr :22222

# Kill old processes
taskkill /F /IM IndyPOS.AppHost.exe
taskkill /F /IM IndyPOS.StoreHub.exe

# Restart
dotnet run --project src/IndyPOS.AppHost --launch-profile https
```

### Docker Issues

```bash
# Check Docker is running
docker ps

# Reset PostgreSQL data
docker-compose down -v
dotnet run --project src/IndyPOS.AppHost --launch-profile https
```

### Test Failures

```bash
# Run with verbose output
dotnet test --logger "console;verbosity=detailed"

# Run single test
dotnet test --filter "FullyQualifiedName=IndyPOS.Application.Tests.StoreHub.Auth.StoreAuthServiceTests.ValidateCredentialsAsync_ValidUser_ReturnsTrue"
```

---

## Useful Links

- [Architecture Overview](../architecture/overview.md)
- [ASCII Diagrams](../diagrams/architecture-overview.md)
- [Data Flow Diagrams](../diagrams/data-flow.md)
- [VM Testing Guide](./vm-testing-guide.md) - Test installers in Hyper-V
- [Implementation Status](.planning/indypos-overhaul/implementation-status.md)

---

## Contributing

1. Create feature branch from `development`
2. Follow conventional commits
3. Ensure all tests pass
4. Create PR with description
