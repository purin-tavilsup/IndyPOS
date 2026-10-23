using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IndyPOS.MigrationTool.Services;

public enum StoreTokenCheckOutcome { Match, Mismatch, NotChecked }

public sealed record StoreTokenCheckResult(StoreTokenCheckOutcome Outcome, string Message);

/// <summary>
/// The store's token from Cloud.Auth: a form post to /oauth/token (client credentials). Used for the cloud upload and
/// for the check that --store-id is the store the cloud knows these credentials as.
/// </summary>
public static class StoreTokenClient
{
    public static async Task<string> RequestTokenAsync(HttpClient auth, string clientId, string clientSecret, CancellationToken ct)
    {
        using var response = await auth.PostAsync("/oauth/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["scope"] = "sync.write"
        }), ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Cloud.Auth refused the store's credentials ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync(ct)}");

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(ct);
        return token?.AccessToken is { Length: > 0 } accessToken
            ? accessToken
            : throw new InvalidOperationException("Cloud.Auth's token response had no access_token.");
    }

    /// <remarks>
    /// A mismatch refuses the migration: every migrated event would carry --store-id and be refused by the cloud.
    /// When Auth cannot be asked, or refuses the credentials, the check is skipped with a message: the migration must
    /// not depend on the cloud, and a mismatch found later still shows as Failed events on the admin page.
    /// </remarks>
    public static async Task<StoreTokenCheckResult> CheckStoreIdAsync(HttpClient auth, string clientId, string clientSecret, string storeId, CancellationToken ct)
    {
        string? tokenStoreId;
        try
        {
            tokenStoreId = StoreIdIn(await RequestTokenAsync(auth, clientId, clientSecret, ct));
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or JsonException or FormatException
                                   || (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            // Unreachable, refused, or not Auth at all (a wrong URL answering a web page): say so and carry on.
            return new StoreTokenCheckResult(StoreTokenCheckOutcome.NotChecked, $"The store id could not be checked against Cloud.Auth: {ex.Message}");
        }

        return string.Equals(tokenStoreId, storeId, StringComparison.Ordinal)
            ? new StoreTokenCheckResult(StoreTokenCheckOutcome.Match, $"--store-id matches the cloud's store '{storeId}'.")
            : new StoreTokenCheckResult(StoreTokenCheckOutcome.Mismatch,
                $"--store-id '{storeId}' is not the store these credentials belong to in the cloud ('{tokenStoreId}'). Every migrated event would be refused.");
    }

    /// <summary>The token's store_id claim. Read, not validated: the token came straight from Auth over this call.</summary>
    private static string? StoreIdIn(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2)
            throw new FormatException("Cloud.Auth's access token is not a JWT.");

        var payload = parts[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

        using var document = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
        return document.RootElement.TryGetProperty("store_id", out var storeId) ? storeId.GetString() : null;
    }

    private sealed record TokenResponse([property: JsonPropertyName("access_token")] string? AccessToken);
}
