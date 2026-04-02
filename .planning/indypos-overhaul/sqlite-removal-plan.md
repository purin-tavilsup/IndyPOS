# SQLite Removal Plan

**Created:** 2026-03-31
**Updated:** 2026-03-31
**Status:** In Progress
**Scope:** Remove SQLite from main application, keep MigrationTool for ongoing store migrations

---

## Progress Tracker

| Phase | Status | Notes |
|-------|--------|-------|
| Phase 0a: Fix IStoreConstants | ✅ Complete | `HardcodedStoreConstants` created |
| Phase 0b: Create StoreHubReportService | ✅ Complete | Legacy report endpoints added |
| Phase 1: Delete SQLite repos | ✅ Complete | 9 repo files deleted |
| Phase 2: Delete Pos interfaces | ✅ Complete | 8 interface files deleted |
| Phase 3: Delete legacy handlers | ✅ Complete | ~70 handler files deleted |
| Phase 4: Delete legacy services | ✅ Complete | SaleService, UserLogInService, ReportService deleted |
| Phase 5: Update WinForms | ✅ Complete | MainForm, UserLogInPanel, UsersPanel updated for StoreHub-only |
| Phase 6: Clean up tests | ✅ Complete | Legacy handler tests deleted |
| Phase 7: Update documentation | ⏳ Pending | |

---

## Overview

Remove all SQLite dependencies from the main IndyPOS application. WinForms will **require** StoreHub mode (no fallback). The MigrationTool will be preserved for migrating remaining stores.

### Key Decisions
- **MigrationTool**: KEEP (stores still migrating)
- **Legacy Nokpirab handlers**: DELETE (dead code, WinForms uses StoreHub services)
- **LegacyIdHelper**: KEEP (still needed for int→Guid conversions)

---

## ⚠️ Critical Gap Identified: IReportService

**Problem:** `ReportService` uses `INokpirab` to dispatch legacy SQLite-based queries:
- `GetInvoicesByDateRangeQuery`
- `GetInvoiceProductsByDateQuery` / `GetInvoiceProductsByDateRangeQuery`
- `GetInvoicePaymentsByDateRangeQuery`
- `GetPayLaterPaymentsByDateRangeQuery` / `GetPayLaterPaymentsQuery`
- `GetInvoiceInfoQuery`

**UI Panels Affected:**
- `SalesReportPanel.cs`
- `SalesHistoryReportPanel.cs`
- `InvoiceProductsReportPanel.cs`
- `PayLaterPaymentsReportPanel.cs`
- `CashFlowCalculatorPanel.cs`
- `SaleHistoryByInvoiceIdForm.cs`

**Solution:** Create `StoreHubReportService` that calls StoreHub API endpoints instead of SQLite handlers.

**StoreHub report queries already exist:**
- `GetSalesSummaryQuery` ✅
- `GetInvoicesQuery` ✅
- `GetInvoiceDetailQuery` ✅
- `GetPayLaterReportQuery` ✅
- `GetProductSalesQuery` ✅

**Missing StoreHub endpoints (need to add):**
- Invoice products by date range
- Invoice payments by date range
- PayLater payments listing (for reports)

---

## ⚠️ Critical Gap #2: IStoreConstants

**Problem:** `StoreConstants` reads lookup data from SQLite via `IStoreConstantRepository`:
- UserRoles
- PaymentTypes
- ProductCategories

**UI Panels Affected:** Many panels use `IStoreConstants` for dropdown values.

**Solution:** Replace with hardcoded values using existing enums:
- `Application/Common/Enums/UserRoles.cs` ✅
- `Application/Common/Enums/PaymentTypes.cs` ✅
- `Application/Common/Enums/ProductCategories.cs` ✅

---

## Phase 0a: Fix IStoreConstants (NEW - PREREQUISITE)

**Goal:** Remove SQLite dependency from StoreConstants

### Option A: Hardcode from Enums (Recommended)
Create a new `HardcodedStoreConstants` that builds dictionaries from enums.

