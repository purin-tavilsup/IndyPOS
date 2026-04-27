using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetInvoices;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using IndyPOS.Infrastructure.QueryHandlers.Reports;
using IndyPOS.Mock;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Reports;

public class GetInvoicesQueryHandlerTests
{
    private static readonly MockStoreIdentityService StoreIdentity = MockStoreIdentityService.GeneralHardware();

    private static StoreHubDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<StoreHubDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new StoreHubDbContext(options);
    }

    private static Invoice CreateInvoice(DateTime createdUtc, decimal totalAmount, string paymentMethod = "Cash")
    {
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            StoreId = "STORE1",
            UserId = Guid.NewGuid(),
            TotalAmount = totalAmount,
            CreatedUtc = createdUtc,
            LastModifiedUtc = createdUtc,
            Lines = new List<InvoiceLine>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = Guid.NewGuid(),
                    ProductName = "Test Product",
                    Quantity = 1,
                    UnitPrice = totalAmount
                    // LineTotal is calculated: UnitPrice * Quantity
                }
            },
            Payments = new List<Payment>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Method = paymentMethod,
                    Amount = totalAmount,
                    CreatedUtc = createdUtc
                }
            }
        };

        invoice.Lines.First().InvoiceId = invoice.Id;
        invoice.Payments.First().InvoiceId = invoice.Id;

        return invoice;
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnPaginatedInvoices()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var handler = new GetInvoicesQueryHandler(dbContext, StoreIdentity, NullLogger<GetInvoicesQueryHandler>.Instance);

        for (var i = 0; i < 5; i++)
        {
            var invoice = CreateInvoice(
                new DateTime(2024, 6, 15 + i, 10, 0, 0, DateTimeKind.Utc),
                100m + i * 10);
            dbContext.Invoices.Add(invoice);
        }
        await dbContext.SaveChangesAsync();

        var query = new GetInvoicesQuery(
            FromDate: new DateOnly(2024, 6, 1),
            ToDate: new DateOnly(2024, 6, 30),
            Page: 1,
            PageSize: 2);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
        result.TotalPages.Should().Be(3);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldOrderByCreatedUtcDescending()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var handler = new GetInvoicesQueryHandler(dbContext, StoreIdentity, NullLogger<GetInvoicesQueryHandler>.Instance);

        var invoice1 = CreateInvoice(new DateTime(2024, 6, 10, 10, 0, 0, DateTimeKind.Utc), 100m);
        var invoice2 = CreateInvoice(new DateTime(2024, 6, 20, 10, 0, 0, DateTimeKind.Utc), 200m);
        var invoice3 = CreateInvoice(new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc), 150m);

        dbContext.Invoices.AddRange(invoice1, invoice2, invoice3);
        await dbContext.SaveChangesAsync();

        var query = new GetInvoicesQuery(
            FromDate: new DateOnly(2024, 6, 1),
            ToDate: new DateOnly(2024, 6, 30));

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].TotalAmount.Should().Be(200m); // Most recent
        result.Items[1].TotalAmount.Should().Be(150m);
        result.Items[2].TotalAmount.Should().Be(100m); // Oldest
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnPrimaryPaymentMethod()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var handler = new GetInvoicesQueryHandler(dbContext, StoreIdentity, NullLogger<GetInvoicesQueryHandler>.Instance);

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            StoreId = "STORE1",
            UserId = Guid.NewGuid(),
            TotalAmount = 200m,
            CreatedUtc = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc),
            LastModifiedUtc = DateTime.UtcNow,
            Lines = new List<InvoiceLine>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = Guid.NewGuid(),
                    ProductName = "Test",
                    Quantity = 1,
                    UnitPrice = 200m
                }
            },
            Payments = new List<Payment>
            {
                new() { Id = Guid.NewGuid(), Method = "Cash", Amount = 50m, CreatedUtc = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), Method = "Card", Amount = 150m, CreatedUtc = DateTime.UtcNow } // Largest
            }
        };

        foreach (var line in invoice.Lines) line.InvoiceId = invoice.Id;
        foreach (var payment in invoice.Payments) payment.InvoiceId = invoice.Id;

        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync();

        var query = new GetInvoicesQuery(
            FromDate: new DateOnly(2024, 6, 1),
            ToDate: new DateOnly(2024, 6, 30));

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].PrimaryPaymentMethod.Should().Be("Card");
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnCorrectLineCount()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var handler = new GetInvoicesQueryHandler(dbContext, StoreIdentity, NullLogger<GetInvoicesQueryHandler>.Instance);

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            StoreId = "STORE1",
            UserId = Guid.NewGuid(),
            TotalAmount = 300m,
            CreatedUtc = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc),
            LastModifiedUtc = DateTime.UtcNow,
            Lines = new List<InvoiceLine>
            {
                new() { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), ProductName = "A", Quantity = 1, UnitPrice = 100 },
                new() { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), ProductName = "B", Quantity = 1, UnitPrice = 100 },
                new() { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), ProductName = "C", Quantity = 1, UnitPrice = 100 }
            },
            Payments = new List<Payment>
            {
                new() { Id = Guid.NewGuid(), Method = "Cash", Amount = 300m, CreatedUtc = DateTime.UtcNow }
            }
        };

        foreach (var line in invoice.Lines) line.InvoiceId = invoice.Id;
        foreach (var payment in invoice.Payments) payment.InvoiceId = invoice.Id;

        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync();

        var query = new GetInvoicesQuery(
            FromDate: new DateOnly(2024, 6, 1),
            ToDate: new DateOnly(2024, 6, 30));

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].LineCount.Should().Be(3);
    }
}
