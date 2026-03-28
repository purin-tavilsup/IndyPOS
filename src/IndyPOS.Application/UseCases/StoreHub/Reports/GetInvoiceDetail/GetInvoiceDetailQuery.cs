using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Reports.GetInvoiceDetail;

/// <summary>
/// Query to get full invoice detail by ID.
/// </summary>
public record GetInvoiceDetailQuery(Guid InvoiceId) : IQuery<InvoiceDetailDto?>;
