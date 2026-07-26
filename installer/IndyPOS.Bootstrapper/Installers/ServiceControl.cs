using System.ServiceProcess;

namespace IndyPOS.Bootstrapper.Installers;

/// <summary>Result of a start/stop attempt, carrying the failure reason for diagnostics.</summary>
public sealed record ServiceControlResult(bool Success, string? ErrorMessage);

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

    /// <summary>Succeeds when the service is stopped afterwards, including when absent.</summary>
    public async Task<ServiceControlResult> StopAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
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

            return new ServiceControlResult(true, null);
        }
        catch (InvalidOperationException)
        {
            // Service doesn't exist - that's fine.
            return new ServiceControlResult(true, null);
        }
        catch (System.ServiceProcess.TimeoutException ex)
        {
            return new ServiceControlResult(false, ex.Message);
        }
    }

    public async Task<ServiceControlResult> StartAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        try
        {
            using var sc = new ServiceController(_serviceName);

            if (sc.Status == ServiceControllerStatus.Running)
            {
                return new ServiceControlResult(true, null);
            }

            sc.Start();
            await Task.Run(
                () => sc.WaitForStatus(ServiceControllerStatus.Running, timeout),
                cancellationToken);

            return new ServiceControlResult(true, null);
        }
        catch (Exception ex)
        {
            return new ServiceControlResult(false, ex.Message);
        }
    }
}
