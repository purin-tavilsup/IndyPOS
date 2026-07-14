# Database Migration Strategy

Version: 1.3.0
Updated: 2026-02-28

## EF Core migrations

### Create migration
```bash
dotnet ef migrations add AddOutbox --project src/IndyPOS.StoreHub --startup-project src/IndyPOS.StoreHub
```

### Apply migration
```bash
dotnet ef database update --project src/IndyPOS.StoreHub --startup-project src/IndyPOS.StoreHub
```
