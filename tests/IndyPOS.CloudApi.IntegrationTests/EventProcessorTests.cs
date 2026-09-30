using System.Text.Json;
using FluentAssertions;
using IndyPOS.Application.Abstractions.Cloud.Repositories;
using IndyPOS.Application.UseCases.Cloud.Sync.Events;
using IndyPOS.CloudApi.Domain;
using IndyPOS.CloudApi.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IndyPOS.CloudApi.IntegrationTests;

/// <summary>
/// Drives one poll of the real EventProcessor against PostgreSQL with the retry strategy on. Its
/// InvoiceCompleted handler opened a bare BeginTransactionAsync, which that strategy rejects, so
/// no v4 sale ever reached cloud_invoice.
/// </summary>
public class EventProcessorTests(CloudPostgresFixture postgres) : IClassFixture<CloudPostgresFixture>
{
    private const int BatchSize = 50;

    [Fact]
    public async Task ProcessPendingEventsAsync_WithAnEventAlreadyInProcessedEvents_DoesNotStoreItAgain()
    {
        await using var pipeline = await Pipeline.CreateAsync(postgres);
        var evt = await pipeline.AddInvoiceCompletedAsync();
        await pipeline.AddProcessedEventAsync(evt.EventId);

        await pipeline.PollAsync();

        (await pipeline.CountInvoicesAsync()).Should()
                                             .Be(0);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithAnEventAlreadyInProcessedEvents_MarksItProcessed()
    {
        await using var pipeline = await Pipeline.CreateAsync(postgres);
        var evt = await pipeline.AddInvoiceCompletedAsync();
        await pipeline.AddProcessedEventAsync(evt.EventId);

        await pipeline.PollAsync();

        (await pipeline.CountUnprocessedAsync()).Should()
                                                .Be(0);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithMoreEventsThanOneBatch_ProcessesThemAllOverTwoPolls()
    {
        await using var pipeline = await Pipeline.CreateAsync(postgres);
        for (var i = 0; i < BatchSize + 1; i++)
            await pipeline.AddInvoiceCompletedAsync();

        await pipeline.PollAsync();
        await pipeline.PollAsync();

        (await pipeline.CountInvoicesAsync()).Should()
                                             .Be(BatchSize + 1, pipeline.LoggedErrors);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithAnInvoiceCompletedEvent_StoresTheInvoice()
    {
        await using var pipeline = await Pipeline.CreateAsync(postgres);
        var evt = await pipeline.AddInvoiceCompletedAsync();

        await pipeline.PollAsync();

        (await pipeline.FindInvoiceAsync(evt.InvoiceId)).Should()
                                                        .NotBeNull(pipeline.LoggedErrors);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithAnInvoiceCompletedEvent_StoresItsLineAndPayment()
    {
        await using var pipeline = await Pipeline.CreateAsync(postgres);
        var evt = await pipeline.AddInvoiceCompletedAsync();

        await pipeline.PollAsync();

        var invoice = await pipeline.FindInvoiceAsync(evt.InvoiceId);
        (invoice?.Lines.Count, invoice?.Payments.Count).Should()
                                                       .Be((1, 1), pipeline.LoggedErrors);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithAnInvoiceCompletedEvent_MarksTheEventProcessed()
    {
        await using var pipeline = await Pipeline.CreateAsync(postgres);
        await pipeline.AddInvoiceCompletedAsync();

        await pipeline.PollAsync();

        (await pipeline.CountUnprocessedAsync()).Should()
                                                .Be(0, pipeline.LoggedErrors);
    }

    /// <summary>A migrated cloud database, an inbox, and the processor wired as Program.cs wires it.</summary>
    private sealed class Pipeline : IAsyncDisposable
    {
        private readonly ServiceProvider _services;
        private readonly EventProcessor _processor;
        private readonly RecordingLogger _logger = new();
        private DateTime _nextReceivedAtUtc = DateTime.UtcNow;

        private Pipeline(ServiceProvider services)
        {
            _services = services;
            _processor = new EventProcessor(
                services.GetRequiredService<IServiceScopeFactory>(),
                _logger);
        }

        public static async Task<Pipeline> CreateAsync(CloudPostgresFixture postgres)
        {
            var connectionString = await postgres.CreateDatabaseAsync();
            var services = new ServiceCollection();
            services.AddDbContext<CloudDbContext>(o => CloudPostgresFixture.Configure(o, connectionString));
            services.AddScoped<ISyncedEventRepository, DbSyncedEventRepository>();

            return new Pipeline(services.BuildServiceProvider());
        }

        /// <summary>The processor logs and swallows a failed event; this puts the reason in the assertion.</summary>
        public string LoggedErrors => "the processor logged: " + string.Join(" | ", _logger.Errors);

        public Task PollAsync() => _processor.ProcessPendingEventsAsync(CancellationToken.None);

        public async Task<InvoiceCompletedEvent> AddInvoiceCompletedAsync(Guid? invoiceId = null)
        {
            var evt = new InvoiceCompletedEvent
            {
                EventId = Guid.NewGuid(),
                InvoiceId = invoiceId ?? Guid.NewGuid(),
                StoreId = "1",
                UserId = Guid.NewGuid(),
                TotalAmount = 35m,
                CreatedAtUtc = DateTime.UtcNow,
                Lines = [new InvoiceLineSnapshot { LineId = Guid.NewGuid(), ProductId = Guid.NewGuid(), ProductName = "Nail", Quantity = 1, UnitPrice = 35m }],
                Payments = [new PaymentSnapshot { PaymentId = Guid.NewGuid(), Method = "Cash", Amount = 35m }]
            };

            // Distinct receive times, so the inbox's oldest-first order is the order added.
            _nextReceivedAtUtc = _nextReceivedAtUtc.AddMilliseconds(1);
            await WithDbAsync(db =>
            {
                db.SyncedEvents.Add(new SyncedEventEntity
                {
                    EventId = evt.EventId,
                    StoreId = 1,
                    EventType = "InvoiceCompleted",
                    Payload = JsonSerializer.Serialize(evt),
                    CreatedAtUtc = evt.CreatedAtUtc,
                    ReceivedAtUtc = _nextReceivedAtUtc
                });
                return db.SaveChangesAsync();
            });

            return evt;
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

        public Task<int> CountUnprocessedAsync() =>
            WithDbAsync(db => db.SyncedEvents.CountAsync(e => e.ProcessedAtUtc == null));

        public Task<CloudInvoice?> FindInvoiceAsync(Guid invoiceId) =>
            WithDbAsync(db => db.Invoices
                                .Include(i => i.Lines)
                                .Include(i => i.Payments)
                                .SingleOrDefaultAsync(i => i.Id == invoiceId));

        public ValueTask DisposeAsync() => _services.DisposeAsync();

        private sealed class RecordingLogger : ILogger<EventProcessor>
        {
            public List<string> Errors { get; } = [];

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;

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
}
