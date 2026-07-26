using System.ServiceProcess;

namespace IndyPOS.Bootstrapper.Installers;

/// <summary>
/// Stop / start / query the StoreHub Windows service.
/// <para>USED BY BOTH INSTALL PATHS — fresh install and in-place upgrade. A change here
/// ships to every store on the next release, not just to upgrades.</para>
/// </summary>
public sealed class ServiceControl(string serviceName)
{
    private readonly string _serviceName = serviceName;

    public bool Exists() =>
        ServiceController.GetServices().Any(s => s.ServiceName == _serviceName);

    public bool IsRunning()
    {
        try
        {
            using var sc = new ServiceController(_serviceName);
            return sc.Status == ServiceControllerStatus.Running;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>Returns true when the service is stopped afterwards, including when absent.</summary>
    public async Task<bool> StopAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        try
        {
            using var sc = new ServiceController(_serviceName);

            if (sc.Status is ServiceControllerStatus.Running or ServiceControllerStatus.StartPending)
            {
                sc.Stop();
                await Task.Run(
                    () => sc.WaitForStatus(ServiceControllerStatus.Stopped, timeout),
                    cancellationToken);
            }

            return true;
        }
        catch (InvalidOperationException)
        {
            // Service doesn't exist - that's fine.
            return true;
        }
        catch (System.ServiceProcess.TimeoutException)
        {
            return false;
        }
    }

    public async Task<bool> StartAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        try
        {
            using var sc = new ServiceController(_serviceName);

            if (sc.Status == ServiceControllerStatus.Running)
            {
                return true;
            }

            sc.Start();
            await Task.Run(
                () => sc.WaitForStatus(ServiceControllerStatus.Running, timeout),
                cancellationToken);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
