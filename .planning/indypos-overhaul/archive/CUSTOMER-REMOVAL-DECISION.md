# Decision Record: Remove Customer/CustomerId

**Date:** 2026-03-03
**Status:** Approved
**Decision Makers:** Pond (Lead Engineer)

## Context

During workspace setup and planning review for the IndyPOS overhaul, we discovered that the Customer/CustomerId concept exists in the codebase but is completely unused.

## Investigation Findings

### Database Analysis (SQLite)

```sql
-- Customers table row count
SELECT COUNT(*) FROM Customers;
-- Result: 0 rows

-- Invoice table structure
PRAGMA table_info(Invoice);
-- Columns: InvoiceId, UserId, DateCreated, Total
-- CustomerId column DOES NOT EXIST
```

### Code Analysis

1. **Invoice Entity** (`src/IndyPOS.Domain/Entities/Invoice.cs:12`)
   ```csharp
   public int? CustomerId { get; set; }  // Exists but unused
   ```

2. **CreateInvoiceCommand** (`src/IndyPOS.Application/UseCases/Invoices/Create/CreateInvoiceCommand.cs:9`)
   ```csharp
   public int? CustomerId { get; set; }  // Exists but unused
   ```

3. **SaleService** (`src/IndyPOS.Infrastructure/Services/SaleService.cs:434`)
   ```csharp
   CustomerId = null  // ALWAYS SET TO NULL
   ```

4. **Customer Entity**
   - Does NOT exist in `IndyPOS.Domain`
   - No `Customer.cs` file found

### Conclusion

- ✅ Customers table exists in database but is empty
- ✅ CustomerId property exists in code but is always null
- ✅ Invoice table doesn't even have a CustomerId column
- ✅ No customer functionality implemented
- ✅ No Customer domain entity exists

## Decision

**REMOVE Customer/CustomerId entirely from the codebase and do not include in new PostgreSQL schema.**

## Rationale

1. **No Active Use:** CustomerId is always null in current system
2. **Database Mismatch:** Property exists in code but not in actual DB schema
3. **Zero Data:** Customers table is empty
4. **Clean Slate:** Better to remove now than maintain dead code
5. **Future Flexibility:** If customer functionality is needed later, design from scratch with proper requirements

## Impact

### Code Changes (Epic B)

Files to modify:
- `src/IndyPOS.Domain/Entities/Invoice.cs` - Remove `CustomerId` property
- `src/IndyPOS.Application/UseCases/Invoices/Create/CreateInvoiceCommand.cs` - Remove `CustomerId` property
- `src/IndyPOS.Application/UseCases/Invoices/InvoiceDto.cs` - Remove `CustomerId` property
- `src/IndyPOS.Application/UseCases/Invoices/InvoiceExtensions.cs` - Remove references
- `src/IndyPOS.Infrastructure/Services/SaleService.cs` - Remove line 434

Estimate: ~10 minutes, 1 small PR

### Schema Changes (Epic D)

PostgreSQL schema:
- **DO NOT** create `customer` table
- **DO NOT** add `customer_id` foreign key to `invoice` table
- **DO** add `user_id` to track which user created the invoice

### Migration Changes

SQLite → PostgreSQL migration:
- **Skip** Customers table migration entirely
- Document decision in migration script
- No data loss (table is empty)

### Documentation Updates

- ✅ Updated `04-database-schema.md` - Removed customer table
- ✅ Updated `APPENDIX-current-sqlite-schema.md` - Documented non-migration
- ✅ Created `MIGRATION-PLAN.md` - Customer cleanup plan
- ✅ Added task B6 to Epic B

## Alternatives Considered

### Alternative 1: Keep CustomerId as nullable
**Rejected:** No value in maintaining dead code

### Alternative 2: Implement customer functionality now
**Rejected:** No requirements, would be premature

### Alternative 3: Keep for future use
**Rejected:** YAGNI principle - design when actually needed

## Future Considerations

If customer functionality is needed later:

1. **Gather requirements first**
   - What customer data do we need?
   - How will it be used?
   - Privacy/GDPR considerations?

2. **Design properly**
   - Create Customer entity in Domain
   - Add customer table with proper fields
   - Link to Invoice via customer_id FK
   - Implement customer management UI

3. **Migration path**
   - Add customer table via EF migration
   - Add customer_id column to invoice (nullable initially)
   - Populate with real data as customers are created

## Verification

After Epic B completion, verify:
- [ ] No references to `CustomerId` in codebase (grep for "CustomerId")
- [ ] No Customer-related files exist
- [ ] Build passes
- [ ] All tests pass
- [ ] Schema diagrams updated

## References

- Investigation: `.claude/sessions/2026-03-03-workspace-setup.md`
- Migration Plan: `.planning/indypos-overhaul/MIGRATION-PLAN.md`
- Implementation Tracker: `.claude/implementation-status.md` (Task B6)

---

**Approved by:** Pond (Lead Engineer)
**Implementation:** Epic B, Task B6
