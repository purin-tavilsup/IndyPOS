using IndyPOS.Application.Common;
using IndyPOS.Windows.Forms.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Versioning;
using Velopack;
using Velopack.Sources;

namespace IndyPOS.Windows.Forms;

[ExcludeFromCodeCoverage]
[type:SupportedOSPlatform("windows")]
internal static class Program
{
	private const string ProcessName = "IndyPOS";

	/// <summary>
	/// Flag indicating this is the first run after Velopack installation.
	/// Checked by Machine.cs to trigger first-run wizard.
	/// </summary>
	public static bool IsFirstRun { get; private set; }

	[STAThread]
	private static void Main()
	{
		// IMPORTANT: VelopackApp.Build().Run() MUST be the first line in Main()
		// It handles Velopack hooks (install, update, uninstall) and exits early if needed
		VelopackApp.Build()
			.OnFirstRun(OnFirstRun)
			.Run();

		// To customize application configuration such as set high DPI settings or default font,
		// see https://aka.ms/applicationconfiguration.
		ApplicationConfiguration.Initialize();

		ClosePreviousProcesses();
		ConfigureLogger();

		try
		{
			Log.Information("Starting application");

			var host = CreateHost();

			host.Services
				.GetRequiredService<IMachine>()
				.Launch();
		}
		catch (Exception ex)
		{
			Log.Fatal(ex, "Application terminated unexpectedly");
		}
		finally
		{
			Log.Information("Stopping application");
			Log.CloseAndFlush();
		}
	}

	private static void ConfigureLogger()
	{
		var logDirectory = InstallPaths.LogsDirectory;

		if (!Directory.Exists(logDirectory))
		{
			Directory.CreateDirectory(logDirectory);
		}

		var logFilePath = Path.Combine(logDirectory, "log.json");

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
		// Base on the executable's own directory, not the current working
		// directory — the app can be launched with an arbitrary CWD (the
		// installer's Finish button inherits the bootstrapper's CWD), and
		// appsettings.json always ships next to the exe.
		configBuilder.SetBasePath(AppContext.BaseDirectory)
					 .AddJsonFile("appsettings.json")
					 .Build();
	}

	private static void AddServices(HostBuilderContext context, IServiceCollection services)
	{
		services.AddApplicationServices()
				.AddUIServices()
				.AddInfrastructureServices(context.Configuration)
				.AddStoreHubClientServices(context.Configuration); // Epic G: StoreHub integration
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

	/// <summary>
	/// Called by Velopack on first run after installation.
	/// Sets the IsFirstRun flag to trigger the first-run wizard.
	/// </summary>
	private static void OnFirstRun(NuGet.Versioning.SemanticVersion version)
	{
		Log.Information("First run after Velopack installation: {Version}", version);
		IsFirstRun = true;
	}
}