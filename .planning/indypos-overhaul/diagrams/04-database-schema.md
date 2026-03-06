# Database Schema Overview

Version: 1.0.0
Date: 2026-03-03

## StoreHub Database (Local PostgreSQL)

Database: `indypos_storehub`

### Core Business Tables

```
┌─────────────────────────────────────────────────────────────────┐
│  invoice                                                        │
├──────────────────┬──────────────────┬──────────────────────────┤
│ id               │ UUID             │ PK                       │
│ store_id         │ VARCHAR(50)      │ NOT NULL                 │
│ user_id          │ BIGINT           │ NOT NULL                 │
│ total_amount     │ NUMERIC(18,2)    │ NOT NULL                 │
│ created_utc      │ TIMESTAMPTZ      │ NOT NULL                 │
│ last_modified_utc│ TIMESTAMPTZ      │ NOT NULL                 │
└──────────────────┴──────────────────┴──────────────────────────┘

Notes:
- id is UUID PRIMARY KEY (microservices-ready)
- No invoice_number (use id for display)
- No status (all invoices are completed)
- total_amount is tax-inclusive (Thai market)
                          │
                          │ 1:N
                          ▼
┌─────────────────────────────────────────────────────────────────┐
│  invoice_line                                                   │
├──────────────────┬──────────────────┬──────────────────────────┤
│ id               │ UUID             │ PK                       │
│ invoice_id       │ UUID             │ NOT NULL (FK → invoice)  │
│ product_id       │ UUID             │ NOT NULL (FK → product)  │
│ product_name     │ VARCHAR(200)     │ NOT NULL (snapshot)      │
│ quantity         │ INT              │ NOT NULL                 │
│ unit_price       │ NUMERIC(18,2)    │ NOT NULL                 │
│ created_utc      │ TIMESTAMPTZ      │ NOT NULL                 │
└──────────────────┴──────────────────┴──────────────────────────┘

Notes:
- No line_total (calculate: unit_price * quantity)
- Group pricing handled via unit_price
- All IDs are UUID (cleaner, conventional naming)

┌─────────────────────────────────────────────────────────────────┐
│  payment                                                        │
├──────────────────┬──────────────────┬──────────────────────────┤
│ id               │ UUID             │ PK                       │
│ invoice_id       │ UUID             │ NOT NULL (FK → invoice)  │
│ method           │ VARCHAR(50)      │ NOT NULL                 │
│ amount           │ NUMERIC(18,2)    │ NOT NULL                 │
│ note             │ TEXT             │ NULL                     │
│ created_utc      │ TIMESTAMPTZ      │ NOT NULL                 │
└──────────────────┴──────────────────┴──────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│  pay_later                                                      │
├──────────────────┬──────────────────┬──────────────────────────┤
│ id               │ UUID             │ PK                       │
│ payment_id       │ UUID             │ NOT NULL (FK → payment)  │
│ invoice_id       │ UUID             │ NOT NULL (FK → invoice)  │
│ description      │ VARCHAR(500)     │ NOT NULL                 │
│ pay_later_amount │ NUMERIC(18,2)    │ NOT NULL                 │
│ paid_amount      │ NUMERIC(18,2)    │ NOT NULL DEFAULT 0       │
│ is_completed     │ BOOLEAN          │ NOT NULL DEFAULT FALSE   │
│ created_utc      │ TIMESTAMPTZ      │ NOT NULL                 │
│ last_modified_utc│ TIMESTAMPTZ      │ NOT NULL                 │
└──────────────────┴──────────────────┴──────────────────────────┘

Notes:
- Tracks credit/trust purchases for low-income customers
- description contains customer name (no formal customer entity)
- Supports partial payments (paid_amount < pay_later_amount)
- Dedicated UI for searching and updating

┌─────────────────────────────────────────────────────────────────┐
│  product                                                        │
├──────────────────┬──────────────────┬──────────────────────────┤
│ id               │ UUID             │ PK                       │
│ barcode          │ VARCHAR(50)      │ UNIQUE NOT NULL          │
│ name             │ VARCHAR(200)     │ NOT NULL                 │
│ description      │ TEXT             │ NULL                     │
│ manufacturer     │ VARCHAR(200)     │ NULL                     │
│ brand            │ VARCHAR(200)     │ NULL                     │
│ category         │ VARCHAR(100)     │ NULL                     │
│ unit_price       │ NUMERIC(18,2)    │ NOT NULL                 │
│ group_price      │ NUMERIC(18,2)    │ NULL                     │
│ group_price_qty  │ INT              │ NULL                     │
│ is_active        │ BOOLEAN          │ NOT NULL DEFAULT TRUE    │
│ created_utc      │ TIMESTAMPTZ      │ NOT NULL                 │
│ last_modified_utc│ TIMESTAMPTZ      │ NOT NULL                 │
└──────────────────┴──────────────────┴──────────────────────────┘

Notes:
- barcode is sufficient (no separate 'code' field)
- description added (long text for product details)
- is_active added (soft delete - useful for future)
- No cost_price (not tracked)
- Group pricing preserved from SQLite

┌─────────────────────────────────────────────────────────────────┐
│  inventory_movement                                             │
├──────────────────┬──────────────────┬──────────────────────────┤
│ id               │ UUID             │ PK                       │
│ store_id         │ VARCHAR(50)      │ NOT NULL                 │
│ product_id       │ UUID             │ NOT NULL (FK → product)  │
│ quantity_delta   │ INT              │ NOT NULL (can be -)      │
│ reason           │ VARCHAR(50)      │ NOT NULL *               │
│ reference_id     │ UUID             │ NULL                     │
│ note             │ VARCHAR(500)     │ NULL                     │
│ created_utc      │ TIMESTAMPTZ      │ NOT NULL                 │
└──────────────────┴──────────────────┴──────────────────────────┘

* reason: Sale, Restock, Adjustment, TransferIn, TransferOut, Loss, Return
* NEW TABLE - movement-based stock tracking (required for cloud sync)

```

