# StoreHub Overview

Version: 1.1.0  
Updated: 2026-02-28

## Responsibilities
- Serve LAN API for POS terminals
- Perform all writes to local DB
- Create Outbox events at commit points
- Run SyncWorker to push/pull
- Provide health + sync status endpoints

## Example: minimal controller (sale complete)

```csharp
[ApiController]
[Route("sales")]
public class SalesController : ControllerBase
{
    private readonly ISaleService _saleService;

    public SalesController(ISaleService saleService) => _saleService = saleService;

    [HttpPost("complete")]
    public async Task<IActionResult> Complete([FromBody] CompleteSaleRequest req, CancellationToken ct)
    {
        var result = await _saleService.CompleteSaleAsync(req, ct);
        return Ok(result); // includes InvoicePublicId
    }
}
```

## Example: background sync worker registration

```csharp
builder.Services.AddHostedService<SyncWorker>();
builder.Services.AddHttpClient<CloudSyncClient>();
```

## Example: Sync status endpoint

```csharp
[HttpGet("/sync/status")]
public async Task<IActionResult> Status([FromServices] IOutboxRepository outbox)
{
    var pending = await outbox.CountPendingAsync();
    return Ok(new { pending });
}
```
