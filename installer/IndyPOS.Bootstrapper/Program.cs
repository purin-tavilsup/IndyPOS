using IndyPOS.Bootstrapper.Silent;
using IndyPOS.Bootstrapper.UI;

namespace IndyPOS.Bootstrapper;

internal static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        var parse = SilentArgs.Parse(args);
        if (parse.Status != ParseStatus.NotSilent)
        {
            // Headless path: no message loop, no ApplicationConfiguration.Initialize.
            return SilentInstaller.Run(parse);
        }

        ApplicationConfiguration.Initialize();

        if (!Elevation.IsElevated())
        {
            MessageBox.Show(
                "IndyPOS Setup requires administrator privileges.\n\n" +
                "Please right-click and select 'Run as administrator'.",
                "Administrator Required",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return 0;
        }

        Application.Run(new InstallationWizard());
        return 0;
    }
}
