using System;
using System.Linq;

namespace IndyPOS
{
    static class Application
    {
        [STAThread]
        static void Main()
        {
            var currentProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
            var processes = System.Diagnostics.Process.GetProcessesByName("IndyPOS.exe");
            var debugProcesses = System.Diagnostics.Process.GetProcessesByName("IndyPOS.vshost");

            // Verify if either Process or DebugProcess has more than one instance
            if (processes.Any() || debugProcesses.Any())
            {
                // Kill all previous processes
                foreach (var process in processes)
                {
                    if (process.Id == currentProcessId)
                        continue;

                    process.Kill();
                    process.WaitForExit(2000);
                }
                
                System.Threading.Thread.Sleep(1000);
            }

            Machine.Instance.Launch();
        }
    }
}
