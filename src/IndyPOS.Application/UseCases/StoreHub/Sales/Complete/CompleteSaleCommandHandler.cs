using System.Text.Json;
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.Cloud.Sync.Events;
using IndyPOS.Application.UseCases.StoreHub.PaymentMethods;
using IndyPOS.Domain.Entities.Core;
using Microsoft.Extensions.Logging;
using Nokpirab;
// Inside IndyPOS.Application.UseCases.StoreHub.*, the bare name PayLater binds to the sibling
// namespace IndyPOS.Application.UseCases.StoreHub.PayLater, not to the entity.
using PayLaterDebt = IndyPOS.Domain.Entities.Core.PayLater;

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

        // Every payment rule runs before any lookup or save, so a refused sale writes nothing.
        var offerable = await _catalog.GetOfferableAsync(cancellationToken);
        var offerableCodes = offerable.Select(m => m.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var invoiceTotal = command.Lines.Sum(l => l.Quantity * l.UnitPrice);

        try
        {
            SalePaymentRules.EnsureValid(command.Payments, invoiceTotal, offerableCodes);
        }
        catch (SaleValidationException ex)
        {
            _logger.LogWarning("Sale refused: {Reason}, StoreType={StoreType}, UserId={UserId}",
                ex.Message, _storeIdentity.StoreType, command.UserId);
            throw;
        }

        var now = DateTime.UtcNow;
        var invoiceId = Guid.NewGuid();

        // Reserved only after the payment rules pass, so a sale they refuse burns no number. A failure
        // after this point (a failed or cancelled save) does burn one: Postgres never hands a sequence
        // value back, so bill numbers can have gaps but never duplicates. Same sequence as the column
        // default, so the database still decides and two tills cannot clash.
        var invoiceNumber = await _saleRepository.ReserveInvoiceNumberAsync(cancellationToken);

        // Build invoice
        var invoice = new Invoice
        {
            Id = invoiceId,
            StoreId = command.StoreId,
            UserId = command.UserId,
            InvoiceNumber = invoiceNumber,
            TotalAmount = invoiceTotal,
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

            // Create inventory movement (negative for sale), stamped with the seller
            var movement = new InventoryMovement
            {
                Id = Guid.NewGuid(),
                StoreId = command.StoreId,
                ProductId = lineRequest.ProductId,
                QuantityDelta = -lineRequest.Quantity,
                Reason = "Sale",
                ReferenceId = invoiceId,
                CreatedByUserId = command.UserId,
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

        // Spec 2026-09-30 §4.1: the debt rides its payment's 1:1 navigation, so EF saves it in the
        // same SaveChangesAsync as the sale. A credit sale can never exist without its debt.
        foreach (var payment in payments.Where(p => SalePaymentRules.IsPayLater(p.Method)))
            payment.PayLater = NewDebt(payment, now);

        // Build rich event payload (transaction snapshot)
        var eventId = Guid.NewGuid();
        var invoiceCompletedEvent = new InvoiceCompletedEvent
        {
            EventId = eventId,
            InvoiceId = invoiceId,
            StoreId = command.StoreId,
            UserId = command.UserId,
            TotalAmount = invoice.TotalAmount,
            InvoiceNumber = invoice.InvoiceNumber,
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
            CreatedUtc: invoice.CreatedUtc,
            InvoiceNumber: invoice.InvoiceNumber);
    }

    /// <summary>The shape the MigrationTool writes for a v3 debt: nothing paid yet.</summary>
    private static PayLaterDebt NewDebt(Payment payment, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        PaymentId = payment.Id,
        InvoiceId = payment.InvoiceId,
        Description = payment.Note!.Trim(),
        PayLaterAmount = payment.Amount,
        PaidAmount = 0m,
        IsCompleted = false,
        CreatedUtc = now,
        LastModifiedUtc = now
    };
}