---

### Sync Infrastructure Tables

```
┌─────────────────────────────────────────────────────────────────┐
│  outbox_event                                                   │
├──────────────────┬──────────────────┬──────────────────────────┤
│ id               │ UUID             │ PK                       │
│ store_id         │ VARCHAR(50)      │ NOT NULL                 │
│ type             │ VARCHAR(100)     │ NOT NULL *               │
│ payload_json     │ TEXT             │ NOT NULL                 │
│ created_utc      │ TIMESTAMPTZ      │ NOT NULL                 │
│ attempts         │ INT              │ NOT NULL DEFAULT 0       │
│ last_attempt_utc │ TIMESTAMPTZ      │ NULL                     │
│ next_retry_utc   │ TIMESTAMPTZ      │ NULL                     │
│ status           │ VARCHAR(20)      │ NOT NULL **              │
└──────────────────┴──────────────────┴──────────────────────────┘

* type: InvoiceCompleted, InventoryMovementRecorded, ProductUpdated
** status: Pending, Sent, Failed

Index: (status, next_retry_utc) for efficient polling
Note: NEW TABLE - Outbox pattern for reliable sync

┌─────────────────────────────────────────────────────────────────┐
│  synced_master_data                                             │
├──────────────────┬──────────────────┬──────────────────────────┤
│ id               │ BIGSERIAL        │ PK                       │
│ type             │ VARCHAR(50)      │ NOT NULL *               │
│ last_sync_utc    │ TIMESTAMPTZ      │ NOT NULL                 │
└──────────────────┴──────────────────┴──────────────────────────┘

* type: Product, PriceList, StoreConfig

Unique: (type)
```

---

## Cloud Database (Central PostgreSQL)

Database: `indypos_cloud`

### Deduplication & Sync Tables

```
┌─────────────────────────────────────────────────────────────────┐
│  synced_event                                                   │
├──────────────────┬──────────────────┬──────────────────────────┤
│ event_public_id  │ UUID             │ PK                       │
│ store_id         │ VARCHAR(50)      │ NOT NULL                 │
│ type             │ VARCHAR(100)     │ NOT NULL                 │
│ received_utc     │ TIMESTAMPTZ      │ NOT NULL                 │
└──────────────────┴──────────────────┴──────────────────────────┘

Purpose: Prevent duplicate event processing (idempotency)
```

---

### Transactional Data (All Stores)

