# Current SQLite Schema Analysis

Version: 1.0.0
Date: 2026-03-03
Source: `Store.db` from production backup

## Purpose

This document analyzes the current SQLite schema to inform Epic D (PostgreSQL schema design). It maps current tables to the planned PostgreSQL structure.

---

## Current Tables (SQLite)

```
Core Tables:
  • Invoice              (sales transactions)
  • InvoiceProduct       (line items)
  • Payment              (payments per invoice)
  • InventoryProduct     (products + stock)
  • Customers            (customer records)

Reference Tables:
  • ProductCategory      (categories)
  • PaymentType          (payment methods)
  • UserRole             (user roles)
  • User                 (users)
  • UserCredential       (login credentials)

Financial Tables:
  • PayLater             (credit sales)
  • Installments         (installment payments)

Utility Tables:
  • ProductBarcodeCounter (barcode generation)
  • sqlite_sequence       (SQLite internal)
```

---

## Current Schema Details

### Invoice
```
InvoiceId            INTEGER         PK NOT NULL
UserId               INTEGER          NOT NULL
DateCreated          TEXT              DEFAULT CURRENT_TIMESTAMP
Total                NUMERIC          NOT NULL DEFAULT 0
```

**Observations:**
- Uses INTEGER PK (auto-increment)
- `DateCreated` is TEXT (should be proper timestamp in PG)
- Missing: `CustomerId`, `Discount`, `Tax`, `Status`
- No `PublicId` or `StoreId` (need to add for sync)

---

### InvoiceProduct (Line Items)
```
InvoiceProductId     INTEGER         PK NOT NULL
Priority             INTEGER
InvoiceId            INTEGER          NOT NULL
InventoryProductId   INTEGER          NOT NULL
Barcode              TEXT
Description          TEXT             NOT NULL
Manufacturer         TEXT
Brand                TEXT
Category             INTEGER
Quantity             INTEGER          NOT NULL DEFAULT 1
IsTrackable          INTEGER           DEFAULT 1
DateCreated          TEXT              DEFAULT CURRENT_TIMESTAMP
Note                 TEXT
UnitPrice            NUMERIC          NOT NULL DEFAULT 0
GroupPrice           NUMERIC          NOT NULL DEFAULT 0
IsGroupProduct       INTEGER          NOT NULL DEFAULT 0
OriginalUnitPrice    NUMERIC          NOT NULL DEFAULT 0
```

**Observations:**
- Denormalizes product data (Description, Manufacturer, Brand, Category) ✅ GOOD
- Has "group pricing" concept (GroupPrice, IsGroupProduct)
- `IsTrackable` determines if stock tracking is needed
- No `PublicId` (need to add)

---

### Payment
```
PaymentId            INTEGER         PK NOT NULL
InvoiceId            INTEGER          NOT NULL
PaymentTypeId        INTEGER          NOT NULL DEFAULT 1
DateCreated          TEXT              DEFAULT CURRENT_TIMESTAMP
Note                 TEXT
Amount               NUMERIC          NOT NULL DEFAULT 0
```

**Observations:**
- Simple structure ✅
- `PaymentTypeId` links to PaymentType table
- No `PublicId` (need to add)

---

### InventoryProduct (Products + Stock)
```
InventoryProductId   INTEGER         PK NOT NULL
Barcode              TEXT             NOT NULL
Description          TEXT             NOT NULL
Manufacturer         TEXT
Brand                TEXT
Category             INTEGER
QuantityInStock      INTEGER          NOT NULL DEFAULT 1
GroupPriceQuantity   INTEGER
IsTrackable          INTEGER           DEFAULT 1
DateCreated          TEXT             NOT NULL DEFAULT CURRENT_TIMESTAMP
DateUpdated          TEXT
UnitPrice            NUMERIC          NOT NULL DEFAULT 0
GroupPrice           NUMERIC          NOT NULL DEFAULT 0
```

**Observations:**
- ⚠️  **SNAPSHOT-BASED STOCK** (`QuantityInStock`)
- Need to convert to **MOVEMENT-BASED** for cloud sync
- Has group pricing logic
- Missing: `CostPrice`, `IsActive`, `PublicId`, `Code` (separate from Barcode)

