# Example Implementation: Outbox + SyncWorker

Version: 1.3.0
Updated: 2026-02-28

## Outbox entity

```csharp
public class OutboxEvent
{
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string StoreId { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string PayloadJson { get; set; } = default!;
}
```
