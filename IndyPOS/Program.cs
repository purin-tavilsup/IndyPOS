using IndyPOS.Common.Interfaces;
using IndyPOS.Constants;
using IndyPOS.Extensions;
using IndyPOS.Interfaces;
using LazyCache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prism.Events;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Runtime.Versioning;

namespace IndyPOS;

[ExcludeFromCodeCoverage]
[type:SupportedOSPlatform("windows")]
internal static class Program
{
	private const string ProcessName = "IndyPOS";

	[STAThread]
	private static void Main()
	{
		// To customize application configuration such as set high DPI settings or default font,
		// see https://aka.ms/applicationconfiguration.
		ApplicationConfiguration.Initialize();

		ClosePreviousProcesses();

		var host = CreateHost();

		// Launch Application
		host.Services.GetRequiredService<IMachine>().Launch();
	}

	private static IHost CreateHost()
	{
		return Host.CreateDefaultBuilder()
				   .ConfigureAppConfiguration(CreateAppConfiguration)
				   .ConfigureServices(AddServices)
				   .Build();
	}

	private static void CreateAppConfiguration(HostBuilderContext context, IConfigurationBuilder configBuilder)
	{
		configBuilder.SetBasePath(Directory.GetCurrentDirectory())
					 .AddJsonFile("appsettings.json")
					 .Build();
	}

	private static void AddServices(HostBuilderContext context, IServiceCollection services)
	{
		services.AddLogger(context)
				.AddHelpers()
				.AddUserInterfaces()
				.AddRepositories()
				.AddControllers()
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