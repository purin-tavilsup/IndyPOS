# Schema Comparison: SQLite (Current) vs PostgreSQL (Proposed)

Version: 1.0.0
Date: 2026-03-03

## Guiding Principle

**"Maintain schema from SQLite to PostgreSQL unless it's necessary or better to have additional or alternative"** - Pond

Only add fields that are:
1. **Necessary** for core functionality (sync, multi-store)
2. **Clearly Better** (proper data types, integrity)

Avoid premature optimization and unnecessary complexity.

---

## Invoice Table

### SQLite (Current)
```sql
CREATE TABLE Invoice (
  InvoiceId INTEGER PRIMARY KEY,
  UserId INTEGER NOT NULL,
  DateCreated TEXT DEFAULT CURRENT_TIMESTAMP,
  Total NUMERIC NOT NULL DEFAULT 0
);
```

### PostgreSQL (Proposed - Original)
```sql
CREATE TABLE invoice (
  id UUID PRIMARY KEY,                       -- ✅ UUID as PK
  store_id VARCHAR(50) NOT NULL,             -- ❓
  invoice_number VARCHAR(50) NOT NULL,       -- ❓
  user_id BIGINT NOT NULL,
  total_amount NUMERIC(18,2) NOT NULL,
  status VARCHAR(20) NOT NULL,               -- ❓
  created_utc TIMESTAMPTZ NOT NULL,
  last_modified_utc TIMESTAMPTZ NOT NULL     -- ❓
);
```

### Field-by-Field Analysis

| Field | SQLite | Proposed PostgreSQL | Necessary? | Reason |
|-------|--------|---------------------|------------|--------|
| **id** / InvoiceId | ✅ INTEGER PK | ✅ UUID PK | ✅ YES | UUID for distributed systems |
| **store_id** | ❌ No | VARCHAR(50) NOT NULL | ✅ YES | For multi-store - necessary |
| **invoice_number** | ❌ No | VARCHAR(50) NOT NULL | **❓ REVIEW** | Human-readable ID - necessary? |
| **user_id** / UserId | ✅ INTEGER | ✅ BIGINT | ✅ YES | Same field, better type |
| **total_amount** / Total | ✅ NUMERIC | ✅ NUMERIC(18,2) | ✅ YES | Same field, explicit precision |
| **status** | ❌ No | VARCHAR(20) NOT NULL | **❓ REVIEW** | Track status - necessary? |
| **created_utc** / DateCreated | ✅ TEXT | ✅ TIMESTAMPTZ | ✅ YES | Better type (was TEXT) |
| **last_modified_utc** | ❌ No | TIMESTAMPTZ NOT NULL | **❓ REVIEW** | Track modifications - necessary? |

---

## Questions for Review

### Q1: UUID as Primary Key ✅ DECIDED

**Decision:** Use UUID as `id` (primary key) for distributed systems

**Reason:** Microservices-ready, no ID collision risk across stores

---

### Q2: Do we need `invoice_number`?

**Purpose:** Human-readable invoice number (e.g., "INV-2026-001234")

**Current system:** Uses `InvoiceId` directly (e.g., 79, 80, 81)

**Questions:**
- Do users need formatted invoice numbers?
- Can we just use `id` like current system?
- Or generate on-the-fly: `"INV-" + id` when displaying?

**Your decision needed:** Keep or remove?

---

### Q3: Do we need `status` field?

**Purpose:** Track invoice state (Pending/Completed/Void)

**Current system:** No status field - all invoices are implicitly "completed"

**Questions:**
- Do you need to void/cancel invoices?
- Do you need pending/draft invoices?
- Or is every saved invoice automatically completed (like now)?

**Your decision needed:** Keep or remove?

---

### Q4: Do we need `last_modified_utc`?

**Purpose:** Track when invoice was last updated

**Current system:** No modification tracking - invoices are immutable

**Questions:**
- Do invoices ever get modified after creation?
- Or are they immutable (create-only)?
- Would you ever need to know "when was this changed?"

**Your decision needed:** Keep or remove?

---

## InvoiceProduct / invoice_line Table

### SQLite (Current)
```sql
CREATE TABLE InvoiceProduct (
  InvoiceProductId INTEGER PRIMARY KEY,
  Priority INTEGER,                    -- For display order
  InvoiceId INTEGER NOT NULL,
  InventoryProductId INTEGER NOT NULL,
  Barcode TEXT,
  Description TEXT NOT NULL,
  Manufacturer TEXT,
  Brand TEXT,
  Category INTEGER,
  Quantity INTEGER NOT NULL DEFAULT 1,
  IsTrackable INTEGER DEFAULT 1,
  DateCreated TEXT DEFAULT CURRENT_TIMESTAMP,
  Note TEXT,
  UnitPrice NUMERIC NOT NULL DEFAULT 0,
  GroupPrice NUMERIC NOT NULL DEFAULT 0,
  IsGroupProduct INTEGER NOT NULL DEFAULT 0,
  OriginalUnitPrice NUMERIC NOT NULL DEFAULT 0
);
```

