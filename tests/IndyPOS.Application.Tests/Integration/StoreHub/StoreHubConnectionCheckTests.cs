using System.Net;
using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Infrastructure.Services.StoreHub;
using Xunit;

namespace IndyPOS.Application.Tests.Integration.StoreHub;

public class StoreHubConnectionCheckTests
{
    private const string ConfiguredBaseUrl = "http://localhost:5012";
    private static readonly TimeSpan ShortTimeout = TimeSpan.FromMilliseconds(50);

    [Fact]
    public async Task CheckAsync_WithAConfiguredBaseUrl_ProbesItsReadyRoute()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var check = CheckWith(handler);

        await check.CheckAsync();

        handler.LastRequestUri.Should()
                              .Be(new Uri(ConfiguredBaseUrl + StoreHubRoutes.HealthReady));
    }

    [Fact]
    public async Task CheckAsync_WithACancelledToken_ThrowsOperationCanceled()
    {
        var check = CheckWith(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        var act = () => check.CheckAsync(cancelled.Token);

        await act.Should()
                 .ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task CheckAsync_WhenStoreHubIsNotListening_ReturnsUnreachable()
    {
        var check = CheckWith(new StubHandler(_ => throw new HttpRequestException("refused")));

        var result = await check.CheckAsync();

        result.Status.Should()
                     .Be(StoreHubConnectionStatus.Unreachable);
    }

    [Fact]
    public async Task CheckAsync_WhenStoreHubDoesNotAnswerInTime_ReturnsTimedOut()
    {
        var check = CheckWith(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK), delay: TimeSpan.FromSeconds(5)),
                              ShortTimeout);

        var result = await check.CheckAsync();

        result.Status.Should()
                     .Be(StoreHubConnectionStatus.TimedOut);
    }

    [Fact]
    public async Task CheckAsync_WhenTheDatabaseIsNotReady_ReturnsNotReady()
    {
        var check = CheckWith(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        var result = await check.CheckAsync();

        result.Status.Should()
                     .Be(StoreHubConnectionStatus.NotReady);
    }

    [Fact]
    public async Task CheckAsync_WithAnUnexpectedStatus_ReturnsUnexpectedWithTheCode()
    {
        var check = CheckWith(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));

        var result = await check.CheckAsync();

        result.Should()
              .Be(new StoreHubConnectionResult(StoreHubConnectionStatus.Unexpected, (int)HttpStatusCode.NotFound));
    }

    [Fact]
    public async Task CheckAsync_WhenStoreHubIsReady_ReturnsHealthy()
    {
        var check = CheckWith(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        var result = await check.CheckAsync();

        result.Status.Should()
                     .Be(StoreHubConnectionStatus.Healthy);
    }

    private static StoreHubConnectionCheck CheckWith(HttpMessageHandler handler, TimeSpan? timeout = null) =>
        new(new HttpClient(handler)
        {
            BaseAddress = new Uri(ConfiguredBaseUrl),
            Timeout = timeout ?? StoreHubConnectionCheck.Timeout
        });

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond, TimeSpan? delay = null)
        : HttpMessageHandler
    {
        public Uri? LastRequestUri { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastRequestUri = request.RequestUri;
            if (delay is { } wait)
                await Task.Delay(wait, cancellationToken);
            return respond(request);
        }
    }
}
