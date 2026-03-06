# Schema Final Decisions

**Date:** 2026-03-03
**Decision Maker:** Pond (Lead Engineer)
**Principle:** Absolute minimum - only what's needed

---

## Field-by-Field Decisions

| Field | Decision | Reason |
|-------|----------|--------|
| **invoice_number** | ❌ REMOVE | We have public_id (UUID), don't need another ID |
| **status** | ❌ REMOVE | All invoices are committed and completed |
| **last_modified_utc** | ✅ KEEP | Might update invoices later |
| **line_total** | ❌ REMOVE | Calculate on the fly (unit_price * quantity) |
| **barcode** | ✅ KEEP | Enough as product identifier |
| **description** (long) | ✅ KEEP | Needed for products |
| **cost_price** | ❌ REMOVE | Not needed |
| **is_active** | ✅ ADD | Will come in handy later (soft delete) |

---

## Final Minimal Schema

### invoice
```sql
CREATE TABLE invoice (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  store_id VARCHAR(50) NOT NULL,
  user_id BIGINT NOT NULL,
  total_amount NUMERIC(18,2) NOT NULL,
  created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  last_modified_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

**Fields:**
- id - UUID PK (clean, conventional)
- store_id - multi-store
- user_id - who created it
- total_amount - tax-inclusive total
- created_utc - when created
- last_modified_utc - when last modified

**Removed:**
- ❌ invoice_number (use public_id)
- ❌ status (all completed)

---

### invoice_line
```sql
CREATE TABLE invoice_line (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  invoice_id UUID NOT NULL REFERENCES invoice(id) ON DELETE CASCADE,
  product_id UUID NOT NULL REFERENCES product(id),
  product_name VARCHAR(200) NOT NULL,
  quantity INT NOT NULL,
  unit_price NUMERIC(18,2) NOT NULL,
  created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

**Fields:**
- id - UUID PK
- invoice_id - FK to invoice (UUID)
- product_id - FK to product (UUID)
- product_name - snapshot of product name
- quantity - how many
- unit_price - price per unit (group or regular)
- created_utc - when created

**Removed:**
- ❌ line_total (calculate: unit_price * quantity)

**Calculation on-the-fly:**
```csharp
decimal lineTotal = line.UnitPrice * line.Quantity;
```

---

### payment
```sql
CREATE TABLE payment (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  invoice_id UUID NOT NULL REFERENCES invoice(id) ON DELETE CASCADE,
  method VARCHAR(50) NOT NULL,
  amount NUMERIC(18,2) NOT NULL,
  note TEXT NULL,
  created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

**No changes from previous** - already minimal

---

### product
```sql
CREATE TABLE product (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  barcode VARCHAR(50) UNIQUE NOT NULL,
  name VARCHAR(200) NOT NULL,
  description TEXT NULL,
  manufacturer VARCHAR(200) NULL,
  brand VARCHAR(200) NULL,
  category VARCHAR(100) NULL,
  unit_price NUMERIC(18,2) NOT NULL,
  group_price NUMERIC(18,2) NULL,
  group_price_qty INT NULL,
  is_active BOOLEAN NOT NULL DEFAULT TRUE,
  created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  last_modified_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

**Fields:**
- id - UUID PK
- barcode - product identifier (unique)
- name - short name
- description - long description (KEEP per Pond)
- manufacturer - from SQLite
- brand - from SQLite
- category - from SQLite
- unit_price - regular price
- group_price - bulk price
- group_price_qty - quantity for bulk price
- is_active - soft delete flag (KEEP per Pond)
- created_utc - when created
- last_modified_utc - when modified

**Removed:**
- ❌ code (separate from barcode) - barcode is enough
- ❌ cost_price - not needed

**Added:**
- ✅ description (long text) - needed
- ✅ is_active - useful for future soft delete

---

### pay_later
```sql
CREATE TABLE pay_later (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  payment_id UUID NOT NULL REFERENCES payment(id),
  invoice_id UUID NOT NULL REFERENCES invoice(id),
  description VARCHAR(500) NOT NULL,
  pay_later_amount NUMERIC(18,2) NOT NULL,
  paid_amount NUMERIC(18,2) NOT NULL DEFAULT 0,
  is_completed BOOLEAN NOT NULL DEFAULT FALSE,
  created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  last_modified_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

**Fields:**
- id - UUID PK
- payment_id - FK to initial payment record
- invoice_id - FK to invoice
- description - Customer name/identifier (no formal customer entity)
- pay_later_amount - Total amount owed
- paid_amount - Amount paid so far (can be partial)
- is_completed - Fully paid flag
- created_utc - when created
- last_modified_utc - when payment updated

**Why this is kept:**
- Track credit/trust purchases for low-income customers
- Search by customer name (description field)
- Track partial payments until fully paid
- Existing feature with dedicated UI (5,181 records, 144 incomplete)

**Outstanding balance calculation:**
```csharp
decimal outstanding = payLater.PayLaterAmount - payLater.PaidAmount;
```

---

### inventory_movement
```sql
CREATE TABLE inventory_movement (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  store_id VARCHAR(50) NOT NULL,
  product_id UUID NOT NULL REFERENCES product(id),
  quantity_delta INT NOT NULL,
  reason VARCHAR(50) NOT NULL,
  reference_id UUID NULL,
  note VARCHAR(500) NULL,
  created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

**No changes** - necessary for sync

---

### outbox_event
```sql
CREATE TABLE outbox_event (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  store_id VARCHAR(50) NOT NULL,
  type VARCHAR(100) NOT NULL,
  payload_json TEXT NOT NULL,
  created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  attempts INT NOT NULL DEFAULT 0,
  last_attempt_utc TIMESTAMPTZ NULL,
  next_retry_utc TIMESTAMPTZ NULL,
  status VARCHAR(20) NOT NULL DEFAULT 'Pending'
);
```

**No changes** - necessary for sync

---

## Summary of Changes from Previous Version

### Removed (Even More Minimal):
- ❌ **invoice_number** - Use id instead
- ❌ **status** - All invoices are completed
- ❌ **line_total** - Calculate on the fly

### Added (Necessary):
- ✅ **description** on product - Long description field
- ✅ **is_active** on product - Soft delete flag
- ✅ **pay_later** table - Track credit purchases (existing feature)

### Kept (Final):
- ✅ **last_modified_utc** on invoice - For updates
- ✅ **barcode** only (no separate code) - Sufficient
- ✅ All SQLite fields (manufacturer, brand, group pricing)
- ✅ PayLater feature - Essential for low-income customers

---

## Application Layer Calculations

### Line Total (Calculate on Read)
```csharp
public class InvoiceLine
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime CreatedUtc { get; set; }

    // Calculated property (not stored)
    public decimal LineTotal => UnitPrice * Quantity;
}
```

### Invoice Total (Calculate from Lines)
```csharp
public class Invoice
{
    public Guid Id { get; set; }
    public string StoreId { get; set; }
    public int UserId { get; set; }
    public decimal TotalAmount { get; set; }  // Store this (summary)
    public DateTime CreatedUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }

    public List<InvoiceLine> Lines { get; set; }

    // Verify total matches lines
    public bool ValidateTotal() => TotalAmount == Lines.Sum(l => l.LineTotal);
}
```

---

## Display Logic

### Show Invoice ID to User
```csharp
// Don't need invoice_number, just format the UUID
public string GetInvoiceDisplayId(Guid id)
{
    // Option 1: Short version
    return id.ToString().Substring(0, 8).ToUpper();
    // Result: "A1B2C3D4"

    // Option 2: Full UUID
    return publicId.ToString().ToUpper();
    // Result: "A1B2C3D4-E5F6-G7H8-I9J0-K1L2M3N4O5P6"

    // Option 3: With prefix
    return $"INV-{publicId.ToString().Substring(0, 8).ToUpper()}";
    // Result: "INV-A1B2C3D4"
}
```

---

## Migration from SQLite

### SQLite (Current)
```
Invoice:
  InvoiceId = 79
  UserId = 1
  DateCreated = "2022-01-05 13:31:31"
  Total = 120
```

### PostgreSQL (New)
```sql
INSERT INTO invoice (public_id, store_id, user_id, total_amount, created_utc, last_modified_utc)
VALUES (
  gen_random_uuid(),                      -- Generate new UUID
  'STORE-001',                            -- From config
  1,                                      -- From SQLite
  120.00,                                 -- From SQLite
  '2022-01-05 13:31:31'::timestamptz,     -- From SQLite (parse)
  '2022-01-05 13:31:31'::timestamptz      -- Same as created
);
```

**Note:** No invoice_number to migrate - just use public_id!

---

## Benefits of This Ultra-Minimal Approach

### ✅ Simpler Schema
- Fewer fields to maintain
- Less storage
- Faster queries

### ✅ Calculate on Read (line_total)
- No risk of stored value being wrong
- Always accurate
- Simpler insert logic

### ✅ Use UUID Everywhere (no invoice_number)
- Consistent identifier
- One less field to generate
- Public_id is globally unique anyway

### ✅ No Status Field
- Simpler logic
- All invoices are completed
- Can add later if needed

### ✅ Soft Delete Ready (is_active)
- Products can be deactivated
- Don't lose historical data
- Can reactivate if needed

---

## Trade-offs Accepted

| Decision | Trade-off | Accepted Because |
|----------|-----------|------------------|
| No invoice_number | UUID not "friendly" for humans | Can format UUID for display |
| No status | Can't track pending/void | All invoices are completed on save |
| Calculate line_total | Repeated calculation | Small cost, always accurate |
| Keep last_modified_utc | Invoices rarely updated | Might need it later |

---

## Final Schema Size Comparison

### SQLite (Current)
- Invoice: 4 fields
- InvoiceProduct: 17 fields
- Payment: 6 fields
- InventoryProduct: 13 fields
- PayLater: 8 fields

### PostgreSQL (Final)
- invoice: 6 fields ✅
- invoice_line: 7 fields ✅ (down from 17!)
- payment: 6 fields ✅
- product: 12 fields ✅
- pay_later: 9 fields ✅ (kept from SQLite)
- inventory_movement: 8 fields (NEW - for sync)
- outbox_event: 9 fields (NEW - for sync)

**Result:** Cleaner, simpler, only what's needed! ✨

---

**Approved by:** Pond (Lead Engineer)
**Status:** FINAL - ready for implementation
**Next:** Epic D (implement with EF Core)
