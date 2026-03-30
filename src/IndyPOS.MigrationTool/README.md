# IndyPOS Migration Tool

CLI tool to migrate data from legacy SQLite database to StoreHub PostgreSQL.

## Quick Start

```powershell
# 1. Dry run (validate without making changes)
.\IndyPOS.MigrationTool.exe `
  --sqlite "C:\ProgramData\IndyPOS\db\Store.db" `
  --postgres "Host=127.0.0.1;Database=indypos_storehub;Username=indypos_app;Password=secret" `
  --store-id "STORE-001" `
  --dry-run

# 2. Execute migration
.\IndyPOS.MigrationTool.exe `
  --sqlite "C:\ProgramData\IndyPOS\db\Store.db" `
  --postgres "Host=127.0.0.1;Database=indypos_storehub;Username=indypos_app;Password=secret" `
  --store-id "STORE-001"

# 3. Verify migration
.\IndyPOS.MigrationTool.exe verify `
  --sqlite "C:\ProgramData\IndyPOS\db\Store.db" `
  --postgres "Host=127.0.0.1;Database=indypos_storehub;Username=indypos_app;Password=secret" `
  --store-id "STORE-001"
```

## Commands

### Default Command (Migrate)

Migrates all data from SQLite to PostgreSQL.

```
IndyPOS.MigrationTool.exe [options]
```

**Required Options:**

| Option | Short | Description |
|--------|-------|-------------|
| `--sqlite` | `-s` | Path to SQLite database file (Store.db) |
| `--postgres` | `-p` | PostgreSQL connection string |
| `--store-id` | `-i` | Store identifier (e.g., STORE-001) |

**Optional Options:**

| Option | Short | Description |
|--------|-------|-------------|
| `--dry-run` | `-d` | Validate without making changes |
| `--verbose` | `-v` | Show detailed output |
| `--cloud-api` | `-c` | Cloud API URL for syncing |
| `--client-id` | | OAuth2 Client ID for Cloud API |
| `--client-secret` | | OAuth2 Client Secret for Cloud API |

### Verify Command

Compares SQLite and PostgreSQL data to verify migration success.

```
IndyPOS.MigrationTool.exe verify [options]
```

**Required Options:**

| Option | Short | Description |
|--------|-------|-------------|
| `--sqlite` | `-s` | Path to SQLite database file |
| `--postgres` | `-p` | PostgreSQL connection string |
| `--store-id` | `-i` | Store identifier |

## What Gets Migrated

| Entity | Source Table | Target Table | Notes |
|--------|--------------|--------------|-------|
| Users | `User` + `UserCredential` | `StoreUsers` | Password hash preserved (upgraded on first login) |
| Products | `InventoryProduct` | `Products` | New Guid IDs generated |
| Invoices | `Invoice` | `Invoices` | With all lines and payments |
| Invoice Lines | `InvoiceProduct` | `InvoiceLines` | ProductId mapped to new Guid |
| Payments | `Payment` | `Payments` | Payment type mapped to string |
| PayLater | `PayLater` | `PayLaters` | Links to invoice preserved |
| Inventory | `InventoryProduct.QuantityInStock` | `InventoryMovements` | Initial stock as movement |

## ID Mapping

The tool generates new Guid IDs for all entities and maintains mapping tables:

```
SQLite (int)  →  PostgreSQL (Guid)
─────────────────────────────────────
Product 1     →  a1b2c3d4-e5f6-...
Product 2     →  b2c3d4e5-f6a7-...
User 1        →  c3d4e5f6-a7b8-...
Invoice 1     →  d4e5f6a7-b8c9-...
```

These mappings are used to preserve relationships (e.g., InvoiceLines → Products).

## Example Output

### Migration

```
 ___           _       ____   ___  ____
|_ _|_ __   __| |_   _|  _ \ / _ \/ ___|
 | || '_ \ / _` | | | | |_) | | | \___ \
 | || | | | (_| | |_| |  __/| |_| |___) |
|___|_| |_|\__,_|\__, |_|    \___/|____/
                 |___/
Migration Tool - SQLite to StoreHub PostgreSQL

┌──────────────┬─────────────────────────────────────────────┐
│ Setting      │ Value                                       │
├──────────────┼─────────────────────────────────────────────┤
│ SQLite File  │ C:\ProgramData\IndyPOS\db\Store.db         │
│ PostgreSQL   │ Host=127.0.0.1;Database=indypos_storehub...│
│ Store ID     │ STORE-001                                   │
│ Cloud API    │ (not configured)                            │
│ Dry Run      │ No                                          │
└──────────────┴─────────────────────────────────────────────┘

┌───────────┬──────────┬─────────┬────────┐
│ Entity    │ Migrated │ Skipped │ Failed │
├───────────┼──────────┼─────────┼────────┤
│ Users     │        5 │       0 │      0 │
│ Products  │      150 │       0 │      0 │
│ Invoices  │    1,234 │       0 │      0 │
│ Payments  │    1,500 │       0 │      0 │
│ PayLater  │       12 │       0 │      0 │
└───────────┴──────────┴─────────┴────────┘

✓ Migration completed successfully!
```

### Verification

```
┌───────────────┬─────────┬────────────┬────────┐
│ Entity        │ SQLite  │ PostgreSQL │ Status │
├───────────────┼─────────┼────────────┼────────┤
│ Users         │       5 │          5 │   ✓    │
│ Products      │     150 │        150 │   ✓    │
│ Invoices      │   1,234 │      1,234 │   ✓    │
│ Invoice Lines │   3,500 │      3,500 │   ✓    │
│ Payments      │   1,500 │      1,500 │   ✓    │
│ PayLater      │      12 │         12 │   ✓    │
│ Total Revenue │ 125,000 │    125,000 │   ✓    │
└───────────────┴─────────┴────────────┴────────┘

✓ Migration verification passed!
```

## Cloud Sync (Optional)

After migrating to local PostgreSQL, you can sync data to the cloud:

```powershell
.\IndyPOS.MigrationTool.exe `
  --sqlite "C:\ProgramData\IndyPOS\db\Store.db" `
  --postgres "Host=127.0.0.1;Database=indypos_storehub;..." `
  --store-id "STORE-001" `
  --cloud-api "https://cloud.indypos.app" `
  --client-id "store-001-client" `
  --client-secret "your-secret-here"
```

The tool will:
1. Migrate SQLite → PostgreSQL (local)
2. Authenticate with Cloud API (OAuth2)
3. POST `/sync/bulk-migration` with all data
4. Report sync results

## Troubleshooting

### "SQLite file not found"

Ensure the path to Store.db is correct. Default location:
```
C:\ProgramData\IndyPOS\db\Store.db
```

### "Cannot connect to PostgreSQL"

1. Verify PostgreSQL is running: `Get-Service postgresql*`
2. Check connection string format
3. Ensure user has permissions on the database

### "User X has no credentials, skipping"

Legacy users without UserCredential records are skipped. These users cannot log in.

### Migration failed mid-way

The migration uses transactions. If it fails:
1. Check error messages in output
2. Fix the issue
3. Re-run migration (existing records are skipped)

### Verification shows mismatched counts

This can happen if:
- Migration was interrupted
- Some records were skipped due to errors
- New data was added to SQLite after migration

Re-run migration to sync remaining records.

## Prerequisites

- .NET 10 Runtime
- PostgreSQL 16 with StoreHub database created
- SQLite database file (Store.db)
- Store registered in system (for cloud sync)

## Building

```powershell
cd src/IndyPOS.MigrationTool
dotnet build
dotnet publish -c Release -o ./publish
```

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Error (check output for details) |
