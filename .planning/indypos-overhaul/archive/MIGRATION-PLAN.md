# SQLite to PostgreSQL Migration Plan

Version: 1.0.0
Date: 2026-03-03
Status: Draft

## Overview

This document outlines the complete strategy for migrating existing store data from SQLite to PostgreSQL as part of the StoreHub architecture rollout.

## Migration Strategy: Two Phases

```
Phase 1: Fresh PostgreSQL Setup (New StoreHub)
  → New stores or stores without critical historical data
  → Fresh start with clean schema

Phase 2: Data Migration (Existing Stores)
  → Stores with existing SQLite data to preserve
  → One-time migration script
  → Fallback/rollback plan
```

---

## Phase 1: Fresh PostgreSQL Setup

**For:** New stores or stores willing to start fresh

### Steps

1. **Install PostgreSQL** (local Windows Service)
2. **Deploy StoreHub** (Windows Service)
3. **Run EF migrations** (automatic on first start)
4. **Configure StoreId** in appsettings.json
5. **Start selling**

### Pros
- Clean slate, no migration complexity
- Modern schema from day 1
- No data integrity concerns

### Cons
- No historical data
- Not suitable for existing stores with data

---

## Phase 2: Data Migration (SQLite → PostgreSQL)

**For:** Existing stores with data to preserve

### Migration Timeline

```
┌────────────────────────────────────────────────────────────┐
│ T-2 weeks: Preparation                                     │
│  • Test migration with production backup                   │
│  • Verify data integrity                                   │
│  • Train staff on new system                               │
└────────────────────────────────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────────────────────────┐
│ T-1 week: Staging Test                                     │
│  • Run full migration on staging environment              │
│  • Validate all queries work                              │
│  • Performance testing                                     │
└────────────────────────────────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────────────────────────┐
│ T-0 (Migration Day): Execute                               │
│  • Close of business (low traffic)                         │
│  • Run migration script                                    │
│  • Validate data                                           │
│  • Deploy StoreHub                                         │
│  • Smoke test sales                                        │
└────────────────────────────────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────────────────────────┐
│ T+1 day: Monitor                                           │
│  • On-site support                                         │
│  • Monitor for issues                                      │
│  • Keep SQLite backup for 1 week                           │
└────────────────────────────────────────────────────────────┘
```

---

## Migration Script Design

### Tool: Custom C# Migration Utility

**Location:** `src/IndyPOS.MigrationTool/`

**Purpose:**
- Read SQLite data
- Transform to new schema
- Write to PostgreSQL
- Generate PublicIds
- Validate integrity

### High-Level Flow

```
┌─────────────────────────────────────────────────────────────┐
│ 1. Pre-Migration Checks                                     │
│    • Verify SQLite file exists and is readable             │
│    • Verify PostgreSQL is running                          │
│    • Check disk space                                      │
│    • Backup SQLite database                                │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│ 2. Schema Creation                                          │
│    • Create PostgreSQL schema via EF migrations            │
│    • Verify all tables created                             │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│ 3. Data Migration (in order of dependencies)               │
│    A. Reference Data                                       │
│       • ProductCategory → product (master)                  │
│       • User (if needed)                                   │
│                                                            │
│    B. Products                                             │
│       • InventoryProduct → product                         │
│       • Generate PublicId for each                         │
│       • Initial stock → inventory_movement                 │
│                                                            │
│    C. Invoices (Transactional Data)                       │
│       • Invoice → invoice                                  │
│       • InvoiceProduct → invoice_line                      │
│       • Payment → payment                                  │
│       • Generate movements for historical sales            │
│                                                            │
│    D. Financial Records                                    │
│       • PayLater → keep as-is (future work)                │
│       • Installments → keep as-is (future work)            │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│ 4. Data Validation                                          │
│    • Row counts match (SQLite vs PostgreSQL)               │
│    • Totals match (invoice totals, payment totals)         │
│    • Stock levels calculated correctly                     │
│    • All PublicIds generated                               │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│ 5. Post-Migration Report                                    │
│    • Print summary (rows migrated, errors)                 │
│    • Save detailed log file                                │
│    • Success/Failure status                                │
└─────────────────────────────────────────────────────────────┘
```

