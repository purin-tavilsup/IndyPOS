# GUID as Primary Key Strategy

**Date:** 2026-03-03
**Status:** Approved
**Decision Maker:** Pond (Lead Engineer)

## Core Decision

**GUID (id) will REPLACE integer IDs as the primary key for all entities.**

This is not a supplementary identifier - it's THE primary key.

---

## Rationale

### Why GUID as PK?

1. **Distributed by design** - Multiple stores can create records without coordination
2. **No collision risk** - Each store generates unique IDs independently
3. **Offline-first ready** - Don't need central ID server
4. **Cloud-native** - Natural fit for distributed systems
5. **Future-proof** - Works for any number of stores/systems

### Why NOT keep int PK?

- **Legacy burden** - Maintaining two ID systems is complex
- **FK complexity** - All foreign keys need both int and GUID
- **Confusion** - Which ID to use where?
- **Not needed** - Modern databases handle UUIDs well

---

## Migration Approach: Hybrid (Pragmatic)

We'll use a **phased approach** to minimize disruption:

### Phase 1: PostgreSQL Schema (NEW) - GUID Primary
**Timeline:** Epic D (Schema Design)

New PostgreSQL schema uses GUID as primary key from day 1:

```sql
CREATE TABLE invoice (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  store_id VARCHAR(50) NOT NULL,
  invoice_number VARCHAR(50) NOT NULL,
  user_id BIGINT NOT NULL,
  total_amount NUMERIC(18,2) NOT NULL,
  status VARCHAR(20) NOT NULL,
  created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE invoice_line (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  invoice_id UUID NOT NULL REFERENCES invoice(id),
  product_id UUID NOT NULL REFERENCES product(id),
  quantity INT NOT NULL,
  unit_price NUMERIC(18,2) NOT NULL,
  line_total NUMERIC(18,2) NOT NULL,
  created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

**Key points:**
- `id` is UUID (not integer)
- `id` is the PRIMARY KEY
- Foreign keys use `_id` suffix (e.g., `invoice_id`, `product_id`)
- Sequential UUID generation for performance (see below)

### Phase 2: SQLite Migration - Add GUIDs
**Timeline:** Before production rollout (Epic H)

Update existing SQLite data:

```sql
-- Add GUID column
ALTER TABLE Invoice ADD COLUMN PublicId TEXT;

-- Populate with GUIDs
UPDATE Invoice SET PublicId = lower(hex(randomblob(16)));

-- Create unique index
CREATE UNIQUE INDEX idx_invoice_publicid ON Invoice(PublicId);
```

**Keep integer IDs in SQLite** - they work fine locally, no need to change.

### Phase 3: Application Layer - Dual Support
**Timeline:** Epic C-G (StoreHub + Desktop integration)

Application entities support both:

```csharp
// Domain entity (transition period)
public class Invoice
{
    // UUID primary key
    public Guid Id { get; set; } = Guid.NewGuid();

    // Other fields...
}
```

**Repositories abstract the difference:**

```csharp
// SQLite repository (legacy)
public class SqliteInvoiceRepository : IInvoiceRepository
{
    public async Task<Invoice> GetByIdAsync(Guid id)
    {
        // Query by Id in SQLite
        return await _db.QuerySingleAsync<Invoice>(
            "SELECT * FROM Invoice WHERE Id = @id",
            new { id = id.ToString() }
        );
    }
}

// PostgreSQL repository (new)
public class PgInvoiceRepository : IInvoiceRepository
{
    public async Task<Invoice> GetByIdAsync(Guid id)
    {
        // Query by id (PK) in PostgreSQL
        return await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == id);
    }
}
```

### Phase 4: Full Migration - GUID Only
**Timeline:** After all stores migrated to StoreHub

Once all stores are on PostgreSQL:
- Only use `Id` (UUID type)
- SQLite becomes legacy/backup only

---

## Schema Comparison

### OLD Approach (Dual ID)
```sql
CREATE TABLE invoice (
  id BIGSERIAL PRIMARY KEY,           -- Keep for backward compat
  public_id UUID UNIQUE NOT NULL,     -- Add for sync
  -- FKs reference id
  user_id BIGINT NOT NULL,
  -- ...
);

