namespace IndyPOS.Application.Abstractions.StoreHub;

/// <summary>StoreHub paths the till calls from more than one place.</summary>
public static class StoreHubRoutes
{
    /// <summary>Readiness: StoreHub is up and its own database answers. Mapped in every environment.</summary>
    public const string HealthReady = "/health/ready";
}
