# PostgreSQL Schema (StoreHub)

Version: 1.1.0  
Updated: 2026-02-28

## Identifier strategy (backward compatible)
- Keep integer PKs if needed for legacy imports.
- Add `PublicId` UUID UNIQUE NOT NULL.
- New APIs and sync use `PublicId`.

## Example: Invoice tables (minimal)

```sql
CREATE TABLE invoice (
  id bigserial PRIMARY KEY,
  public_id uuid NOT NULL UNIQUE,
  store_id varchar(50) NOT NULL,
  total_amount numeric(18,2) NOT NULL,
  created_utc timestamptz NOT NULL,
  last_modified_utc timestamptz NOT NULL
);

CREATE TABLE invoice_line (
  id bigserial PRIMARY KEY,
  public_id uuid NOT NULL UNIQUE,
  invoice_public_id uuid NOT NULL,
  product_public_id uuid NOT NULL,
  quantity numeric(18,3) NOT NULL,
  price numeric(18,2) NOT NULL,
  created_utc timestamptz NOT NULL
);
```

## Example: inventory movements (recommended)

```sql
CREATE TABLE inventory_movement (
  public_id uuid PRIMARY KEY,
  store_id varchar(50) NOT NULL,
  product_public_id uuid NOT NULL,
  quantity_delta numeric(18,3) NOT NULL,
  reason varchar(50) NOT NULL,
  reference_public_id uuid NULL,
  created_utc timestamptz NOT NULL
);
```