CREATE TABLE invoice_line (
  id BIGSERIAL PRIMARY KEY,
  invoice_id BIGINT NOT NULL,         -- FK to invoice.id (integer)
  product_id BIGINT NOT NULL,         -- FK to product.id (integer)
  -- ...
);
```

**Problems:**
- Two ID systems to maintain
- Which one is "real"?
- Foreign keys use int, but sync uses GUID

### NEW Approach (UUID Primary)
```sql
CREATE TABLE invoice (
  id UUID PRIMARY KEY,                -- THE primary key (UUID type)
  store_id VARCHAR(50) NOT NULL,
  user_id BIGINT NOT NULL,            -- Still reference User by int (for now)
  -- ...
);

CREATE TABLE invoice_line (
  id UUID PRIMARY KEY,
  invoice_id UUID NOT NULL,           -- FK to invoice.id (UUID type)
  product_id UUID NOT NULL,           -- FK to product.id (UUID type)
  -- ...
);
```

**Benefits:**
- Single source of truth
- Natural for distributed systems
- Cleaner, no confusion

---

## Performance Considerations

### UUID Performance Myths

**Myth:** "UUIDs are slower than integers"
**Reality:** Negligible difference for our scale (3 stores, <100k records/year)

**Myth:** "UUIDs cause index fragmentation"
**Reality:** Use sequential UUIDs (UUIDv7 or ordered generation)

### Sequential UUID Generation

PostgreSQL has several options:

**Option 1: gen_random_uuid() (default)**
```sql
id UUID PRIMARY KEY DEFAULT gen_random_uuid()
```
- ✅ Simple
- ⚠️  Random (potential fragmentation)
- 👍 **Good enough for our scale**

**Option 2: UUIDv7 (time-ordered)**
```sql
-- Using extension
CREATE EXTENSION IF NOT EXISTS pg_uuidv7;
id UUID PRIMARY KEY DEFAULT uuid_generate_v7()
```
- ✅ Time-ordered (no fragmentation)
- ✅ Still unique
- 👍 **Best for large scale**

**Option 3: Application-generated (C#)**
```csharp
// Generate in application
public Guid Id { get; set; } = Guid.NewGuid();

// Or use ordered library
using IdGen;
public Guid Id { get; set; } = IdGenerator.CreateSequentialGuid();
```

**Recommendation:** Start with `gen_random_uuid()`, switch to UUIDv7 if needed.

### Index Size

| PK Type | Size per row | Index size (100k rows) |
|---------|--------------|------------------------|
| INTEGER (4 bytes) | 4 bytes | ~400 KB |
| BIGINT (8 bytes) | 8 bytes | ~800 KB |
| UUID (16 bytes) | 16 bytes | ~1.6 MB |

**Impact:** Negligible for our scale (3 stores, <100k invoices/year)

---

## Foreign Key Strategy

### User ID Exception

User table can keep integer PK for now:

```sql
CREATE TABLE "user" (
  id BIGSERIAL PRIMARY KEY,  -- Keep as int (centrally managed)
  username VARCHAR(50) NOT NULL,
  -- ...
);

CREATE TABLE invoice (
  public_id UUID PRIMARY KEY,
  user_id BIGINT NOT NULL,    -- FK to user.id (int)
  -- ...
);
```

**Reason:** Users are centrally managed, not created offline per store.

**Future:** Can add `public_id` to User later if needed.

---

## Migration Data Flow

### SQLite (Current)
```
Invoice:
  InvoiceId = 79 (int)
  ─────────────────────
  InvoiceProduct:
    InvoiceId = 79 (FK)
```

### During Migration
```
Invoice:
  InvoiceId = 79 (int, keep for SQLite)
  Id = "a1b2c3d4-..." (UUID, NEW)
  ─────────────────────────────────────
  InvoiceProduct:
    InvoiceId = 79 (FK, still works in SQLite)
    Id = "e5f6g7h8-..." (UUID, NEW)
    InvoiceId = "a1b2c3d4-..." (FK, NEW - UUID type)
