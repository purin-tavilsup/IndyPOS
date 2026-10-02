using System.Text.Json;
using IndyPOS.Application.UseCases.Cloud.Sync.Events;
using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Application.UseCases.StoreHub.Sales.Reprints;

/// <summary>Builds the InvoiceReprinted sync event for a reprint row.</summary>
public static class InvoiceReprintOutbox
{
    public const string InvoiceReprinted = "InvoiceReprinted";

    public static OutboxEvent Reprinted(InvoiceReprint reprint)
    {
        var eventId = Guid.NewGuid();
        var payload = new InvoiceReprintedEvent
        {
            EventId = eventId,
            ReprintId = reprint.Id,
            InvoiceId = reprint.InvoiceId,
            StoreId = reprint.StoreId,
            CreatedUtc = reprint.CreatedUtc,
            CreatedByUserId = reprint.CreatedByUserId
        };

        return new OutboxEvent
        {
            Id = eventId,
            StoreId = reprint.StoreId,
            Type = InvoiceReprinted,
            PayloadJson = JsonSerializer.Serialize(payload),
            CreatedUtc = reprint.CreatedUtc,
            Status = "Pending"
        };
    }
}
