# Decision Record: Simplify PostgreSQL Schema (Remove Tax, Discount, Loyalty)

**Date:** 2026-03-03
**Status:** Approved
**Decision Makers:** Pond (Lead Engineer)

## Context

During schema review, Pond identified that the proposed PostgreSQL schema included fields (tax, discount, loyalty_points) that are not used in the current system and won't be needed in the new system.

## Investigation Findings

### Current SQLite Schema

```sql
-- Invoice table (actual schema)
InvoiceId            INTEGER         PK
UserId               INTEGER         NOT NULL
DateCreated          TEXT            DEFAULT CURRENT_TIMESTAMP
Total                NUMERIC         NOT NULL

-- NO discount field
-- NO tax field
-- NO subtotal field
```

### Code Analysis

**Search Results:**
- `grep -r "discount" src/` → **No files found**
- `grep -r "tax" src/` → **No files found**
- `grep -r "loyalty" src/` → **No files found**

**Conclusion:** Current system does NOT use discount, tax, or loyalty concepts at all.

### Thai Market Context (Provided by Pond)

**Tax in Thailand:**
- All retail prices are **tax-inclusive** (VAT already included in price)
- No need to calculate or track tax separately
- Invoice total is the final amount customer pays

## Decision

**REMOVE the following fields from PostgreSQL schema:**

### From `invoice` table:
- ❌ `subtotal` - Not needed (just use `total_amount`)
- ❌ `discount` - Not used
- ❌ `tax` - Not needed (prices are tax-inclusive in Thailand)

### From `invoice_line` table:
- ❌ `discount` - Not used

### From `customer` table (already removed):
- ❌ `loyalty_points` - Already handled (customer table removed)

## Simplified Schema

### invoice (StoreHub & Cloud)

```sql
CREATE TABLE invoice (
  id BIGSERIAL PRIMARY KEY,
  public_id UUID UNIQUE NOT NULL,
  store_id VARCHAR(50) NOT NULL,
  invoice_number VARCHAR(50) NOT NULL,
  user_id BIGINT NOT NULL,
  total_amount NUMERIC(18,2) NOT NULL,  -- Final amount (tax-inclusive)
  status VARCHAR(20) NOT NULL,
  created_utc TIMESTAMPTZ NOT NULL,
  last_modified_utc TIMESTAMPTZ NOT NULL
);
```

**Fields removed:** subtotal, discount, tax, customer_id

### invoice_line (StoreHub & Cloud)

```sql
CREATE TABLE invoice_line (
  id BIGSERIAL PRIMARY KEY,
  public_id UUID UNIQUE NOT NULL,
  invoice_id BIGINT NOT NULL,
  product_public_id UUID NOT NULL,
  product_name VARCHAR(200) NOT NULL,
  quantity INT NOT NULL,
  unit_price NUMERIC(18,2) NOT NULL,
  line_total NUMERIC(18,2) NOT NULL,  -- unit_price * quantity (or group price)
  created_utc TIMESTAMPTZ NOT NULL
);
```

**Fields removed:** discount

## Rationale

### 1. Tax Not Needed
- **Thai market:** All prices are tax-inclusive
- **No calculation required:** Price on shelf = price customer pays
- **Simpler accounting:** Total is already the final amount

### 2. Discount Not Needed
- **Current system:** No discount tracking
- **Group pricing:** Handled at unit_price level (group price becomes the unit price)
- **Simpler model:** Avoid premature complexity

### 3. YAGNI Principle
- "You Aren't Gonna Need It"
- Don't build features that aren't required
- Can add later if requirements emerge

### 4. Backward Compatible
- Matches current SQLite schema closely
- Migration is simpler (fewer fields to map)
- Existing logic doesn't need to change

## Impact

### Epic D (Schema Design)
- ✅ **Simpler schema** - Fewer fields to design and validate
- ✅ **Easier migration** - Fewer transformations needed
- ✅ **Less code** - Fewer properties in entities

### Migration (Epic H)
```csharp
// BEFORE (complex)
var invoice = new Invoice {
    Subtotal = calculateSubtotal(),
    Discount = calculateDiscount(),
    Tax = calculateTax(),
    TotalAmount = subtotal - discount + tax
};

// AFTER (simple)
var invoice = new Invoice {
    TotalAmount = calculateTotal()  // Done!
};
```

### Code Simplification
- No discount calculation logic
- No tax calculation logic
- No subtotal tracking
- **Just Total** (like current system)

## Comparison

| Field | Current SQLite | Proposed (Original) | **Approved (Simplified)** |
|-------|----------------|---------------------|---------------------------|
| total | ✅ Total | ✅ total_amount | ✅ total_amount |
| subtotal | ❌ | ❌ subtotal | ❌ **REMOVED** |
| discount | ❌ | ❌ discount | ❌ **REMOVED** |
| tax | ❌ | ❌ tax | ❌ **REMOVED** |
| customer_id | ❌ | ❌ customer_id | ❌ **REMOVED** |

**Result:** Perfect alignment with current system + distributed IDs

## Future Considerations

### If Discount is Needed Later:

**Option A:** Add `discount_amount` to invoice
```sql
ALTER TABLE invoice ADD COLUMN discount_amount NUMERIC(18,2) DEFAULT 0;
```

**Option B:** Line-level discount
```sql
ALTER TABLE invoice_line ADD COLUMN discount NUMERIC(18,2) DEFAULT 0;
-- line_total = (unit_price * quantity) - discount
```

**Recommendation:** Line-level is more flexible

### If Tax Tracking is Needed Later:

**Thailand scenario:** Unlikely, but if required for reporting:
```sql
ALTER TABLE invoice ADD COLUMN tax_rate NUMERIC(5,2);  -- e.g., 7.00 for 7% VAT
ALTER TABLE invoice ADD COLUMN tax_included NUMERIC(18,2);
-- Calculation: tax_included = total_amount * (tax_rate / (100 + tax_rate))
```

## Group Pricing Handling

Current system uses `GroupPrice` and `IsGroupProduct` flag. In new schema:

**Approach:** Store the actual price paid in `unit_price`

```csharp
// If group price applies
line.UnitPrice = product.GroupPrice;  // e.g., 90 baht (was 100)
line.Quantity = groupQuantity;        // e.g., 10
line.LineTotal = line.UnitPrice * line.Quantity;  // 900 baht

// Regular price
line.UnitPrice = product.UnitPrice;   // e.g., 100 baht
line.Quantity = quantity;             // e.g., 1
line.LineTotal = line.UnitPrice * line.Quantity;  // 100 baht
```

**No need for:** `IsGroupProduct` flag, separate `GroupPrice` field, or `discount` field

## Documentation Updates Required

- ✅ Update `04-database-schema.md` - Remove discount, tax, subtotal
- ✅ Update `APPENDIX-current-sqlite-schema.md` - Simplify mapping
- ✅ Update `MIGRATION-PLAN.md` - Simpler transformations
- ✅ Update `02-data-flow.md` - Remove discount/tax from examples

## Verification

After Epic D completion, verify:
- [ ] Schema has NO discount fields
- [ ] Schema has NO tax fields
- [ ] Schema has NO subtotal field
- [ ] Migration plan reflects simplified schema
- [ ] All diagrams updated

## References

- Current Schema: SQLite analysis in `Store.db`
- Code Search: No discount/tax usage found
- Market Context: Thai retail (tax-inclusive pricing)

---

**Approved by:** Pond (Lead Engineer)
**Implementation:** Epic D (Schema Design)