---

### Customers
```
CustomerId           INTEGER         PK NOT NULL
FirstName            TEXT
LastName             TEXT
DateCreated          TEXT              DEFAULT CURRENT_TIMESTAMP
DateUpdated          TEXT
```

**Observations:**
- Very simple structure
- Missing: `Phone`, `Email`, `LoyaltyPoints`, `PublicId`

---

## Mapping to PostgreSQL Schema

### Invoice (SQLite) → invoice (PostgreSQL)

| SQLite Column | PostgreSQL Column | Notes |
|---------------|-------------------|-------|
| InvoiceId (PK) | id (BIGSERIAL) | Keep for backward compat |
| ➕ NEW | public_id (UUID) | **ADD** for sync |
| ➕ NEW | store_id (VARCHAR) | **ADD** for multi-store |
| ➕ NEW | invoice_number (VARCHAR) | **ADD** for human-readable ID |
| UserId | user_id (BIGINT) | Keep |
| Total | total_amount (NUMERIC) | Rename (tax-inclusive) |
| ➕ NEW | status (VARCHAR) | **ADD** (Pending/Completed/Void) |
| DateCreated | created_utc (TIMESTAMPTZ) | Change type, rename |
| ➕ NEW | last_modified_utc (TIMESTAMPTZ) | **ADD** |

**Removed from original design:**
- ❌ customer_id - Customer table not used
- ❌ subtotal - Not needed (just use total_amount)
- ❌ discount - Not used in current system
- ❌ tax - Not needed (Thai market: all prices tax-inclusive)

---

### InvoiceProduct (SQLite) → invoice_line (PostgreSQL)

| SQLite Column | PostgreSQL Column | Notes |
|---------------|-------------------|-------|
| InvoiceProductId (PK) | id (BIGSERIAL) | Keep for backward compat |
| ➕ NEW | public_id (UUID) | **ADD** for sync |
| InvoiceId | invoice_id (BIGINT) | Keep |
| InventoryProductId | ❌ REMOVE | Don't store internal ID |
| ➕ NEW | product_public_id (UUID) | **ADD** reference to product |
| Description | product_name (VARCHAR) | Rename, keep snapshot |
| Barcode | ❌ REMOVE | Not needed in line item |
| Manufacturer | ❌ REMOVE | Too detailed for line item |
| Brand | ❌ REMOVE | Too detailed for line item |
| Category | ❌ REMOVE | Too detailed for line item |
| Quantity | quantity (INT) | Keep |
| UnitPrice | unit_price (NUMERIC) | Keep (use GroupPrice if applicable) |
| GroupPrice | ❌ REMOVE | Handled via unit_price |
| IsGroupProduct | ❌ REMOVE | Not needed in new schema |
| OriginalUnitPrice | ❌ REMOVE | Redundant |
| ➕ NEW | line_total (NUMERIC) | **ADD** (unit_price * quantity) |

**Note on Group Pricing:**
- If group price applies: set unit_price = GroupPrice value
- If regular: set unit_price = UnitPrice value
- No need for separate discount field or IsGroupProduct flag
| IsTrackable | ❌ REMOVE | Not needed in line |
| Priority | ❌ REMOVE | Not needed |
| Note | ❌ REMOVE or keep | Decision needed |
| DateCreated | created_utc (TIMESTAMPTZ) | Keep |

---

### Payment (SQLite) → payment (PostgreSQL)

| SQLite Column | PostgreSQL Column | Notes |
|---------------|-------------------|-------|
| PaymentId (PK) | id (BIGSERIAL) | Keep for backward compat |
| ➕ NEW | public_id (UUID) | **ADD** for sync |
| InvoiceId | invoice_id (BIGINT) | Keep |
| PaymentTypeId | ❌ REMOVE | Replace with direct method name |
| ➕ NEW | method (VARCHAR) | **ADD** (Cash/Card/Transfer/PayLater) |
| Amount | amount (NUMERIC) | Keep |
| Note | reference (VARCHAR) | Rename for bank ref, etc. |
| DateCreated | created_utc (TIMESTAMPTZ) | Keep |

