using System.Net;
using System.Text;
using FluentAssertions;
using IndyPOS.MigrationTool.Services;

namespace IndyPOS.MigrationTool.Tests;

/// <summary>
/// Pure unit tests. The migrator now gets its token from Cloud.Auth, as a form post to /oauth/token (it used to post
/// JSON to /connect/token and never got one). When Auth can be asked, the token's store_id must be the --store-id,
/// or every migrated event would be refused by the cloud.
/// </summary>
public class StoreTokenClientTests
{
    private const string StoreId = "STORE-001";

    private static string JwtFor(string storeId)
    {
        static string Part(string json) => Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return $"{Part("""{"alg":"RS256","typ":"at+jwt"}""")}.{Part($$"""{"store_id":"{{storeId}}","sub":"{{storeId}}"}""")}.c2ln";
    }

    private static HttpClient AuthAnswering(Func<HttpRequestMessage, HttpResponseMessage> answer) =>
        new(new FakeHandler(answer)) { BaseAddress = new Uri("https://auth.test/") };

    private static HttpResponseMessage TokenFor(string storeId) =>
        new(HttpStatusCode.OK) { Content = new StringContent($$"""{"access_token":"{{JwtFor(storeId)}}","token_type":"Bearer","expires_in":900}""", Encoding.UTF8, "application/json") };

    [Fact]
    public async Task RequestTokenAsync_PostsAFormToTheOauthTokenEndpoint()
    {
        HttpRequestMessage? sent = null;
        string? body = null;
        var auth = AuthAnswering(request =>
        {
            sent = request;
            body = request.Content!.ReadAsStringAsync().Result;
            return TokenFor(StoreId);
        });

        var token = await StoreTokenClient.RequestTokenAsync(auth, "store_STORE-001", "secret", CancellationToken.None);

        sent!.RequestUri!.AbsolutePath.Should().Be("/oauth/token");
        sent.Content!.Headers.ContentType!.MediaType.Should().Be("application/x-www-form-urlencoded");
        body.Should().Contain("grant_type=client_credentials");
        token.Should().Be(JwtFor(StoreId));
    }

    [Fact]
    public async Task CheckStoreIdAsync_WhenTheTokensStoreIdMatches_ReturnsAMatch()
    {
        var result = await StoreTokenClient.CheckStoreIdAsync(AuthAnswering(_ => TokenFor(StoreId)), "c", "s", StoreId, CancellationToken.None);

        result.Outcome.Should().Be(StoreTokenCheckOutcome.Match);
    }

    [Fact]
    public async Task CheckStoreIdAsync_WhenItDiffers_ReturnsAMismatchNamingBoth()
    {
        var result = await StoreTokenClient.CheckStoreIdAsync(AuthAnswering(_ => TokenFor("STORE-002")), "c", "s", StoreId, CancellationToken.None);

        result.Outcome.Should().Be(StoreTokenCheckOutcome.Mismatch);
        result.Message.Should().Contain("STORE-002").And.Contain(StoreId);
    }

    // Migration must not depend on the cloud: a mismatch found later still shows as Failed events on the admin page.
    [Fact]
    public async Task CheckStoreIdAsync_WhenAuthIsUnreachable_SaysSoAndLetsTheMigrationContinue()
    {
        var result = await StoreTokenClient.CheckStoreIdAsync(AuthAnswering(_ => throw new HttpRequestException("No such host is known.")), "c", "s", StoreId, CancellationToken.None);

        result.Outcome.Should().Be(StoreTokenCheckOutcome.NotChecked);
        result.Message.Should().Contain("could not");
    }

    [Fact]
    public async Task CheckStoreIdAsync_WhenAuthRefusesTheCredentials_SaysSoAndLetsTheMigrationContinue()
    {
        var result = await StoreTokenClient.CheckStoreIdAsync(AuthAnswering(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"error":"invalid_client"}""", Encoding.UTF8, "application/json")
        }), "c", "s", StoreId, CancellationToken.None);

        result.Outcome.Should().Be(StoreTokenCheckOutcome.NotChecked);
    }

    // A wrong --auth-url that answers with a web page used to crash the migrator with a JsonException.
    [Fact]
    public async Task CheckStoreIdAsync_WhenAuthAnswersSomethingThatIsNotJson_SaysSoAndLetsTheMigrationContinue()
    {
        var result = await StoreTokenClient.CheckStoreIdAsync(AuthAnswering(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("<html>not Auth</html>", Encoding.UTF8, "text/html")
        }), "c", "s", StoreId, CancellationToken.None);

        result.Outcome.Should().Be(StoreTokenCheckOutcome.NotChecked);
    }

    [Fact]
    public async Task CheckStoreIdAsync_WhenTheTokenIsNotAJwt_SaysSoAndLetsTheMigrationContinue()
    {
        var result = await StoreTokenClient.CheckStoreIdAsync(AuthAnswering(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"access_token":"not-a-jwt"}""", Encoding.UTF8, "application/json")
        }), "c", "s", StoreId, CancellationToken.None);

        result.Outcome.Should().Be(StoreTokenCheckOutcome.NotChecked);
    }

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(answer(request));
    }
}
