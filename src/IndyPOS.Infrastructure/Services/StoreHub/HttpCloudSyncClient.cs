using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IndyPOS.Application.Abstractions.StoreHub.Services;
using IndyPOS.Application.UseCases.Cloud.Sync;
using IndyPOS.Domain.Entities.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IndyPOS.Infrastructure.Services.StoreHub;

/// <summary>
/// HTTP client for syncing events to Cloud API with OAuth2 Bearer authentication.
/// Handles token acquisition, retry on 401, and graceful offline degradation.
/// </summary>
public class HttpCloudSyncClient : ICloudSyncClient
{
    private readonly HttpClient _httpClient;
    private readonly ITokenService _tokenService;
    private readonly CloudTokenOptions _options;
    private readonly ILogger<HttpCloudSyncClient> _logger;

    public HttpCloudSyncClient(
        HttpClient httpClient,
        ITokenService tokenService,
        IOptions<CloudTokenOptions> options,
        ILogger<HttpCloudSyncClient> logger)
    {
        _httpClient = httpClient;
        _tokenService = tokenService;
        _options = options.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
    }

    public async Task<bool> SendEventAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default)
    {
        // Get access token
        var token = await _tokenService.GetAccessTokenAsync(cancellationToken);
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning(
                "Unable to acquire token for event {EventId}, operating in offline mode",
                outboxEvent.Id);
            return false;
        }

        // Convert to batch request (single event)
        var request = new SyncEventsRequest(
        [
            new SyncEventRequest(
                EventId: outboxEvent.Id,
                StoreId: ParseStoreId(outboxEvent.StoreId),
                EventType: outboxEvent.Type,
                Payload: outboxEvent.PayloadJson,
                CreatedAtUtc: outboxEvent.CreatedUtc)
        ]);

        // Send with authentication
        var success = await SendWithAuthAsync(request, token, cancellationToken);

        if (success)
        {
            _logger.LogInformation(
                "Successfully synced event {EventId} ({Type}) for store {StoreId}",
                outboxEvent.Id,
                outboxEvent.Type,
                outboxEvent.StoreId);
        }

        return success;
    }

    private async Task<bool> SendWithAuthAsync(
        SyncEventsRequest request,
        string token,
        CancellationToken cancellationToken)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/sync/events");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            httpRequest.Content = JsonContent.Create(request);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            // Handle 401 - token may have expired
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Received 401 from Cloud API, clearing cached token and retrying");
                _tokenService.ClearCachedToken();

                // Try once more with fresh token
                var newToken = await _tokenService.GetAccessTokenAsync(cancellationToken);
                if (string.IsNullOrEmpty(newToken))
                {
                    return false;
                }

                using var retryRequest = new HttpRequestMessage(HttpMethod.Post, "/sync/events");
                retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                retryRequest.Content = JsonContent.Create(request);

                var retryResponse = await _httpClient.SendAsync(retryRequest, cancellationToken);
                return retryResponse.IsSuccessStatusCode;
            }

            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Cloud API returned {StatusCode}: {Error}",
                response.StatusCode,
                errorBody);

            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HTTP error sending event to Cloud API (offline mode?)");
            return false;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Request to Cloud API timed out");
            return false;
        }
    }

    private static int ParseStoreId(string storeId)
    {
        // Handle string-to-int conversion for StoreId
        // This accommodates the difference between OutboxEvent.StoreId (string)
        // and SyncEventRequest.StoreId (int)
        return int.TryParse(storeId, out var id) ? id : 0;
    }
}
