# IndyPOS - Current Status

> Quick checkpoint for session start / context handoff

## Current State

| Field | Value |
|-------|-------|
| **Branch** | `indypos-overhaul` |
| **Sprint** | Sprint 7 |
| **Phase** | Epic M In Progress (M1-M6 done) |
| **Blocked?** | No |

## In Progress

### Epic M: Multi-Store Type Support 🟡 IN PROGRESS

Support multiple store types (GeneralHardware, Minimart, CoffeeShop).

**All Decisions Finalized:**
| Aspect | Decision |
|--------|----------|
| StoreHub Location | Local per store (most stores = 1 POS) |
| Offline Support | Local PostgreSQL required (offline-first) |
| StoreId Generation | Manual UUID by System Admin |
| Central Database | One DB per store type (`generalHardware`, `minimart`, `coffeeShop`) |
| Store Type | Immutable after installation |
| Migration | Migrate existing 1 store → `generalHardware` DB |
| **StoreId on Entities** | **Root entities only** (child entities inherit via JOIN) |

**Task Progress:**
| Task | Status | Notes |
|------|--------|-------|
| M1: StoreType enum + StoreTypeFeatures | ✅ Done | `Domain/Enums/`, `Domain/ValueObjects/` |
| M2: Update config schemas | ✅ Done | `StoreIdentityOptions` has Type; `IStoreIdentityService` exposes Features |
| M3: Add StoreId to entities | ✅ Done | Added to Product, StoreSetting; repositories filter by StoreId |
| M4: Update repositories for StoreId | ✅ Done | PayLaterRepository now filters via Invoice JOIN |
| M5: Verify StoreHub API | ✅ Done | StoreId passed to commands via IStoreIdentityService |
| M6: Add feature validation | ✅ Done | PayLater blocked for non-GeneralHardware stores |
| M7-M13 | Pending | UI, installer, CloudApi, migrations, docs |

**M4-M6 Summary:**
- PayLaterRepository filters by StoreId via JOIN to Invoice
- CompleteSaleCommand validates PayLater payment method against store type
- PayLater queries/commands validate `Features.PayLaterEnabled`
- MockStoreIdentityService added to test project for unit testing
- EF Core migration: `AddStoreIdToProductAndStoreSetting`

**Plan:** `.planning/indypos-overhaul/drafts/epic-m-multi-store-type.md`

## Next Actions (Priority Order)

### 1. Implement VM Testing Scripts (Phase 1 - Quick Win)
- [ ] Create `scripts/vm-testing/Initialize-TestVM.ps1`
- [ ] Create `scripts/vm-testing/New-CleanSnapshot.ps1`
- [ ] Create `scripts/vm-testing/Test-IndyPOSInstaller.ps1`
- [ ] Create `scripts/vm-testing/Test-IndyPOSInstallation.ps1`
- [ ] Manual test & debug

### 2. Test Epic V Installer in VM
- [ ] Create Hyper-V VM with Windows 11
- [ ] Run `scripts\publish.ps1` to create Velopack packages
- [ ] Run `installer\build-installer.ps1` to build bootstrapper
- [ ] Test full installation in VM
- [ ] Test update scenarios

### 3. Continue Epic M (M7-M13)
- M7: Update First-Run Wizard (store type selection)
- M8: Update WinForms UI to respect feature flags
- M9: Update CloudApi for store type routing
- M10-M13: Installer, migrations, docs

## Key Files

| Purpose | Path |
|---------|------|
| Full plan | `.planning/indypos-overhaul/PLAN.md` |
| Epic M draft | `.planning/indypos-overhaul/drafts/epic-m-multi-store-type.md` |
| VM testing plan | `.planning/indypos-overhaul/drafts/vm-installer-testing-plan.md` |
| VM testing guide | `docs/development/vm-testing-guide.md` |
| Bootstrapper project | `installer/IndyPOS.Bootstrapper/` |
| Publish script | `scripts/publish.ps1` |
| Store installation guide | `docs/operations/store-installation-guide.md` |
| Mock for tests | `tests/IndyPOS.Mock/MockStoreIdentityService.cs` |

## Quick Context

- **Epic V (Velopack):** ✅ Complete - one-stop installer with auto-updates
- **Epic M (Multi-Store):** 🟡 Core domain complete (M1-M6), UI/installer pending (M7-M13)

## Stats

- **Tests:** 301 passing (214 + 15 + 23 + 49)
- **Build:** 0 errors, 0 warnings

---
*Last updated: 2026-04-06*
