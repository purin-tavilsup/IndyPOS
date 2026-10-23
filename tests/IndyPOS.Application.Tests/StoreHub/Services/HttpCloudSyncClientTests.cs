using System.Net;
using System.Text;
using System.Text.Json;
using IndyPOS.Application.Abstractions.StoreHub.Services;
using IndyPOS.Application.UseCases.Cloud.Sync;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Services.StoreHub;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Services;

/// <summary>
/// Every answer becomes Sent, RetryLater or Refused. Only the cloud's own marked refusal is Refused: it
/// parks the event as Failed for good, so a bare 4xx from a proxy, a missing route or the auth layer
/// must look like an outage instead. The cloud also answers 200 for a batch when it failed to store an
/// event, so only its per-event "accepted" counts as Sent.
/// </summary>
public class HttpCloudSyncClientTests
{
    // The cloud serialises with ASP.NET's web defaults, so the body is camelCase.
    private static readonly JsonSerializerOptions CloudJson = new(JsonSerializerDefaults.Web);

    private const string Json = "application/json";
    private const string Problem = "application/problem+json";

    private readonly OutboxEvent _event = new()
    {
        Id = Guid.NewGuid(),
        StoreId = "STORE-001",
        Type = "InvoiceCompleted",
        PayloadJson = """{"StoreId":"STORE-001"}""",
        CreatedUtc = DateTime.UtcNow
    };

    [Fact]
    public async Task SendEventAsync_WhenTheCloudFailedToStoreTheEvent_ReturnsRetryLater()
    {
        var client = ClientAnswering(HttpStatusCode.OK, ResponseWith(new SyncEventResult(_event.Id, Accepted: false, Reason: "database unavailable")));

        Assert.Equal(SyncSendOutcome.RetryLater, (await client.SendEventAsync(_event)).Outcome);
    }

    [Fact]
    public async Task SendEventAsync_WhenTheResponseHasNoResultForTheEvent_ReturnsRetryLater()
    {
        var client = ClientAnswering(HttpStatusCode.OK, ResponseWith());

        Assert.Equal(SyncSendOutcome.RetryLater, (await client.SendEventAsync(_event)).Outcome);
    }

    [Fact]
    public async Task SendEventAsync_WhenTheResultIsForAnotherEvent_ReturnsRetryLater()
    {
        var client = ClientAnswering(HttpStatusCode.OK, ResponseWith(new SyncEventResult(Guid.NewGuid(), Accepted: true)));

        Assert.Equal(SyncSendOutcome.RetryLater, (await client.SendEventAsync(_event)).Outcome);
    }

    [Fact]
    public async Task SendEventAsync_WhenTheBodyIsNotASyncResponse_ReturnsRetryLater()
    {
        var client = ClientAnswering(HttpStatusCode.OK, "not json");

        Assert.Equal(SyncSendOutcome.RetryLater, (await client.SendEventAsync(_event)).Outcome);
    }

    [Fact]
    public async Task SendEventAsync_WhenTheRetryAfterA401ReportsAFailure_ReturnsRetryLater()
    {
        var failed = ResponseWith(new SyncEventResult(_event.Id, Accepted: false, Reason: "database unavailable"));
        var client = ClientAnsweringInTurn((HttpStatusCode.Unauthorized, "", Json), (HttpStatusCode.OK, failed, Json));

        Assert.Equal(SyncSendOutcome.RetryLater, (await client.SendEventAsync(_event)).Outcome);
    }

    [Fact]
    public async Task SendEventAsync_WhenTheRetryAfterA401IsRefused_ReturnsRefused()
    {
        var refusal = RefusalBody(403, SyncRefusal.StoreMismatch);
        var client = ClientAnsweringInTurn((HttpStatusCode.Unauthorized, "", Json), (HttpStatusCode.Forbidden, refusal, Problem));

        Assert.Equal(SyncSendOutcome.Refused, (await client.SendEventAsync(_event)).Outcome);
    }

    [Fact]
    public async Task SendEventAsync_WhenTheCloudRefusesTheBatchAsMalformed_ReturnsRefused()
    {
        var client = ClientAnswering(HttpStatusCode.BadRequest, RefusalBody(400, SyncRefusal.MalformedBatch), Problem);

        var result = await client.SendEventAsync(_event);

        Assert.Equal(SyncSendOutcome.Refused, result.Outcome);
    }

    [Fact]
    public async Task SendEventAsync_WhenTheCloudRefusesTheStore_ReturnsRefused()
    {
        var client = ClientAnswering(HttpStatusCode.Forbidden, RefusalBody(403, SyncRefusal.StoreMismatch), Problem);

        var result = await client.SendEventAsync(_event);

        Assert.Equal(SyncSendOutcome.Refused, result.Outcome);
    }

