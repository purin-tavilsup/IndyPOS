# Data Flow Diagrams

Version: 1.0.0
Date: 2026-03-03

## Flow 1: Complete Sale (Happy Path)

```
┌─────────────┐
│ POS Client  │
│ (Desktop/   │
│  Tablet)    │
└──────┬──────┘
       │
       │ 1. POST /sales/complete
       │    {
       │      items: [...],
       │      payments: [...],
       │      customerId: "..."
       │    }
       │
       ▼
┌──────────────────────────────────────┐
│   StoreHub API                       │
│   SalesController.Complete()         │
└──────┬───────────────────────────────┘
       │
       │ 2. Begin Transaction
       │
       ▼
┌────────────────────────────────────────────────────────────┐
│  Application Layer - CompleteSaleCommandHandler            │
│                                                            │
│  • Validate stock availability                            │
│  • Calculate totals                                       │
│  • Create Invoice entity (with PublicId GUID)             │
│  • Create InvoiceLine entities                            │
│  • Create Payment entities                                │
│  • Create InventoryMovement entities (negative qty)       │
│  • Create OutboxEvent ("InvoiceCompleted")                │
└────────┬───────────────────────────────────────────────────┘
         │
         │ 3. Save all to DB (atomic transaction)
         │
         ▼
┌─────────────────────────────────────────────────────────┐
│   Local PostgreSQL (indypos_storehub)                   │
│                                                         │
│   INSERT INTO invoice (...) VALUES (...)                │
│   INSERT INTO invoice_line (...) VALUES (...)           │
│   INSERT INTO payment (...) VALUES (...)                │
│   INSERT INTO inventory_movement (...) VALUES (...)     │
│   INSERT INTO outbox_event (                            │
│     public_id = '...',                                  │
│     store_id = 'STORE-001',                             │
│     type = 'InvoiceCompleted',                          │
│     payload_json = '{...}',                             │
│     status = 'Pending'                                  │
│   )                                                     │
│                                                         │
│   COMMIT;                                               │
└─────────┬───────────────────────────────────────────────┘
          │
          │ 4. Return success
          │
          ▼
    ┌──────────┐
    │   200 OK │
    │ {        │
    │   invoicePublicId: "guid",
    │   total: 1250.00
    │ }        │
    └──────────┘
          │
          │ 5. Display receipt
          │
          ▼
    ┌──────────────┐
    │  POS Client  │
    │  Shows       │
    │  Receipt     │
    └──────────────┘
```

---

## Flow 2: Background Sync (Outbox → Cloud)

```
                 ┌─────────────────────────────┐
                 │  SyncWorker                 │
                 │  (BackgroundService)        │
                 │  Runs every 3 seconds       │
                 └──────┬──────────────────────┘
                        │
                        │ 1. Query pending events
                        │
                        ▼
        ┌────────────────────────────────────────────────┐
        │  SELECT * FROM outbox_event                    │
        │  WHERE status = 'Pending'                      │
        │    AND (next_retry_utc IS NULL                 │
        │         OR next_retry_utc <= NOW())            │
        │  ORDER BY created_utc                          │
        │  LIMIT 20                                      │
        └────────┬───────────────────────────────────────┘
                 │
                 │ 2. For each event:
                 │
                 ▼
        ┌──────────────────────┐
        │  CloudSyncClient     │
        │  POST /sync/events   │
        └──────┬───────────────┘
               │
               │ 3. HTTPS Request
               │    {
               │      storeId: "STORE-001",
               │      eventPublicId: "guid-123",
               │      type: "InvoiceCompleted",
               │      createdUtc: "2026-03-03T10:30:00Z",
               │      payload: { invoice, lines, payments }
               │    }
               │
               ▼
    ┌──────────────────────────────────────────────┐
    │  Cloud API - /sync/events                    │
    │                                              │
    │  4. Check idempotency:                       │
    │     SELECT * FROM synced_event               │
    │     WHERE event_public_id = 'guid-123'       │
    │                                              │
    │  5. If exists → return 200 OK (already done) │
    │     If new → process event                   │
    └──────┬───────────────────────────────────────┘
           │
           │ 6. Process event (if new)
           │
           ▼
    ┌────────────────────────────────────────────┐
    │  Cloud PostgreSQL                          │
    │                                            │
    │  BEGIN;                                    │
    │                                            │
    │  INSERT INTO synced_event (                │
    │    event_public_id = 'guid-123',           │
    │    store_id = 'STORE-001',                 │
    │    received_utc = NOW()                    │
    │  );                                        │
    │                                            │
    │  INSERT INTO invoice (...);                │
    │  INSERT INTO invoice_line (...);           │
    │  INSERT INTO payment (...);                │
    │  INSERT INTO inventory_movement (...);     │
    │                                            │
    │  COMMIT;                                   │
    └────────┬───────────────────────────────────┘
             │
             │ 7. Return 200 OK
             │
             ▼
    ┌──────────────┐
    │  SyncWorker  │
    │  Receives OK │
    └──────┬───────┘
           │
           │ 8. Update outbox
           │
           ▼
    ┌─────────────────────────────────┐
    │  UPDATE outbox_event            │
    │  SET status = 'Sent',           │
    │      last_attempt_utc = NOW()   │
    │  WHERE public_id = 'guid-123'   │
    └─────────────────────────────────┘
```

### Retry Flow (Internet Down)

