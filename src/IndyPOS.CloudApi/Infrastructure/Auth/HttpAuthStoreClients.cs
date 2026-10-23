using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using IndyPOS.Application.Abstractions.Cloud.Auth;
using IndyPOS.Application.Common.Constants;

namespace IndyPOS.CloudApi.Infrastructure.Auth;

/// <summary>
/// Cloud.Auth's store-client API, called with CloudApi's own service client (client credentials, scope
/// auth.store-clients). The token is cached process-wide (<see cref="AuthServiceTokenCache"/>), because DI creates this
/// typed client per scope.
/// </summary>
public sealed class HttpAuthStoreClients(HttpClient httpClient, AuthServiceTokenCache tokens, IConfiguration configuration)
    : IAuthStoreClients
{
    public const string ServiceClientId = "cloud-api";
    public const string ServiceClientSecretKey = "ServiceClients:CloudApi:ClientSecret";

    public async Task<StoreClientCredentials> CreateAsync(string storeId, string displayName, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Put, $"/store-clients/{Uri.EscapeDataString(storeId)}",
            JsonContent.Create(new { displayName }), cancellationToken);

        return await response.Content.ReadFromJsonAsync<StoreClientCredentials>(cancellationToken)
            ?? throw new AuthUnavailableException("Auth answered the store-client request with no body.");
    }

    public async Task DeleteAsync(string storeId, CancellationToken cancellationToken = default)
    {
        using var _ = await SendAsync(HttpMethod.Delete, $"/store-clients/{Uri.EscapeDataString(storeId)}", null, cancellationToken);
    }

    public async Task DisableAsync(string storeId, CancellationToken cancellationToken = default)
    {
        using var _ = await SendAsync(HttpMethod.Post, $"/store-clients/{Uri.EscapeDataString(storeId)}/disable", null, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, HttpContent? content, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(method, path) { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await TokenAsync(cancellationToken));

            var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                return response;

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            response.Dispose();
            throw new AuthUnavailableException($"Auth answered {(int)response.StatusCode} to {method} {path}: {body}");
        }
        catch (Exception ex) when (ex is HttpRequestException || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            throw new AuthUnavailableException($"Auth could not be reached for {method} {path}.", ex);
        }
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
