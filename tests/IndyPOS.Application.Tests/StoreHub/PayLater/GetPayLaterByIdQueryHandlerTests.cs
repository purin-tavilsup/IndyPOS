using FluentAssertions;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.PayLater;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

using PayLaterRepository = IndyPOS.Infrastructure.Persistence.StoreHub.Repositories.PayLaterRepository;

namespace IndyPOS.Application.Tests.StoreHub.PayLater;

public class GetPayLaterByIdQueryHandlerTests
{
    private static StoreHubDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<StoreHubDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new StoreHubDbContext(options);
    }

    private static Domain.Entities.Core.PayLater CreatePayLater(
        Guid? id = null,
        string description = "Test Customer",
        decimal payLaterAmount = 1000m,
        decimal paidAmount = 0m,
        bool isCompleted = false)
    {
        return new Domain.Entities.Core.PayLater
        {
            Id = id ?? Guid.NewGuid(),
            PaymentId = Guid.NewGuid(),
            InvoiceId = Guid.NewGuid(),
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
        var repository = new PayLaterRepository(dbContext);
        var handler = new GetPayLaterByIdQueryHandler(repository);

        var payLaterId = Guid.NewGuid();
        var payLater = CreatePayLater(
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
        var repository = new PayLaterRepository(dbContext);
        var handler = new GetPayLaterByIdQueryHandler(repository);

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
        var repository = new PayLaterRepository(dbContext);
        var handler = new GetPayLaterByIdQueryHandler(repository);

        var payLaterId = Guid.NewGuid();
        var payLater = CreatePayLater(
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
        var repository = new PayLaterRepository(dbContext);
        var handler = new GetPayLaterByIdQueryHandler(repository);

        var payLaterId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
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
}
