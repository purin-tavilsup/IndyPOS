using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using IndyPOS.Application.Abstractions.StoreHub.Services;
using IndyPOS.Application.UseCases.Cloud.Sync.Heartbeat;
using Microsoft.Extensions.Logging;

namespace IndyPOS.Infrastructure.Services.StoreHub;

/// <summary>
/// Sends StoreHub's heartbeat to POST /sync/heartbeats with the store token, as HttpCloudSyncClient sends events.
/// A missed beat is only logged: the next one goes on schedule.
/// </summary>
public class HttpCloudHeartbeatClient(HttpClient httpClient, ITokenService tokenService, ILogger<HttpCloudHeartbeatClient> logger)
    : ICloudHeartbeatClient
{
    public async Task<HeartbeatResponse?> SendAsync(HeartbeatRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await tokenService.GetAccessTokenAsync(cancellationToken);
            if (string.IsNullOrEmpty(token))
                return Missed("no access token could be obtained");

            using var response = await PostAsync(request, token, cancellationToken);
            if (response.StatusCode != HttpStatusCode.Unauthorized)
                return await ReadAsync(response, cancellationToken);

            tokenService.ClearCachedToken();
            var freshToken = await tokenService.GetAccessTokenAsync(cancellationToken);
            if (string.IsNullOrEmpty(freshToken))
                return Missed("401, and no fresh token could be obtained");

            using var retry = await PostAsync(request, freshToken, cancellationToken);
            return await ReadAsync(retry, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException
                                   || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            return Missed(ex.Message);
        }
    }

    private async Task<HttpResponseMessage> PostAsync(HeartbeatRequest request, string token, CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/sync/heartbeats");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        httpRequest.Content = JsonContent.Create(request);

        return await httpClient.SendAsync(httpRequest, cancellationToken);
    }

    private async Task<HeartbeatResponse?> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken) =>
        response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<HeartbeatResponse>(cancellationToken) ?? new HeartbeatResponse([])
            : Missed($"{(int)response.StatusCode} from the cloud");

    private HeartbeatResponse? Missed(string reason)
    {
        logger.LogWarning("Heartbeat missed: {Reason}", reason);
        return null;
    }
}