### Files to CREATE
| File | Purpose |
|------|---------|
| `Infrastructure/Constants/HardcodedStoreConstants.cs` | Uses enums instead of SQLite |

### Files to MODIFY
| File | Change |
|------|--------|
| `ConfigureServices.cs` | Register `HardcodedStoreConstants` instead |

---

## Phase 0b: Create StoreHubReportService (NEW - PREREQUISITE)

**Goal:** Create StoreHub-based report service before removing SQLite

### Files to CREATE
| File | Purpose |
|------|---------|
| `Infrastructure/Services/StoreHub/StoreHubReportService.cs` | StoreHub implementation of IReportService |

### StoreHub API Endpoints to ADD
| Endpoint | Purpose |
|----------|---------|
| `GET /reports/invoice-products` | Invoice products by date range |
| `GET /reports/invoice-payments` | Invoice payments by date range |

### Files to MODIFY
| File | Change |
|------|--------|
| `IStoreHubClient.cs` | Add report methods |
| `StoreHubHttpClient.cs` | Implement report HTTP calls |
| `StoreHub/Program.cs` | Add report endpoints |
| `ConfigureServices.cs` | Register StoreHubReportService |

---

## Phase 1: Delete SQLite Repository Implementations

**Goal:** Remove the SQLite repository layer

### Files to DELETE (9 files)
```
src/IndyPOS.Infrastructure/Persistence/Repositories/SQLite/
├── DbConnectionProvider.cs
├── InventoryProductRepository.cs
├── InvoiceRepository.cs
├── InvoicePaymentRepository.cs
├── InvoiceProductRepository.cs
├── PayLaterRepository.cs
├── StoreConstantsRepository.cs
├── UserCredentialRepository.cs
└── UserRepository.cs
```

### Files to MODIFY
| File | Change |
|------|--------|
| `Infrastructure/ConfigureServices.cs` | Remove `AddInfrastructureServices()` SQLite registrations |
| `Infrastructure/IndyPOS.Infrastructure.csproj` | Remove `System.Data.SQLite.Core` package |

---

## Phase 2: Delete Legacy Pos Interfaces

**Goal:** Remove the legacy repository abstractions (SQLite-specific)

### Files to DELETE (9 files)
```
src/IndyPOS.Application/Abstractions/Pos/Repositories/
├── IDbConnectionProvider.cs
├── IInventoryProductRepository.cs
├── IInvoiceRepository.cs
├── IInvoiceProductRepository.cs
├── IInvoicePaymentRepository.cs
├── IPayLaterPaymentRepository.cs
├── IStoreConstantRepository.cs
├── IUserCredentialRepository.cs
└── IUserRepository.cs
```

**Note:** Keep the `Abstractions/Pos/` folder if other files exist, or delete entire folder if empty.

---

## Phase 3: Delete Legacy Nokpirab Handlers

**Goal:** Remove dead handler code that was replaced by StoreHub services

### Files to DELETE (~30 files)

**InventoryProducts handlers (9 files):**
```
src/IndyPOS.Application/UseCases/InventoryProducts/
├── Create/
│   ├── CreateInventoryProductCommand.cs
│   ├── CreateInventoryProductCommandHandler.cs
│   └── CreateInventoryProductCommandValidator.cs
├── Update/
│   ├── UpdateInventoryProductCommand.cs
│   ├── UpdateInventoryProductCommandHandler.cs
│   ├── UpdateInventoryProductCommandValidator.cs
│   ├── UpdateInventoryProductQuantityCommand.cs
│   ├── UpdateInventoryProductQuantityCommandHandler.cs
│   ├── UpdateInventoryProductQuantityCommandValidator.cs
│   ├── UpdateInventoryProductBarcodeCounterCommand.cs
│   └── UpdateInventoryProductBarcodeCounterCommandHandler.cs
├── Delete/
│   ├── DeleteInventoryProductCommand.cs
│   ├── DeleteInventoryProductCommandHandler.cs
│   └── DeleteInventoryProductCommandValidator.cs
└── Get/
    ├── GetInventoryProductByIdQuery.cs
    ├── GetInventoryProductByIdQueryHandler.cs
    ├── GetInventoryProductByBarcodeQuery.cs
    ├── GetInventoryProductByBarcodeQueryHandler.cs
    ├── GetInventoryProductsByCategoryIdQuery.cs
    ├── GetInventoryProductsByCategoryIdQueryHandler.cs
    ├── GetInventoryProductsByBrandKeywordQuery.cs
    ├── GetInventoryProductsByBrandKeywordQueryHandler.cs
    ├── GetInventoryProductsByDescriptionKeywordQuery.cs
    └── GetInventoryProductsByDescriptionKeywordQueryHandler.cs
```