```
    ┌──────────────┐
    │  SyncWorker  │
    └──────┬───────┘
           │
           │ 1. Try to send event
           │
           ▼
    [Internet Down / Cloud Unreachable]
           │
           │ 2. POST fails
           │
           ▼
    ┌─────────────────────────────────────────┐
    │  UPDATE outbox_event                    │
    │  SET attempts = attempts + 1,           │
    │      last_attempt_utc = NOW(),          │
    │      next_retry_utc = NOW() + backoff   │
    │  WHERE public_id = 'guid-123'           │
    │                                         │
    │  Backoff formula:                       │
    │  min(1800, 2^min(10, attempts)) seconds │
    │                                         │
    │  Attempt 1: retry in 2 sec              │
    │  Attempt 2: retry in 4 sec              │
    │  Attempt 3: retry in 8 sec              │
    │  Attempt 4: retry in 16 sec             │
    │  Attempt 10+: retry in 30 min           │
    └─────────────────────────────────────────┘
           │
           │ 3. Wait until next_retry_utc
           │
           ▼
    ┌──────────────┐
    │  Try again   │
    │  (loop)      │
    └──────────────┘
```

---

## Flow 3: Master Data Pull (Cloud → Store)

```
┌─────────────────────────────┐
│  SyncWorker                 │
│  (Background - every 5 min) │
└──────┬──────────────────────┘
       │
       │ 1. Check last sync timestamp
       │    SELECT MAX(last_sync_utc)
       │    FROM synced_master_data
       │    WHERE type = 'Product'
       │
       ▼
┌──────────────────────────────────────┐
│  CloudSyncClient                     │
│  GET /master/products                │
│  ?storeId=STORE-001                  │
│  &since=2026-03-03T08:00:00Z         │
└──────┬───────────────────────────────┘
       │
       │ 2. HTTPS Request
       │
       ▼
┌────────────────────────────────────────┐
│  Cloud API - /master/products          │
│                                        │
│  SELECT * FROM product                 │
│  WHERE last_modified_utc > @since      │
│                                        │
│  Return:                               │
│  {                                     │
│    products: [                         │
│      {                                 │
│        publicId: "...",                │
│        name: "...",                    │
│        price: 100,                     │
│        lastModifiedUtc: "..."          │
│      }                                 │
│    ],                                  │
│    syncToken: "2026-03-03T10:00:00Z"   │
│  }                                     │
└────────┬───────────────────────────────┘
         │
         │ 3. Receive products
         │
         ▼
┌──────────────────────────────────────────┐
│  StoreHub - MasterDataSyncService        │
│                                          │
│  For each product:                       │
│    • Check if exists (by public_id)      │
│    • If exists → UPDATE                  │
│    • If new → INSERT                     │
│                                          │
│  Update sync token:                      │
│    INSERT/UPDATE synced_master_data      │
│    SET last_sync_utc = @syncToken        │
└──────────────────────────────────────────┘
```

---

## Flow 4: Inventory Adjustment

```
┌──────────────┐
│  POS Client  │
└──────┬───────┘
       │
       │ POST /inventory/adjust
       │ {
       │   productPublicId: "...",
       │   quantityDelta: +50,
       │   reason: "Restock",
       │   note: "Supplier delivery"
       │ }
       │
       ▼
┌──────────────────────────────────────┐
│  StoreHub API                        │
│  InventoryController.Adjust()       │
└──────┬───────────────────────────────┘
       │
       │ Begin Transaction
       │
       ▼
┌────────────────────────────────────────────────────┐
│  Application Layer                                 │
│                                                    │
│  INSERT INTO inventory_movement (                  │
│    public_id = newGuid(),                          │
│    store_id = 'STORE-001',                         │
│    product_public_id = '...',                      │
│    quantity_delta = +50,                           │
│    reason = 'Restock',                             │
│    reference_public_id = NULL,                     │
│    created_utc = NOW()                             │
│  );                                                │
│                                                    │
│  INSERT INTO outbox_event (                        │
│    type = 'InventoryMovementRecorded',             │
│    payload_json = '{...}'                          │
│  );                                                │
│                                                    │
│  COMMIT;                                           │
└────────────────────────────────────────────────────┘
       │
       │ Sync to Cloud (same as Flow 2)
       │
       ▼
    [Cloud receives and stores movement]
```

---

## Flow 5: Query Current Stock

```
┌──────────────┐
│  POS Client  │
└──────┬───────┘
       │
       │ GET /products/{publicId}/stock
       │
       ▼
┌──────────────────────────────────────┐
│  StoreHub API                        │
│  ProductsController.GetStock()       │
└──────┬───────────────────────────────┘
       │
       ▼
┌───────────────────────────────────────────────────┐
│  Query:                                           │
│                                                   │
│  SELECT SUM(quantity_delta) as current_stock      │
│  FROM inventory_movement                          │
│  WHERE product_public_id = @publicId              │
│    AND store_id = 'STORE-001'                     │
│                                                   │
│  • Sale: -qty                                     │
│  • Restock: +qty                                  │
│  • Adjustment: +/- qty                            │
│  • Transfer in: +qty                              │
│  • Transfer out: -qty                             │
└───────────────┬───────────────────────────────────┘
                │
                │ Return stock level
                │
                ▼
         ┌──────────────┐
         │  200 OK      │
         │  {           │
         │    stock: 25 │
         │  }           │
         └──────────────┘
```

---

## Data Consistency Guarantees

### Local (StoreHub)
```
✅ ACID Transactions
   • Business data + Outbox event written together
   • If commit fails, entire transaction rolls back
   • Never lose a sale

✅ Sequential Processing
   • Outbox events sent in order (oldest first)
   • Natural ordering by created_utc
```

### Cloud (Central)
```
✅ Idempotency
   • Same event_public_id sent twice → processed once
   • synced_event table prevents duplicates

✅ Eventual Consistency
   • Store data may lag behind cloud by minutes/hours
   • Cloud always eventually catches up
   • Acceptable for reporting use case
```

---

**Next:** See `03-component-relationships.md` for component diagram
