# PayLater Feature Documentation

**Date:** 2026-03-03
**Status:** Essential Feature - Must Keep

## Overview

PayLater (ลงบัญชี - Credit Account) is an essential feature for serving low-income customers in small towns who use credit/trust to purchase products and pay later through partial or full payments.

---

## Current Implementation (SQLite)

### Database Structure

**PayLater Table:**
```sql
CREATE TABLE PayLater (
  PaymentId INTEGER PRIMARY KEY,           -- FK to Payment
  Description TEXT,                        -- Customer name/identifier
  InvoiceId INTEGER NOT NULL,             -- FK to Invoice
  IsCompleted INTEGER NOT NULL DEFAULT 0, -- Fully paid flag
  DateCreated TEXT DEFAULT CURRENT_TIMESTAMP,
  DateUpdated TEXT,
  PayLaterAmount NUMERIC NOT NULL DEFAULT 0,  -- Total owed
  PaidAmount NUMERIC NOT NULL DEFAULT 0       -- Amount paid so far
);
```

### How It Works

1. **Initial Purchase:**
   - Customer buys products on credit
   - Invoice created with total amount
   - Payment record created with Type 2 (ลงบัญชี)
   - Customer name stored in Payment.Note field
   - PayLater record created to track the debt

2. **Tracking:**
   - `PayLaterAmount`: Total amount owed
   - `PaidAmount`: Amount paid so far (starts at 0)
   - `IsCompleted`: False until fully paid
   - `Description`: Customer name/identifier

3. **Partial Payments:**
   - Customer makes partial payment
   - `PaidAmount` updated (+= payment amount)
   - When `PaidAmount >= PayLaterAmount`, mark `IsCompleted = True`

4. **UI Features:**
   - Dedicated panel for managing PayLater payments
   - Search by customer name (Description field)
   - View all incomplete PayLater records
   - Update payments and mark as complete

### Current Statistics
- **Total PayLater records:** 5,181
- **Incomplete payments:** 144
- **Fully paid:** 5,037

---

## PostgreSQL Schema (New)

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
CREATE INDEX idx_pay_later_incomplete ON pay_later(is_completed)
  WHERE is_completed = FALSE;
```

### Changes from SQLite:
- ✅ `PaymentId` → `payment_id` (UUID FK)
- ✅ `InvoiceId` → `invoice_id` (UUID FK)
- ✅ `Description` → `description` (customer name)
- ✅ `PayLaterAmount` → `pay_later_amount`
- ✅ `PaidAmount` → `paid_amount`
- ✅ `IsCompleted` → `is_completed` (BOOLEAN instead of INTEGER)
- ✅ `DateCreated` → `created_utc` (TIMESTAMPTZ instead of TEXT)
- ✅ `DateUpdated` → `last_modified_utc` (TIMESTAMPTZ instead of TEXT)
- ✅ Added `id` (UUID PK)

---

## Entity Relationships

```
invoice (1) ──┬──> (N) invoice_line
              │
              ├──> (N) payment ──> (1) pay_later
              │
              └──> (N) inventory_movement
```

**Key Points:**
- Each PayLater record links to ONE payment record (initial credit payment)
- Each PayLater record links to ONE invoice
- Payment.method = "ลงบัญชี" (Credit Account / PayLater)
- Payment.note contains customer name
- PayLater.description also contains customer name (for search)

---

## Domain Model (C#)

```csharp
namespace IndyPOS.Domain.Entities;

public class PayLaterPayment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Foreign keys
    public Guid PaymentId { get; set; }
    public Guid InvoiceId { get; set; }

    // Customer tracking (no formal customer entity)
    public string Description { get; set; } = string.Empty;

    // Payment tracking
    public decimal PayLaterAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public bool IsCompleted { get; set; }

    // Audit
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastModifiedUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public Payment? Payment { get; set; }
    public Invoice? Invoice { get; set; }

    // Calculated properties
    public decimal OutstandingBalance => PayLaterAmount - PaidAmount;
    public decimal PercentPaid => PayLaterAmount > 0
        ? (PaidAmount / PayLaterAmount) * 100
        : 0;
}
```

---

## Use Cases

### Current Codebase Has:

1. **Create PayLater Payment**
   - `CreatePayLaterPaymentCommand`
   - `CreatePayLaterPaymentCommandHandler`
   - `CreatePayLaterPaymentCommandValidator`

2. **Update PayLater Payment**
   - `UpdatePayLaterPaymentCommand`
   - `UpdatePayLaterPaymentCommandHandler`
   - `UpdatePayLaterPaymentCommandValidator`

3. **Delete PayLater Payment**
   - `DeletePayLaterPaymentCommand`
   - `DeletePayLaterPaymentCommandHandler`
   - `DeletePayLaterPaymentCommandValidator`

4. **Query PayLater Payments**
   - `GetPayLaterPaymentsQuery` - Get all
   - `GetPayLaterPaymentByIdQuery` - Get by ID
   - `GetPayLaterPaymentByInvoiceIdQuery` - Get by invoice
   - `GetPayLaterPaymentsByDateRangeQuery` - Date range filter
   - `GetPayLaterPaymentsByDescriptionKeywordQuery` - Search by customer name

### UI Components:
- `PayLaterPaymentPanel.cs` - Main UI for managing PayLater
- `PayLaterPaymentsReportPanel.cs` - Reporting UI

---

## Migration Considerations

### SQLite → PostgreSQL

**Data Mapping:**
```sql
INSERT INTO pay_later (
    id,
    payment_id,
    invoice_id,
    description,
    pay_later_amount,
    paid_amount,
    is_completed,
    created_utc,
    last_modified_utc
)
SELECT
    gen_random_uuid(),                              -- New UUID
    p.uuid_for_payment,                             -- Map from Payment UUID
    i.uuid_for_invoice,                             -- Map from Invoice UUID
    pl.Description,
    pl.PayLaterAmount,
    pl.PaidAmount,
    CASE WHEN pl.IsCompleted = 1 THEN TRUE ELSE FALSE END,
    pl.DateCreated::TIMESTAMPTZ,
    COALESCE(pl.DateUpdated::TIMESTAMPTZ, pl.DateCreated::TIMESTAMPTZ)
