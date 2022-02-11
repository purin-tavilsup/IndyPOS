using Autofac;
using IndyPOS.IoC;
using Serilog;
using Serilog.Formatting.Json;
using Squirrel;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IndyPOS
{
    [ExcludeFromCodeCoverage]
    internal static class Application
	{
		private const string ProcessName = "IndyPOS";
		private static string _localReleaseDirectoryPath;
		private static string _remoteReleaseDirectoryPath;
		private static string _logDirectory;

        [STAThread]
		private static void Main()
		{
			var appSettings = ConfigurationManager.AppSettings;

			_localReleaseDirectoryPath = appSettings.Get("LocalReleasesDirectory");
			_remoteReleaseDirectoryPath = appSettings.Get("RemoteReleasesDirectory");
			_logDirectory = appSettings.Get("LogDirectory");

			UpdateApplication();

			System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

			ClosePreviousProcesses();
			ConfigureLogger();

			var container = ContainerConfig.Configure();

			LaunchApplication(container);
		}

		private static void ClosePreviousProcesses()
        {
			var currentProcessId = Process.GetCurrentProcess().Id;
			var processes = Process.GetProcessesByName(ProcessName)
								   .Where(p =>p.Id != currentProcessId)
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

        private static void ConfigureLogger()
        {
			if (!Directory.Exists(_logDirectory))
			{
				Directory.CreateDirectory(_logDirectory);
			}

			var logFilePath = $"{_logDirectory}\\log.json";

			Log.Logger = new LoggerConfiguration().MinimumLevel.Debug()
												  .WriteTo.File(new JsonFormatter(), logFilePath, rollingInterval: RollingInterval.Day)
												  .CreateLogger();
        }

        private static void LaunchApplication(IContainer container)
        {
			using (var scope = container.BeginLifetimeScope())
			{
				scope.Resolve<IMachine>().Launch();
			}
        }

		private static void CopyNewPackageFiles()
		{
			if (!Directory.Exists(_remoteReleaseDirectoryPath))
				return;

			if (!Directory.Exists(_localReleaseDirectoryPath))
			{
				Directory.CreateDirectory(_localReleaseDirectoryPath);
			}

            var originalFiles = Directory.GetFiles(_remoteReleaseDirectoryPath);

            foreach (var originalFile in originalFiles)
            {
                var fileName = Path.GetFileName(originalFile);
                var destinationFile = Path.Combine(_localReleaseDirectoryPath, fileName);

                File.Copy(originalFile, destinationFile, true);
            }
        }

		private static void DeletePreviousPackageFiles()
        {
			if (!Directory.Exists(_localReleaseDirectoryPath))
				return;

			var files = Directory.GetFiles(_localReleaseDirectoryPath, "*.nupkg");

			foreach (var file in files)
            {
				File.Delete(file);
            }
        }

		[Conditional("RELEASE")]
		private static void UpdateApplication()
        {
			DeletePreviousPackageFiles();
			CopyNewPackageFiles();

			Task.Run(CheckAndApplyUpdate).GetAwaiter().GetResult();
        }

		private static async Task CheckAndApplyUpdate()
		{
			var updated = false;

			using (var updateManager = new UpdateManager(_localReleaseDirectoryPath))
			{
				var updateInfo = await updateManager.CheckForUpdate();

				if (updateInfo.ReleasesToApply != null && 
					updateInfo.ReleasesToApply.Count > 0)
				{
					await updateManager.UpdateApp();
					updated = true;
				}
			}

			if (updated)
			{
				UpdateManager.RestartApp($"{ProcessName}.exe");
			}
		}
    }
}
