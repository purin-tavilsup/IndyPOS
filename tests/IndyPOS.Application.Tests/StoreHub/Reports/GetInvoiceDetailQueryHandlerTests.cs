using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetInvoiceDetail;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using IndyPOS.Infrastructure.QueryHandlers.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Reports;

public class GetInvoiceDetailQueryHandlerTests
{
    private static StoreHubDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<StoreHubDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new StoreHubDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_WithExistingInvoice_ShouldReturnFullDetail()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var handler = new GetInvoiceDetailQueryHandler(dbContext, NullLogger<GetInvoiceDetailQueryHandler>.Instance);

        var invoiceId = Guid.NewGuid();
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();

        var invoice = new Invoice
        {
            Id = invoiceId,
            StoreId = "STORE1",
            UserId = Guid.NewGuid(),
            TotalAmount = 250m,
            CreatedUtc = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc),
            LastModifiedUtc = DateTime.UtcNow,
            Lines = new List<InvoiceLine>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoiceId,
                    ProductId = productId1,
                    ProductName = "Product A",
                    Quantity = 2,
                    UnitPrice = 50m
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoiceId,
                    ProductId = productId2,
                    ProductName = "Product B",
                    Quantity = 3,
                    UnitPrice = 50m
                }
            },
            Payments = new List<Payment>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoiceId,
                    Method = "Cash",
                    Amount = 200m,
                    Note = "Cash payment",
                    CreatedUtc = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoiceId,
                    Method = "Card",
                    Amount = 50m,
                    Note = null,
                    CreatedUtc = DateTime.UtcNow
                }
            }
        };

        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync();

        var query = new GetInvoiceDetailQuery(invoiceId);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(invoiceId);
        result.StoreId.Should().Be("STORE1");
        result.TotalAmount.Should().Be(250m);

        result.Lines.Should().HaveCount(2);
        result.Lines.Should().Contain(l => l.ProductName == "Product A" && l.Quantity == 2);
        result.Lines.Should().Contain(l => l.ProductName == "Product B" && l.Quantity == 3);

        result.Payments.Should().HaveCount(2);
        result.Payments.Should().Contain(p => p.Method == "Cash" && p.Amount == 200m);
        result.Payments.Should().Contain(p => p.Method == "Card" && p.Amount == 50m);
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentInvoice_ShouldReturnNull()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var handler = new GetInvoiceDetailQueryHandler(dbContext, NullLogger<GetInvoiceDetailQueryHandler>.Instance);

        var query = new GetInvoiceDetailQuery(Guid.NewGuid());

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_ShouldMapAllLineProperties()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var handler = new GetInvoiceDetailQueryHandler(dbContext, NullLogger<GetInvoiceDetailQueryHandler>.Instance);

        var invoiceId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var invoice = new Invoice
        {
            Id = invoiceId,
            StoreId = "STORE1",
            UserId = Guid.NewGuid(),
            TotalAmount = 150m,
            CreatedUtc = DateTime.UtcNow,
            LastModifiedUtc = DateTime.UtcNow,
            Lines = new List<InvoiceLine>
            {
                new()
                {
                    Id = lineId,
                    InvoiceId = invoiceId,
                    ProductId = productId,
                    ProductName = "Test Product",
                    Quantity = 3,
                    UnitPrice = 50m
                }
            },
            Payments = new List<Payment>
            {
                new() { Id = Guid.NewGuid(), InvoiceId = invoiceId, Method = "Cash", Amount = 150m, CreatedUtc = DateTime.UtcNow }
            }
        };

        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync();

        var query = new GetInvoiceDetailQuery(invoiceId);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        var line = result!.Lines.Single();
        line.Id.Should().Be(lineId);
        line.ProductId.Should().Be(productId);
        line.ProductName.Should().Be("Test Product");
        line.Quantity.Should().Be(3);
        line.UnitPrice.Should().Be(50m);
        line.LineTotal.Should().Be(150m);
    }

    [Fact]
    public async Task HandleAsync_ShouldMapPaymentNote()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var handler = new GetInvoiceDetailQueryHandler(dbContext, NullLogger<GetInvoiceDetailQueryHandler>.Instance);

        var invoiceId = Guid.NewGuid();

        var invoice = new Invoice
        {
            Id = invoiceId,
            StoreId = "STORE1",
            UserId = Guid.NewGuid(),
            TotalAmount = 100m,
            CreatedUtc = DateTime.UtcNow,
            LastModifiedUtc = DateTime.UtcNow,
            Lines = new List<InvoiceLine>
            {
                new() { Id = Guid.NewGuid(), InvoiceId = invoiceId, ProductId = Guid.NewGuid(), ProductName = "Test", Quantity = 1, UnitPrice = 100m }
            },
            Payments = new List<Payment>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoiceId,
                    Method = "PayLater",
                    Amount = 100m,
                    Note = "Customer: John Doe",
                    CreatedUtc = DateTime.UtcNow
                }
            }
        };

        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync();

        var query = new GetInvoiceDetailQuery(invoiceId);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        var payment = result!.Payments.Single();
        payment.Method.Should().Be("PayLater");
        payment.Note.Should().Be("Customer: John Doe");
    }
}
