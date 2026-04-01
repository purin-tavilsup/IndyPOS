using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Exceptions;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.PayLater;

/// <summary>
/// Query to get a single PayLater record by ID.
/// </summary>
public record GetPayLaterByIdQuery(Guid Id) : IQuery<PayLaterDto>;

/// <summary>
/// Handler for GetPayLaterByIdQuery.
/// </summary>
public class GetPayLaterByIdQueryHandler : IQueryHandler<GetPayLaterByIdQuery, PayLaterDto>
{
    private readonly IPayLaterRepository _payLaterRepository;

    public GetPayLaterByIdQueryHandler(IPayLaterRepository payLaterRepository)
    {
        _payLaterRepository = payLaterRepository;
    }

    public async Task<PayLaterDto> HandleAsync(
        GetPayLaterByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var payLater = await _payLaterRepository.GetByIdAsync(query.Id, cancellationToken)
            ?? throw new PayLaterPaymentNotFoundException($"PayLater with ID {query.Id} not found.");

        return new PayLaterDto(
            payLater.Id,
            payLater.PaymentId,
            payLater.InvoiceId,
            payLater.Description,
            payLater.PayLaterAmount,
            payLater.PaidAmount,
            payLater.RemainingAmount,
            payLater.IsCompleted,
            payLater.CreatedUtc,
            payLater.LastModifiedUtc);
    }
}