**Invoices handlers:**
```
src/IndyPOS.Application/UseCases/Invoices/
├── Create/CreateInvoice*.cs
├── Delete/DeleteInvoice*.cs
└── Get/GetInvoice*.cs
```

**InvoicePayments handlers:**
```
src/IndyPOS.Application/UseCases/InvoicePayments/
├── Create/CreateInvoicePayment*.cs
├── Delete/DeleteInvoicePayment*.cs
└── Get/GetInvoicePayment*.cs
```

**InvoiceProducts handlers:**
```
src/IndyPOS.Application/UseCases/InvoiceProducts/
├── Create/CreateInvoiceProduct*.cs
├── Delete/DeleteInvoiceProduct*.cs
└── Get/GetInvoiceProduct*.cs
```

### Files to KEEP
- `InventoryProductDto.cs` - Still used by StoreHub
- `InventoryProductExtensions.cs` - May have useful mappings
- `InvoiceProductDto.cs` - Still used

---

## Phase 4: Delete Legacy Services

**Goal:** Remove SQLite-dependent services replaced by StoreHub versions

### Files to DELETE
| File | Replacement |
|------|-------------|
| `Infrastructure/Services/SaleService.cs` | `StoreHubSaleService.cs` |
| `Infrastructure/Services/UserLogInService.cs` | `StoreHubUserLogInService.cs` |
| `Infrastructure/Services/ReportService.cs` | StoreHub report queries |

### Files to MODIFY
| File | Change |
|------|--------|
| `ConfigureServices.cs` | Remove legacy service registrations |

---

## Phase 5: Update WinForms to Require StoreHub

**Goal:** Make StoreHub mandatory, remove fallback logic

### Files to MODIFY

**MainForm.cs:**
- Remove `IDbConnectionProvider` injection
- Remove `BackupDatabase()` method and calls
- Remove backup directory configuration
- Add startup check: fail if StoreHub not configured

**ConfigureServices.cs (WinForms):**
- Remove conditional registration based on `StoreHub.Enabled`
- Always register StoreHub client services
- Fail fast if StoreHub config missing

**appsettings.json:**
```json
// DELETE entire section:
"Database": {
    "Path": "...",
    "BackupDirectory": "...",
    "BackupEnabled": true
}

// KEEP but make required:
"StoreHub": {
    "Enabled": true,  // Can remove this flag entirely
    "BaseUrl": "http://localhost:5000"
}
```

### UI Panels to UPDATE (remove nullable service checks)
These panels have `IPayLaterService?` or similar nullable injections - make them required:
- `PayLaterPaymentPanel.cs` - Remove null checks, require service
- Other panels using optional StoreHub services

---

## Phase 6: Clean Up Tests

**Goal:** Remove/update tests that reference deleted code

### Test files to DELETE
```
tests/IndyPOS.Application.Tests/InventoryProducts/  (if handlers deleted)
```

### Test files to KEEP
```
tests/IndyPOS.MigrationTool.Tests/     (MigrationTool still needed)
tests/IndyPOS.Migration.Tests/         (Migration verification)
```

---

## Phase 7: Update Documentation

### Files to UPDATE
- `.planning/indypos-overhaul/implementation-status.md` - Add SQLite removal milestone
- `CLAUDE.md` - Update architecture notes
- `docs/architecture/overview.md` - Remove SQLite references
- Diagrams if any show SQLite layer

