# Epic E: Outbox + SyncWorker - COMPLETE

**Completed:** 2026-03-08
**Commit:** `df4f1b5`

## Goal
Reliable sync from StoreHub to Cloud

## Tasks Completed

| Task | Description |
|------|-------------|
| E1 | Create Outbox table |
| E2 | Write Outbox events at commit points |
| E3 | Implement SyncWorker |
| E4 | Local observability endpoints |

## Implementation Details

**SyncWorker BackgroundService:**
- Polls outbox table every N seconds (configurable)
- Batch processing with configurable size
- Exponential backoff retry (30s, 60s, 120s, 240s...)
- Max retries limit (default 5)
- Enable/disable via configuration
- Uses `ICloudSyncClient` interface

## Files Created
- `IOutboxRepository.cs`
- `ICloudSyncClient.cs`
- `OutboxRepository.cs`
- `SyncWorker.cs`
- `SyncWorkerOptions.cs`
- `StubCloudSyncClient.cs`

## Observability
- `GET /sync/status` returns pending count, failed count, sync status

## Deliverable
Disconnect internet -> sales continue -> reconnect -> events sync
