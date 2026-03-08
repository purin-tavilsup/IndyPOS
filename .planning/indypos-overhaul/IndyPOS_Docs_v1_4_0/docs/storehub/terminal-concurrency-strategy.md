# StoreHub Terminal Concurrency Strategy
Version: 1.5.1

This document covers the tricky in-store case:
- 2 terminals in one store
- both can sell while internet is down
- both must avoid stock and numbering conflicts

## Core rule

Both terminals must call the same local StoreHub API.
Do not allow each terminal to maintain its own separate local database.

That means:

+----------------+        LAN        +------------------+        +--------------------+
| POS Terminal 1 | --------------->  |                  | -----> |  Local PostgreSQL  |
+----------------+                   |    StoreHub API  |        +--------------------+
                                     |                  |
+----------------+        LAN        |                  |
| POS Terminal 2 | --------------->  |                  |
+----------------+                   +------------------+

## Why this is the safest model

If each terminal writes to its own DB:
- invoice numbers can collide
- stock can drift
- sync conflict handling gets much harder

If both terminals use one StoreHub:
- one transaction boundary
- one stock source of truth
- one invoice sequence owner
- simpler sync to cloud

## Transaction strategy

Use PostgreSQL transactions for sale completion.

Example flow:
1. Start DB transaction
2. Lock / validate inventory rows needed for the sale
3. Insert invoice
4. Insert invoice lines
5. Insert payments
6. Insert inventory movements
7. Insert outbox event
8. Commit

## Inventory consistency

Recommended approach:
- use inventory movements, not "set quantity = X"
- calculate stock from movements or maintain a derived balance table
- when selling, check available stock inside the same transaction

Example:

Terminal 1 sells Product A qty 1
Terminal 2 sells Product A qty 1 at nearly the same time

StoreHub handles both through the same DB.
PostgreSQL serialization / row locking prevents double-spend bugs.

## Invoice numbering

Do not let terminals generate final invoice numbers independently.

Recommended:
- StoreHub generates the next invoice number
- terminals may use a temporary client-side cart ID only
- final invoice number is assigned on successful commit

Example:

Terminal cart id: CART-T1-20260228-0001
Final invoice no: INV-STORE001-20260228-0152

## Offline behavior

"Offline" means internet to cloud is down.
It should NOT mean terminal-to-StoreHub LAN is down.

Normal offline store flow:

+----------------+       +------------------+       +--------------------+
| POS Terminal 1 | ----> |    StoreHub API  | ----> |  Local PostgreSQL  |
+----------------+       +------------------+       +--------------------+
                                  |
                                  v
                         +------------------+
                         | Outbox Pending   |
                         +------------------+

Later, when internet returns:

+------------------+       +------------------+       +--------------------+
| Outbox Pending   | ----> |    Cloud API     | ----> |  Cloud PostgreSQL  |
+------------------+       +------------------+       +--------------------+

## What if StoreHub PC fails?

This is the real local risk.

Recommended mitigation:
- StoreHub runs on the main desktop or a dedicated mini PC
- PostgreSQL backups every 4 hours
- spare restore procedure available
- optional spare hub device for critical stores

## Recommended implementation rules

1. One local PostgreSQL per store
2. One StoreHub per store
3. Many terminals can connect to StoreHub over LAN
4. StoreHub is the only writer
5. Cloud sync is asynchronous
6. PublicId is used for sync identity
7. Invoice numbers are assigned centrally by StoreHub

## Example API pattern

POST /sales/complete

Request:
- cart lines
- payments
- terminal id
- cashier id

StoreHub response:
- invoice public id
- final invoice number
- committed timestamp

## Recommendation

For IndyPOS, this is the best practical model:
- desktop PC hosts StoreHub + PostgreSQL
- tablet and desktop both call StoreHub over LAN
- cloud is only for sync, reporting, and MCP