```
┌─────────────────────────────────────────────────────────────────┐
│  invoice (Cloud)                                                │
├──────────────────┬──────────────────┬──────────────────────────┤
│ public_id        │ UUID             │ PK                       │
│ store_id         │ VARCHAR(50)      │ NOT NULL                 │
│ invoice_number   │ VARCHAR(50)      │ NOT NULL                 │
│ user_id          │ BIGINT           │ NOT NULL                 │
│ total_amount     │ NUMERIC(18,2)    │ NOT NULL                 │
│ status           │ VARCHAR(20)      │ NOT NULL                 │
│ created_utc      │ TIMESTAMPTZ      │ NOT NULL                 │
│ received_utc     │ TIMESTAMPTZ      │ NOT NULL (cloud time)    │
└──────────────────┴──────────────────┴──────────────────────────┘

Notes:
- Simplified schema (no subtotal, discount, tax)
- total_amount is final payment (tax-inclusive)

Unique: (store_id, public_id)
Index: (store_id, created_utc) for reports

┌─────────────────────────────────────────────────────────────────┐
│  invoice_line (Cloud)                                           │
├──────────────────┬──────────────────┬──────────────────────────┤
│ public_id        │ UUID             │ PK                       │
│ invoice_public_id│ UUID             │ NOT NULL                 │
│ product_public_id│ UUID             │ NOT NULL                 │
│ product_name     │ VARCHAR(200)     │ NOT NULL                 │
│ quantity         │ INT              │ NOT NULL                 │
│ unit_price       │ NUMERIC(18,2)    │ NOT NULL                 │
│ line_total       │ NUMERIC(18,2)    │ NOT NULL                 │
└──────────────────┴──────────────────┴──────────────────────────┘

Note: Simplified (no discount field)

┌─────────────────────────────────────────────────────────────────┐
│  payment                                                        │
├──────────────────┬──────────────────┬──────────────────────────┤
│ public_id        │ UUID             │ PK                       │
│ invoice_public_id│ UUID             │ NOT NULL                 │
│ method           │ VARCHAR(50)      │ NOT NULL                 │
│ amount           │ NUMERIC(18,2)    │ NOT NULL                 │
│ reference        │ VARCHAR(100)     │ NULL                     │
│ created_utc      │ TIMESTAMPTZ      │ NOT NULL                 │
└──────────────────┴──────────────────┴──────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│  inventory_movement                                             │
├──────────────────┬──────────────────┬──────────────────────────┤
│ public_id        │ UUID             │ PK                       │
│ store_id         │ VARCHAR(50)      │ NOT NULL                 │
│ product_public_id│ UUID             │ NOT NULL                 │
│ quantity_delta   │ INT              │ NOT NULL                 │
│ reason           │ VARCHAR(50)      │ NOT NULL                 │
│ reference_public_id│ UUID           │ NULL                     │
│ note             │ VARCHAR(500)     │ NULL                     │
│ created_utc      │ TIMESTAMPTZ      │ NOT NULL                 │
└──────────────────┴──────────────────┴──────────────────────────┘

Index: (store_id, product_public_id, created_utc) for stock queries
```

---

### Master Data Tables (Cloud)

```
┌─────────────────────────────────────────────────────────────────┐
│  product (MASTER - Source of Truth)                             │
├──────────────────┬──────────────────┬──────────────────────────┤
│ public_id        │ UUID             │ PK                       │
│ code             │ VARCHAR(50)      │ UNIQUE NOT NULL          │
│ barcode          │ VARCHAR(50)      │ NULL                     │
│ name             │ VARCHAR(200)     │ NOT NULL                 │
│ description      │ TEXT             │ NULL                     │
│ category         │ VARCHAR(100)     │ NULL                     │
│ unit_price       │ NUMERIC(18,2)    │ NOT NULL                 │
│ cost_price       │ NUMERIC(18,2)    │ NULL                     │
│ is_active        │ BOOLEAN          │ NOT NULL DEFAULT TRUE    │
│ created_utc      │ TIMESTAMPTZ      │ NOT NULL                 │
│ last_modified_utc│ TIMESTAMPTZ      │ NOT NULL                 │
└──────────────────┴──────────────────┴──────────────────────────┘

Note: Stores pull this periodically

┌─────────────────────────────────────────────────────────────────┐
│  store                                                          │
├──────────────────┬──────────────────┬──────────────────────────┤
│ store_id         │ VARCHAR(50)      │ PK                       │
│ name             │ VARCHAR(200)     │ NOT NULL                 │
│ location         │ VARCHAR(200)     │ NULL                     │
│ api_key_hash     │ VARCHAR(255)     │ NOT NULL                 │
│ is_active        │ BOOLEAN          │ NOT NULL DEFAULT TRUE    │
│ created_utc      │ TIMESTAMPTZ      │ NOT NULL                 │
│ last_sync_utc    │ TIMESTAMPTZ      │ NULL                     │
└──────────────────┴──────────────────┴──────────────────────────┘
```

