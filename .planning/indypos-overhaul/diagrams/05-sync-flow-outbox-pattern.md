# Sync Flow - Outbox Pattern Details

Version: 1.0.0
Date: 2026-03-03

## Outbox Pattern Overview

The Outbox Pattern ensures reliable event delivery from Store → Cloud even when the network is unreliable.

```
┌────────────────────────────────────────────────────────────────┐
│  Key Guarantee:                                                │
│  Business operation + event creation happens in ONE            │
│  database transaction. Either both succeed or both fail.       │
│  This prevents data inconsistency.                             │
└────────────────────────────────────────────────────────────────┘
```

---

## Complete Sale with Outbox - Detailed Flow

### Phase 1: Transaction (Atomic)

```
┌─────────────────────────────────────────────────────────────────┐
│  Application Handler: CompleteSaleCommandHandler               │
└───────────────────────┬─────────────────────────────────────────┘
                        │
                        │ 1. Begin Transaction
                        ▼
┌─────────────────────────────────────────────────────────────────┐
│  PostgreSQL Transaction Boundary                                │
│  ═══════════════════════════════════════════════════════════════│
│                                                                 │
│  Step 1: Insert Invoice                                        │
│  ────────────────────────                                      │
│  INSERT INTO invoice (                                          │
│    public_id,              -- GUID generated                    │
│    store_id,               -- 'STORE-001'                       │
│    invoice_number,         -- 'INV-2026-001234'                 │
│    total_amount,           -- 1250.00                           │
│    status,                 -- 'Completed'                       │
│    created_utc             -- NOW()                             │
│  ) VALUES (...);                                                │
│                                                                 │
│  Step 2: Insert Invoice Lines                                  │
│  ──────────────────────────────                                │
│  INSERT INTO invoice_line (                                     │
│    public_id,              -- GUID                              │
│    invoice_id,             -- FK to invoice.id                  │
│    product_public_id,      -- GUID of product                   │
│    quantity,               -- 2                                 │
│    unit_price,             -- 500.00                            │
│    line_total              -- 1000.00                           │
│  ) VALUES (...);                                                │
│                                                                 │
│  Step 3: Insert Payments                                       │
│  ──────────────────────                                        │
│  INSERT INTO payment (                                          │
│    public_id,              -- GUID                              │
│    invoice_id,             -- FK to invoice.id                  │
│    method,                 -- 'Cash'                            │
│    amount                  -- 1250.00                           │
│  ) VALUES (...);                                                │
│                                                                 │
│  Step 4: Insert Inventory Movements                            │
│  ────────────────────────────────────                          │
│  INSERT INTO inventory_movement (                               │
│    public_id,              -- GUID                              │
│    store_id,               -- 'STORE-001'                       │
│    product_public_id,      -- GUID of product                   │
│    quantity_delta,         -- -2 (sold)                         │
│    reason,                 -- 'Sale'                            │
│    reference_public_id     -- Invoice public_id                 │
│  ) VALUES (...);                                                │
│                                                                 │
│  Step 5: Insert Outbox Event (CRITICAL)                        │
│  ────────────────────────────────────────                      │
│  INSERT INTO outbox_event (                                     │
│    public_id,              -- GUID (event identifier)           │
│    store_id,               -- 'STORE-001'                       │
│    type,                   -- 'InvoiceCompleted'                │
│    payload_json,           -- Full invoice data as JSON         │
│    status,                 -- 'Pending'                         │
│    created_utc,            -- NOW()                             │
│    attempts                -- 0                                 │
│  ) VALUES (...);                                                │
│                                                                 │
│  payload_json example:                                          │
│  {                                                              │
│    "invoicePublicId": "guid-123",                               │
│    "invoiceNumber": "INV-2026-001234",                          │
│    "totalAmount": 1250.00,                                      │
│    "createdUtc": "2026-03-03T10:30:00Z",                        │
│    "lines": [                                                   │
│      {                                                          │
│        "publicId": "guid-456",                                  │
│        "productPublicId": "guid-789",                           │
│        "productName": "Product A",                              │
│        "quantity": 2,                                           │
│        "unitPrice": 500.00,                                     │
│        "lineTotal": 1000.00                                     │
│      }                                                          │
│    ],                                                           │
│    "payments": [                                                │
│      {                                                          │
│        "publicId": "guid-abc",                                  │
│        "method": "Cash",                                        │
│        "amount": 1250.00                                        │
│      }                                                          │
│    ],                                                           │
│    "movements": [                                               │
│      {                                                          │
│        "publicId": "guid-def",                                  │
│        "productPublicId": "guid-789",                           │
│        "quantityDelta": -2,                                     │
│        "reason": "Sale"                                         │
│      }                                                          │
│    ]                                                            │
│  }                                                              │
│                                                                 │
│  COMMIT;  ◄── All or nothing!                                  │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
         │
         │ Success!
         │
         ▼
┌──────────────────┐
│  Return 200 OK   │
│  to POS Client   │
└──────────────────┘
```

