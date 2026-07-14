using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IndyPOS.Application.Abstractions.StoreHub.Services;
using IndyPOS.Application.UseCases.Cloud.Sync;
using IndyPOS.Application.UseCases.StoreHub.Users;
using IndyPOS.Domain.Entities.Core;
using Microsoft.Extensions.Logging;

namespace IndyPOS.Infrastructure.Services.StoreHub;

/// <summary>
/// HTTP client for syncing events to Cloud API with OAuth2 Bearer authentication.
/// Handles token acquisition, retry on 401, and graceful offline degradation.
/// BaseAddress is configured via Aspire service discovery in ConfigureServices.
/// </summary>
public class HttpCloudSyncClient : ICloudSyncClient
{
    private readonly HttpClient _httpClient;
    private readonly ITokenService _tokenService;
    private readonly ILogger<HttpCloudSyncClient> _logger;

    public HttpCloudSyncClient(
        HttpClient httpClient,
        ITokenService tokenService,
        ILogger<HttpCloudSyncClient> logger)
    {
        _httpClient = httpClient;
        _tokenService = tokenService;
        _logger = logger;
        // BaseAddress is configured via DI in ConfigureServices (Aspire service discovery)
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

    public async Task<CloudUserSyncResponse?> GetUsersAsync(
        string storeId,
        long? sinceVersion = null,
        CancellationToken cancellationToken = default)
    {
        // Get access token
        var token = await _tokenService.GetAccessTokenAsync(cancellationToken);
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Unable to acquire token for user sync, operating in offline mode");
            return null;
        }

        return await GetUsersWithAuthAsync(storeId, sinceVersion, token, cancellationToken);
    }

    private async Task<CloudUserSyncResponse?> GetUsersWithAuthAsync(
        string storeId,
        long? sinceVersion,
        string token,
        CancellationToken cancellationToken)
    {
        try
        {
            var url = $"/master/users/{storeId}";
            if (sinceVersion.HasValue)
            {
                url += $"?sinceVersion={sinceVersion.Value}";
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CloudUserSyncResponse>(cancellationToken);
            }

            // Handle 401 - token may have expired
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Received 401 from Cloud API during user sync, clearing cached token and retrying");
                _tokenService.ClearCachedToken();

                // Try once more with fresh token
                var newToken = await _tokenService.GetAccessTokenAsync(cancellationToken);
                if (string.IsNullOrEmpty(newToken))
                {
                    return null;
                }

                using var retryRequest = new HttpRequestMessage(HttpMethod.Get, url);
                retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);

                var retryResponse = await _httpClient.SendAsync(retryRequest, cancellationToken);
                if (retryResponse.IsSuccessStatusCode)
                {
                    return await retryResponse.Content.ReadFromJsonAsync<CloudUserSyncResponse>(cancellationToken);
                }
            }

            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Cloud API returned {StatusCode} during user sync: {Error}",
                response.StatusCode,
                errorBody);

            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HTTP error during user sync (offline mode?)");
            return null;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "User sync request timed out");
            return null;
        }
    }
}
