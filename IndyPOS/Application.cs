using Autofac;
using IndyPOS.IoC;
using System;
using System.Diagnostics;
using System.Linq;

namespace IndyPOS
{
    internal static class Application
    {
        [STAThread]
		private static void Main()
        {
            var currentProcessId = Process.GetCurrentProcess().Id;
            var processes = Process.GetProcessesByName("IndyPOS")
								   .Where(p =>p.Id != currentProcessId)
								   .ToList();

            // Verify if either Process or DebugProcess has more than one instance
            if (processes.Any())
            {
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
                
                System.Threading.Thread.Sleep(1000);
            }

            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

            var container = ContainerConfig.Configure();

            using (var scope = container.BeginLifetimeScope())
            {
                scope.Resolve<IMachine>().Launch();
            }
        }
    }
}
