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

public class GetPayLaterByIdQueryHandlerTests
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
    public async Task HandleAsync_ShouldReturnPayLaterDto_WhenRecordExists()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var repository = new PayLaterRepository(dbContext, _storeIdentity);
        var handler = new GetPayLaterByIdQueryHandler(repository, _storeIdentity);

        var payLaterId = Guid.NewGuid();
        var payLater = CreatePayLater(
            dbContext,
            id: payLaterId,
            description: "John Doe",
            payLaterAmount: 1500m,
            paidAmount: 500m);

        dbContext.PayLaters.Add(payLater);
        await dbContext.SaveChangesAsync();

        var query = new GetPayLaterByIdQuery(payLaterId);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(payLaterId);
        result.Description.Should().Be("John Doe");
        result.PayLaterAmount.Should().Be(1500m);
        result.PaidAmount.Should().Be(500m);
        result.RemainingAmount.Should().Be(1000m);
        result.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowPayLaterPaymentNotFoundException_WhenRecordDoesNotExist()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var repository = new PayLaterRepository(dbContext, _storeIdentity);
        var handler = new GetPayLaterByIdQueryHandler(repository, _storeIdentity);

        var nonExistentId = Guid.NewGuid();
        var query = new GetPayLaterByIdQuery(nonExistentId);

        // Act & Assert
        await handler.Invoking(h => h.HandleAsync(query))
            .Should().ThrowAsync<PayLaterPaymentNotFoundException>()
            .WithMessage($"*{nonExistentId}*");
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnCompletedPayLater()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var repository = new PayLaterRepository(dbContext, _storeIdentity);
        var handler = new GetPayLaterByIdQueryHandler(repository, _storeIdentity);

        var payLaterId = Guid.NewGuid();
        var payLater = CreatePayLater(
            dbContext,
            id: payLaterId,
            payLaterAmount: 1000m,
            paidAmount: 1000m,
            isCompleted: true);

        dbContext.PayLaters.Add(payLater);
        await dbContext.SaveChangesAsync();

        var query = new GetPayLaterByIdQuery(payLaterId);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.IsCompleted.Should().BeTrue();
        result.RemainingAmount.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_ShouldMapAllFieldsCorrectly()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var repository = new PayLaterRepository(dbContext, _storeIdentity);
        var handler = new GetPayLaterByIdQueryHandler(repository, _storeIdentity);

        var payLaterId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var invoiceId = CreateTestInvoice(dbContext);
        var createdUtc = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var lastModifiedUtc = new DateTime(2024, 6, 20, 15, 30, 0, DateTimeKind.Utc);

        var payLater = new Domain.Entities.Core.PayLater
        {
            Id = payLaterId,
            PaymentId = paymentId,
            InvoiceId = invoiceId,
            Description = "Full Test",
            PayLaterAmount = 2000m,
            PaidAmount = 750m,
            IsCompleted = false,
            CreatedUtc = createdUtc,
            LastModifiedUtc = lastModifiedUtc
        };

        dbContext.PayLaters.Add(payLater);
        await dbContext.SaveChangesAsync();

        var query = new GetPayLaterByIdQuery(payLaterId);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.Id.Should().Be(payLaterId);
        result.PaymentId.Should().Be(paymentId);
        result.InvoiceId.Should().Be(invoiceId);
        result.Description.Should().Be("Full Test");
        result.PayLaterAmount.Should().Be(2000m);
        result.PaidAmount.Should().Be(750m);
        result.RemainingAmount.Should().Be(1250m);
        result.IsCompleted.Should().BeFalse();
        result.CreatedUtc.Should().Be(createdUtc);
        result.LastModifiedUtc.Should().Be(lastModifiedUtc);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowException_WhenPayLaterDisabledForStoreType()
    {
        // Arrange
        var minimartStoreIdentity = MockStoreIdentityService.Minimart();
        await using var dbContext = CreateDbContext();
        var repository = new PayLaterRepository(dbContext, minimartStoreIdentity);
        var handler = new GetPayLaterByIdQueryHandler(repository, minimartStoreIdentity);

        var payLaterId = Guid.NewGuid();
        var query = new GetPayLaterByIdQuery(payLaterId);

        // Act & Assert
        await handler.Invoking(h => h.HandleAsync(query))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*PayLater is not available*Minimart*");
    }
}
