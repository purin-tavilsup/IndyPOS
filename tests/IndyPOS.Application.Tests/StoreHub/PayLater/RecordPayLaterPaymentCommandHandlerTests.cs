using FluentAssertions;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.PayLater;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;
using IndyPOS.Mock;
using Microsoft.EntityFrameworkCore;
using Xunit;

using PayLaterRepository = IndyPOS.Infrastructure.Persistence.StoreHub.Repositories.PayLaterRepository;

namespace IndyPOS.Application.Tests.StoreHub.PayLater;

public class RecordPayLaterPaymentCommandHandlerTests
{
    private static readonly MockStoreIdentityService _storeIdentity = MockStoreIdentityService.GeneralHardware();

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
        var handler = new RecordPayLaterPaymentCommandHandler(repository, _storeIdentity);

        var payLaterId = Guid.NewGuid();
        var payLater = CreatePayLater(
            dbContext,
            id: payLaterId,
            payLaterAmount: 1000m,
            paidAmount: 0m);

        dbContext.PayLaters.Add(payLater);
        await dbContext.SaveChangesAsync();

        var command = new RecordPayLaterPaymentCommand(payLaterId, PaymentAmount: 300m);

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
        var handler = new RecordPayLaterPaymentCommandHandler(repository, _storeIdentity);

        var payLaterId = Guid.NewGuid();
        var payLater = CreatePayLater(
            dbContext,
            id: payLaterId,
            payLaterAmount: 1000m,
            paidAmount: 500m);

        dbContext.PayLaters.Add(payLater);
        await dbContext.SaveChangesAsync();

        var command = new RecordPayLaterPaymentCommand(payLaterId, PaymentAmount: 500m);

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
        var handler = new RecordPayLaterPaymentCommandHandler(repository, _storeIdentity);

        var payLaterId = Guid.NewGuid();
        var payLater = CreatePayLater(
            dbContext,
            id: payLaterId,
            payLaterAmount: 1000m,
            paidAmount: 800m);

        dbContext.PayLaters.Add(payLater);
        await dbContext.SaveChangesAsync();

        // Paying 300 when only 200 is owed
        var command = new RecordPayLaterPaymentCommand(payLaterId, PaymentAmount: 300m);

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
        var handler = new RecordPayLaterPaymentCommandHandler(repository, _storeIdentity);

        var nonExistentId = Guid.NewGuid();
        var command = new RecordPayLaterPaymentCommand(nonExistentId, PaymentAmount: 100m);

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
        var handler = new RecordPayLaterPaymentCommandHandler(repository, _storeIdentity);

        var payLaterId = Guid.NewGuid();
        var payLater = CreatePayLater(
            dbContext,
            id: payLaterId,
            payLaterAmount: 1000m,
            paidAmount: 1000m,
            isCompleted: true);

        dbContext.PayLaters.Add(payLater);
        await dbContext.SaveChangesAsync();

        var command = new RecordPayLaterPaymentCommand(payLaterId, PaymentAmount: 100m);

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
        var handler = new RecordPayLaterPaymentCommandHandler(repository, _storeIdentity);

        var payLaterId = Guid.NewGuid();
        var payLater = CreatePayLater(dbContext, id: payLaterId);

        dbContext.PayLaters.Add(payLater);
        await dbContext.SaveChangesAsync();

        var command = new RecordPayLaterPaymentCommand(payLaterId, PaymentAmount: invalidAmount);

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
        var handler = new RecordPayLaterPaymentCommandHandler(repository, _storeIdentity);

        var payLaterId = Guid.NewGuid();
        var originalDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var payLater = CreatePayLater(dbContext, id: payLaterId, payLaterAmount: 1000m);
        payLater.LastModifiedUtc = originalDate;

        dbContext.PayLaters.Add(payLater);
        await dbContext.SaveChangesAsync();

        var command = new RecordPayLaterPaymentCommand(payLaterId, PaymentAmount: 100m);

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
        var handler = new RecordPayLaterPaymentCommandHandler(repository, _storeIdentity);

        var payLaterId = Guid.NewGuid();
        var payLater = CreatePayLater(
            dbContext,
            id: payLaterId,
            payLaterAmount: 1000m,
            paidAmount: 0m);

        dbContext.PayLaters.Add(payLater);
        await dbContext.SaveChangesAsync();

        var command = new RecordPayLaterPaymentCommand(payLaterId, PaymentAmount: 500m);

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
        var handler = new RecordPayLaterPaymentCommandHandler(repository, _storeIdentity);

        var payLaterId = Guid.NewGuid();
        var payLater = CreatePayLater(
            dbContext,
            id: payLaterId,
            payLaterAmount: 1000m,
            paidAmount: 0m);

        dbContext.PayLaters.Add(payLater);
        await dbContext.SaveChangesAsync();

        // Act - make multiple payments
        await handler.HandleAsync(new RecordPayLaterPaymentCommand(payLaterId, 200m));
        await handler.HandleAsync(new RecordPayLaterPaymentCommand(payLaterId, 300m));
        var result = await handler.HandleAsync(new RecordPayLaterPaymentCommand(payLaterId, 500m));

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
        var handler = new RecordPayLaterPaymentCommandHandler(repository, minimartStoreIdentity);

        var payLaterId = Guid.NewGuid();
        var command = new RecordPayLaterPaymentCommand(payLaterId, PaymentAmount: 100m);

        // Act & Assert
        await handler.Invoking(h => h.HandleAsync(command))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*PayLater is not available*Minimart*");
    }
}
