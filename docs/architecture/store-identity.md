# Store Identity

## Overview

Each store in the IndyPOS system has a unique identity used for:
- **Multi-store operations**: Distinguishing data from different stores
- **Cloud sync**: Attributing transactions to the correct store
- **Reporting**: Aggregating data across stores

## Configuration

Add store identity to `appsettings.json`:

```json
{
  "Store": {
    "Id": "STORE-001",
    "Name": "Bangkok Branch 1",
    "ConfigPath": "C:\\ProgramData\\IndyPOS\\Config\\StoreConfiguration.json"
  }
}
```

### Configuration Options

| Property | Required | Description |
|----------|----------|-------------|
| `Id` | Yes* | Unique store identifier (e.g., "STORE-001") |
| `Name` | No | Display name for the store |
| `ConfigPath` | No | Path to store configuration JSON |

*For cloud sync operations, `Id` is required. During migration, a default ID based on machine name is used.

## Usage

### Accessing Store Identity

```csharp
public class MyService
{
    private readonly IStoreIdentityService _storeIdentity;

    public MyService(IStoreIdentityService storeIdentity)
    {
        _storeIdentity = storeIdentity;
    }

    public void DoSomething()
    {
        var storeId = _storeIdentity.StoreId;
        var storeName = _storeIdentity.StoreName;

        // Use for logging, sync, etc.
    }
}
```

### Ensuring Configuration

For operations that require store identity, call `EnsureConfigured()`:

```csharp
public async Task SyncToCloud()
{
    _storeIdentity.EnsureConfigured(); // Throws if not configured

    // Proceed with sync...
}
```

## Interface

```csharp
public interface IStoreIdentityService
{
    string StoreId { get; }
    string StoreName { get; }
    void EnsureConfigured();
}
```

## Migration Notes

### Existing Installations

Existing single-store installations will continue to work without explicit configuration:
- A default `StoreId` is generated from the machine name
- This allows gradual migration to explicit store IDs

### New Installations

New installations should always configure:
- A unique `Store:Id` in appsettings.json
- This ensures proper identification when cloud sync is enabled

## Best Practices

1. **Use meaningful IDs**: Use a consistent naming scheme (e.g., "STORE-001", "BKK-MAIN")
2. **Keep IDs stable**: Once set, don't change the store ID
3. **Configure early**: Set the store ID before enabling cloud sync
4. **Validate on startup**: Use `EnsureConfigured()` in services that require store identity
