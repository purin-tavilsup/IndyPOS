using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Authorization;
using IndyPOS.Application.Common.Interfaces;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.PayLater;

/// <summary>
/// Query to get PayLater records with optional filtering.
/// </summary>
/// <param name="CreatedOn">Only the debts taken on this store day (the store's own calendar), e.g. the cash drawer's day.</param>
public record GetPayLaterQuery(
    bool IncludeCompleted = false,
    string? SearchTerm = null,
    DateOnly? CreatedOn = null) : IQuery<GetPayLaterResponse>;

/// <summary>
/// Handler for GetPayLaterQuery.
/// </summary>
public class GetPayLaterQueryHandler : IQueryHandler<GetPayLaterQuery, GetPayLaterResponse>
{
    private readonly IPayLaterRepository _payLaterRepository;
    private readonly IStoreIdentityService _storeIdentity;

    public GetPayLaterQueryHandler(
        IPayLaterRepository payLaterRepository,
        IStoreIdentityService storeIdentity)
    {
        _payLaterRepository = payLaterRepository;
        _storeIdentity = storeIdentity;
    }

    public async Task<GetPayLaterResponse> HandleAsync(
        GetPayLaterQuery query,
        CancellationToken cancellationToken = default)
    {
        // Return empty response if PayLater is not enabled for this store type
        if (!_storeIdentity.Features.PayLaterEnabled)
        {
            return new GetPayLaterResponse(
                TotalOutstanding: 0,
                TotalPaid: 0,
                ActiveCount: 0,
                CompletedCount: 0,
                Items: []);
        }

        var payLaters = await _payLaterRepository.GetAllAsync(
            query.IncludeCompleted,
            query.SearchTerm,
            cancellationToken);

        var items = payLaters
            .Where(p => query.CreatedOn is not { } day || TodayOnlyRule.BusinessDateOf(p.CreatedUtc, _storeIdentity.TimeZone) == day)
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