FROM PayLater pl
JOIN Payment p ON p.PaymentId = pl.PaymentId
JOIN Invoice i ON i.InvoiceId = pl.InvoiceId;
```

**Challenges:**
- Need to generate UUIDs for existing PayLater records
- Need to map integer PaymentId/InvoiceId to new UUIDs
- Date format conversion (TEXT → TIMESTAMPTZ)
- Boolean conversion (INTEGER → BOOLEAN)

---

## Cloud Sync Considerations

### Outbox Events

When PayLater payment is updated:

```csharp
public class PayLaterPaymentUpdated
{
    public Guid PayLaterPaymentId { get; set; }
    public Guid InvoiceId { get; set; }
    public decimal PaidAmount { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime UpdatedUtc { get; set; }
}
```

**Sync to Cloud:**
- PayLater records should sync to cloud for reporting
- Outstanding balance visible across all stores
- Cash flow tracking includes PayLater receivables

**Cloud Dashboard Should Show:**
- Total outstanding PayLater across all stores
- Top debtors (by outstanding amount)
- PayLater collection rate (paid vs owed)
- Days outstanding average

---

## Business Rules

1. **No Customer Entity Required**
   - Store doesn't maintain formal customer database
   - Customer identified by description (name) only
   - Search and matching done by string match

2. **Partial Payments Allowed**
   - Customer can pay any amount (not necessarily full balance)
   - Track cumulative `paid_amount`
   - Mark complete when `paid_amount >= pay_later_amount`

3. **No Due Dates**
   - No formal due date tracking
   - Trust-based system
   - Store tracks by customer name and manual follow-up

4. **No Interest Charges**
   - Simple credit system
   - No late fees or interest
   - Community-based trust

5. **EXCLUSIVE Payment Type (CRITICAL)**
   - If an invoice has PayLater payment, it CANNOT have other payment types combined
   - PayLater must be the ONLY payment method on that invoice
   - Rationale: The entire invoice amount becomes a credit; partial cash + partial credit is not allowed
   - This simplifies tracking and ensures clear debt calculation
   - Validation must enforce this rule when adding payments to an invoice

---

## Key Queries

### Get Outstanding Balance
```sql
SELECT
    id,
    description,
    invoice_id,
    pay_later_amount,
    paid_amount,
    (pay_later_amount - paid_amount) as outstanding,
    created_utc
FROM pay_later
WHERE is_completed = FALSE
ORDER BY created_utc ASC;
```

### Search by Customer Name
```sql
SELECT *
FROM pay_later
WHERE description ILIKE '%ชื่อลูกค้า%'
ORDER BY created_utc DESC;
```

### Total Receivable (All Stores)
```sql
SELECT
    i.store_id,
    SUM(pl.pay_later_amount - pl.paid_amount) as total_outstanding,
    COUNT(*) as incomplete_count
FROM pay_later pl
JOIN invoice i ON i.id = pl.invoice_id
WHERE pl.is_completed = FALSE
GROUP BY i.store_id;
```

---

## Testing Considerations

### Unit Tests Should Cover:
- Creating PayLater record
- Updating paid amount
- Marking as complete
- Calculating outstanding balance
- Search by description

### Integration Tests Should Cover:
- Complete workflow: Sale → PayLater → Partial Payment → Complete
- Multiple partial payments
- Search functionality
- Sync to cloud

### Edge Cases:
- Overpayment (paid_amount > pay_later_amount)
- Multiple PayLater records for same customer (different invoices)
- PayLater payment without corresponding Payment record

---

**Status:** ESSENTIAL FEATURE - Must be included in PostgreSQL schema
**Migration:** Required as part of Epic H (SQLite → PostgreSQL migration)
**UI:** Existing panels will be ported to new StoreHub

