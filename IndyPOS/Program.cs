using IndyPOS.Common.Interfaces;
using IndyPOS.Configurations;
using IndyPOS.Constants;
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
using LazyCache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prism.Events;
using Serilog;
using Serilog.Formatting.Json;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;

namespace IndyPOS;

[ExcludeFromCodeCoverage]
internal static class Program
{
	private const string ProcessName = "IndyPOS";

	[STAThread]
	private static void Main()
	{
		// To customize application configuration such as set high DPI settings or default font,
		// see https://aka.ms/applicationconfiguration.
		ApplicationConfiguration.Initialize();

		ClosePreviousProcesses();

		var host = CreateHost();

		host.Services.GetRequiredService<IMachine>().Launch();
	}

	private static IHost CreateHost()
	{
		return Host.CreateDefaultBuilder()
				   .ConfigureAppConfiguration(CreateConfiguration)
				   .ConfigureServices(ConfigureServicesForApplication)
				   .Build();
	}

	private static void CreateConfiguration(HostBuilderContext context, IConfigurationBuilder configBuilder)
	{
		configBuilder.SetBasePath(Directory.GetCurrentDirectory())
					 .AddJsonFile("appsettings.json")
					 .Build();
	}

	private static void ConfigureServicesForApplication(HostBuilderContext context, IServiceCollection services)
	{
		services.AddSingleton<IJsonUtility, JsonUtility>();
		services.AddSingleton<IConfig, Config>();
		services.AddSingleton<IEventAggregator, EventAggregator>();
		services.AddSingleton<IStoreConstants, StoreConstants>();
		services.AddSingleton<IBarcodeUtility, BarcodeUtility>();
		services.AddSingleton<IAppCache, CachingService>();
		services.AddSingleton<HttpClient, HttpClient>();
		services.AddSingleton<IMachine, Machine>();

		ConfigureServicesForLogger(context, services);
		ConfigureServicesForHelpers(services);
		ConfigureServicesForUi(services);
		ConfigureServicesForRepositories(services);
		ConfigureServicesForControllers(services);
	}

	private static void ConfigureServicesForHelpers(IServiceCollection services)
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
	}

	private static void ConfigureServicesForLogger(HostBuilderContext context, IServiceCollection services)
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
	}

	private static void ConfigureServicesForUi(IServiceCollection services)
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
	}

	private static void ConfigureServicesForRepositories(IServiceCollection services)
	{
		services.AddSingleton<IDbConnectionProvider, DbConnectionProvider>();
		services.AddSingleton<IInvoiceRepository, InvoiceRepository>();
		services.AddSingleton<IInventoryProductRepository, InventoryProductRepository>();
		services.AddSingleton<IStoreConstantRepository, StoreConstantRepository>();
		services.AddSingleton<IUserRepository, UserRepository>();
		services.AddSingleton<IAccountsReceivableRepository, AccountsReceivableRepository>();
	}

	private static void ConfigureServicesForControllers(IServiceCollection services)
	{
		services.AddSingleton<IAccountsReceivableController, AccountsReceivableController>();
		services.AddSingleton<IInventoryController, InventoryController>();
		services.AddSingleton<IReportController, ReportController>();
		services.AddSingleton<ISaleInvoiceController, SaleInvoiceController>();
		services.AddSingleton<IUserController, UserController>();
	}

	private static void ClosePreviousProcesses()
	{
		var currentProcessId = Environment.ProcessId;
		var processes = Process.GetProcessesByName(ProcessName)
							   .Where(p => p.Id != currentProcessId)
							   .ToList();

		// Verify if either Process or DebugProcess has more than one instance
		if (!processes.Any()) 
			return;

		// Kill all previous processes
		foreach (var process in processes)
		{
			process.CloseMainWindow();
			process.WaitForExit(4000);

			if (process.HasExited) 
				continue;
					
			process.Kill();
			process.WaitForExit(4000);
		}
	}
}