---

## Entity Relationships (StoreHub)

```
            invoice (UUID PK)
               │
    ┌──────────┼──────────┬──────────┐
    │          │          │          │
    │ 1:N      │ 1:N      │ 1:N      │ 1:N (via reference_id)
    │          │          │          │
    ▼          ▼          ▼          ▼
invoice_line  payment  pay_later  inventory_movement
    │            │          │          │
    │ N:1        └──────────┘          │ N:1
    │                1:1                │
    └──────────┬───────────────────────┘
               │
               ▼
          product (UUID PK)

Notes:
- All PKs and FKs use UUID
- User relationship (user_id INT FK) not shown for clarity
- pay_later has 1:1 relationship with payment (initial credit payment)
- invoice_line → product via product_id
- inventory_movement → product via product_id
- inventory_movement → invoice via reference_id (for sale movements)
```

---

## Query Patterns

### Get Current Stock (StoreHub)
```sql
SELECT SUM(quantity_delta) as stock
FROM inventory_movement
WHERE product_public_id = @productPublicId
  AND store_id = @storeId;
```

### Get Pending Outbox Events (StoreHub)
```sql
SELECT *
FROM outbox_event
WHERE status = 'Pending'
  AND (next_retry_utc IS NULL OR next_retry_utc <= NOW())
ORDER BY created_utc
LIMIT 20;
```

### Check Event Idempotency (Cloud)
```sql
SELECT 1
FROM synced_event
WHERE event_public_id = @eventPublicId;
```

### Daily Sales Report (Cloud)
```sql
SELECT
  i.store_id,
  DATE(i.created_utc AT TIME ZONE 'Asia/Bangkok') as sale_date,
  COUNT(*) as invoice_count,
  SUM(i.total_amount) as total_sales
FROM invoice i
WHERE i.created_utc >= @startDate
  AND i.created_utc < @endDate
GROUP BY i.store_id, sale_date
ORDER BY sale_date DESC, i.store_id;
```

### Stock by Store (Cloud)
```sql
SELECT
  im.store_id,
  p.name as product_name,
  SUM(im.quantity_delta) as current_stock
FROM inventory_movement im
JOIN product p ON p.public_id = im.product_public_id
GROUP BY im.store_id, p.public_id, p.name
HAVING SUM(im.quantity_delta) != 0
ORDER BY im.store_id, p.name;
```

---

## Indexes Strategy

### StoreHub
```sql
-- Primary lookups
CREATE INDEX idx_invoice_public_id ON invoice(public_id);
CREATE INDEX idx_product_public_id ON product(public_id);
CREATE INDEX idx_product_barcode ON product(barcode) WHERE barcode IS NOT NULL;

-- Sync worker performance
CREATE INDEX idx_outbox_polling ON outbox_event(status, next_retry_utc, created_utc);

-- Stock queries
CREATE INDEX idx_inventory_movement_product ON inventory_movement(product_public_id, store_id);
```

### Cloud
```sql
-- Idempotency check (critical path)
CREATE UNIQUE INDEX idx_synced_event_public_id ON synced_event(event_public_id);

-- Reporting queries
CREATE INDEX idx_invoice_store_date ON invoice(store_id, created_utc);
CREATE INDEX idx_invoice_line_product ON invoice_line(product_public_id);
CREATE INDEX idx_inventory_movement_store_product ON inventory_movement(store_id, product_public_id, created_utc);

-- Master data sync
CREATE INDEX idx_product_modified ON product(last_modified_utc);
```

---

**Next:** See `05-deployment.md` for deployment architecture
