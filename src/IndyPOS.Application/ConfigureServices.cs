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
		RemoveOpenGenericHandlers(services);

		return services;
    }

	// Nokpirab 1.1.0's scan registers a generic handler class as written, e.g.
	// DeleteCashEntryCommandHandler<TEntry> for ICommandHandler<DeleteCashEntryCommand<TEntry>>. The
	// container cannot build that shape and throws while building, which stopped the till at startup.
	// Hosts that need a generic handler register its closed forms themselves (StoreHub's cash drawer).
	private static void RemoveOpenGenericHandlers(IServiceCollection services)
	{
		var openGenerics = services.Where(d => d.ImplementationType is { IsGenericTypeDefinition: true })
								   .ToList();

		foreach (var descriptor in openGenerics)
		{
			services.Remove(descriptor);
		}
	}
}