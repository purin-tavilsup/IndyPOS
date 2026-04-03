# Epic G: Desktop Integration - COMPLETE

**Completed:** 2026-04-01

## Goal
Desktop app becomes hub client; decommission SQLite

## Tasks Completed

| Task | Description | Status |
|------|-------------|--------|
| G1 | Desktop becomes hub client | Complete |
| G2 | Prepare for tablet | Deferred (post-MAUI) |
| G3 | Decommission direct SQLite writes | Complete |

## G1: Desktop Hub Client (2026-03-27)

**Architecture:** WinForms -> StoreHub API -> PostgreSQL

**New Abstractions:**
- `IStoreHubClient` - HTTP client for StoreHub API
- `IProductCacheService` - Local product cache with sync/clear

**New Implementations:**
- `StoreHubHttpClient` - HTTP client with JWT auth
- `ProductCacheService` - In-memory cache with barcode lookup
- `StoreHubSaleService` - ISaleService using StoreHub API
- `StoreHubUserLogInService` - IUserLogInService using StoreHub auth

**E2E Testing:**
- `DevelopmentDataSeeder` - Seeds test users and products
- `StoreHubHttpClientTests` - 8 tests with mocked HTTP
- `StoreHubE2ETests` - 5 tests using WireMock

## G3: SQLite Removal (2026-04-01)

**Phases Completed:**
1. Delete SQLite repositories (9 files)
2. Delete Pos interfaces (8 files)
3. Delete legacy Nokpirab handlers (~70 files)
4. Delete legacy services (SaleService, UserLogInService, ReportService, StoreConstants)
5. Update WinForms (MainForm, UserLogInPanel, UsersPanel, AddNewUserForm)
6. Clean up tests

**Key Changes:**
- Removed `System.Data.SQLite.Core` and `Dapper` packages
- Created `LegacyDtos.cs` for backward compatibility
- User management disabled in WinForms (use CloudAPI)
- Database backup feature removed
- **174 files changed, 5,699 lines deleted**

**MigrationTool:** KEPT for ongoing store migrations

## Technical Debt

`StoreHubReportService` has stub implementations for int-based methods:
- `GetInvoicesByPeriodAsync()`, `GetInvoicesByDateRangeAsync()`
- `GetPayLaterPaymentsByPeriodAsync()`, `GetPayLaterPaymentsAsync()`
- `GetInvoiceProductsByDateAsync()`, `GetInvoiceProductsByDateRangeAsync()`
- `GetInvoiceProductsByInvoiceIdAsync(int)`, `GetPaymentsByInvoiceIdAsync(int)`
- `GetInvoiceInfoAsync(int)`

**Future Work (MAUI Migration):**
1. Replace `IReportService` interface with Guid-based methods
2. Update UI to use StoreHub DTOs directly
3. Remove legacy endpoints and handlers

## Deliverable
Desktop uses hub API; local Postgres is single source of truth; SQLite fully removed
