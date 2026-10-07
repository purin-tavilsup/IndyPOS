using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Application.UseCases.StoreHub.Sales.Reprints;
using IndyPOS.Infrastructure.QueryHandlers.Sales;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nokpirab;

namespace IndyPOS.StoreHub.Endpoints.Sales;

/// <summary>Registers the /sales handlers. "Today" comes from the cash drawer's clock (spec §5).</summary>
public static class SalesServiceCollectionExtensions
{
    public static IServiceCollection AddSalesHistory(this IServiceCollection services)
    {
        // TryAdd: AddCashDrawer registers the same clock; either order works.
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<ICashDrawerClock, CashDrawerClock>();

        services.AddTransient<IQueryHandler<ListSalesQuery, SalesPage>, ListSalesQueryHandler>();
        services.AddTransient<IQueryHandler<ListSaleLinesQuery, SaleLinesPage>, ListSaleLinesQueryHandler>();
        services.AddTransient<IQueryHandler<GetSaleByIdQuery, InvoiceDetailDto?>, GetSaleQueryHandler>();
        services.AddTransient<IQueryHandler<GetSaleByNumberQuery, InvoiceDetailDto?>, GetSaleQueryHandler>();
        services.AddTransient<ICommandHandler<CreateInvoiceReprintCommand, InvoiceReprintResultDto>, CreateInvoiceReprintCommandHandler>();
        return services;
    }
}
