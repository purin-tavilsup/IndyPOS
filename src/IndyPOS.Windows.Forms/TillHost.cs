using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Runtime.Versioning;

namespace IndyPOS.Windows.Forms;

/// <summary>
/// The till's composition root, kept apart from <see cref="Program"/> so a test can build the real
/// container. In Development the default host validates every registration on build, so one service
/// the container cannot construct stops the till before any window opens.
/// </summary>
[type:SupportedOSPlatform("windows")]
public static class TillHost
{
	public static IHostBuilder CreateBuilder()
	{
		return Host.CreateDefaultBuilder()
				   .UseSerilog()
				   .ConfigureAppConfiguration(BuildAppConfiguration)
				   .ConfigureServices(AddServices);
	}

	private static void BuildAppConfiguration(HostBuilderContext context, IConfigurationBuilder configBuilder)
	{
		// Base on the executable's own directory, not the current working
		// directory — the app can be launched with an arbitrary CWD (the
		// installer's Finish button inherits the bootstrapper's CWD), and
		// appsettings.json always ships next to the exe.
		// Environment variables are added last so the Aspire AppHost can override
		// StoreHub__BaseUrl in dev without changing the shipped appsettings.json.
		configBuilder.SetBasePath(AppContext.BaseDirectory)
					 .AddJsonFile("appsettings.json")
					 .AddEnvironmentVariables();
	}

	private static void AddServices(HostBuilderContext context, IServiceCollection services)
	{
		services.AddApplicationServices()
				.AddUIServices()
				.AddInfrastructureServices(context.Configuration)
				.AddStoreHubClientServices(context.Configuration); // Epic G: StoreHub integration
	}
}
