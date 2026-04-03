# Epic D: Schema Design - COMPLETE

**Completed:** 2026-03-06

## Goal
Define PostgreSQL schema with PublicId + StoreId

## Tasks Completed

| Task | Description |
|------|-------------|
| D1 | Create Core entities with UUID Id |
| D2 | Create OutboxEvent entity |
| D3 | Create InventoryMovement entity |
| D4 | Create EF Core configurations |

## Files Created

**Domain (Entities/Core/):**
- `Invoice.cs`, `InvoiceLine.cs`, `Payment.cs`, `Product.cs`, `PayLater.cs`
- `OutboxEvent.cs`, `InventoryMovement.cs`

**Infrastructure (Persistence/StoreHub/):**
- `StoreHubDbContext.cs`
- `Configurations/` - 7 entity configuration files

## Architecture Decision
- See `ADR-002-entity-organization.md` for entity organization strategy
- Core entities coexist with legacy entities until Epic G cleanup

## Deliverable
Schema defined; EF Core ready for StoreHub