```

### PostgreSQL (New)
```
invoice:
  id = "a1b2c3d4-..." (UUID, PK)
  ───────────────────────────────────────
  invoice_line:
    id = "e5f6g7h8-..." (UUID, PK)
    invoice_id = "a1b2c3d4-..." (UUID, FK)
```

---

## Example Entities

### Invoice Entity (Domain)

```csharp
namespace IndyPOS.Domain.Entities;

public class Invoice
{
    // Primary identifier (UUID)
    public Guid Id { get; set; } = Guid.NewGuid();

    // Multi-store support
    public string StoreId { get; set; } = string.Empty;

    // Human-readable number (can generate: "INV-" + counter)
    public string InvoiceNumber { get; set; } = string.Empty;

    // User who created it
    public int UserId { get; set; }

    // Financial data
    public decimal TotalAmount { get; set; }

    // Audit
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    // Navigation properties (use UUID FKs)
    public List<InvoiceLine> Lines { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
}

public class InvoiceLine
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Foreign keys (UUID)
    public Guid InvoiceId { get; set; }
    public Guid ProductId { get; set; }

    // Snapshot data
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public Invoice? Invoice { get; set; }
}
```

### EF Core Configuration

```csharp
public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoice");

        // UUID as primary key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        // Other properties
        builder.Property(x => x.StoreId)
            .HasColumnName("store_id")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.InvoiceNumber)
            .HasColumnName("invoice_number")
            .HasMaxLength(50)
            .IsRequired();

        // Relationships (UUID FK)
        builder.HasMany(x => x.Lines)
            .WithOne(x => x.Invoice)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

---

## API Contracts (Always use GUID)

```csharp
// API Request/Response - ALWAYS use UUID
public record CompleteSaleRequest
{
    public List<SaleLineDto> Lines { get; init; } = new();
    public List<PaymentDto> Payments { get; init; } = new();
}

public record CompleteSaleResponse
{
    public Guid InvoiceId { get; init; }  // Return UUID
    public string InvoiceNumber { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
}

// Get invoice by ID - use UUID
public record GetInvoiceRequest
{
    public Guid InvoiceId { get; init; }
}
```

**Never expose integer IDs in APIs** - only UUIDs.

---

## Benefits of This Approach

### ✅ Distributed-First
- Stores create records offline without coordination
- No ID collision risk
- Natural fit for multi-store

### ✅ Future-Proof
- Add stores without ID range planning
- Works for unlimited stores
- Cloud-native design

### ✅ Cleaner Code
- Single ID system (no dual ID confusion)
- Foreign keys are clear
- APIs use consistent identifiers

### ✅ Migration Safety
- SQLite can keep integer IDs during transition
- Application layer abstracts the difference
- Gradual migration (not big bang)

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| UUID performance | Low | Use sequential UUIDs if needed |
| Index fragmentation | Low | Our scale is small (~100k records/year) |
| Migration complexity | Medium | Gradual rollout, test thoroughly |
| Developer unfamiliarity | Low | Document patterns, examples |

---

## Action Items

### Epic D (Schema Design)
- [x] Use UUID as primary key in PostgreSQL schema
- [x] Use `id` column (UUID type) as primary key
- [x] Update all foreign keys to use UUID type
- [ ] Configure EF Core for UUID PKs

### Epic H (Migration)
- [ ] Add `Id` (UUID) columns to SQLite
- [ ] Populate UUIDs for existing data
- [ ] Create migration utility that maps int → UUID
- [ ] Test foreign key relationships

### Application Code
- [ ] Update Domain entities (UUID primary key)
- [ ] Update all repositories (query by UUID)
- [ ] Update API contracts (UUID only)
- [ ] Update UI (display ID when needed)

---

## Related Documents

- Schema diagrams: `diagrams/04-database-schema.md` (needs update)
- Migration plan: `MIGRATION-PLAN.md` (needs update)
- Current schema: `diagrams/APPENDIX-current-sqlite-schema.md`

---

**Approved by:** Pond (Lead Engineer)
**Decision:** Use `id` (UUID type) as primary key - cleaner and conventional
