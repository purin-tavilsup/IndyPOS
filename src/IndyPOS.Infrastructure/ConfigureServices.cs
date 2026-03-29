using System.Security.Cryptography;
using IndyPOS.Application.Abstractions.Pos.Repositories;
using IndyPOS.Application.Abstractions.Security;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Abstractions.StoreHub.Services;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using IndyPOS.Infrastructure.Constants;
using IndyPOS.Infrastructure.Persistence.Repositories.SQLite;
using IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;
using IndyPOS.Infrastructure.Persistence.StoreHub.Seeders;
using IndyPOS.Infrastructure.Services;
using IndyPOS.Infrastructure.Services.Security;
using IndyPOS.Infrastructure.Services.StoreHub;
using LazyCache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prism.Events;
using System.Runtime.Versioning;

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
		        .AddScoped<ISaleRepository, SaleRepository>()
		        .AddScoped<IOutboxRepository, OutboxRepository>()
		        .AddScoped<IInventoryMovementRepository, InventoryMovementRepository>()
		        .AddScoped<IStoreSettingRepository, StoreSettingRepository>();

		// SyncWorker configuration
		services.Configure<SyncWorkerOptions>(configuration.GetSection(SyncWorkerOptions.SectionName));

		// Cloud API configuration
		services.Configure<CloudTokenOptions>(configuration.GetSection(CloudTokenOptions.SectionName));

		// DPAPI secret storage for secure credential storage (S5b)
		// Stores encrypted secrets in %ProgramData%\IndyPOS\Secrets
		var secretsDirectory = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
			"IndyPOS", "Secrets");
		services.AddSingleton<ISecretStorage>(sp =>
			new DpapiSecretStorage(
				secretsDirectory,
				sp.GetRequiredService<ILogger<DpapiSecretStorage>>(),
				DataProtectionScope.LocalMachine)); // LocalMachine allows any user on this PC

		// Secure cloud token options (wraps CloudTokenOptions with DPAPI)
		services.AddSingleton<SecureCloudTokenOptions>();

		// Cloud sync client - use HTTP client if configured, otherwise stub
		var cloudApiSection = configuration.GetSection(CloudTokenOptions.SectionName);
		var clientId = cloudApiSection.GetValue<string>("ClientId");

		if (!string.IsNullOrEmpty(clientId))
		{
			// HTTP client with OAuth2 authentication
			// Uses Aspire service discovery: "https+http://cloud-api" resolves to CloudApi service
			// The ServiceDefaults configures AddServiceDiscovery() on all HttpClients
			var cloudApiBaseUrl = configuration["services:cloud-api:https:0"]
				?? configuration["services:cloud-api:http:0"]
				?? "https+http://cloud-api"; // Aspire service discovery fallback

			services.AddHttpClient<ITokenService, CloudTokenService>(client =>
			{
				client.BaseAddress = new Uri(cloudApiBaseUrl);
			});

			services.AddHttpClient<ICloudSyncClient, HttpCloudSyncClient>(client =>
			{
				client.BaseAddress = new Uri(cloudApiBaseUrl);
			});
		}
		else
		{
			// Stub for development/testing without Cloud API
			services.AddScoped<ICloudSyncClient, StubCloudSyncClient>();
		}

		return services;
	}

	/// <summary>
	/// Adds the SyncWorker hosted service.
	/// Call this after AddStoreHubServices.
	/// </summary>
	public static IServiceCollection AddSyncWorker(this IServiceCollection services)
	{
		services.AddHostedService<SyncWorker>();
		return services;
	}

	/// <summary>
	/// Adds StoreHub authentication services (BCrypt, JWT, auth service).
	/// Call this after AddStoreHubServices.
	/// </summary>
	public static IServiceCollection AddStoreHubAuthServices(this IServiceCollection services, IConfiguration configuration)
	{
		// Local token configuration
		services.Configure<LocalTokenOptions>(configuration.GetSection(LocalTokenOptions.SectionName));

		// Auth services (Scoped for EF Core DbContext)
		services.AddScoped<IStoreUserRepository, StoreUserRepository>();
		services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
		services.AddSingleton<ILocalTokenService, LocalTokenService>();
		services.AddScoped<IStoreAuthService, StoreAuthService>();

		// User sync service (Epic S2: Local User Cache)
		services.AddScoped<IUserSyncService, UserSyncService>();

		// Legacy crypto service for password migration
		services.AddTransient<ICryptographyService, CryptographyService>();

		// SQLite connection provider (for user migration from legacy database)
		services.AddSingleton<IDbConnectionProvider, DbConnectionProvider>();

		// User migration seeder (for dev migration from SQLite)
		services.AddScoped<UserMigrationSeeder>();

		// Development data seeder (test users and products)
		services.AddScoped<DevelopmentDataSeeder>();

		return services;
	}

	/// <summary>
	/// Registers StoreHub client services for WinForms app.
	/// Enables WinForms to call StoreHub API instead of direct SQLite access.
	/// Configure via "StoreHub" section in appsettings.json.
	/// </summary>
	public static IServiceCollection AddStoreHubClientServices(this IServiceCollection services, IConfiguration configuration)
	{
		// StoreHub options
		var storeHubOptions = configuration.GetSection(StoreHubOptions.SectionName).Get<StoreHubOptions>()
			?? new StoreHubOptions();

		services.Configure<StoreHubOptions>(configuration.GetSection(StoreHubOptions.SectionName));

		if (!storeHubOptions.Enabled)
		{
			// StoreHub disabled - use legacy SQLite services
			return services;
		}

		// Register StoreHub HTTP client
		services.AddHttpClient<IStoreHubClient, StoreHubHttpClient>(client =>
		{
			client.BaseAddress = new Uri(storeHubOptions.BaseUrl);
			client.Timeout = TimeSpan.FromSeconds(storeHubOptions.TimeoutSeconds);
		});

		// Product cache service (in-memory)
		services.AddSingleton<IProductCacheService, ProductCacheService>();

		// Inventory product service (uses StoreHub API + cache)
		services.AddSingleton<IInventoryProductService, StoreHubInventoryProductService>();

		// Replace legacy services with StoreHub versions
		// Note: These replace registrations from AddInfrastructureServices
		services.AddSingleton<ISaleService, StoreHubSaleService>();
		services.AddSingleton<IUserLogInService, StoreHubUserLogInService>();

		return services;
	}
}