using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Application.UseCases.StoreHub.Sales.Reprints;

/// <summary>Who reprinted and when. The till prints "พิมพ์ซ้ำ … โดย &lt;name&gt;" with the logged-in user's name.</summary>
public record InvoiceReprintDto(Guid Id, Guid InvoiceId, DateTime CreatedUtc, Guid CreatedByUserId);

/// <summary>The 201 body: the bill to print as a copy, and the record just written.</summary>
public record InvoiceReprintResultDto(InvoiceDetailDto Sale, InvoiceReprintDto Reprint);

public static class InvoiceReprintMapping
{
    public static InvoiceReprintDto ToDto(this InvoiceReprint reprint) =>
        new(reprint.Id, reprint.InvoiceId, reprint.CreatedUtc, reprint.CreatedByUserId);
}