### PostgreSQL (Proposed - Simplified)
```sql
CREATE TABLE invoice_line (
  id UUID PRIMARY KEY,                   -- ✅ UUID as PK
  invoice_id UUID NOT NULL,              -- ✅ FK to invoice (UUID)
  product_id UUID NOT NULL,              -- ✅ FK to product (UUID)
  product_name VARCHAR(200) NOT NULL,
  quantity INT NOT NULL,
  unit_price NUMERIC(18,2) NOT NULL,
  line_total NUMERIC(18,2) NOT NULL,     -- ❓
  created_utc TIMESTAMPTZ NOT NULL
);
```

### What We Removed (Good!)
- ✅ Priority - Not needed for sync
- ✅ Barcode, Manufacturer, Brand, Category - Too detailed
- ✅ IsTrackable - All products tracked via movements
- ✅ Note - Rarely used
- ✅ GroupPrice, IsGroupProduct, OriginalUnitPrice - Handled via unit_price

### Questions

**Q5: Do we need `product_name` snapshot?**
- **Current:** Description is copied to each line
- **Alternative:** Just reference product and join when needed?
- **Trade-off:** Snapshot = denormalized but handles product name changes

**Your decision:** Keep snapshot or reference only?

---

**Q6: Do we need `line_total` field?**
- **Current:** Not explicitly stored (calculated on the fly)
- **Proposed:** Store calculated value
- **Reason:** Easier queries, historical accuracy
- **Trade-off:** Redundant (can calculate from unit_price * quantity)

**Your decision:** Store or calculate?

---

## Product / InventoryProduct Table

### SQLite (Current)
```sql
CREATE TABLE InventoryProduct (
  InventoryProductId INTEGER PRIMARY KEY,
  Barcode TEXT NOT NULL,
  Description TEXT NOT NULL,
  Manufacturer TEXT,
  Brand TEXT,
  Category INTEGER,
  QuantityInStock INTEGER NOT NULL DEFAULT 1,
  GroupPriceQuantity INTEGER,
  IsTrackable INTEGER DEFAULT 1,
  DateCreated TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  DateUpdated TEXT,
  UnitPrice NUMERIC NOT NULL DEFAULT 0,
  GroupPrice NUMERIC NOT NULL DEFAULT 0
);
```

### PostgreSQL (Proposed)
```sql
CREATE TABLE product (
  id UUID PRIMARY KEY,                   -- ✅ UUID as PK
  code VARCHAR(50) UNIQUE NOT NULL,      -- ❓
  barcode VARCHAR(50) NULL,
  name VARCHAR(200) NOT NULL,
  description TEXT NULL,                 -- ❓
  manufacturer TEXT NULL,                -- ❓
  brand TEXT NULL,                       -- ❓
  category VARCHAR(100) NULL,
  unit_price NUMERIC(18,2) NOT NULL,
  cost_price NUMERIC(18,2) NULL,         -- ❓
  is_active BOOLEAN NOT NULL DEFAULT TRUE, -- ❓
  created_utc TIMESTAMPTZ NOT NULL,
  last_modified_utc TIMESTAMPTZ NOT NULL
);

CREATE TABLE inventory_movement (        -- ❓ NEW CONCEPT
  id UUID PRIMARY KEY,
  store_id VARCHAR(50) NOT NULL,
  product_id UUID NOT NULL,              -- ✅ FK to product (UUID)
  quantity_delta INT NOT NULL,
  reason VARCHAR(50) NOT NULL,
  reference_id UUID NULL,
  note VARCHAR(500) NULL,
  created_utc TIMESTAMPTZ NOT NULL
);
```

### Questions

**Q7: Do we need separate `code` and `barcode`?**
- **Current:** Only `Barcode` field
- **Proposed:** `code` (internal SKU) + `barcode` (actual barcode)
- **Reason:** Some products have no barcode, need internal code

**Your decision:** Keep Barcode as is, or split into code + barcode?

---

**Q8: Do we need `description` (long text)?**
- **Current:** Only `Description` (used as product name)
- **Proposed:** `name` (short) + `description` (long text)

**Your decision:** Just use name (like current), or add long description?

---

**Q9: Keep Manufacturer and Brand?**
- **Current:** ✅ Has both
- **Proposed:** ✅ Keep both

Looks like you use these - should we keep them?

---

**Q10: Do we need `cost_price`?**
- **Current:** ❌ Not tracked
- **Proposed:** Add for profit margin tracking

**Your decision:** Add or skip?

---