    // The reason is what Pond reads on the admin page, so it must say what the cloud said.
    [Fact]
    public async Task SendEventAsync_WhenTheCloudRefusesTheStore_ReturnsTheCloudsReason()
    {
        var client = ClientAnswering(HttpStatusCode.Forbidden, RefusalBody(403, SyncRefusal.StoreMismatch), Problem);

        var result = await client.SendEventAsync(_event);

        Assert.Equal("403 store-mismatch: refused for the test", result.Reason);
    }

    [Fact]
    public async Task SendEventAsync_WhenTheCloudForbidsTheEvent_ReturnsRetryLater()
    {
        var client = ClientAnswering(HttpStatusCode.Forbidden, "");

        Assert.Equal(SyncSendOutcome.RetryLater, (await client.SendEventAsync(_event)).Outcome);
    }

    [Fact]
    public async Task SendEventAsync_WithAProblemThatIsNotASyncRefusal_ReturnsRetryLater()
    {
        var problem = """{"title":"Forbidden","status":403,"detail":"policy says no"}""";
        var client = ClientAnswering(HttpStatusCode.Forbidden, problem, Problem);

        Assert.Equal(SyncSendOutcome.RetryLater, (await client.SendEventAsync(_event)).Outcome);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.RequestEntityTooLarge)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task SendEventAsync_WithAnUnmarkedFailureStatus_ReturnsRetryLater(HttpStatusCode status)
    {
        var client = ClientAnswering(status, "<html>gateway says no</html>", "text/html");

        Assert.Equal(SyncSendOutcome.RetryLater, (await client.SendEventAsync(_event)).Outcome);
    }

    [Fact]
    public async Task SendEventAsync_WhenNoTokenCanBeObtained_ReturnsRetryLater()
    {
        var client = NewClient(_ => throw new InvalidOperationException("no request should be sent"), token: null);

        Assert.Equal(SyncSendOutcome.RetryLater, (await client.SendEventAsync(_event)).Outcome);
    }

    [Fact]
    public async Task SendEventAsync_WhenTheNetworkFails_ReturnsRetryLater()
    {
        var client = NewClient(_ => throw new HttpRequestException("No such host is known."));

        Assert.Equal(SyncSendOutcome.RetryLater, (await client.SendEventAsync(_event)).Outcome);
    }

    [Fact]
    public async Task SendEventAsync_WhenTheCloudAcceptedTheEvent_ReturnsSent()
    {
        var client = ClientAnswering(HttpStatusCode.OK, ResponseWith(new SyncEventResult(_event.Id, Accepted: true)));

        Assert.Equal(SyncSendOutcome.Sent, (await client.SendEventAsync(_event)).Outcome);
    }

    // The cloud already holds it, so the store may stop sending it.
    [Fact]
    public async Task SendEventAsync_WhenTheCloudReportsADuplicate_ReturnsSent()
    {
        var client = ClientAnswering(HttpStatusCode.OK, ResponseWith(new SyncEventResult(_event.Id, Accepted: true, Reason: "duplicate")));

        Assert.Equal(SyncSendOutcome.Sent, (await client.SendEventAsync(_event)).Outcome);
    }

    private static string ResponseWith(params SyncEventResult[] results) =>
        JsonSerializer.Serialize(
            new SyncEventsResponse(
                AcceptedCount: results.Count(r => r.Accepted),
                DuplicateCount: 0,
                FailedCount: results.Count(r => !r.Accepted),
                Results: results),
            CloudJson);

    /// <summary>The problem details body CloudApi's SyncEventsEndpoints writes for a refusal.</summary>
    private static string RefusalBody(int status, string refusal) =>
        JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["title"] = status == 400 ? "Bad Request" : "Forbidden",
            ["status"] = status,
            ["detail"] = "refused for the test",
            [SyncRefusal.ExtensionKey] = refusal
        });

    private static HttpCloudSyncClient ClientAnswering(HttpStatusCode status, string body, string mediaType = Json) =>
        NewClient(_ => Answer(status, body, mediaType));

    private static HttpCloudSyncClient ClientAnsweringInTurn(params (HttpStatusCode Status, string Body, string MediaType)[] answers)
    {
        var queue = new Queue<(HttpStatusCode Status, string Body, string MediaType)>(answers);

        return NewClient(_ =>
        {
            var (status, body, mediaType) = queue.Dequeue();
            return Answer(status, body, mediaType);
        });
    }

    private static HttpResponseMessage Answer(HttpStatusCode status, string body, string mediaType) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, mediaType) };

    private static HttpCloudSyncClient NewClient(Func<HttpRequestMessage, HttpResponseMessage> answer, string? token = "token")
    {
        var tokens = new Mock<ITokenService>();
        tokens.Setup(t => t.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(token);

        var http = new HttpClient(new FakeHttpMessageHandler(answer)) { BaseAddress = new Uri("https://cloud.example.com") };

        return new HttpCloudSyncClient(http, tokens.Object, NullLogger<HttpCloudSyncClient>.Instance);
    }

    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(answer(request));
    }
}
