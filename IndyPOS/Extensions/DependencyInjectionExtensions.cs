using IndyPOS.Controllers;
using IndyPOS.DataAccess;
using IndyPOS.DataAccess.Interfaces;
using IndyPOS.DataAccess.Repositories.SQLite;
using IndyPOS.Facade.Helpers;
using IndyPOS.Facade.Interfaces;
using IndyPOS.Facade.Utilities;
using IndyPOS.Interfaces;
using IndyPOS.UI;
using IndyPOS.UI.Reports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Json;
using System.IO;
using System.Runtime.Versioning;

namespace IndyPOS.Extensions;

[type:SupportedOSPlatform("windows")]
internal static class DependencyInjectionExtensions
{
    internal static IServiceCollection AddLogger(this IServiceCollection services, HostBuilderContext context)
    {
		const string defaultDirectory = @"C:\\ProgramData\\IndyPOS\\Logs";
		var logDirectory = context.Configuration.GetValue<string>("LogDirectory") ?? defaultDirectory;

		if (!Directory.Exists(logDirectory))
		{
			Directory.CreateDirectory(logDirectory);
		}

		var logFilePath = $"{logDirectory}\\log.json";
		var logger = new LoggerConfiguration().MinimumLevel.Debug()
											  .WriteTo.File(new JsonFormatter(), logFilePath, rollingInterval: RollingInterval.Day)
											  .CreateLogger();

		services.AddSingleton<ILogger>(logger);

		return services;
	}

	internal static IServiceCollection AddUtilities(this IServiceCollection services)
	{
		services.AddSingleton<IJsonUtility, JsonUtility>()
				.AddSingleton<IBarcodeUtility, BarcodeUtility>();

		return services;
	}

	internal static IServiceCollection AddHelpers(this IServiceCollection services)
	{
		services.AddSingleton<IStoreConfigurationHelper, StoreConfigurationHelper>()
				.AddSingleton<ISaleInvoiceHelper, SaleInvoiceHelper>()
				.AddSingleton<IInventoryHelper, InventoryHelper>()
				.AddSingleton<IUserHelper, UserHelper>()
				.AddSingleton<IBarcodeScannerHelper, BarcodeScannerHelper>()
				.AddSingleton<IReceiptPrinterHelper, ReceiptPrinterHelper>()
				.AddSingleton<ICryptographyUtility, CryptographyUtility>()
				.AddSingleton<IAccountsReceivableHelper, AccountsReceivableHelper>()
				.AddSingleton<IReportHelper, ReportHelper>()
				.AddSingleton<IDataFeedApiHelper, DataFeedApiHelper>();

		return services;
	}

	internal static IServiceCollection AddUserInterfaces(this IServiceCollection services)
	{
		services.AddSingleton<InvoiceProductsReportPanel>()
				.AddSingleton<SalesHistoryReportPanel>()
				.AddSingleton<SalesReportPanel>()
				.AddSingleton<AcceptPaymentForm>()
				.AddSingleton<AccountsReceivablePanel>()
				.AddSingleton<AddInvoiceProductForm>()
				.AddSingleton<AddNewInventoryProductForm>()
				.AddSingleton<AddNewInventoryProductWithCustomBarcodeForm>()
				.AddSingleton<AddNewUserForm>()
				.AddSingleton<InventoryPanel>()
				.AddSingleton<MainForm>()
				.AddSingleton<MessageForm>()
				.AddSingleton<PrintReceiptForm>()
				.AddSingleton<ReportsPanel>()
				.AddSingleton<SaleHistoryByInvoiceIdForm>()
				.AddSingleton<SalePanel>()
				.AddSingleton<SettingsPanel>()
				.AddSingleton<UpdateInventoryProductForm>()
				.AddSingleton<UpdateInvoiceProductForm>()
				.AddSingleton<UserLogInPanel>()
				.AddSingleton<UsersPanel>();

		return services;
	}

	internal static IServiceCollection AddRepositories(this IServiceCollection services)
	{
		services.AddSingleton<IDbConnectionProvider, DbConnectionProvider>()
				.AddSingleton<IInvoiceRepository, InvoiceRepository>()
				.AddSingleton<IInventoryProductRepository, InventoryProductRepository>()
				.AddSingleton<IStoreConstantRepository, StoreConstantRepository>()
				.AddSingleton<IUserRepository, UserRepository>()
				.AddSingleton<IAccountsReceivableRepository, AccountsReceivableRepository>();

		return services;
	}

	internal static IServiceCollection AddControllers(this IServiceCollection services)
	{
		services.AddSingleton<IAccountsReceivableController, AccountsReceivableController>()
				.AddSingleton<IInventoryController, InventoryController>()
				.AddSingleton<IReportController, ReportController>()
				.AddSingleton<ISaleInvoiceController, SaleInvoiceController>()
				.AddSingleton<IUserController, UserController>();

		return services;
	}
}