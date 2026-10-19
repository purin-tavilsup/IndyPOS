using FluentAssertions;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Application.UseCases.StoreHub.PayLater;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;
using IndyPOS.Mock;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;

using PayLaterRepository = IndyPOS.Infrastructure.Persistence.StoreHub.Repositories.PayLaterRepository;

namespace IndyPOS.Application.Tests.StoreHub.PayLater;

public class RecordPayLaterPaymentCommandHandlerTests
{
    private static readonly MockStoreIdentityService _storeIdentity = BangkokGeneralHardware();
    private static readonly Guid Cashier = Guid.Parse("11111111-1111-1111-1111-111111111111");

    // 17:30 UTC is 00:30 the next day in Bangkok, so a UTC-day business date would be caught.
    private static readonly DateTimeOffset HalfPastMidnightBangkok = new(2026, 10, 19, 17, 30, 0, TimeSpan.Zero);
    private static readonly DateOnly BangkokToday = new(2026, 10, 20);

    private static MockStoreIdentityService BangkokGeneralHardware()
    {
        var identity = MockStoreIdentityService.GeneralHardware();
        identity.TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");
        return identity;
    }

    private static RecordPayLaterPaymentCommandHandler CreateHandler(PayLaterRepository repository, IStoreIdentityService identity) =>
        new(repository, identity, new CashDrawerClock(new FakeTimeProvider(HalfPastMidnightBangkok), identity));

    /// <summary>A database holding one debt of 1,000 with <paramref name="paidAmount"/> already paid.</summary>
    private static async Task<(StoreHubDbContext Db, RecordPayLaterPaymentCommandHandler Handler, Guid DebtId)> ArrangeDebtAsync(
        decimal paidAmount = 0m, bool isCompleted = false)
    {
        var dbContext = CreateDbContext();
        var debt = CreatePayLater(dbContext, payLaterAmount: 1000m, paidAmount: paidAmount, isCompleted: isCompleted);
        dbContext.PayLaters.Add(debt);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        return (dbContext, CreateHandler(new PayLaterRepository(dbContext, _storeIdentity), _storeIdentity), debt.Id);
    }

