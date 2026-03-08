using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using IndyPOS.Infrastructure.Constants;
using IndyPOS.Infrastructure.Persistence.Repositories.SQLite;
using IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;
using IndyPOS.Infrastructure.Services;
using LazyCache;
using Microsoft.Extensions.Configuration;
using Prism.Events;
using System.Runtime.Versioning;
using IndyPOS.Application.Abstractions.Pos.Repositories;

// ReSharper disable CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

[type: SupportedOSPlatform("windows")]
public static class ConfigureServices
{
	public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
	{
		// Store Identity
		services.Configure<StoreIdentityOptions>(configuration.GetSection(StoreIdentityOptions.SectionName));
		services.AddSingleton<IStoreIdentityService, StoreIdentityService>();

		// Persistence
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
				.AddSingleton<ICashDrawerService, CashDrawerService>()
				.AddSingleton<IAppCache, CachingService>()
				.AddSingleton<HttpClient, HttpClient>();

		services.AddTransient<ICryptographyService, CryptographyService>()
				.AddTransient<IJsonService, JsonService>()
				.AddTransient<IBarcodeGeneratorService, BarcodeGeneratorService>()
				.AddTransient<IDateTimeService, DateTimeService>()
				.AddTransient<ICsvService, CsvService>();

		return services;
    }

	/// <summary>
	/// Registers StoreHub-specific services (EF Core repositories).
	/// Use this for StoreHub API, not for Windows.Forms.
	/// </summary>
	public static IServiceCollection AddStoreHubServices(this IServiceCollection services, IConfiguration configuration)
	{
		// Store Identity
		services.Configure<StoreIdentityOptions>(configuration.GetSection(StoreIdentityOptions.SectionName));
		services.AddSingleton<IStoreIdentityService, StoreIdentityService>();

		// StoreHub repositories (Scoped for EF Core DbContext)
		services.AddScoped<IProductRepository, ProductRepository>()
		        .AddScoped<ISaleRepository, SaleRepository>();

		return services;
	}
}