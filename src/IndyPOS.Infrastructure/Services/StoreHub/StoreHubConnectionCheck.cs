using System.Net;
using IndyPOS.Application.Abstractions.StoreHub;

namespace IndyPOS.Infrastructure.Services.StoreHub;

/// <summary>
/// One readiness probe of the configured StoreHub, classified for a person: up, up without its
/// database, refused, too slow, or something unexpected.
/// </summary>
public sealed class StoreHubConnectionCheck(HttpClient httpClient) : IStoreHubConnectionCheck
{
    /// <summary>Short on purpose: a person is waiting on the button.</summary>
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public async Task<StoreHubConnectionResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClient.GetAsync(StoreHubRoutes.HealthReady, cancellationToken);
            return Classify(response.StatusCode);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException)
        {
            return new StoreHubConnectionResult(StoreHubConnectionStatus.TimedOut);
        }
        catch (HttpRequestException)
        {
            return new StoreHubConnectionResult(StoreHubConnectionStatus.Unreachable);
        }
    }

    private static StoreHubConnectionResult Classify(HttpStatusCode code) => code switch
    {
        >= HttpStatusCode.OK and < HttpStatusCode.MultipleChoices => new(StoreHubConnectionStatus.Healthy),
        HttpStatusCode.ServiceUnavailable => new(StoreHubConnectionStatus.NotReady, (int)code),
        _ => new(StoreHubConnectionStatus.Unexpected, (int)code)
    };
}