---

## Data Transformation Rules

### 1. Products (InventoryProduct → product + inventory_movement)

```csharp
// Read from SQLite
var sqliteProducts = ReadSqlite<InventoryProduct>("SELECT * FROM InventoryProduct");

foreach (var sp in sqliteProducts)
{
    // Transform to PostgreSQL product
    var product = new Product
    {
        Id = sp.InventoryProductId,  // Keep for backward compat
        PublicId = Guid.NewGuid(),    // NEW
        Code = sp.Barcode,            // Use barcode as code initially
        Barcode = sp.Barcode,
        Name = sp.Description,
        Category = GetCategoryName(sp.Category),  // Convert int to string
        UnitPrice = sp.UnitPrice,
        CostPrice = null,             // Not in SQLite
        IsActive = true,              // Default
        CreatedUtc = ParseDateTime(sp.DateCreated),
        LastModifiedUtc = ParseDateTime(sp.DateUpdated ?? sp.DateCreated)
    };

    InsertPostgres(product);

    // Create initial inventory movement for current stock
    if (sp.QuantityInStock > 0 && sp.IsTrackable)
    {
        var movement = new InventoryMovement
        {
            PublicId = Guid.NewGuid(),
            StoreId = storeId,
            ProductPublicId = product.PublicId,
            QuantityDelta = sp.QuantityInStock,  // Positive initial stock
            Reason = "InitialStock",
            CreatedUtc = product.CreatedUtc
        };

        InsertPostgres(movement);
    }
}
```

### 2. Invoices (Invoice → invoice)

```csharp
var sqliteInvoices = ReadSqlite<Invoice>("SELECT * FROM Invoice");

foreach (var si in sqliteInvoices)
{
    var invoice = new Invoice
    {
        Id = si.InvoiceId,            // Keep for backward compat
        PublicId = Guid.NewGuid(),    // NEW
        StoreId = storeId,            // NEW
        InvoiceNumber = $"INV-{si.InvoiceId:D6}",  // Generate
        UserId = si.UserId,
        TotalAmount = si.Total,       // Tax-inclusive final amount
        Status = "Completed",         // Assume all legacy are completed
        CreatedUtc = ParseDateTime(si.DateCreated),
        LastModifiedUtc = ParseDateTime(si.DateCreated)
    };

    InsertPostgres(invoice);
}

// Note: Removed customer_id, subtotal, discount, tax (not used)
```

### 3. Invoice Lines (InvoiceProduct → invoice_line)

```csharp
var sqliteLines = ReadSqlite<InvoiceProduct>("SELECT * FROM InvoiceProduct");

foreach (var sl in sqliteLines)
{
    // Look up product PublicId from migration map
    var productPublicId = GetMigratedProductPublicId(sl.InventoryProductId);
    var invoicePublicId = GetMigratedInvoicePublicId(sl.InvoiceId);

    var line = new InvoiceLine
    {
        Id = sl.InvoiceProductId,
        PublicId = Guid.NewGuid(),
        InvoiceId = sl.InvoiceId,     // Internal FK still works
        ProductPublicId = productPublicId,
        ProductName = sl.Description,  // Snapshot
        Quantity = sl.Quantity,
        UnitPrice = sl.IsGroupProduct ? sl.GroupPrice : sl.UnitPrice,  // Use actual price paid
        LineTotal = sl.IsGroupProduct ? sl.GroupPrice : (sl.UnitPrice * sl.Quantity),
        CreatedUtc = ParseDateTime(sl.DateCreated)
    };

    InsertPostgres(line);

// Note: Group pricing handled by setting unit_price to GroupPrice value when applicable

    // Create inventory movement (negative for sales)
    if (sl.IsTrackable)
    {
        var movement = new InventoryMovement
        {
            PublicId = Guid.NewGuid(),
            StoreId = storeId,
            ProductPublicId = productPublicId,
            QuantityDelta = -sl.Quantity,  // Negative (sold)
            Reason = "Sale",
            ReferencePublicId = invoicePublicId,  // Link to invoice
            CreatedUtc = line.CreatedUtc
        };

        InsertPostgres(movement);
    }
}
```

