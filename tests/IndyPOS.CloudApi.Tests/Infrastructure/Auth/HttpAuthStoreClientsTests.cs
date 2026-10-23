using System.Net;
using System.Text;
using FluentAssertions;
using IndyPOS.Application.Abstractions.Cloud.Auth;
using IndyPOS.CloudApi.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IndyPOS.CloudApi.Tests.Infrastructure.Auth;

/// <summary>
/// CloudApi's calls to Cloud.Auth's store-client API. A create must never be sent twice by the HTTP layer (a slow
/// first attempt could finish after the retry and leave Auth with another secret than the one shown), every way of
/// not reaching Auth is "unavailable", Auth's real refusal is a conflict, and Auth's own error text stays in the logs.
/// </summary>
public class HttpAuthStoreClientsTests
{
    private const string Token = """{"access_token":"service-token","token_type":"Bearer","expires_in":900}""";
    private const string Created = """{"clientId":"store_STORE-A","clientSecret":"secret"}""";

    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static IAuthStoreClients Client(Func<HttpRequestMessage, HttpResponseMessage> answer, bool withStandardResilience = false)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ServiceClients:CloudApi:ClientSecret"] = "service-secret",
            ["services:cloud-auth:https:0"] = "https://auth.test/"
        }).Build());

        if (withStandardResilience)
            services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler()); // as ServiceDefaults does

        services.AddAuthStoreClients();
        services.AddHttpClient<IAuthStoreClients, HttpAuthStoreClients>()
                .ConfigurePrimaryHttpMessageHandler(() => new FakeHandler(answer));

        return services.BuildServiceProvider().GetRequiredService<IAuthStoreClients>();
    }

    private static Func<HttpRequestMessage, HttpResponseMessage> TokenThen(Func<HttpRequestMessage, HttpResponseMessage> storeClients) =>
        request => request.RequestUri!.AbsolutePath == "/oauth/token" ? Json(HttpStatusCode.OK, Token) : storeClients(request);

    [Fact]
    public async Task CreateAsync_WhenAuthAnswers503_IsSentOnlyOnceEvenWithTheStandardRetries()
    {
        var puts = 0;
        var client = Client(TokenThen(_ =>
        {
            Interlocked.Increment(ref puts);
            return Json(HttpStatusCode.ServiceUnavailable, "");
        }), withStandardResilience: true);

        var act = () => client.CreateAsync("STORE-A", "Store A");

        await act.Should().ThrowAsync<AuthUnavailableException>();
        puts.Should().Be(1);
    }

    // The resilience pipeline's own failures (Polly's timeout or open circuit) are not HttpRequestExceptions, and
    // used to surface as a 500 instead of "Auth unavailable".
    [Fact]
    public async Task CreateAsync_WhenTheCallFailsWithoutAnAnswer_ThrowsAuthUnavailable()
    {
        var client = Client(TokenThen(_ => throw new TimeoutException("attempt timed out")));

        var act = () => client.CreateAsync("STORE-A", "Store A");

        await act.Should().ThrowAsync<AuthUnavailableException>();
    }

    [Fact]
    public async Task CreateAsync_WhenAuthSaysTheStoreWasDeactivated_ThrowsAConflict()
    {
        var client = Client(TokenThen(_ => Json(HttpStatusCode.Conflict, """{"error":"Store 'STORE-A' was deactivated."}""")));

        var act = () => client.CreateAsync("STORE-A", "Store A");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*deactivated*");
    }

    [Fact]
    public async Task CreateAsync_WhenAuthFails_KeepsItsResponseBodyOutOfTheMessage()
    {
        var client = Client(TokenThen(_ => Json(HttpStatusCode.InternalServerError, "<html>developer exception page with internals</html>")));

        var act = () => client.CreateAsync("STORE-A", "Store A");

        (await act.Should().ThrowAsync<AuthUnavailableException>()).Which.Message.Should().NotContain("internals");
    }

    // A rotated service secret or Auth key made the cached token useless for up to 15 minutes.
    [Fact]
    public async Task CreateAsync_WhenTheCachedServiceTokenIsRefused_GetsAFreshOneAndRetriesOnce()
    {
        var tokenRequests = 0;
        var puts = 0;
        var client = Client(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/oauth/token")
            {
                Interlocked.Increment(ref tokenRequests);
                return Json(HttpStatusCode.OK, Token);
            }

            return Interlocked.Increment(ref puts) == 1 ? Json(HttpStatusCode.Unauthorized, "") : Json(HttpStatusCode.OK, Created);
        });

        var credentials = await client.CreateAsync("STORE-A", "Store A");

        credentials.ClientSecret.Should().Be("secret");
        tokenRequests.Should().Be(2);
    }

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(answer(request));
    }
}
