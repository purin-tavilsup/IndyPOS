using FluentAssertions;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Delete;
using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.CashDrawer.Delete;

public class DeleteCashEntryCommandHandlerTests
{
    private static DeleteCashEntryCommandHandler<CashPayout> HandlerFor(CashDrawerTestContext context) =>
        new(context.EntryRepository<CashPayout>(), context.Clock);

    private static DeleteCashEntryCommand<CashPayout> Command(Guid id) =>
        new(id, CashDrawerTestContext.OtherCashierId);

    private static Task<CashPayout> StoredAsync(CashDrawerTestContext context, Guid id) =>
        context.Db.CashPayouts.IgnoreQueryFilters().AsNoTracking().SingleAsync(p => p.Id == id);

    [Fact]
    public async Task HandleAsync_WithUnknownId_Throws()
    {
        await using var context = new CashDrawerTestContext();

        var act = () => HandlerFor(context).HandleAsync(Command(Guid.NewGuid()));

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
    public async Task HandleAsync_WithYesterdaysPayout_LeavesItActive()
    {
        await using var context = new CashDrawerTestContext();
        var payout = await context.SeedPayoutAsync(CashDrawerTestContext.Yesterday);

        try { await HandlerFor(context).HandleAsync(Command(payout.Id)); } catch (CashDayClosedException) { }

        (await StoredAsync(context, payout.Id)).IsDeleted.Should()
                                                         .BeFalse();
    }

    [Fact]
    public async Task HandleAsync_WhenAlreadyDeleted_DoesNotThrow()
    {
        await using var context = new CashDrawerTestContext();
        var payout = await context.SeedPayoutAsync(CashDrawerTestContext.Today, isDeleted: true);

        var act = () => HandlerFor(context).HandleAsync(Command(payout.Id));

        await act.Should()
                 .NotThrowAsync();
    }

    [Fact]
    public async Task HandleAsync_WhenAlreadyDeleted_KeepsTheOriginalDeleter()
    {
        await using var context = new CashDrawerTestContext();
        var payout = await context.SeedPayoutAsync(CashDrawerTestContext.Today, isDeleted: true);

        await HandlerFor(context).HandleAsync(Command(payout.Id));

        var stored = await StoredAsync(context, payout.Id);
        stored.LastModifiedByUserId.Should()
                                   .Be(CashDrawerTestContext.CashierId);
    }

    [Fact]
    public async Task HandleAsync_WhenAlreadyDeleted_KeepsTheOriginalDeletionTime()
    {
        await using var context = new CashDrawerTestContext();
        var payout = await context.SeedPayoutAsync(CashDrawerTestContext.Today, isDeleted: true);

        await HandlerFor(context).HandleAsync(Command(payout.Id));

        var stored = await StoredAsync(context, payout.Id);
        stored.DeletedUtc.Should()
                         .Be(payout.DeletedUtc);
    }

    [Fact]
    public async Task HandleAsync_WhenAlreadyDeleted_WritesNoEvent()
    {
        await using var context = new CashDrawerTestContext();
        var payout = await context.SeedPayoutAsync(CashDrawerTestContext.Today, isDeleted: true);

        await HandlerFor(context).HandleAsync(Command(payout.Id));

        context.OutboxEvents().Should()
                              .BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WhenYesterdaysPayoutAlreadyDeleted_DoesNotThrow()
    {
        await using var context = new CashDrawerTestContext();
        var payout = await context.SeedPayoutAsync(CashDrawerTestContext.Yesterday, isDeleted: true);

        var act = () => HandlerFor(context).HandleAsync(Command(payout.Id));

        await act.Should()
                 .NotThrowAsync();
    }

    [Fact]
    public async Task HandleAsync_WithTodaysPayout_SetsTheDeletedFlag()
    {
        await using var context = new CashDrawerTestContext();
        var payout = await context.SeedPayoutAsync(CashDrawerTestContext.Today);

        await HandlerFor(context).HandleAsync(Command(payout.Id));

        var stored = await StoredAsync(context, payout.Id);
        stored.IsDeleted.Should()
                        .BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WithTodaysPayout_RecordsTheDeleter()
    {
        await using var context = new CashDrawerTestContext();
        var payout = await context.SeedPayoutAsync(CashDrawerTestContext.Today);

        await HandlerFor(context).HandleAsync(Command(payout.Id));

        var stored = await StoredAsync(context, payout.Id);
        stored.LastModifiedByUserId.Should()
                                   .Be(CashDrawerTestContext.OtherCashierId);
    }

    [Fact]
    public async Task HandleAsync_WithTodaysPayout_WritesOneEventCarryingTheDeletedFlag()
    {
        await using var context = new CashDrawerTestContext();
        var payout = await context.SeedPayoutAsync(CashDrawerTestContext.Today);

        await HandlerFor(context).HandleAsync(Command(payout.Id));

        context.OutboxEvents().Should()
                              .ContainSingle(e => e.Type == CashDrawerOutbox.CashPayoutChanged
                                                  && e.PayloadJson.Contains("\"IsDeleted\":true"));
    }

    [Fact]
    public async Task HandleAsync_WhenDeletedConcurrently_DoesNotThrow()
    {
        await using var context = new CashDrawerTestContext();
        var payout = ConcurrentDeleteRepository.ActiveToday<CashPayout>();
        var repository = ConcurrentDeleteRepository.For(payout);
        var handler = new DeleteCashEntryCommandHandler<CashPayout>(repository.Object, context.Clock);

        var act = () => handler.HandleAsync(Command(payout.Id));

        await act.Should()
                 .NotThrowAsync();
    }
}
