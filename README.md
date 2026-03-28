# IndyPOS

A Point-of-Sale system for small retail stores in Thailand. Supports offline-first operations with cloud sync.

```
┌────────────────┐     ┌────────────────┐
│  Desktop POS   │     │  Tablet POS    │
│  (WinForms)    │     │  (Future)      │
└───────┬────────┘     └───────┬────────┘
        │ HTTP                 │ HTTP
        └──────────┬───────────┘
                   ▼
        ┌─────────────────────┐
        │   StoreHub API      │ ← Local service
        │   (ASP.NET Core)    │
        └──────────┬──────────┘
                   ▼
        ┌─────────────────────┐
        │  Local PostgreSQL   │ ← Per-store
        └──────────┬──────────┘
                   │ HTTPS (when online)
                   ▼
        ┌─────────────────────┐
        │     Cloud API       │ ← Central
        └─────────────────────┘
```

## Quick Start

```bash
# Clone & build
git clone https://github.com/your-repo/IndyPOS.git
cd IndyPOS && dotnet build

# Run with Aspire (requires Docker Desktop)
dotnet run --project src/IndyPOS.AppHost --launch-profile https

# Run tests (no Docker needed)
dotnet test tests/IndyPOS.Application.Tests/
```

**Full setup guide:** [docs/development/getting-started.md](docs/development/getting-started.md)

## Tech Stack

| Component | Technology |
|-----------|------------|
| Desktop UI | Windows.Forms (.NET 10) |
| API | ASP.NET Core Minimal APIs |
| Database | PostgreSQL (EF Core) |
| Legacy DB | SQLite (Dapper) - migrating |
| Dev Orchestration | .NET Aspire |
| CQRS | Nokpirab |
| Auth | JWT + BCrypt |

## Architecture

Clean Architecture with 4 layers:

- **Domain** - Entities, business rules (no dependencies)
- **Application** - Use cases, CQRS commands/queries
- **Infrastructure** - Repositories, external services
- **Presentation** - UI (WinForms), APIs (StoreHub, CloudApi)

## Project Structure

```
src/
├── IndyPOS.Domain/          # Core entities
├── IndyPOS.Application/     # Use cases (CQRS)
├── IndyPOS.Infrastructure/  # EF Core, repositories
├── IndyPOS.StoreHub/        # Local API service
├── IndyPOS.CloudApi/        # Central cloud API
├── IndyPOS.Windows.Forms/   # Desktop UI
├── IndyPOS.AppHost/         # Aspire orchestrator
└── IndyPOS.ServiceDefaults/ # Shared config

tests/
└── IndyPOS.Application.Tests/  # 179 tests (unit + integration)

docs/                        # Documentation
.planning/                   # Planning docs, ADRs
```

## Testing

```bash
# All tests (no Docker required)
dotnet test tests/IndyPOS.Application.Tests/

# Run specific category
dotnet test --filter "FullyQualifiedName~StoreHub"
dotnet test --filter "FullyQualifiedName~Integration"
```

| Test Type | Docker? | Framework |
|-----------|---------|-----------|
| Unit tests | No | xUnit, Moq, AutoFixture |
| Integration tests | No | WireMock.Net |
| Manual E2E | Yes | Aspire + PostgreSQL |

## Documentation

- [Developer Guide](docs/development/getting-started.md) - Full setup & testing
- [Architecture Overview](docs/architecture/overview.md)
- [ASCII Diagrams](docs/diagrams/architecture-overview.md)
- [Data Flow](docs/diagrams/data-flow.md)

## Store Configuration

Required file: `C:\ProgramData\IndyPOS\Config\StoreConfiguration.json`

```json
{
  "StoreFullName": "Test Store",
  "StoreName": "Test Store",
  "StoreAddressLine1": "123 Test Street",
  "StoreAddressLine2": "Test City 12345",
  "StorePhoneNumber": "000-000-0000",
  "PrinterName": "XP-58",
  "Code": 1
}
```
