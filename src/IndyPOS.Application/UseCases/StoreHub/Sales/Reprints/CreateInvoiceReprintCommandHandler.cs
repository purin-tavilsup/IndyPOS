using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Domain.Entities.Core;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Sales.Reprints;

/// <summary>
/// Records a reprint request. The visibility rule runs BEFORE the write, through the same detail
/// query the GET uses: a bill the caller cannot see is a 404 and leaves no row and no event.
/// </summary>
public class CreateInvoiceReprintCommandHandler(
    IQueryHandler<GetSaleByIdQuery, InvoiceDetailDto?> sales,
    IInvoiceReprintRepository repository,
    ICashDrawerClock clock) : ICommandHandler<CreateInvoiceReprintCommand, InvoiceReprintResultDto>
{
    public async Task<InvoiceReprintResultDto> HandleAsync(CreateInvoiceReprintCommand command, CancellationToken cancellationToken = default)
    {
        var sale = await sales.HandleAsync(new GetSaleByIdQuery(command.InvoiceId, command.CanViewAnyDay), cancellationToken)
                   ?? throw new SaleNotFoundException();

        var now = clock.Now().Utc;
        var reprint = new InvoiceReprint
        {
            Id = Guid.NewGuid(),
            InvoiceId = sale.Id,
            StoreId = sale.StoreId,
            CreatedUtc = now,
            LastModifiedUtc = now,
            CreatedByUserId = command.UserId
        };

        await repository.AddAsync(reprint, InvoiceReprintOutbox.Reprinted(reprint), cancellationToken);
        return new InvoiceReprintResultDto(sale, reprint.ToDto());
    }
}
