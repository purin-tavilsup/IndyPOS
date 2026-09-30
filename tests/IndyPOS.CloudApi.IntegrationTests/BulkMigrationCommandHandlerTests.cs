using System.Data.Common;
using FluentAssertions;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.Cloud.Sync.BulkMigration;
using IndyPOS.CloudApi.Domain;
using IndyPOS.CloudApi.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace IndyPOS.CloudApi.IntegrationTests;

/// <summary>
/// The migration-time history push (Epic 3 Phase B) runs through this handler. It opened a bare
/// BeginTransactionAsync, which the Npgsql retrying strategy rejects, so every push threw before
/// importing anything.
/// </summary>
public class BulkMigrationCommandHandlerTests(CloudPostgresFixture postgres) : IClassFixture<CloudPostgresFixture>
{
    private const string StoreId = "1";
    private const string OtherStoreId = "2";

    // The endpoint trusted the body's StoreId, so any registered store could import history as another.
    [Fact]
    public async Task HandleAsync_WithAPushForAnotherStore_ThrowsStoreMismatch()
    {
        await using var db = CloudPostgresFixture.CreateContext(await postgres.CreateDatabaseAsync());
        var command = CommandWith(NewInvoice()) with { AuthenticatedStoreId = OtherStoreId };

        var act = () => NewHandler(db).HandleAsync(command);

        await act.Should()
                 .ThrowAsync<StoreMismatchException>();
    }

    [Fact]
    public async Task HandleAsync_WithAPushForAnotherStore_ImportsNothing()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        await using (var db = CloudPostgresFixture.CreateContext(connectionString))
        {
            var command = CommandWith(NewInvoice()) with { AuthenticatedStoreId = OtherStoreId };
            await FluentActions.Awaiting(() => NewHandler(db).HandleAsync(command))
                               .Should()
                               .ThrowAsync<StoreMismatchException>();
        }

        await using var check = CloudPostgresFixture.CreateContext(connectionString);

        (await check.Invoices.CountAsync()).Should()
                                           .Be(0);
    }

    // A fresh invoice beside it, so a handler that imports nothing at all cannot pass.
    [Fact]
    public async Task HandleAsync_WithAnInvoiceAlreadyInTheCloud_ImportsOnlyTheFreshOne()
    {
        await using var db = CloudPostgresFixture.CreateContext(await postgres.CreateDatabaseAsync());
        var alreadyInCloud = NewInvoice();
        db.Invoices.Add(new CloudInvoice { Id = alreadyInCloud.Id, StoreId = StoreId, CreatedAtUtc = DateTime.UtcNow, SyncedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var response = await NewHandler(db).HandleAsync(CommandWith(alreadyInCloud, NewInvoice()));

        response.InvoicesImported.Should()
                                 .Be(1, string.Join(" | ", response.Errors));
    }

    // The handler's catch-all sat inside strategy.ExecuteAsync, so a transient fault came back as a
    // "Transaction failed" response and the strategy never saw anything to retry.
    [Fact]
    public async Task HandleAsync_WhenTheFirstAttemptHitsATransientFault_StillImportsTheInvoice()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        var invoice = NewInvoice();
        var transientFault = new FailFirstInsert(new NpgsqlException("Simulated connection drop", new IOException()));
        await using (var db = CloudPostgresFixture.CreateContext(connectionString, transientFault))
            await NewHandler(db).HandleAsync(CommandWith(invoice));

        await using var check = CloudPostgresFixture.CreateContext(connectionString);

        (await check.Invoices.AnyAsync(i => i.Id == invoice.Id)).Should()
                                                                .BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenCancelledMidImport_ThrowsOperationCanceled()
    {
        var cancelled = new FailFirstInsert(new OperationCanceledException());
        await using var db = CloudPostgresFixture.CreateContext(await postgres.CreateDatabaseAsync(), cancelled);

        var act = () => NewHandler(db).HandleAsync(CommandWith(NewInvoice()));

        await act.Should()
                 .ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task HandleAsync_UnderTheRetryStrategy_ReturnsSuccess()
    {
        await using var db = CloudPostgresFixture.CreateContext(await postgres.CreateDatabaseAsync());

        var response = await NewHandler(db).HandleAsync(CommandWith(NewInvoice()));

        response.Success.Should()
                        .BeTrue(string.Join(" | ", response.Errors));
    }

    [Fact]
    public async Task HandleAsync_UnderTheRetryStrategy_StoresTheInvoiceWithItsLineAndPayment()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        var invoice = NewInvoice();
        await using (var db = CloudPostgresFixture.CreateContext(connectionString))
            await NewHandler(db).HandleAsync(CommandWith(invoice));

        await using var check = CloudPostgresFixture.CreateContext(connectionString);
        var stored = await check.Invoices
                                .Include(i => i.Lines)
                                .Include(i => i.Payments)
                                .SingleAsync(i => i.Id == invoice.Id);

        (stored.Lines.Count, stored.Payments.Count).Should()
                                                   .Be((1, 1));
    }

    /// <summary>Throws the given exception from the first INSERT only, so any retry runs clean.</summary>
    private sealed class FailFirstInsert(Exception fault) : DbCommandInterceptor
    {
        private int _thrown;

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            ThrowOnFirstInsert(command);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            ThrowOnFirstInsert(command);
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }

        private void ThrowOnFirstInsert(DbCommand command)
        {
            if (command.CommandText.Contains("INSERT", StringComparison.OrdinalIgnoreCase)
                && Interlocked.Exchange(ref _thrown, 1) == 0)
                throw fault;
        }
    }

    private static BulkMigrationCommandHandler NewHandler(CloudDbContext db) =>
        new(db, NullLogger<BulkMigrationCommandHandler>.Instance);

    private static BulkMigrationCommand CommandWith(params MigratedInvoice[] invoices) =>
        new(StoreId, Users: [], Products: [], Invoices: invoices, AuthenticatedStoreId: StoreId);

    private static MigratedInvoice NewInvoice() =>
        new(Id: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            TotalAmount: 35m,
            CreatedAtUtc: DateTime.UtcNow,
            Lines: [new MigratedInvoiceLine(Guid.NewGuid(), Guid.NewGuid(), "Nail", Quantity: 1, UnitPrice: 35m)],
            Payments: [new MigratedPayment(Guid.NewGuid(), "Cash", Amount: 35m, Note: null)]);
}