    [Fact]
    public async Task HandleAsync_ForACompletedDebt_WritesNoRepayment()
    {
        var (db, handler, debtId) = await ArrangeDebtAsync(paidAmount: 1000m, isCompleted: true);
        await using var _ = db;

        await handler.Invoking(h => h.HandleAsync(new RecordPayLaterPaymentCommand(debtId, 100m, Cashier)))
                     .Should()
                     .ThrowAsync<PayLaterPaymentNotUpdatedException>();

        db.PayLaterRepayments.Should()
                             .BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ForACompletedDebt_WritesNoEvent()
    {
        var (db, handler, debtId) = await ArrangeDebtAsync(paidAmount: 1000m, isCompleted: true);
        await using var _ = db;

        await handler.Invoking(h => h.HandleAsync(new RecordPayLaterPaymentCommand(debtId, 100m, Cashier)))
                     .Should()
                     .ThrowAsync<PayLaterPaymentNotUpdatedException>();

        db.OutboxEvents.Should()
                       .BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WithAZeroAmount_WritesNoRepayment()
    {
        var (db, handler, debtId) = await ArrangeDebtAsync();
        await using var _ = db;

        await handler.Invoking(h => h.HandleAsync(new RecordPayLaterPaymentCommand(debtId, 0m, Cashier)))
                     .Should()
                     .ThrowAsync<ArgumentException>();

        db.PayLaterRepayments.Should()
                             .BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ForAnOpenDebt_RecordsTheCashier()
    {
        var (db, handler, debtId) = await ArrangeDebtAsync();
        await using var _ = db;

        await handler.HandleAsync(new RecordPayLaterPaymentCommand(debtId, 300m, Cashier));

        db.PayLaterRepayments.Single().Should()
                                      .BeEquivalentTo(new { PayLaterId = debtId, Amount = 300m, CreatedByUserId = Cashier, _storeIdentity.StoreId });
    }

    [Fact]
    public async Task HandleAsync_ForAnOpenDebt_FilesItOnTodaysBusinessDate()
    {
        var (db, handler, debtId) = await ArrangeDebtAsync();
        await using var _ = db;

        await handler.HandleAsync(new RecordPayLaterPaymentCommand(debtId, 300m, Cashier));

        db.PayLaterRepayments.Single().BusinessDate.Should()
                                                   .Be(BangkokToday);
    }

    [Fact]
    public async Task HandleAsync_ForAnOpenDebt_WritesARepaidAndAChangedEvent()
    {
        var (db, handler, debtId) = await ArrangeDebtAsync();
        await using var _ = db;

        await handler.HandleAsync(new RecordPayLaterPaymentCommand(debtId, 300m, Cashier));

        db.OutboxEvents.Select(e => e.Type).Should()
                                           .BeEquivalentTo(PayLaterOutbox.PayLaterRepaid, PayLaterOutbox.PayLaterChanged);
    }

    [Fact]
    public async Task HandleAsync_PayingTheRest_QueuesTheDebtAsCompleted()
    {
        var (db, handler, debtId) = await ArrangeDebtAsync(paidAmount: 700m);
        await using var _ = db;

        await handler.HandleAsync(new RecordPayLaterPaymentCommand(debtId, 300m, Cashier));

        var changed = db.OutboxEvents.Single(e => e.Type == PayLaterOutbox.PayLaterChanged);
        System.Text.Json.JsonSerializer.Deserialize<PayLaterChangedPayload>(changed.PayloadJson)!.IsCompleted.Should()
                                                                                                         .BeTrue();
    }

    private static StoreHubDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<StoreHubDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new StoreHubDbContext(options);
    }

    private static Guid CreateTestInvoice(StoreHubDbContext dbContext)
    {
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            StoreId = _storeIdentity.StoreId,
            UserId = Guid.NewGuid(),
            TotalAmount = 1000m,
            CreatedUtc = DateTime.UtcNow,
            LastModifiedUtc = DateTime.UtcNow
        };
        dbContext.Invoices.Add(invoice);
        return invoice.Id;
    }

    private static Domain.Entities.Core.PayLater CreatePayLater(
        StoreHubDbContext dbContext,
        Guid? id = null,
        string description = "Test Customer",
        decimal payLaterAmount = 1000m,
        decimal paidAmount = 0m,
        bool isCompleted = false)
    {
        var invoiceId = CreateTestInvoice(dbContext);
        return new Domain.Entities.Core.PayLater
        {
            Id = id ?? Guid.NewGuid(),
            PaymentId = Guid.NewGuid(),
            InvoiceId = invoiceId,
            Description = description,
            PayLaterAmount = payLaterAmount,
            PaidAmount = paidAmount,
            IsCompleted = isCompleted,
            CreatedUtc = DateTime.UtcNow,
            LastModifiedUtc = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task HandleAsync_ShouldRecordPartialPayment()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var repository = new PayLaterRepository(dbContext, _storeIdentity);
        var handler = CreateHandler(repository, _storeIdentity);

        var payLaterId = Guid.NewGuid();
        var payLater = CreatePayLater(
            dbContext,
            id: payLaterId,
            payLaterAmount: 1000m,
            paidAmount: 0m);

        dbContext.PayLaters.Add(payLater);
        await dbContext.SaveChangesAsync();

        var command = new RecordPayLaterPaymentCommand(payLaterId, PaymentAmount: 300m, Cashier);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.PaidAmount.Should().Be(300m);
        result.RemainingAmount.Should().Be(700m);
        result.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldMarkAsCompleted_WhenFullAmountPaid()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var repository = new PayLaterRepository(dbContext, _storeIdentity);
        var handler = CreateHandler(repository, _storeIdentity);

        var payLaterId = Guid.NewGuid();
        var payLater = CreatePayLater(
            dbContext,
            id: payLaterId,
            payLaterAmount: 1000m,
            paidAmount: 500m);

        dbContext.PayLaters.Add(payLater);
        await dbContext.SaveChangesAsync();

        var command = new RecordPayLaterPaymentCommand(payLaterId, PaymentAmount: 500m, Cashier);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.PaidAmount.Should().Be(1000m);
        result.RemainingAmount.Should().Be(0);
        result.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ShouldMarkAsCompleted_WhenOverpaying()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var repository = new PayLaterRepository(dbContext, _storeIdentity);
        var handler = CreateHandler(repository, _storeIdentity);

        var payLaterId = Guid.NewGuid();
        var payLater = CreatePayLater(
            dbContext,
            id: payLaterId,
            payLaterAmount: 1000m,
            paidAmount: 800m);

        dbContext.PayLaters.Add(payLater);
        await dbContext.SaveChangesAsync();

        // Paying 300 when only 200 is owed
        var command = new RecordPayLaterPaymentCommand(payLaterId, PaymentAmount: 300m, Cashier);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.PaidAmount.Should().Be(1100m); // Overpaid
        result.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowPayLaterPaymentNotFoundException_WhenRecordDoesNotExist()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var repository = new PayLaterRepository(dbContext, _storeIdentity);
        var handler = CreateHandler(repository, _storeIdentity);

        var nonExistentId = Guid.NewGuid();
        var command = new RecordPayLaterPaymentCommand(nonExistentId, PaymentAmount: 100m, Cashier);

        // Act & Assert
        await handler.Invoking(h => h.HandleAsync(command))
            .Should().ThrowAsync<PayLaterPaymentNotFoundException>()
            .WithMessage($"*{nonExistentId}*");
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowPayLaterPaymentNotUpdatedException_WhenAlreadyCompleted()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var repository = new PayLaterRepository(dbContext, _storeIdentity);
        var handler = CreateHandler(repository, _storeIdentity);

        var payLaterId = Guid.NewGuid();
        var payLater = CreatePayLater(
            dbContext,
            id: payLaterId,
            payLaterAmount: 1000m,
            paidAmount: 1000m,
            isCompleted: true);

        dbContext.PayLaters.Add(payLater);
        await dbContext.SaveChangesAsync();

        var command = new RecordPayLaterPaymentCommand(payLaterId, PaymentAmount: 100m, Cashier);

        // Act & Assert
        await handler.Invoking(h => h.HandleAsync(command))
            .Should().ThrowAsync<PayLaterPaymentNotUpdatedException>()
            .WithMessage("*already completed*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    [InlineData(-0.01)]
    public async Task HandleAsync_ShouldThrowArgumentException_WhenPaymentAmountIsNotPositive(decimal invalidAmount)
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var repository = new PayLaterRepository(dbContext, _storeIdentity);
        var handler = CreateHandler(repository, _storeIdentity);

        var payLaterId = Guid.NewGuid();
        var payLater = CreatePayLater(dbContext, id: payLaterId);

        dbContext.PayLaters.Add(payLater);
        await dbContext.SaveChangesAsync();

        var command = new RecordPayLaterPaymentCommand(payLaterId, PaymentAmount: invalidAmount, Cashier);

        // Act & Assert
        await handler.Invoking(h => h.HandleAsync(command))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Payment amount must be positive*");
    }

    [Fact]
    public async Task HandleAsync_ShouldUpdateLastModifiedUtc()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var repository = new PayLaterRepository(dbContext, _storeIdentity);
        var handler = CreateHandler(repository, _storeIdentity);

        var payLaterId = Guid.NewGuid();
        var originalDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var payLater = CreatePayLater(dbContext, id: payLaterId, payLaterAmount: 1000m);
        payLater.LastModifiedUtc = originalDate;

        dbContext.PayLaters.Add(payLater);
        await dbContext.SaveChangesAsync();

        var command = new RecordPayLaterPaymentCommand(payLaterId, PaymentAmount: 100m, Cashier);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.LastModifiedUtc.Should().BeAfter(originalDate);
    }

    [Fact]
    public async Task HandleAsync_ShouldPersistChangesToDatabase()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var repository = new PayLaterRepository(dbContext, _storeIdentity);
        var handler = CreateHandler(repository, _storeIdentity);

        var payLaterId = Guid.NewGuid();
        var payLater = CreatePayLater(
            dbContext,
            id: payLaterId,
            payLaterAmount: 1000m,
            paidAmount: 0m);

        dbContext.PayLaters.Add(payLater);
        await dbContext.SaveChangesAsync();

        var command = new RecordPayLaterPaymentCommand(payLaterId, PaymentAmount: 500m, Cashier);

        // Act
        await handler.HandleAsync(command);

        // Assert - verify directly in database
        var updatedPayLater = await dbContext.PayLaters.FindAsync(payLaterId);
        updatedPayLater.Should().NotBeNull();
        updatedPayLater!.PaidAmount.Should().Be(500m);
        updatedPayLater.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldAccumulateMultiplePayments()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var repository = new PayLaterRepository(dbContext, _storeIdentity);
        var handler = CreateHandler(repository, _storeIdentity);

        var payLaterId = Guid.NewGuid();
        var payLater = CreatePayLater(
            dbContext,
            id: payLaterId,
            payLaterAmount: 1000m,
            paidAmount: 0m);

        dbContext.PayLaters.Add(payLater);
        await dbContext.SaveChangesAsync();

        // Act - make multiple payments
        await handler.HandleAsync(new RecordPayLaterPaymentCommand(payLaterId, 200m, Cashier));
        await handler.HandleAsync(new RecordPayLaterPaymentCommand(payLaterId, 300m, Cashier));
        var result = await handler.HandleAsync(new RecordPayLaterPaymentCommand(payLaterId, 500m, Cashier));

        // Assert
        result.PaidAmount.Should().Be(1000m);
        result.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowException_WhenPayLaterDisabledForStoreType()
    {
        // Arrange
        var minimartStoreIdentity = MockStoreIdentityService.Minimart();
        await using var dbContext = CreateDbContext();
        var repository = new PayLaterRepository(dbContext, minimartStoreIdentity);
        var handler = CreateHandler(repository, minimartStoreIdentity);

        var payLaterId = Guid.NewGuid();
        var command = new RecordPayLaterPaymentCommand(payLaterId, PaymentAmount: 100m, Cashier);

        // Act & Assert
        await handler.Invoking(h => h.HandleAsync(command))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*PayLater is not available*Minimart*");
    }
}
