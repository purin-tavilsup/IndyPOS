using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.PayLater;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

using PayLaterRepository = IndyPOS.Infrastructure.Persistence.StoreHub.Repositories.PayLaterRepository;

namespace IndyPOS.Application.Tests.StoreHub.PayLater;

public class GetPayLaterQueryHandlerTests
{
    private static StoreHubDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<StoreHubDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new StoreHubDbContext(options);
    }

    private static Domain.Entities.Core.PayLater CreatePayLater(
        string description,
        decimal payLaterAmount,
        decimal paidAmount = 0,
        bool isCompleted = false)
    {
        return new Domain.Entities.Core.PayLater
        {
            Id = Guid.NewGuid(),
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
    public async Task HandleAsync_ShouldReturnAllPayLaterRecords_WhenIncludeCompletedIsTrue()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var repository = new PayLaterRepository(dbContext);
        var handler = new GetPayLaterQueryHandler(repository);

        var active1 = CreatePayLater("Customer A", 1000m, 500m, false);
        var active2 = CreatePayLater("Customer B", 2000m, 0m, false);
        var completed = CreatePayLater("Customer C", 500m, 500m, true);

        dbContext.PayLaters.AddRange(active1, active2, completed);
        await dbContext.SaveChangesAsync();

        var query = new GetPayLaterQuery(IncludeCompleted: true);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.Items.Should().HaveCount(3);
        result.ActiveCount.Should().Be(2);
        result.CompletedCount.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnOnlyActiveRecords_WhenIncludeCompletedIsFalse()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var repository = new PayLaterRepository(dbContext);
        var handler = new GetPayLaterQueryHandler(repository);

        var active = CreatePayLater("Customer A", 1000m, 500m, false);
        var completed = CreatePayLater("Customer B", 500m, 500m, true);

        dbContext.PayLaters.AddRange(active, completed);
        await dbContext.SaveChangesAsync();

        var query = new GetPayLaterQuery(IncludeCompleted: false);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Description.Should().Be("Customer A");
    }

    [Fact]
    public async Task HandleAsync_ShouldFilterBySearchTerm()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var repository = new PayLaterRepository(dbContext);
        var handler = new GetPayLaterQueryHandler(repository);

        var customer1 = CreatePayLater("John Doe", 1000m);
        var customer2 = CreatePayLater("Jane Smith", 2000m);
        var customer3 = CreatePayLater("Johnny Walker", 1500m);

        dbContext.PayLaters.AddRange(customer1, customer2, customer3);
        await dbContext.SaveChangesAsync();

        var query = new GetPayLaterQuery(IncludeCompleted: true, SearchTerm: "John");

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(x => x.Description.Should().Contain("John"));
    }

    [Fact]
    public async Task HandleAsync_ShouldCalculateTotalsCorrectly()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var repository = new PayLaterRepository(dbContext);
        var handler = new GetPayLaterQueryHandler(repository);

        // Active: 1000 - 300 = 700 remaining
        var active1 = CreatePayLater("Customer A", 1000m, 300m, false);
        // Active: 2000 - 500 = 1500 remaining
        var active2 = CreatePayLater("Customer B", 2000m, 500m, false);
        // Completed: 500 paid
        var completed = CreatePayLater("Customer C", 500m, 500m, true);

        dbContext.PayLaters.AddRange(active1, active2, completed);
        await dbContext.SaveChangesAsync();

        var query = new GetPayLaterQuery(IncludeCompleted: true);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.TotalOutstanding.Should().Be(2200m); // 700 + 1500
        result.TotalPaid.Should().Be(1300m); // 300 + 500 + 500
        result.ActiveCount.Should().Be(2);
        result.CompletedCount.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnEmptyResponse_WhenNoRecordsExist()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var repository = new PayLaterRepository(dbContext);
        var handler = new GetPayLaterQueryHandler(repository);

        var query = new GetPayLaterQuery();

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalOutstanding.Should().Be(0);
        result.TotalPaid.Should().Be(0);
        result.ActiveCount.Should().Be(0);
        result.CompletedCount.Should().Be(0);
    }
}
