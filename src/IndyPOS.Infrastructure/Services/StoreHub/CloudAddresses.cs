using Microsoft.Extensions.Configuration;

namespace IndyPOS.Infrastructure.Services.StoreHub;

/// <summary>
/// Where StoreHub reaches the cloud. Tokens come from Cloud.Auth; events, users and heartbeats go to CloudApi. Each is
/// the Aspire service-discovery setting (https first), falling back to discovery by name.
/// </summary>
public static class CloudAddresses
{
    public static Uri Auth(IConfiguration configuration) => Resolve(configuration, "cloud-auth");

    public static Uri CloudApi(IConfiguration configuration) => Resolve(configuration, "cloud-api");

    private static Uri Resolve(IConfiguration configuration, string service) =>
        new(configuration[$"services:{service}:https:0"]
            ?? configuration[$"services:{service}:http:0"]
            ?? $"https+http://{service}");
}
