using System.Text.Json;
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.Cloud.Sync.Events;
using IndyPOS.Application.UseCases.StoreHub.PaymentMethods;
using IndyPOS.Domain.Entities.Core;
using Microsoft.Extensions.Logging;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Sales.Complete;

public class CompleteSaleCommandHandler : ICommandHandler<CompleteSaleCommand, CompleteSaleResponse>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IProductRepository _productRepository;
    private readonly IStoreIdentityService _storeIdentity;
    private readonly IPaymentMethodCatalogService _catalog;
    private readonly ILogger<CompleteSaleCommandHandler> _logger;

    public CompleteSaleCommandHandler(
        ISaleRepository saleRepository,
        IProductRepository productRepository,
        IStoreIdentityService storeIdentity,
        IPaymentMethodCatalogService catalog,
        ILogger<CompleteSaleCommandHandler> logger)
    {
        _saleRepository = saleRepository;
        _productRepository = productRepository;
        _storeIdentity = storeIdentity;
        _catalog = catalog;
        _logger = logger;
    }

    public async Task<CompleteSaleResponse> HandleAsync(
        CompleteSaleCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Processing sale: StoreId={StoreId}, UserId={UserId}, Lines={LineCount}, Payments={PaymentCount}",
            command.StoreId, command.UserId, command.Lines.Count, command.Payments.Count);

        // Validate all payment methods are offerable for this store (catalog is the single source of truth)
        var offerable = await _catalog.GetOfferableAsync(cancellationToken);
        var offerableCodes = offerable.Select(m => m.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rejected = command.Payments.FirstOrDefault(p => !offerableCodes.Contains(p.Method));
        if (rejected is not null)
        {
            _logger.LogWarning("Payment method rejected: Method={Method}, StoreType={StoreType}, UserId={UserId}",
                rejected.Method, _storeIdentity.StoreType, command.UserId);
            throw new InvalidOperationException(
                $"Payment method '{rejected.Method}' is not available for {_storeIdentity.StoreType} stores.");
        }

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

            // Defect 7b: a non-trackable product holds no stock, so selling it moves none. Without
            // this guard every sale wrote a movement, driving such a product permanently negative
            // from its first v4 sale -- 29 real products across the three stores, the sold-by-hand
            // items like ice and "5-baht snack".
            //
            // The LINE is still recorded above either way: the sale happened and its money is real.
            // Only the stock movement is skipped.
            //
            // An unknown product (product is null) keeps its movement, unchanged: that is a
            // different problem and silently dropping its stock effect would hide it.
            if (product is { IsTrackable: false })
            {
                continue;
            }

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

        _logger.LogInformation(
            "Sale completed: InvoiceId={InvoiceId}, Total={TotalAmount:C}, Lines={LineCount}, UserId={UserId}",
            invoice.Id, invoice.TotalAmount, lines.Count, command.UserId);

        return new CompleteSaleResponse(
            InvoiceId: invoice.Id,
            TotalAmount: invoice.TotalAmount,
            CreatedUtc: invoice.CreatedUtc);
    }
}
