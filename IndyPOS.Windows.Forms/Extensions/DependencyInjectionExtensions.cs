using System.Runtime.Versioning;
using IndyPOS.Application.Helpers;
using IndyPOS.Application.Interfaces;
using IndyPOS.Application.Utilities;
using IndyPOS.DataAccess;
using IndyPOS.DataAccess.Interfaces;
using IndyPOS.DataAccess.Repositories.SQLite;
using IndyPOS.Windows.Forms.Controllers;
using IndyPOS.Windows.Forms.Interfaces;
using IndyPOS.Windows.Forms.UI;
using IndyPOS.Windows.Forms.UI.Inventory;
using IndyPOS.Windows.Forms.UI.Login;
using IndyPOS.Windows.Forms.UI.PayLater;
using IndyPOS.Windows.Forms.UI.Payment;
using IndyPOS.Windows.Forms.UI.Report;
using IndyPOS.Windows.Forms.UI.Sale;
using IndyPOS.Windows.Forms.UI.Setting;
using IndyPOS.Windows.Forms.UI.User;
using Microsoft.Extensions.DependencyInjection;

namespace IndyPOS.Windows.Forms.Extensions;

[type:SupportedOSPlatform("windows")]
internal static class DependencyInjectionExtensions 
{
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
				.AddSingleton<IPayLaterPaymentHelper, PayLaterPaymentHelper>()
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
				.AddSingleton<PayLaterPaymentPanel>()
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
				.AddSingleton<IInvoiceProductRepository, InvoiceProductRepository>()
				.AddSingleton<IInvoicePaymentRepository, InvoicePaymentRepository>()
				.AddSingleton<IInventoryProductRepository, InventoryProductRepository>()
				.AddSingleton<IStoreConstantRepository, StoreConstantRepository>()
				.AddSingleton<IUserRepository, UserRepository>()
				.AddSingleton<IPayLaterPaymentRepository, PayLaterRepository>();

		return services;
	}

	internal static IServiceCollection AddIndyPosControllers(this IServiceCollection services)
	{
		services.AddSingleton<IPayLaterPaymentController, PayLaterPaymentController>()
				.AddSingleton<IInventoryController, InventoryController>()
				.AddSingleton<IReportController, ReportController>()
				.AddSingleton<ISaleInvoiceController, SaleInvoiceController>()
				.AddSingleton<IUserController, UserController>();

		return services;
	}
}