**Key Point:** If commit fails (power loss, DB crash, constraint violation), the entire transaction rolls back. No orphaned data. No lost events.

---

### Phase 2: Background Sync (Eventual)

```
┌─────────────────────────────────────────────────────────────────┐
│  SyncWorker Loop (runs every 3 seconds)                         │
└───────────────────────┬─────────────────────────────────────────┘
                        │
                        │ Query for pending events
                        ▼
┌─────────────────────────────────────────────────────────────────┐
│  SELECT * FROM outbox_event                                     │
│  WHERE status = 'Pending'                                       │
│    AND (next_retry_utc IS NULL                                  │
│         OR next_retry_utc <= NOW())                             │
│  ORDER BY created_utc                                           │
│  LIMIT 20;                                                      │
└───────────────────────┬─────────────────────────────────────────┘
                        │
                        │ Found 3 events
                        ▼
┌─────────────────────────────────────────────────────────────────┐
│  For each event:                                                │
│                                                                 │
│  Event 1: event_public_id = guid-123                            │
│  ──────────────────────────────────────────                    │
│                                                                 │
│  Try to send:                                                   │
│    POST https://api.indypos.com/sync/events                     │
│    Headers:                                                     │
│      Authorization: ApiKey STORE-001:secret                     │
│      Content-Type: application/json                             │
│    Body:                                                        │
│    {                                                            │
│      "storeId": "STORE-001",                                    │
│      "eventPublicId": "guid-123",                               │
│      "type": "InvoiceCompleted",                                │
│      "createdUtc": "2026-03-03T10:30:00Z",                      │
│      "payload": { ...full invoice data... }                     │
│    }                                                            │
│                                                                 │
└───────────────────────┬─────────────────────────────────────────┘
                        │
                        ▼
              ┌─────────────────┐
              │  Network OK?    │
              └────┬────────┬───┘
                   │        │
              YES  │        │ NO
                   │        │
                   ▼        ▼
        ┌──────────────┐  ┌────────────────────────────┐
        │ Cloud Receives│  │ Network Error / Timeout   │
        └──────┬───────┘  └───────────┬────────────────┘
               │                      │
               │                      │ Log error
               │                      ▼
               │          ┌─────────────────────────────────┐
               │          │ UPDATE outbox_event             │
               │          │ SET attempts = attempts + 1,    │
               │          │     last_attempt_utc = NOW(),   │
               │          │     next_retry_utc = NOW() +    │
               │          │       EXPONENTIAL_BACKOFF       │
               │          │ WHERE public_id = 'guid-123'    │
               │          └─────────────────────────────────┘
               │                      │
               │                      │ Backoff calculation:
               │                      │ min(1800, 2^min(10, attempts))
               │                      │
               │                      │ Attempt 1: 2 sec
               │                      │ Attempt 2: 4 sec
               │                      │ Attempt 3: 8 sec
               │                      │ Attempt 4: 16 sec
               │                      │ Attempt 5: 32 sec
               │                      │ Attempt 10+: 30 min
               │                      │
               │                      ▼
               │             [Wait until next_retry_utc]
               │
               ▼
┌─────────────────────────────────────────────────────────────────┐
│  Cloud API: POST /sync/events                                   │
│                                                                 │
│  Step 1: Check Idempotency                                      │
│  ──────────────────────────                                    │
│  SELECT 1 FROM synced_event                                     │
│  WHERE event_public_id = 'guid-123';                            │
│                                                                 │
│  If exists → return 200 OK (already processed)                  │
│  If not exists → continue to Step 2                             │
│                                                                 │
│  Step 2: Process Event (if new)                                │
│  ────────────────────────────────                              │
│  BEGIN TRANSACTION;                                             │
│                                                                 │
│    -- Mark event as received (deduplication)                    │
│    INSERT INTO synced_event (                                   │
│      event_public_id,                                           │
│      store_id,                                                  │
│      type,                                                      │
│      received_utc                                               │
│    ) VALUES (                                                   │
│      'guid-123',                                                │
│      'STORE-001',                                               │
│      'InvoiceCompleted',                                        │
│      NOW()                                                      │
│    );                                                           │
│                                                                 │
│    -- Parse payload and insert transactional data               │
│    INSERT INTO invoice (...) VALUES (...);                      │
│    INSERT INTO invoice_line (...) VALUES (...);                 │
│    INSERT INTO payment (...) VALUES (...);                      │
│    INSERT INTO inventory_movement (...) VALUES (...);           │
│                                                                 │
│  COMMIT;                                                        │
│                                                                 │
│  Return 200 OK                                                  │
└───────────────────────┬─────────────────────────────────────────┘
                        │
                        │ Success!
                        ▼
┌─────────────────────────────────────────────────────────────────┐
│  SyncWorker receives 200 OK                                     │
│                                                                 │
│  UPDATE outbox_event                                            │
│  SET status = 'Sent',                                           │
│      last_attempt_utc = NOW()                                   │
│  WHERE public_id = 'guid-123';                                  │
└─────────────────────────────────────────────────────────────────┘
         │
         │ Event successfully synced!
         │
         ▼
    [Move to next event in batch]
```

