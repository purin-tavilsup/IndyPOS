using IndyPOS.Bootstrapper.UI;

namespace IndyPOS.Bootstrapper;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        // Check if running as admin
        if (!IsRunningAsAdmin())
        {
            MessageBox.Show(
                "IndyPOS Setup requires administrator privileges.\n\n" +
                "Please right-click and select 'Run as administrator'.",
                "Administrator Required",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        Application.Run(new InstallationWizard());
    }

    private static bool IsRunningAsAdmin()
    {
        using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        var principal = new System.Security.Principal.WindowsPrincipal(identity);
        return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
    }
}
