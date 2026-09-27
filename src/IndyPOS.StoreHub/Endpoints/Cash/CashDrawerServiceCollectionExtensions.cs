using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.DebtRepayments;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Delete;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Floats;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Payouts;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Summary;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.QueryHandlers.CashDrawer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nokpirab;

namespace IndyPOS.StoreHub.Endpoints.Cash;

/// <summary>Registers the cash-drawer clock and handlers (repositories come from AddStoreHubServices).</summary>
public static class CashDrawerServiceCollectionExtensions
{
    public static IServiceCollection AddCashDrawer(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ICashDrawerClock, CashDrawerClock>();

        services.AddTransient<ICommandHandler<AddCashPayoutCommand, CashPayoutDto>, AddCashPayoutCommandHandler>();
        services.AddTransient<ICommandHandler<EditCashPayoutCommand, CashPayoutDto>, EditCashPayoutCommandHandler>();
        services.AddTransient<IQueryHandler<GetCashPayoutsQuery, IReadOnlyList<CashPayoutDto>>, GetCashPayoutsQueryHandler>();

        services.AddTransient<ICommandHandler<AddCashFloatCommand, CashFloatDto>, AddCashFloatCommandHandler>();
        services.AddTransient<ICommandHandler<EditCashFloatCommand, CashFloatDto>, EditCashFloatCommandHandler>();
        services.AddTransient<IQueryHandler<GetCashFloatsQuery, IReadOnlyList<CashFloatDto>>, GetCashFloatsQueryHandler>();

        services.AddTransient<ICommandHandler<AddDebtRepaymentCommand, DebtRepaymentDto>, AddDebtRepaymentCommandHandler>();
        services.AddTransient<ICommandHandler<EditDebtRepaymentCommand, DebtRepaymentDto>, EditDebtRepaymentCommandHandler>();
        services.AddTransient<IQueryHandler<GetDebtRepaymentsQuery, IReadOnlyList<DebtRepaymentDto>>, GetDebtRepaymentsQueryHandler>();

        services.AddTransient<ICommandHandler<DeleteCashEntryCommand<CashPayout>>, DeleteCashEntryCommandHandler<CashPayout>>();
        services.AddTransient<ICommandHandler<DeleteCashEntryCommand<CashFloat>>, DeleteCashEntryCommandHandler<CashFloat>>();
        services.AddTransient<ICommandHandler<DeleteCashEntryCommand<DebtRepayment>>, DeleteCashEntryCommandHandler<DebtRepayment>>();

        services.AddTransient<ICommandHandler<AddCashCountCommand, CashCountDto>, AddCashCountCommandHandler>();
        services.AddTransient<IQueryHandler<GetCashCountsQuery, IReadOnlyList<CashCountDto>>, GetCashCountsQueryHandler>();

        services.AddTransient<IQueryHandler<GetCashDrawerSummaryQuery, CashDrawerSummaryDto>, GetCashDrawerSummaryQueryHandler>();
        return services;
    }
}