---

## Idempotency Guarantee

```
Scenario: Network glitch causes retry of same event

┌──────────────────┐
│  SyncWorker      │
│  Attempt 1       │
└────────┬─────────┘
         │
         │ POST event guid-123
         │
         ▼
┌─────────────────────────────────┐
│  Cloud API                      │
│  • Check synced_event           │
│  • Not found → Process          │
│  • Insert into synced_event     │
│  • Insert transactional data    │
│  • Return 200 OK                │
└────────┬────────────────────────┘
         │
         │ 200 OK
         │ [But response lost in network!]
         │
         ✗ [Network error - no response received]

┌──────────────────┐
│  SyncWorker      │
│  Thinks it failed│
│  Schedules retry │
└────────┬─────────┘
         │
         │ (After backoff)
         │ POST event guid-123 AGAIN
         │
         ▼
┌─────────────────────────────────┐
│  Cloud API                      │
│  • Check synced_event           │
│  • FOUND! ◄── Already processed │
│  • Skip processing              │
│  • Return 200 OK                │
└────────┬────────────────────────┘
         │
         │ 200 OK
         │
         ▼
┌──────────────────┐
│  SyncWorker      │
│  Receives OK     │
│  Marks as Sent   │
└──────────────────┘

Result: No duplicate data in cloud! ✅
```

---

## Failure Scenarios & Recovery

### Scenario 1: Internet Down During Sale

```
Customer buys item
     │
     ▼
StoreHub processes sale
     │
     ▼
Transaction commits (invoice + outbox event)
     │
     ▼
Return 200 OK to POS ✅
     │
     │ (Later, in background)
     ▼
SyncWorker tries to send
     │
     ▼
[Internet Down] ✗
     │
     ▼
Retry with exponential backoff
     │
     │ (Hours later, internet back)
     ▼
Successfully syncs ✅

Impact: NONE for customer! Sale completed locally.
```

### Scenario 2: Database Crash During Transaction

```
Begin Transaction
     │
     ▼
Insert invoice ✅
     │
     ▼
Insert invoice_line ✅
     │
     ▼
[Power loss / DB crash] ✗
     │
     ▼
Transaction ROLLBACK (automatic)
     │
     ▼
Nothing persisted

Result: Sale fails, POS shows error, cashier retries
No partial data! ✅
```

### Scenario 3: Cloud API Down

```
SyncWorker sends event
     │
     ▼
[Cloud API 503 Service Unavailable]
     │
     ▼
Retry attempt 1: 2 sec later
     │
     ▼
[Still 503]
     │
     ▼
Retry attempt 2: 4 sec later
...continues with exponential backoff
     │
     ▼
(Eventually cloud comes back online)
     │
     ▼
Successfully syncs ✅

Impact: Local sales continue normally!
Cloud data lags temporarily.
```

---

## Monitoring & Observability

### SyncWorker Metrics

```
┌──────────────────────────────────────────────────────┐
│  GET /sync/status                                    │
│                                                      │
│  {                                                   │
│    "pendingEventCount": 5,                           │
│    "oldestPendingEventAge": "PT2H15M",  (2hr 15min) │
│    "lastSuccessfulSyncUtc": "2026-03-03T10:45:00Z",  │
│    "failedEventCount": 0,                            │
│    "syncWorkerStatus": "Running"                     │
│  }                                                   │
└──────────────────────────────────────────────────────┘
```

### Health Check

```
Healthy Conditions:
  ✅ pendingEventCount < 100
  ✅ oldestPendingEventAge < 1 hour
  ✅ failedEventCount == 0

Warning Conditions:
  ⚠️  pendingEventCount 100-500
  ⚠️  oldestPendingEventAge 1-4 hours
  ⚠️  failedEventCount > 0 but < 10

Critical Conditions:
  🔴 pendingEventCount > 500
  🔴 oldestPendingEventAge > 4 hours
  🔴 failedEventCount > 10
```

### Logging

```
[2026-03-03 10:30:15] [INFO] SyncWorker: Starting batch
[2026-03-03 10:30:15] [INFO] Found 3 pending events
[2026-03-03 10:30:16] [INFO] Event guid-123 sent successfully (attempt 1)
[2026-03-03 10:30:16] [INFO] Event guid-456 sent successfully (attempt 1)
[2026-03-03 10:30:17] [WARN] Event guid-789 failed (attempt 3): Network timeout
[2026-03-03 10:30:17] [INFO] Scheduled retry at 2026-03-03 10:30:25 (8 sec backoff)
[2026-03-03 10:30:17] [INFO] Batch complete: 2 sent, 1 retry scheduled
```

---

**Next:** See `06-epic-roadmap.md` for implementation timeline