### 4. Payments (Payment → payment)

```csharp
var sqlitePayments = ReadSqlite<Payment>("SELECT * FROM Payment");

// First, get payment type mapping
var paymentTypes = ReadSqlite<PaymentType>("SELECT * FROM PaymentType");

foreach (var sp in sqlitePayments)
{
    var paymentMethod = GetPaymentMethod(sp.PaymentTypeId, paymentTypes);

    var payment = new Payment
    {
        Id = sp.PaymentId,
        PublicId = Guid.NewGuid(),
        InvoiceId = sp.InvoiceId,
        Method = paymentMethod,  // Convert from ID to string
        Amount = sp.Amount,
        Reference = sp.Note,
        CreatedUtc = ParseDateTime(sp.DateCreated)
    };

    InsertPostgres(payment);
}

string GetPaymentMethod(int paymentTypeId, List<PaymentType> types)
{
    var type = types.FirstOrDefault(t => t.Id == paymentTypeId);
    return type?.Name ?? "Cash";  // Default to Cash
}
```

### 5. Customers - SKIP ❌

```csharp
// ⚠️ Decision: DO NOT MIGRATE Customers table
// Reason:
//   1. Table is empty (0 rows in production)
//   2. CustomerId is always null in Invoice
//   3. No customer functionality in current system
//   4. Will be removed from codebase (Epic B)

// Action: Skip migration entirely
```

---

## Validation Queries

After migration, run these checks:

```sql
-- 1. Row count validation
SELECT 'Products' as TableName,
       (SELECT COUNT(*) FROM sqlite.InventoryProduct) as SQLite,
       (SELECT COUNT(*) FROM product) as PostgreSQL;

SELECT 'Invoices' as TableName,
       (SELECT COUNT(*) FROM sqlite.Invoice) as SQLite,
       (SELECT COUNT(*) FROM invoice) as PostgreSQL;

-- 2. Total amounts match
SELECT 'Invoice Totals' as Check,
       (SELECT SUM(Total) FROM sqlite.Invoice) as SQLite,
       (SELECT SUM(total_amount) FROM invoice) as PostgreSQL;

-- 3. Stock levels calculated correctly
SELECT
    p.name,
    SUM(im.quantity_delta) as calculated_stock
FROM inventory_movement im
JOIN product p ON p.public_id = im.product_public_id
GROUP BY p.public_id, p.name
ORDER BY p.name;

-- Compare with original:
-- SELECT Description, QuantityInStock FROM InventoryProduct

-- 4. All PublicIds generated
SELECT 'Products without PublicId' as Issue, COUNT(*)
FROM product WHERE public_id IS NULL;

SELECT 'Invoices without PublicId' as Issue, COUNT(*)
FROM invoice WHERE public_id IS NULL;

-- 5. Movement balance (should be current stock)
SELECT
    p.name,
    SUM(CASE WHEN im.reason = 'InitialStock' THEN im.quantity_delta ELSE 0 END) as initial,
    SUM(CASE WHEN im.reason = 'Sale' THEN im.quantity_delta ELSE 0 END) as sold,
    SUM(im.quantity_delta) as current_stock
FROM inventory_movement im
JOIN product p ON p.public_id = im.product_public_id
GROUP BY p.public_id, p.name;
```

---

## Rollback Plan

If migration fails or critical issues arise:

### Immediate Rollback (T+0 to T+1 day)

```
1. Stop StoreHub service
2. Restore SQLite backup
3. Restart old Windows.Forms app
4. Investigate issue
5. Fix and retry migration later
```

### Data Loss Window

- **Migration Day:** No data loss (migration runs after close of business)
- **T+1 Day:** If rolled back, lose 1 day of sales
  - **Mitigation:** Keep StoreHub running, manually export sales as backup

