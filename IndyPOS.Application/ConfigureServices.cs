using FluentValidation;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Helpers;
using MediatR;
using System.Reflection;
using System.Runtime.Versioning;

// ReSharper disable CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

[type: SupportedOSPlatform("windows")]
public static class ConfigureServices
{
	public static IServiceCollection AddApplicationServices(this IServiceCollection services)
	{
		services.AddAutoMapper(Assembly.GetExecutingAssembly())
				.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly())
				.AddMediatR(Assembly.GetExecutingAssembly());

        services.AddSingleton<ISaleInvoiceHelper, SaleInvoiceHelper>()
				.AddSingleton<IInventoryHelper, InventoryHelper>()
				.AddSingleton<IUserHelper, UserHelper>()
				.AddSingleton<IPayLaterPaymentHelper, PayLaterPaymentHelper>()
				.AddSingleton<IReportHelper, ReportHelper>();

		return services;
    }
}