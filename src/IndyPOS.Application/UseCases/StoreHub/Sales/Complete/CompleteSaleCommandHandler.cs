using System.Text.Json;
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.UseCases.Cloud.Sync.Events;
using IndyPOS.Domain.Entities.Core;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Sales.Complete;

public class CompleteSaleCommandHandler : ICommandHandler<CompleteSaleCommand, CompleteSaleResponse>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IProductRepository _productRepository;

    public CompleteSaleCommandHandler(
        ISaleRepository saleRepository,
        IProductRepository productRepository)
    {
        _saleRepository = saleRepository;
        _productRepository = productRepository;
    }

    public async Task<CompleteSaleResponse> HandleAsync(
        CompleteSaleCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var invoiceId = Guid.NewGuid();

        // Build invoice
        var invoice = new Invoice
        {
            Id = invoiceId,
            StoreId = command.StoreId,
            UserId = command.UserId,
            TotalAmount = command.Lines.Sum(l => l.Quantity * l.UnitPrice),
            CreatedUtc = now,
            LastModifiedUtc = now
        };

        // Build invoice lines with product name snapshot
        var lines = new List<InvoiceLine>();
        var inventoryMovements = new List<InventoryMovement>();

        foreach (var lineRequest in command.Lines)
        {
            var product = await _productRepository.GetByIdAsync(lineRequest.ProductId, cancellationToken);
            var productName = product?.Name ?? "Unknown Product";

            var line = new InvoiceLine
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoiceId,
                ProductId = lineRequest.ProductId,
                ProductName = productName,
                Quantity = lineRequest.Quantity,
                UnitPrice = lineRequest.UnitPrice,
                CreatedUtc = now
            };
            lines.Add(line);

            // Create inventory movement (negative for sale)
            var movement = new InventoryMovement
            {
                Id = Guid.NewGuid(),
                StoreId = command.StoreId,
                ProductId = lineRequest.ProductId,
                QuantityDelta = -lineRequest.Quantity,
                Reason = "Sale",
                ReferenceId = invoiceId,
                CreatedUtc = now
            };
            inventoryMovements.Add(movement);
        }

        // Build payments
        var payments = command.Payments.Select(p => new Payment
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoiceId,
            Method = p.Method,
            Amount = p.Amount,
            Note = p.Note,
            CreatedUtc = now
        }).ToList();

        // Build rich event payload (transaction snapshot)
        var eventId = Guid.NewGuid();
        var invoiceCompletedEvent = new InvoiceCompletedEvent
        {
            EventId = eventId,
            InvoiceId = invoiceId,
            StoreId = command.StoreId,
            UserId = command.UserId,
            TotalAmount = invoice.TotalAmount,
            CreatedAtUtc = now,
            Lines = lines.Select(l => new InvoiceLineSnapshot
            {
                LineId = l.Id,
                ProductId = l.ProductId,
                ProductName = l.ProductName,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice
            }).ToList(),
            Payments = payments.Select(p => new PaymentSnapshot
            {
                PaymentId = p.Id,
                Method = p.Method,
                Amount = p.Amount,
                Note = p.Note
            }).ToList(),
            InventoryMovements = inventoryMovements.Select(m => new InventoryMovementSnapshot
            {
                MovementId = m.Id,
                ProductId = m.ProductId,
                QuantityDelta = m.QuantityDelta,
                Reason = m.Reason
            }).ToList()
        };

        // Build outbox event for cloud sync
        var outboxEvent = new OutboxEvent
        {
            Id = eventId,
            StoreId = command.StoreId,
            Type = "InvoiceCompleted",
            PayloadJson = JsonSerializer.Serialize(invoiceCompletedEvent),
            CreatedUtc = now,
            Status = "Pending"
        };

        // Complete the sale atomically
        await _saleRepository.CompleteSaleAsync(
            invoice,
            lines,
            payments,
            inventoryMovements,
            outboxEvent,
            cancellationToken);

        return new CompleteSaleResponse(
            InvoiceId: invoice.Id,
            TotalAmount: invoice.TotalAmount,
            CreatedUtc: invoice.CreatedUtc);
    }
}
