using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Infrastructure.Constants;
using IndyPOS.Infrastructure.Persistence.Repositories.SQLite;
using IndyPOS.Infrastructure.Services;
using LazyCache;
using Prism.Events;
using System.Runtime.Versioning;
using IndyPOS.Application.Abstractions.Pos.Repositories;
using IndyPOS.Application.Abstractions.Reports.Repositories;
using IndyPOS.Infrastructure.Persistence.Repositories.PostgreSql;

// ReSharper disable CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

[type: SupportedOSPlatform("windows")]
public static class ConfigureServices
{
	public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
	{
		// Persistence
		services.AddSingleton<IReportDbConnectionProvider, ReportDbConnectionProvider>();
		services.AddScoped<IReportRepository, ReportRepository>();
        
		services.AddSingleton<IDbConnectionProvider, DbConnectionProvider>()
                .AddSingleton<IInvoiceRepository, InvoiceRepository>()
                .AddSingleton<IInvoiceProductRepository, InvoiceProductRepository>()
                .AddSingleton<IInvoicePaymentRepository, InvoicePaymentRepository>()
                .AddSingleton<IInventoryProductRepository, InventoryProductRepository>()
                .AddSingleton<IStoreConstantRepository, StoreConstantRepository>()
                .AddSingleton<IUserRepository, UserRepository>()
				.AddSingleton<IUserCredentialRepository, UserCredentialRepository>()
                .AddSingleton<IPayLaterPaymentRepository, PayLaterRepository>();

        services.AddSingleton<IStoreConstants, StoreConstants>()
				.AddSingleton<IStoreConfigurationService, StoreConfigurationService>()
				.AddSingleton<IUserLogInService, UserLogInService>()
				.AddSingleton<ISaleService, SaleService>()
				.AddSingleton<IReportService, ReportService>()
				.AddSingleton<IEventAggregator, EventAggregator>()
				.AddSingleton<IRawInputDeviceService, RawInputDeviceService>()
				.AddSingleton<IReceiptPrinterService, ReceiptPrinterService>()
				.AddSingleton<IAppCache, CachingService>()
				.AddSingleton<HttpClient, HttpClient>();

		services.AddTransient<ICryptographyService, CryptographyService>()
				.AddTransient<IJsonService, JsonService>()
				.AddTransient<IBarcodeGeneratorService, BarcodeGeneratorService>()
				.AddTransient<IDateTimeService, DateTimeService>();

		return services;
    }
}