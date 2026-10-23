using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using IndyPOS.Application.Abstractions.Cloud.Auth;
using IndyPOS.Application.Common.Constants;
using Microsoft.Extensions.Http.Resilience;

namespace IndyPOS.CloudApi.Infrastructure.Auth;

/// <summary>
/// Cloud.Auth's store-client API, called with CloudApi's own service client (client credentials, scope
/// auth.store-clients). The token is cached process-wide (<see cref="AuthServiceTokenCache"/>), because DI creates this
/// typed client per scope.
/// </summary>
public sealed class HttpAuthStoreClients(
    HttpClient httpClient, AuthServiceTokenCache tokens, IConfiguration configuration, ILogger<HttpAuthStoreClients> logger)
    : IAuthStoreClients
{
    public const string ServiceClientId = "cloud-api";
    public const string ServiceClientSecretKey = "ServiceClients:CloudApi:ClientSecret";

    public async Task<StoreClientCredentials> CreateAsync(string storeId, string displayName, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Put, $"/store-clients/{Uri.EscapeDataString(storeId)}",
            () => JsonContent.Create(new { displayName }), cancellationToken);

        return await response.Content.ReadFromJsonAsync<StoreClientCredentials>(cancellationToken)
            ?? throw new AuthUnavailableException("Auth answered the store-client request with no body.");
    }

    public async Task DeleteAsync(string storeId, CancellationToken cancellationToken = default)
    {
        using var _ = await SendAsync(HttpMethod.Delete, $"/store-clients/{Uri.EscapeDataString(storeId)}", () => null, cancellationToken);
    }

    public async Task DisableAsync(string storeId, CancellationToken cancellationToken = default)
    {
        using var _ = await SendAsync(HttpMethod.Post, $"/store-clients/{Uri.EscapeDataString(storeId)}/disable", () => null, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, Func<HttpContent?> content, CancellationToken cancellationToken)
    {
        var response = await SendOnceAsync(method, path, content(), cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return EnsureAnswered(response, method, path);

        // A rotated service secret or a new Auth key makes the cached token useless: get a fresh one, once.
        response.Dispose();
        tokens.Clear();
        return EnsureAnswered(await SendOnceAsync(method, path, content(), cancellationToken), method, path);
    }

    private async Task<HttpResponseMessage> SendOnceAsync(HttpMethod method, string path, HttpContent? content, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(method, path) { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await TokenAsync(cancellationToken));
            return await httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is not AuthUnavailableException && !(ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
        {
            // No answer at all: network, timeout, or the resilience pipeline giving up (Polly's own exceptions).
            throw new AuthUnavailableException($"Auth could not be reached for {method} {path}.", ex);
        }
    }

    /// <remarks>The text of Auth's error is logged, never put in the message, which reaches the admin's response.</remarks>
    private HttpResponseMessage EnsureAnswered(HttpResponseMessage response, HttpMethod method, string path)
    {
        if (response.IsSuccessStatusCode)
            return response;

        var status = (int)response.StatusCode;
        logger.LogWarning("Auth answered {Status} to {Method} {Path}: {Body}", status, method, path,
            response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
        response.Dispose();

        if (status == StatusCodes.Status409Conflict)
            throw new InvalidOperationException("The store was deactivated in Cloud.Auth. A store that comes back is registered again as a new store.");

        throw new AuthUnavailableException($"Auth answered {status} to {method} {path}.");
    }

    private Task<string> TokenAsync(CancellationToken cancellationToken) =>
        tokens.GetAsync(async () =>
        {
            var secret = configuration[ServiceClientSecretKey] is { Length: > 0 } s
                ? s
                : throw new AuthUnavailableException($"{ServiceClientSecretKey} is not set, so CloudApi cannot call Auth.");

            using var response = await httpClient.PostAsync("/oauth/token", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = ServiceClientId,
                ["client_secret"] = secret,
                ["scope"] = AuthScopes.StoreClients
            }), cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new AuthUnavailableException($"Auth refused CloudApi's service client ({(int)response.StatusCode}).");

            var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken)
                ?? throw new AuthUnavailableException("Auth's token response was empty.");
            return (token.AccessToken, TimeSpan.FromSeconds(token.ExpiresIn));
        });

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}

/// <summary>CloudApi's service token for Auth, shared by every HttpAuthStoreClients in the process.</summary>
public sealed class AuthServiceTokenCache
{
    private static readonly TimeSpan RefreshEarly = TimeSpan.FromSeconds(30);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _token;
    private DateTime _expiresAtUtc = DateTime.MinValue;

    /// <summary>Forgets the token, so the next call fetches a fresh one.</summary>
    public void Clear() => (_token, _expiresAtUtc) = (null, DateTime.MinValue);

    public async Task<string> GetAsync(Func<Task<(string Token, TimeSpan Lifetime)>> fetch)
    {
        if (_token is { } cached && DateTime.UtcNow < _expiresAtUtc - RefreshEarly)
            return cached;

        await _gate.WaitAsync();
        try
        {
            if (_token is null || DateTime.UtcNow >= _expiresAtUtc - RefreshEarly)
            {
                var (token, lifetime) = await fetch();
                (_token, _expiresAtUtc) = (token, DateTime.UtcNow + lifetime);
            }

            return _token;
        }
        finally
        {
            _gate.Release();
        }
    }
}

public static class AuthStoreClientsRegistration
{
    /// <summary>
    /// The store-client API's typed client, with retries only for safe methods: a create retried after a slow first
    /// attempt could leave Auth with a different secret than the one shown to the admin.
    /// </summary>
    public static IServiceCollection AddAuthStoreClients(this IServiceCollection services)
    {
        services.AddSingleton<AuthServiceTokenCache>();
        // RemoveAllResilienceHandlers is marked experimental (EXTEXP0001); it is the documented way to replace the
        // standard handler ServiceDefaults adds to every client, and only this client needs different retries.
#pragma warning disable EXTEXP0001
        services.AddHttpClient<IAuthStoreClients, HttpAuthStoreClients>((provider, client) =>
                    client.BaseAddress = AuthBaseAddress(provider.GetRequiredService<IConfiguration>()))
                .RemoveAllResilienceHandlers()
                .AddStandardResilienceHandler(options => options.Retry.DisableForUnsafeHttpMethods());
#pragma warning restore EXTEXP0001
        return services;
    }

    private static Uri AuthBaseAddress(IConfiguration configuration) =>
        new(configuration["services:cloud-auth:https:0"] ?? configuration["services:cloud-auth:http:0"] ?? "https+http://cloud-auth");
}