**Q11: Do we need `is_active` soft delete?**
- **Current:** No soft delete - products stay forever
- **Proposed:** Mark products inactive instead of deleting

**Your decision:** Add soft delete or keep simple (no deletion)?

---

**Q12: Movement-based inventory - necessary?**
- **Current:** `QuantityInStock` field (snapshot)
- **Proposed:** `inventory_movement` table (event-based)

**This is critical for cloud sync** - we need historical record of stock changes.

**Your decision:** Accept movement-based approach?

---

## Payment Table

### SQLite (Current)
```sql
CREATE TABLE Payment (
  PaymentId INTEGER PRIMARY KEY,
  InvoiceId INTEGER NOT NULL,
  PaymentTypeId INTEGER NOT NULL DEFAULT 1,
  DateCreated TEXT DEFAULT CURRENT_TIMESTAMP,
  Note TEXT,
  Amount NUMERIC NOT NULL DEFAULT 0
);
```

### PostgreSQL (Proposed)
```sql
CREATE TABLE payment (
  id UUID PRIMARY KEY,                   -- ✅ UUID as PK
  invoice_id UUID NOT NULL,              -- ✅ FK to invoice (UUID)
  method VARCHAR(50) NOT NULL,           -- Changed from PaymentTypeId
  amount NUMERIC(18,2) NOT NULL,
  reference VARCHAR(100) NULL,           -- Renamed from Note
  created_utc TIMESTAMPTZ NOT NULL
);
```

### Questions

**Q13: Convert PaymentTypeId → method string?**
- **Current:** References PaymentType table (1=Cash, 2=Card, etc.)
- **Proposed:** Store method directly ("Cash", "Card", etc.)

**Trade-off:**
- Current: Normalized, need join
- Proposed: Denormalized, simpler queries

**Your decision:** Keep PaymentTypeId or convert to string?

---

**Q14: Rename Note → reference?**
- **Current:** `Note` (generic)
- **Proposed:** `reference` (clearer for bank reference, etc.)

**Your decision:** Keep "Note" or rename to "reference"?

---

## Summary of Questions

| # | Question | Current | Proposed | Decision |
|---|----------|---------|----------|----------|
| 1 | UUID as primary key | No | UUID | ✅ YES |
| 2 | invoice_number | No | VARCHAR | ❌ REMOVE |
| 3 | status field | No | VARCHAR | ❌ REMOVE |
| 4 | last_modified_utc | No | TIMESTAMPTZ | ✅ KEEP |
| 5 | product_name snapshot | Yes | Yes | ✅ KEEP |
| 6 | line_total stored | No | Yes | ❌ CALCULATE |
| 7 | code + barcode split | Barcode only | Both | ❌ BARCODE ONLY |
| 8 | long description | No | TEXT | ✅ ADD |
| 9 | manufacturer + brand | Yes | Yes | ✅ KEEP |
| 10 | cost_price | No | NUMERIC | ❌ REMOVE |
| 11 | is_active soft delete | No | BOOLEAN | ✅ ADD |
| 12 | Movement-based inventory | No | Yes | ✅ YES (needed for sync) |
| 13 | PaymentTypeId → method | FK to table | String | ✅ STRING |
| 14 | Note → reference | Note | note | ✅ KEEP AS note |

---

## My Recommendations (Based on Minimal Principle)

### ✅ MUST ADD (Necessary for sync):
- ✅ UUID as `id` (primary key) - Distributed identity
- ✅ `store_id` - Multi-store support
- ✅ Movement-based inventory - Historical tracking for sync

### ✅ BETTER TYPE (Clear improvement):
- ✅ TEXT → TIMESTAMPTZ for dates
- ✅ INTEGER → UUID for PKs (microservices-ready)
- ✅ Explicit NUMERIC(18,2) precision

### ✅ KEEP (From SQLite):
- ✅ `last_modified_utc` - Might update invoices later
- ✅ `product_name` snapshot - Handle product name changes
- ✅ `description` (long text) - Needed for products
- ✅ `is_active` - Soft delete for products
- ✅ `manufacturer` + `brand` - Already used
- ✅ Keep as `note` (not `reference`)

### ❌ REMOVE (Not needed):
- ❌ `invoice_number` - Use id
- ❌ `status` - All invoices completed
- ❌ `line_total` - Calculate on the fly
- ❌ `code` separate from `barcode` - Barcode is enough
- ❌ `cost_price` - Not tracked

### ✅ CONVERT (Simplification):
- ✅ PaymentTypeId → method string (simpler, no join)

---

## ✅ Final Decisions Applied

All decisions have been made by Pond (Lead Engineer).

**See:** `SCHEMA-FINAL-DECISIONS.md` for the complete final schema.

**Status:** APPROVED - Ready for implementation in Epic D
