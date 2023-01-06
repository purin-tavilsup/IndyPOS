using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Helpers;
using System.Runtime.Versioning;

// ReSharper disable CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

[type: SupportedOSPlatform("windows")]
public static class ConfigureServices
{
	public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<IStoreConfigurationHelper, StoreConfigurationHelper>()
				.AddSingleton<IBarcodeScannerHelper, BarcodeScannerHelper>()
				.AddSingleton<IReceiptPrinterHelper, ReceiptPrinterHelper>()
                .AddSingleton<ISaleInvoiceHelper, SaleInvoiceHelper>()
                .AddSingleton<IInventoryHelper, InventoryHelper>()
                .AddSingleton<IUserHelper, UserHelper>()
				.AddSingleton<IPayLaterPaymentHelper, PayLaterPaymentHelper>()
                .AddSingleton<IReportHelper, ReportHelper>()
                .AddSingleton<IDataFeedApiHelper, DataFeedApiHelper>();

		return services;
    }
}