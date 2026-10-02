using FluentAssertions;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests;

/// <summary>
/// Real-PostgreSQL checks for invoice.invoice_number: the default fills every INSERT (including
/// one that never names the column), and the number is unique per store, not per database.
/// </summary>
[Collection("Integration")]
public class InvoiceNumberPersistenceTests : IntegrationTestBase
{
    private const string OtherStoreId = "other-store";

    /// <summary>Explicit numbers far above anything the sequence reaches in a test run.</summary>
    private static long _lastExplicitNumber = 900_000_000;

    public InvoiceNumberPersistenceTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    private static long NextExplicitNumber() => Interlocked.Increment(ref _lastExplicitNumber);

    private static Invoice NewInvoice(string storeId, long invoiceNumber) => new()
    {
        Id = Guid.NewGuid(),
        StoreId = storeId,
        UserId = Guid.NewGuid(),
        TotalAmount = 10m,
        CreatedUtc = DateTime.UtcNow,
        LastModifiedUtc = DateTime.UtcNow,
        InvoiceNumber = invoiceNumber
    };

    private async Task SaveAsync(Invoice invoice)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Insert_WithADuplicateNumberInTheSameStore_ThrowsUniqueViolation()
    {
        var number = NextExplicitNumber();
        await SaveAsync(NewInvoice(TestStoreIdentityService.TestStoreId, number));

        var act = () => SaveAsync(NewInvoice(TestStoreIdentityService.TestStoreId, number));

        (await act.Should()
                  .ThrowAsync<DbUpdateException>())
            .WithInnerException<PostgresException>()
            .Which.SqlState.Should()
                           .Be(PostgresErrorCodes.UniqueViolation);
    }

    [Fact]
    public async Task Insert_WithTheSameNumberInAnotherStore_Succeeds()
    {
        var number = NextExplicitNumber();
        await SaveAsync(NewInvoice(TestStoreIdentityService.TestStoreId, number));

        var act = () => SaveAsync(NewInvoice(OtherStoreId, number));

        await act.Should()
                 .NotThrowAsync();
    }

    [Fact]
    public async Task Insert_WithOnlyPreReleaseColumns_GetsANumberFromTheDefault()
    {
        // The forward-only gate in code form: the previous release's binaries INSERT an invoice
        // without ever naming invoice_number, and the row must still get one.
        await using var connection = new NpgsqlConnection(Factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            INSERT INTO invoice (id, store_id, user_id, total_amount, created_utc, last_modified_utc)
            VALUES (gen_random_uuid(), 'test-store', gen_random_uuid(), 10, now(), now())
            RETURNING invoice_number
            """, connection);

        var number = (long)(await command.ExecuteScalarAsync())!;

        number.Should()
              .BePositive();
    }

    [Fact]
    public async Task Insert_ThroughEfWithoutANumber_ReadsBackTheDatabaseNumber()
    {
        var invoice = await SeedInvoiceAsync(DateTime.UtcNow);

        invoice.InvoiceNumber.Should()
                             .BePositive();
    }

    [Fact]
    public async Task Insert_TwiceWithoutANumber_AssignsAHigherNumberSecond()
    {
        var first = await SeedInvoiceAsync(DateTime.UtcNow);

        var second = await SeedInvoiceAsync(DateTime.UtcNow);

        second.InvoiceNumber.Should()
                            .BeGreaterThan(first.InvoiceNumber);
    }
}