---

## Testing Strategy

### Test Migration with Production Backup

```
1. Copy Store.db from production → test environment
2. Run migration script
3. Validate all checks pass
4. Test StoreHub with migrated data:
   • Complete a sale
   • Check stock levels
   • View historical invoices
5. Performance test:
   • Query response times
   • Sync to cloud
```

### Test Environments

| Environment | Purpose | Database |
|-------------|---------|----------|
| Dev | Development | Empty PostgreSQL |
| Staging | Pre-migration test | Migrated from PROD backup |
| Production | Live store | Real data |

---

## Migration Tool Implementation

### Project Structure

```
src/IndyPOS.MigrationTool/
├── Program.cs                      # Entry point
├── MigrationOrchestrator.cs        # Main flow
├── Readers/
│   └── SqliteReader.cs             # Read from SQLite
├── Writers/
│   └── PostgresWriter.cs           # Write to PostgreSQL
├── Transformers/
│   ├── ProductTransformer.cs
│   ├── InvoiceTransformer.cs
│   ├── PaymentTransformer.cs
│   └── MovementGenerator.cs
├── Validators/
│   └── DataValidator.cs
└── appsettings.json                # Connection strings
```

### Usage

```bash
# Dry run (validation only)
dotnet run --project src/IndyPOS.MigrationTool -- --dry-run

# Full migration
dotnet run --project src/IndyPOS.MigrationTool -- --store-id STORE-001

# With custom paths
dotnet run --project src/IndyPOS.MigrationTool -- \
  --store-id STORE-001 \
  --sqlite-path "C:\ProgramData\IndyPOS\db\Store.db" \
  --pg-connection "Host=localhost;Database=indypos_storehub;..."
```

---

## Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Data loss during migration | Low | Critical | Backup before migration, test with prod backup |
| Stock calculation mismatch | Medium | High | Extensive validation queries, manual spot checks |
| Performance issues | Low | Medium | Test with real data volume, add indexes |
| Migration takes too long | Low | Medium | Run after close of business, optimize batch size |
| Rollback needed | Low | High | Keep SQLite backup for 1 week |

---

## Timeline for Migration Tool Development

```
Epic D (Schema Design):        Weeks 1-2  (defines target schema)
Migration Tool Development:    Weeks 3-4  (parallel with Epic C)
Testing with Prod Backup:      Week 5     (before Epic H)
Production Migration:          Week 10    (Epic H - Rollout)
```

**Note:** Migration tool should be ready BEFORE pilot rollout (Epic H)

---

## Customer/CustomerId Cleanup Plan

As part of Epic B (Remove Deprecated Features), we will:

1. **Remove from Code:**
   - Delete `CustomerId` property from `Invoice` entity
   - Delete `CustomerId` property from `CreateInvoiceCommand`
   - Delete `CustomerId` property from `InvoiceDto`
   - Remove line 434 in `SaleService.cs` (CustomerId = null)
   - Remove from any extension methods

2. **Remove from Database:**
   - SQLite: No change needed (column doesn't exist in actual DB)
   - PostgreSQL: Don't add `customer_id` column at all
   - Customers table: Don't create in PostgreSQL

3. **Update Documentation:**
   - Remove Customer from all schema diagrams
   - Update APPENDIX-current-sqlite-schema.md
   - Remove Customer from target schema in 04-database-schema.md

**PR:** Can be part of Epic B or separate small PR

---

## Next Steps

1. **Epic B:** Clean up deprecated features (including Customer)
2. **Epic D:** Finalize PostgreSQL schema (without Customer)
3. **Week 3-4:** Develop migration tool
4. **Week 5:** Test migration with production backup
5. **Week 10:** Execute production migration

---

**Related Documents:**
- Schema Analysis: `diagrams/APPENDIX-current-sqlite-schema.md`
- Target Schema: `diagrams/04-database-schema.md`
- Implementation Status: `.claude/implementation-status.md`
