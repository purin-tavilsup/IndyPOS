using FluentAssertions;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Payouts;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.CashDrawer.Payouts;

public class EditCashPayoutCommandHandlerTests
{
    private const decimal EditedAmount = 275m;

    private static EditCashPayoutCommandHandler HandlerFor(CashDrawerTestContext context) =>
        new(context.EntryRepository<CashPayout>(), context.Clock);

    private static EditCashPayoutCommand Command(Guid id, decimal amount = EditedAmount, PayoutCategory? category = PayoutCategory.Hardware) =>
        new(id, CashDrawerTestContext.OtherCashierId, amount, category, "แก้ไข");

    [Fact]
    public async Task HandleAsync_WithUnknownId_Throws()
    {
        await using var context = new CashDrawerTestContext();

        var act = () => HandlerFor(context).HandleAsync(Command(Guid.NewGuid()));

        await act.Should()
                 .ThrowAsync<CashEntryNotFoundException>();
    }

    [Fact]
    public async Task HandleAsync_WithSoftDeletedId_Throws()
    {
        await using var context = new CashDrawerTestContext();
        var payout = await context.SeedPayoutAsync(CashDrawerTestContext.Today, isDeleted: true);

        var act = () => HandlerFor(context).HandleAsync(Command(payout.Id));

        await act.Should()
                 .ThrowAsync<CashEntryNotFoundException>();
    }

    [Fact]
    public async Task HandleAsync_WithYesterdaysPayout_Throws()
    {
        await using var context = new CashDrawerTestContext();
        var payout = await context.SeedPayoutAsync(CashDrawerTestContext.Yesterday);

        var act = () => HandlerFor(context).HandleAsync(Command(payout.Id));

        await act.Should()
                 .ThrowAsync<CashDayClosedException>();
    }

    [Fact]
    public async Task HandleAsync_WhenCreatedBeforeMidnightAndEditedAfter_Throws()
    {
        await using var context = new CashDrawerTestContext();
        context.Time.SetUtcNow(new DateTimeOffset(2026, 9, 26, 16, 59, 0, TimeSpan.Zero)); // 23:59 Bangkok
        var added = await new AddCashPayoutCommandHandler(context.EntryRepository<CashPayout>(), context.Clock, context.StoreIdentity)
            .HandleAsync(new AddCashPayoutCommand(CashDrawerTestContext.CashierId, 100m, null, null));
        context.Time.Advance(TimeSpan.FromMinutes(2)); // 00:01 Bangkok, next day

        var act = () => HandlerFor(context).HandleAsync(Command(added.Id));

        await act.Should()
                 .ThrowAsync<CashDayClosedException>();
    }

    [Fact]
    public async Task HandleAsync_WithYesterdaysPayout_LeavesTheRowUnchanged()
    {
        await using var context = new CashDrawerTestContext();
        var payout = await context.SeedPayoutAsync(CashDrawerTestContext.Yesterday);

        try { await HandlerFor(context).HandleAsync(Command(payout.Id)); } catch (CashDayClosedException) { }

        var stored = await context.Db.CashPayouts.AsNoTracking().SingleAsync(p => p.Id == payout.Id);
        stored.Amount.Should()
                     .Be(payout.Amount);
    }

    [Fact]
    public async Task HandleAsync_WithNegativeAmount_Throws()
    {
        await using var context = new CashDrawerTestContext();
        var payout = await context.SeedPayoutAsync(CashDrawerTestContext.Today);

        var act = () => HandlerFor(context).HandleAsync(Command(payout.Id, amount: -1m));

        await act.Should()
                 .ThrowAsync<CashEntryValidationException>();
    }

    [Fact]
    public async Task HandleAsync_WithoutCategory_Throws()
    {
        await using var context = new CashDrawerTestContext();
        var payout = await context.SeedPayoutAsync(CashDrawerTestContext.Today);

        var act = () => HandlerFor(context).HandleAsync(Command(payout.Id, category: null));

        await act.Should()
                 .ThrowAsync<CashEntryValidationException>();
    }

    [Fact]
    public async Task HandleAsync_WithTodaysPayout_UpdatesTheAmount()
    {
        await using var context = new CashDrawerTestContext();
        var payout = await context.SeedPayoutAsync(CashDrawerTestContext.Today);

        var result = await HandlerFor(context).HandleAsync(Command(payout.Id));

        result.Amount.Should()
                     .Be(EditedAmount);
    }

    [Fact]
    public async Task HandleAsync_WithTodaysPayout_RecordsTheEditor()
    {
        await using var context = new CashDrawerTestContext();
        var payout = await context.SeedPayoutAsync(CashDrawerTestContext.Today);

        var result = await HandlerFor(context).HandleAsync(Command(payout.Id));

        result.LastModifiedByUserId.Should()
                                   .Be(CashDrawerTestContext.OtherCashierId);
    }

    [Fact]
    public async Task HandleAsync_WithTodaysPayout_KeepsItsBusinessDate()
    {
        await using var context = new CashDrawerTestContext();
        var payout = await context.SeedPayoutAsync(CashDrawerTestContext.Today);

        var result = await HandlerFor(context).HandleAsync(Command(payout.Id));

        result.BusinessDate.Should()
                           .Be(CashDrawerTestContext.Today);
    }

    [Fact]
    public async Task HandleAsync_WithTodaysPayout_WritesOnePayoutChangedEvent()
    {
        await using var context = new CashDrawerTestContext();
        var payout = await context.SeedPayoutAsync(CashDrawerTestContext.Today);

        await HandlerFor(context).HandleAsync(Command(payout.Id));

        context.OutboxEvents().Should()
                              .ContainSingle(e => e.Type == CashDrawerOutbox.CashPayoutChanged);
    }
}
