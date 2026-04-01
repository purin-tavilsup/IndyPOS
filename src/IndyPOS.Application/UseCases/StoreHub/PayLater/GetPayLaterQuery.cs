using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.PayLater;

/// <summary>
/// Query to get PayLater records with optional filtering.
/// </summary>
public record GetPayLaterQuery(
    bool IncludeCompleted = false,
    string? SearchTerm = null) : IQuery<GetPayLaterResponse>;

/// <summary>
/// Handler for GetPayLaterQuery.
/// </summary>
public class GetPayLaterQueryHandler : IQueryHandler<GetPayLaterQuery, GetPayLaterResponse>
{
    private readonly IPayLaterRepository _payLaterRepository;

    public GetPayLaterQueryHandler(IPayLaterRepository payLaterRepository)
    {
        _payLaterRepository = payLaterRepository;
    }

    public async Task<GetPayLaterResponse> HandleAsync(
        GetPayLaterQuery query,
        CancellationToken cancellationToken = default)
    {
        var payLaters = await _payLaterRepository.GetAllAsync(
            query.IncludeCompleted,
            query.SearchTerm,
            cancellationToken);

        var items = payLaters
            .Select(p => new PayLaterDto(
                p.Id,
                p.PaymentId,
                p.InvoiceId,
                p.Description,
                p.PayLaterAmount,
                p.PaidAmount,
                p.RemainingAmount,
                p.IsCompleted,
                p.CreatedUtc,
                p.LastModifiedUtc))
            .ToList();

        var activeItems = items.Where(x => !x.IsCompleted).ToList();
        var completedItems = items.Where(x => x.IsCompleted).ToList();

        return new GetPayLaterResponse(
            TotalOutstanding: activeItems.Sum(x => x.RemainingAmount),
            TotalPaid: items.Sum(x => x.PaidAmount),
            ActiveCount: activeItems.Count,
            CompletedCount: completedItems.Count,
            Items: items);
    }
}
