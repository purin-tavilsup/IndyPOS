# Terminal Concurrency (Multi-Terminal Safety)

Version: 1.0.0
Date: 2026-03-08

## Overview

This diagram shows how multiple POS terminals safely share one StoreHub and PostgreSQL database without conflicts.

## Multi-Terminal Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           STORE (Single Location)                           │
│                                                                             │
│   ┌─────────────────┐                     ┌─────────────────┐              │
│   │  Terminal 1     │                     │  Terminal 2     │              │
│   │  (Desktop)      │                     │  (Tablet)       │              │
│   │                 │                     │                 │              │
│   │  Cart: 3 items  │                     │  Cart: 2 items  │              │
│   └────────┬────────┘                     └────────┬────────┘              │
│            │                                       │                        │
│            │ POST /sales/complete                  │ POST /sales/complete   │
│            │ (nearly same time)                    │                        │
│            │                                       │                        │
│            └───────────────────┬───────────────────┘                        │
│                                │                                            │
│                                ▼                                            │
│            ┌───────────────────────────────────────┐                       │
│            │          StoreHub API                 │                       │
│            │  ┌─────────────────────────────────┐  │                       │
│            │  │  PostgreSQL Transaction Lock    │  │                       │
│            │  │                                 │  │                       │
│            │  │  Terminal 1: BEGIN TX           │  │                       │
│            │  │    → Lock Product A row         │  │                       │
│            │  │    → Check stock: 5 available   │  │                       │
│            │  │    → Sell qty 2 ✓               │  │                       │
│            │  │    → Generate INV-001           │  │                       │
│            │  │  COMMIT                         │  │                       │
│            │  │                                 │  │                       │
│            │  │  Terminal 2: BEGIN TX           │  │                       │
│            │  │    → Lock Product A row         │  │                       │
│            │  │    → Check stock: 3 available   │  │  ← Updated!           │
│            │  │    → Sell qty 1 ✓               │  │                       │
│            │  │    → Generate INV-002           │  │  ← Sequential!        │
│            │  │  COMMIT                         │  │                       │
│            │  └─────────────────────────────────┘  │                       │
│            └───────────────────┬───────────────────┘                       │
│                                │                                            │
│                                ▼                                            │
│            ┌───────────────────────────────────────┐                       │
│            │          Local PostgreSQL             │                       │
│            │                                       │                       │
│            │  Product A: stock = 2 (was 5)        │                       │
│            │  Invoice INV-001: Terminal 1         │                       │
│            │  Invoice INV-002: Terminal 2         │                       │
│            │  Outbox: 2 events pending            │                       │
│            └───────────────────────────────────────┘                       │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Key Safety Rules

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           SAFETY RULES                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ✓ Both terminals call SAME StoreHub (not separate DBs)                    │
│                                                                             │
│  ✓ StoreHub generates invoice numbers (not terminals)                      │
│                                                                             │
│  ✓ PostgreSQL row locking prevents double-selling last item                │
│                                                                             │
│  ✓ Stock calculated from movements, not "SET quantity = X"                 │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Why NOT Separate Databases?

```
❌ BAD: Each Terminal Has Own DB
──────────────────────────────────

Terminal 1 DB          Terminal 2 DB
┌──────────────┐       ┌──────────────┐
│ Stock A = 5  │       │ Stock A = 5  │    ← Same starting stock!
│ INV-001      │       │ INV-001      │    ← Collision!
└──────────────┘       └──────────────┘

Problems:
• Invoice numbers collide
• Stock drifts apart
• Sync conflicts are complex
• Who has the "truth"?


✓ GOOD: Shared StoreHub + One DB
────────────────────────────────

                 ┌───────────────────┐
Terminal 1 ────► │                   │
                 │  StoreHub + PG    │  ← Single source of truth
Terminal 2 ────► │                   │
                 └───────────────────┘

Benefits:
• One transaction boundary
• One stock source of truth
• One invoice sequence
• Simple cloud sync
```

## Transaction Sequence

```
Terminal 1                StoreHub                 PostgreSQL
    │                         │                         │
    │ POST /sales/complete    │                         │
    ├────────────────────────►│                         │
    │                         │ BEGIN TX                │
    │                         ├────────────────────────►│
    │                         │                         │
    │                         │ SELECT FOR UPDATE       │
    │                         │ (lock product rows)     │
    │                         ├────────────────────────►│
    │                         │                         │
    │                         │ Check stock available   │
    │                         │◄────────────────────────┤
    │                         │                         │
    │                         │ INSERT invoice          │
    │                         ├────────────────────────►│
    │                         │                         │
    │                         │ INSERT lines            │
    │                         ├────────────────────────►│
    │                         │                         │
    │                         │ INSERT payments         │
    │                         ├────────────────────────►│
    │                         │                         │
    │                         │ INSERT inventory_movement│
    │                         │ (qty_delta = -2)        │
    │                         ├────────────────────────►│
    │                         │                         │
    │                         │ INSERT outbox_event     │
    │                         ├────────────────────────►│
    │                         │                         │
    │                         │ COMMIT                  │
    │                         ├────────────────────────►│
    │                         │                         │
    │  { invoiceId, number }  │                         │
    │◄────────────────────────┤                         │
    │                         │                         │
```

## Inventory Movement Pattern

```
❌ BAD: Direct Stock Update
───────────────────────────

UPDATE product SET quantity = 3 WHERE id = 1;

Problem: Two terminals can overwrite each other


✓ GOOD: Movement-Based Tracking
────────────────────────────────

INSERT INTO inventory_movement (
  product_id,
  quantity_delta,  ← Always relative: -2, +1, etc.
  reason,
  reference_id
) VALUES (...);

Benefits:
• Never overwrites
• Full audit trail
• Calculate balance: SUM(quantity_delta)
• Easy reconciliation
```

## Invoice Number Generation

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      INVOICE NUMBER OWNERSHIP                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Terminal sends:                                                            │
│    cart_id: "CART-T1-20260228-0001"   ← Temporary, client-side only        │
│                                                                             │
│  StoreHub generates:                                                        │
│    invoice_number: "INV-STORE001-20260228-0152"   ← Final, sequential      │
│                                                                             │
│  Method:                                                                    │
│    • Database sequence or counter table                                     │
│    • Generated inside the transaction                                       │
│    • Never gaps (unless rollback)                                          │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

**Related:**
- `02-data-flow.md` - Sale completion sequence
- `05-sync-flow-outbox-pattern.md` - How outbox events sync to cloud
- `IndyPOS_Docs_v1_4_0/docs/storehub/terminal-concurrency-strategy.md` - Full spec
