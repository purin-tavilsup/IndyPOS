using FluentAssertions;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Floats;
using IndyPOS.Domain.Entities.Core;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.CashDrawer.Floats;

public class CashFloatHandlersTests
{
    private static AddCashFloatCommandHandler AddHandler(CashDrawerTestContext c) =>
        new(c.EntryRepository<CashFloat>(), c.Clock, c.StoreIdentity);

    private static EditCashFloatCommandHandler EditHandler(CashDrawerTestContext c) =>
        new(c.EntryRepository<CashFloat>(), c.Clock);

    private static GetCashFloatsQueryHandler GetHandler(CashDrawerTestContext c) =>
        new(c.EntryRepository<CashFloat>(), c.Clock, c.StoreIdentity);

    private static async Task<CashFloat> SeedFloatAsync(CashDrawerTestContext c, DateOnly businessDate)
    {
        var cashFloat = new CashFloat
        {
            Id = Guid.NewGuid(), StoreId = c.StoreIdentity.StoreId, Amount = 500m, BusinessDate = businessDate,
            CreatedUtc = CashDrawerTestContext.TenAmBangkok.UtcDateTime,
            LastModifiedUtc = CashDrawerTestContext.TenAmBangkok.UtcDateTime,
            CreatedByUserId = CashDrawerTestContext.CashierId
        };
        c.Db.CashFloats.Add(cashFloat);
        await c.Db.SaveChangesAsync();
        c.Db.ChangeTracker.Clear();
        return cashFloat;
    }

    [Fact]
    public async Task Add_WithNegativeAmount_Throws()
    {
        await using var c = new CashDrawerTestContext();

        var act = () => AddHandler(c).HandleAsync(new AddCashFloatCommand(CashDrawerTestContext.CashierId, -5m, null));

        await act.Should()
                 .ThrowAsync<CashEntryValidationException>();
    }

    [Fact]
    public async Task Edit_WithUnknownId_Throws()
    {
        await using var c = new CashDrawerTestContext();

        var act = () => EditHandler(c).HandleAsync(new EditCashFloatCommand(Guid.NewGuid(), CashDrawerTestContext.CashierId, 10m, null));

        await act.Should()
                 .ThrowAsync<CashEntryNotFoundException>();
    }

    [Fact]
    public async Task Edit_WithYesterdaysFloat_Throws()
    {
        await using var c = new CashDrawerTestContext();
        var cashFloat = await SeedFloatAsync(c, CashDrawerTestContext.Yesterday);

        var act = () => EditHandler(c).HandleAsync(new EditCashFloatCommand(cashFloat.Id, CashDrawerTestContext.CashierId, 10m, null));

        await act.Should()
                 .ThrowAsync<CashDayClosedException>();
    }

    [Fact]
    public async Task Add_WithValidCommand_StampsToday()
    {
        await using var c = new CashDrawerTestContext();

        var result = await AddHandler(c).HandleAsync(new AddCashFloatCommand(CashDrawerTestContext.CashierId, 1000m, " ทอนเช้า "));

        result.BusinessDate.Should()
                           .Be(CashDrawerTestContext.Today);
    }

    [Fact]
    public async Task Add_WithValidCommand_WritesAFloatChangedEvent()
    {
        await using var c = new CashDrawerTestContext();

        await AddHandler(c).HandleAsync(new AddCashFloatCommand(CashDrawerTestContext.CashierId, 1000m, " ทอนเช้า "));

        c.OutboxEvents().Should()
                        .ContainSingle(e => e.Type == CashDrawerOutbox.CashFloatChanged);
    }

    [Fact]
    public async Task Edit_WithTodaysFloat_UpdatesTheAmount()
    {
        await using var c = new CashDrawerTestContext();
        var cashFloat = await SeedFloatAsync(c, CashDrawerTestContext.Today);

        var result = await EditHandler(c).HandleAsync(new EditCashFloatCommand(cashFloat.Id, CashDrawerTestContext.OtherCashierId, 700m, null));

        result.Amount.Should()
                     .Be(700m);
    }

    [Fact]
    public async Task Edit_WithTodaysFloat_RecordsTheEditor()
    {
        await using var c = new CashDrawerTestContext();
        var cashFloat = await SeedFloatAsync(c, CashDrawerTestContext.Today);

        var result = await EditHandler(c).HandleAsync(new EditCashFloatCommand(cashFloat.Id, CashDrawerTestContext.OtherCashierId, 700m, null));

        result.LastModifiedByUserId.Should()
                                   .Be(CashDrawerTestContext.OtherCashierId);
    }

    [Fact]
    public async Task Get_WithoutDate_ReturnsOnlyToday()
    {
        await using var c = new CashDrawerTestContext();
        await SeedFloatAsync(c, CashDrawerTestContext.Yesterday);
        var todays = await SeedFloatAsync(c, CashDrawerTestContext.Today);

        var result = await GetHandler(c).HandleAsync(new GetCashFloatsQuery(null));

        result.Select(f => f.Id).Should()
                                .Equal(todays.Id);
    }

    [Fact]
    public async Task Edit_WhenDeletedConcurrently_ThrowsNotFound()
    {
        await using var c = new CashDrawerTestContext();
        var cashFloat = ConcurrentDeleteRepository.ActiveToday<CashFloat>();
        var repository = ConcurrentDeleteRepository.For(cashFloat);
        var handler = new EditCashFloatCommandHandler(repository.Object, c.Clock);

        var act = () => handler.HandleAsync(new EditCashFloatCommand(cashFloat.Id, CashDrawerTestContext.CashierId, 10m, null));

        await act.Should()
                 .ThrowAsync<CashEntryNotFoundException>();
    }
}