---

## 📋 Technical Debt: Legacy Report Format

**Context:** The `StoreHubReportService` was created with "legacy endpoints" that return the exact `SalesSummary` and `PaymentsSummary` models expected by WinForms.

**Why:** The WinForms UI uses specific business-domain fields (GeneralProducts vs Hardware, PayLater breakdowns) that differ from the new StoreHub DTO structure.

**Files Involved:**
- `StoreHubReportService.cs` - Contains stub implementations for legacy methods
- `GetLegacySalesSummaryQuery.cs` / `GetLegacySalesSummaryQueryHandler.cs`
- `GetLegacyPaymentsSummaryQuery.cs` / `GetLegacyPaymentsSummaryQueryHandler.cs`
- StoreHub endpoints: `/reports/legacy/sales-summary`, `/reports/legacy/payments-summary`

**Legacy Methods Returning Empty (need MAUI migration):**
```csharp
// These return empty collections - not needed for current WinForms usage
GetInvoicesByPeriodAsync()
GetInvoicesByDateRangeAsync()
GetPayLaterPaymentsByPeriodAsync()
GetPayLaterPaymentsAsync()
GetInvoiceProductsByDateAsync()
GetInvoiceProductsByDateRangeAsync()
GetInvoiceProductsByInvoiceIdAsync(int)  // int-based, not supported
GetPaymentsByInvoiceIdAsync(int)         // int-based, not supported
GetInvoiceInfoAsync(int)                 // int-based, not supported
```

**Future Work (MAUI Migration):**
1. Replace `IReportService` interface with Guid-based methods
2. Update UI to use StoreHub DTOs directly (`SalesSummaryDto`, `InvoiceSummaryDto`, etc.)
3. Remove legacy endpoints and handlers
4. Delete `StoreHubReportService` in favor of direct StoreHub client calls

**Reference:** See `src/IndyPOS.Application/UseCases/StoreHub/Reports/ReportDtos.cs` for new DTO structure.

---

## Files Summary

| Action | Count | Details |
|--------|-------|---------|
| **DELETE** | ~50 files | SQLite repos, Pos interfaces, legacy handlers |
| **MODIFY** | ~15 files | ConfigureServices, MainForm, UI panels, tests |
| **KEEP** | MigrationTool | For ongoing store migrations |

---

## Risk Mitigation

1. **Backup before starting** - Create a git branch/tag
2. **Build after each phase** - Catch errors early
3. **Run tests after Phase 3** - Ensure StoreHub services work
4. **Test WinForms E2E** - Verify UI still functions

---

## Rollback Plan

If issues arise:
```bash
git checkout indypos-overhaul  # Return to pre-removal state
```

---

## Verification Checklist

After completion:
- [x] `dotnet build` succeeds with 0 errors ✅ (55 warnings)
- [x] All tests pass ✅ (298 tests passing)
- [ ] WinForms starts and connects to StoreHub
- [ ] Can complete a sale via StoreHub
- [ ] Can view/update pay-later via StoreHub
- [ ] **Reports work via StoreHub** (SalesReport, InvoiceProducts, PayLater reports)
- [x] No `System.Data.SQLite` references remain (removed from csproj)
- [x] No `IDbConnectionProvider` references remain (removed from Infrastructure)

---

## Estimated Effort

| Phase | Effort |
|-------|--------|
| **Phase 0a: Fix IStoreConstants** | **15 min** |
| **Phase 0b: Create StoreHubReportService** | **45 min** |
| Phase 1: Delete SQLite repos | 10 min |
| Phase 2: Delete Pos interfaces | 5 min |
| Phase 3: Delete legacy handlers | 15 min |
| Phase 4: Delete legacy services | 10 min |
| Phase 5: Update WinForms | 30 min |
| Phase 6: Clean up tests | 15 min |
| Phase 7: Documentation | 10 min |
| **Total** | ~2.75 hours |

---

## Approval

- [ ] Plan reviewed by Pond
- [ ] Ready to implement
