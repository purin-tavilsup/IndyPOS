using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetSalesSummary;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using IndyPOS.Infrastructure.QueryHandlers.Reports;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Reports;

public class GetSalesSummaryQueryHandlerTests
{
    private static StoreHubDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<StoreHubDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new StoreHubDbContext(options);
    }

    private static Invoice CreateInvoice(
        DateTime createdUtc,
        decimal totalAmount,
        List<(string Method, decimal Amount)>? payments = null,
        List<(Guid ProductId, string ProductName, int Qty, decimal Price)>? lines = null)
    {
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            StoreId = "STORE1",
            UserId = Guid.NewGuid(),
            TotalAmount = totalAmount,
            CreatedUtc = createdUtc,
            LastModifiedUtc = createdUtc,
            Lines = new List<InvoiceLine>(),
            Payments = new List<Payment>()
        };

        payments ??= [("Cash", totalAmount)];
        foreach (var (method, amount) in payments)
        {
            invoice.Payments.Add(new Payment
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                Method = method,
                Amount = amount,
                CreatedUtc = createdUtc
            });
        }

        lines ??= [(Guid.NewGuid(), "Product", 1, totalAmount)];
        foreach (var (productId, productName, qty, price) in lines)
        {
            invoice.Lines.Add(new InvoiceLine
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                ProductId = productId,
                ProductName = productName,
                Quantity = qty,
                UnitPrice = price
                // LineTotal is calculated property: UnitPrice * Quantity
            });
        }

        return invoice;
    }

    [Fact]
    public async Task HandleAsync_WithInvoicesInDateRange_ShouldReturnCorrectSummary()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var handler = new GetSalesSummaryQueryHandler(dbContext);

        var invoice1 = CreateInvoice(
            new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc),
            100m,
            [("Cash", 100m)]);
        var invoice2 = CreateInvoice(
            new DateTime(2024, 6, 16, 14, 0, 0, DateTimeKind.Utc),
            200m,
            [("Card", 200m)]);

        dbContext.Invoices.AddRange(invoice1, invoice2);
        await dbContext.SaveChangesAsync();

        var query = new GetSalesSummaryQuery(
            FromDate: new DateOnly(2024, 6, 1),
            ToDate: new DateOnly(2024, 6, 30));

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.InvoiceCount.Should().Be(2);
        result.TotalRevenue.Should().Be(300m);
        result.PaymentBreakdown.Cash.Should().Be(100m);
        result.PaymentBreakdown.Card.Should().Be(200m);
    }

    [Fact]
    public async Task HandleAsync_ShouldExcludeInvoicesOutsideDateRange()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var handler = new GetSalesSummaryQueryHandler(dbContext);

        var invoiceInRange = CreateInvoice(
            new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc),
            100m);
        var invoiceOutOfRange = CreateInvoice(
            new DateTime(2024, 7, 15, 10, 0, 0, DateTimeKind.Utc),
            500m);

        dbContext.Invoices.AddRange(invoiceInRange, invoiceOutOfRange);
        await dbContext.SaveChangesAsync();

        var query = new GetSalesSummaryQuery(
            FromDate: new DateOnly(2024, 6, 1),
            ToDate: new DateOnly(2024, 6, 30));

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.InvoiceCount.Should().Be(1);
        result.TotalRevenue.Should().Be(100m);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnTopProducts()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var handler = new GetSalesSummaryQueryHandler(dbContext);

        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();

        var invoice = CreateInvoice(
            new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc),
            300m,
            [("Cash", 300m)],
            [
                (productId1, "Product A", 2, 100m),  // Revenue: 200
                (productId2, "Product B", 1, 100m)   // Revenue: 100
            ]);

        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync();

        var query = new GetSalesSummaryQuery(
            FromDate: new DateOnly(2024, 6, 1),
            ToDate: new DateOnly(2024, 6, 30),
            TopProductsCount: 10);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.TopProducts.Should().HaveCount(2);
        result.TopProducts[0].ProductName.Should().Be("Product A");
        result.TopProducts[0].Revenue.Should().Be(200m);
        result.TopProducts[0].QuantitySold.Should().Be(2);
    }

    [Fact]
    public async Task HandleAsync_WithNoInvoices_ShouldReturnEmptySummary()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var handler = new GetSalesSummaryQueryHandler(dbContext);

        var query = new GetSalesSummaryQuery(
            FromDate: new DateOnly(2024, 6, 1),
            ToDate: new DateOnly(2024, 6, 30));

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.InvoiceCount.Should().Be(0);
        result.TotalRevenue.Should().Be(0);
        result.PaymentBreakdown.Cash.Should().Be(0);
        result.TopProducts.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ShouldCalculateCorrectPaymentBreakdown()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var handler = new GetSalesSummaryQueryHandler(dbContext);

        var invoice = CreateInvoice(
            new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc),
            500m,
            [
                ("Cash", 100m),
                ("Card", 150m),
                ("Transfer", 100m),
                ("PayLater", 50m),
                ("WelfareCard", 75m),
                ("Bitcoin", 25m)  // Other
            ]);

        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync();

        var query = new GetSalesSummaryQuery(
            FromDate: new DateOnly(2024, 6, 1),
            ToDate: new DateOnly(2024, 6, 30));

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.PaymentBreakdown.Cash.Should().Be(100m);
        result.PaymentBreakdown.Card.Should().Be(150m);
        result.PaymentBreakdown.Transfer.Should().Be(100m);
        result.PaymentBreakdown.PayLater.Should().Be(50m);
        result.PaymentBreakdown.WelfareCard.Should().Be(75m);
        result.PaymentBreakdown.Other.Should().Be(25m);
    }
}
