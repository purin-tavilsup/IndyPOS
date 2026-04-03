# Session Log Archive

> Historical sessions (older than recent 5-10)

---

## 2026-03-06: Epic 0 - Business Logic Extraction

**Epic:** 0 | **Status:** Complete

### Summary
Completed Epic 0: Extract Business Logic from UI codebehind. After auditing codebase, discovered it was already well-structured with CQRS (Nokpirab). Only minor extractions needed.

### Key Accomplishments
1. **Product Total Calculation (DRY)** - Extracted to `Product.GetTotal()` and `InvoiceProductDto.GetTotal()`
2. **PayLater Completion Logic** - Moved to domain entity and DTO methods
3. **CashFlow Calculator** - Moved static methods to `CashFlowData` instance methods
4. **Unit Tests** - Created 24 tests (ProductTests, CashFlowDataTests, InvoiceProductDtoTests, PayLaterPaymentDtoTests)

### Key Finding
**PayLater Exclusive Payment Type Rule:** If invoice has PayLater payment, it CANNOT have other payment types combined.

---

## 2026-03-03 (Session 2): Schema Finalization & PayLater Feature

**Status:** Complete

### Summary
Completed final schema design decisions and discovered/documented PayLater feature.

### Key Accomplishments
1. **Schema Naming Convention** - Renamed `public_id` -> `id` (UUID type)
2. **PayLater Feature Discovered** - 5,181 records, 144 incomplete, essential for low-income customers
3. **Documents Updated** - All schema docs aligned with final decisions

### Final PostgreSQL Schema (7 Core Tables)
- Business: invoice, invoice_line, payment, pay_later, product
- Infrastructure: inventory_movement, outbox_event

---

## 2026-03-03 (Session 1): Workspace Setup & Planning Review

**Status:** Complete

### Summary
Set up project workspace and reviewed all planning documentation for IndyPOS overhaul.

### Key Accomplishments
1. **Reviewed Planning Docs** - Architecture, ADRs, implementation plans
2. **Confirmed Architecture** - PostgreSQL, GUID identifiers, Outbox pattern, Windows Service
3. **Set Up Workspace** - Created `.claude/` folder, CLAUDE.md, diagrams
4. **Schema Simplification** - Removed unused fields (discount, tax, subtotal)
5. **Customer/CustomerId Audit** - Confirmed not used, removed

### Files Created
- CLAUDE.md (project context)
- 8 architecture diagrams
- Migration plan
- Schema decision documents

---

## Key Decisions Made (Historical)

| Date | Decision | Reason |
|------|----------|--------|
| 2026-03-03 | Remove Customer entity | Never used (0 rows in SQLite) |
| 2026-03-03 | Remove discount/tax fields | Thai market uses tax-inclusive pricing |
| 2026-03-03 | GUID primary keys | Distributed identity for cloud sync |
| 2026-03-03 | Schema before StoreHub | Need clear schema for SQLite -> PostgreSQL |
| 2026-03-06 | PayLater exclusive rule | Business rule - no combined payment types |
