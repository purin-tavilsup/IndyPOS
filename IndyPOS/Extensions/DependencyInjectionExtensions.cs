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
		services.AddSingleton<IJsonUtility, JsonUtility>();
		services.AddSingleton<IBarcodeUtility, BarcodeUtility>();

		return services;
	}

	internal static IServiceCollection AddHelpers(this IServiceCollection services)
	{
		services.AddSingleton<ISaleInvoiceHelper, SaleInvoiceHelper>();
		services.AddSingleton<IInventoryHelper, InventoryHelper>();
		services.AddSingleton<IUserHelper, UserHelper>();
		services.AddSingleton<IBarcodeScannerHelper, BarcodeScannerHelper>();
		services.AddSingleton<IReceiptPrinterHelper, ReceiptPrinterHelper>();
		services.AddSingleton<ICryptographyUtility, CryptographyUtility>();
		services.AddSingleton<IAccountsReceivableHelper, AccountsReceivableHelper>();
		services.AddSingleton<IReportHelper, ReportHelper>();
		services.AddSingleton<IDataFeedApiHelper, DataFeedApiHelper>();

		return services;
	}

	internal static IServiceCollection AddUserInterfaces(this IServiceCollection services)
	{
		services.AddSingleton<InvoiceProductsReportPanel>();
		services.AddSingleton<SalesHistoryReportPanel>();
		services.AddSingleton<SalesReportPanel>();
		services.AddSingleton<AcceptPaymentForm>();
		services.AddSingleton<AccountsReceivablePanel>();
		services.AddSingleton<AddInvoiceProductForm>();
		services.AddSingleton<AddNewInventoryProductForm>();
		services.AddSingleton<AddNewInventoryProductWithCustomBarcodeForm>();
		services.AddSingleton<AddNewUserForm>();
		services.AddSingleton<InventoryPanel>();
		services.AddSingleton<MainForm>();
		services.AddSingleton<MessageForm>();
		services.AddSingleton<PrintReceiptForm>();
		services.AddSingleton<ReportsPanel>();
		services.AddSingleton<SaleHistoryByInvoiceIdForm>();
		services.AddSingleton<SalePanel>();
		services.AddSingleton<SettingsPanel>();
		services.AddSingleton<UpdateInventoryProductForm>();
		services.AddSingleton<UpdateInvoiceProductForm>();
		services.AddSingleton<UserLogInPanel>();
		services.AddSingleton<UsersPanel>();

		return services;
	}

	internal static IServiceCollection AddRepositories(this IServiceCollection services)
	{
		services.AddSingleton<IDbConnectionProvider, DbConnectionProvider>();
		services.AddSingleton<IInvoiceRepository, InvoiceRepository>();
		services.AddSingleton<IInventoryProductRepository, InventoryProductRepository>();
		services.AddSingleton<IStoreConstantRepository, StoreConstantRepository>();
		services.AddSingleton<IUserRepository, UserRepository>();
		services.AddSingleton<IAccountsReceivableRepository, AccountsReceivableRepository>();

		return services;
	}

	internal static IServiceCollection AddControllers(this IServiceCollection services)
	{
		services.AddSingleton<IAccountsReceivableController, AccountsReceivableController>();
		services.AddSingleton<IInventoryController, InventoryController>();
		services.AddSingleton<IReportController, ReportController>();
		services.AddSingleton<ISaleInvoiceController, SaleInvoiceController>();
		services.AddSingleton<IUserController, UserController>();

		return services;
	}
}