using FluentValidation;
using IndyPOS.Application.Common.Behaviors;
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
		services.AddAutoMapper(Assembly.GetExecutingAssembly());
		services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
		services.AddMediatR(Assembly.GetExecutingAssembly());

		services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

		services.AddSingleton<ISaleInvoiceHelper, SaleInvoiceHelper>();
		services.AddSingleton<IInventoryHelper, InventoryHelper>();
		services.AddSingleton<IUserHelper, UserHelper>();
		services.AddSingleton<IPayLaterPaymentHelper, PayLaterPaymentHelper>();
		services.AddSingleton<IReportHelper, ReportHelper>();

		return services;
    }
}