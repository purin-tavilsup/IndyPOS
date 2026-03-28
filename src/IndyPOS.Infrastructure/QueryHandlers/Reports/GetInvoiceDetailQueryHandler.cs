using IndyPOS.Application.UseCases.StoreHub.Reports;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetInvoiceDetail;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Nokpirab;

namespace IndyPOS.Infrastructure.QueryHandlers.Reports;

/// <summary>
/// Handler for GetInvoiceDetailQuery.
/// Returns full invoice with lines and payments.
/// </summary>
public class GetInvoiceDetailQueryHandler : IQueryHandler<GetInvoiceDetailQuery, InvoiceDetailDto?>
{
    private readonly StoreHubDbContext _dbContext;

    public GetInvoiceDetailQueryHandler(StoreHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<InvoiceDetailDto?> HandleAsync(GetInvoiceDetailQuery query, CancellationToken cancellationToken = default)
    {
        var invoice = await _dbContext.Invoices
            .Where(i => i.Id == query.InvoiceId)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (invoice is null)
            return null;

        return new InvoiceDetailDto(
            Id: invoice.Id,
            StoreId: invoice.StoreId,
            UserId: invoice.UserId,
            TotalAmount: invoice.TotalAmount,
            CreatedUtc: invoice.CreatedUtc,
            Lines: invoice.Lines
                .Select(l => new InvoiceLineDto(
                    Id: l.Id,
                    ProductId: l.ProductId,
                    ProductName: l.ProductName,
                    Quantity: l.Quantity,
                    UnitPrice: l.UnitPrice,
                    LineTotal: l.LineTotal))
                .ToList(),
            Payments: invoice.Payments
                .Select(p => new PaymentDto(
                    Id: p.Id,
                    Method: p.Method,
                    Amount: p.Amount,
                    Note: p.Note))
                .ToList());
    }
}
