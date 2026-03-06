# Database Migration Strategy

Version: 1.1.0  
Updated: 2026-02-28

## A) EF Core migrations (normal development)

### Create migration
```bash
dotnet ef migrations add AddOutbox --project src/IndyPOS.StoreHub --startup-project src/IndyPOS.StoreHub
```

### Apply migration
```bash
dotnet ef database update --project src/IndyPOS.StoreHub --startup-project src/IndyPOS.StoreHub
```

## B) Production startup migration
StoreHub runs `db.Database.Migrate()` on startup and logs:
- current migration
- pending migrations
- success/failure

## C) Legacy SQLite upgrade (backward compatibility)

If older SQLite DBs exist, run an upgrade routine:
1. Detect missing `PublicId` columns → `ALTER TABLE ... ADD COLUMN PublicId TEXT`
2. Fill null values with GUID strings
3. Continue using legacy int PKs internally, but new APIs use PublicId

### Example: SQLite upgrade snippet (C#)
```csharp
// Pseudocode
if (!ColumnExists("Invoice", "PublicId"))
    Execute("ALTER TABLE Invoice ADD COLUMN PublicId TEXT");

Execute("UPDATE Invoice SET PublicId = @id WHERE PublicId IS NULL", new { id = Guid.NewGuid().ToString() });
```
