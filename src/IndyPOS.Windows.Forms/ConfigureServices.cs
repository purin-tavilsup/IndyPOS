using IndyPOS.Windows.Forms;
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
using System.Runtime.Versioning;

// ReSharper disable CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

[type: SupportedOSPlatform("windows")]
internal static class ConfigureServices
{
	internal static IServiceCollection AddUIServices(this IServiceCollection services)
    {
		services.AddSingleton<InvoiceProductsReportPanel>()
				.AddSingleton<SalesHistoryReportPanel>()
				.AddSingleton<SalesReportPanel>()
				.AddSingleton<CashFlowCalculatorPanel>()
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
				.AddSingleton<PayLaterPaymentsReportPanel>()
				.AddSingleton<SaleHistoryByInvoiceIdForm>()
				.AddSingleton<SalePanel>()
				.AddSingleton<SettingsPanel>()
				.AddSingleton<UpdateInventoryProductForm>()
				.AddSingleton<UpdateInvoiceProductForm>()
				.AddSingleton<UserLogInPanel>()
				.AddSingleton<UsersPanel>();

		services.AddSingleton<IMachine, Machine>();

		return services;
    }
}