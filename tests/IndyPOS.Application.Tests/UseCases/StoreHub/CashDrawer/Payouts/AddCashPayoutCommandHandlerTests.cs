using FluentAssertions;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Payouts;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.CashDrawer.Payouts;

public class AddCashPayoutCommandHandlerTests
{
    private static AddCashPayoutCommandHandler HandlerFor(CashDrawerTestContext context) =>
        new(context.EntryRepository<CashPayout>(), context.Clock, context.StoreIdentity);

    private static AddCashPayoutCommand Command(decimal amount = 150m, PayoutCategory? category = null) =>
        new(CashDrawerTestContext.CashierId, amount, category, "ค่าน้ำแข็ง");

    [Fact]
    public async Task HandleAsync_WithZeroAmount_Throws()
    {
        await using var context = new CashDrawerTestContext();

        var act = () => HandlerFor(context).HandleAsync(Command(amount: 0m));

        await act.Should()
                 .ThrowAsync<CashEntryValidationException>();
    }

    [Fact]
    public async Task HandleAsync_WithZeroAmount_WritesNothing()
    {
        await using var context = new CashDrawerTestContext();

        try { await HandlerFor(context).HandleAsync(Command(amount: 0m)); } catch (CashEntryValidationException) { }

        context.Db.CashPayouts.Should()
                              .BeEmpty();
        context.OutboxEvents().Should()
                              .BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WithUndefinedCategory_Throws()
    {
        await using var context = new CashDrawerTestContext();

        var act = () => HandlerFor(context).HandleAsync(Command(category: (PayoutCategory)7));

        await act.Should()
                 .ThrowAsync<CashEntryValidationException>();
    }

    [Fact]
    public async Task HandleAsync_WithoutCategory_DefaultsToGeneral()
    {
        await using var context = new CashDrawerTestContext();

        var result = await HandlerFor(context).HandleAsync(Command(category: null));

        result.Category.Should()
                       .Be(PayoutCategory.General);
    }

    [Fact]
    public async Task HandleAsync_WithHardwareCategory_KeepsHardware()
    {
        await using var context = new CashDrawerTestContext();

        var result = await HandlerFor(context).HandleAsync(Command(category: PayoutCategory.Hardware));

        result.Category.Should()
                       .Be(PayoutCategory.Hardware);
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_StampsTodayFromTheServerClock()
    {
        await using var context = new CashDrawerTestContext();

        var result = await HandlerFor(context).HandleAsync(Command());

        result.BusinessDate.Should()
                           .Be(CashDrawerTestContext.Today);
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_RecordsTheCashierFromTheCommand()
    {
        await using var context = new CashDrawerTestContext();

        var result = await HandlerFor(context).HandleAsync(Command());

        result.CreatedByUserId.Should()
                              .Be(CashDrawerTestContext.CashierId);
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_WritesOnePayoutChangedEvent()
    {
        await using var context = new CashDrawerTestContext();

        await HandlerFor(context).HandleAsync(Command());

        context.OutboxEvents().Should()
                              .ContainSingle(e => e.Type == CashDrawerOutbox.CashPayoutChanged);
    }
}
