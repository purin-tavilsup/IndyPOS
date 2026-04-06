# Epic M: Multi-Store Type Support

**Status:** 🟢 Ready for Implementation
**Created:** 2026-04-05
**Updated:** 2026-04-05
**Priority:** Future Feature

---

## Overview

Support multiple store types (GeneralHardware, Minimart, CoffeeShop) with:
- Different feature sets per store type
- Local PostgreSQL per store (offline-first, unchanged from current architecture)
- Central PostgreSQL with database-per-store-type for sync/reporting
- Store isolation via StoreId column in all entities

---

## Business Requirements

| Store Type | PayLater | Product Types | Payment Methods |
|------------|----------|---------------|-----------------|
| GeneralHardware | ✅ Enabled | Multiple (Hardware, General, etc.) | Cash, Bank, PayLater |
| Minimart | ❌ Disabled | General only | Cash, Bank |
| CoffeeShop | ❌ Disabled | General only | Cash, Bank |

---

## Technical Decisions

| Aspect | Decision | Rationale |
|--------|----------|-----------|
| **StoreHub Location** | Local per store | Most stores = 1 POS machine, keep simple |
| **Offline Support** | Local PostgreSQL required | Offline-first POS - non-negotiable |
| **Store ID** | Manual UUID by System Admin | Avoid ID mismatch; documented + provided at install |
| **Store Name** | User-provided during setup | |
| **Database Strategy (Central)** | One database per store type | `generalHardware`, `minimart`, `coffeeShop` |
| **Store Type** | Immutable after installation | |
| **Feature Overrides** | Not supported | Keep simple |
| **Migration** | Migrate existing 1 store | Move to shared `generalHardware` DB + sync to cloud |

---

## Architecture

**No architectural changes** - this builds on existing Epic E (Outbox + SyncWorker) pattern:

```
┌─────────────────────────────────────────────────┐
│              Store (Local Machine)              │
│  ┌──────────┐    ┌──────────┐    ┌──────────┐  │
│  │ WinForms │───▶│ StoreHub │───▶│ Local PG │  │
│  └──────────┘    └──────────┘    └──────────┘  │
│                        │                        │
└────────────────────────│────────────────────────┘
                         │ (when online, via SyncWorker)
                         ▼
              ┌──────────────────────┐
              │      CloudApi        │
              │  ┌────────────────┐  │
              │  │ Central PG     │  │
              │  │ (per store     │  │
              │  │  type DBs)     │  │
              │  └────────────────┘  │
              └──────────────────────┘
```

**Central PostgreSQL (CloudApi):**
```
┌─────────────────────────────────────────────────────────────┐
│                Central PostgreSQL Server                     │
│                                                              │
│   ┌──────────────────┐  ┌──────────────┐  ┌──────────────┐  │
│   │ generalHardware  │  │   minimart   │  │  coffeeShop  │  │
│   │                  │  │              │  │              │  │
│   │ Store A (uuid)   │  │ Store X (uuid)│ │ Store P (uuid)│ │
│   │ Store B (uuid)   │  │ Store Y (uuid)│ │              │  │
│   └──────────────────┘  └──────────────┘  └──────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

- **Local PG** = source of truth for that store (offline-first)
- **Central PG** = synced copy for reporting/backup/cross-store visibility
- CloudApi routes synced data to correct database based on StoreType

---

## Implementation Tasks

| Task | Description | Scope | Complexity |
|------|-------------|-------|------------|
| **M1** | Add `StoreType` enum and `StoreTypeFeatures` to Domain | Domain | Low |
| **M2** | Update `StoreConfiguration` schema (add StoreId, StoreType) | Configuration | Low |
| **M3** | Add `StoreId` column to all entities | Domain, Schema | Medium |
| **M4** | Update all repositories to filter by StoreId | Infrastructure | Medium |
| **M5** | Update StoreHub API to accept/validate StoreId | StoreHub | Low |
| **M6** | Add feature validation in Application commands | Application | Medium |
| **M7** | Update First-Run Wizard (store type selection, StoreId input) | WinForms | Medium |
| **M8** | Update WinForms UI to respect feature flags | WinForms | Medium |
| **M9** | Update CloudApi store registry + database routing | CloudApi | Medium |
| **M10** | Update Bootstrapper for new setup flow | Installer | Low |
| **M11** | Database migrations (local + central) | Infrastructure | Medium |
| **M12** | Migrate existing store to new schema | Migration | Medium |
| **M13** | Tests + Documentation | Tests, Docs | Medium |

---

## Code Sketches

### StoreType Enum

```csharp
public enum StoreType
{
    GeneralHardware = 1,  // Full features
    Minimart = 2,         // Limited features
    CoffeeShop = 3        // Limited features
}
```

### StoreTypeFeatures

```csharp
public record StoreTypeFeatures
{
    public bool PayLaterEnabled { get; init; }
    public bool MultipleProductTypesEnabled { get; init; }
    public IReadOnlyList<PaymentMethod> AllowedPaymentMethods { get; init; }

