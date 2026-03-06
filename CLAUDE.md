# IndyPOS - Project Context

## Overview

IndyPOS is a Point-of-Sale system for small retail stores (3 stores, 1-2 terminals each).

**Tech Stack:**
- Backend: C# .NET 8 (Clean Architecture)
- UI: Windows.Forms (future: MAUI)
- Database: SQLite (current) → PostgreSQL (planned)
- Patterns: CQRS (Nokpirab), Domain-Driven Design

## Project Structure

```
src/
├── IndyPOS.Domain/          # Entities, Value Objects, Domain Logic
├── IndyPOS.Application/     # Use Cases (Commands/Queries), Interfaces, DTOs
├── IndyPOS.Infrastructure/  # Repositories, External Services
└── IndyPOS.Windows.Forms/   # Desktop UI

tests/
└── IndyPOS.Application.Tests/

docs/                        # Architecture docs, diagrams
.planning/                   # Planning docs, ADRs, implementation status
.claude/                     # Claude workspace (session logs)
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

### Local Postgres (Dev)
```bash
docker-compose up -d
```

## Important Reminders

- Apply SOLID principles and Clean Code standards
- Write small, testable functions
- Use async/await for I/O operations
- Validate input at system boundaries
