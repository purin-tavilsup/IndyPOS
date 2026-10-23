using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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

    public async Task<SyncSendResult> SendEventAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default)
    {
        var token = await _tokenService.GetAccessTokenAsync(cancellationToken);
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning(
                "Unable to acquire token for event {EventId}, operating in offline mode",
                outboxEvent.Id);
            return SyncSendResult.RetryLater("No access token could be obtained");
        }

        var request = new SyncEventsRequest(
        [
            new SyncEventRequest(
                EventId: outboxEvent.Id,
                StoreId: ParseStoreId(outboxEvent.StoreId),
                EventType: outboxEvent.Type,
                Payload: outboxEvent.PayloadJson,
                CreatedAtUtc: outboxEvent.CreatedUtc)
        ]);

        var result = await SendWithAuthAsync(request, token, cancellationToken);

        if (result.Outcome == SyncSendOutcome.Sent)
        {
            _logger.LogInformation(
                "Successfully synced event {EventId} ({Type}) for store {StoreId}",
                outboxEvent.Id,
                outboxEvent.Type,
                outboxEvent.StoreId);
        }

        return result;
    }

    private async Task<SyncSendResult> SendWithAuthAsync(
        SyncEventsRequest request,
        string token,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await PostEventsAsync(request, token, cancellationToken);

            if (response.StatusCode != HttpStatusCode.Unauthorized)
                return await ClassifyAsync(response, request, cancellationToken);

            _logger.LogWarning("Received 401 from Cloud API, clearing cached token and retrying");
            _tokenService.ClearCachedToken();

            var newToken = await _tokenService.GetAccessTokenAsync(cancellationToken);
            if (string.IsNullOrEmpty(newToken))
                return SyncSendResult.RetryLater("401 from the cloud, and no fresh token could be obtained");

            using var retryResponse = await PostEventsAsync(request, newToken, cancellationToken);
            return await ClassifyAsync(retryResponse, request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HTTP error sending event to Cloud API (offline mode?)");
            return SyncSendResult.RetryLater($"Cloud unreachable: {ex.Message}");
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Request to Cloud API timed out");
            return SyncSendResult.RetryLater("The request to the cloud timed out");
        }
    }

    private async Task<HttpResponseMessage> PostEventsAsync(
        SyncEventsRequest request,
        string token,
        CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/sync/events");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        httpRequest.Content = JsonContent.Create(request);

        return await _httpClient.SendAsync(httpRequest, cancellationToken);
    }

    /// <remarks>
    /// Only a 400 or 403 carrying CloudApi's <see cref="SyncRefusal"/> marker is Refused. Any other
    /// failure status may come from something that is not the sync handler at all (a proxy, a route not
    /// deployed yet, the auth layer), and refusing on it would park every event of every store as
    /// Failed on one bad deploy, so it is treated like an outage.
    /// </remarks>
    private async Task<SyncSendResult> ClassifyAsync(
        HttpResponseMessage response,
        SyncEventsRequest request,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return await WasStoredAsync(response, request, cancellationToken);

        var status = (int)response.StatusCode;
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Forbidden
            && ReadRefusal(response, body) is { } refusal)
        {
            _logger.LogWarning("Cloud API refused the event: {Status} {Refusal}", status, refusal);
            return SyncSendResult.Refused($"{status} {refusal}");
        }

        _logger.LogWarning("Cloud API returned {StatusCode}: {Error}", response.StatusCode, body);
        return SyncSendResult.RetryLater($"{status} from the cloud");
    }

    /// <summary>"code: detail" from a problem details body with a sync refusal, or null.</summary>
    private static string? ReadRefusal(HttpResponseMessage response, string body)
    {
        if (response.Content.Headers.ContentType?.MediaType != "application/problem+json")
            return null;

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty(SyncRefusal.ExtensionKey, out var code)
                || code.ValueKind != JsonValueKind.String)
                return null;

            var detail = root.TryGetProperty("detail", out var d) && d.ValueKind == JsonValueKind.String ? d.GetString() : null;
            return detail is null ? code.GetString() : $"{code.GetString()}: {detail}";
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <remarks>
    /// A 2xx means only that the batch was read. The cloud reports each event's outcome in the body,
    /// and answers 200 even when it failed to store one. Counting the status as success let
    /// SyncWorker mark a failed event Sent, and a single ingest failure lost it for good. So only
    /// the cloud's own "accepted" for this event counts; anything else is a failure and retries.
    /// </remarks>
    private async Task<SyncSendResult> WasStoredAsync(
        HttpResponseMessage response,
        SyncEventsRequest request,
        CancellationToken cancellationToken)
    {
        var eventId = request.Events.Single().EventId;

        try
        {
            var body = await response.Content.ReadFromJsonAsync<SyncEventsResponse>(cancellationToken);
            var result = body?.Results?.FirstOrDefault(r => r.EventId == eventId);

            if (result is { Accepted: true })
                return SyncSendResult.Sent;

            var reason = result?.Reason ?? "no result for this event in the response";
            _logger.LogWarning("Cloud API did not store event {EventId}: {Reason}", eventId, reason);
            return SyncSendResult.RetryLater($"The cloud did not store the event: {reason}");
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Cloud API sent an unreadable sync response for event {EventId}", eventId);
            return SyncSendResult.RetryLater("The cloud sent an unreadable sync response");
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
