# ADR-002: Entity Organization Strategy

**Status:** Accepted
**Date:** 2026-03-06
**Decision Makers:** Pond (Lead Engineer)

## Context

During Epic D (Schema Design), we created new simplified domain entities for the StoreHub PostgreSQL database. These entities are cleaner and follow the minimal schema design decisions documented in `SCHEMA-FINAL-DECISIONS.md`.

However, we have existing SQLite-shaped entities that are actively used by the Windows.Forms app and current Dapper repositories. We needed to decide how to organize both sets of entities while maintaining Clean Architecture principles.

## Decision

**We will maintain both entity sets side-by-side during the transition period:**

```
src/IndyPOS.Domain/Entities/
├── (existing entities)     # SQLite-shaped, used by current app
│   ├── Invoice.cs
│   ├── InvoiceProduct.cs
│   ├── Payment.cs
│   └── ...
│
└── Core/                   # New clean entities for StoreHub
    ├── Invoice.cs
    ├── InvoiceLine.cs
    ├── Payment.cs
    ├── Product.cs
    ├── PayLater.cs
    ├── OutboxEvent.cs
    └── InventoryMovement.cs
```

**Key principles:**
1. Domain layer remains database-agnostic (no EF Core or PostgreSQL references)
2. Infrastructure layer handles all database-specific mapping (EF Core configurations)
3. Both entity sets coexist until migration is complete

## Cleanup Plan (Epic G)

When Epic G (Desktop Integration) is complete and the app uses StoreHub:

1. Move old entities to `Entities/Deprecated/`
2. Move `Core/` entities up to `Entities/` directly
3. Update all references
4. Eventually remove `Deprecated/` folder entirely

**Tracking Issue:** This cleanup is part of Epic G task G3 (Decommission direct SQLite writes)

## Consequences

### Positive
- No breaking changes to current app during transition
- Clear separation between old and new models
- Clean Architecture maintained (Domain is DB-agnostic)
- Easy to identify which entities are the "future state"

### Negative
- Temporary duplication of similar entities (Invoice in both places)
- Need to remember to clean up after Epic G
- Potential confusion about which entities to use (mitigated by folder naming)

## Related Decisions

- ADR-001: Offline-First Architecture
- SCHEMA-FINAL-DECISIONS.md: Minimal schema design
- GUID-PRIMARY-KEY-STRATEGY.md: UUID as primary key
