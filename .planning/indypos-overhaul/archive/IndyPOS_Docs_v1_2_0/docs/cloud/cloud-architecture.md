# Cloud Architecture & Sync Ingestion

Version: 1.1.0  
Updated: 2026-02-28

## Core idea
StoreHub sends events. Cloud ingests them **idempotently**.

## Example: event contract

```json
{
  "storeId": "STORE-001",
  "eventPublicId": "b7b8f1a0-2c64-4b2c-9d4e-07d14a2f79ad",
  "type": "InvoiceCompleted",
  "createdUtc": "2026-02-28T05:12:34Z",
  "payload": { "...": "..." }
}
```

## Example: idempotent endpoint (ASP.NET Core)

```csharp
[HttpPost("/sync/events")]
public async Task<IActionResult> Ingest([FromBody] SyncEventDto dto, CancellationToken ct)
{
    if (await _syncedEvents.ExistsAsync(dto.EventPublicId, ct))
        return Ok(new { status = "duplicate" });

    await _service.ProcessAsync(dto, ct); // within a transaction
    await _syncedEvents.InsertAsync(dto, ct);

    return Ok(new { status = "processed" });
}
```

## Example: database table for idempotency

```sql
CREATE TABLE synced_event (
  event_public_id uuid PRIMARY KEY,
  store_id varchar(50) NOT NULL,
  type text NOT NULL,
  received_utc timestamptz NOT NULL
);
```
