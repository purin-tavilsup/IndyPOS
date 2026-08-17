using FluentAssertions;
using IndyPOS.MigrationTool.Tests.Fixtures;
using IndyPOS.MigrationTool.Tests.Tools;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.MigrationTool.Tests;

/// <summary>
/// Pins how lines and payments are attached to their invoice.
/// </summary>
/// <remarks>
/// The invoice phase used to re-query <c>InvoiceProduct</c> and <c>Payment</c> once per invoice, and
/// now reads each table once and groups in memory (defect 19 — those per-invoice queries were full
/// table scans, 3.5 hours on GeneralHardware). That is a behaviour-preserving change, so these tests
/// exist as its safety net rather than to drive it: they cover the cases a grouped read can get
/// wrong and a per-invoice query cannot — an invoice whose key is simply absent from the lookup, and
/// rows landing against the wrong invoice.
///
/// Each was checked to be load-bearing by deliberately mis-grouping the implementation.
/// </remarks>
[Collection("Postgres")]
public class InvoiceGroupingTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;

    public InvoiceGroupingTests(PostgresFixture postgres) => _postgres = postgres;

    public Task InitializeAsync() => _postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task MigrateInvoices_WithAnInvoiceThatHasNoLinesOrPayments_StillMigratesTheInvoice()
    {
        // A grouped read has no entry at all for such an invoice, where a per-invoice query simply
        // returned nothing. Real stores hold these: a sale voided down to nothing still leaves its
        // Invoice row behind.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddInvoiceAsync(1, userId: 1, total: 0m, dateCreated: "2024-03-15 14:30:00");

        var result = await MigrationScenario.RunAsync(store, _postgres);

        result.Invoices.Migrated.Should().Be(1);
        result.Invoices.Failed.Should().Be(0);

        await using var db = _postgres.CreateDbContext();
        var invoice = await db.Invoices.SingleAsync();
        (await db.InvoiceLines.CountAsync()).Should().Be(0);
        (await db.Payments.CountAsync()).Should().Be(0);
        invoice.TotalAmount.Should().Be(0m);
    }

    [Fact]
    public async Task MigrateInvoices_WithSeveralInvoices_KeepsEachOnesLinesAndPaymentsWithIt()
    {
        // The cross-attribution guard. Invoice 1 has two lines and one payment, invoice 2 has one
        // line and two payments, and invoice 3 has none -- so a grouping that is off by one, keyed
        // on the wrong column, or that reuses the previous invoice's rows shows up here.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");

        await builder.AddProductAsync(
            productId: 1, barcode: "8850001000001", description: "Cement",
            unitPrice: 120m, quantityInStock: 50, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");
        await builder.AddProductAsync(
            productId: 2, barcode: "8850001000002", description: "Nails",
            unitPrice: 35m, quantityInStock: 80, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");

        await builder.AddInvoiceAsync(1, userId: 1, total: 275m, dateCreated: "2024-03-15 14:30:00");
        await builder.AddInvoiceLineAsync(
            invoiceProductId: 1, invoiceId: 1, productId: 1, barcode: "8850001000001",
            description: "Cement", quantity: 2, unitPrice: 120m, originalUnitPrice: 120m);
        await builder.AddInvoiceLineAsync(
            invoiceProductId: 2, invoiceId: 1, productId: 2, barcode: "8850001000002",
            description: "Nails", quantity: 1, unitPrice: 35m, originalUnitPrice: 35m);
        await builder.AddPaymentAsync(
            paymentId: 1, invoiceId: 1, paymentTypeId: 1, amount: 275m,
            dateCreated: "2024-03-15 14:30:00");

        await builder.AddInvoiceAsync(2, userId: 1, total: 120m, dateCreated: "2024-03-16 10:00:00");
        await builder.AddInvoiceLineAsync(
            invoiceProductId: 3, invoiceId: 2, productId: 1, barcode: "8850001000001",
            description: "Cement", quantity: 1, unitPrice: 120m, originalUnitPrice: 120m);
        await builder.AddPaymentAsync(
            paymentId: 2, invoiceId: 2, paymentTypeId: 1, amount: 100m,
            dateCreated: "2024-03-16 10:00:00");
        await builder.AddPaymentAsync(
            paymentId: 3, invoiceId: 2, paymentTypeId: 5, amount: 20m,
            dateCreated: "2024-03-16 10:00:00");

        await builder.AddInvoiceAsync(3, userId: 1, total: 0m, dateCreated: "2024-03-17 11:00:00");

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        var invoices = await db.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .OrderBy(i => i.CreatedUtc)
            .ToListAsync();

        invoices.Should().HaveCount(3);

        invoices[0].Lines.Should().HaveCount(2);
        invoices[0].Lines.Sum(l => l.Quantity * l.UnitPrice).Should().Be(275m);
        invoices[0].Payments.Should().ContainSingle().Which.Amount.Should().Be(275m);

        invoices[1].Lines.Should().ContainSingle().Which.Quantity.Should().Be(1);
        invoices[1].Payments.Should().HaveCount(2);
        invoices[1].Payments.Sum(p => p.Amount).Should().Be(120m);

        invoices[2].Lines.Should().BeEmpty("invoice 3 has no lines of its own");
        invoices[2].Payments.Should().BeEmpty("and no payments either");
    }
}
