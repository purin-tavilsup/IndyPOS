# Final Minimal Schema (PostgreSQL)

**Date:** 2026-03-03
**Principle:** "Maintain schema from SQLite unless necessary or better" - Pond
**Decision:** UUID as primary key (microservices-ready)

---

## Guiding Principles

1. **Match SQLite closely** - Don't add unnecessary fields
2. **UUID primary keys** - Distributed ID generation (no integer IDs)
3. **Add only what's necessary** - For sync and multi-store
4. **Better types** - TIMESTAMPTZ instead of TEXT, proper precision

---

## StoreHub Database Schema

### invoice

```sql
CREATE TABLE invoice (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  store_id VARCHAR(50) NOT NULL,
  invoice_number VARCHAR(50) NOT NULL,
  user_id BIGINT NOT NULL,
  total_amount NUMERIC(18,2) NOT NULL,
  status VARCHAR(20) NOT NULL DEFAULT 'Completed',
  created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_invoice_store_created ON invoice(store_id, created_utc);
CREATE INDEX idx_invoice_number ON invoice(invoice_number);
```

**What we kept from SQLite:**
- ✅ UserId → user_id
- ✅ Total → total_amount
- ✅ DateCreated → created_utc (better type)

**What we added (necessary):**
- ✅ id (UUID PK) - distributed ID
- ✅ store_id - multi-store support
- ✅ invoice_number - human-readable ID
- ✅ status - track invoice state

**What we removed (not needed):**
- ❌ customer_id - never used
- ❌ subtotal, discount, tax - not tracked
- ❌ last_modified_utc - invoices are immutable

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
  line_total NUMERIC(18,2) NOT NULL,
  created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_invoice_line_invoice ON invoice_line(invoice_id);
CREATE INDEX idx_invoice_line_product ON invoice_line(product_id);
```

**What we kept from SQLite (simplified):**
- ✅ InvoiceId → invoice_id (UUID FK)
- ✅ InventoryProductId → product_id (UUID FK)
- ✅ Description → product_name (snapshot)
- ✅ Quantity → quantity
- ✅ UnitPrice → unit_price
- ✅ DateCreated → created_utc

**What we added (necessary):**
- ✅ id (UUID PK)
- ✅ line_total - stored calculation

**What we removed (not needed):**
- ❌ Priority - display order not critical
- ❌ Barcode, Manufacturer, Brand, Category - too detailed
- ❌ Note - rarely used
- ❌ IsTrackable - all products tracked
- ❌ GroupPrice, IsGroupProduct, OriginalUnitPrice - handled via unit_price

**Group Pricing Strategy:**
- Just set `unit_price` to the actual price paid (group or regular)
- No special flags needed

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

CREATE INDEX idx_payment_invoice ON payment(invoice_id);
```

**What we kept from SQLite:**
- ✅ InvoiceId → invoice_id (UUID FK)
- ✅ Amount → amount
- ✅ Note → note
- ✅ DateCreated → created_utc

**What we changed (better):**
- ✅ PaymentTypeId → method (string) - simpler, no join needed

**What we added:**
- ✅ id (UUID PK)

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

