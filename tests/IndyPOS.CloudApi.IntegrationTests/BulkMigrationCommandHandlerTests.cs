using FluentAssertions;
using IndyPOS.Application.UseCases.Cloud.Sync.BulkMigration;
using IndyPOS.CloudApi.Domain;
using IndyPOS.CloudApi.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
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

    [Fact]
    public async Task HandleAsync_WithAnInvoiceAlreadyInTheCloud_DoesNotImportItAgain()
    {
        await using var db = CloudPostgresFixture.CreateContext(await postgres.CreateDatabaseAsync());
        var invoice = NewInvoice();
        db.Invoices.Add(new CloudInvoice { Id = invoice.Id, StoreId = StoreId, CreatedAtUtc = DateTime.UtcNow, SyncedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var response = await NewHandler(db).HandleAsync(CommandWith(invoice));

        response.InvoicesImported.Should()
                                 .Be(0, string.Join(" | ", response.Errors));
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

    private static BulkMigrationCommandHandler NewHandler(CloudDbContext db) =>
        new(db, NullLogger<BulkMigrationCommandHandler>.Instance);

    private static BulkMigrationCommand CommandWith(MigratedInvoice invoice) =>
        new(StoreId, Users: [], Products: [], Invoices: [invoice]);

    private static MigratedInvoice NewInvoice() =>
        new(Id: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            TotalAmount: 35m,
            CreatedAtUtc: DateTime.UtcNow,
            Lines: [new MigratedInvoiceLine(Guid.NewGuid(), Guid.NewGuid(), "Nail", Quantity: 1, UnitPrice: 35m)],
            Payments: [new MigratedPayment(Guid.NewGuid(), "Cash", Amount: 35m, Note: null)]);
}
