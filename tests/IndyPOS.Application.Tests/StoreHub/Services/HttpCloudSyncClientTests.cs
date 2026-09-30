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
/// The cloud answers 200 for a batch even when it failed to store an event, reporting the failure
/// per event. The client counted any 2xx as sent, so SyncWorker marked the event Sent and a single
/// ingest failure lost it for good. It now trusts only the cloud's per-event result.
/// </summary>
public class HttpCloudSyncClientTests
{
    // The cloud serialises with ASP.NET's web defaults, so the body is camelCase.
    private static readonly JsonSerializerOptions CloudJson = new(JsonSerializerDefaults.Web);

    private readonly OutboxEvent _event = new()
    {
        Id = Guid.NewGuid(),
        StoreId = "STORE-001",
        Type = "InvoiceCompleted",
        PayloadJson = """{"StoreId":"STORE-001"}""",
        CreatedUtc = DateTime.UtcNow
    };

    [Fact]
    public async Task SendEventAsync_WhenTheCloudFailedToStoreTheEvent_ReturnsFalse()
    {
        var client = ClientAnswering(HttpStatusCode.OK, ResponseWith(new SyncEventResult(_event.Id, Accepted: false, Reason: "database unavailable")));

        Assert.False(await client.SendEventAsync(_event));
    }

    [Fact]
    public async Task SendEventAsync_WhenTheResponseHasNoResultForTheEvent_ReturnsFalse()
    {
        var client = ClientAnswering(HttpStatusCode.OK, ResponseWith());

        Assert.False(await client.SendEventAsync(_event));
    }

    [Fact]
    public async Task SendEventAsync_WhenTheResultIsForAnotherEvent_ReturnsFalse()
    {
        var client = ClientAnswering(HttpStatusCode.OK, ResponseWith(new SyncEventResult(Guid.NewGuid(), Accepted: true)));

        Assert.False(await client.SendEventAsync(_event));
    }

    [Fact]
    public async Task SendEventAsync_WhenTheBodyIsNotASyncResponse_ReturnsFalse()
    {
        var client = ClientAnswering(HttpStatusCode.OK, "not json");

        Assert.False(await client.SendEventAsync(_event));
    }

    [Fact]
    public async Task SendEventAsync_WhenTheRetryAfterA401ReportsAFailure_ReturnsFalse()
    {
        var failed = ResponseWith(new SyncEventResult(_event.Id, Accepted: false, Reason: "database unavailable"));
        var answers = new Queue<(HttpStatusCode, string)>([(HttpStatusCode.Unauthorized, ""), (HttpStatusCode.OK, failed)]);
        var client = NewClient(_ => answers.Dequeue());

        Assert.False(await client.SendEventAsync(_event));
    }

    [Fact]
    public async Task SendEventAsync_WhenTheCloudForbidsTheEvent_ReturnsFalse()
    {
        var client = ClientAnswering(HttpStatusCode.Forbidden, "");

        Assert.False(await client.SendEventAsync(_event));
    }

    [Fact]
    public async Task SendEventAsync_WhenTheCloudAcceptedTheEvent_ReturnsTrue()
    {
        var client = ClientAnswering(HttpStatusCode.OK, ResponseWith(new SyncEventResult(_event.Id, Accepted: true)));

        Assert.True(await client.SendEventAsync(_event));
    }

    // The cloud already holds it, so the store may stop sending it.
    [Fact]
    public async Task SendEventAsync_WhenTheCloudReportsADuplicate_ReturnsTrue()
    {
        var client = ClientAnswering(HttpStatusCode.OK, ResponseWith(new SyncEventResult(_event.Id, Accepted: true, Reason: "duplicate")));

        Assert.True(await client.SendEventAsync(_event));
    }

    private static string ResponseWith(params SyncEventResult[] results) =>
        JsonSerializer.Serialize(
            new SyncEventsResponse(
                AcceptedCount: results.Count(r => r.Accepted),
                DuplicateCount: 0,
                FailedCount: results.Count(r => !r.Accepted),
                Results: results),
            CloudJson);

    private static HttpCloudSyncClient ClientAnswering(HttpStatusCode status, string body) =>
        NewClient(_ => (status, body));

    private static HttpCloudSyncClient NewClient(Func<HttpRequestMessage, (HttpStatusCode Status, string Body)> answer)
    {
        var tokens = new Mock<ITokenService>();
        tokens.Setup(t => t.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync("token");

        var http = new HttpClient(new FakeHttpMessageHandler(answer)) { BaseAddress = new Uri("https://cloud.example.com") };

        return new HttpCloudSyncClient(http, tokens.Object, NullLogger<HttpCloudSyncClient>.Instance);
    }

    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, (HttpStatusCode Status, string Body)> answer)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var (status, body) = answer(request);

            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }
}
