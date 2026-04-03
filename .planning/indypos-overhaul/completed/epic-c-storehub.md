# Epic C: StoreHub Service + Aspire Foundation - COMPLETE

**Completed:** 2026-03-08

## Goal
New StoreHub Windows Service project with .NET Aspire dev orchestration

## Tasks Completed

| Task | Description |
|------|-------------|
| C0a | Add IndyPOS.ServiceDefaults project |
| C0b | Add IndyPOS.AppHost project |
| C1 | Add IndyPOS.StoreHub project |
| C2 | Add local Postgres persistence |
| C3 | Expose minimal endpoints |
| C4 | Implement selling logic |
| C5 | Add StoreHub to AppHost |

## Aspire Setup

**IndyPOS.ServiceDefaults** provides:
- Health check endpoints (`/health`, `/alive`)
- OpenTelemetry tracing/metrics
- HTTP resilience handlers
- Service discovery

**IndyPOS.AppHost** orchestrates:
- PostgreSQL container (auto-start)
- PgAdmin (on-demand)
- DbGate (on-demand)
- StoreHub API (auto-start)

## Endpoints Implemented

**GET /products:**
- `ProductDto`, `ProductExtensions` for entity-to-DTO mapping
- `GetProductsQuery` with filtering (activeOnly, category, search)
- `GetProductsQueryHandler` using Nokpirab CQRS
- `IProductRepository` + `ProductRepository` with EF Core

**POST /sales/complete:**
- `CompleteSaleRequest/Response` DTOs
- `CompleteSaleCommand` + `CompleteSaleCommandHandler`
- Full transaction flow with atomic EF Core transaction

## Developer Workflow
```bash
dotnet run --project src/IndyPOS.AppHost --launch-profile https
# Dashboard: https://localhost:17222 (requires Docker)
```

## Deliverable
Hub runs locally via Aspire; can complete a sale into local Postgres
