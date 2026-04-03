# Example Implementation: Outbox + SyncWorker

Version: 1.1.0  
Updated: 2026-02-28

## 1) Outbox entity (EF Core)

```csharp
public class OutboxEvent
{
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string StoreId { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string PayloadJson { get; set; } = default!;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public int Attempts { get; set; }
    public DateTime? LastAttemptUtc { get; set; }
    public DateTime? NextRetryUtc { get; set; }
    public string Status { get; set; } = "Pending"; // Pending/Sent/Failed
}
```

## 2) Write Outbox event in the same transaction (sale complete)

```csharp
await using var tx = await _db.Database.BeginTransactionAsync(ct);

_db.Invoices.Add(invoice);
_db.InvoiceLines.AddRange(lines);

_db.Outbox.Add(new OutboxEvent {
    StoreId = storeId,
    Type = "InvoiceCompleted",
    PayloadJson = JsonSerializer.Serialize(new {
        invoicePublicId = invoice.PublicId,
        total = invoice.TotalAmount,
        lines = lines.Select(l => new { l.PublicId, l.ProductPublicId, l.Quantity, l.Price }),
        payments = payments.Select(p => new { p.PublicId, p.Amount, p.Method })
    })
});

await _db.SaveChangesAsync(ct);
await tx.CommitAsync(ct);
```

## 3) SyncWorker (BackgroundService) with retry

```csharp
public class SyncWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CloudSyncClient _client;
    private readonly ILogger<SyncWorker> _log;

    public SyncWorker(IServiceScopeFactory scopeFactory, CloudSyncClient client, ILogger<SyncWorker> log)
    {
        _scopeFactory = scopeFactory;
        _client = client;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();

                var now = DateTime.UtcNow;
                var batch = await db.Outbox
                    .Where(x => x.Status == "Pending" && (x.NextRetryUtc == null || x.NextRetryUtc <= now))
                    .OrderBy(x => x.CreatedUtc)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                foreach (var e in batch)
                {
                    e.Attempts += 1;
                    e.LastAttemptUtc = now;

                    var ok = await _client.SendEventAsync(e, stoppingToken);
                    if (ok)
                    {
                        e.Status = "Sent";
                    }
                    else
                    {
                        // exponential backoff up to 30 minutes
                        var backoff = TimeSpan.FromSeconds(Math.Min(1800, Math.Pow(2, Math.Min(10, e.Attempts))));
                        e.NextRetryUtc = now.Add(backoff);
                    }
                }

                await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "SyncWorker loop failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }
}
```

## 4) CloudSyncClient (HttpClient)

```csharp
public class CloudSyncClient
{
    private readonly HttpClient _http;

    public CloudSyncClient(HttpClient http) => _http = http;

    public async Task<bool> SendEventAsync(OutboxEvent e, CancellationToken ct)
    {
        var dto = new {
            storeId = e.StoreId,
            eventPublicId = e.PublicId,
            type = e.Type,
            createdUtc = e.CreatedUtc,
            payload = JsonSerializer.Deserialize<object>(e.PayloadJson)
        };

        var res = await _http.PostAsJsonAsync("/sync/events", dto, ct);
        return res.IsSuccessStatusCode;
    }
}
```
