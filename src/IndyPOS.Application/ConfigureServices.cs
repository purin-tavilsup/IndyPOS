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
		var firstScanned = services.Count;
		services.AddNokpirabFromAssembly(assembly);
		RemoveOpenGenericHandlers(services, firstScanned);

		return services;
    }

	// Nokpirab 1.1.0's scan registers a generic handler class as written, e.g.
	// DeleteCashEntryCommandHandler<TEntry> for ICommandHandler<DeleteCashEntryCommand<TEntry>>. The
	// container cannot build that shape and throws while building, which stopped the till at startup.
	// Hosts that need a generic handler register its closed forms themselves (StoreHub's cash drawer).
	// Only the scan's own registrations are touched: the host may already hold valid open generics,
	// such as ILogger<> -> Logger<>, registered before this runs.
	private static void RemoveOpenGenericHandlers(IServiceCollection services, int firstScanned)
	{
		for (var i = services.Count - 1; i >= firstScanned; i--)
		{
			if (services[i].ImplementationType is { IsGenericTypeDefinition: true })
				services.RemoveAt(i);
		}
	}
}