---

### InventoryProduct (SQLite) → product + inventory_movement (PostgreSQL)

**Split into TWO tables:**

#### product (Master Data)
| SQLite Column | PostgreSQL Column | Notes |
|---------------|-------------------|-------|
| InventoryProductId | id (BIGSERIAL) | Keep for backward compat |
| ➕ NEW | public_id (UUID) | **ADD** for sync |
| Barcode | barcode (VARCHAR) | Keep, nullable |
| ➕ NEW | code (VARCHAR) | **ADD** internal SKU |
| Description | name (VARCHAR) | Rename |
| ➕ NEW | description (TEXT) | **ADD** long description |
| Manufacturer | ❌ REMOVE or keep | Decision needed |
| Brand | ❌ REMOVE or keep | Decision needed |
| Category | category (VARCHAR) | Change to direct string |
| UnitPrice | unit_price (NUMERIC) | Keep |
| GroupPrice | ❌ REMOVE | Handle via pricing rules |
| GroupPriceQuantity | ❌ REMOVE | Handle via pricing rules |
| ➕ NEW | cost_price (NUMERIC) | **ADD** for margin tracking |
| IsTrackable | ❌ REMOVE | All products tracked via movements |
| ➕ NEW | is_active (BOOLEAN) | **ADD** soft delete |
| DateCreated | created_utc (TIMESTAMPTZ) | Keep |
| DateUpdated | last_modified_utc (TIMESTAMPTZ) | Keep |

#### inventory_movement (NEW - Movement-Based)
| Column | Type | Notes |
|--------|------|-------|
| id | BIGSERIAL | Internal PK |
| public_id | UUID | For sync |
| store_id | VARCHAR | Multi-store |
| product_public_id | UUID | FK to product |
| quantity_delta | INT | **+/- amount** |
| reason | VARCHAR | Sale/Restock/Adjustment/etc. |
| reference_public_id | UUID | Link to Invoice, etc. |
| note | VARCHAR | Optional note |
| created_utc | TIMESTAMPTZ | When movement occurred |

**Migration Strategy for Stock:**
```sql
-- One-time conversion of QuantityInStock to movements
INSERT INTO inventory_movement (
  public_id,
  store_id,
  product_public_id,
  quantity_delta,
  reason,
  created_utc
)
SELECT
  gen_random_uuid(),
  'STORE-001',
  p.public_id,
  ip.QuantityInStock,  -- Initial stock as positive movement
  'InitialStock',
  ip.DateCreated
FROM InventoryProduct ip
JOIN product p ON p.id = ip.InventoryProductId
WHERE ip.QuantityInStock > 0;
```

---

### Customers (SQLite) → ❌ NOT MIGRATED

**Decision:** Do NOT migrate Customers table

**Rationale:**
1. ✅ Customers table is EMPTY (0 rows in production DB)
2. ✅ CustomerId property exists in Invoice entity but is ALWAYS NULL (line 434 in SaleService.cs)
3. ✅ Invoice table in SQLite doesn't even have a CustomerId column
4. ✅ No customer functionality in current system

**Action Plan:**
1. Epic B: Remove CustomerId from codebase
   - Remove from Invoice entity
   - Remove from CreateInvoiceCommand
   - Remove from InvoiceDto
   - Remove from SaleService.cs
2. Epic D: Do NOT create customer table in PostgreSQL
3. Future: If customer functionality is needed, design from scratch

---

## Data Type Conversions

| SQLite Type | PostgreSQL Type | Reason |
|-------------|-----------------|--------|
| INTEGER | BIGSERIAL (PK) or BIGINT | Future-proof |
| TEXT (dates) | TIMESTAMPTZ | Proper timezone support |
| TEXT (strings) | VARCHAR(n) | Enforce max length |
| TEXT (long) | TEXT | Keep for long content |
| NUMERIC | NUMERIC(18,2) | Explicit precision for money |
| INTEGER (booleans) | BOOLEAN | Proper type |

