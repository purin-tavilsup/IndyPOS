using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub.Services;
using IndyPOS.Application.UseCases.Cloud.Sync.Heartbeat;
using IndyPOS.Infrastructure.Services.StoreHub;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Services;

/// <summary>
/// The heartbeat must never get in the way: any failure is a missed beat (null), never an exception, and a reply
/// from a newer cloud with fields this store does not know still counts.
/// </summary>
public class HttpCloudHeartbeatClientTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private static readonly HeartbeatRequest Beat = new("4.1.0", 0, 0, null, null, null);

    [Fact]
    public async Task SendAsync_WhenTheCloudAnswers_ReturnsItsResends()
    {
        var requestId = Guid.NewGuid();
        var client = ClientAnswering(HttpStatusCode.OK, JsonSerializer.Serialize(new HeartbeatResponse([new ResendInstruction(requestId, null)]), Web));

        var reply = await client.SendAsync(Beat);

        reply!.Resends.Should().ContainSingle(r => r.RequestId == requestId);
    }

    [Fact]
    public async Task SendAsync_WhenTheReplyHasFieldsItDoesNotKnow_ReturnsItsResends()
    {
        var requestId = Guid.NewGuid();
        var client = ClientAnswering(HttpStatusCode.OK, $$"""{"resends":[{"requestId":"{{requestId}}","eventId":null,"priority":9}],"newThing":true}""");

        var reply = await client.SendAsync(Beat);

        reply!.Resends.Should().ContainSingle(r => r.RequestId == requestId);
    }

    [Fact]
    public async Task SendAsync_WhenTheNetworkFails_ReturnsNull()
    {
        var client = NewClient(_ => throw new HttpRequestException("No such host is known."));

        (await client.SendAsync(Beat)).Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_WhenNoTokenCanBeObtained_ReturnsNull()
    {
        var client = NewClient(_ => throw new InvalidOperationException("no request should be sent"), token: null);

        (await client.SendAsync(Beat)).Should().BeNull();
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task SendAsync_WhenTheCloudAnswersAFailure_ReturnsNull(HttpStatusCode status)
    {
        var client = ClientAnswering(status, "");

        (await client.SendAsync(Beat)).Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_WhenTheFirstAnswerIs401_RetriesWithAFreshToken()
    {
        var answers = new Queue<HttpStatusCode>([HttpStatusCode.Unauthorized, HttpStatusCode.OK]);
        var client = NewClient(_ => Answer(answers.Dequeue(), """{"resends":[]}"""));

        (await client.SendAsync(Beat)).Should().NotBeNull();
    }

    private static HttpCloudHeartbeatClient ClientAnswering(HttpStatusCode status, string body) =>
        NewClient(_ => Answer(status, body));

    private static HttpResponseMessage Answer(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static HttpCloudHeartbeatClient NewClient(Func<HttpRequestMessage, HttpResponseMessage> answer, string? token = "token")
    {
        var tokens = new Mock<ITokenService>();
        tokens.Setup(t => t.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(token);

        var http = new HttpClient(new FakeHandler(answer)) { BaseAddress = new Uri("https://cloud.example.com") };

        return new HttpCloudHeartbeatClient(http, tokens.Object, NullLogger<HttpCloudHeartbeatClient>.Instance);
    }

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(answer(request));
    }
}