    public static StoreTypeFeatures For(StoreType type) => type switch
    {
        StoreType.GeneralHardware => new StoreTypeFeatures
        {
            PayLaterEnabled = true,
            MultipleProductTypesEnabled = true,
            AllowedPaymentMethods = [PaymentMethod.Cash, PaymentMethod.BankTransfer, PaymentMethod.PayLater]
        },
        StoreType.Minimart or StoreType.CoffeeShop => new StoreTypeFeatures
        {
            PayLaterEnabled = false,
            MultipleProductTypesEnabled = false,
            AllowedPaymentMethods = [PaymentMethod.Cash, PaymentMethod.BankTransfer]
        },
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}
```

### Entity with StoreId

```csharp
public class Invoice
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }  // Required for multi-tenant
    public DateTime CreatedUtc { get; set; }
    // ... rest of fields
}
```

### StoreConfiguration

```json
{
    "StoreId": "550e8400-e29b-41d4-a716-446655440000",  // Provided by System Admin
    "StoreName": "Mini Express Downtown",               // User-provided at install
    "StoreType": "Minimart",                            // Selected at install, immutable
    "StoreAddressLine1": "123 Main Street",
    "StoreAddressLine2": "Bangkok 10110",
    "StorePhoneNumber": "02-123-4567",
    "PrinterName": "XP-58",
    "BarcodeScannerDeviceName": "",
    "SerialPortName": "COM1"
}
```

> **Note:** StoreId is a UUID created by System Admin (Pond or Claude) and documented before providing to the user for installation. This avoids accidental ID mismatches.

---

## Impact Analysis

### High Impact (Significant Changes)

| Area | Changes Required |
|------|------------------|
| **All Entities** | Add `StoreId` column |
| **All Repositories** | Add StoreId filter to all queries |
| **All API Endpoints** | Accept StoreId (header or context) |
| **Database Schema** | Add indexes on StoreId columns |

### Medium Impact

| Area | Changes Required |
|------|------------------|
| **First-Run Wizard** | Add store type selection step |
| **WinForms UI** | Hide/disable features based on store type |
| **Bootstrapper** | Update database setup flow |
| **CloudApi** | Store registry with type routing |

### Low Impact

| Area | Changes Required |
|------|------------------|
| **Domain** | Add StoreType enum + features |
| **Configuration** | Add StoreId, StoreType fields |

---

## Migration Plan

### Existing Store (1 GeneralHardware store)

1. **Pre-migration:**
   - Create UUID for the store (System Admin)
   - Document StoreId in store registry
   - Backup current local database

2. **Migration steps:**
   - Add `StoreId` column to all tables (with default = new UUID)
   - Update `StoreConfiguration.json` with StoreId + StoreType
   - Run migration tool to update local schema
   - Sync to central `generalHardware` database

3. **Verification:**
   - Verify all local data has correct StoreId
   - Verify sync to CloudApi works
   - Verify reports show data correctly

---

## Notes

- This is a **breaking change** for existing store schema
- Migration path is well-defined (1 store to migrate)
- Consider implementing in phases:
  - **Phase 1:** M1-M6 (Core domain + infrastructure)
  - **Phase 2:** M7-M10 (UI + Installer)
  - **Phase 3:** M11-M13 (Migrations + Tests)

---

## Dependencies

- **Requires:** Epic I (Cloud Infrastructure) for central database routing
- **Can start without CloudApi:** M1-M8 can be done locally

---

*Last updated: 2026-04-05*
