using System.Text.Json;
using IndyPOS.Application.Abstractions.Cloud.Repositories;
using IndyPOS.Application.UseCases.Cloud.Sync.Events;
using IndyPOS.CloudApi.Domain;
using IndyPOS.CloudApi.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IndyPOS.CloudApi.IntegrationTests;

/// <summary>A migrated cloud database, an inbox, and the processor wired as Program.cs wires it.</summary>
internal sealed class EventPipeline : IAsyncDisposable
{
    private readonly ServiceProvider _services;
    private readonly EventProcessor _processor;
    private readonly RecordingLogger _logger = new();
    private DateTime _nextReceivedAtUtc = DateTime.UtcNow;

    private EventPipeline(ServiceProvider services)
    {
        _services = services;
        _processor = new EventProcessor(
            services.GetRequiredService<IServiceScopeFactory>(),
            _logger);
    }

    public static async Task<EventPipeline> CreateAsync(CloudPostgresFixture postgres)
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        var services = new ServiceCollection();
        services.AddDbContext<CloudDbContext>(o => CloudPostgresFixture.Configure(o, connectionString));
        services.AddScoped<ISyncedEventRepository, DbSyncedEventRepository>();

        return new EventPipeline(services.BuildServiceProvider());
    }

    /// <summary>The processor logs and swallows a failed event; this puts the reason in the assertion.</summary>
    public string LoggedErrors => "the processor logged: " + string.Join(" | ", _logger.Errors);

    public Task PollAsync() => _processor.ProcessPendingEventsAsync(CancellationToken.None);

    public async Task<InvoiceCompletedEvent> AddInvoiceCompletedAsync(
        Guid? invoiceId = null, long? invoiceNumber = null, string storeId = "1")
    {
        var evt = new InvoiceCompletedEvent
        {
            EventId = Guid.NewGuid(),
            InvoiceId = invoiceId ?? Guid.NewGuid(),
            InvoiceNumber = invoiceNumber,
            StoreId = storeId,
            UserId = Guid.NewGuid(),
            TotalAmount = 35m,
            CreatedAtUtc = DateTime.UtcNow,
            Lines = [new InvoiceLineSnapshot { LineId = Guid.NewGuid(), ProductId = Guid.NewGuid(), ProductName = "Nail", Quantity = 1, UnitPrice = 35m }],
            Payments = [new PaymentSnapshot { PaymentId = Guid.NewGuid(), Method = "Cash", Amount = 35m }]
        };

        await AddEventAsync("InvoiceCompleted", evt.EventId, SerializeAsTheStoreDid(evt, invoiceNumber));

        return evt;
    }

    // A till from before bill numbers sent no InvoiceNumber key at all, not an explicit null.
    private static string SerializeAsTheStoreDid(InvoiceCompletedEvent evt, long? invoiceNumber)
    {
        var payload = JsonSerializer.SerializeToNode(evt)!.AsObject();

        if (invoiceNumber is null)
            payload.Remove(nameof(InvoiceCompletedEvent.InvoiceNumber));

        return payload.ToJsonString();
    }

    public async Task<InvoiceReprintedEvent> AddInvoiceReprintedAsync(Guid invoiceId)
    {
        // Postgres keeps microseconds; a DateTime has 100-ns ticks, so round-trips compare equal only at this precision.
        var now = DateTime.UtcNow;
        var evt = new InvoiceReprintedEvent
        {
            EventId = Guid.NewGuid(),
            ReprintId = Guid.NewGuid(),
            InvoiceId = invoiceId,
            StoreId = "1",
            CreatedUtc = new DateTime(now.Ticks - now.Ticks % 10, DateTimeKind.Utc),
            CreatedByUserId = Guid.NewGuid()
        };

        await AddEventAsync("InvoiceReprinted", evt.EventId, JsonSerializer.Serialize(evt));

        return evt;
    }

    public Task<List<CloudInvoiceReprint>> ReprintsOfAsync(Guid invoiceId) =>
        WithDbAsync(db => db.InvoiceReprints
                            .AsNoTracking()
                            .Where(r => r.InvoiceId == invoiceId)
                            .ToListAsync());

    public Task AddEventAsync(string eventType) => AddEventAsync(eventType, Guid.NewGuid(), "{}");

    public Task AddEventAsync(string eventType, string payload) => AddEventAsync(eventType, Guid.NewGuid(), payload);

    private Task AddEventAsync(string eventType, Guid eventId, string payload)
    {
        // Distinct receive times, so the inbox's oldest-first order is the order added.
        _nextReceivedAtUtc = _nextReceivedAtUtc.AddMilliseconds(1);
        var receivedAtUtc = _nextReceivedAtUtc;

        return WithDbAsync(db =>
        {
            db.SyncedEvents.Add(new SyncedEventEntity
            {
                EventId = eventId,
                StoreId = 1,
                EventType = eventType,
                Payload = payload,
                CreatedAtUtc = receivedAtUtc,
                ReceivedAtUtc = receivedAtUtc
            });
            return db.SaveChangesAsync();
        });
    }

    /// <summary>An invoice already in the cloud, so an event for the same id fails on its key.</summary>
    public async Task<Guid> AddCloudInvoiceAsync()
    {
        var invoiceId = Guid.NewGuid();
        await WithDbAsync(db =>
        {
            db.Invoices.Add(new CloudInvoice
            {
                Id = invoiceId,
                StoreId = "1",
                CreatedAtUtc = DateTime.UtcNow,
                SyncedAtUtc = DateTime.UtcNow
            });
            return db.SaveChangesAsync();
        });

        return invoiceId;
    }

    public Task AddProcessedEventAsync(Guid eventId) =>
        WithDbAsync(db =>
        {
            db.ProcessedEvents.Add(new ProcessedEvent
            {
                EventId = eventId,
                EventType = "InvoiceCompleted",
                StoreId = "1",
                ProcessedAtUtc = DateTime.UtcNow
            });
            return db.SaveChangesAsync();
        });

    public Task<int> CountInvoicesAsync() => WithDbAsync(db => db.Invoices.CountAsync());

    public Task<int> FailedAttemptsAsync() =>
        WithDbAsync(db => db.SyncedEvents.SumAsync(e => e.Attempts));

    public Task<int> CountUnprocessedAsync() =>
        WithDbAsync(db => db.SyncedEvents.CountAsync(e => e.ProcessedAtUtc == null));

    public Task<CloudInvoice?> FindInvoiceAsync(Guid invoiceId) =>
        WithDbAsync(db => db.Invoices
                            .Include(i => i.Lines)
                            .Include(i => i.Payments)
                            .SingleOrDefaultAsync(i => i.Id == invoiceId));

    /// <summary>Stands in for the retry delays passing, so a test need not wait for real time.</summary>
    public Task ElapseRetryDelaysAsync() =>
        WithDbAsync(db => db.SyncedEvents.ExecuteUpdateAsync(s => s.SetProperty(e => e.NextAttemptAtUtc, (DateTime?)null)));

    public ValueTask DisposeAsync() => _services.DisposeAsync();

    private sealed class RecordingLogger : ILogger<EventProcessor>
    {
        public List<string> Errors { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
                Errors.Add($"{formatter(state, exception)}: {exception?.Message}");
        }
    }

    private async Task<T> WithDbAsync<T>(Func<CloudDbContext, Task<T>> action)
    {
        using var scope = _services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<CloudDbContext>());
    }
}
