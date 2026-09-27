using FluentAssertions;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.DebtRepayments;
using IndyPOS.Domain.Entities.Core;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.CashDrawer.DebtRepayments;

public class DebtRepaymentHandlersTests
{
    private const string CustomerName = "ลุงสมชาย";

    private static AddDebtRepaymentCommandHandler AddHandler(CashDrawerTestContext c) =>
        new(c.EntryRepository<DebtRepayment>(), c.Clock, c.StoreIdentity);

    private static EditDebtRepaymentCommandHandler EditHandler(CashDrawerTestContext c) =>
        new(c.EntryRepository<DebtRepayment>(), c.Clock);

    private static GetDebtRepaymentsQueryHandler GetHandler(CashDrawerTestContext c) =>
        new(c.EntryRepository<DebtRepayment>(), c.Clock, c.StoreIdentity);

    private static async Task<DebtRepayment> SeedRepaymentAsync(CashDrawerTestContext c, DateOnly businessDate)
    {
        var repayment = new DebtRepayment
        {
            Id = Guid.NewGuid(), StoreId = c.StoreIdentity.StoreId, CustomerName = CustomerName, Amount = 300m,
            BusinessDate = businessDate,
            CreatedUtc = CashDrawerTestContext.TenAmBangkok.UtcDateTime,
            LastModifiedUtc = CashDrawerTestContext.TenAmBangkok.UtcDateTime,
            CreatedByUserId = CashDrawerTestContext.CashierId
        };
        c.Db.DebtRepayments.Add(repayment);
        await c.Db.SaveChangesAsync();
        c.Db.ChangeTracker.Clear();
        return repayment;
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Add_WithBlankCustomerName_Throws(string? customerName)
    {
        await using var c = new CashDrawerTestContext();

        var act = () => AddHandler(c).HandleAsync(new AddDebtRepaymentCommand(CashDrawerTestContext.CashierId, customerName, 300m));

        await act.Should()
                 .ThrowAsync<CashEntryValidationException>();
    }

    [Fact]
    public async Task Add_WithZeroAmount_Throws()
    {
        await using var c = new CashDrawerTestContext();

        var act = () => AddHandler(c).HandleAsync(new AddDebtRepaymentCommand(CashDrawerTestContext.CashierId, CustomerName, 0m));

        await act.Should()
                 .ThrowAsync<CashEntryValidationException>();
    }

    [Fact]
    public async Task Edit_WithBlankCustomerName_Throws()
    {
        await using var c = new CashDrawerTestContext();
        var repayment = await SeedRepaymentAsync(c, CashDrawerTestContext.Today);

        var act = () => EditHandler(c).HandleAsync(new EditDebtRepaymentCommand(repayment.Id, CashDrawerTestContext.CashierId, " ", 300m));

        await act.Should()
                 .ThrowAsync<CashEntryValidationException>();
    }

    [Fact]
    public async Task Edit_WithYesterdaysRepayment_Throws()
    {
        await using var c = new CashDrawerTestContext();
        var repayment = await SeedRepaymentAsync(c, CashDrawerTestContext.Yesterday);

        var act = () => EditHandler(c).HandleAsync(new EditDebtRepaymentCommand(repayment.Id, CashDrawerTestContext.CashierId, CustomerName, 300m));

        await act.Should()
                 .ThrowAsync<CashDayClosedException>();
    }

    [Fact]
    public async Task Add_WithValidCommand_TrimsTheCustomerName()
    {
        await using var c = new CashDrawerTestContext();

        var result = await AddHandler(c).HandleAsync(new AddDebtRepaymentCommand(CashDrawerTestContext.CashierId, "  ลุงสมชาย ", 300m));

        result.CustomerName.Should()
                           .Be(CustomerName);
    }

    [Fact]
    public async Task Add_WithValidCommand_WritesARepaymentChangedEvent()
    {
        await using var c = new CashDrawerTestContext();

        await AddHandler(c).HandleAsync(new AddDebtRepaymentCommand(CashDrawerTestContext.CashierId, "  ลุงสมชาย ", 300m));

        c.OutboxEvents().Should()
                        .ContainSingle(e => e.Type == CashDrawerOutbox.DebtRepaymentChanged);
    }

    [Fact]
    public async Task Get_WithoutDate_ReturnsOnlyToday()
    {
        await using var c = new CashDrawerTestContext();
        await SeedRepaymentAsync(c, CashDrawerTestContext.Yesterday);
        var todays = await SeedRepaymentAsync(c, CashDrawerTestContext.Today);

        var result = await GetHandler(c).HandleAsync(new GetDebtRepaymentsQuery(null));

        result.Select(r => r.Id).Should()
                                .Equal(todays.Id);
    }

    [Fact]
    public async Task Edit_WhenDeletedConcurrently_ThrowsNotFound()
    {
        await using var c = new CashDrawerTestContext();
        var repayment = ConcurrentDeleteRepository.ActiveToday<DebtRepayment>();
        var repository = ConcurrentDeleteRepository.For(repayment);
        var handler = new EditDebtRepaymentCommandHandler(repository.Object, c.Clock);

        var act = () => handler.HandleAsync(new EditDebtRepaymentCommand(repayment.Id, CashDrawerTestContext.CashierId, CustomerName, 300m));

        await act.Should()
                 .ThrowAsync<CashEntryNotFoundException>();
    }
}