---

## New Concepts for PostgreSQL

### 1. PublicId (UUID)
```sql
ALTER TABLE invoice ADD COLUMN public_id UUID UNIQUE NOT NULL;
ALTER TABLE invoice_line ADD COLUMN public_id UUID UNIQUE NOT NULL;
-- etc.
```

### 2. StoreId
```sql
ALTER TABLE invoice ADD COLUMN store_id VARCHAR(50) NOT NULL;
ALTER TABLE inventory_movement ADD COLUMN store_id VARCHAR(50) NOT NULL;
-- etc.
```

### 3. Outbox Event
```sql
CREATE TABLE outbox_event (
  id BIGSERIAL PRIMARY KEY,
  public_id UUID UNIQUE NOT NULL,
  store_id VARCHAR(50) NOT NULL,
  type VARCHAR(100) NOT NULL,
  payload_json TEXT NOT NULL,
  created_utc TIMESTAMPTZ NOT NULL,
  attempts INT NOT NULL DEFAULT 0,
  last_attempt_utc TIMESTAMPTZ,
  next_retry_utc TIMESTAMPTZ,
  status VARCHAR(20) NOT NULL
);
```

---

## Migration Challenges

### Challenge 1: TEXT Dates → TIMESTAMPTZ
**Problem:** SQLite stores dates as TEXT (e.g., "2026-03-03 10:30:00")
**Solution:**
```sql
-- PostgreSQL can parse ISO 8601 strings
INSERT INTO invoice (created_utc)
SELECT DateCreated::timestamptz
FROM old_invoice;
```

### Challenge 2: QuantityInStock → Movements
**Problem:** Current system uses snapshot-based stock
**Solution:** Create initial movement records representing current stock

### Challenge 3: CustomerId Never Used
**Problem:** CustomerId exists in code but is always null and not in DB schema
**Solution:** Remove CustomerId entirely from codebase and don't include in PostgreSQL schema

### Challenge 4: Group Pricing
**Problem:** GroupPrice, IsGroupProduct logic embedded in schema
**Solution:**
- Option A: Convert to line-level discount
- Option B: Keep as product pricing tier (future work)
- **Recommended:** Option A for simplicity

---

## Migration Script Outline

```sql
-- Phase 1: Create new tables with new schema
CREATE TABLE invoice (...);
CREATE TABLE invoice_line (...);
-- etc.

-- Phase 2: Migrate data + generate PublicIds
INSERT INTO invoice (
  id, public_id, store_id, invoice_number, user_id,
  total_amount, status, created_utc, last_modified_utc
)
SELECT
  InvoiceId,
  gen_random_uuid(),  -- Generate PublicId
  'STORE-001',        -- Default StoreId
  'INV-' || InvoiceId,  -- Generate invoice number
  UserId,
  Total,
  'Completed',  -- Default status
  DateCreated::timestamptz,
  DateCreated::timestamptz
FROM old_Invoice;

-- Phase 3: Migrate line items
INSERT INTO invoice_line (...)
SELECT ... FROM old_InvoiceProduct;

-- Phase 4: Create inventory movements from current stock
INSERT INTO inventory_movement (...)
SELECT ... FROM old_InventoryProduct;

-- Phase 5: Verify data integrity
SELECT COUNT(*) FROM invoice;  -- Should match old count
-- etc.
```

---

## Backward Compatibility Notes

- Keep `id` (BIGSERIAL) for internal references
- Use `public_id` (UUID) for all sync/API operations
- Existing Windows.Forms app can continue using `id` initially
- Gradually refactor to use `public_id` in application layer

---

**Next Steps:**
1. Review this analysis with team
2. Decide on optional columns (Manufacturer, Brand, etc.)
3. Design complete PostgreSQL schema (Epic D)
4. Write migration scripts
5. Test migration with production backup

---

**Related:**
- Target Schema: `04-database-schema.md`
- Implementation Plan: `../IndyPOS_OfflineFirst_CloudSync_Plan.md`
