using FluentValidation;
using System.Reflection;
using System.Runtime.Versioning;
using Nokpirab;

// ReSharper disable CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

[type: SupportedOSPlatform("windows")]
public static class ConfigureServices
{
	public static IServiceCollection AddApplicationServices(this IServiceCollection services)
	{
		var assembly = Assembly.GetExecutingAssembly();
		services.AddValidatorsFromAssembly(assembly);
		services.AddNokpirabFromAssembly(assembly);

		return services;
    }
}