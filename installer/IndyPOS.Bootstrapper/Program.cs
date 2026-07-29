using IndyPOS.Bootstrapper.Installers;
using IndyPOS.Bootstrapper.Silent;
using IndyPOS.Bootstrapper.UI;
using IndyPOS.Bootstrapper.Upgrade;

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

        // One detection result, one router. If the wizard and the silent path each called
        // the detector, the two forks would drift.
        var probeConfig = new InstallationConfig { StoreId = "pending", Interactive = true };
        var detected = InstallModeDetector.Detect(
            new WindowsInstallProbe(probeConfig), probeConfig.StoreHubInstallPath);

        if (detected.Mode != InstallMode.Fresh)
        {
            MessageBox.Show(
                detected.Mode == InstallMode.Upgrade
                    ? InstallationWizard.BuildExistingInstallMessage(detected)
                    : detected.Reason,
                "Existing Installation Detected",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return 0;
        }

        Application.Run(new InstallationWizard());
        return 0;
    }
}
