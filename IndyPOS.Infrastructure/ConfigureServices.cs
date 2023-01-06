using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Infrastructure.Constants;
using IndyPOS.Infrastructure.Persistence.Repositories.SQLite;
using IndyPOS.Infrastructure.Services;
using LazyCache;
using Prism.Events;
using System.Runtime.Versioning;

// ReSharper disable CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

[type: SupportedOSPlatform("windows")]
public static class ConfigureServices
{
	public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<IDbConnectionProvider, DbConnectionProvider>()
                .AddSingleton<IInvoiceRepository, InvoiceRepository>()
                .AddSingleton<IInvoiceProductRepository, InvoiceProductRepository>()
                .AddSingleton<IInvoicePaymentRepository, InvoicePaymentRepository>()
                .AddSingleton<IInventoryProductRepository, InventoryProductRepository>()
                .AddSingleton<IStoreConstantRepository, StoreConstantRepository>()
                .AddSingleton<IUserRepository, UserRepository>()
                .AddSingleton<IPayLaterPaymentRepository, PayLaterRepository>();

        services.AddSingleton<IEventAggregator, EventAggregator>()
				.AddSingleton<IAppCache, CachingService>()
				.AddSingleton<HttpClient, HttpClient>();

		services.AddSingleton<ICryptographyService, CryptographyService>()
				.AddSingleton<IJsonService, JsonService>()
				.AddSingleton<IBarcodeService, BarcodeService>()
				.AddSingleton<IDateTimeService, DateTimeService>();

		services.AddSingleton<IStoreConstants, StoreConstants>();

        return services;
    }
}