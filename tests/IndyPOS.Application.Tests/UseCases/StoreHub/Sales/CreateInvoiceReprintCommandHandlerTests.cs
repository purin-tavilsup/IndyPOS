using System.Text.Json;
using FluentAssertions;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.Cloud.Sync.Events;
using IndyPOS.Application.UseCases.StoreHub.Sales.Reprints;
using IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static IndyPOS.Application.Tests.UseCases.StoreHub.Sales.SalesHistoryTestContext;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.Sales;

public class CreateInvoiceReprintCommandHandlerTests
{
    private const long KnownNumber = 7001;
    private static readonly Guid ReprinterId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static CreateInvoiceReprintCommandHandler Handler(SalesHistoryTestContext c) =>
        new(c.DetailHandler(), new InvoiceReprintRepository(c.Db), c.Clock);

    private static CreateInvoiceReprintCommand TodayOnly(Guid invoiceId) => new(invoiceId, ReprinterId, CanViewAnyDay: false);

    [Fact]
    public async Task Handle_WithAnUnknownInvoice_ThrowsNotFound()
    {
        await using var c = new SalesHistoryTestContext();

        var act = () => Handler(c).HandleAsync(TodayOnly(Guid.NewGuid()));

        await act.Should()
                 .ThrowAsync<SaleNotFoundException>();
    }

    [Fact]
    public async Task Handle_AsTodayOnlyCallerForYesterdaysBill_ThrowsNotFound()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmYesterdayUtc));

        var act = () => Handler(c).HandleAsync(TodayOnly(invoice.Id));

        await act.Should()
                 .ThrowAsync<SaleNotFoundException>();
    }

    [Fact]
    public async Task Handle_AsTodayOnlyCallerForYesterdaysBill_WritesNoRow()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmYesterdayUtc));

        try { await Handler(c).HandleAsync(TodayOnly(invoice.Id)); }
        catch (SaleNotFoundException) { }

        (await c.Db.InvoiceReprints.CountAsync()).Should()
                                                 .Be(0);
    }

    [Fact]
    public async Task Handle_AsTodayOnlyCallerForYesterdaysBill_WritesNoEvent()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmYesterdayUtc));

        try { await Handler(c).HandleAsync(TodayOnly(invoice.Id)); }
        catch (SaleNotFoundException) { }

        (await c.Db.OutboxEvents.CountAsync()).Should()
                                              .Be(0);
    }

    [Fact]
    public async Task Handle_Twice_WritesTwoRows()
    {
        // A printer failure after the first leaves its record; pressing reprint again adds one.
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmTodayUtc));

        await Handler(c).HandleAsync(TodayOnly(invoice.Id));
        await Handler(c).HandleAsync(TodayOnly(invoice.Id));

        (await c.Db.InvoiceReprints.CountAsync()).Should()
                                                 .Be(2);
    }

    [Fact]
    public async Task Handle_WithTodaysBill_WritesOneInvoiceReprintedEvent()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmTodayUtc));

        await Handler(c).HandleAsync(TodayOnly(invoice.Id));

        (await c.Db.OutboxEvents.SingleAsync()).Type.Should()
                                               .Be(InvoiceReprintOutbox.InvoiceReprinted);
    }

    [Fact]
    public async Task Handle_WithTodaysBill_PutsTheReprintIdInThePayload()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmTodayUtc));

        var result = await Handler(c).HandleAsync(TodayOnly(invoice.Id));

        var outbox = await c.Db.OutboxEvents.SingleAsync();
        JsonSerializer.Deserialize<InvoiceReprintedEvent>(outbox.PayloadJson)!.ReprintId.Should()
                                                                            .Be(result.Reprint.Id);
    }

    [Fact]
    public async Task Handle_WithTodaysBill_StampsTheCallerAsReprinter()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmTodayUtc));

        await Handler(c).HandleAsync(TodayOnly(invoice.Id));

        (await c.Db.InvoiceReprints.SingleAsync()).CreatedByUserId.Should()
                                                  .Be(ReprinterId);
    }

    [Fact]
    public async Task Handle_WithTodaysBill_SetsLastModifiedToCreated()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmTodayUtc));

        await Handler(c).HandleAsync(TodayOnly(invoice.Id));

        var row = await c.Db.InvoiceReprints.SingleAsync();
        row.LastModifiedUtc.Should()
                           .Be(row.CreatedUtc);
    }

    [Fact]
    public async Task Handle_WithTodaysBill_ReturnsTheBillDetail()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmTodayUtc));

        var result = await Handler(c).HandleAsync(TodayOnly(invoice.Id));

        result.Sale.InvoiceNumber.Should()
                                 .Be(KnownNumber);
    }

    [Fact]
    public async Task Handle_AsAnyDayCallerForYesterdaysBill_WritesOneRow()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmYesterdayUtc));

        await Handler(c).HandleAsync(TodayOnly(invoice.Id) with { CanViewAnyDay = true });

        (await c.Db.InvoiceReprints.CountAsync()).Should()
                                                 .Be(1);
    }
}
