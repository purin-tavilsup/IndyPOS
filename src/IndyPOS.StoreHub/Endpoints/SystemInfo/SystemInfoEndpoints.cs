namespace IndyPOS.StoreHub.Endpoints.SystemInfo;

/// <summary>The two unauthenticated routes that say what is running: a banner and the build's version.</summary>
public static class SystemInfoEndpoints
{
    public static IEndpointRouteBuilder MapSystemInfoEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", () => "IndyPOS StoreHub API");
        MapVersion(app);
        return app;
    }

    // Version endpoint (Velopack prep - used for update checks)
    private static void MapVersion(IEndpointRouteBuilder app)
    {
        app.MapGet("/version", (IWebHostEnvironment environment) =>
        {
            var versionInfo = IndyPOS.Application.Common.AppVersion.GetVersionInfo(typeof(Program).Assembly);

            return Results.Ok(new
            {
                version = versionInfo.DisplayVersion,
                assemblyVersion = versionInfo.AssemblyVersion,
                fullVersion = versionInfo.InformationalVersion,
                name = "IndyPOS.StoreHub",
                environment = environment.EnvironmentName
            });
        });
    }
}
