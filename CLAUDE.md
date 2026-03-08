# IndyPOS - Project Context

## Active Plan

**Current:** `indypos-overhaul` → `.planning/indypos-overhaul/implementation-status.md`

---

## Overview

IndyPOS is a Point-of-Sale system for small retail stores (3 stores, 1-2 terminals each).

**Tech Stack:**
- Backend: C# .NET 10 (Clean Architecture)
- UI: Windows.Forms (future: MAUI)
- Database: SQLite (legacy) → PostgreSQL (StoreHub)
- Dev Environment: .NET Aspire
- Patterns: CQRS, Domain-Driven Design

## Project Structure

```
src/
├── IndyPOS.Domain/          # Entities, Value Objects, Domain Logic
├── IndyPOS.Application/     # Use Cases (Commands/Queries), Interfaces, DTOs
├── IndyPOS.Infrastructure/  # Repositories, External Services
├── IndyPOS.Windows.Forms/   # Desktop UI (legacy)
├── IndyPOS.StoreHub/        # Local API service (ASP.NET Core)
├── IndyPOS.AppHost/         # Aspire orchestrator
└── IndyPOS.ServiceDefaults/ # Shared health checks, OpenTelemetry

tests/
└── IndyPOS.Application.Tests/

docs/                        # Architecture docs, diagrams
.planning/                   # Planning docs, ADRs, implementation status
```

## Coding Standards

### Clean Architecture Layers
- **Domain**: No external dependencies, pure business logic
- **Application**: Orchestrates use cases, depends on Domain only
- **Infrastructure**: Implements interfaces, depends on Application + Domain
- **UI**: Thin presentation layer

### Naming Patterns
- **Use Cases**: `[Verb][Noun]Command/Query` (e.g., `CompleteSaleCommand`)
- **Handlers**: `[Command/Query]Handler` (e.g., `CompleteSaleCommandHandler`)
- **Repositories**: `I[Entity]Repository` (e.g., `IInvoiceRepository`)
- **Services**: `I[Domain]Service` (e.g., `IStoreIdentityService`)

### Entity Conventions
- **Legacy entities**: Keep `int Id` for SQLite compatibility
- **New entities**: Add `Guid PublicId` for distributed identity
- **All entities**: Add `DateTime CreatedUtc`, `DateTime LastModifiedUtc`

### Method Chaining Style
Use vertical alignment for fluent APIs / method chaining:

```csharp
// Good - dots vertically aligned
builder.AddProject<Projects.IndyPOS_StoreHub>("storehub-api")
       .WithReference(storeHubDb)
       .WaitFor(postgres);

// Good - also acceptable with standard indent
builder.AddProject<Projects.IndyPOS_StoreHub>("storehub-api")
    .WithReference(storeHubDb)
    .WaitFor(postgres);

// Bad - no alignment
builder.AddProject<Projects.IndyPOS_StoreHub>("storehub-api").WithReference(storeHubDb).WaitFor(postgres);
```

## Development Workflow

- **Branch**: Feature branches from `development`
- **Main Branch**: `development`
- **Commits**: Follow conventional commits
- **PRs**: Small, focused changes with tests

## Key Documentation

| Topic | Location |
|-------|----------|
| Architecture Overview | `docs/architecture/overview.md` |
| ASCII Diagrams | `docs/diagrams/` |
| Planning & Roadmap | `.planning/indypos-overhaul/` |
| Implementation Status | `.planning/indypos-overhaul/implementation-status.md` |
| Detailed Specs | `.planning/indypos-overhaul/IndyPOS_Docs_v1_2_0/` |

## Quick Reference

### Run Tests
```bash
dotnet test tests/IndyPOS.Application.Tests/
```

### Build
```bash
dotnet build
```

### Run with Aspire (Dev)
```bash
dotnet run --project src/IndyPOS.AppHost --launch-profile https
```
Opens dashboard at https://localhost:17222 (requires Docker)

### Store Configuration (Required for Debug)

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

> If file doesn't exist, app auto-creates with defaults.

## Important Reminders

- Apply SOLID principles and Clean Code standards
- Write small, testable functions
- Use async/await for I/O operations
- Validate input at system boundaries
