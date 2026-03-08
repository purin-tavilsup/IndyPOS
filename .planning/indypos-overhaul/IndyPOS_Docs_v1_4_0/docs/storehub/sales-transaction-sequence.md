# StoreHub Sales Transaction Sequence
Version: 1.5.2

This document shows how one sale should be committed safely when:
- 2 terminals can sell in the same store
- the internet may be down
- inventory and invoice numbering must stay correct
- cloud sync is asynchronous

## Core rule

The entire sale commit must happen inside one PostgreSQL transaction.

That transaction should include:
1. invoice number generation
2. invoice header insert
3. invoice line inserts
4. payment inserts
5. inventory movement inserts
6. outbox event insert

Only after all of that succeeds should the transaction commit.

## ASCII sequence

+-------------+       +------------------+       +--------------------+
| POS Terminal | ----> |    StoreHub API  | ----> |  PostgreSQL Tx     |
+-------------+       +------------------+       +--------------------+
        |                       |                            |
        | 1. POST /sales/complete                           |
        |---------------------->|                            |
        |                       | 2. validate request        |
        |                       |--------------------------->|
        |                       |                            |
        |                       | 3. begin transaction       |
        |                       |--------------------------->|
        |                       |                            |
        |                       | 4. reserve/generate        |
        |                       |    invoice number          |
        |                       |--------------------------->|
        |                       |                            |
        |                       | 5. check stock / lock rows |
        |                       |--------------------------->|
        |                       |                            |
        |                       | 6. insert invoice          |
        |                       |--------------------------->|
        |                       |                            |
        |                       | 7. insert invoice lines    |
        |                       |--------------------------->|
        |                       |                            |
        |                       | 8. insert payments         |
        |                       |--------------------------->|
        |                       |                            |
        |                       | 9. insert inventory moves  |
        |                       |--------------------------->|
        |                       |                            |
        |                       | 10. insert outbox event    |
        |                       |--------------------------->|
        |                       |                            |
        |                       | 11. commit                 |
        |                       |--------------------------->|
        |                       |                            |
        | 12. success response  |                            |
        |<----------------------|                            |

## Transaction details

### Step 1 - Receive request
Request should include:
- terminal id
- cashier id
- cart lines
- discounts
- payments
- customer reference if any

### Step 2 - Validate
Validate before opening or early in the transaction:
- product exists
- payment totals are valid
- cart is not empty
- cashier has permission

### Step 3 - Begin transaction
Use one DB transaction for all write operations.

### Step 4 - Generate invoice number
StoreHub owns final invoice numbering.

Recommended:
- use a database-backed sequence or counter table
- never let terminals generate final invoice numbers independently

Example:
- temporary cart id: CART-T1-20260228-0007
- final invoice no: INV-STORE001-20260228-0152

### Step 5 - Check stock / lock rows
For stock-controlled items:
- read the current stock state
- lock relevant product/inventory rows
- verify enough quantity is available

This prevents both terminals from selling the same last unit at the same time.

### Step 6 - Insert invoice
Insert the invoice header with:
- PublicId
- StoreId
- final invoice number
- totals
- created timestamp
- terminal id
- cashier id

### Step 7 - Insert invoice lines
Insert each line item with:
- line PublicId
- product PublicId
- qty
- price
- discounts/tax info

### Step 8 - Insert payments
Insert one or more payment rows:
- cash
- card
- promptpay / qr
- split payment if supported

### Step 9 - Insert inventory movements
Do not set stock directly.
Insert movements instead.

Example:
- sale of qty 2 => quantity_delta = -2
- refund of qty 1 => quantity_delta = +1

### Step 10 - Insert outbox event
Insert one outbox event in the same transaction.

Recommended event type:
- InvoiceCompleted

Payload should contain enough data for cloud replay:
- invoice
- lines
- payments
- inventory movements
- store id
- timestamps

### Step 11 - Commit
Only commit after every write succeeds.

If any part fails:
- roll back the whole transaction
- return an error to terminal
- do not emit partial invoice or partial stock changes

### Step 12 - Return response
Response should include:
- invoice PublicId
- final invoice number
- totals
- committed timestamp

## Recommended database write order

Use this order consistently:

1. invoice number
2. invoice header
3. invoice lines
4. payments
5. inventory movements
6. outbox

This keeps the system easier to reason about.

## Example API response

{
  "invoicePublicId": "3e3bc0b3-b0f1-4d95-a3b8-c1dfb8f8c520",
  "invoiceNumber": "INV-STORE001-20260228-0152",
  "committedUtc": "2026-02-28T10:42:13Z",
  "totalAmount": 450.00
}

## Background sync after commit

Cloud sync does NOT happen inside the sale transaction.

After commit:

+--------------------+       +------------------+       +--------------------+
| Outbox Pending Row | ----> |    Sync Worker   | ----> |     Cloud API      |
+--------------------+       +------------------+       +--------------------+
                                                              |
                                                              v
                                                     +--------------------+
                                                     | Cloud PostgreSQL   |
                                                     +--------------------+

This is important because:
- a store must keep selling if internet is down
- cloud failure must not block checkout
- retries must be safe and idempotent

## Failure handling

### Case A - DB write fails before commit
Result:
- rollback
- no invoice
- no inventory change
- no outbox event

### Case B - Commit succeeds but internet is down
Result:
- sale is complete locally
- outbox remains pending
- sync happens later

### Case C - Cloud receives duplicate event
Result:
- cloud detects existing EventPublicId
- returns success/duplicate
- safe retry

## Recommendation

For IndyPOS, every successful sale should produce:
- one committed local invoice
- one committed set of inventory movements
- one committed outbox event

That gives you:
- correct in-store behavior
- safe multi-terminal concurrency
- reliable async cloud sync
