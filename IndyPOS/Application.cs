using Autofac;
using IndyPOS.Interfaces;
using IndyPOS.IoC;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace IndyPOS
{
	[ExcludeFromCodeCoverage]
    internal static class Application
	{
		private const string ProcessName = "IndyPOS";

        [STAThread]
		private static void Main()
		{
			System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

			ClosePreviousProcesses();

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

        private static void LaunchApplication(IContainer container)
        {
			using (var scope = container.BeginLifetimeScope())
			{
				scope.Resolve<IMachine>().Launch();
			}
        }
	}
}