CREATE INDEX idx_pay_later_invoice ON pay_later(invoice_id);
CREATE INDEX idx_pay_later_payment ON pay_later(payment_id);
CREATE INDEX idx_pay_later_incomplete ON pay_later(is_completed) WHERE is_completed = FALSE;
```

**What we kept from SQLite:**
- ✅ PaymentId → payment_id (UUID FK)
- ✅ InvoiceId → invoice_id (UUID FK)
- ✅ Description → description (customer name)
- ✅ PayLaterAmount → pay_later_amount
- ✅ PaidAmount → paid_amount
- ✅ IsCompleted → is_completed
- ✅ DateCreated → created_utc (better type)
- ✅ DateUpdated → last_modified_utc (better type)

**What we added:**
- ✅ id (UUID PK)

**Why this is essential:**
- Track credit/trust purchases for low-income customers
- Customer identified by description (no formal customer entity)
- Supports partial payments (paid_amount < pay_later_amount)
- Has dedicated UI for searching and managing
- Current data: 5,181 records (144 still incomplete)

---

### product

```sql
CREATE TABLE product (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  barcode VARCHAR(50) UNIQUE NOT NULL,
  name VARCHAR(200) NOT NULL,
  manufacturer VARCHAR(200) NULL,
  brand VARCHAR(200) NULL,
  category VARCHAR(100) NULL,
  unit_price NUMERIC(18,2) NOT NULL,
  group_price NUMERIC(18,2) NULL,
  group_price_qty INT NULL,
  created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  last_modified_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_product_barcode ON product(barcode);
CREATE INDEX idx_product_category ON product(category);
```

**What we kept from SQLite:**
- ✅ Barcode → barcode
- ✅ Description → name
- ✅ Manufacturer → manufacturer
- ✅ Brand → brand
- ✅ Category → category (VARCHAR instead of INT)
- ✅ UnitPrice → unit_price
- ✅ GroupPrice → group_price
- ✅ GroupPriceQuantity → group_price_qty
- ✅ DateCreated → created_utc
- ✅ DateUpdated → last_modified_utc

**What we added:**
- ✅ id (UUID PK)

**What we removed (not currently used):**
- ❌ QuantityInStock - replaced by inventory_movement table
- ❌ IsTrackable - all products tracked
- ❌ separate 'code' field - barcode is sufficient
- ❌ long 'description' field - name is enough
- ❌ cost_price - not tracked
- ❌ is_active soft delete - not needed

---

### inventory_movement (NEW - Required for sync)

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

CREATE INDEX idx_movement_store_product ON inventory_movement(store_id, product_id, created_utc);
CREATE INDEX idx_movement_reference ON inventory_movement(reference_id);
```

**Why this is necessary:**
- **Current:** QuantityInStock (snapshot) - can't track history
- **New:** Movement-based - tracks every change
- **Benefit:** Cloud can reconstruct stock history for any point in time

**Reasons:**
- Sale, Restock, Adjustment, TransferIn, TransferOut, Loss, Return

**Current stock calculation:**
```sql
SELECT SUM(quantity_delta)
FROM inventory_movement
WHERE product_id = @productId
  AND store_id = @storeId;
```

---

### outbox_event (NEW - Required for sync)

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

CREATE INDEX idx_outbox_polling ON outbox_event(status, next_retry_utc, created_utc);
```

**Why this is necessary:**
- Reliable event delivery to cloud
- Retry with exponential backoff
- Survives network failures

---

## Cloud Database Schema

Same as StoreHub, plus:

### synced_event (Deduplication)

```sql
CREATE TABLE synced_event (
  event_id UUID PRIMARY KEY,
  store_id VARCHAR(50) NOT NULL,
  type VARCHAR(100) NOT NULL,
  received_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_synced_event_store ON synced_event(store_id, received_utc);
```

**Purpose:** Idempotency - prevent duplicate event processing

---

## Summary: What Changed from SQLite

### Added (Necessary):
- ✅ **UUID PKs** - All tables use `id` (microservices-ready)
- ✅ **store_id** - Multi-store support
- ✅ **invoice_number** - Human-readable invoice ID
- ✅ **status** - Track invoice state
- ✅ **pay_later** - Track credit purchases (kept from SQLite)
- ✅ **inventory_movement** - Movement-based stock (replaces QuantityInStock)
- ✅ **outbox_event** - Reliable sync mechanism

### Changed (Better):
- ✅ **TEXT → TIMESTAMPTZ** - Proper date/time with timezone
- ✅ **INTEGER → UUID** - Distributed primary keys
- ✅ **PaymentTypeId → method** - Simplified (no join needed)
- ✅ **Category INT → VARCHAR** - Direct string (no lookup table)

### Kept (Unchanged):
- ✅ **User IDs** - Still use BIGINT (centrally managed)
- ✅ **Manufacturer, Brand** - Preserved from SQLite
- ✅ **Group pricing** - Preserved (group_price, group_price_qty)
- ✅ **Product fields** - All kept (barcode, name, etc.)
- ✅ **Total amount** - Tax-inclusive (Thai market)

### Removed (Not needed):
- ❌ **customer_id** - Never used
- ❌ **subtotal, discount, tax** - Not tracked
- ❌ **last_modified_utc** on invoice - Immutable
- ❌ **Priority** on invoice_line - Not critical
- ❌ **Barcode, Manufacturer, Brand** on invoice_line - Too detailed
- ❌ **IsTrackable** - All products tracked
- ❌ **Note** on invoice_line - Rarely used
- ❌ **cost_price** - Not tracked
- ❌ **is_active** soft delete - Not needed
- ❌ **separate code field** - Barcode is sufficient

---

## Migration Examples

### Create Invoice (Application Code)

```csharp
var invoice = new Invoice
{
    Id = Guid.NewGuid(),                 // Generated
    StoreId = "STORE-001",               // Config
    InvoiceNumber = GenerateInvoiceNumber(),  // Sequential
    UserId = currentUser.Id,
    TotalAmount = 1250.00m,
    Status = "Completed",
    CreatedUtc = DateTime.UtcNow
};

var lines = new List<InvoiceLine>
{
    new InvoiceLine
    {
        Id = Guid.NewGuid(),
        InvoiceId = invoice.Id,          // UUID FK
        ProductId = product.Id,          // UUID FK
        ProductName = product.Name,      // Snapshot
        Quantity = 2,
        UnitPrice = 500.00m,
        LineTotal = 1000.00m
    }
};

var payments = new List<Payment>
{
    new Payment
    {
        Id = Guid.NewGuid(),
        InvoiceId = invoice.Id,          // UUID FK
        Method = "Cash",
        Amount = 1250.00m
    }
};

// Save all in transaction
await _db.SaveInvoiceAsync(invoice, lines, payments);
```

### Query Current Stock

```csharp
public async Task<int> GetCurrentStockAsync(Guid productId, string storeId)
{
    var stock = await _db.InventoryMovements
        .Where(m => m.ProductId == productId && m.StoreId == storeId)
        .SumAsync(m => m.QuantityDelta);

    return stock;
}
```

---

## Benefits of This Approach

### ✅ Minimal Changes
- Matches SQLite schema closely
- Only adds what's necessary for sync
- Preserves group pricing, manufacturer, brand

### ✅ Microservices-Ready
- UUID primary keys (distributed ID generation)
- No central coordination needed
- Each store is independent

### ✅ Offline-First
- Movement-based inventory (historical record)
- Outbox pattern (reliable sync)
- Works offline indefinitely

### ✅ Clean Migration
- SQLite → PostgreSQL mapping is straightforward
- Minimal field transformations
- Clear upgrade path

---

## Next Steps

1. **Epic D:** Implement this schema with EF Core
2. **Epic H:** Write migration tool (SQLite → PostgreSQL)
3. **Test:** Validate with production backup

---

**Approved by:** Pond (Lead Engineer)
**Principle:** "Maintain schema from SQLite unless necessary or better"
**Result:** Minimal, clean, microservices-ready schema ✨
