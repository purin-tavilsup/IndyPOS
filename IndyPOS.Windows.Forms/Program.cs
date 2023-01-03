using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Runtime.Versioning;
using IndyPOS.Common.Interfaces;
using IndyPOS.Windows.Forms.Constants;
using IndyPOS.Windows.Forms.Extensions;
using IndyPOS.Windows.Forms.Interfaces;
using LazyCache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prism.Events;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace IndyPOS.Windows.Forms;

[ExcludeFromCodeCoverage]
[type:SupportedOSPlatform("windows")]
internal static class Program
{
	private const string ProcessName = "IndyPOS";
	private const string LogDirectory = @"C:\\ProgramData\\IndyPOS\\Logs";

	[STAThread]
	private static void Main()
	{
		// To customize application configuration such as set high DPI settings or default font,
		// see https://aka.ms/applicationconfiguration.
		ApplicationConfiguration.Initialize();

		ClosePreviousProcesses();
		ConfigureLogger();

		try
		{
			Log.Information("Starting application");

			var host = CreateHost();

			// Launch Application
			host.Services.GetRequiredService<IMachine>().Launch();
		}
		catch (Exception ex)
		{
			Log.Fatal(ex, "Application terminated unexpectedly");
		}
		finally
		{
			Log.CloseAndFlush();
		}
	}

	private static void ConfigureLogger()
	{
		if (!Directory.Exists(LogDirectory))
		{
			Directory.CreateDirectory(LogDirectory);
		}

		const string logFilePath = $"{LogDirectory}\\log.json";

		Log.Logger = new LoggerConfiguration().MinimumLevel.Debug()
											  .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
											  .Enrich.FromLogContext()
											  .WriteTo.File(new CompactJsonFormatter(), logFilePath, rollingInterval: RollingInterval.Day)
											  .CreateLogger();
	}

	private static IHost CreateHost()
	{
		return Host.CreateDefaultBuilder()
				   .UseSerilog()
				   .ConfigureAppConfiguration(BuildAppConfiguration)
				   .ConfigureServices(AddServices)
				   .Build();
	}

	private static void BuildAppConfiguration(HostBuilderContext context, IConfigurationBuilder configBuilder)
	{
		configBuilder.SetBasePath(Directory.GetCurrentDirectory())
					 .AddJsonFile("appsettings.json")
					 .Build();
	}

	private static void AddServices(HostBuilderContext context, IServiceCollection services)
	{
		services.AddHelpers()
				.AddUserInterfaces()
				.AddRepositories()
				.AddIndyPosControllers()
				.AddUtilities()
				.AddSingleton<IEventAggregator, EventAggregator>()
				.AddSingleton<IStoreConstants, StoreConstants>()
				.AddSingleton<IAppCache, CachingService>()
				.AddSingleton<HttpClient, HttpClient>()
				.AddSingleton<IMachine, Machine>();
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