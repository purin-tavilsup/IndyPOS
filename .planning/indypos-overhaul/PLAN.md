# IndyPOS Overhaul - Implementation Plan

**Last Updated:** 2026-04-03
**Progress:** ~95% Complete

---

## Epic Overview

| Epic | Name | Status |
|------|------|--------|
| 0 | Extract Business Logic | Complete |
| A | Prepare Codebase | Complete |
| B | Remove Deprecated PG Report | Complete |
| C | StoreHub Service + Aspire | Complete |
| D | Schema Design | Complete |
| E | Outbox + SyncWorker | Complete |
| F | Cloud API | Complete |
| G | Desktop Integration | Complete |
| H | Testing & Rollout | Complete |
| I | Cloud Infrastructure | Not Started |
| S | Security Hardening | 5/9 Complete |

> For detailed epic history, see `completed/` folder

---

## Current Focus

### Epic S: Security Hardening (Partial)

**Completed:**
- S1: POS offline authentication (BCrypt, JWT)
- S2: Local user cache (sync from cloud)
- S3: RBAC implementation (capability-based)
- S4: CloudApi user management
- S5: RSA key signing + DPAPI secrets

**Remaining (LOW priority):**
| Task | Description | Priority |
|------|-------------|----------|
| S6 | Key rotation support | LOW |
| S7 | Security audit logging | LOW |
| S8 | Rate limiting | LOW |
| S9 | Secrets management | LOW (covered by S5) |

### Epic I: Cloud Infrastructure (Not Started)

**Prerequisites:** Epic H3 (Pilot rollout) must complete first

| Task | Description |
|------|-------------|
| I1 | Provision DigitalOcean Droplet |
| I2 | Provision DO Managed PostgreSQL |
| I3 | Deploy CloudApi to Droplet |
| I4 | Configure SyncWorker with real CloudApi |
| I5 | Multi-store sync testing |
| I6 | Central reporting dashboard |

**Cloud Specs:**
- Droplet: Basic Premium AMD (2 GB RAM, 1 vCPU, 50 GB SSD)
- Managed PostgreSQL: Smallest tier (1 GB RAM)
- Region: Singapore
- Monthly cost: ~$20-30 USD

---

## Backlog

| Item | Description | Priority |
|------|-------------|----------|
| Migration `--sync-to-cloud` | Create outbox events for migrated invoices | LOW |
| MAUI Migration | Replace WinForms with MAUI | Future |
| Legacy Report Cleanup | Remove int-based report methods | MAUI Migration |

---

## Technical Debt

### StoreHubReportService Legacy Methods
Stub implementations return empty collections. Address during MAUI migration:
- `GetInvoicesByPeriodAsync()`, `GetInvoicesByDateRangeAsync()`
- `GetPayLaterPaymentsByPeriodAsync()`, `GetPayLaterPaymentsAsync()`
- `GetInvoiceProductsByDateAsync()`, `GetInvoiceProductsByDateRangeAsync()`
- `GetInvoiceProductsByInvoiceIdAsync(int)`, `GetPaymentsByInvoiceIdAsync(int)`
- `GetInvoiceInfoAsync(int)`

---

## Statistics

| Metric | Value |
|--------|-------|
| Total Epics | 10 |
| Completed Epics | 9 |
| Total Tests | 298 |
| Build Status | 0 Errors, 55 Warnings |

---

## Reference Documentation

| Doc | Location |
|-----|----------|
| Current status | `.claude/STATUS.md` |
| Session history | `.claude/session-log.md` |
| Completed epics | `.planning/indypos-overhaul/completed/` |
| Security spec | `.planning/indypos-overhaul/security/` |
| Diagrams | `.planning/indypos-overhaul/diagrams/` |
| Operations docs | `docs/operations/` |

---

## Solution Structure

```
src/
  Core/
    IndyPOS.Domain
    IndyPOS.Application
    IndyPOS.Infrastructure
  DesktopApp/
    IndyPOS.Windows.Forms
  Services/
    IndyPOS.StoreHub
    IndyPOS.CloudApi
  DevAppHost/
    IndyPOS.AppHost
    IndyPOS.ServiceDefaults
  Tools/
    IndyPOS.MigrationTool

tests/
  Core/ -> IndyPOS.Application.Tests
  DesktopApp/ -> IndyPOS.Windows.Forms.Tests
  Services/ -> IndyPOS.StoreHub.IntegrationTests
  Tools/ -> IndyPOS.Migration.Tests, IndyPOS.MigrationTool.Tests
```

---

## Quick Commands

```bash
# Run tests
dotnet test

# Build
dotnet build

# Run with Aspire (requires Docker)
dotnet run --project src/IndyPOS.AppHost --launch-profile https
# Dashboard: https://localhost:17222
```
